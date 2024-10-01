using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

public class UploadProductImage
{
    private readonly ILogger<UploadProductImage> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public UploadProductImage(ILogger<UploadProductImage> logger)
    {
        _logger = logger;
        _blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
    }

    [Function("UploadProductImage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Uploading product image to Blob Storage.");

        var file = req.Form.Files["image"];
        if (file == null || file.Length == 0)
        {
            return new BadRequestObjectResult("No file uploaded.");
        }

        var blobClient = _blobServiceClient.GetBlobContainerClient("product-images");
        await blobClient.CreateIfNotExistsAsync();

        var blob = blobClient.GetBlobClient(file.FileName);
        using (var stream = file.OpenReadStream())
        {
            await blob.UploadAsync(stream);
        }

        return new OkObjectResult($"File {file.FileName} uploaded successfully to Blob Storage.");
    }
}
