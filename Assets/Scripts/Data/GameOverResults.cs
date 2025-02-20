namespace Data
{
    public class GameOverResults
    {
        public bool Won { get; private set; }
        public int Score { get; private set; }

        public GameOverResults(int score, bool won)
        {
            Score = score;
            Won = won;
        }
    }
}