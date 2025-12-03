using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models
{
    // Feature Category Model
    public class FeatureCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }
        public string IconClass { get; set; }
        public List<FeatureItem> Features { get; set; } = new List<FeatureItem>();
    }
}
