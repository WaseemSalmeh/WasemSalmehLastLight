using System;
using System.Collections.Generic;
using LastLight.Core;
using UnityEngine;

namespace LastLight.Gameplay
{
    public sealed class LastLightGameSession
    {
        public readonly HorizontalState Horizontal = new();
        public readonly VerticalState Vertical = new();

        public GameMode Mode { get; private set; } = GameMode.Horizontal;
        public bool IsActive { get; private set; }
        public bool IsPaused { get; set; }
        public int DisplayScore => Mathf.FloorToInt(Mode == GameMode.Horizontal ? Horizontal.Score : Vertical.Score);

        public event Action<int> GameOver;
        public event Action ScoreChanged;

        private readonly List<HorizontalPattern> horizontalPool = new();
        private readonly List<VerticalPattern> verticalPool = new();
        private int horizontalPoolIndex;
        private int verticalPoolIndex;
        private bool horizontalHardMode;
        private bool verticalHardMode;
        private int lastDisplayScore;
        private float width;
        private float height;

        public void Start(GameMode mode, float viewportWidth, float viewportHeight)
        {
            Mode = mode;
            IsActive = true;
            IsPaused = false;
            width = Mathf.Max(1f, viewportWidth);
            height = Mathf.Max(1f, viewportHeight);
            lastDisplayScore = 0;

            if (Mode == GameMode.Horizontal)
            {
                ResetHorizontal();
            }
            else
            {
                ResetVertical();
            }

            ScoreChanged?.Invoke();
        }

        public void Stop()
        {
            IsActive = false;
            IsPaused = false;
        }

        public void Resize(float viewportWidth, float viewportHeight)
        {
            var nextWidth = Mathf.Max(1f, viewportWidth);
            var nextHeight = Mathf.Max(1f, viewportHeight);

            if (Mathf.Approximately(nextWidth, width) && Mathf.Approximately(nextHeight, height))
            {
                return;
            }

            if (Mode == GameMode.Horizontal)
            {
                var deltaX = Horizontal.Player.X - Horizontal.Player.BaseX;
                Horizontal.GroundY = nextHeight * 0.5f + 130f;
                Horizontal.RoofY = nextHeight * 0.5f - 130f;
                Horizontal.Player.BaseX = nextWidth * 0.22f;
                Horizontal.Player.X = Horizontal.Player.BaseX + deltaX;

                if (Horizontal.Player.IsGrounded)
                {
                    Horizontal.Player.Y = Horizontal.GroundY - Horizontal.Player.Size * 0.5f;
                }
            }
            else
            {
                var channelWidth = VerticalGameplayConfig.GetChannelWidth(nextWidth);
                Vertical.LeftWallX = nextWidth * 0.5f - channelWidth * 0.5f;
                Vertical.RightWallX = nextWidth * 0.5f + channelWidth * 0.5f;
                Vertical.Player.TargetX = VerticalGameplayConfig.GetWallLeanX(
                    Vertical.Player.Side,
                    Vertical.LeftWallX,
                    Vertical.RightWallX,
                    Vertical.Player.Size);
                Vertical.Player.X = Vertical.Player.TargetX;
            }

            width = nextWidth;
            height = nextHeight;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive || IsPaused)
            {
                return;
            }

            var dt = Mathf.Min(deltaTime, 0.05f);
            var frameFactor = Mathf.Clamp(dt * 60f, 0.15f, 3f);

            if (Mode == GameMode.Horizontal)
            {
                UpdateHorizontal(dt, frameFactor);
            }
            else
            {
                UpdateVertical(dt, frameFactor);
            }

            var score = DisplayScore;
            if (score != lastDisplayScore)
            {
                lastDisplayScore = score;
                ScoreChanged?.Invoke();
            }
        }

        public void PressHorizontalJump()
        {
            if (!IsActive || IsPaused || Mode != GameMode.Horizontal || Horizontal.Dying)
            {
                return;
            }

            if (Horizontal.Player.IsGrounded)
            {
                DoHorizontalJump();
            }
            else
            {
                Horizontal.JumpQueued = true;
                Horizontal.JumpQueuedAt = Time.unscaledTime;
            }
        }

        public void ToggleVerticalSide()
        {
            MoveVerticalTo(Vertical.Player.Side == VerticalObstacleSide.Left
                ? VerticalObstacleSide.Right
                : VerticalObstacleSide.Left);
        }

        public void MoveVerticalTo(VerticalObstacleSide side)
        {
            if (!IsActive || IsPaused || Mode != GameMode.Vertical || Vertical.Dying)
            {
                return;
            }

            var targetX = VerticalGameplayConfig.GetWallLeanX(side, Vertical.LeftWallX, Vertical.RightWallX, Vertical.Player.Size);
            if (Vertical.Player.Side == side && Mathf.Abs(Vertical.Player.TargetX - targetX) < 0.5f)
            {
                return;
            }

            var shieldBoostActive = Vertical.Shield.TimeLeft > 0f;
            Vertical.Player.Side = side;
            Vertical.Player.TargetX = targetX;
            Vertical.Player.TargetRotation += side == VerticalObstacleSide.Left ? -Mathf.PI * 0.5f : Mathf.PI * 0.5f;
            Vertical.Player.ScaleX = shieldBoostActive ? 0.76f : 0.85f;
            Vertical.Player.ScaleY = shieldBoostActive ? 1.24f : 1.15f;
            Vertical.Player.IsSwitching = true;

            if (shieldBoostActive)
            {
                TriggerVerticalShieldDash(0.72f);
            }

            AudioManager.Instance?.Jump();
        }

