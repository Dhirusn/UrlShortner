using Microsoft.AspNetCore.Identity;
using UrlShortener.Data.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Provider { get; set; }
    public string ProviderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UrlMapping>? UrlMappings { get; set; }
}
