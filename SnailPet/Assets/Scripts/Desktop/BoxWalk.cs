using UnityEngine;

namespace SnailPet.Desktop
{
    /// <summary>박스의 네 벽. 이 순서대로 순환하면 안쪽을 한 바퀴 돈다.</summary>
    public enum BoxEdge { Bottom = 0, Right = 1, Top = 2, Left = 3 }

    /// <summary>
    /// 달팽이가 어디에 붙어 있는지. 화면 좌표가 아니라 (박스, 벽, 진행도) 로 들고 있는 것이 핵심이다.
    /// 창이 움직이거나 크기가 바뀌어도 매 프레임 현재 사각형에서 다시 계산하므로 알아서 따라붙는다.
    /// </summary>
    public struct BoxAnchor
    {
        public BoxEdge Edge;
        public float T;          // 벽 위에서의 진행도 0..1 (진행 방향 기준)
        public bool Forward;     // 진행 방향으로 걷는가 (뒤로 걸으면 false)

        /// <summary>모서리를 도는 중일 때 0..1. 0 이면 벽 위에 평평하게 붙어 있다.</summary>
        public float Turn;

        /// <summary>도는 쪽. true 면 <see cref="BoxWalk.Next"/> 쪽 모서리를 돌고 있다.</summary>
        public bool TurnToNext;
    }

    /// <summary>한 프레임의 자세. 좌표는 가상 화면 px (y 는 아래로 증가).</summary>
    public struct SnailPose
    {
        public Vector2 RootScreen;   // 스프라이트 루트가 놓일 지점
        public float RotationDeg;    // 월드 회전 (CCW 양수)
        public bool FlipX;
        public bool Valid;
    }

    /// <summary>
    /// 박스 <b>안쪽</b> 벽을 기어다니는 기하.
    ///
    /// 발은 항상 박스 바깥을 향하고 몸은 안쪽으로 들어온다. 테라리움 유리벽에 붙은 달팽이와 같다.
    ///  · 아래 벽 → 똑바로 섬        · 위 벽   → 뒤집혀 매달림
    ///  · 오른쪽 벽 → 90° 회전       · 왼쪽 벽 → -90° 회전
    ///
    /// 박스가 창이든 화면이든 사각형이기만 하면 같은 코드가 돈다.
    /// </summary>
    public static class BoxWalk
    {
        /// <summary>벽의 바깥 방향 (화면 좌표, y 아래로 증가).</summary>
        public static Vector2 OutwardNormal(BoxEdge e) => e switch
        {
            BoxEdge.Bottom => new Vector2(0f,  1f),
            BoxEdge.Right  => new Vector2(1f,  0f),
            BoxEdge.Top    => new Vector2(0f, -1f),
            _              => new Vector2(-1f, 0f),
        };

        /// <summary>
        /// 발이 바깥 법선을 향하게 하는 월드 회전.
        /// 스프라이트의 기본 발 방향은 로컬 -y (월드 아래) 이므로 아래 벽이 0도가 된다.
        /// </summary>
        public static float RotationOf(BoxEdge e) => e switch
        {
            BoxEdge.Bottom => 0f,
            BoxEdge.Right  => 90f,
            BoxEdge.Top    => 180f,
            _              => -90f,
        };

        /// <summary>벽의 시작점과 진행 방향. 네 벽을 순서대로 이으면 끊김 없이 한 바퀴가 된다.</summary>
        public static void EdgeSegment(ScreenRect box, BoxEdge e, out Vector2 start, out Vector2 dir, out float length)
        {
            switch (e)
            {
                case BoxEdge.Bottom:
                    start = new Vector2(box.Left, box.Bottom); dir = new Vector2(1, 0); length = box.Width; break;
                case BoxEdge.Right:
                    start = new Vector2(box.Right, box.Bottom); dir = new Vector2(0, -1); length = box.Height; break;
                case BoxEdge.Top:
                    start = new Vector2(box.Right, box.Top); dir = new Vector2(-1, 0); length = box.Width; break;
                default:
                    start = new Vector2(box.Left, box.Top); dir = new Vector2(0, 1); length = box.Height; break;
            }
        }

        public static BoxEdge Next(BoxEdge e) => (BoxEdge)(((int)e + 1) & 3);
        public static BoxEdge Prev(BoxEdge e) => (BoxEdge)(((int)e + 3) & 3);

