# Azure Blob Storage Migration - Quick Start

## What Changed?

Your `MediaController` now uses **Azure Blob Storage** instead of **Cloudinary** for image uploads and storage.

## Quick Configuration

1. **Get Azure Storage Connection String**:
   ```
   Azure Portal → Storage Account → Access Keys → Copy Connection String
   ```

2. **Update appsettings.json**:
   ```json
   "AzureBlobStorage": {
     "ConnectionString": "YOUR_AZURE_CONNECTION_STRING_HERE",
     "ContainerName": "media",
     "CacheDirectory": "cache/media",
     "UseLocalCache": true
   }
   ```

3. **For Local Development (Optional)**:
   Use Azurite emulator:
   ```json
   "ConnectionString": "UseDevelopmentStorage=true"
   ```

## API Endpoints (Unchanged)

- `POST /media` - Upload image
- `POST /media/avatar` - Upload avatar
- `POST /media/cover` - Upload cover
- `GET /media/{blobName}` - Get image

## Key Changes

| Before | After |
|--------|-------|
| `publicId` | `blobName` |
| Cloudinary URLs | Azure Blob URLs |
| ICloudinaryService | IAzureBlobStorageService |

## Frontend Changes Required

Update your frontend to use `blobName` instead of `publicId`:

```javascript
// Before
const { publicId, url } = response.data;

// After
const { blobName, url } = response.data;
```

## Files Modified

✅ MediaController.cs - Uses Azure Blob Storage service  
✅ Program.cs - Registers Azure service  
✅ AzureBlobStorageService.cs - New service implementation  
✅ Linqyard.Infra.csproj - Added Azure.Storage.Blobs package  
✅ appsettings.json - Added AzureBlobStorage configuration  

## Testing

1. Update connection string in appsettings.json
2. Run the API: `dotnet run --project Linqyard.Api`
3. Test upload: `POST /media` with a file
4. Test retrieve: `GET /media/{blobName}`

## Need Help?

See: `AzureBlobStorage-Migration.md` for detailed documentation.

---
✨ Migration completed successfully!
