namespace Data
{
    /// <summary>
    /// Structure to pass result information around upon level completion.
    /// </summary>
    public struct LevelResults
    {
        /// <summary>
        /// Player won the level.
        /// </summary>
        /// <value>True if won, otherwise False.</value>
        public bool Won { get; private set; }
        /// <summary>
        /// Achieved score points.
        /// </summary>
        /// <value>Integer >= 0.</value>
        public int Score { get; private set; }
        /// <summary>
        /// Stores next or the same level ID.
        /// It will be the same when:
        /// - Failed to unlock next level.
        /// - It is the last level.
        /// </summary>
        /// <value>Level ID.</value>
        public int NextLevel { get; private set; }
        /// <summary>
        /// If played level was the last level in the game.
        /// </summary>
        /// <value>True if yes, otherwise False.</value>
        public bool WasLastLevel { get; private set; }
        /// <summary>
        /// The amount of achieved stars.
        /// </summary>
        /// <value>Stars amount >= 0.</value>
        public int EarnedStars { get; private set; }
        /// <summary>
        /// Flag that tells if Score is new highscore.
        /// </summary>
        /// <value></value>
        public bool IsHighscore { get; private set; }

        public LevelResults(int score, bool won, int nextLevelId, bool wasLastLevel, int earnedStars, bool isHighscore)
        {
            Score = score;
            Won = won;
            NextLevel = nextLevelId;
            WasLastLevel = wasLastLevel;
            EarnedStars = earnedStars;
            IsHighscore = isHighscore;
        }
    }
}