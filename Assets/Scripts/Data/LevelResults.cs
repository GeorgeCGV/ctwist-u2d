namespace Data
{
    public struct LevelResults
    {
        public bool Won { get; private set; }
        public int Score { get; private set; }
        public int NextLevel { get; private set; }
        public bool WasLastLevel { get; private set; }
        public int EarnedStars { get; private set; }

        public LevelResults(int score, bool won, int nextLevelId, bool wasLastLevel, int earnedStars)
        {
            Score = score;
            Won = won;
            NextLevel = nextLevelId;
            WasLastLevel = wasLastLevel;
            EarnedStars = earnedStars;
        }
    }
}