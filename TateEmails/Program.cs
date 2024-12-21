
using System.Runtime.CompilerServices;
using System.Text.Json;
using TateEmails;


var currentFolder = GetCurrentFolder();


var emailReader = new EmailReader();

var emailsPath = Path.Combine(currentFolder, "emails.json");

var emails = JsonSerializer.Deserialize<List<Email>>(File.ReadAllText(emailsPath))!;

var latest = await emailReader.ReadLatestEmailsAsync(currentFolder, emails);

if(latest.Count > 0)
{
    emails.AddRange(latest);

    File.WriteAllText(emailsPath, JsonSerializer.Serialize(emails));
}

var contentManager = new ContentManager();
contentManager.GeneratePreview(currentFolder);

Console.ReadLine();
Console.ReadLine();



string GetCurrentFolder([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
