using System.Text.Json.Serialization;

namespace TateEmails;

[JsonDerivedType(typeof(BaseEmailContent), typeDiscriminator: nameof(BaseEmailContent))]
[JsonDerivedType(typeof(LinkContent), typeDiscriminator: nameof(LinkContent))]
[JsonDerivedType(typeof(TextContent), typeDiscriminator: nameof(TextContent))]
[JsonDerivedType(typeof(ImageContent), typeDiscriminator: nameof(ImageContent))]
public class BaseEmailContent
{
    public virtual string GetContent() { throw new NotImplementedException(); }
}
