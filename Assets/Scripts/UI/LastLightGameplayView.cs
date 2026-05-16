using LastLight.Core;
using LastLight.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight.UI
{
    public sealed class LastLightGameplayView : Graphic
    {
        private const float HorizontalZoom = HorizontalGameplayConfig.Zoom;
        private const float VerticalZoom = VerticalGameplayConfig.Zoom;

        private readonly Color32 black = new(0, 0, 0, 255);
        private readonly Color32 white = new(255, 255, 255, 255);

        private LastLightGameSession session;
        private float glowIntensity = 0.8f;
        private bool deathFlashesEnabled = true;

        public void Configure(LastLightGameSession nextSession, float glow, bool deathFlashes)
        {
            session = nextSession;
            glowIntensity = Mathf.Max(0f, glow);
            deathFlashesEnabled = deathFlashes;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            var width = rect.width;
            var height = rect.height;
            AddLocalRect(vh, rect.min.x, rect.min.y, width, height, black);

            if (session == null)
            {
                return;
            }

            if (session.Mode == GameMode.Horizontal)
            {
                DrawHorizontal(vh, width, height);
            }
            else
            {
                DrawVertical(vh, width, height);
            }
        }

        private void DrawHorizontal(VertexHelper vh, float width, float height)
        {
            var state = session.Horizontal;
            var visibleWorldLeft = width * 0.5f + state.CamX - width / (2f * HorizontalZoom);
            var visibleWorldRight = width * 0.5f + state.CamX + width / (2f * HorizontalZoom);
            var visibleWorldWidth = visibleWorldRight - visibleWorldLeft;
            var softAlpha = Alpha(0.05f * glowIntensity);
            var coreAlpha = Alpha(0.92f);

            var gridStartX = Mathf.Floor((visibleWorldLeft - 160f - state.GridOffset) / 80f) * 80f + state.GridOffset;
            for (var gx = gridStartX; gx < visibleWorldRight + 160f; gx += 80f)
            {
                AddWorldLineHorizontal(vh, gx, state.RoofY, gx, state.GroundY, 1.4f, Alpha(0.035f), width, height, state.CamX);
            }

            DrawHorizontalBricks(vh, width, height, state, visibleWorldLeft, visibleWorldRight);

            foreach (var line in state.SpeedLines)
            {
                AddWorldLineHorizontal(
                    vh,
                    line.X,
                    line.Y,
                    line.X + line.Length,
                    line.Y,
                    1.3f,
                    Alpha(line.Opacity),
                    width,
                    height,
                    state.CamX);
            }

            AddWorldRectHorizontal(vh, visibleWorldLeft - visibleWorldWidth, state.RoofY - 5f, visibleWorldWidth * 3f, 6f, Alpha(softAlpha), width, height, state.CamX);
            AddWorldRectHorizontal(vh, visibleWorldLeft - visibleWorldWidth, state.GroundY - 1f, visibleWorldWidth * 3f, 6f, Alpha(softAlpha), width, height, state.CamX);
            AddWorldRectHorizontal(vh, visibleWorldLeft - visibleWorldWidth, state.RoofY - 4f, visibleWorldWidth * 3f, 4f, Alpha(coreAlpha), width, height, state.CamX);
            AddWorldRectHorizontal(vh, visibleWorldLeft - visibleWorldWidth, state.GroundY, visibleWorldWidth * 3f, 4f, Alpha(coreAlpha), width, height, state.CamX);

            foreach (var obstacle in state.Obstacles)
            {
                AddHorizontalSpike(vh, obstacle, Alpha(0.98f), width, height, state.CamX);
            }

            foreach (var point in state.Trail)
            {
                var size = state.Player.Size * 0.38f;
                AddWorldRectHorizontal(vh, point.X - size * 0.5f, point.Y - size * 0.5f, size, size, Alpha(point.Opacity * 0.85f), width, height, state.CamX);
            }

            DrawParticlesHorizontal(vh, state.Particles, width, height, state.CamX);
            DrawDashHorizontal(vh, state, width, height);
            DrawShieldHorizontal(vh, state, width, height);
            DrawPlayerHorizontal(vh, state.Player, width, height, state.CamX);

            if (state.Dying && deathFlashesEnabled && state.DeathFrames > 10f)
            {
                AddLocalRect(vh, -width * 0.5f, -height * 0.5f, width, height, Alpha((state.DeathFrames - 10f) * 0.12f));
            }
        }

        private void DrawVertical(VertexHelper vh, float width, float height)
        {
            var state = session.Vertical;
            DrawVerticalBackdrop(vh, width, height, state);

            foreach (var line in state.SpeedLines)
            {
                AddWorldLineVertical(
                    vh,
                    line.X,
                    line.Y,
                    line.X,
                    line.Y + line.Length,
                    1.3f,
                    Alpha(line.Opacity),
                    width,
                    height);
            }

            AddWorldRectVertical(vh, state.LeftWallX - 5f, -height, 7f, height * 4f, Alpha(0.05f * glowIntensity), width, height);
            AddWorldRectVertical(vh, state.RightWallX - 1f, -height, 7f, height * 4f, Alpha(0.05f * glowIntensity), width, height);
            AddWorldRectVertical(vh, state.LeftWallX - 4f, -height, 4f, height * 4f, Alpha(0.92f), width, height);
            AddWorldRectVertical(vh, state.RightWallX, -height, 4f, height * 4f, Alpha(0.92f), width, height);

            foreach (var obstacle in state.Obstacles)
            {
                AddVerticalSpike(vh, obstacle, Alpha(0.98f), width, height);
            }

            foreach (var point in state.Trail)
            {
                var size = state.Player.Size * 0.38f;
                AddWorldRectVertical(vh, point.X - size * 0.5f, point.Y - size * 0.5f, size, size, Alpha(point.Opacity * 0.85f), width, height);
            }

            DrawParticlesVertical(vh, state.Particles, width, height);
            DrawDashVertical(vh, state, width, height);
            DrawShieldVertical(vh, state, width, height);
            DrawPlayerVertical(vh, state.Player, width, height);

            if (state.Dying && deathFlashesEnabled && state.DeathFrames > 10f)
            {
                AddLocalRect(vh, -width * 0.5f, -height * 0.5f, width, height, Alpha((state.DeathFrames - 10f) * 0.12f));
            }
        }

        private void DrawHorizontalBricks(VertexHelper vh, float width, float height, HorizontalState state, float visibleWorldLeft, float visibleWorldRight)
        {
            const float brickW = 40f;
            const float brickH = 16f;
            const float brickGap = 1.5f;
            var scrollX = state.GridOffset % (brickW + brickGap);
            var alpha = Alpha(0.07f);

            DrawHorizontalBrickBand(vh, width, height, state.CamX, visibleWorldLeft, visibleWorldRight, state.GroundY + 8f, state.GroundY + height, brickW, brickH, brickGap, scrollX, alpha);
            DrawHorizontalBrickBand(vh, width, height, state.CamX, visibleWorldLeft, visibleWorldRight, state.RoofY - height, state.RoofY - 8f, brickW, brickH, brickGap, scrollX, alpha);
        }

        private void DrawHorizontalBrickBand(
            VertexHelper vh,
            float width,
            float height,
            float camX,
            float left,
            float right,
            float startY,
            float endY,
            float brickW,
            float brickH,
            float brickGap,
            float scrollX,
            Color32 color)
        {
            var row = 0;
            for (var by = startY; by < endY; by += brickH + brickGap)
            {
                var offset = row % 2 == 1 ? (brickW + brickGap) * 0.5f : 0f;
                var brickStartX = left - brickW * 3f - scrollX + offset;
                for (var bx = brickStartX; bx < right + brickW * 3f; bx += brickW + brickGap)
                {
                    AddWorldRectHorizontal(vh, bx, by, brickW, 1f, color, width, height, camX);
                    AddWorldRectHorizontal(vh, bx, by + brickH, brickW, 1f, color, width, height, camX);
                    AddWorldRectHorizontal(vh, bx, by, 1f, brickH, color, width, height, camX);
                    AddWorldRectHorizontal(vh, bx + brickW, by, 1f, brickH, color, width, height, camX);
                }

                row++;
            }
        }

        private void DrawVerticalBackdrop(VertexHelper vh, float width, float height, VerticalState state)
        {
            var leftWallScreenX = ProjectVerticalX(state.LeftWallX, width);
            var rightWallScreenX = ProjectVerticalX(state.RightWallX, width);
            var gridStep = 80f * VerticalZoom;
            var lineStartY = -(state.GridOffset * VerticalZoom % gridStep) - gridStep;

            for (var y = lineStartY; y < height + gridStep; y += gridStep)
            {
                AddScreenRect(vh, leftWallScreenX, y, rightWallScreenX - leftWallScreenX, 1f, Alpha(0.035f), width, height);
            }

            var brickW = 16f * VerticalZoom;
            var brickH = 40f * VerticalZoom;
            var brickGap = 1.5f * VerticalZoom;
            var rowStep = brickH + brickGap;
            var columnStep = brickW + brickGap;
            var brickStartY = -(state.GridOffset * VerticalZoom % rowStep) - rowStep * 2f;
            var alpha = Alpha(0.07f);

            DrawVerticalBrickColumns(vh, width, height, 0f, leftWallScreenX - 3f, leftWallScreenX - 3f, -1f, brickStartY, brickW, brickH, columnStep, rowStep, alpha);
            DrawVerticalBrickColumns(vh, width, height, rightWallScreenX + 3f, width, rightWallScreenX + 3f, 1f, brickStartY, brickW, brickH, columnStep, rowStep, alpha);
        }

        private void DrawVerticalBrickColumns(
            VertexHelper vh,
            float width,
            float height,
            float startX,
            float endX,
            float anchorX,
            float direction,
            float brickStartY,
            float brickW,
            float brickH,
            float columnStep,
            float rowStep,
            Color32 color)
        {
            var column = 0;
            var x = direction < 0f ? anchorX - brickW : anchorX;

            while (direction < 0f ? x > startX - columnStep : x < endX + columnStep)
            {
                var columnOffsetY = column % 2 == 1 ? rowStep * 0.5f : 0f;
                var clampedX = Mathf.Max(x, startX);
                var drawW = Mathf.Min(brickW, endX - clampedX);
                if (drawW > 1f)
                {
                    for (var y = brickStartY + columnOffsetY; y < height + rowStep; y += rowStep)
                    {
                        AddScreenRect(vh, clampedX, y, drawW, 1f, color, width, height);
                        AddScreenRect(vh, clampedX, y + brickH, drawW, 1f, color, width, height);
                        AddScreenRect(vh, clampedX, y, 1f, brickH, color, width, height);
                        AddScreenRect(vh, clampedX + drawW, y, 1f, brickH, color, width, height);
                    }
                }

                x += direction * columnStep;
                column++;
            }
        }

        private void DrawPlayerHorizontal(VertexHelper vh, HorizontalPlayer player, float width, float height, float camX)
        {
            var screen = ProjectHorizontal(player.X, player.Y, width, height, camX);
            DrawRoundedBox(vh, screen, player.Size * HorizontalZoom, player.Size * HorizontalZoom, player.Rotation, player.ScaleX, player.ScaleY, Alpha(0.05f * glowIntensity), 2.15f);
            DrawRoundedBox(vh, screen, player.Size * HorizontalZoom, player.Size * HorizontalZoom, player.Rotation, player.ScaleX, player.ScaleY, Alpha(0.13f * glowIntensity), 1.55f);
            DrawRoundedBox(vh, screen, player.Size * HorizontalZoom, player.Size * HorizontalZoom, player.Rotation, player.ScaleX, player.ScaleY, white, 1f);
        }

        private void DrawPlayerVertical(VertexHelper vh, VerticalPlayer player, float width, float height)
        {
            var screen = ProjectVertical(player.X, player.Y, width, height);
            DrawRoundedBox(vh, screen, player.Size * VerticalZoom, player.Size * VerticalZoom, player.Rotation, player.ScaleX, player.ScaleY, Alpha(0.05f * glowIntensity), 2.15f);
            DrawRoundedBox(vh, screen, player.Size * VerticalZoom, player.Size * VerticalZoom, player.Rotation, player.ScaleX, player.ScaleY, Alpha(0.13f * glowIntensity), 1.55f);
            DrawRoundedBox(vh, screen, player.Size * VerticalZoom, player.Size * VerticalZoom, player.Rotation, player.ScaleX, player.ScaleY, white, 1f);
        }

        private void DrawDashHorizontal(VertexHelper vh, HorizontalState state, float width, float height)
        {
            if (state.Shield.DashFlash <= 0.01f)
            {
                return;
            }

            var dashLength = 86f + state.Shield.DashFlash * 54f;
            var alpha = Alpha(state.Shield.DashFlash * 0.28f);
            AddWorldRectHorizontal(vh, state.Player.X - dashLength - 8f, state.Player.Y - 4f, dashLength, 8f, alpha, width, height, state.CamX);
            AddWorldRectHorizontal(vh, state.Player.X - dashLength * 0.72f - 8f, state.Player.Y - 10f, dashLength * 0.72f, 4f, Alpha(state.Shield.DashFlash * 0.2f), width, height, state.CamX);
            AddWorldRectHorizontal(vh, state.Player.X - dashLength * 0.72f - 8f, state.Player.Y + 6f, dashLength * 0.72f, 4f, Alpha(state.Shield.DashFlash * 0.2f), width, height, state.CamX);
        }

        private void DrawDashVertical(VertexHelper vh, VerticalState state, float width, float height)
        {
            if (state.Shield.DashFlash <= 0.01f)
            {
                return;
            }

            var dashLength = 86f + state.Shield.DashFlash * 54f;
            var alpha = Alpha(state.Shield.DashFlash * 0.28f);
            AddWorldRectVertical(vh, state.Player.X - 4f, state.Player.Y + 8f, 8f, dashLength, alpha, width, height);
            AddWorldRectVertical(vh, state.Player.X - 10f, state.Player.Y + 20f, 4f, dashLength * 0.72f, Alpha(state.Shield.DashFlash * 0.2f), width, height);
            AddWorldRectVertical(vh, state.Player.X + 6f, state.Player.Y + 20f, 4f, dashLength * 0.72f, Alpha(state.Shield.DashFlash * 0.2f), width, height);
        }

        private void DrawShieldHorizontal(VertexHelper vh, HorizontalState state, float width, float height)
        {
            if (state.Shield.Opacity <= 0.01f)
            {
                return;
            }

            var screen = ProjectHorizontal(state.Player.X, state.Player.Y, width, height, state.CamX);
            DrawShield(vh, screen, state.Player.Size * HorizontalZoom, state.Shield);
        }

        private void DrawShieldVertical(VertexHelper vh, VerticalState state, float width, float height)
        {
            if (state.Shield.Opacity <= 0.01f)
            {
                return;
            }

            var screen = ProjectVertical(state.Player.X, state.Player.Y, width, height);
            DrawShield(vh, screen, state.Player.Size * VerticalZoom, state.Shield);
        }

        private void DrawShield(VertexHelper vh, Vector2 center, float playerSize, ShieldState shield)
        {
            var warningProgress = shield.TimeLeft > 0f ? Mathf.Max(0f, 1f - shield.TimeLeft / HorizontalGameplayConfig.ShieldWarningWindow) : 0f;
            var warningBlink = warningProgress > 0f
                ? 0.38f + 0.62f * (0.5f + 0.5f * Mathf.Sin(shield.Pulse * (12f + warningProgress * 10f)))
                : 1f;
            var opacity = shield.Opacity * (warningProgress > 0f ? 0.28f + 0.72f * warningBlink : 1f);
            var pulse = Mathf.Sin(shield.Pulse) * 2.6f;
            var size = playerSize + 22f + pulse;
            var ringRadius = size * 0.68f;
            var tint = warningProgress > 0f ? new Color32(255, 214, 160, 255) : new Color32(125, 230, 255, 255);
            tint.a = (byte)Mathf.Clamp(Mathf.RoundToInt((0.58f * opacity + shield.HitFlash * 0.18f) * 255f), 0, 255);

            DrawRoundedBox(vh, center, size, size, shield.Pulse * 0.08f, 1f, 1f, Alpha(0.08f * opacity), 1f);
            AddRing(vh, center, ringRadius, 1.7f, tint, 48, shield.Pulse * 0.9f);
            AddRing(vh, center, ringRadius - 7f, 1.1f, Alpha(0.22f * opacity), 48, -shield.Pulse);

            var orbit = ringRadius + 4f;
            for (var i = 0; i < 2; i++)
            {
                var angle = shield.Pulse * 1.7f + i * Mathf.PI;
                AddCircle(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbit, 2.6f, tint, 10);
            }
        }

        private void DrawParticlesHorizontal(VertexHelper vh, System.Collections.Generic.List<Particle> particles, float width, float height, float camX)
        {
            foreach (var particle in particles)
            {
                AddCircle(vh, ProjectHorizontal(particle.X, particle.Y, width, height, camX), Mathf.Max(1f, particle.Size * HorizontalZoom), Alpha(particle.Life), 8);
            }
        }

        private void DrawParticlesVertical(VertexHelper vh, System.Collections.Generic.List<Particle> particles, float width, float height)
        {
            foreach (var particle in particles)
            {
                AddCircle(vh, ProjectVertical(particle.X, particle.Y, width, height), Mathf.Max(1f, particle.Size * VerticalZoom), Alpha(particle.Life), 8);
            }
        }

        private void AddHorizontalSpike(VertexHelper vh, HorizontalObstacle obstacle, Color32 color, float width, float height, float camX)
        {
            const float spikeH = 10f;
            if (obstacle.Type == HorizontalObstacleType.Bottom)
            {
                AddWorldPolygonHorizontal(vh, color, width, height, camX,
                    new Vector2(obstacle.X, obstacle.Y + obstacle.Height),
                    new Vector2(obstacle.X, obstacle.Y + spikeH),
                    new Vector2(obstacle.X + obstacle.Width * 0.5f, obstacle.Y),
                    new Vector2(obstacle.X + obstacle.Width, obstacle.Y + spikeH),
                    new Vector2(obstacle.X + obstacle.Width, obstacle.Y + obstacle.Height));
            }
            else
            {
                AddWorldPolygonHorizontal(vh, color, width, height, camX,
                    new Vector2(obstacle.X, obstacle.Y),
                    new Vector2(obstacle.X, obstacle.Y + obstacle.Height - spikeH),
                    new Vector2(obstacle.X + obstacle.Width * 0.5f, obstacle.Y + obstacle.Height),
                    new Vector2(obstacle.X + obstacle.Width, obstacle.Y + obstacle.Height - spikeH),
                    new Vector2(obstacle.X + obstacle.Width, obstacle.Y));
            }
        }

        private void AddVerticalSpike(VertexHelper vh, VerticalObstacle obstacle, Color32 color, float width, float height)
        {
            const float spikeW = 10f;
            if (obstacle.Side == VerticalObstacleSide.Left)
            {
                AddWorldPolygonVertical(vh, color, width, height,
                    new Vector2(obstacle.X, obstacle.Y),
                    new Vector2(obstacle.X, obstacle.Y + obstacle.Height),
                    new Vector2(obstacle.X + obstacle.Width - spikeW, obstacle.Y + obstacle.Height),
                    new Vector2(obstacle.X + obstacle.Width, obstacle.Y + obstacle.Height * 0.5f),
                    new Vector2(obstacle.X + obstacle.Width - spikeW, obstacle.Y));
            }
            else
            {
                AddWorldPolygonVertical(vh, color, width, height,
                    new Vector2(obstacle.X + obstacle.Width, obstacle.Y),
                    new Vector2(obstacle.X + obstacle.Width, obstacle.Y + obstacle.Height),
                    new Vector2(obstacle.X + spikeW, obstacle.Y + obstacle.Height),
                    new Vector2(obstacle.X, obstacle.Y + obstacle.Height * 0.5f),
                    new Vector2(obstacle.X + spikeW, obstacle.Y));
            }
        }

        private void AddWorldRectHorizontal(VertexHelper vh, float x, float y, float rectWidth, float rectHeight, Color32 color, float width, float height, float camX)
        {
            AddPolygon(vh, color,
                ToLocal(ProjectHorizontal(x, y, width, height, camX), width, height),
                ToLocal(ProjectHorizontal(x + rectWidth, y, width, height, camX), width, height),
                ToLocal(ProjectHorizontal(x + rectWidth, y + rectHeight, width, height, camX), width, height),
                ToLocal(ProjectHorizontal(x, y + rectHeight, width, height, camX), width, height));
        }

        private void AddWorldRectVertical(VertexHelper vh, float x, float y, float rectWidth, float rectHeight, Color32 color, float width, float height)
        {
            AddPolygon(vh, color,
                ToLocal(ProjectVertical(x, y, width, height), width, height),
                ToLocal(ProjectVertical(x + rectWidth, y, width, height), width, height),
                ToLocal(ProjectVertical(x + rectWidth, y + rectHeight, width, height), width, height),
                ToLocal(ProjectVertical(x, y + rectHeight, width, height), width, height));
        }

        private void AddWorldLineHorizontal(VertexHelper vh, float x1, float y1, float x2, float y2, float thickness, Color32 color, float width, float height, float camX)
        {
            var start = ProjectHorizontal(x1, y1, width, height, camX);
            var end = ProjectHorizontal(x2, y2, width, height, camX);
            AddScreenLine(vh, start, end, thickness, color, width, height);
        }

        private void AddWorldLineVertical(VertexHelper vh, float x1, float y1, float x2, float y2, float thickness, Color32 color, float width, float height)
        {
            var start = ProjectVertical(x1, y1, width, height);
            var end = ProjectVertical(x2, y2, width, height);
            AddScreenLine(vh, start, end, thickness, color, width, height);
        }

        private void AddWorldPolygonHorizontal(VertexHelper vh, Color32 color, float width, float height, float camX, params Vector2[] worldPoints)
        {
            var local = new Vector2[worldPoints.Length];
            for (var i = 0; i < worldPoints.Length; i++)
            {
                local[i] = ToLocal(ProjectHorizontal(worldPoints[i].x, worldPoints[i].y, width, height, camX), width, height);
            }

            AddPolygon(vh, color, local);
        }

        private void AddWorldPolygonVertical(VertexHelper vh, Color32 color, float width, float height, params Vector2[] worldPoints)
        {
            var local = new Vector2[worldPoints.Length];
            for (var i = 0; i < worldPoints.Length; i++)
            {
                local[i] = ToLocal(ProjectVertical(worldPoints[i].x, worldPoints[i].y, width, height), width, height);
            }

            AddPolygon(vh, color, local);
        }

        private static Vector2 ProjectHorizontal(float x, float y, float width, float height, float camX)
        {
            return new Vector2(
                width * 0.5f + HorizontalZoom * (x - width * 0.5f - camX),
                height * 0.5f + HorizontalZoom * (y - height * 0.5f));
        }

        private static Vector2 ProjectVertical(float x, float y, float width, float height)
        {
            return new Vector2(
                ProjectVerticalX(x, width),
                height * 0.5f + VerticalZoom * (y - height * 0.5f));
        }

        private static float ProjectVerticalX(float x, float width)
        {
            return width * 0.5f + (x - width * 0.5f) * VerticalZoom;
        }

        private static Vector2 ToLocal(Vector2 screen, float width, float height)
        {
            return new Vector2(screen.x - width * 0.5f, height * 0.5f - screen.y);
        }

        private void AddScreenRect(VertexHelper vh, float x, float y, float rectWidth, float rectHeight, Color32 color, float width, float height)
        {
            AddPolygon(vh, color,
                ToLocal(new Vector2(x, y), width, height),
                ToLocal(new Vector2(x + rectWidth, y), width, height),
                ToLocal(new Vector2(x + rectWidth, y + rectHeight), width, height),
                ToLocal(new Vector2(x, y + rectHeight), width, height));
        }

        private void AddScreenLine(VertexHelper vh, Vector2 screenStart, Vector2 screenEnd, float thickness, Color32 color, float width, float height)
        {
            var direction = (screenEnd - screenStart).normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            var normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;
            AddPolygon(vh, color,
                ToLocal(screenStart - normal, width, height),
                ToLocal(screenStart + normal, width, height),
                ToLocal(screenEnd + normal, width, height),
                ToLocal(screenEnd - normal, width, height));
        }

        private static void AddLocalRect(VertexHelper vh, float x, float y, float rectWidth, float rectHeight, Color32 color)
        {
            AddPolygon(vh, color,
                new Vector2(x, y),
                new Vector2(x + rectWidth, y),
                new Vector2(x + rectWidth, y + rectHeight),
                new Vector2(x, y + rectHeight));
        }

        private void DrawRoundedBox(VertexHelper vh, Vector2 screenCenter, float boxWidth, float boxHeight, float rotation, float scaleX, float scaleY, Color32 color, float sizeMultiplier)
        {
            var rect = rectTransform.rect;
            var half = new Vector2(boxWidth * scaleX * sizeMultiplier * 0.5f, boxHeight * scaleY * sizeMultiplier * 0.5f);
            var radius = Mathf.Min(half.x, half.y) * 0.32f;
            var cos = Mathf.Cos(rotation);
            var sin = Mathf.Sin(rotation);
            var points = new Vector2[28];
            var index = 0;

            AddRoundedCorner(points, ref index, new Vector2(half.x - radius, -half.y + radius), radius, -90f, 0f, screenCenter, cos, sin, rect);
            AddRoundedCorner(points, ref index, new Vector2(half.x - radius, half.y - radius), radius, 0f, 90f, screenCenter, cos, sin, rect);
            AddRoundedCorner(points, ref index, new Vector2(-half.x + radius, half.y - radius), radius, 90f, 180f, screenCenter, cos, sin, rect);
            AddRoundedCorner(points, ref index, new Vector2(-half.x + radius, -half.y + radius), radius, 180f, 270f, screenCenter, cos, sin, rect);

            AddPolygon(vh, color, points);
        }

        private void AddRoundedCorner(
            Vector2[] points,
            ref int index,
            Vector2 cornerCenter,
            float radius,
            float startAngle,
            float endAngle,
            Vector2 screenCenter,
            float cos,
            float sin,
            Rect rect)
        {
            const int cornerSegments = 6;
            for (var i = 0; i < cornerSegments + 1; i++)
            {
                var angle = Mathf.Lerp(startAngle, endAngle, i / (float)cornerSegments) * Mathf.Deg2Rad;
                var point = cornerCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                points[index++] = ToLocal(Rotate(point, cos, sin) + screenCenter, rect.width, rect.height);
            }
        }

        private static Vector2 Rotate(Vector2 point, float cos, float sin)
        {
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }

        private void AddCircle(VertexHelper vh, Vector2 screenCenter, float radius, Color32 color, int segments)
        {
            var rect = rectTransform.rect;
            var center = ToLocal(screenCenter, rect.width, rect.height);
            var start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);

            for (var i = 0; i <= segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var screen = screenCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(ToLocal(screen, rect.width, rect.height), color, Vector2.zero);
            }

            for (var i = 1; i <= segments; i++)
            {
                vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        private void AddRing(VertexHelper vh, Vector2 screenCenter, float radius, float thickness, Color32 color, int segments, float rotation)
        {
            var rect = rectTransform.rect;
            var start = vh.currentVertCount;
            var inner = radius - thickness;
            var outer = radius + thickness;

            for (var i = 0; i <= segments; i++)
            {
                var angle = rotation + i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(ToLocal(screenCenter + direction * inner, rect.width, rect.height), color, Vector2.zero);
                vh.AddVert(ToLocal(screenCenter + direction * outer, rect.width, rect.height), color, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var index = start + i * 2;
                vh.AddTriangle(index, index + 1, index + 3);
                vh.AddTriangle(index, index + 3, index + 2);
            }
        }

        private static void AddPolygon(VertexHelper vh, Color32 color, params Vector2[] points)
        {
            if (points.Length < 3)
            {
                return;
            }

            var start = vh.currentVertCount;
            for (var i = 0; i < points.Length; i++)
            {
                vh.AddVert(points[i], color, Vector2.zero);
            }

            for (var i = 1; i < points.Length - 1; i++)
            {
                vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        private static Color32 Alpha(float alpha)
        {
            return new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
        }

        private static Color32 Alpha(Color32 color)
        {
            return color;
        }
    }
}
