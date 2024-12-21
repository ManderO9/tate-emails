
namespace TateEmails;

public class LinkContent : BaseEmailContent
{
    public required string Url { get; set; }
    public required string LinkText { get; set; }
    public override string GetContent() => $"[{LinkText}]({Url})";
}