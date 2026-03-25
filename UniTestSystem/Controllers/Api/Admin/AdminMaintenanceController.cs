using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Controllers.Api.Admin;

[ApiController]
[Authorize(Policy = PermissionCodes.System_Backup)]
[Route("api/admin/maintenance")]
public class AdminMaintenanceController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditService _auditService;

    public AdminMaintenanceController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IAuditService auditService)
    {
        _configuration = configuration;
        _environment = environment;
        _auditService = auditService;
    }

    [HttpGet("backups")]
    public IActionResult GetBackups()
    {
        var backupDirectory = GetBackupDirectory();
        Directory.CreateDirectory(backupDirectory);

        var files = Directory.GetFiles(backupDirectory, "*.bak", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => new
            {
                f.Name,
                f.FullName,
                f.Length,
                f.LastWriteTimeUtc
            })
            .ToList();

        return Ok(files);
    }

    [HttpPost("backup")]
    public async Task<IActionResult> Backup([FromBody] BackupRequest? request = null)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return BadRequest(new { message = "DefaultConnection is not configured." });
        }

        var dbName = GetDatabaseName(connectionString);
        if (string.IsNullOrWhiteSpace(dbName))
        {
            return BadRequest(new { message = "Unable to resolve database name from connection string." });
        }

        var backupDirectory = GetBackupDirectory();
        Directory.CreateDirectory(backupDirectory);

        var safeDatabaseName = dbName.Replace("]", "]]", StringComparison.Ordinal);
        var fileName = string.IsNullOrWhiteSpace(request?.FileName)
            ? $"{safeDatabaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak"
            : EnsureBakExtension(Path.GetFileName(request.FileName));
        var backupPath = Path.Combine(backupDirectory, fileName);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = $"BACKUP DATABASE [{safeDatabaseName}] TO DISK = @path WITH INIT, COPY_ONLY";
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@path", backupPath);
            await command.ExecuteNonQueryAsync();
        }

        await _auditService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system",
            "System.Backup",
            "Database",
            dbName,
            null,
            new { backupPath, at = DateTime.UtcNow });

        return Ok(new { message = "Backup completed.", fileName, backupPath });
    }

    [HttpPost("restore")]
    [RequestSizeLimit(1024L * 1024L * 1024L)]
    public async Task<IActionResult> Restore([FromForm] RestoreRequest request)
    {
        if (request.File == null || request.File.Length <= 0)
        {
            return BadRequest(new { message = "Backup file is required." });
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return BadRequest(new { message = "DefaultConnection is not configured." });
        }

        var dbName = GetDatabaseName(connectionString);
        if (string.IsNullOrWhiteSpace(dbName))
        {
            return BadRequest(new { message = "Unable to resolve database name from connection string." });
        }

        var backupDirectory = GetBackupDirectory();
        Directory.CreateDirectory(backupDirectory);

        var safeFileName = EnsureBakExtension(Path.GetFileName(request.File.FileName));
        var uploadedPath = Path.Combine(backupDirectory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{safeFileName}");
        await using (var stream = System.IO.File.Create(uploadedPath))
        {
            await request.File.CopyToAsync(stream);
        }

        var masterConnectionString = ToMasterConnectionString(connectionString);
        var safeDatabaseName = dbName.Replace("]", "]]", StringComparison.Ordinal);

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        var sql = $@"
ALTER DATABASE [{safeDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{safeDatabaseName}] FROM DISK = @path WITH REPLACE;
ALTER DATABASE [{safeDatabaseName}] SET MULTI_USER;";

        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@path", uploadedPath);
            command.CommandTimeout = 0;
            await command.ExecuteNonQueryAsync();
        }

        await _auditService.LogAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system",
            "System.Restore",
            "Database",
            dbName,
            null,
            new { backupFile = safeFileName, restoredAt = DateTime.UtcNow });

        return Ok(new { message = "Restore completed.", file = safeFileName });
    }

    private string GetBackupDirectory()
    {
        return Path.Combine(_environment.ContentRootPath, "App_Data", "Backups");
    }

    private static string EnsureBakExtension(string fileName)
    {
        if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }

        return $"{fileName}.bak";
    }

    private static string? GetDatabaseName(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            return builder.InitialCatalog;
        }

        if (builder.TryGetValue("Database", out var dbObj) && dbObj is string dbName && !string.IsNullOrWhiteSpace(dbName))
        {
            return dbName;
        }

        return null;
    }

    private static string ToMasterConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };
        return builder.ConnectionString;
    }
}

public class BackupRequest
{
    public string? FileName { get; set; }
}

public class RestoreRequest
{
    public IFormFile? File { get; set; }
}
