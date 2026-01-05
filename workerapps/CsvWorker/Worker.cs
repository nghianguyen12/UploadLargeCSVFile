using CsvHelper;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Globalization;
using System.Text;

namespace CsvWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _connectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres123;Database=mydb";

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        // Create async connection
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Declare queue
        await channel.QueueDeclareAsync(
            queue: "jobs",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var jobId = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("Received JobId: {JobId}", jobId);

            try
            {
                // TODO: Fetch CSV file path from database using jobId
                // TODO: Process CSV file rows in batches
                // For demo, we just simulate work:
                await Task.Delay(1000, stoppingToken);

                string filePath = await GetCsvFilePathAsync(jobId);

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    _logger.LogWarning("File not found for JobId: {JobId}", jobId);
                    await UpdateJobStatusAsync(jobId, "failed");
                    return;
                }

                // 2️⃣ Process CSV in batches
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                var records = csv.GetRecords<dynamic>().ToList();
                int batchSize = 500;
                int totalRows = records.Count;
                int processedRows = 0;

                await UpdateJobTotalRowsAsync(jobId, totalRows);
                await UpdateJobStatusAsync(jobId, "in_progress");

                while (processedRows < totalRows)
                {
                    var batch = records.Skip(processedRows).Take(batchSize);

                    await InsertBatchAsync(batch);
                    processedRows += batch.Count();

                    await UpdateJobProgressAsync(jobId, processedRows);
                    _logger.LogInformation("JobId {JobId} processed {Processed}/{Total}", jobId, processedRows, totalRows);
                }

                await UpdateJobStatusAsync(jobId, "completed");
                _logger.LogInformation("JobId {JobId} completed", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JobId: {JobId}", jobId);
            }
        };

        await channel.BasicConsumeAsync(
            queue: "jobs",
            autoAck: true, // you can switch to false if you want manual ack
            consumer: consumer
        );

        _logger.LogInformation("Worker started. Listening to 'jobs' queue...");

        // Keep the worker running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
    // ==========================
    // Database helper methods
    // ==========================
    private async Task<string> GetCsvFilePathAsync(string jobId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT file_path FROM import_job WHERE job_id = @jobId", conn);
        cmd.Parameters.AddWithValue("jobId", Guid.Parse(jobId));

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    private async Task UpdateJobStatusAsync(string jobId, string status)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "UPDATE import_job SET status = @status, completed_at = CASE WHEN @status = 'completed' THEN now() ELSE completed_at END WHERE job_id = @jobId", 
            conn
        );
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("jobId", Guid.Parse(jobId));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpdateJobTotalRowsAsync(string jobId, int totalRows)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "UPDATE import_job SET total_rows = @totalRows WHERE job_id = @jobId",
            conn
        );
        cmd.Parameters.AddWithValue("totalRows", totalRows);
        cmd.Parameters.AddWithValue("jobId", Guid.Parse(jobId));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpdateJobProgressAsync(string jobId, int processedRows)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "UPDATE import_job SET processed_rows = @processedRows, last_processed_row = @processedRows WHERE job_id = @jobId",
            conn
        );
        cmd.Parameters.AddWithValue("processedRows", processedRows);
        cmd.Parameters.AddWithValue("jobId", Guid.Parse(jobId));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertBatchAsync(IEnumerable<dynamic> batch)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // For demo: assume table "customer" with columns (id, name, email)
        await using var writer = conn.BeginBinaryImport("COPY customer (customer_id, name, email) FROM STDIN (FORMAT BINARY)");

        foreach (var record in batch)
        {
            writer.StartRow();
            writer.Write(Guid.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
            writer.Write(record.name?.ToString(), NpgsqlTypes.NpgsqlDbType.Text);
            writer.Write(record.email?.ToString(), NpgsqlTypes.NpgsqlDbType.Text);
        }

        await writer.CompleteAsync();
    }
}
