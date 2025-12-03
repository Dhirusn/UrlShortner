using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models
{
    public class PricingPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(100)]
        public string Description { get; set; }

        [DataType(DataType.Currency)]
        public decimal MonthlyPrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal YearlyPrice { get; set; }

        public bool IsPopular { get; set; }
        public bool IsActive { get; set; } = true;
        public string RecommendedFor { get; set; }
        public string ButtonText { get; set; } = "Get Started";
        public string ButtonCssClass { get; set; } = "btn-primary";

        // Features included in this plan
        public List<PricingFeature> Features { get; set; } = new List<PricingFeature>();

        // Calculated property for yearly savings
        public decimal YearlySavingsPercentage => YearlyPrice > 0 ?
            ((MonthlyPrice * 12) - YearlyPrice) / (MonthlyPrice * 12) * 100 : 0;
    }
}
