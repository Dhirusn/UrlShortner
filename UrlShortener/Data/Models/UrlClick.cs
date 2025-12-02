using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Data.Models
{
    public class UrlClick
    {
        public long Id { get; set; }

        [StringLength(10)]
        public string ShortCode { get; set; }
        public UrlMapping UrlMapping { get; set; }

        public DateTime ClickedAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Referrer { get; set; }
        public string CountryCode { get; set; }
        public string DeviceType { get; set; } // "desktop", "mobile", "tablet"
    }
}
