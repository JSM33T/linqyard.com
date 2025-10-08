namespace Linqyard.Infra
{
    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, string contentType, string? publicId = null, CancellationToken cancellationToken = default);
        Task<CachedImageResult?> GetImageAsync(string publicId, CancellationToken cancellationToken = default);
    }

    public sealed record CloudinaryUploadResult(string PublicId, string Url, string LocalPath);

    public sealed record CachedImageResult(string FilePath, string ContentType, string FileName);
}

