using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;

    public BlobStorageService(IConfiguration config) => _config = config;

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        var connection = _config["AzureStorage:ConnectionString"];
        var containerName = _config["AzureStorage:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Azure Storage is not configured.");

        var container = new BlobContainerClient(connection, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }
}