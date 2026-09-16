using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Common.Services;

public class AzureBlobService : IAzureBlobService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobService> _logger;
    private readonly string? _connectionString;
    private readonly string _containerName;

    public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = _configuration["AzureStorage:ConnectionString"];
        _containerName = _configuration["AzureStorage:ContainerName"] ?? "product-images";
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        // 1. Production Mode: If Azure Connection String is configured, upload to Azure Blob Storage
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            try
            {
                _logger.LogInformation("Uploading {FileName} to Azure Blob Storage container: {ContainerName}", fileName, _containerName);
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var uniqueBlobName = $"{Guid.NewGuid()}_{fileName}";
                var blobClient = containerClient.GetBlobClient(uniqueBlobName);

                fileStream.Position = 0;
                await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

                _logger.LogInformation("Successfully uploaded {FileName} to Azure Blob. URI: {Uri}", fileName, blobClient.Uri);
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to Azure Blob Storage. Falling back to local storage simulator.");
            }
        }

        // 2. Development / Local Fallback Mode: Saves to local storage folder when no Azure credentials provided
        _logger.LogInformation("Azure Storage connection string not found. Using local file storage fallback for development.");
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueLocalFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueLocalFileName);

        fileStream.Position = 0;
        using (var outputStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outputStream);
        }

        var localUrl = $"/uploads/{uniqueLocalFileName}";
        _logger.LogInformation("Saved file locally at: {Path}. Accessible via {Url}", filePath, localUrl);
        return localUrl;
    }

    public async Task<bool> DeleteFileAsync(string fileName)
    {
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(fileName);
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from Azure Blob Storage.");
                return false;
            }
        }

        var localFilePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileName);
        if (File.Exists(localFilePath))
        {
            File.Delete(localFilePath);
            return true;
        }

        return false;
    }
}
