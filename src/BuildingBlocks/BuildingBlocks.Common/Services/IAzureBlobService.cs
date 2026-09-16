namespace BuildingBlocks.Common.Services;

public interface IAzureBlobService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteFileAsync(string fileName);
}