        private void ResetHorizontal()
        {
            horizontalHardMode = false;
            ShuffleHorizontal(HorizontalGameplayConfig.EasyPatterns);

            var groundY = height * 0.5f + 130f;
            var roofY = height * 0.5f - 130f;
            var baseX = width * 0.22f;

            Horizontal.Player = new HorizontalPlayer
            {
                X = baseX,
                Y = groundY - HorizontalGameplayConfig.PlayerSize * 0.5f,
                Size = HorizontalGameplayConfig.PlayerSize,
                ScaleX = 1f,
                ScaleY = 1f,
                IsGrounded = true,
                BaseX = baseX,
            };
            Horizontal.Obstacles.Clear();
            Horizontal.Particles.Clear();
            Horizontal.Trail.Clear();
            Horizontal.SpeedLines.Clear();
            Horizontal.GroundY = groundY;
            Horizontal.RoofY = roofY;
            Horizontal.Speed = HorizontalGameplayConfig.BaseSpeed;
            Horizontal.FrameCount = 0;
            Horizontal.SpawnTimer = HorizontalGameplayConfig.SpawnRateInitial;
            Horizontal.SpawnRate = HorizontalGameplayConfig.SpawnRateInitial;
            Horizontal.Score = 0f;
            Horizontal.LastMilestone = 0;
            Horizontal.GridOffset = 0f;
            Horizontal.Dying = false;
            Horizontal.DeathFrames = 0f;
            Horizontal.DeathScore = 0;
            Horizontal.CamX = 0f;
            Horizontal.JumpQueued = false;
            Horizontal.JumpQueuedAt = 0f;
            Horizontal.Shield = ShieldState.Create(HorizontalGameplayConfig.ShieldInterval);
        }

        private void ResetVertical()
        {
            verticalHardMode = false;
            ShuffleVertical(VerticalGameplayConfig.EasyPatterns);

            var channelWidth = VerticalGameplayConfig.GetChannelWidth(width);
            var leftWallX = width * 0.5f - channelWidth * 0.5f;
            var rightWallX = width * 0.5f + channelWidth * 0.5f;
            var initialSide = VerticalObstacleSide.Right;
            var initialX = VerticalGameplayConfig.GetWallLeanX(initialSide, leftWallX, rightWallX, VerticalGameplayConfig.PlayerSize);

            Vertical.Player = new VerticalPlayer
            {
                X = initialX,
                Y = height * 0.7f,
                TargetX = initialX,
                Size = VerticalGameplayConfig.PlayerSize,
                ScaleX = 1f,
                ScaleY = 1f,
                Side = initialSide,
            };
            Vertical.Obstacles.Clear();
            Vertical.Particles.Clear();
            Vertical.Trail.Clear();
            Vertical.SpeedLines.Clear();
            Vertical.LeftWallX = leftWallX;
            Vertical.RightWallX = rightWallX;
            Vertical.Speed = VerticalGameplayConfig.BaseSpeed;
            Vertical.FrameCount = 0;
            Vertical.SpawnTimer = VerticalGameplayConfig.SpawnRateInitial;
            Vertical.SpawnRate = VerticalGameplayConfig.SpawnRateInitial;
            Vertical.Score = 0f;
            Vertical.LastMilestone = 0;
            Vertical.GridOffset = 0f;
            Vertical.Dying = false;
            Vertical.DeathFrames = 0f;
            Vertical.DeathScore = 0;
            Vertical.Shield = ShieldState.Create(VerticalGameplayConfig.ShieldInterval);
        }

