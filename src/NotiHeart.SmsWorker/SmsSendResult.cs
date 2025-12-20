namespace NotiHeart.SmsWorker;

public sealed record SmsSendResult(bool Success, bool IsPermanentFailure, string? Error);
