namespace UrlShortener.Data.Models
{
    public class FeatureItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IconClass { get; set; }
        public string ColorClass { get; set; } = "text-primary";
    }
}
