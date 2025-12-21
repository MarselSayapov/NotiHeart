namespace SmsWorker.Sms;

public sealed class TemporarySmsException(string message) : Exception(message);
