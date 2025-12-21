namespace PushWorker.Push;

public sealed class TemporaryPushException(string message) : Exception(message);
