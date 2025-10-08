using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Linqyard.Infra.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Linqyard.Infra
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _cacheDirectory;

        public CloudinaryService(
            IOptions<CloudinarySettings> settings,
            ILogger<CloudinaryService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

            if (string.IsNullOrWhiteSpace(_settings.CloudName) ||
                string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.ApiSecret))
            {
                throw new InvalidOperationException("Cloudinary settings are not configured correctly.");
            }

            _cacheDirectory = ResolveCacheDirectory(_settings.CacheDirectory);
            EnsureCacheDirectoryExists(_cacheDirectory);

            var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string? publicId = null,
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

            byte[] fileBytes;
            await using (var buffer = new MemoryStream())
            {
                await fileStream.CopyToAsync(buffer, cancellationToken);
                fileBytes = buffer.ToArray();
            }

            var isDeterministicPublicId = !string.IsNullOrWhiteSpace(publicId);
            await using var uploadStream = new MemoryStream(fileBytes, writable: false);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, uploadStream),
                Folder = string.IsNullOrWhiteSpace(_settings.Folder) ? null : _settings.Folder,
                UseFilename = false,
                UniqueFilename = !isDeterministicPublicId,
                Overwrite = isDeterministicPublicId,
                Invalidate = isDeterministicPublicId
            };

            if (isDeterministicPublicId)
            {
                uploadParams.PublicId = publicId;
            }

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error is not null)
            {
                _logger.LogError("Cloudinary upload failed for file {FileName}: {Message}", fileName, uploadResult.Error.Message);
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            var effectivePublicId = uploadResult.PublicId;
            var extension = ResolveExtension(uploadResult.Format, contentType);
            ClearCachedFiles(effectivePublicId);
            var localPath = BuildLocalPath(effectivePublicId, extension);

            await using (var localFile = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await localFile.WriteAsync(fileBytes, cancellationToken);
            }

            _logger.LogInformation("Image {PublicId} uploaded to Cloudinary and cached at {LocalPath}", effectivePublicId, localPath);

            var url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty;
            return new CloudinaryUploadResult(effectivePublicId, url, localPath);
        }

        public async Task<CachedImageResult?> GetImageAsync(string publicId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return null;
            }

            var existingPath = FindCachedFile(publicId);
            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                return new CachedImageResult(existingPath!, GetContentTypeFromExtension(Path.GetExtension(existingPath)), Path.GetFileName(existingPath));
            }

            var resourceResult = await _cloudinary.GetResourceAsync(new GetResourceParams(publicId));

            if (resourceResult.Error is not null)
            {
                _logger.LogWarning("Failed to find Cloudinary resource {PublicId}: {Message}", publicId, resourceResult.Error.Message);
                return null;
            }

            var downloadUrl = resourceResult.SecureUrl ?? resourceResult.Url;
            if (downloadUrl is null)
            {
                _logger.LogWarning("Cloudinary resource {PublicId} did not return a download URL", publicId);
                return null;
            }

            var extension = ResolveExtension(resourceResult.Format, ExtractExtensionFromUrl(downloadUrl));
            var localPath = BuildLocalPath(publicId, extension);

            var client = _httpClientFactory.CreateClient(nameof(CloudinaryService));
            using var response = await client.GetAsync(downloadUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download Cloudinary resource {PublicId}. Status code: {StatusCode}", publicId, response.StatusCode);
                return null;
            }

            await using (var localFile = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await response.Content.CopyToAsync(localFile, cancellationToken);
            }

            var contentType = response.Content.Headers.ContentType?.ToString();
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = GetContentTypeFromExtension(Path.GetExtension(localPath));
            }

            _logger.LogInformation("Cached Cloudinary resource {PublicId} to {LocalPath}", publicId, localPath);

            return new CachedImageResult(localPath, contentType ?? "application/octet-stream", Path.GetFileName(localPath));
        }

        private void ClearCachedFiles(string publicId)
        {
            var safeId = SanitizePublicId(publicId);
            var searchPattern = $"{safeId}.*";

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
                        _logger.LogWarning(ex, "Failed to remove cached image {CachedPath} while refreshing {PublicId}", match, publicId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate cached files for {PublicId}", publicId);
            }
        }

        private string? FindCachedFile(string publicId)
        {
            var safeId = SanitizePublicId(publicId);
            var searchPattern = $"{safeId}.*";
            var matches = Directory.GetFiles(_cacheDirectory, searchPattern, SearchOption.TopDirectoryOnly);
            return matches.FirstOrDefault();
        }

        private string BuildLocalPath(string publicId, string extension)
        {
            var safeId = SanitizePublicId(publicId);
            var normalizedExtension = NormalizeExtension(extension);
            return Path.Combine(_cacheDirectory, $"{safeId}.{normalizedExtension}");
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

        private static string SanitizePublicId(string publicId)
        {
            return publicId.Replace('\\', '_').Replace('/', '_');
        }

        private static string ResolveExtension(string? primary, string? secondary)
        {
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary;
            }

            if (string.IsNullOrWhiteSpace(secondary))
            {
                return "jpg";
            }

            if (secondary.Contains('/'))
            {
                return MapContentTypeToExtension(secondary);
            }

            if (secondary.StartsWith('.'))
            {
                secondary = secondary[1..];
            }

            return MapContentTypeToExtension(secondary);
        }

        private static string MapContentTypeToExtension(string? contentTypeOrFormat)
        {
            if (string.IsNullOrWhiteSpace(contentTypeOrFormat))
            {
                return "jpg";
            }

            var key = contentTypeOrFormat.Trim().ToLowerInvariant();

            return key switch
            {
                "jpeg" => "jpg",
                "image/jpeg" => "jpg",
                "jpg" => "jpg",
                "image/png" => "png",
                "png" => "png",
                "image/gif" => "gif",
                "gif" => "gif",
                "image/webp" => "webp",
                "webp" => "webp",
                "image/bmp" => "bmp",
                "bmp" => "bmp",
                "image/tiff" => "tiff",
                "tiff" => "tiff",
                "image/svg+xml" => "svg",
                "svg" => "svg",
                _ => "jpg"
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

        private static string? ExtractExtensionFromUrl(string downloadUrl)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                return null;
            }

            if (Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
            {
                var extension = Path.GetExtension(uri.AbsolutePath);
                return string.IsNullOrWhiteSpace(extension) ? null : extension;
            }

            var fallbackExtension = Path.GetExtension(downloadUrl);
            return string.IsNullOrWhiteSpace(fallbackExtension) ? null : fallbackExtension;
        }
    }
}

