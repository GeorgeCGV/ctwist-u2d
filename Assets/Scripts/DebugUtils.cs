using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Proxies different UnityEngine debug calls.
/// Similarly to the logger, allows to exclude
/// funcitonality from the build.
///
/// Functions won't be available if ENABLE_LOGS
/// is not defined.
/// </summary>
public static class DebugUtils
{
    [Conditional("ENABLE_LOGS")]
    public static void DrawLine(Vector2 start, Vector2 end, Color color, float duration)
    {
        UnityEngine.Debug.DrawLine(start, end, color, duration);
    }

    [Conditional("ENABLE_LOGS")]
    public static void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        UnityEngine.Debug.DrawLine(start, end, color, 0);
    }
}