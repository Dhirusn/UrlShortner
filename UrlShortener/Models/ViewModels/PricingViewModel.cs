using UrlShortener.Data.Models;

namespace UrlShortener.Models.ViewModels
{
    public class PricingViewModel
    {
        public List<PricingPlan> Plans { get; set; } = new List<PricingPlan>();
        public List<FeatureCategory> FeatureCategories { get; set; } = new List<FeatureCategory>();
        public List<FAQ> FAQs { get; set; } = new List<FAQ>();
        public string PageTitle { get; set; } = "Find a Plan";
        public string PageDescription { get; set; }
        public bool IsAnnualBilling { get; set; } = true;
        public string BrandedLinksDescription { get; set; }
        public string FeatureBreakdownDescription { get; set; }
    }
}
