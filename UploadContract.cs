using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

public class UploadContract
{
    private readonly ILogger<UploadContract> _logger;
    private readonly ShareClient _shareClient;

    public UploadContract(ILogger<UploadContract> logger)
    {
        _logger = logger;
        _shareClient = new ShareClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "contracts-and-logs");
    }

    [Function("UploadContract")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Uploading contract file to Azure File Share.");
        var file = req.Form.Files["File"];

        if (file == null || file.Length == 0)
        {
            return new BadRequestObjectResult("No file uploaded.");
        }

        await _shareClient.CreateIfNotExistsAsync();
        var shareDirectoryClient = _shareClient.GetRootDirectoryClient();
        var shareFileClient = shareDirectoryClient.GetFileClient(file.FileName);

        using (var stream = file.OpenReadStream())
        {
            await shareFileClient.CreateAsync(stream.Length);
            await shareFileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
        }

        return new OkObjectResult($"File {file.FileName} uploaded successfully.");
    }
}
