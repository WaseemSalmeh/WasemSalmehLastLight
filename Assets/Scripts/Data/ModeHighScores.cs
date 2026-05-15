using System;
using LastLight.Core;

namespace LastLight.Data
{
    [Serializable]
    public sealed class ModeHighScores
    {
        public int horizontal;
        public int vertical;

        public int Get(GameMode mode)
        {
            return mode == GameMode.Horizontal ? horizontal : vertical;
        }

        public bool TrySet(GameMode mode, int score)
        {
            if (score <= Get(mode))
            {
                return false;
            }

            if (mode == GameMode.Horizontal)
            {
                horizontal = score;
            }
            else
            {
                vertical = score;
            }

            return true;
        }
    }
}

