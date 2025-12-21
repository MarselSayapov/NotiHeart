namespace SmsWorker.Sms;

public sealed class PermanentSmsException(string message) : Exception(message);
