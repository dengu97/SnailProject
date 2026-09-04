using System.Collections.Generic;
using SnailPet.Desktop;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 같은 방에 있는 <b>남의 달팽이</b>들. 화면 벽을 같이 기어다닌다.
    ///
    /// 위치는 맞추지 않는다(비동기). 받은 것은 「어떻게 생겼는가」뿐이고, 어디를 걷는지는
    /// 각자 자기 화면에서 알아서 정한다 — 같이 있다는 느낌만 주면 된다는 결정(2026-08-18)에
    /// 따른 것이고, 덕분에 오갈 것이 문자열 하나로 끝난다.
    ///
    /// 내 달팽이와 달리 먹지도, 들리지도, 늘어나지도 않는다. 벽을 따라 걷기만 한다 —
    /// 그래서 본체(SnailPetBootstrap)의 상태 기계를 건드리지 않고 여기서 따로 돈다.
    ///
    /// 자리는 <b>화면 좌표</b>로 들고 있고 월드로 옮기는 것은 부르는 쪽이 한다. 부스러기·똥과
    /// 같은 규칙이다 — 화면↔월드 환산이 한 곳에만 있어야 한다.
    /// </summary>
    public sealed class GuestField
    {
        public sealed class Guest
        {
            public string Name;
            public string Look;          // 받은 글자. 이게 바뀌면 다시 세운다

            /// <summary>달팽이 이름. 머리 위 이름표에 그대로 나간다(사람 이름이 아니다).</summary>
            public string SnailName;

            /// <summary>머리 위 이름표. 손님이 나가면 같이 사라진다.</summary>
            public SnailNameTag Tag;

            /// <summary>세워 둔 모습. 교배 짝을 고를 때 이걸 그대로 쓴다.</summary>
            public SnailAppearance Appearance;
            public Transform Root;
            public BoxAnchor Anchor;
            public SnailBounds Bounds;
            public float Scale;

            /// <summary>이 손님이 걷는 속도(px/s). 제 레벨의 LevelData 에서 나온다.</summary>
            public float Speed;

            /// <summary>이 손님의 파츠에 딸린 이펙트. 손님이 나가면 같이 치운다.</summary>
            public List<SparkField.Attached> Effects;
            public Vector2 Screen;       // 발이 놓인 화면 좌표
            public float RotationDeg;
            public bool Flip;

            /// <summary>손에 들려 있다. 그동안은 걷지 않고 손을 따라간다.</summary>
            public bool Held;

            /// <summary>놓아서 떨어지는 중. 바닥에 닿으면 아래 벽에 붙는다.</summary>
            public bool Falling;

            /// <summary>들리거나 떨어지는 동안의 발 좌표(화면 px). 벽에서 떨어져 있으므로 앵커 대신 이것이 자리를 정한다.</summary>
            public Vector2 FootScreen;

            /// <summary>떨어지는 속도(px/s).</summary>
            public float VelY;

            public bool Free => Held || Falling;

            // ── 흐물거림 ──
            //
            // 내 달팽이와 같은 부품이다. 합성 결과(Soft·Rigid)를 들고 있어야 매 프레임
            // 변형을 먹일 수 있어서, 세울 때 받은 것을 버리지 않고 그대로 둔다.

            public SnailComposer.Composed Composed;
            public SnailDeform Deform;

            /// <summary>껍질 한가운데의 로컬 y. 안 휘는 껍질을 이 점 기준으로 옮긴다.</summary>
            public float ShellCenterY;

            /// <summary>걸음 위상. 나아간 거리로 돈다.</summary>
            public float WalkPhase;

            /// <summary>
            /// 벽에서 떼는 중. 발은 붙어 있고 몸만 손을 따라 늘어난다.
            /// 다 늘어나면 떨어져 손에 들리고, 덜 당기고 놓으면 그대로 붙어 있는다.
            /// </summary>
            public bool Peeling;

            /// <summary>잡기 시작한 화면 좌표. 여기서 얼마나 멀어졌는지로 잰다.</summary>
            public Vector2 GrabScreen;

            // 스프링. 목표로 곧장 가지 않고 따라붙으므로 당길 때 저항이 생긴다.
            public float Stretch, StretchVel;
            public float Lean, LeanVel;

            /// <summary>떼는 중(또는 들린 중)의 목표. 걷는 동안에는 안 쓴다.</summary>
            public float PeelStretch, PeelLean;
        }

        private readonly Transform _parent;
        private readonly Dictionary<string, Guest> _guests = new Dictionary<string, Guest>();

        /// <summary>파츠에 딸린 이펙트를 붙일 곳. 내 달팽이와 같은 것을 쓴다.</summary>
        private readonly SparkField _sparks;

        /// <summary>이름표에 쓸 글꼴. UI 가 쓰는 것과 같은 것을 받는다.</summary>
        private readonly Font _font;

        public GuestField(Transform parent, SparkField sparks = null, Font font = null)
        {
            _parent = parent;
            _sparks = sparks;
            _font = font;
        }

        public IEnumerable<Guest> Items => _guests.Values;
        public int Count => _guests.Count;

        /// <summary>
        /// 방에 있는 사람들에 맞춘다. 새로 온 사람은 세우고, 나간 사람은 치우고,
        /// 모습이 바뀐 사람은 다시 세운다. <b>나는 빼고</b> 넘겨야 한다 — 내 달팽이는 본체가 그린다.
        /// </summary>
        /// <summary>
        /// 이펙트를 다시 붙인다. 설정에서 켜고 끌 때 부른다.
        ///
        /// 손님은 세워 둔 채로 이펙트만 갈아 끼운다 — <see cref="Sync"/> 는 외형이 그대로면
        /// 아무것도 안 하므로 설정만 바뀐 것은 그쪽으로는 반영되지 않고, 통째로 다시 세우면
        /// 걷던 자리와 자세가 처음으로 돌아가 눈에 띈다.
        /// </summary>
        public void RefreshEffects()
        {
            foreach (var g in _guests.Values)
            {
                SparkField.Detach(g.Effects);
                g.Effects = _sparks?.AttachTo(g.Appearance, g.Root);
            }
        }

        public void Sync((string name, string card)[] members)
        {
            var alive = new HashSet<string>();

            if (members != null)
                foreach (var (name, card) in members)
                {
                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(card)) continue;
                    alive.Add(name);

                    if (_guests.TryGetValue(name, out var had))
                    {
                        if (had.Look == card) continue;    // 그대로면 둔다
                        Destroy(had);
                        _guests.Remove(name);
                    }

                    var guest = Make(name, card);
                    if (guest != null) _guests[name] = guest;
                }

            var gone = new List<string>();
            foreach (var kv in _guests)
                if (!alive.Contains(kv.Key)) gone.Add(kv.Key);

            foreach (string name in gone)
            {
                Destroy(_guests[name]);
                _guests.Remove(name);
            }
        }

        private Guest Make(string name, string card)
        {
            // 받은 것은 「한 장」이라 머리말(이름·등급·레벨)이 붙어 있다. 외형만 떼어 읽으면
            // 첫 파츠가 머리말에 묻혀 사라진다 — 껍질 없는 달팽이가 걸어다니게 된다.
            var (snailName, _, level, appearance) = SnailShare.ReadCard(card);
            if (appearance == null) return null;

            var composed = SnailComposer.Build(appearance, "Guest_" + name);
            composed.Root.transform.SetParent(_parent, false);

            var bounds = SnailMetrics.Measure(appearance);
            float width = Mathf.Max(0.01f, bounds.Right - bounds.Left);

            // 크기·속도는 내 달팽이와 같은 규칙이다. 레벨만 알면 나머지는 데이터에서 나온다.
            var row = SnailGrowth.At(level);

            // 남의 달팽이도 제 파츠에 딸린 이펙트를 달고 다닌다.
            // 자리는 아래 Tick 에서 루트 자세가 정해진 뒤에 맞춘다.
            var effects = _sparks?.AttachTo(appearance, composed.Root.transform);

            // 흐물거림은 내 달팽이와 같은 부품으로 먹인다. 재는 것(발바닥 선·껍질 중심)은
            // 모습이 바뀌지 않으므로 세울 때 한 번만 한다.
            var deform = new SnailDeform { Foot = bounds.Foot };
            MeasureSole(appearance, deform);

            return new Guest
            {
                Effects = effects,
                Name = name,
                SnailName = snailName,

                // 이름표는 루트 밖에 세운다. 자리는 아래 Tick 뒤에 부르는 쪽이 맞춘다.
                Tag = new SnailNameTag(_parent, _font),

                Look = card,
                Appearance = appearance,
                Composed = composed,
                Deform = deform,
                ShellCenterY = ShellCenterOf(appearance, bounds),
                Root = composed.Root.transform,
                Bounds = bounds,
                Scale = (float)(row.Size * SnailGrowth.PixelsPerSizeUnit) / width,
                Speed = (float)(row.Speed * SnailGrowth.PixelsPerSpeedUnit),

                // 시작 자리는 아무 데나. 남과 겹쳐 서 있지 않게 벽과 진행도를 흩어 놓는다.
                Anchor = new BoxAnchor
                {
                    Edge = (BoxEdge)Random.Range(0, 4),
                    T = Random.value,
                    Forward = Random.value < 0.5f,
                },
            };
        }

        /// <summary>
        /// 벽을 따라 걷는다. 자리 계산은 내 달팽이와 같은 기하를 쓴다.
        /// 들려 있거나 떨어지는 손님은 걷지 않고 손·중력을 따른다.
        /// </summary>
        /// <param name="gravity">떨어질 때 붙는 가속(px/s²). 내 달팽이와 같은 값을 넘긴다.</param>
        public void Tick(float deltaSeconds, ScreenRect box, float pixelsPerWorld, float gravity)
        {
            foreach (var g in _guests.Values)
            {
                // 내 달팽이와 같은 값을 써야 같은 자리에 선다.
                //
                // 벽에 놓이는 것은 루트(몸 원점)이므로, 발까지의 깊이만큼 안쪽으로 들여야
                // 발바닥이 벽에 닿는다. 0 을 주면 원점이 벽에 놓여 발 아래가 통째로 화면 밖으로
                // 나간다 — 60px 짜리 달팽이면 20px 쯤이다.
                //
                // 벽 위에서 차지하는 폭도 원점 기준이다. 반폭(대칭)을 쓰면 원점이 한쪽으로
                // 치우친 만큼 벽 끝에서 삐져나온다.
                float halfExtent = BoxWalk.HalfExtent(g.Bounds.Left  * g.Scale * pixelsPerWorld,
                                                      g.Bounds.Right * g.Scale * pixelsPerWorld);
                float footDepth = Mathf.Abs(g.Bounds.Foot) * g.Scale * pixelsPerWorld;

                if (g.Free)
                {
                    Carry(g, deltaSeconds, box, halfExtent, footDepth, gravity);
                    StepWobble(g, deltaSeconds, pixelsPerWorld);   // 들려 있으면 물결이 잦아든다
                    continue;
                }

                g.Anchor = BoxWalk.Advance(box, g.Anchor, g.Speed, deltaSeconds, halfExtent);

                var pose = BoxWalk.Evaluate(box, g.Anchor, footDepth, halfExtent);
                if (!pose.Valid) continue;

                g.Screen = pose.RootScreen;
                g.RotationDeg = pose.RotationDeg;
                g.Flip = pose.FlipX;

                StepWobble(g, deltaSeconds, pixelsPerWorld);
            }
        }

        // ── 흐물거림 ──
        //
        // 내 달팽이와 같은 값(<see cref="Wobble"/>)으로 흔든다. 다른 점은 스프링을 안 거는 것뿐이다 —
        // 손님은 들리거나 먹지 않아 목표가 튀는 일이 없어서, 사인 값을 그대로 써도 같아 보인다.

        private static void StepWobble(Guest g, float deltaSeconds, float pixelsPerWorld)
        {
            if (g.Deform == null || g.Composed == null) return;

            var d = g.Deform;
            float width = Mathf.Max(0.01f, g.Bounds.Right - g.Bounds.Left);
            bool walking = !g.Free && !g.Peeling;

            // 걸음 위상은 나아간 거리로 돈다. 서 있거나 떼는 중이면 그대로 멈춘다.
            if (walking) g.WalkPhase += g.Speed * deltaSeconds / Wobble.Wavelength;

            float amp = walking ? Mathf.Clamp01(g.Speed / Wobble.FullSpeed) : 0f;
            float w = g.WalkPhase * Mathf.PI * 2f;

            d.Foot = g.Bounds.Foot;
            d.HalfWidth = width * 0.5f;
            d.Mirrored = g.Flip;

            // 발바닥 물결
            d.FootBand = (g.Bounds.Top - g.Bounds.Foot) * Wobble.FootBandFraction;
            d.WaveLength = width * Wobble.WaveLengthFraction;
            d.WaveDirection = g.Anchor.Forward ? 1f : -1f;

            float want = walking ? width * Wobble.WaveAmplitudeFraction : 0f;
            d.WaveAmplitude = Mathf.MoveTowards(d.WaveAmplitude, want, width * 0.12f * deltaSeconds);

            if (walking && d.WaveLength > 0f && g.Scale > 0f)
                d.WavePhase += g.Speed / (g.Scale * d.WaveLength) * deltaSeconds;

            // 물결에 맞춰 몸이 끄덕인다. 내 달팽이와 같은 규칙이다.
            float full = Mathf.Max(0.0001f, width * Wobble.WaveAmplitudeFraction);
            float ramp = Mathf.Clamp01(d.WaveAmplitude / full);
            float bob = Mathf.Sin(d.WavePhase * Mathf.PI * 2f) * Wobble.BobAmount * ramp;

            // 목표를 정하고 스프링으로 따라붙는다. 내 달팽이와 같은 규칙이라
            // 떼는 저항도, 놓았을 때의 출렁임도 같은 느낌이 된다.
            float stretchTarget = walking ? Mathf.Sin(w) * Wobble.Stretch * amp : g.PeelStretch;
            float leanTarget    = walking ? Mathf.Cos(w) * Wobble.LeanDeg * amp : g.PeelLean;

            Wobble.Spring(ref g.Stretch, ref g.StretchVel, stretchTarget, deltaSeconds);
            Wobble.Spring(ref g.Lean,    ref g.LeanVel,    leanTarget,    deltaSeconds);

            d.Stretch = g.Stretch + bob;
            d.LeanDeg = g.Lean;

            foreach (var s in g.Composed.Soft) s.Apply(d);

            // 껍질은 안 휜다. 껍질 중심이 간 자리로 통째로 옮기고 같이 기울기만 한다.
            var rigid = g.Composed.GroupOrNull(PartsLayer.RigidGroup);
            if (rigid == null) return;

            d.RigidPose(g.ShellCenterY, out var pos, out var rot);
            rigid.localScale = Vector3.one;
            rigid.localRotation = rot;
            rigid.localPosition = pos;
        }

        /// <summary>발바닥 선을 재어 변형에 물려 준다. 몸통 파츠에서 잰다.</summary>
        private static void MeasureSole(SnailAppearance look, SnailDeform deform)
        {
            foreach (var p in look.Parts)
            {
                if (p.Type != SnailPet.Data.PartsType.Body) continue;

                var sprite = SnailComposer.LoadFrame(SnailComposer.LinePath(p.Type, p.ResourceKey));
                if (sprite == null) continue;
                if (!SnailMetrics.TryMeasureSole(sprite, Wobble.SoleSamples, out var sole,
                                                 out float minX, out float maxX)) continue;

                deform.SetSole(sole, minX, maxX);
                return;
            }
        }

        /// <summary>껍질 한가운데의 로컬 y. 껍질이 없으면 몸 한가운데로 둔다.</summary>
        private static float ShellCenterOf(SnailAppearance look, SnailBounds bounds)
        {
            foreach (var p in look.Parts)
            {
                if (p.Type != SnailPet.Data.PartsType.Shell) continue;

                var sprite = SnailComposer.LoadFrame(SnailComposer.LinePath(p.Type, p.ResourceKey));
                if (sprite != null && SnailMetrics.TryMeasure(sprite, out var e))
                    return (e.Bottom + e.Top) * 0.5f;
            }
            return (bounds.Foot + bounds.Top) * 0.5f;
        }

        // ── 집어서 옮기기 ──
        //
        // 남의 달팽이도, 짝꿍도 손으로 옮길 수 있다. 다만 <b>옮기는 것까지만</b>이다 —
        // 먹지도, 자라지도, 똥을 누지도 않는 것은 그대로다.
        //
        // 내 달팽이와 다른 점이 하나 있다. 내 달팽이는 벽에서 <b>당겨 떼어내야</b> 들리는데,
        // 그 저항 연출은 몸이 늘어나는 변형(SnailDeform)에 기대고 있다. 손님에게는 변형이
        // 없어 당기는 동안 아무 일도 안 일어나 보이므로, 누르면 바로 들리게 둔다.

        /// <summary>커서 아래에 있는 손님. 겹쳐 있으면 먼저 찾은 하나다.</summary>
        public Guest FindAt(Vector3 world)
        {
            foreach (var g in _guests.Values)
            {
                if (g.Root == null || !g.Bounds.Measured) continue;

                Vector3 local = g.Root.InverseTransformPoint(world);
                if (local.x >= g.Bounds.Left && local.x <= g.Bounds.Right
                 && local.y >= g.Bounds.Foot && local.y <= g.Bounds.Top) return g;
            }
            return null;
        }

        /// <summary>
        /// 잡았다. 아직 안 떨어진다 — 벽에서 <b>당겨 떼어내야</b> 손에 들린다.
        /// 내 달팽이와 같은 규칙이다.
        /// </summary>
        public static void StartPeel(Guest g, Vector2 cursor)
        {
            if (g == null) return;

            g.Peeling = true;
            g.GrabScreen = cursor;
        }

        /// <summary>덜 당기고 놓았다. 그대로 벽에 붙어 있는다.</summary>
        public static void CancelPeel(Guest g)
        {
            if (g == null) return;

            g.Peeling = false;
            g.PeelStretch = 0f;
            g.PeelLean = 0f;
        }

        /// <summary>
        /// 떼는 중. 당긴 만큼 몸이 늘어나고, 충분히 늘어나면 떨어져 손에 들린다(true).
        ///
        /// 커서가 얼마나 갔는지가 아니라 <b>몸이 실제로 얼마나 늘어났는지</b>로 판정한다 —
        /// 확 잡아채도 스프링이 따라잡을 때까지는 안 떨어지므로 「쭉 늘어나다 툭」이 항상 보인다.
        /// </summary>
        public bool StepPeel(Guest g, Vector2 cursor, ScreenRect box, float pixelsPerWorld)
        {
            if (g == null || !g.Peeling) return false;

            var n = BoxWalk.OutwardNormal(g.Anchor);
            var dir = BoxWalk.Tangent(box, g.Anchor);
            Vector2 pull = cursor - g.GrabScreen;

            float away  = Vector2.Dot(pull, -n);     // 벽에서 멀어지는 성분
            float along = Vector2.Dot(pull, dir);    // 벽을 따라가는 성분

            g.PeelStretch = Mathf.Clamp01(away / Wobble.PeelThreshold) * Wobble.PeelMaxStretch;
            g.PeelLean    = Mathf.Clamp(along / Wobble.PeelThreshold, -1f, 1f) * -Wobble.PeelMaxLeanDeg;

            if (away < Wobble.PeelThreshold || g.Stretch < Wobble.PeelMaxStretch * 0.9f) return false;

            CancelPeel(g);
            Grab(g, box, pixelsPerWorld);
            g.StretchVel += Wobble.PopRecoil * Wobble.SpringStiffness * 0.05f;   // 툭 하고 되튕긴다
            return true;
        }

        /// <summary>집어 든다. 지금 서 있는 자리에서 발 좌표를 받아 둔다(그래야 손이 튀지 않는다).</summary>
        public void Grab(Guest g, ScreenRect box, float pixelsPerWorld)
        {
            if (g == null) return;

            float footDepth = Mathf.Abs(g.Bounds.Foot) * g.Scale * pixelsPerWorld;

            g.FootScreen = g.Screen + BoxWalk.OutwardNormal(g.Anchor) * footDepth;
            g.Held = true;
            g.Falling = false;
            g.VelY = 0f;
        }

        /// <summary>놓는다. 아래로 떨어져 바닥에 붙는다.</summary>
        public static void Drop(Guest g)
        {
            if (g == null) return;

            g.Held = false;
            g.Falling = true;
            g.VelY = 0f;
        }

        /// <summary>
        /// 들려 있거나 떨어지는 동안의 자리. 벽에서 떨어져 있으므로 <b>똑바로 세운다</b>.
        /// 바닥에 닿으면 그 자리에서 아래 벽에 다시 붙는다 — 내 달팽이와 같은 규칙이다.
        /// </summary>
        private static void Carry(Guest g, float deltaSeconds, ScreenRect box,
                                  float halfExtent, float footDepth, float gravity)
        {
            if (g.Falling)
            {
                g.VelY += gravity * deltaSeconds;
                g.FootScreen.y += g.VelY * deltaSeconds;

                if (g.FootScreen.y >= box.Bottom)
                {
                    g.FootScreen.y = box.Bottom;
                    g.VelY = 0f;
                    g.Falling = false;

                    float p = BoxWalk.BottomXToPerimeter(box, g.FootScreen.x);
                    g.Anchor = BoxWalk.FromPerimeter(box, p, g.Anchor.Forward, halfExtent);
                }
            }

            g.FootScreen = new Vector2(
                Mathf.Clamp(g.FootScreen.x, box.Left + halfExtent, box.Right - halfExtent),
                Mathf.Clamp(g.FootScreen.y, box.Top, box.Bottom));

            g.Screen = g.FootScreen - new Vector2(0f, footDepth);
            g.RotationDeg = 0f;
        }

        public void Clear()
        {
            foreach (var g in _guests.Values) Destroy(g);
            _guests.Clear();
        }

        private static void Destroy(Guest g)
        {
            if (g == null) return;

            // 이펙트와 이름표는 루트의 자식이 아니라 따로 서 있다. 안 치우면 손님이 나가도 남는다.
            SparkField.Detach(g.Effects);
            g.Tag?.Dispose();

            if (g.Root != null) Object.Destroy(g.Root.gameObject);
        }
    }
}
