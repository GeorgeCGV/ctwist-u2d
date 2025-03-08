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
        /// Destroy all blocks of the same block type.
        /// </summary>
        DestroyAllOfSameType,

        /// <summary>
        /// Stops matching process.
        /// </summary>
        StopMatching,
    }
}