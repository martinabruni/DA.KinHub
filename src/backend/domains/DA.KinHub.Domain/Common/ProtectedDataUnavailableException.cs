namespace DA.KinHub.Domain.Common;

public sealed class ProtectedDataUnavailableException(string message, Exception? innerException = null) : Exception(message, innerException);