        /// <summary>
        /// 벽 위에서 몸통이 차지하는 구간. 좌우 비대칭을 그대로 쓰면 진행 방향이 바뀔 때마다
        /// 여백이 달라져 둘레 좌표가 방향에 의존하게 된다. 큰 쪽으로 대칭을 맞춘다.
        /// 한쪽이 10px 남짓 일찍 멈추는 대신 위치 계산이 방향과 무관해진다.
        /// </summary>
        public static float HalfExtent(float alongMin, float alongMax) =>
            Mathf.Max(Mathf.Abs(alongMin), Mathf.Abs(alongMax));

        /// <summary>벽 위에서 몸이 들어갈 수 있는 구간 [min, max] (벽 시작점으로부터의 거리).</summary>
        public static void UsableSpan(ScreenRect box, BoxEdge e, float halfExtent,
                                      out float min, out float max)
        {
            EdgeSegment(box, e, out _, out _, out float length);
            min = halfExtent;
            max = length - halfExtent;
            if (max < min) min = max = length * 0.5f;   // 벽이 몸보다 짧으면 가운데
        }

        /// <summary>
        /// 앵커를 실제 자세로 바꾼다.
        ///
        /// 회전하면 스프라이트의 로컬 +x 축이 항상 벽의 진행 방향과 일치한다(네 벽 모두).
        /// 달팽이 아트는 로컬 -x 를 보고 있으므로, 진행 방향으로 걸으려면 좌우를 뒤집어야 한다.
        /// </summary>
        /// <param name="footDepth">루트에서 발까지의 거리(px). SnailBounds.Foot 의 절대값에 스케일을 곱한 값.</param>
        /// <param name="halfExtent">진행축 방향 몸통 반폭(px). <see cref="HalfExtent"/> 로 구한다.</param>
        public static SnailPose Evaluate(ScreenRect box, BoxAnchor anchor, float footDepth, float halfExtent)
        {
            var pose = new SnailPose();
            if (box.Width <= 0 || box.Height <= 0) return pose;

            if (anchor.Turn > 0f) return EvaluateTurn(box, anchor, footDepth, halfExtent);

            EdgeSegment(box, anchor.Edge, out var start, out var dir, out _);
            UsableSpan(box, anchor.Edge, halfExtent, out float sMin, out float sMax);

            float s = Mathf.Lerp(sMin, sMax, Mathf.Clamp01(anchor.T));

            Vector2 foot = start + dir * s;                       // 발이 벽에 닿는 지점
            Vector2 n = OutwardNormal(anchor.Edge);

            pose.RootScreen  = foot - n * footDepth;              // 몸은 박스 안쪽으로
            pose.RotationDeg = RotationOf(anchor.Edge);
            pose.FlipX       = anchor.Forward;                    // 아트가 왼쪽을 보므로 진행 방향이면 뒤집는다
            pose.Valid       = true;
            return pose;
        }

        // ── 모서리 돌기 ──
        //
        // 벽이 바뀌는 순간 회전을 90도 갈아끼우면 순간이동으로 보인다.
        // 대신 발바닥을 하나의 선분으로 보고 <b>모서리 점을 축으로 굴린다</b>.
        // 앞발 끝이 다음 벽에 닿는 순간부터 뒷발 끝이 모서리를 넘을 때까지,
        // 접점이 발바닥을 따라 앞에서 뒤로 미끄러지며 몸이 90도 돌아간다.
        //
        // 시작·끝이 각각 이전 벽의 sMax, 다음 벽의 sMin 과 정확히 일치하므로
        // 평지 이동과 이어 붙여도 끊기지 않는다.

        /// <summary>모서리 하나를 도는 데 드는 거리 = 이 값 × halfExtent.</summary>
        public const float TurnLengthFactor = 1.0f;

        /// <summary>
        /// 도는 동안 발바닥을 오므리는 정도. 1 이면 오므리지 않는다.
        ///
        /// 곧은 발바닥은 오목한 모서리에 닿을 수 없어 양 끝이 벽 밖으로 뜬다. 지금은
        /// SnailDeform 이 발바닥을 실제로 접어 두 벽에 눕히므로 오므릴 이유가 없어졌다.
        /// 몸통 두께가 모서리 밖으로 휩쓸리는 것만 남는데, 그건 오므려도 안 없어진다.
        /// </summary>
        public const float TurnFootShrink = 1f;

