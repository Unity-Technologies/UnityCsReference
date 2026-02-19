namespace Unity.Scripting.LifecycleManagement
{
    internal static class DebugLifecycle
    {
        static private readonly bool loggingEnabled = false;
        static private readonly bool verificationEnabled = false;

        static DebugLifecycle()
        {
            loggingEnabled = IsLoggingEnabled();
            verificationEnabled = IsVerificationEnabled();
        }

        public static void Log(string message)
        {
            if (loggingEnabled)
                Debug.Log(message);
        }

        public static void ReportError(string message, bool criticalError = true)
        {
            if (criticalError || loggingEnabled)
            {
                Debug.LogError(message);
            }
        }

        internal static bool IsLoggingEnabled()
        {
            return Debug.IsDiagnosticSwitchEnabled("LifecycleManagementLogging");
        }

        internal static bool IsVerificationEnabled()
        {
            return Debug.IsDiagnosticSwitchEnabled("LifecycleManagementVerification");
        }

    }
}
