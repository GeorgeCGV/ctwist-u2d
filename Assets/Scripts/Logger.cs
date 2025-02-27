using System.Diagnostics;

/// <summary>
/// Custom logger.
/// Allows to easily avoid inlusion of log output to builds.
/// See https://docs.unity3d.com/6000.2/Documentation/Manual/UnderstandingPerformanceGeneralOptimizations.html.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Proxy for UnityEngine.Debug.Log(string).
    ///
    /// Excluded from the build if ENABLE_LOGS
    /// is not defined.
    /// </summary>
    /// <param name="msg">Message.</param>
    [Conditional("ENABLE_LOGS")]
    public static void Debug(string msg)
    {
        UnityEngine.Debug.Log(msg);
    }
}