        private void UpdateHorizontal(float dt, float factor)
        {
            var state = Horizontal;

            if (state.Dying)
            {
                UpdateHorizontalDeath(factor);
                return;
            }

            state.Speed += HorizontalGameplayConfig.SpeedIncrement * factor;
            var dashRatio = state.Shield.DashTime > 0f
                ? Mathf.Min(1f, state.Shield.DashTime / HorizontalGameplayConfig.ShieldDashDuration)
                : 0f;
            var dashStrength = dashRatio * dashRatio;
            var worldSpeed = state.Speed * (1f + dashStrength * HorizontalGameplayConfig.ShieldDashWorldBoost);
            state.SpawnRate = Mathf.Max(20f, HorizontalGameplayConfig.SpawnRateInitial - (state.Speed - HorizontalGameplayConfig.BaseSpeed) * 8f);

            var speedRatio = state.Speed / HorizontalGameplayConfig.BaseSpeed;
            var jumpScale = 1f + (speedRatio - 1f) * 0.4f;
            state.Player.DY += HorizontalGameplayConfig.Gravity * jumpScale * factor;
            state.Player.Y += state.Player.DY * factor;
            state.Player.Rotation += (state.Player.TargetRotation - state.Player.Rotation) * 0.3f * factor;
            state.Player.ScaleX += (1f - state.Player.ScaleX) * 0.22f * factor;
            state.Player.ScaleY += (1f - state.Player.ScaleY) * 0.22f * factor;
            state.Player.X += state.Player.DX * factor;
            state.Player.DX *= Mathf.Pow(0.94f, factor);
            if (Mathf.Abs(state.Player.DX) < 0.1f)
            {
                state.Player.DX = 0f;
            }

            state.Player.X -= (state.Player.X - state.Player.BaseX) * 0.06f * factor;

            var wasGrounded = state.Player.IsGrounded;
            if (state.Player.Y + state.Player.Size * 0.5f > state.GroundY)
            {
                if (!wasGrounded)
                {
                    state.Player.ScaleX = 1.25f;
                    state.Player.ScaleY = 0.75f;
                    CreateParticles(state.Particles, state.Player.X, state.GroundY, 3, false);
                    AudioManager.Instance?.Land();
                }

                state.Player.Y = state.GroundY - state.Player.Size * 0.5f;
                state.Player.DY = 0f;
                state.Player.IsGrounded = true;
            }
            else if (state.Player.Y - state.Player.Size * 0.5f < state.RoofY)
            {
                state.Player.Y = state.RoofY + state.Player.Size * 0.5f;
                state.Player.DY = Mathf.Abs(state.Player.DY) * 0.3f + 2f;
                state.Player.IsGrounded = false;
            }
            else
            {
                state.Player.IsGrounded = false;
            }

            if (state.Player.IsGrounded && state.JumpQueued)
            {
                if (Time.unscaledTime - state.JumpQueuedAt < 0.35f)
                {
                    DoHorizontalJump();
                }
                else
                {
                    state.JumpQueued = false;
                    state.JumpQueuedAt = 0f;
                }
            }

            state.FrameCount++;
            UpdateSharedMotion(
                state.Particles,
                state.Trail,
                state.SpeedLines,
                state.FrameCount,
                state.Player.X,
                state.Player.Y,
                state.Player.Size,
                state.Shield.TimeLeft > 0f,
                worldSpeed,
                factor,
                true,
                state.RoofY,
                state.GroundY,
                width);

            state.GridOffset = Mathf.Repeat(state.GridOffset + worldSpeed * factor * 0.5f, 80f);

            if (!horizontalHardMode && state.Score >= HorizontalGameplayConfig.HardModeStartScore)
            {
                horizontalHardMode = true;
                ShuffleHorizontal(HorizontalGameplayConfig.HardPatterns);
            }

            SpawnHorizontal(worldSpeed, factor);
            MoveAndCollideHorizontal(worldSpeed, factor);
            UpdateScoreAndShield(dt, true, dashStrength);
            UpdateShieldAnimation(ref state.Shield, dt);

            var camTargetX = (state.Player.X - state.Player.BaseX) * 0.3f;
            state.CamX += (camTargetX - state.CamX) * 0.06f;
        }

        private void DoHorizontalJump()
        {
            var state = Horizontal;
            var speedRatio = state.Speed / HorizontalGameplayConfig.BaseSpeed;
            var jumpScale = 1f + (speedRatio - 1f) * 0.4f;
            var shieldBoostActive = state.Shield.TimeLeft > 0f;

            state.Player.DY = HorizontalGameplayConfig.JumpForce * Mathf.Sqrt(jumpScale) * (shieldBoostActive ? 1.03f : 1f);
            state.Player.IsGrounded = false;
            state.Player.TargetRotation += Mathf.PI * 0.5f;
            state.Player.ScaleX = shieldBoostActive ? 0.76f : 0.8f;
            state.Player.ScaleY = shieldBoostActive ? 1.26f : 1.2f;
            state.Player.DX = shieldBoostActive ? 11.5f : 8f;
            CreateParticles(state.Particles, state.Player.X, state.Player.Y + state.Player.Size * 0.5f, shieldBoostActive ? 8 : 5, false);

            if (shieldBoostActive)
            {
                state.Shield.HitFlash = Mathf.Max(state.Shield.HitFlash, 0.35f);
                state.Shield.DashTime = Mathf.Max(state.Shield.DashTime, HorizontalGameplayConfig.ShieldDashDuration * 0.55f);
                state.Shield.DashFlash = Mathf.Max(state.Shield.DashFlash, 0.52f);
            }

            state.JumpQueued = false;
            state.JumpQueuedAt = 0f;
            AudioManager.Instance?.Jump();
        }

        private void SpawnHorizontal(float worldSpeed, float factor)
        {
            var state = Horizontal;
            state.SpawnTimer -= factor;

            if (state.SpawnTimer > 0f || horizontalPool.Count == 0)
            {
                return;
            }

            var pattern = horizontalPool[horizontalPoolIndex % horizontalPool.Count];
            horizontalPoolIndex++;
            if (horizontalPoolIndex % horizontalPool.Count == 0)
            {
                ShuffleHorizontal(horizontalHardMode ? HorizontalGameplayConfig.HardPatterns : HorizontalGameplayConfig.EasyPatterns);
            }

            var patternSpan = HorizontalGameplayConfig.GetPatternSpan(pattern);
            var sameBottomHeight = pattern.Steps.Length > 1 && AllHorizontalSteps(pattern, HorizontalObstacleType.Bottom)
                ? HorizontalGameplayConfig.BottomHeights[UnityEngine.Random.Range(0, HorizontalGameplayConfig.BottomHeights.Length)]
                : -1f;

            foreach (var step in pattern.Steps)
            {
                var heights = step.Type == HorizontalObstacleType.Top
                    ? HorizontalGameplayConfig.TopHeights
                    : HorizontalGameplayConfig.BottomHeights;
                var obstacleHeight = step.Type == HorizontalObstacleType.Bottom && sameBottomHeight > 0f
                    ? sameBottomHeight
                    : heights[UnityEngine.Random.Range(0, heights.Length)];
                state.Obstacles.Add(new HorizontalObstacle
                {
                    X = width + 200f + Mathf.Max(0f, state.CamX) + step.OffsetX,
                    Y = step.Type == HorizontalObstacleType.Bottom ? state.GroundY - obstacleHeight : state.RoofY,
                    Width = HorizontalGameplayConfig.ObstacleWidth,
                    Height = obstacleHeight,
                    Type = step.Type,
                });
            }

            var baseSpawnDelay = horizontalHardMode ? Mathf.Max(22f, state.SpawnRate * 0.7f) : state.SpawnRate;
            state.SpawnTimer = baseSpawnDelay + patternSpan / Mathf.Max(state.Speed, HorizontalGameplayConfig.BaseSpeed);
        }

