using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 기어갈 때의 흐물거림을 정하는 값들.
    ///
    /// 내 달팽이와 손님이 <b>같은 값</b>을 써야 한 화면에서 따로 놀지 않는다.
    /// 예전에는 부트스트랩에만 있어서 손님은 아예 뻣뻣했다(2026-08-25).
    /// </summary>
    public static class Wobble
    {
        /// <summary>이만큼 나아갈 때마다 한 번 출렁인다(화면 px).</summary>
        public const float Wavelength = 46f;

        public const float Stretch = 0.045f;
        public const float LeanDeg = 2.2f;

        /// <summary>이 속도(px/s)에서 흔들림이 최대가 된다. 느리면 그만큼 얕다.</summary>
        public const float FullSpeed = 120f;

        // ── 발바닥 물결 ──
        public const float FootBandFraction = 0.35f;
        public const float WaveLengthFraction = 0.30f;
        public const float WaveAmplitudeFraction = 0.045f;

        /// <summary>발바닥 선을 몇 점으로 재는가.</summary>
        public const int SoleSamples = 48;

        /// <summary>걸음에 맞춘 끄덕임의 세기.</summary>
        public const float BobAmount = 0.05f;

        // ── 벽에서 떼기 ──
        //
        // 몸이 목표를 곧장 따라가지 않고 스프링으로 따라붙는다. 그래서 당기면 저항이 생기고,
        // 놓으면 지나쳤다 돌아오며 출렁인다 — 「쫀득함」은 전부 여기서 나온다.

        public const float SpringStiffness = 320f;
        public const float SpringDamping = 12f;

        /// <summary>이만큼 당겨야 떨어진다(화면 px).</summary>
        public const float PeelThreshold = 72f;

        public const float PeelMaxStretch = 0.35f;
        public const float PeelMaxLeanDeg = 18f;

        /// <summary>떨어지는 순간 되튕기는 양.</summary>
        public const float PopRecoil = -0.22f;

        /// <summary>스프링 한 걸음. 내 달팽이와 손님이 같은 것을 쓴다.</summary>
        public static void Spring(ref float value, ref float velocity, float target, float dt)
        {
            velocity += (target - value) * SpringStiffness * dt;
            velocity *= Mathf.Exp(-SpringDamping * dt);
            value += velocity * dt;
        }
    }

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

        /// <summary>
        /// 껍질 속으로 빨려 들어가는 정도. 0 = 평소, 1 = 한 점으로 사라짐.
        ///
        /// 늘어남(<see cref="Stretch"/>)은 부피를 지키느라 세로로 줄면 가로로 퍼지는데,
        /// 「껍질 속으로 쏙」은 그 반대로 <b>가로·세로가 함께</b> 줄어야 한다. 그래서 축을 따로 뒀다.
        /// 목표점을 껍질 한가운데로 두면 몸이 껍질 안으로 빨려 들어가는 것으로 보인다.
        /// </summary>
        public float Retract;

        /// <summary>빨려 들어가는 목표점(로컬).</summary>
        public Vector2 RetractTo;

        /// <summary>몸 전체 기울기(도). 발을 축으로 돈다.</summary>
        public float LeanDeg;

        /// <summary>루트가 좌우 반전됐는가. 반전되면 자식 회전이 거울로 보여 각을 되돌려야 한다.</summary>
        public bool Mirrored;

        // ── 발바닥 ──
        // 위 세 값이 몸 <b>전체</b>를 다룬다면, 아래는 발바닥 근처만 다룬다.
        // 높이 가중치 w(h) 로 섞이므로 몸통 위쪽은 영향을 받지 않는다.

        /// <summary>이 높이(로컬)까지가 발바닥. 위로 갈수록 발 변형이 사라진다.</summary>
        public float FootBand = 1f;

        // ── 발바닥 선 ──
        // 「발바닥에서의 높이」를 최하단 한 점에서 재면, 머리 쪽이 들려 있는 몸통에서는
        // 그 부분만 실제보다 높게 나와 변형이 약해진다. 실측한 발바닥 선을 기준으로 삼는다.
        // 비워 두면 Foot 한 줄을 쓴다 — 예전 동작과 같다.

        private float[] _sole;
        private float _soleMinX, _soleMaxX;

        public void SetSole(float[] profile, float minX, float maxX)
        {
            _sole = (profile != null && profile.Length >= 2 && maxX > minX) ? profile : null;
            _soleMinX = minX; _soleMaxX = maxX;
        }

        public bool HasSole => _sole != null;

        /// <summary>그 x 에서의 발바닥 높이. 샘플 사이는 선형 보간한다.</summary>
        public float SoleY(float x)
        {
            if (_sole == null) return Foot;

            float u = (x - _soleMinX) / (_soleMaxX - _soleMinX) * (_sole.Length - 1);
            if (u <= 0f) return _sole[0];
            if (u >= _sole.Length - 1) return _sole[_sole.Length - 1];

            int i = (int)u;
            return Mathf.Lerp(_sole[i], _sole[i + 1], u - i);
        }

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
        /// 발바닥 높이 가중치. 그 x 의 발바닥에서 1, FootBand 위로는 0.
        /// 부드럽게 떨어져야 발과 몸통 사이에 접힌 자국이 안 생긴다.
        /// </summary>
        public float FootWeight(float localX, float localY)
        {
            float h = (localY - SoleY(localX)) / Mathf.Max(0.0001f, FootBand);
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
            float w = FootWeight(p.x, p.y);
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

            Vector2 result;
            if (lean != 0f)
            {
                float r = lean * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
                result = new Vector2(ax * c - ay * s, ax * s + ay * c + Foot);
            }
            else result = new Vector2(ax, ay + Foot);

            // 5) 껍질 속으로. 마지막에 목표점 쪽으로 통째로 오므린다.
            if (Retract > 0f)
            {
                float k = 1f - Mathf.Clamp01(Retract);
                result = RetractTo + (result - RetractTo) * k;
            }
            return result;
        }

        /// <summary>
        /// 안 휘는 파츠(껍질 등)가 따라갈 자세. 껍질은 절대 변형되지 않고,
        /// <b>붙어 있는 자리의 변형만</b> 강체로 물려받는다.
        ///
        /// 각도를 LeanDeg 로 직접 쓰지 않고 그 지점의 접선을 재서 구한다.
        /// 그래야 기울기든 모서리 접힘이든 나중에 무엇이 더 붙든 껍질이 저절로 따라간다.
        /// (예전에는 LeanDeg 만 썼고, 그래서 모서리에서 몸이 휘면 껍질만 남아 벌어졌다.)
        /// </summary>
        public void RigidPose(float anchorLocalY, out Vector3 position, out Quaternion rotation)
        {
            // 껍질은 빨려 들어가지 않는다 — 몸이 들어갈 목적지이지 같이 사라질 것이 아니다.
            // 오므림을 켠 채로 재면 껍질까지 목표점으로 끌려가고 접선이 사라져 각도가 튄다.
            float retract = Retract;
            Retract = 0f;

            var anchor = new Vector2(0f, anchorLocalY);
            Vector2 a = Apply(anchor);

            float probe = Mathf.Max(1f, FootBand * 0.25f);
            Vector2 t = Apply(anchor + new Vector2(probe, 0f)) - a;

            float deg = t.sqrMagnitude > 1e-8f ? Mathf.Atan2(t.y, t.x) * Mathf.Rad2Deg : 0f;
            rotation = Quaternion.Euler(0f, 0f, deg);
            position = new Vector3(a.x, a.y, 0f) - rotation * new Vector3(anchor.x, anchor.y, 0f);

            Retract = retract;
        }
    }
}