        /// <summary>화면 좌표계(y 아래로 증가)에서의 회전.</summary>
        public static Vector2 RotateScreen(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// <summary>
        /// 지금 이 순간의 바깥 법선. 모서리를 도는 중이면 그만큼 돌아간 값이다.
        /// 벽 기준 <see cref="OutwardNormal(BoxEdge)"/> 를 그냥 쓰면 도는 동안 방향이 어긋난다.
        /// </summary>
        public static Vector2 OutwardNormal(BoxAnchor anchor) =>
            anchor.Turn <= 0f ? OutwardNormal(anchor.Edge)
                              : RotateScreen(OutwardNormal(anchor.Edge), TurnScreenDeg(anchor));

        /// <summary>지금 이 순간의 진행 방향(벽 접선). 모서리를 도는 중이면 그만큼 돌아간 값이다.</summary>
        public static Vector2 Tangent(ScreenRect box, BoxAnchor anchor)
        {
            EdgeSegment(box, anchor.Edge, out _, out var dir, out _);
            return anchor.Turn <= 0f ? dir : RotateScreen(dir, TurnScreenDeg(anchor));
        }

        private static float TurnScreenDeg(BoxAnchor a) =>
            -90f * (a.TurnToNext ? 1f : -1f) * Mathf.Clamp01(a.Turn);

        /// <summary>도는 동안의 유효 발 반폭. 양 끝(0, 1)에서는 원래 값과 같아 이어 붙는다.</summary>
        public static float TurnHalfExtent(float halfExtent, float turn) =>
            halfExtent * Mathf.Lerp(1f, TurnFootShrink, Mathf.Sin(Mathf.Clamp01(turn) * Mathf.PI));

        private static SnailPose EvaluateTurn(ScreenRect box, BoxAnchor anchor, float footDepth, float halfExtent)
        {
            EdgeSegment(box, anchor.Edge, out var start, out var dir, out float length);
            Vector2 n = OutwardNormal(anchor.Edge);

            float tau = Mathf.Clamp01(anchor.Turn);
            float sign = anchor.TurnToNext ? 1f : -1f;
            Vector2 corner = anchor.TurnToNext ? start + dir * length : start;

            // 월드 회전은 +90*sign, 화면 벡터는 그 반대로 돈다 (월드 y 는 위, 화면 y 는 아래)
            float scr = TurnScreenDeg(anchor);
            Vector2 tangent = RotateScreen(dir, scr);
            Vector2 normal  = RotateScreen(n,   scr);

            // 접점이 앞발 끝 → 뒷발 끝으로 옮겨간다
            float half = TurnHalfExtent(halfExtent, tau);
            float a = (anchor.TurnToNext ? half : -half) * (1f - 2f * tau);

            var pose = new SnailPose();
            pose.RootScreen  = corner - tangent * a - normal * footDepth;
            pose.RotationDeg = RotationOf(anchor.Edge) + 90f * sign * tau;
            pose.FlipX       = anchor.Forward;
            pose.Valid       = true;
            return pose;
        }

        /// <summary>
        /// 벽을 따라 이동시키고, 끝에 닿으면 모서리를 돌아 이웃 벽으로 넘긴다.
        /// 벽마다 길이가 달라 T 를 그대로 쓰면 짧은 벽에서 빨라지므로, 속도는 px/초로 받는다.
        /// </summary>
        public static BoxAnchor Advance(ScreenRect box, BoxAnchor anchor,
                                        float pixelsPerSecond, float deltaTime, float halfExtent)
        {
            float step = pixelsPerSecond * deltaTime;

            if (anchor.Turn > 0f)
            {
                float turnLen = Mathf.Max(1f, halfExtent * TurnLengthFactor);
                // 돌던 도중에 방향을 틀면(먹이를 발견 등) 돌던 걸 되감아 원래 벽으로 돌아간다
                bool onward = anchor.Forward == anchor.TurnToNext;
                anchor.Turn += (onward ? step : -step) / turnLen;

                if (anchor.Turn >= 1f)
                {
                    anchor.Edge = anchor.TurnToNext ? Next(anchor.Edge) : Prev(anchor.Edge);
                    anchor.T = anchor.TurnToNext ? 0f : 1f;
                    anchor.Turn = 0f;
                }
                else if (anchor.Turn <= 0f)
                {
                    anchor.T = anchor.TurnToNext ? 1f : 0f;
                    anchor.Turn = 0f;
                }
                return anchor;
            }

            UsableSpan(box, anchor.Edge, halfExtent, out float sMin, out float sMax);
            float usable = Mathf.Max(1f, sMax - sMin);
            anchor.T += (anchor.Forward ? step : -step) / usable;

            if (anchor.T > 1f)      { anchor.T = 1f; anchor.Turn = 1e-4f; anchor.TurnToNext = true; }
            else if (anchor.T < 0f) { anchor.T = 0f; anchor.Turn = 1e-4f; anchor.TurnToNext = false; }
            return anchor;
        }

        // ── 둘레 좌표 ──
        // 네 벽을 한 줄로 편 1차원 좌표. 달팽이와 먹이를 같은 축에 놓으면
        // "어느 쪽으로 돌아야 가까운가" 가 뺄셈 한 번으로 끝난다.

        public static float Perimeter(ScreenRect box) => 2f * (box.Width + box.Height);

        /// <summary>해당 벽이 둘레 좌표에서 시작하는 지점.</summary>
        public static float EdgeStart(ScreenRect box, BoxEdge e) => e switch
        {
            BoxEdge.Bottom => 0f,
            BoxEdge.Right  => box.Width,
            BoxEdge.Top    => box.Width + box.Height,
            _              => 2f * box.Width + box.Height,
        };

        public static float ToPerimeter(ScreenRect box, BoxAnchor anchor, float halfExtent)
        {
            UsableSpan(box, anchor.Edge, halfExtent, out float sMin, out float sMax);
            float here = EdgeStart(box, anchor.Edge) + Mathf.Lerp(sMin, sMax, Mathf.Clamp01(anchor.T));
            if (anchor.Turn <= 0f) return here;

            // 도는 동안에도 둘레 좌표는 이어져야 한다. 안 그러면 먹이까지의 방향 판단이 모서리에서 뒤집힌다.
            BoxEdge to = anchor.TurnToNext ? Next(anchor.Edge) : Prev(anchor.Edge);
            UsableSpan(box, to, halfExtent, out float tMin, out float tMax);
            float there = EdgeStart(box, to) + (anchor.TurnToNext ? tMin : tMax);

            float p = here + ShortestDelta(box, here, there) * Mathf.Clamp01(anchor.Turn);
            return Mathf.Repeat(p, Perimeter(box));
        }

        /// <summary>바닥에 놓인 것(먹이 등)의 화면 x 를 둘레 좌표로.</summary>
        public static float BottomXToPerimeter(ScreenRect box, float screenX) =>
            Mathf.Clamp(screenX - box.Left, 0f, Mathf.Max(0f, box.Width));

        /// <summary>둘레 좌표를 앵커로. 진행 방향은 호출한 쪽이 정한다.</summary>
        public static BoxAnchor FromPerimeter(ScreenRect box, float p, bool forward, float halfExtent)
        {
            float total = Perimeter(box);
            if (total <= 0f) return new BoxAnchor { Edge = BoxEdge.Bottom, T = 0f, Forward = forward };

            p = Mathf.Repeat(p, total);

            BoxEdge edge = BoxEdge.Left;
            if (p < box.Width)                          edge = BoxEdge.Bottom;
            else if (p < box.Width + box.Height)        edge = BoxEdge.Right;
            else if (p < 2f * box.Width + box.Height)   edge = BoxEdge.Top;

            UsableSpan(box, edge, halfExtent, out float sMin, out float sMax);
            float s = p - EdgeStart(box, edge);
            float t = (sMax > sMin) ? Mathf.Clamp01((s - sMin) / (sMax - sMin)) : 0.5f;
            return new BoxAnchor { Edge = edge, T = t, Forward = forward };
        }

        /// <summary>
        /// from 에서 to 로 가는 가장 짧은 방향과 거리.
        /// 양수면 진행 방향(Forward), 음수면 반대 방향이 가깝다.
        /// </summary>
        public static float ShortestDelta(ScreenRect box, float from, float to)
        {
            float total = Perimeter(box);
            if (total <= 0f) return 0f;
            float d = Mathf.Repeat(to - from, total);
            return (d <= total * 0.5f) ? d : d - total;
        }
    }
}