        private void MoveAndCollideHorizontal(float worldSpeed, float factor)
        {
            var state = Horizontal;
            var visibleWorldLeft = width * 0.5f + state.CamX - width / (2f * HorizontalGameplayConfig.Zoom);

            for (var i = state.Obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = state.Obstacles[i];
                obstacle.X -= worldSpeed * factor;
                state.Obstacles[i] = obstacle;

                var cx = obstacle.X + obstacle.Width * 0.5f;
                var cy = obstacle.Y + obstacle.Height * 0.5f;
                var hit = Mathf.Abs(state.Player.X - cx) < state.Player.Size * 0.5f + obstacle.Width * 0.5f - 2f
                    && Mathf.Abs(state.Player.Y - cy) < state.Player.Size * 0.5f + obstacle.Height * 0.5f - 2f;

                if (hit)
                {
                    if (state.Shield.TimeLeft > 0f)
                    {
                        CreateParticles(state.Particles, cx, cy, 18, false);
                        state.Shield.HitFlash = 1f;
                        state.Obstacles.RemoveAt(i);
                        TriggerHorizontalShieldDash(0.65f);
                        AudioManager.Instance?.Land();
                        continue;
                    }

                    BeginHorizontalDeath();
                    return;
                }

                if (obstacle.X + obstacle.Width < visibleWorldLeft - 140f)
                {
                    state.Obstacles.RemoveAt(i);
                }
            }
        }

        private void UpdateVertical(float dt, float factor)
        {
            var state = Vertical;

            if (state.Dying)
            {
                UpdateVerticalDeath(factor);
                return;
            }

            var hardModeActive = state.Score >= VerticalGameplayConfig.HardModeStartScore;
            var shieldBoost = state.Shield.TimeLeft > 0f ? VerticalGameplayConfig.ShieldSwitchBoost : 1f;
            var dashRatio = state.Shield.DashTime > 0f
                ? Mathf.Min(1f, state.Shield.DashTime / VerticalGameplayConfig.ShieldDashDuration)
                : 0f;
            var dashStrength = dashRatio * dashRatio;

            state.Speed += (hardModeActive
                ? VerticalGameplayConfig.SpeedIncrementHard
                : VerticalGameplayConfig.SpeedIncrementEasy) * factor;
            var worldSpeed = state.Speed * (1f + dashStrength * VerticalGameplayConfig.ShieldDashWorldBoost);
            state.SpawnRate = hardModeActive
                ? Mathf.Max(20f, 50f - (state.Speed - VerticalGameplayConfig.BaseSpeed) * 8.8f)
                : Mathf.Max(36f, 72f - (state.Speed - VerticalGameplayConfig.BaseSpeed) * 5.8f);

            var distanceBeforeMove = Mathf.Abs(state.Player.TargetX - state.Player.X);
            state.Player.X += (state.Player.TargetX - state.Player.X) * VerticalGameplayConfig.MoveSpeed * shieldBoost * dt * 4f;
            state.Player.Rotation += (state.Player.TargetRotation - state.Player.Rotation) * 0.3f * factor;
            state.Player.ScaleX += (1f - state.Player.ScaleX) * 0.2f * factor;
            state.Player.ScaleY += (1f - state.Player.ScaleY) * 0.2f * factor;
            var distanceAfterMove = Mathf.Abs(state.Player.TargetX - state.Player.X);

            if (state.Player.IsSwitching && distanceBeforeMove > 1.5f && distanceAfterMove <= 1.5f)
            {
                state.Player.X = state.Player.TargetX;
                state.Player.ScaleX = 1.2f;
                state.Player.ScaleY = 0.8f;
                state.Player.IsSwitching = false;
                CreateParticles(state.Particles, state.Player.X, state.Player.Y, 6, false);
                AudioManager.Instance?.Land();
            }

            state.FrameCount++;
            UpdateSharedMotion(
                state.Particles,
                state.Trail,
                state.SpeedLines,
                state.FrameCount,
                state.Player.X,
                state.Player.Y,
                state.Player.Size,
                state.Shield.TimeLeft > 0f,
                worldSpeed,
                factor,
                false,
                0f,
                height,
                width);

            state.GridOffset = Mathf.Repeat(state.GridOffset + worldSpeed * factor * 0.5f, 80f);

            if (!verticalHardMode && state.Score >= VerticalGameplayConfig.HardModeStartScore)
            {
                verticalHardMode = true;
                ShuffleVertical(VerticalGameplayConfig.HardPatterns);
            }

            SpawnVertical(factor);
            MoveAndCollideVertical(worldSpeed, factor);
            UpdateScoreAndShield(dt, false, dashStrength);
            UpdateShieldAnimation(ref state.Shield, dt);
        }

