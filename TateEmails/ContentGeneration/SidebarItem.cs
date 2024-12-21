
namespace TateEmails;

public class SidebarItem
{
    public required string Text { get; set; }
    public bool Collapsed { get; set; }
    public string? Link { get; set; }
    public List<SidebarItem>? Items { get; set; }
}