namespace LastLight.Gameplay
{
    public enum HorizontalObstacleType
    {
        Bottom = 0,
        Top = 1,
    }

    public readonly struct HorizontalPatternStep
    {
        public HorizontalPatternStep(HorizontalObstacleType type, float offsetX)
        {
            Type = type;
            OffsetX = offsetX;
        }

        public HorizontalObstacleType Type { get; }
        public float OffsetX { get; }
    }

    public sealed class HorizontalPattern
    {
        public HorizontalPattern(params HorizontalPatternStep[] steps)
        {
            Steps = steps;
        }

        public HorizontalPatternStep[] Steps { get; }
    }

    public static class HorizontalGameplayConfig
    {
        public const float Gravity = 1.87f;
        public const float JumpForce = -22f;
        public const float BaseSpeed = 6.5f;
        public const float SpeedIncrement = 0.003f;
        public const float SpawnRateInitial = 60f;
        public const float PlayerSize = 38f;
        public const float ObstacleWidth = 22f;
        public const float Zoom = 0.58f;
        public const float ShieldInterval = 300f;
        public const float ShieldDuration = 5f;
        public const float ShieldWarningWindow = 1.35f;
        public const float ShieldDashDuration = 0.28f;
        public const float ShieldDashWorldBoost = 1.75f;
        public const float ShieldDashScoreBoost = 0.55f;
        public const float HardModeStartScore = 500f;

        public static readonly float[] BottomHeights = { 45f, 52f, 60f, 68f };
        public static readonly float[] TopHeights = { 132f, 142f, 151f, 162f };

        private const float JumpDistance = 200f;
        private const float StackedGap = ObstacleWidth - 2f;
        private const float HardJumpDistance = 176f;

        public static readonly HorizontalPattern[] EasyPatterns =
        {
            new(new HorizontalPatternStep(HorizontalObstacleType.Bottom, 0f)),
            new(new HorizontalPatternStep(HorizontalObstacleType.Top, 0f)),
            new(new HorizontalPatternStep(HorizontalObstacleType.Bottom, 0f)),
            new(new HorizontalPatternStep(HorizontalObstacleType.Top, 0f)),
            new(
                new HorizontalPatternStep(HorizontalObstacleType.Bottom, 0f),
                new HorizontalPatternStep(HorizontalObstacleType.Bottom, StackedGap)),
            new(
                new HorizontalPatternStep(HorizontalObstacleType.Bottom, 0f),
                new HorizontalPatternStep(HorizontalObstacleType.Top, JumpDistance)),
            new(
                new HorizontalPatternStep(HorizontalObstacleType.Top, 0f),
                new HorizontalPatternStep(HorizontalObstacleType.Bottom, JumpDistance)),
        };

        public static readonly HorizontalPattern[] HardPatterns =
        {
            new(new HorizontalPatternStep(HorizontalObstacleType.Bottom, 0f)),
            new(new HorizontalPatternStep(HorizontalObstacleType.Top, 0f)),
            new(new HorizontalPatternStep(HorizontalObstacleType.Bottom, 0f)),
            new(new HorizontalPatternStep(HorizontalObstacleType.Top, 0f)),
            new(
                new HorizontalPatternStep(HorizontalObstacleType.Bottom, 0f),
                new HorizontalPatternStep(HorizontalObstacleType.Top, HardJumpDistance)),
            new(
                new HorizontalPatternStep(HorizontalObstacleType.Top, 0f),
                new HorizontalPatternStep(HorizontalObstacleType.Bottom, HardJumpDistance)),
        };

        public static float GetPatternSpan(HorizontalPattern pattern)
        {
            var max = ObstacleWidth;
            foreach (var step in pattern.Steps)
            {
                var span = step.OffsetX + ObstacleWidth;
                if (span > max)
                {
                    max = span;
                }
            }

            return max;
        }
    }
}

