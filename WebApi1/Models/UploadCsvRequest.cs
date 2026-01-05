using Microsoft.AspNetCore.Http;

public class UploadCsvRequest
{
    public List<IFormFile> Files { get; set; } = new();
}
