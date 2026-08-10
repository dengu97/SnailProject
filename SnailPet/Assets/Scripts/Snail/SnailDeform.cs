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

        // ── 모서리 ──
        // 발바닥을 모서리에서 접어 두 벽에 나눠 붙인다. 접힘은 발바닥에서만 100% 이고
        // 위로 갈수록 사라지므로, 몸통은 접히지 않고 통째로 도는 것으로 보인다.
        //
        // 각도는 부호를 손으로 따지지 않는다. 부르는 쪽이 벽 방향을 로컬 좌표로
        // 역변환해 넣어 주므로 벽·진행 방향·좌우 반전이 저절로 맞는다.

        public bool Cornering;

        /// <summary>모서리의 로컬 x. 발바닥 위의 한 점이다.</summary>
        public float CornerX;

        /// <summary>+1 이면 x > CornerX 쪽이 모서리 '너머'.</summary>
        public float CornerFarSign = 1f;

        /// <summary>모서리 이쪽(지나온 벽) 발바닥을 돌릴 각.</summary>
        public float CornerNearDeg;

        /// <summary>모서리 너머(갈 벽) 발바닥을 돌릴 각.</summary>
        public float CornerFarDeg;

        /// <summary>
        /// 꺾임이 x 방향으로 퍼지는 폭.
        ///
        /// 모서리에서 칼로 자르듯 각을 바꾸면 발바닥 한 조각만 뾰족하게 튀어나온다.
        /// 이 폭에 걸쳐 각을 부드럽게 옮겨야 몸이 「휘어 돌아가는」 것으로 보인다.
        /// </summary>
        public float CornerSpanX = 1f;

        // ── 들렸을 때 ──
        // 벽에서 떨어진 발은 붙잡을 곳이 없어 축 늘어지고 좌우로 흔들린다.

        /// <summary>아래로 처지는 양(로컬).</summary>
        public float DangleDepth;

        /// <summary>좌우로 쏠리는 양(로컬). 손을 움직이면 뒤늦게 따라온다.</summary>
        public float DangleSway;

        /// <summary>몸통 반폭(로컬). 발 가장자리가 가운데보다 더 처지게 하는 데 쓴다.</summary>
        public float HalfWidth = 1f;

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

        /// <summary>
        /// 로컬 좌표 하나를 변형한다.
        ///
        /// 순서: 모서리 접기 → 물결 → 늘어짐 → 몸 전체 신장·기울기.
        /// 앞의 셋은 발바닥만 건드리고, 마지막 하나가 몸 전체를 다룬다.
        /// 가중치는 항상 <b>원래</b> 높이로 구해서, 앞 단계가 점을 옮겨도 흔들리지 않는다.
        /// </summary>
        public Vector2 Apply(Vector2 p)
        {
            float w = FootWeight(p.y);
            float x = p.x, y = p.y;

            // 1) 모서리에서 접기. 모서리 점을 축으로 양쪽을 각자의 벽에 눕힌다.
            //    각이 양쪽에서 다르므로 모서리에 정확히 꺾인 자국이 생긴다. 그게 맞다.
            if (Cornering && w > 0f)
            {
                // 모서리를 넘어가며 각이 near → far 로 부드럽게 옮겨간다.
                // 같은 축을 도므로 축에서의 거리는 보존되고, 결과는 발바닥이 모서리를
                // 감싸며 휘는 모양이 된다.
                float u = (x - CornerX) * CornerFarSign / Mathf.Max(1f, CornerSpanX) * 0.5f + 0.5f;
                u = Mathf.Clamp01(u);
                u = u * u * (3f - 2f * u);

                float deg = Mathf.Lerp(CornerNearDeg, CornerFarDeg, u) * w;
                if (deg != 0f)
                {
                    float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
                    float dx = x - CornerX, dy = y - Foot;
                    x = CornerX + dx * c - dy * s;
                    y = Foot    + dx * s + dy * c;
                }
            }

            // 2) 기어갈 때의 근육 파동. 벽 안쪽으로 파고들지 않게 0..1 로만 부푼다.
            //    가중치를 세제곱해 접힘보다 훨씬 얇은 띠에만 남긴다. 물결은 발바닥 표면의
            //    일이라 몸통 절반이 같이 출렁이면 안 된다.
            if (WaveAmplitude > 0f && w > 0f)
            {
                float t = p.x / Mathf.Max(1f, WaveLength) - WavePhase * WaveDirection;
                y += (Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f) * WaveAmplitude * (w * w * w);
            }

            // 3) 들렸을 때. 가장자리가 가운데보다 더 처져야 늘어진 것으로 보인다.
            if (w > 0f && (DangleDepth != 0f || DangleSway != 0f))
            {
                float edge = Mathf.Clamp01(Mathf.Abs(p.x) / Mathf.Max(1f, HalfWidth));
                y -= DangleDepth * w * (0.35f + 0.65f * edge * edge);
                x += DangleSway * w;
            }

            // 4) 몸 전체. 부피 보존이라 세로로 늘면 가로가 그만큼 홀쭉해진다.
            float sy = 1f + Stretch;
            float sx = 1f / Mathf.Sqrt(Mathf.Max(0.2f, sy));

            float ax = x * sx;
            float ay = (y - Foot) * sy;

            float lean = Mirrored ? -LeanDeg : LeanDeg;
            if (lean != 0f)
            {
                float r = lean * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
                return new Vector2(ax * c - ay * s, ax * s + ay * c + Foot);
            }
            return new Vector2(ax, ay + Foot);
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