        private void SpawnVertical(float factor)
        {
            var state = Vertical;
            state.SpawnTimer -= factor;

            if (state.SpawnTimer > 0f || verticalPool.Count == 0)
            {
                return;
            }

            var pattern = verticalPool[verticalPoolIndex % verticalPool.Count];
            verticalPoolIndex++;
            if (verticalPoolIndex % verticalPool.Count == 0)
            {
                ShuffleVertical(verticalHardMode ? VerticalGameplayConfig.HardPatterns : VerticalGameplayConfig.EasyPatterns);
            }

            var patternWidth = VerticalGameplayConfig.PickObstacleWidth(pattern.Kind, verticalHardMode);
            foreach (var step in pattern.Steps)
            {
                var obstacleWidth = pattern.Kind == VerticalPatternKind.Single
                    ? VerticalGameplayConfig.PickObstacleWidth(VerticalPatternKind.Single, verticalHardMode)
                    : patternWidth;
                var obstacleX = step.Side == VerticalObstacleSide.Left ? state.LeftWallX : state.RightWallX - obstacleWidth;
                state.Obstacles.Add(new VerticalObstacle
                {
                    X = obstacleX,
                    Y = VerticalGameplayConfig.GetObstacleSpawnY(height, step.OffsetY),
                    Width = obstacleWidth,
                    Height = 22f,
                    Side = step.Side,
                });
            }

            if (pattern.Kind == VerticalPatternKind.Stacked)
            {
                state.SpawnTimer = Mathf.Max(22f, state.SpawnRate * (verticalHardMode ? 0.78f : 0.92f));
            }
            else if (pattern.Kind == VerticalPatternKind.ZigZag)
            {
                state.SpawnTimer = Mathf.Max(verticalHardMode ? 26f : 44f, state.SpawnRate * (verticalHardMode ? 0.86f : 1.08f));
            }
            else
            {
                state.SpawnTimer = Mathf.Max(verticalHardMode ? 28f : 50f, state.SpawnRate * (verticalHardMode ? 0.92f : 1.18f));
            }
        }

        private void MoveAndCollideVertical(float worldSpeed, float factor)
        {
            var state = Vertical;
            var visibleBottom = height * 0.5f + height / (2f * VerticalGameplayConfig.Zoom);

            for (var i = state.Obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = state.Obstacles[i];
                obstacle.Y += worldSpeed * factor;
                state.Obstacles[i] = obstacle;

                var cx = obstacle.X + obstacle.Width * 0.5f;
                var cy = obstacle.Y + obstacle.Height * 0.5f;
                var hit = Mathf.Abs(state.Player.X - cx) < state.Player.Size * 0.5f + obstacle.Width * 0.5f - 4f
                    && Mathf.Abs(state.Player.Y - cy) < state.Player.Size * 0.5f + obstacle.Height * 0.5f - 4f;

                if (hit)
                {
                    if (state.Shield.TimeLeft > 0f)
                    {
                        CreateParticles(state.Particles, cx, cy, 18, false);
                        state.Shield.HitFlash = 1f;
                        state.Obstacles.RemoveAt(i);
                        AudioManager.Instance?.Land();
                        continue;
                    }

                    BeginVerticalDeath();
                    return;
                }

                if (obstacle.Y > visibleBottom + 140f)
                {
                    state.Obstacles.RemoveAt(i);
                }
            }
        }

        private void UpdateScoreAndShield(float dt, bool horizontal, float dashStrength)
        {
            var scoreRate = 0f;
            if (horizontal)
            {
                var state = Horizontal;
                scoreRate = (6f + (state.Speed - HorizontalGameplayConfig.BaseSpeed) * 1.2f)
                    * (1f + dashStrength * HorizontalGameplayConfig.ShieldDashScoreBoost);
                state.Score += dt * scoreRate;
                UpdateMilestoneAndShield(ref state.Shield, state.Score, state.LastMilestone, true, dt, out state.LastMilestone);
            }
            else
            {
                var state = Vertical;
                scoreRate = (6f + (state.Speed - VerticalGameplayConfig.BaseSpeed) * 1.2f)
                    * (1f + dashStrength * VerticalGameplayConfig.ShieldDashScoreBoost);
                state.Score += dt * scoreRate;
                UpdateMilestoneAndShield(ref state.Shield, state.Score, state.LastMilestone, false, dt, out state.LastMilestone);
            }
        }

