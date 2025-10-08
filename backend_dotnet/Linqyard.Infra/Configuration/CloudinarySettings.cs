namespace Linqyard.Infra.Configuration
{
    public class CloudinarySettings
    {
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string? Folder { get; set; }
        public string CacheDirectory { get; set; } = "Cache";
    }
}
