namespace EmailWorker.Email;

public sealed class EmailAttachment
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public long Size { get; init; }
}