        private void UpdateMilestoneAndShield(ref ShieldState shield, float score, int lastMilestone, bool horizontal, float dt, out int nextMilestone)
        {
            var shieldTriggered = false;
            var triggerScore = horizontal ? HorizontalGameplayConfig.ShieldInterval : VerticalGameplayConfig.ShieldInterval;
            var duration = horizontal ? HorizontalGameplayConfig.ShieldDuration : VerticalGameplayConfig.ShieldDuration;

            while (Mathf.FloorToInt(score) >= shield.NextTriggerScore)
            {
                shield.NextTriggerScore += triggerScore;
                shield.TimeLeft = duration;
                shield.HitFlash = 1f;
                shield.WarningStep = 0;
                shieldTriggered = true;

                if (horizontal)
                {
                    CreateParticles(Horizontal.Particles, Horizontal.Player.X, Horizontal.Player.Y, 28, false);
                    TriggerHorizontalShieldDash(1f);
                }
                else
                {
                    CreateParticles(Vertical.Particles, Vertical.Player.X, Vertical.Player.Y, 28, false);
                    TriggerVerticalShieldDash(1f);
                }

                AudioManager.Instance?.ShieldAppear();
            }

            var previousShieldTime = shield.TimeLeft;
            if (shield.TimeLeft > 0f)
            {
                var warningWindow = horizontal
                    ? HorizontalGameplayConfig.ShieldWarningWindow
                    : VerticalGameplayConfig.ShieldWarningWindow;
                shield.TimeLeft = Mathf.Max(0f, shield.TimeLeft - dt);
                if (shield.TimeLeft > 0f && shield.TimeLeft <= warningWindow)
                {
                    var warningProgress = 1f - shield.TimeLeft / warningWindow;
                    var warningStepTarget = Mathf.Min(4, Mathf.FloorToInt(warningProgress * 4f) + 1);
                    while (shield.WarningStep < warningStepTarget)
                    {
                        shield.WarningStep++;
                        AudioManager.Instance?.ShieldWarning(shield.WarningStep / 4f);
                    }
                }

                if (previousShieldTime > 0f && shield.TimeLeft <= 0f)
                {
                    shield.HitFlash = 1f;
                    shield.DashFlash = 1f;
                    shield.WarningStep = 0;
                    if (horizontal)
                    {
                        CreateParticles(Horizontal.Particles, Horizontal.Player.X, Horizontal.Player.Y, 20, false);
                    }
                    else
                    {
                        CreateParticles(Vertical.Particles, Vertical.Player.X, Vertical.Player.Y, 20, false);
                    }

                    AudioManager.Instance?.ShieldEnd();
                }
            }

            nextMilestone = lastMilestone;
            var milestone = Mathf.FloorToInt(score / 100f);
            if (milestone > lastMilestone)
            {
                nextMilestone = milestone;
                if (!shieldTriggered)
                {
                    AudioManager.Instance?.Milestone();
                }
            }
        }

        private static void UpdateShieldAnimation(ref ShieldState shield, float dt)
        {
            var targetOpacity = shield.TimeLeft > 0f ? 1f : 0f;
            shield.Opacity += (targetOpacity - shield.Opacity) * Mathf.Min(1f, dt * (shield.TimeLeft > 0f ? 8f : 4f));
            shield.Pulse += dt * (shield.TimeLeft > 0f ? 6f : 3f);
            shield.HitFlash = Mathf.Max(0f, shield.HitFlash - dt * 2.5f);
            shield.DashTime = Mathf.Max(0f, shield.DashTime - dt);
            shield.DashFlash = Mathf.Max(0f, shield.DashFlash - dt * 3.6f);
        }

        private void UpdateHorizontalDeath(float factor)
        {
            Horizontal.DeathFrames -= factor;
            UpdateParticles(Horizontal.Particles, 0.4f, false);

            if (Horizontal.DeathFrames <= 0f)
            {
                var score = Horizontal.DeathScore;
                IsActive = false;
                GameOver?.Invoke(score);
            }
        }

        private void UpdateVerticalDeath(float factor)
        {
            Vertical.DeathFrames -= factor;
            UpdateParticles(Vertical.Particles, 1f, true);

            if (Vertical.DeathFrames <= 0f)
            {
                var score = Vertical.DeathScore;
                IsActive = false;
                GameOver?.Invoke(score);
            }
        }

        private void BeginHorizontalDeath()
        {
            Horizontal.Dying = true;
            Horizontal.DeathFrames = 14f;
            Horizontal.DeathScore = Mathf.FloorToInt(Horizontal.Score);
            CreateParticles(Horizontal.Particles, Horizontal.Player.X, Horizontal.Player.Y, 45, true);
            AudioManager.Instance?.Death();
        }

        private void BeginVerticalDeath()
        {
            Vertical.Dying = true;
            Vertical.DeathFrames = 14f;
            Vertical.DeathScore = Mathf.FloorToInt(Vertical.Score);
            CreateParticles(Vertical.Particles, Vertical.Player.X, Vertical.Player.Y, 45, true);
            AudioManager.Instance?.Death();
        }

        private void TriggerHorizontalShieldDash(float strength)
        {
            if (!Horizontal.Shield.IsLive)
            {
                return;
            }

            var dashStrength = Mathf.Clamp(strength, 0.4f, 1.2f);
            Horizontal.Shield.DashTime = Mathf.Max(Horizontal.Shield.DashTime, HorizontalGameplayConfig.ShieldDashDuration * dashStrength);
            Horizontal.Shield.DashFlash = Mathf.Max(Horizontal.Shield.DashFlash, 0.55f + dashStrength * 0.45f);
            Horizontal.Shield.HitFlash = Mathf.Max(Horizontal.Shield.HitFlash, 0.35f + dashStrength * 0.35f);
            Horizontal.Player.DX = Mathf.Max(Horizontal.Player.DX, 10f + dashStrength * 6f);
            Horizontal.Player.ScaleX = Mathf.Min(Horizontal.Player.ScaleX, 0.74f);
            Horizontal.Player.ScaleY = Mathf.Max(Horizontal.Player.ScaleY, 1.28f);
            CreateParticles(Horizontal.Particles, Horizontal.Player.X - Horizontal.Player.Size * 0.2f, Horizontal.Player.Y, Mathf.RoundToInt(8f + dashStrength * 8f), false);

            for (var i = 0; i < Mathf.RoundToInt(5f + dashStrength * 3f); i++)
            {
                Horizontal.SpeedLines.Add(SpeedLine.CreateHorizontal(
                    Horizontal.Player.X - 20f - UnityEngine.Random.value * 70f,
                    Horizontal.Player.Y + (UnityEngine.Random.value - 0.5f) * 70f,
                    54f + UnityEngine.Random.value * 60f,
                    0.16f + UnityEngine.Random.value * 0.12f));
            }
        }

