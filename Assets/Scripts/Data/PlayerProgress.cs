using System;

namespace LastLight.Data
{
    [Serializable]
    public sealed class PlayerProgress
    {
        public GameSettings settings = new();
        public ModeHighScores highScores = new();
        public int totalRuns;
    }
}

