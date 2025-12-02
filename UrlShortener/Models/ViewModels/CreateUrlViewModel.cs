using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Models.ViewModels
{
    public class CreateUrlViewModel
    {
        [Required(ErrorMessage = "URL is required")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Long URL")]
        public string OriginalUrl { get; set; }

        [StringLength(20, MinimumLength = 3, ErrorMessage = "Custom alias must be between 3 and 20 characters")]
        [RegularExpression("^[a-zA-Z0-9_-]+$", ErrorMessage = "Only letters, numbers, hyphens, and underscores allowed")]
        [Display(Name = "Custom Alias")]
        public string CustomAlias { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
