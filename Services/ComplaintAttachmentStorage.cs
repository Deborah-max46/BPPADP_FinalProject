using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace ConsumersVoiceSystemPrototype.Services;

public class ComplaintAttachmentStorage
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".doc", ".docx", ".xlsx", ".txt"
    };

    private readonly IAmazonS3? _s3;
    private readonly string? _bucket;
    private readonly string? _publicUrl;
    private readonly IWebHostEnvironment _env;

    public long MaxFileSizeBytes => MaxBytes;

    public ComplaintAttachmentStorage(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        var accountId = config["R2:AccountId"];
        var accessKey = config["R2:AccessKey"];
        var secretKey = config["R2:SecretKey"];
        _bucket = config["R2:Bucket"];
        _publicUrl = config["R2:PublicUrl"]?.TrimEnd('/');

        if (!string.IsNullOrEmpty(accountId) && !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
        {
            _s3 = new AmazonS3Client(accessKey, secretKey, new Amazon.S3.AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            });
        }
    }

    public bool IsAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    public async Task<(string StoredFileName, string RelativeWebPath, string ContentType, long Size)?> SaveAsync(
        int complaintId,
        IFormFile file,
        CancellationToken ct = default)
    {
        if (file.Length <= 0 || file.Length > MaxBytes) return null;
        if (!IsAllowedExtension(file.FileName)) return null;

        var ext = Path.GetExtension(file.FileName);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

        if (_s3 != null && !string.IsNullOrEmpty(_bucket))
        {
            var key = $"complaints/{complaintId}/{stored}";
            using var stream = file.OpenReadStream();
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            }, ct);

            var url = !string.IsNullOrEmpty(_publicUrl)
                ? $"{_publicUrl}/{key}"
                : $"r2://{_bucket}/{key}";

            return (stored, url, contentType, file.Length);
        }

        // Fallback: local disk (dev only)
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", "complaints", complaintId.ToString());
        Directory.CreateDirectory(dir);
        var physical = Path.Combine(dir, stored);
        await using (var fs = File.Create(physical))
            await file.CopyToAsync(fs, ct);

        return (stored, $"/uploads/complaints/{complaintId}/{stored}", contentType, file.Length);
    }

    public string GetPhysicalPath(int complaintId, string storedFileName)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "complaints", complaintId.ToString(), storedFileName);
    }

    public bool FileExists(int complaintId, string storedFileName) =>
        File.Exists(GetPhysicalPath(complaintId, storedFileName));

    public bool IsR2Url(string path) =>
        path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("r2://", StringComparison.OrdinalIgnoreCase);
}