        private void TriggerVerticalShieldDash(float strength)
        {
            if (!Vertical.Shield.IsLive)
            {
                return;
            }

            var dashStrength = Mathf.Clamp(strength, 0.4f, 1.2f);
            Vertical.Shield.DashTime = Mathf.Max(Vertical.Shield.DashTime, VerticalGameplayConfig.ShieldDashDuration * dashStrength);
            Vertical.Shield.DashFlash = Mathf.Max(Vertical.Shield.DashFlash, 0.55f + dashStrength * 0.45f);
            Vertical.Shield.HitFlash = Mathf.Max(Vertical.Shield.HitFlash, 0.35f + dashStrength * 0.35f);
            Vertical.Player.ScaleX = Mathf.Min(Vertical.Player.ScaleX, 0.74f);
            Vertical.Player.ScaleY = Mathf.Max(Vertical.Player.ScaleY, 1.3f);
            CreateParticles(Vertical.Particles, Vertical.Player.X, Vertical.Player.Y + Vertical.Player.Size * 0.12f, Mathf.RoundToInt(8f + dashStrength * 8f), false);

            for (var i = 0; i < Mathf.RoundToInt(5f + dashStrength * 3f); i++)
            {
                Vertical.SpeedLines.Add(SpeedLine.CreateVertical(
                    Vertical.LeftWallX + UnityEngine.Random.value * (Vertical.RightWallX - Vertical.LeftWallX),
                    Vertical.Player.Y - 40f - UnityEngine.Random.value * 70f,
                    54f + UnityEngine.Random.value * 60f,
                    0.16f + UnityEngine.Random.value * 0.12f));
            }
        }

