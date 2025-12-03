using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models
{
    // FAQ Model
    public class FAQ
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
    }
}
