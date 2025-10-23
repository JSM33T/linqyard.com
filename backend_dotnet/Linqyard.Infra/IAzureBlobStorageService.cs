namespace Linqyard.Infra
{
    /// <summary>
    /// Defines the contract for Azure Blob Storage operations used by Linqyard.
    /// </summary>
    public interface IAzureBlobStorageService
    {
        /// <summary>
        /// Uploads an image stream to Azure Blob Storage asynchronously.
        /// </summary>
        /// <param name="fileStream">The stream containing the image data to upload.</param>
        /// <param name="fileName">The original file name of the image.</param>
        /// <param name="contentType">The MIME content type of the image (e.g., <c>image/png</c>).</param>
        /// <param name="blobName">An optional blob name to assign. If not provided, one will be generated.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous upload operation.
        /// The task result contains the <see cref="BlobUploadResult"/> including the blob name, public URL, and local path.
        /// </returns>
        Task<BlobUploadResult> UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string? blobName = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an image from Azure Blob Storage by its blob name.
        /// </summary>
        /// <param name="blobName">The name of the blob to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous retrieval operation.
        /// The task result contains a <see cref="CachedImageResult"/> if found; otherwise, <c>null</c>.
        /// </returns>
        Task<CachedImageResult?> GetImageAsync(
            string blobName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an image from Azure Blob Storage using its full blob URL.
        /// </summary>
        /// <param name="blobUrl">The full URL of the blob to retrieve.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous retrieval operation.
        /// The task result contains a <see cref="CachedImageResult"/> if found; otherwise, <c>null</c>.
        /// </returns>
        Task<CachedImageResult?> GetImageByUrlAsync(
            string blobUrl,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the result of a successful blob upload operation.
    /// </summary>
    /// <param name="BlobName">The unique name of the blob in Azure Storage.</param>
    /// <param name="Url">The public URL of the uploaded blob.</param>
    /// <param name="LocalPath">The local file system path (if applicable) where the blob was temporarily stored.</param>
    public sealed record BlobUploadResult(string BlobName, string Url, string LocalPath);

    /// <summary>
    /// Represents a cached image retrieved from Azure Blob Storage.
    /// </summary>
    /// <param name="FilePath">The local or cached file system path to the image.</param>
    /// <param name="ContentType">The MIME content type of the image.</param>
    /// <param name="FileName">The original or derived file name of the image.</param>
    public sealed record CachedImageResult(string FilePath, string ContentType, string FileName);
}
