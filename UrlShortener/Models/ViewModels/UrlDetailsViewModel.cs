namespace UrlShortener.Models.ViewModels
{
    public class UrlDetailsViewModel
    {
        public string ShortCode { get; set; }
        public string OriginalUrl { get; set; }
        public string ShortUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public long ClickCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        // Daily stats for the last 7 days
        public Dictionary<DateTime, int> DailyClicks { get; set; }

        // Top referrers
        public List<ReferrerStat> TopReferrers { get; set; }
    }

    public class ReferrerStat
    {
        public string Referrer { get; set; }
        public int Count { get; set; }
    }
}
