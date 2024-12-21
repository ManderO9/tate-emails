using System.Text;

namespace TateEmails;

public class Email
{
    public required string Title { get; set; }
    public required List<BaseEmailContent> Content { get; set; }
    public required DateTime CreatedDate { get; set; }

    public string RenderMarkdownPage()
    {
        var markdown = new StringBuilder();
        
        markdown.AppendLine($"# {Title.Trim()}");

        foreach(var content in Content)
        {
            markdown.AppendLine(content.GetContent());
        }

        return markdown.ToString();
    }
}
