namespace DA.KinHub.Domain.Common;

public sealed class RepositoryUnavailableException(string message, Exception? innerException = null) : Exception(message, innerException);
