using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models
{
    // Feature Model
    public class PricingFeature
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }
        public bool IsAvailable { get; set; }
        public string IconClass { get; set; } = "fas fa-check";
        public int DisplayOrder { get; set; }
    }
}
