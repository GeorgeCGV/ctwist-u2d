using System.Diagnostics;

/// <summary>
/// Custom logger.
/// Allows to easily avoid inlusion of log output to builds.
/// See https://docs.unity3d.com/6000.2/Documentation/Manual/UnderstandingPerformanceGeneralOptimizations.html.
/// </summary>
public static class Logger {

    [Conditional("ENABLE_LOGS")]
    public static void Debug(string logMsg) {
        UnityEngine.Debug.Log(logMsg);
    }
}