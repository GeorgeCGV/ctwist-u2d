using UnityEngine;

namespace Configs
{
    /// <summary>
    /// Score configuration that provides values to use
    /// when player is awarded.
    /// </summary>
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "Config/ScoreConfig")]
    public class ScoreConfig : ScriptableObject
    {
        [SerializeField]
        public int scorePerMatch3 = 250;
        [SerializeField]
        public int scorePerMatch4 = 1000;
        [SerializeField]
        public int scorePerMatchMore = 2000;
        [SerializeField]
        public int scorePerFloating = 25;
        [SerializeField]
        public int scoreBaseForTimeLimit = 1000;
        [SerializeField]
        public int scoreBaseTimeFactorForSpawnsLimit = 10;
        [SerializeField]
        public float scoreTimeLimitBonusFactor = 3.0f;
        [SerializeField]
        public int scoreBaseForSpawnsLimit = 10000;

        /// <summary>
        /// Computes score depending on the amount of matched blocks.
        /// </summary>
        /// <param name="amount">Amount of matched blocks.</param>
        /// <returns>Score.</returns>
        public int ComputeScoreForMatchAmount(int amount)
        {
            switch (amount)
            {
                case 3:
                    return scorePerMatch3;
                case 4:
                    return scorePerMatch4;
                case > 4:
                    return scorePerMatchMore + (amount - 4) * (amount - 3) * scorePerMatch3;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Computes bonus points for time left.
        /// </summary>
        /// <remarks>
        /// Exponential.
        /// Guarantees at least ScoreBaseForTimeLimit base bonus score.
        /// The ScoreTimeLimitBonusFactor is applied to squared time left.
        /// </remarks>
        /// <param name="elapsedTime">Elapsed level time in seconds.</param>
        /// <param name="timeLimit">Level time limit.</param>
        /// <returns>Bonus scores.</returns>
        public int ComputeBonusForTimeLimit(float elapsedTime, float timeLimit)
        {
            if (timeLimit <= 0)
            {
                return 0;
            }
            
            float timeLeftPercent = (timeLimit - elapsedTime) / timeLimit;
            float multiplier = 1 + timeLeftPercent * timeLeftPercent * scoreTimeLimitBonusFactor;
            
            return (int)(scoreBaseForTimeLimit * multiplier);
        }

        /// <summary>
        /// Computes bonus points for spawn limit modules.
        /// </summary>
        /// <remarks>
        /// Computed from ScoreBaseForSpawnsLimit multiplied by a combined factor.
        /// Where factor is a combination of moves left in percent multiplied by
        /// decreasing time bonus.
        /// </remarks>
        /// <param name="elapsedTime">Level's elapsed time.</param>
        /// <param name="totalSpawned">Amount of spawned blocks during gameplay.</param>
        /// <param name="spawnsLimit">Spawns limit.</param>
        /// <returns>Bonus scores.</returns>
        public int ComputeBonusForSpawnLimit(float elapsedTime, int totalSpawned, int spawnsLimit)
        {
            if (spawnsLimit <= 0)
            {
                return 0;
            }
            
            float moveLeftPercent = (float)(spawnsLimit - totalSpawned) / spawnsLimit;
            // decreases as elapsedTime increases
            float timeBonus = 1 / (1 + elapsedTime * scoreBaseTimeFactorForSpawnsLimit);
            float factor = moveLeftPercent * timeBonus;
            
            return (int)(scoreBaseForSpawnsLimit * factor);
        }
    }
}