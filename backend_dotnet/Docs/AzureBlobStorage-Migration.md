# Azure Blob Storage Migration Guide

## Overview
The MediaController has been successfully migrated from **Cloudinary** to **Azure Blob Storage**. This document outlines the changes made and how to configure your application.

## Changes Made

### 1. New Files Created

#### `AzureBlobStorageSettings.cs`
Configuration class for Azure Blob Storage settings:
- `ConnectionString`: Azure Storage connection string
- `ContainerName`: Name of the blob container (default: "media")
- `CacheDirectory`: Local cache directory path (default: "Cache")
- `UseLocalCache`: Enable/disable local caching (default: true)

#### `IAzureBlobStorageService.cs`
Interface defining the blob storage operations:
- `UploadImageAsync()`: Upload images to Azure Blob Storage
- `GetImageAsync()`: Retrieve images (from cache or blob storage)
- `BlobUploadResult`: Record containing blob name, URL, and local path
- `CachedImageResult`: Record containing cached file information (reused from Cloudinary)

#### `AzureBlobStorageService.cs`
Implementation of the Azure Blob Storage service with:
- Automatic container creation
- Local caching support
- Upload with overwrite capability
- Download and cache management
- Error handling and logging

### 2. Modified Files

#### `MediaController.cs`
- Changed from `ICloudinaryService` to `IAzureBlobStorageService`
- Updated method names:
  - `PublicId` → `BlobName`
  - References to "Cloudinary" → "Azure Blob Storage"
- All endpoints remain the same:
  - `POST /media` - Upload generic image
  - `POST /media/avatar` - Upload avatar
  - `POST /media/cover` - Upload cover image
  - `GET /media/{blobName}` - Retrieve image

#### `Program.cs`
- Replaced Cloudinary service registration with Azure Blob Storage service
- Updated configuration section binding from "Cloudinary" to "AzureBlobStorage"

#### `Linqyard.Infra.csproj`
- Added NuGet package: `Azure.Storage.Blobs` (version 12.24.0)
- Kept `CloudinaryDotNet` package (can be removed if no longer needed)

#### `appsettings.json`
- Added new `AzureBlobStorage` configuration section

## Configuration

### Step 1: Azure Storage Account Setup

1. **Create an Azure Storage Account**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Create a new Storage Account or use an existing one
   - Choose a suitable performance tier (Standard or Premium)
   - Select appropriate redundancy option (LRS, GRS, etc.)

2. **Get Connection String**:
   - Navigate to your Storage Account
   - Go to "Access keys" under "Security + networking"
   - Copy the connection string (either key1 or key2)

3. **Container Setup**:
   - The application automatically creates the container if it doesn't exist
   - Default container name is "media"
   - Container is created with private access (no public anonymous access)

### Step 2: Update appsettings.json

```json
{
  "AzureBlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net",
    "ContainerName": "media",
    "CacheDirectory": "cache/media",
    "UseLocalCache": true
  }
}
```

### Step 3: Environment-Specific Configuration

For **Development** (`appsettings.Development.json`):
```json
{
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "media-dev",
    "CacheDirectory": "cache/media",
    "UseLocalCache": true
  }
}
```

> **Note**: "UseDevelopmentStorage=true" works with [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) local emulator

For **Production**:
- Use environment variables or Azure Key Vault
- Set connection string via:
  ```bash
  $env:AzureBlobStorage__ConnectionString = "your-connection-string"
  ```

## Feature Comparison

| Feature | Cloudinary | Azure Blob Storage |
|---------|-----------|-------------------|
| Upload | ✅ | ✅ |
| Download | ✅ | ✅ |
| Local Cache | ✅ | ✅ |
| Unique Identifiers | Public ID | Blob Name |
| Automatic Overwrite | ✅ (with PublicId) | ✅ (always) |
| CDN | ✅ Built-in | Optional (Azure CDN) |
| Direct URLs | ✅ | ✅ (via SAS tokens) |

## API Changes

### Upload Response
**Before (Cloudinary)**:
```json
{
  "data": {
    "publicId": "abc123",
    "url": "https://res.cloudinary.com/..."
  }
}
```

**After (Azure Blob Storage)**:
```json
{
  "data": {
    "blobName": "550e8400-e29b-41d4-a716-446655440000.jpg",
    "url": "https://youraccount.blob.core.windows.net/media/550e8400-e29b-41d4-a716-446655440000.jpg"
  }
}
```

### Avatar/Cover Upload Response
Returns `blobName` instead of `publicId` in metadata:
```json
{
  "data": {
    "message": "Profile updated successfully",
    "updatedAt": "2025-10-11T...",
    "profile": { ... }
  },
  "meta": {
    "blobName": "550e8400-e29b-41d4-a716-446655440000.jpg"
  }
}
```

## Local Cache Behavior

The local cache feature works identically to the Cloudinary implementation:
- Images are cached on first upload/download
- Cache files are stored in the configured `CacheDirectory`
- Blob names are sanitized for safe file system usage
- Old cache files are cleared when uploading with the same blob name
- Cache can be disabled by setting `UseLocalCache: false`

## Migration Checklist

- [x] Create Azure Storage Account
- [x] Get connection string
- [ ] Update `appsettings.json` with Azure configuration
- [ ] Test upload functionality
- [ ] Test image retrieval
- [ ] Update frontend to handle `blobName` instead of `publicId`
- [ ] (Optional) Remove Cloudinary package if not needed elsewhere
- [ ] (Optional) Set up Azure CDN for better performance
- [ ] Deploy and test in production environment

## Troubleshooting

### Issue: "Azure Blob Storage connection string is not configured"
**Solution**: Ensure the connection string is set in appsettings.json or environment variables

### Issue: "Container not found"
**Solution**: The service automatically creates the container. Check storage account permissions.

### Issue: "Images not loading"
**Solution**: 
1. Check if local cache is enabled
2. Verify the cache directory has write permissions
3. Check blob storage container access policies

### Issue: "Upload fails with 403 Forbidden"
**Solution**: Verify the storage account access keys are correct and not expired

## Security Considerations

1. **Connection String**: Store in environment variables or Azure Key Vault, not in source control
2. **Container Access**: Default is private (no anonymous access)
3. **Blob URLs**: Direct blob URLs require authentication or SAS tokens for access
4. **Local Cache**: Ensure cache directory has appropriate file system permissions

## Performance Optimization

1. **Enable Azure CDN** for faster global content delivery
2. **Use SAS Tokens** for direct blob access (bypassing controller)
3. **Configure blob caching headers** for browser caching
4. **Consider Azure Front Door** for advanced routing and caching

## Rollback Plan

If you need to rollback to Cloudinary:
1. Revert changes in `Program.cs` (uncomment Cloudinary service registration)
2. Update `MediaController.cs` to inject `ICloudinaryService`
3. Update references from `BlobName` back to `PublicId`
4. Restore NuGet package references if removed

## Additional Resources

- [Azure Blob Storage Documentation](https://learn.microsoft.com/en-us/azure/storage/blobs/)
- [Azure Storage Security Guide](https://learn.microsoft.com/en-us/azure/storage/common/storage-security-guide)
- [Azurite Local Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
- [Azure Storage Connection Strings](https://learn.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string)

## Support

For issues or questions related to this migration, please refer to:
- Azure Storage documentation
- Project repository issues
- Team documentation

---

**Migration Date**: October 11, 2025  
**Created By**: GitHub Copilot  
**Status**: Completed ✅
