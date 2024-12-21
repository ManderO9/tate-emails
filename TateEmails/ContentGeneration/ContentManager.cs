using System.Text.Json.Serialization;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using X.Web.Sitemap;
using X.Web.Sitemap.Extensions;

namespace TateEmails;

public class ContentManager
{
    public string GetConfigFileContent(SidebarItem links, Email latestEmail)
    {
        var linksContent = JsonSerializer.Serialize(links, options: new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        return $$"""

        import { defineConfig } from 'vitepress'

        export default defineConfig({
          title: "Tate Newsletter",
          description: "Find all the emails Andrew Tate sends in his newsletter",
          base: '/tate-emails/',
                
          head: [
            ['link', { rel: 'icon', href: '/tate-emails/favicon.ico' }],
          ],

          themeConfig: {
            nav: [
              { text: 'Emails', link: '{{GetFilePath(latestEmail)}}' },
              { text: 'Home', link: '/' },
              { text: 'Sign up', link: 'https://www.cobratate.com/newsletter' },
            ],
            
            sidebar: [
              {{linksContent}}
            ],

            socialLinks: [
              { icon: 'github', link: 'https://github.com/ManderO9' }
            ]
          }
        })
        """;
    }

    public string GetIndexFileContent(List<Email> emails)
    {
        var orderedEmails = emails.OrderByDescending(x => x.CreatedDate);

        var latestEmail = orderedEmails.First();
        var oldestEmail = orderedEmails.Last();

        // TODO: add an emails link to the top bar with a link to the latest email

        return $$"""
        ---
        # https://vitepress.dev/reference/default-theme-home-page
        layout: home

        hero:
          name: "Tate Newsletter"
          text: "Stories that enspire the masses"
          tagline: You can find a list of all the emails that Andrew Tate sends in his Newsletter
          actions:
            - theme: brand
              text: Sign up to the newsletter
              link: https://www.cobratate.com/newsletter
            - theme: brand
              text: Oldest Email Andrew Sent
              link: {{GetFilePath(oldestEmail)}}
            - theme: brand
              text: Latest Email Andrew Sent
              link: {{GetFilePath(latestEmail)}}

        features:
          - title: Lookup email history
            details: see the list of emails by date, you can find all of what you need here
          - title: Neatly organized
            details: super simple to browse around
        ---
        """;

    }

    public string GetSitemapFileContent(string baseUrl, List<Email> emails)
    {
        var sitemap = new Sitemap
        {
            new()
            {
                ChangeFrequency = ChangeFrequency.Daily,
                TimeStamp = DateTime.Now,
                Location = $"{baseUrl}/index.html",
                Priority = 0.9,
            }
        };

        emails = emails.OrderBy(x=>x.CreatedDate).ToList();

    
        for(var i = 0; i < emails.Count; i++)
        {
            var email = emails[i];
            var priority = 0.5;
            if(i == 0) priority = 0.9;
            if(i == 1) priority = 0.7;

            if(i == emails.Count - 1) priority = 0.9;
            if(i == emails.Count - 2) priority = 0.8;
            if(i == emails.Count - 3) priority = 0.7;
            if(i == emails.Count - 4) priority = 0.7;
            if(i == emails.Count - 5) priority = 0.6;


            var fileName = email.CreatedDate.ToString("yyyy-MM-dd") + ".html";

            var year = email.CreatedDate.Year.ToString();
            var month = email.CreatedDate.ToString("MMMM").ToLower();
            var day = email.CreatedDate.ToString("dd");

            var filePath = $"/emails/{year}/{month}/{fileName}";

            sitemap.Add(new Url()
            {
                ChangeFrequency = ChangeFrequency.Never,
                TimeStamp = email.CreatedDate,
                Location = $"{baseUrl}{filePath}",
                Priority = priority,

                // TODO: add images
                //Images = email.Content.Where(x=> x is ImageContent).Select(x=>
                //{
                //    var image = (x as ImageContent)!;

                //    return new Image() { Caption = image.AltText, Location = $"{baseUrl}{image.ImageUrl}"};
                //}).ToList()
            });
        }

        return sitemap.ToXml();
    }

    private string GetFilePath(Email email)
    {
        var fileName = email.CreatedDate.ToString("yyyy-MM-dd") + ".md";

        var year = email.CreatedDate.Year.ToString();
        var month = email.CreatedDate.ToString("MMMM").ToLower();
        var day = email.CreatedDate.ToString("dd");

        return $"/emails/{year}/{month}/{fileName}";
    }

    public SidebarItem GenerateMarkdownFiles(List<Email> emails, string outputDirectory)
    {

        var links = new SidebarItem() { Text = "Emails", Collapsed = false, Items = [] };

        foreach(var email in emails)
        {

            var fileContent = email.RenderMarkdownPage();
            var fileName = email.CreatedDate.ToString("yyyy-MM-dd") + ".md";

            var year = email.CreatedDate.Year.ToString();
            var month = email.CreatedDate.ToString("MMMM").ToLower();
            var day = email.CreatedDate.ToString("dd");

            var filePath = Path.Combine(outputDirectory, "emails", year, month, fileName);

            var routeName = day + " - " + email.Title.Trim();

            if(!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            File.WriteAllText(filePath, fileContent);

            var yearItem = links.Items.FirstOrDefault(x => x.Text == year);
            if(yearItem == null)
            {
                yearItem = new SidebarItem() { Collapsed = true, Text = year, Items = [] };
                links.Items.Add(yearItem);
            }

            var monthItem = yearItem.Items!.FirstOrDefault(x => x.Text == month);

            if(monthItem == null)
            {
                monthItem = new SidebarItem() { Collapsed = true, Text = month, Items = [] };
                yearItem.Items!.Add(monthItem);
            }

            monthItem.Items!.Add(new SidebarItem() { Text = routeName, Link = GetFilePath(email) });
        }



        var maxDate = emails.Max(x => x.CreatedDate);

        var yearOfItem = links.Items.FirstOrDefault(x => x.Text == maxDate.Year.ToString());
        var monthOfItem = yearOfItem.Items.FirstOrDefault(x => x.Text == maxDate.ToString("MMMM").ToLower());

        yearOfItem.Collapsed = false;
        monthOfItem.Collapsed = false;


        return links;
    }

    public List<Email> GetEmailsList(string inputFilePath)
    {

        var emails = JsonSerializer.Deserialize<List<Email>>(File.ReadAllText(inputFilePath));

        // Order the mails by creation date
        return emails.OrderBy(x => x.CreatedDate).ToList();
    }

    public void GenerateHtmlFromMarkdown(string outputDirectory)
    {
        var process = new Process();
        var fileName = "npm";
        var args = "--prefix=vite-template run docs:build";

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName = "cmd.exe";
            args = $"/c cd {outputDirectory} && npm run docs:build";
        }

        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = args;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;

        process.Start();
        process.WaitForExit();
    }


    public void AddEmail(string emailsFilePath, Email email)
    {
        var emails = JsonSerializer.Deserialize<List<Email>>(File.ReadAllText(emailsFilePath));

        if(emails.Any(x => x.CreatedDate.Date == email.CreatedDate.Date))
        {
            throw new Exception("Email with the same date already exists");
        }
        emails.Add(email);

        File.WriteAllText(emailsFilePath, JsonSerializer.Serialize(emails));
    }


    public string GetRobotsTxtFileContent(string sitemapPath, List<Email> emails)
    {
        var content = Environment.NewLine;

        //content += "Allow: /";

        //foreach(var email in emails)
        //{

        //    var fileName = email.CreatedDate.ToString("yyyy-MM-dd") + ".html";

        //    var year = email.CreatedDate.Year.ToString();
        //    var month = email.CreatedDate.ToString("MMMM").ToLower();
        //    var day = email.CreatedDate.ToString("dd");

        //    var filePath = $"/emails/{year}/{month}/{fileName}";

        //    content += Environment.NewLine;
        //    content += "Allow: /";

        //}

        content += "Sitemap: " + sitemapPath + Environment.NewLine;

        return content;
    }

    public void GeneratePreview(string currentFolder)
    {

        var start = DateTime.Now;
        var time = DateTime.Now;

        Console.WriteLine("Starting up...");

        var baseUrl = "https://ManderO9.github.io/tate-emails";

        var inputFileName = "emails.json";
        var inputFilePath = Path.Combine(currentFolder, inputFileName);

        var outputDirectory = Path.Combine(currentFolder, "vite-template");


        time = LogWithTime("Reading emails from file...", time);
        // Read the content of the emails
        var emails = GetEmailsList(inputFilePath);

        time = LogWithTime("Generating markdown files...", time);
        // Generate markdown files and get links to the files
        var links = GenerateMarkdownFiles(emails, outputDirectory);

        var latestEmail = emails.OrderByDescending(x => x.CreatedDate).First();

        time = LogWithTime("Creating config file...", time);
        // Generate config file content using the links
        var configFileContent = GetConfigFileContent(links, latestEmail);

        var configFolderPath = Path.Combine(outputDirectory, ".vitepress");

        if(!Directory.Exists(configFolderPath))
            Directory.CreateDirectory(configFolderPath);

        // Write the config file content to the config file
        File.WriteAllText(Path.Combine(configFolderPath, "config.mts"), configFileContent);

        time = LogWithTime("Creating index file...", time);
        // Create content for the index file
        var indexFileContent = GetIndexFileContent(emails);
        var indexFilePath = Path.Combine(outputDirectory, "index.md");

        // Write the index file content to the index file
        File.WriteAllText(indexFilePath, indexFileContent);

        time = LogWithTime("Generating Html using markdown files...", time);
        // Publish the markdown files to html
        GenerateHtmlFromMarkdown(outputDirectory);


        time = LogWithTime("Creating sitemap file...", time);
        // Create content for the sitemap file
        var sitemapContent = GetSitemapFileContent(baseUrl, emails);

        var webroot = Path.Combine(configFolderPath, "dist");
        var sitemapFileName = "sitemap.xml";

        // Get file to write sitemap into
        var sitemapFilePath = Path.Combine(webroot, sitemapFileName);
        if(!Directory.Exists(sitemapFilePath))
            Directory.CreateDirectory(Path.GetDirectoryName(sitemapFilePath)!);

        // Write the sitemap file content to the sitemap file
        File.WriteAllText(sitemapFilePath, sitemapContent);


        time = LogWithTime("Creating robots.txt file...", time);
        // Create content for the robots.txt file
        var robotsTxtContent = GetRobotsTxtFileContent(baseUrl + "/" +  sitemapFileName, emails);

        // Get file to write robots.txt into
        var robotsFilePath = Path.Combine(webroot, "robots.txt");

        // Write the robots file content to the robots file
        File.WriteAllText(robotsFilePath, robotsTxtContent);

        time = LogWithTime("Program Finished. " + Environment.NewLine + "press enter twice to exit", time);

        var totalDuration = DateTime.Now - start;

        Console.WriteLine($"[Total time]: {totalDuration.Seconds}s {totalDuration.Milliseconds}ms");
    }

    private DateTime LogWithTime(string message, DateTime oldTime)
    {
        var time = DateTime.Now;
        var duration = time - oldTime;

        Console.WriteLine($"[Duration]: ............................ {duration.Seconds}s {duration.Milliseconds}ms");
        Console.WriteLine(message);

        return time;
    }
}
