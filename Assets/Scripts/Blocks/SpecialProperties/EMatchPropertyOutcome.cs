namespace Blocks.SpecialProperties
{
    /// <summary>
    /// Result of processing <see cref="IMatchProperty"/> that applies
    /// to the overall match behaviour.
    /// </summary>
    public enum EMatchPropertyOutcome
    {
        /// <summary>
        /// Continue with normal matching process.
        /// </summary>
        ContinueNormalMatching,

        /// <summary>
        /// If returned, then <see cref="IMatchProperty.ExecuteSpecial"/> is invoked.
        /// </summary>
        SpecialMatchRule,

        /// <summary>
        /// Stops matching process, regardless of other properties.
        /// </summary>
        /// <remarks>
        /// Doesn't stop matched block's properties processing.
        /// </remarks>
        StopMatching,
    }
}