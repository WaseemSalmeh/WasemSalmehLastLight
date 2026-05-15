using System;

namespace LastLight.Data
{
    [Serializable]
    public sealed class GameSettings
    {
        public float masterVolume = 0.8f;
        public float glowIntensity = 0.8f;
        public bool deathFlashesEnabled = true;
    }
}