        private static void UpdateSharedMotion(
            List<Particle> particles,
            List<TrailPoint> trail,
            List<SpeedLine> speedLines,
            int frameCount,
            float playerX,
            float playerY,
            float playerSize,
            bool shieldActive,
            float worldSpeed,
            float factor,
            bool horizontal,
            float minY,
            float maxY,
            float viewportWidth)
        {
            if (horizontal ? worldSpeed > 8f : worldSpeed > 7f)
            {
                var randomGate = (worldSpeed - (horizontal ? 8f : 7f)) * 0.015f * factor;
                if (UnityEngine.Random.value < randomGate)
                {
                    speedLines.Add(horizontal
                        ? SpeedLine.CreateHorizontal(viewportWidth + 10f, minY + UnityEngine.Random.value * (maxY - minY), 30f + UnityEngine.Random.value * 60f, 0.08f + UnityEngine.Random.value * 0.12f)
                        : SpeedLine.CreateVertical(playerX + (UnityEngine.Random.value - 0.5f) * 160f, -20f, 30f + UnityEngine.Random.value * 60f, 0.08f + UnityEngine.Random.value * 0.12f));
                }
            }

            for (var i = speedLines.Count - 1; i >= 0; i--)
            {
                var line = speedLines[i];
                if (horizontal)
                {
                    line.X -= worldSpeed * 2.5f * factor;
                    line.Opacity -= 0.006f * factor;
                    if (line.Opacity <= 0f || line.X + line.Length < -100f)
                    {
                        speedLines.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    line.Y += worldSpeed * 2.5f * factor;
                    line.Opacity -= 0.006f * factor;
                    if (line.Opacity <= 0f || line.Y > maxY + line.Length + 120f)
                    {
                        speedLines.RemoveAt(i);
                        continue;
                    }
                }

                speedLines[i] = line;
            }

            if (frameCount % 2 == 0)
            {
                trail.Add(new TrailPoint(playerX, playerY, shieldActive ? 0.58f : 0.4f));
            }

            if (shieldActive && frameCount % 4 == 0)
            {
                particles.Add(new Particle
                {
                    X = playerX + (UnityEngine.Random.value - 0.5f) * 16f,
                    Y = playerY + (UnityEngine.Random.value - 0.5f) * 16f,
                    DX = horizontal ? -0.35f - UnityEngine.Random.value * 0.85f : (UnityEngine.Random.value - 0.5f) * 0.9f,
                    DY = (UnityEngine.Random.value - 0.5f) * 0.9f,
                    Life = 0.45f,
                    Size = 1.2f + UnityEngine.Random.value * 1.6f,
                });
            }

            for (var i = trail.Count - 1; i >= 0; i--)
            {
                var point = trail[i];
                point.Opacity -= 0.02f * factor;
                if (point.Opacity <= 0f)
                {
                    trail.RemoveAt(i);
                    continue;
                }

                trail[i] = point;
            }

            UpdateParticles(particles, 1f, true);
        }

        private static void UpdateParticles(List<Particle> particles, float movementScale, bool gravity)
        {
            for (var i = particles.Count - 1; i >= 0; i--)
            {
                var particle = particles[i];
                particle.X += particle.DX * movementScale;
                particle.Y += particle.DY * movementScale;
                if (gravity)
                {
                    particle.DY += 0.05f;
                }

                particle.Life -= gravity ? 0.035f : 0.03f;
                if (particle.Life <= 0f)
                {
                    particles.RemoveAt(i);
                    continue;
                }

                particles[i] = particle;
            }
        }

        private static void CreateParticles(List<Particle> particles, float x, float y, int count, bool big)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                var speed = big ? 2f + UnityEngine.Random.value * 6f : 1f + UnityEngine.Random.value * 4f;
                particles.Add(new Particle
                {
                    X = x,
                    Y = y,
                    DX = Mathf.Cos(angle) * speed,
                    DY = Mathf.Sin(angle) * speed,
                    Life = 1f,
                    Size = big ? 2f + UnityEngine.Random.value * 4f : 1f + UnityEngine.Random.value * 3f,
                });
            }
        }

        private void ShuffleHorizontal(HorizontalPattern[] patterns)
        {
            horizontalPool.Clear();
            horizontalPool.AddRange(patterns);
            Shuffle(horizontalPool);
            horizontalPoolIndex = 0;
        }

        private void ShuffleVertical(VerticalPattern[] patterns)
        {
            verticalPool.Clear();
            verticalPool.AddRange(patterns);
            Shuffle(verticalPool);
            verticalPoolIndex = 0;
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private static bool AllHorizontalSteps(HorizontalPattern pattern, HorizontalObstacleType type)
        {
            foreach (var step in pattern.Steps)
            {
                if (step.Type != type)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class HorizontalState
    {
        public HorizontalPlayer Player;
        public readonly List<HorizontalObstacle> Obstacles = new();
        public readonly List<Particle> Particles = new();
        public readonly List<TrailPoint> Trail = new();
        public readonly List<SpeedLine> SpeedLines = new();
        public float GroundY;
        public float RoofY;
        public float Speed;
        public float SpawnTimer;
        public float SpawnRate;
        public float Score;
        public int FrameCount;
        public int LastMilestone;
        public float GridOffset;
        public bool Dying;
        public float DeathFrames;
        public int DeathScore;
        public float CamX;
        public bool JumpQueued;
        public float JumpQueuedAt;
        public ShieldState Shield;
    }

    public sealed class VerticalState
    {
        public VerticalPlayer Player;
        public readonly List<VerticalObstacle> Obstacles = new();
        public readonly List<Particle> Particles = new();
        public readonly List<TrailPoint> Trail = new();
        public readonly List<SpeedLine> SpeedLines = new();
        public float LeftWallX;
        public float RightWallX;
        public float Speed;
        public float SpawnTimer;
        public float SpawnRate;
        public float Score;
        public int FrameCount;
        public int LastMilestone;
        public float GridOffset;
        public bool Dying;
        public float DeathFrames;
        public int DeathScore;
        public ShieldState Shield;
    }

    public struct HorizontalPlayer
    {
        public float X;
        public float Y;
        public float DY;
        public float DX;
        public float Size;
        public float Rotation;
        public float TargetRotation;
        public float ScaleX;
        public float ScaleY;
        public bool IsGrounded;
        public float BaseX;
    }

    public struct VerticalPlayer
    {
        public float X;
        public float Y;
        public float TargetX;
        public float Size;
        public float Rotation;
        public float TargetRotation;
        public float ScaleX;
        public float ScaleY;
        public VerticalObstacleSide Side;
        public bool IsSwitching;
    }

    public struct HorizontalObstacle
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public HorizontalObstacleType Type;
    }

    public struct VerticalObstacle
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public VerticalObstacleSide Side;
    }

    public struct Particle
    {
        public float X;
        public float Y;
        public float DX;
        public float DY;
        public float Life;
        public float Size;
    }

    public struct TrailPoint
    {
        public TrailPoint(float x, float y, float opacity)
        {
            X = x;
            Y = y;
            Opacity = opacity;
        }

        public float X;
        public float Y;
        public float Opacity;
    }

    public struct SpeedLine
    {
        public static SpeedLine CreateHorizontal(float x, float y, float length, float opacity)
        {
            return new SpeedLine { X = x, Y = y, Length = length, Opacity = opacity, IsHorizontal = true };
        }

        public static SpeedLine CreateVertical(float x, float y, float length, float opacity)
        {
            return new SpeedLine { X = x, Y = y, Length = length, Opacity = opacity, IsHorizontal = false };
        }

        public float X;
        public float Y;
        public float Length;
        public float Opacity;
        public bool IsHorizontal;
    }

    public struct ShieldState
    {
        public static ShieldState Create(float nextTriggerScore)
        {
            return new ShieldState
            {
                NextTriggerScore = nextTriggerScore,
            };
        }

        public bool IsLive => TimeLeft > 0f;

        public float TimeLeft;
        public float Opacity;
        public float NextTriggerScore;
        public float Pulse;
        public float HitFlash;
        public float DashTime;
        public float DashFlash;
        public int WarningStep;
    }
}
