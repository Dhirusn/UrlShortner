using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models
{
    // ViewModel for Pricing Page


    // Model for Plan Selection
    public class PlanSelectionModel
    {
        [Required]
        public int PlanId { get; set; }

        [Required]
        public string BillingPeriod { get; set; } // "monthly" or "yearly"

        [EmailAddress]
        [Required]
        public string Email { get; set; }

        public string PromoCode { get; set; }

        // User details for trial
        public string Name { get; set; }
        public string Company { get; set; }
        public string Phone { get; set; }
    }
}
