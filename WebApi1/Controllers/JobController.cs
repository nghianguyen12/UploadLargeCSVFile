using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi1.Controllers;

[ApiController]
[Route("[controller]")]
public class JobController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    public JobController(AppDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
    }

    [HttpGet(Name = "Get")]
    public ActionResult<string> Get()
    {
        return "OK";
    }

     /// <summary>
    /// Upload a CSV file and create an ImportJob
    /// </summary>
    [HttpPost("imports")]
    public async Task<IActionResult> UploadCsv([FromForm] UploadCsvRequest request)
    {
        if (request.Files == null || request.Files.Count == 0)
            return BadRequest("At least one file is required");

        var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);

        var publisher = new RabbitMqPublisher(
            hostname: "localhost",
            queueName: "jobs",
            username: "guest",
            password: "guest"
        );

        var results = new List<object>();

        foreach (var file in request.Files)
        {
            if (file == null || file.Length == 0)
                continue;

            var jobId = Guid.NewGuid();
            var filePath = Path.Combine(
                uploadsPath,
                $"{jobId}_{file.FileName}");

            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var job = new ImportJob
            {
                JobId = jobId,
                FileName = file.FileName,
                FilePath = filePath,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ImportJobs.Add(job);
            await _dbContext.SaveChangesAsync();

            // publish job to RabbitMQ
            await publisher.PublishJobAsync(jobId.ToString());

            results.Add(new
            {
                JobId = jobId,
                FileName = file.FileName,
                Status = job.Status
            });
        }

        return Ok(results);
    }

    /// <summary>
    /// Check progress of an ImportJob
    /// </summary>
    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetStatus(Guid jobId)
    {
        var job = await _dbContext.ImportJobs
            .FirstOrDefaultAsync(j => j.JobId == jobId);

        if (job == null)
            return NotFound("Job not found.");

        return Ok(new
        {
            jobId = job.JobId,
            status = job.Status,
            totalRows = job.TotalRows,
            processedRows = job.ProcessedRows,
            failedRows = job.FailedRows,
            lastProcessedRow = job.LastProcessedRow,
            startedAt = job.StartedAt,
            completedAt = job.CompletedAt
        });
    }
}
