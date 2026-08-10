using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 달팽이 한 마리의 변형 상태 전부. 파츠마다 따로 계산하지 않고
    /// 이 하나가 「로컬 좌표 → 변형된 로컬 좌표」 함수 노릇을 한다.
    ///
    /// 모든 파츠가 같은 캔버스를 공유하므로 이 함수 하나면 몸통·눈·더듬이가
    /// 저절로 같이 움직인다. 껍질처럼 안 휘어야 하는 것은 이 함수를 한 점에서만
    /// 평가해 강체로 따라가게 한다(<see cref="RigidPose"/>).
    ///
    /// 좌표계: 로컬 x = 진행 방향, 로컬 y = 벽에서 멀어지는 방향, 발바닥은 y = Foot.
    /// </summary>
    public sealed class SnailDeform
    {
        /// <summary>발바닥의 로컬 y. 모든 변형의 피벗이다.</summary>
        public float Foot;

        /// <summary>몸 전체 세로 신장. 0 = 평소.</summary>
        public float Stretch;

        /// <summary>몸 전체 기울기(도). 발을 축으로 돈다.</summary>
        public float LeanDeg;

        /// <summary>루트가 좌우 반전됐는가. 반전되면 자식 회전이 거울로 보여 각을 되돌려야 한다.</summary>
        public bool Mirrored;

        // ── 발바닥 ──
        // 위 세 값이 몸 <b>전체</b>를 다룬다면, 아래는 발바닥 근처만 다룬다.
        // 높이 가중치 w(h) 로 섞이므로 몸통 위쪽은 영향을 받지 않는다.

        /// <summary>이 높이(로컬)까지가 발바닥. 위로 갈수록 발 변형이 사라진다.</summary>
        public float FootBand = 1f;

        /// <summary>기어갈 때 발바닥을 지나가는 물결. 이동 거리로 도는 위상.</summary>
        public float WavePhase;
        public float WaveAmplitude;
        public float WaveLength = 1f;

        /// <summary>발바닥을 따라 물결이 진행하는 방향. 진행 방향과 맞춰 준다.</summary>
        public float WaveDirection = 1f;

        /// <summary>
        /// 발바닥 높이 가중치. 발바닥에서 1, FootBand 위로는 0.
        /// 부드럽게 떨어져야 발과 몸통 사이에 접힌 자국이 안 생긴다.
        /// </summary>
        public float FootWeight(float localY)
        {
            float h = (localY - Foot) / Mathf.Max(0.0001f, FootBand);
            if (h <= 0f) return 1f;
            if (h >= 1f) return 0f;
            return 1f - h * h * (3f - 2f * h);      // smoothstep
        }

        /// <summary>로컬 좌표 하나를 변형한다.</summary>
        public Vector2 Apply(Vector2 p)
        {
            float sy = 1f + Stretch;

            // 부피 보존. 세로로 늘면 가로가 그만큼 홀쭉해진다.
            float sx = 1f / Mathf.Sqrt(Mathf.Max(0.2f, sy));

            float x = p.x * sx;
            float y = (p.y - Foot) * sy;

            // 발바닥 물결. 벽에 붙은 채로 살짝 부풀었다 가라앉는 근육 파동이라
            // 음수(벽 안쪽)로는 안 가게 0..1 로 만든다.
            if (WaveAmplitude > 0f)
            {
                float w = FootWeight(p.y);
                if (w > 0f)
                {
                    float t = p.x / Mathf.Max(1f, WaveLength) - WavePhase * WaveDirection;
                    float bump = Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f;
                    y += bump * WaveAmplitude * w;
                }
            }

            float deg = Mirrored ? -LeanDeg : LeanDeg;
            if (deg != 0f)
            {
                float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
                return new Vector2(x * c - y * s, x * s + y * c + Foot);
            }
            return new Vector2(x, y + Foot);
        }

        /// <summary>
        /// 안 휘는 파츠(껍질 등)가 따라갈 자세.
        /// 기준점 하나를 변형한 자리로 옮기고 몸과 같은 각으로 돌기만 한다.
        /// </summary>
        public void RigidPose(float anchorLocalY, out Vector3 position, out Quaternion rotation)
        {
            float deg = Mirrored ? -LeanDeg : LeanDeg;
            rotation = Quaternion.Euler(0f, 0f, deg);

            var anchor = new Vector3(0f, anchorLocalY, 0f);
            Vector2 moved = Apply(new Vector2(0f, anchorLocalY));
            position = new Vector3(moved.x, moved.y, 0f) - rotation * anchor;
        }
    }
}
