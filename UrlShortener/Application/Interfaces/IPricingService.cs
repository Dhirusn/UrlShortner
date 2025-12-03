using UrlShortener.Data.Models;
using UrlShortener.Models.ViewModels;

namespace UrlShortener.Application.Interfaces
{
    public interface IPricingService
    {
        Task<PricingViewModel> GetPricingPageDataAsync();
        Task<PricingPlan> GetPlanByIdAsync(int id);
        Task<List<FAQ>> GetFAQsAsync();
        Task<string> StartTrialAsync(PlanSelectionModel model);
    }
}
