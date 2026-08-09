#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
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
        /// 앵커를 실제 자세로 바꾼다.
        ///
        /// 회전하면 스프라이트의 로컬 +x 축이 항상 벽의 진행 방향과 일치한다(네 벽 모두).
        /// 달팽이 아트는 로컬 -x 를 보고 있으므로, 진행 방향으로 걸으려면 좌우를 뒤집어야 한다.
        /// </summary>
        /// <param name="footDepth">루트에서 발까지의 거리(px). SnailBounds.Foot 의 절대값에 스케일을 곱한 값.</param>
        /// <param name="alongMin">진행축 방향 몸통 최소 오프셋(px). SnailBounds.Left * scale.</param>
        /// <param name="alongMax">진행축 방향 몸통 최대 오프셋(px). SnailBounds.Right * scale.</param>
        public static SnailPose Evaluate(ScreenRect box, BoxAnchor anchor,
                                         float footDepth, float alongMin, float alongMax)
        {
            var pose = new SnailPose();
            if (box.Width <= 0 || box.Height <= 0) return pose;

            EdgeSegment(box, anchor.Edge, out var start, out var dir, out float length);

            // 진행 방향으로 걸으려면 뒤집어야 한다. 뒤집으면 몸통의 진행축 범위도 좌우가 바뀐다.
            bool flip = anchor.Forward;
            float lo = flip ? -alongMax : alongMin;
            float hi = flip ? -alongMin : alongMax;

            // 몸 전체가 벽 안에 들어오는 구간. 벽이 몸보다 짧으면 가운데에 둔다.
            float sMin = -lo;
            float sMax = length - hi;
            float s = (sMax >= sMin) ? Mathf.Lerp(sMin, sMax, Mathf.Clamp01(anchor.T))
                                     : (length - (hi + lo)) * 0.5f;

            Vector2 foot = start + dir * s;                       // 발이 벽에 닿는 지점
            Vector2 n = OutwardNormal(anchor.Edge);

            pose.RootScreen  = foot - n * footDepth;              // 몸은 박스 안쪽으로
            pose.RotationDeg = RotationOf(anchor.Edge);
            pose.FlipX       = flip;
            pose.Valid       = true;
            return pose;
        }

        /// <summary>
        /// 벽을 따라 이동시키고, 끝에 닿으면 이웃 벽으로 넘긴다.
        /// 벽마다 길이가 달라 T 를 그대로 쓰면 짧은 벽에서 빨라지므로, 속도는 px/초로 받는다.
        /// </summary>
        public static BoxAnchor Advance(ScreenRect box, BoxAnchor anchor,
                                        float pixelsPerSecond, float deltaTime,
                                        float alongMin, float alongMax)
        {
            EdgeSegment(box, anchor.Edge, out _, out _, out float length);

            bool flip = anchor.Forward;
            float lo = flip ? -alongMax : alongMin;
            float hi = flip ? -alongMin : alongMax;
            float usable = Mathf.Max(1f, (length - hi) - (-lo));   // 실제로 걸을 수 있는 길이

            float dt = pixelsPerSecond * deltaTime / usable;
            anchor.T += anchor.Forward ? dt : -dt;

            while (anchor.T > 1f)
            {
                anchor.T -= 1f;
                anchor.Edge = Next(anchor.Edge);
            }
            while (anchor.T < 0f)
            {
                anchor.T += 1f;
                anchor.Edge = Prev(anchor.Edge);
            }
            return anchor;
        }
    }
}
#endif
