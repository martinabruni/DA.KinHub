namespace Kin.KinHub.KinList.Business.Common;

public static class KinListTransactionExecutorGuard
{
    public static void EnsureConfigured(IKinListTransactionExecutor executor, bool isDevelopment)
    {
        if (!isDevelopment && executor is NoOpKinListTransactionExecutor)
        {
            throw new InvalidOperationException("IKinListTransactionExecutor resolved to NoOpKinListTransactionExecutor outside development.");
        }
    }
}
