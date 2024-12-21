
using System.Text;

namespace TateEmails;

public class TextContent : BaseEmailContent
{
    public required string Content { get; set; }

    private static string[] mSpecialChars = ["\\", "`", "*", "_", "{", "}", "[", "]", "(", ")", "#", "+", "-", ".", "!", "|", "<", ">"];

    public override string GetContent()
    {
        var output = new StringBuilder(Content);

        foreach(var specialChar in mSpecialChars)
            output.Replace(specialChar, "\\" + specialChar);

        return output.ToString();
    }
}
