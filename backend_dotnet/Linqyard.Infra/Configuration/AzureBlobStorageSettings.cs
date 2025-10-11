namespace Linqyard.Infra.Configuration
{
    public class AzureBlobStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "media";
        public string CacheDirectory { get; set; } = "Cache";
        public bool UseLocalCache { get; set; } = true;
    }
}
