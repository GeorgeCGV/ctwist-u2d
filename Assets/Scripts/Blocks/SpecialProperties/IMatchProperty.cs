using System.Collections.Generic;

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
        /// Executes special match rule.
        /// </summary>
        /// <remarks>
        /// Blocks to destroy must be appended to matches;
        /// the other way for saving.
        /// </remarks>
        /// <param name="parent">Block that has the property.</param>
        /// <param name="matches">Current matches.</param>
        void ExecuteSpecial(BasicBlock parent, HashSet<BasicBlock> matches);

        /// <summary>
        /// Called when a match is made on a block with this property.
        /// </summary>
        /// <param name="removeProperty">Shall the property be removed from the block?</param>
        /// <returns>Matching behaviour modification.</returns>
        EMatchPropertyOutcome Execute(out bool removeProperty);
    }
}