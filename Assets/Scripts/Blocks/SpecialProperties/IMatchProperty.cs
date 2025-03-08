namespace Blocks.SpecialProperties
{
    /// <summary>
    /// Special property interface. 
    /// </summary>
    /// <remarks>
    /// <see cref="BasicBlock"/> may have special properties that
    /// are processed during blocks match.
    /// </remarks>
    public interface IMatchProperty
    {
        /// <summary>
        /// Activates the property.
        /// </summary>
        /// <param name="parent">Block to apply the property to.</param>
        void Activate(BasicBlock parent);

        /// <summary>
        /// Called when a match is made on a block with the property.
        /// </summary>
        /// <param name="removeProperty">Shall the property be removed?</param>
        /// <returns>Matching behaviour modification.</returns>
        EMatchPropertyOutcome ProcessMatch(out bool removeProperty);
    }
}