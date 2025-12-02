using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models
{
    public class UrlMapping
    {
        [Key]
        [StringLength(10)]
        public string ShortCode { get; set; }

        [Required]
        [Url]
        public string OriginalUrl { get; set; }

        public Guid? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [StringLength(500)]
        public string Title { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public long ClickCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<UrlClick> Clicks { get; set; }
    }
}
