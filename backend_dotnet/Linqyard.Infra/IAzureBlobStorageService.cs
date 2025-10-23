namespace Linqyard.Infra
{
    public interface IAzureBlobStorageService
    {
        Task<BlobUploadResult> UploadImageAsync(Stream fileStream, string fileName, string contentType, string? blobName = null, CancellationToken cancellationToken = default);
        Task<CachedImageResult?> GetImageAsync(string blobName, CancellationToken cancellationToken = default);
        Task<CachedImageResult?> GetImageByUrlAsync(string blobUrl, CancellationToken cancellationToken = default);
    }

    public sealed record BlobUploadResult(string BlobName, string Url, string LocalPath);
}
