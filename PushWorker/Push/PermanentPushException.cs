namespace PushWorker.Push;

public sealed class PermanentPushException(string message) : Exception(message);
