
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using MailKit.Security;
using MailKit.Net.Imap;
using MailKit;
using MailKit.Search;
using System.Text.Json;
using MimeKit.Text;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using MimeKit;

namespace TateEmails;

public class EmailReader
{

    private string clientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? "";
    private string clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? "";
    private string gmailAccount = Environment.GetEnvironmentVariable("USER_EMAIL") ?? "";

    public async Task<List<Email>> ReadLatestEmailsAsync(string currentFolder, List<Email> existingEmails)
    {
        var clientSecrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        var tokenFolder = Path.Combine(currentFolder, "CredentialCacheFolder");
        if(!Directory.Exists(tokenFolder))
            Directory.CreateDirectory(tokenFolder);

        var tokenFile = Path.Combine(tokenFolder, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-" + gmailAccount);
        File.WriteAllText(tokenFile, Environment.GetEnvironmentVariable("ACCESS_TOKEN"));


        var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            DataStore = new FileDataStore(Path.Combine(currentFolder, "CredentialCacheFolder"), true),
            Scopes = ["https://mail.google.com/"],
            ClientSecrets = clientSecrets,
            LoginHint = gmailAccount
        });

        // Note: For a web app, you'll want to use AuthorizationCodeWebApp instead.
        var codeReceiver = new LocalServerCodeReceiver();
        var authCode = new AuthorizationCodeInstalledApp(codeFlow, codeReceiver);

        var credential = await authCode.AuthorizeAsync(gmailAccount, CancellationToken.None);

        if(credential.Token.IsStale)
            await credential.RefreshTokenAsync(CancellationToken.None);

        var oauth2 = new SaslMechanismOAuth2(credential.UserId, credential.Token.AccessToken);

        var emails = new List<Email>();

        using(var client = new ImapClient())
        {
            await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(oauth2);

            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

            var messages = new List<MimeMessage>();

            var lastDate = existingEmails.Select(x => x.CreatedDate).Max();

            var uids = await client.Inbox.SearchAsync(SearchQuery.FromContains("tate@cobratate.com").And(SearchQuery.SentSince(lastDate.AddDays(1))));

            foreach(var id in uids)
            {
                var message = await client.Inbox.GetMessageAsync(id);

                var email = new Email()
                {
                    Content = TranslateContentFromHtml(message.HtmlBody, message.Subject),
                    CreatedDate = message.Date.DateTime,
                    Title = message.Subject
                };

                emails.Add(email);
            }


            await client.DisconnectAsync(true);

            return emails;
        }
    }

    public Task GenerateEmailsAsync()
    {
        var filePath = @"C:\Users\nnnn\Desktop\tate-emails.raw.json";
        var outputPath = @"C:\Users\nnnn\Desktop\tate-emails.content.json";

        var json = File.ReadAllText(filePath);
        var emails = JsonSerializer.Deserialize<List<EmailMessage>>(json)!;

        var sb = new StringBuilder();

        int i = 0;

        var newEmails = new List<Email>();

        foreach(var email in emails)
        {
            var content = TranslateContentFromHtml(email.HtmlContent, email.Subject);
            var newEmail = new Email()
            {
                Content = content,
                CreatedDate = email.TimeSent.DateTime,
                Title = email.Subject
            };

            newEmails.Add(newEmail);
            Console.WriteLine(i++ + ": " + email.Subject);
        }


        json = JsonSerializer.Serialize(newEmails, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(outputPath, json);

        return Task.CompletedTask;
    }


    public List<BaseEmailContent> TranslateContentFromHtml(string html, string title)
    {

        var stringBuilder = new StringBuilder();

        // Create html document
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Get all text nodes that are relevant
        var nodes = doc.DocumentNode.Descendants().Where(n =>
            n.NodeType == HtmlNodeType.Text &&
            n.ParentNode.Name != "script" &&
            n.ParentNode.Name != "style");

        // Append the text to the string builder html decoded
        foreach(var node in nodes)
            stringBuilder.AppendLine(HtmlUtils.HtmlDecode(node.InnerText));

        // Get the string result
        var str = stringBuilder.ToString();

        // Get the length of the subject string
        var subjectLength = title.Trim().Length;

        // Get the start index at the start of the subject
        var start = str.IndexOf(title, StringComparison.OrdinalIgnoreCase);

        // Get the end index at the end of the email
        var end = str.IndexOf("Don't want to receive these e-mails", StringComparison.OrdinalIgnoreCase);

        if(end == -1)
            end = str.IndexOf("Cobratate", StringComparison.OrdinalIgnoreCase);

        if(end == -1)
            end = str.Length;


        // Get the length of the content by removing the start plus subject length, until the end
        var length = end - start - subjectLength;

        // Get final trimmed string 
        var output = str.Substring(start + subjectLength, length).Trim();

        // Replace multiple new lines with only two
        output = Regex.Replace(output, @"(?<!\r\n\s?)\r\n\s?(?!\r\n\s?)|[\r\n\s?]{3,}", "\r\n\r\n");

        // Get list of email content by parsing links and images
        var content = ParseEmailContentFromText(output);

        return content;
    }


    private List<BaseEmailContent> ParseEmailContentFromText(string text)
    {

        var output = new List<BaseEmailContent>();

        var regex = @"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)";

        var matches = Regex.Matches(text, regex);

        var index = 0;

        foreach(Match match in matches)
        {
            if(match.Success)
            {

                output.Add(new TextContent() { Content = text.Substring(index, match.Index - index - 1) });
                if(match.Value.StartsWith("image://"))
                {
                    var imageName = match.Value.Replace("image://", "");

                    output.Add(new ImageContent() { ImageUrl = "/assets/" + imageName, AltText = imageName });
                }
                else
                {
                    output.Add(new LinkContent() { LinkText = match.Value, Url = match.Value.StartsWith("http") ? match.Value : "https://" + match.Value });
                }

                index = match.Index + match.Length;
            }
        }
        if(index < text.Length - 1)
        {
            output.Add(new TextContent() { Content = text.Substring(index) });
        }

        output.RemoveAll(x => x.GetContent().Length == 0);

        return output;
    }
}


file class EmailMessage
{
    public string Subject { get; set; }
    public DateTimeOffset TimeSent { get; set; }
    public string HtmlContent { get; set; }
    public string TextContent { get; set; }
}
