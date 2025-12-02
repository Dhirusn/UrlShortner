using UrlShortener.Data.Models;

namespace UrlShortener.Models.ViewModels
{
    public class DashboardViewModel
    {
        public List<UrlMapping> Urls { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
