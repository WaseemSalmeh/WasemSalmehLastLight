using UnityEngine;

namespace LastLight.Gameplay
{
    public enum VerticalObstacleSide
    {
        Left = 0,
        Right = 1,
    }

    public enum VerticalPatternKind
    {
        Single = 0,
        Stacked = 1,
        ZigZag = 2,
    }

    public readonly struct VerticalPatternStep
    {
        public VerticalPatternStep(VerticalObstacleSide side, float offsetY)
        {
            Side = side;
            OffsetY = offsetY;
        }

        public VerticalObstacleSide Side { get; }
        public float OffsetY { get; }
    }

    public sealed class VerticalPattern
    {
        public VerticalPattern(VerticalPatternKind kind, params VerticalPatternStep[] steps)
        {
            Kind = kind;
            Steps = steps;
        }

        public VerticalPatternKind Kind { get; }
        public VerticalPatternStep[] Steps { get; }
    }

    public static class VerticalGameplayConfig
    {
        public const float BaseSpeed = 4.65f;
        public const float SpeedIncrementEasy = 0.00135f;
        public const float SpeedIncrementHard = 0.0027f;
        public const float SpawnRateInitial = 58f;
        public const float PlayerSize = 38f;
        public const float Zoom = 0.58f;
        public const float MoveSpeed = 5f;
        public const float WallPadding = 10f;
        public const float ShieldInterval = 300f;
        public const float ShieldDuration = 5f;
        public const float ShieldWarningWindow = 1.35f;
        public const float ShieldSwitchBoost = 1.65f;
        public const float ShieldDashDuration = 0.28f;
        public const float ShieldDashWorldBoost = 1.85f;
        public const float ShieldDashScoreBoost = 0.55f;
        public const float HardModeStartScore = 400f;

        private const float EasyZigZagGap = 180f;
        private const float HardZigZagGap = 158f;
        private const float StackedGap = 34f;

        public static readonly VerticalPattern[] EasyPatterns =
        {
            new(VerticalPatternKind.Single, new VerticalPatternStep(VerticalObstacleSide.Left, 0f)),
            new(VerticalPatternKind.Single, new VerticalPatternStep(VerticalObstacleSide.Right, 0f)),
            new(VerticalPatternKind.Single, new VerticalPatternStep(VerticalObstacleSide.Left, 0f)),
            new(VerticalPatternKind.Single, new VerticalPatternStep(VerticalObstacleSide.Right, 0f)),
            new(
                VerticalPatternKind.ZigZag,
                new VerticalPatternStep(VerticalObstacleSide.Left, 0f),
                new VerticalPatternStep(VerticalObstacleSide.Right, EasyZigZagGap)),
            new(
                VerticalPatternKind.ZigZag,
                new VerticalPatternStep(VerticalObstacleSide.Right, 0f),
                new VerticalPatternStep(VerticalObstacleSide.Left, EasyZigZagGap)),
        };

        public static readonly VerticalPattern[] HardPatterns =
        {
            new(VerticalPatternKind.Single, new VerticalPatternStep(VerticalObstacleSide.Left, 0f)),
            new(VerticalPatternKind.Single, new VerticalPatternStep(VerticalObstacleSide.Right, 0f)),
            new(
                VerticalPatternKind.Stacked,
                new VerticalPatternStep(VerticalObstacleSide.Left, 0f),
                new VerticalPatternStep(VerticalObstacleSide.Left, StackedGap)),
            new(
                VerticalPatternKind.Stacked,
                new VerticalPatternStep(VerticalObstacleSide.Right, 0f),
                new VerticalPatternStep(VerticalObstacleSide.Right, StackedGap)),
            new(
                VerticalPatternKind.ZigZag,
                new VerticalPatternStep(VerticalObstacleSide.Left, 0f),
                new VerticalPatternStep(VerticalObstacleSide.Right, HardZigZagGap)),
            new(
                VerticalPatternKind.ZigZag,
                new VerticalPatternStep(VerticalObstacleSide.Right, 0f),
                new VerticalPatternStep(VerticalObstacleSide.Left, HardZigZagGap)),
        };

        public static float GetChannelWidth(float width)
        {
            return Mathf.Min(260f, Mathf.Max(200f, width * 0.68f));
        }

        public static float GetObstacleSpawnY(float height, float offsetY)
        {
            return height / 2f - height / (2f * Zoom) - 160f - offsetY;
        }

        public static float GetWallLeanX(
            VerticalObstacleSide side,
            float leftWallX,
            float rightWallX,
            float size)
        {
            return side == VerticalObstacleSide.Left
                ? leftWallX + size / 2f + WallPadding
                : rightWallX - size / 2f - WallPadding;
        }

        public static float PickObstacleWidth(VerticalPatternKind kind, bool hardMode)
        {
            if (kind == VerticalPatternKind.ZigZag)
            {
                var widths = hardMode ? new[] { 46f, 54f, 62f } : new[] { 42f, 50f, 58f };
                return widths[Random.Range(0, widths.Length)];
            }

            if (kind == VerticalPatternKind.Stacked)
            {
                var widths = new[] { 60f, 72f, 84f };
                return widths[Random.Range(0, widths.Length)];
            }

            var singleWidths = hardMode ? new[] { 56f, 68f, 80f, 92f } : new[] { 48f, 58f, 68f, 78f };
            return singleWidths[Random.Range(0, singleWidths.Length)];
        }
    }
}
