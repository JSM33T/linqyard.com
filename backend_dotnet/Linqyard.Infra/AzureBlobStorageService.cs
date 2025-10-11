using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Linqyard.Infra.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Linqyard.Infra
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly AzureBlobStorageSettings _settings;
        private readonly ILogger<AzureBlobStorageService> _logger;
        private readonly string _cacheDirectory;

        public AzureBlobStorageService(
            IOptions<AzureBlobStorageSettings> settings,
            ILogger<AzureBlobStorageService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_settings.ContainerName))
            {
                throw new InvalidOperationException("Azure Blob Storage container name is not configured.");
            }

            _cacheDirectory = ResolveCacheDirectory(_settings.CacheDirectory);
            
            if (_settings.UseLocalCache)
            {
                EnsureCacheDirectoryExists(_cacheDirectory);
            }

            var blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            
            // Ensure container exists
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<BlobUploadResult> UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string? blobName = null,
            CancellationToken cancellationToken = default)
        {
            if (fileStream is null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (!fileStream.CanRead)
            {
                throw new InvalidOperationException("Provided stream cannot be read.");
            }

            // Generate blob name if not provided
            var effectiveBlobName = string.IsNullOrWhiteSpace(blobName)
                ? $"{Guid.NewGuid():N}{GetExtensionFromContentType(contentType)}"
                : SanitizeBlobName(blobName);

            // Read stream into memory for both upload and cache
            byte[] fileBytes;
            await using (var buffer = new MemoryStream())
            {
                await fileStream.CopyToAsync(buffer, cancellationToken);
                fileBytes = buffer.ToArray();
            }

            // Upload to Azure Blob Storage
            await using var uploadStream = new MemoryStream(fileBytes, writable: false);
            
            var blobClient = _containerClient.GetBlobClient(effectiveBlobName);
            
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            };

            try
            {
                await blobClient.UploadAsync(
                    uploadStream,
                    new BlobUploadOptions
                    {
                        HttpHeaders = blobHttpHeaders,
                        Conditions = null // Allow overwrite
                    },
                    cancellationToken);

                _logger.LogInformation("Image {BlobName} uploaded to Azure Blob Storage", effectiveBlobName);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Blob Storage upload failed for file {FileName}", fileName);
                throw new InvalidOperationException($"Azure Blob Storage upload failed: {ex.Message}", ex);
            }

            // Cache locally if enabled
            string localPath = string.Empty;
            if (_settings.UseLocalCache)
            {
                var extension = GetExtensionFromContentType(contentType);
                localPath = BuildLocalPath(effectiveBlobName, extension);

                ClearCachedFiles(effectiveBlobName);

                await using (var localFile = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await localFile.WriteAsync(fileBytes, cancellationToken);
                }

                _logger.LogInformation("Image {BlobName} cached at {LocalPath}", effectiveBlobName, localPath);
            }

            var url = blobClient.Uri.ToString();
            return new BlobUploadResult(effectiveBlobName, url, localPath);
        }

        public async Task<CachedImageResult?> GetImageAsync(string blobName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return null;
            }

            // Check local cache first if enabled
            if (_settings.UseLocalCache)
            {
                var existingPath = FindCachedFile(blobName);
                if (!string.IsNullOrWhiteSpace(existingPath))
                {
                    _logger.LogDebug("Returning cached image for {BlobName}", blobName);
                    return new CachedImageResult(
                        existingPath!, 
                        GetContentTypeFromExtension(Path.GetExtension(existingPath)), 
                        Path.GetFileName(existingPath));
                }
            }

            // Download from Azure Blob Storage
            var blobClient = _containerClient.GetBlobClient(blobName);

            try
            {
                if (!await blobClient.ExistsAsync(cancellationToken))
                {
                    _logger.LogWarning("Blob {BlobName} not found in Azure Blob Storage", blobName);
                    return null;
                }

                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                var contentType = properties.Value.ContentType ?? "application/octet-stream";

                if (_settings.UseLocalCache)
                {
                    // Download and cache
                    var extension = GetExtensionFromContentType(contentType);
                    var localPath = BuildLocalPath(blobName, extension);

                    await using (var localFile = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await blobClient.DownloadToAsync(localFile, cancellationToken);
                    }

                    _logger.LogInformation("Cached blob {BlobName} to {LocalPath}", blobName, localPath);

                    return new CachedImageResult(localPath, contentType, Path.GetFileName(localPath));
                }
                else
                {
                    // Return blob URL for direct access (not recommended for private blobs)
                    _logger.LogWarning("Local cache disabled, returning blob URL directly");
                    return null;
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to download blob {BlobName}", blobName);
                return null;
            }
        }

        private void ClearCachedFiles(string blobName)
        {
            if (!_settings.UseLocalCache)
            {
                return;
            }

            var safeName = SanitizeBlobName(blobName);
            var searchPattern = $"{safeName}.*";

            try
            {
                var matches = Directory.GetFiles(_cacheDirectory, searchPattern, SearchOption.TopDirectoryOnly);

                foreach (var match in matches)
                {
                    try
                    {
                        File.Delete(match);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to remove cached image {CachedPath} while refreshing {BlobName}", match, blobName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate cached files for {BlobName}", blobName);
            }
        }

        private string? FindCachedFile(string blobName)
        {
            if (!_settings.UseLocalCache)
            {
                return null;
            }

            var safeName = SanitizeBlobName(blobName);
            var searchPattern = $"{safeName}.*";
            
            try
            {
                var matches = Directory.GetFiles(_cacheDirectory, searchPattern, SearchOption.TopDirectoryOnly);
                return matches.Length > 0 ? matches[0] : null;
            }
            catch
            {
                return null;
            }
        }

        private string BuildLocalPath(string blobName, string extension)
        {
            var safeName = SanitizeBlobName(blobName);
            var normalizedExtension = NormalizeExtension(extension);
            return Path.Combine(_cacheDirectory, $"{safeName}.{normalizedExtension}");
        }

        private static string SanitizeBlobName(string blobName)
        {
            return blobName.Replace('\\', '_').Replace('/', '_').Replace(':', '_');
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return "jpg";
            }

            extension = extension.Trim();
            if (extension.StartsWith('.'))
            {
                extension = extension[1..];
            }

            return extension.ToLowerInvariant();
        }

        private static string GetExtensionFromContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return ".jpg";
            }

            var key = contentType.Trim().ToLowerInvariant();

            return key switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/tiff" => ".tiff",
                "image/svg+xml" => ".svg",
                _ => ".jpg"
            };
        }

        private static string GetContentTypeFromExtension(string? extension)
        {
            var ext = NormalizeExtension(extension ?? string.Empty);
            return ext switch
            {
                "jpg" => "image/jpeg",
                "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "webp" => "image/webp",
                "bmp" => "image/bmp",
                "tiff" => "image/tiff",
                "svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        private static void EnsureCacheDirectoryExists(string cacheDirectory)
        {
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
        }

        private static string ResolveCacheDirectory(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.Combine(AppContext.BaseDirectory, "Cache");
            }

            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
        }
    }
}
