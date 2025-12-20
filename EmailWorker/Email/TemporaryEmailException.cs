namespace EmailWorker.Email;

public sealed class TemporaryEmailException(string message) : Exception(message);
