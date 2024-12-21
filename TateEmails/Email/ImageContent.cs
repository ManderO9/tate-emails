namespace TateEmails;

public class ImageContent : BaseEmailContent
{
    public required string ImageUrl { get; set; }
    public required string AltText { get; set; }
    public override string GetContent() => $"![{AltText}]({ImageUrl})";
}
