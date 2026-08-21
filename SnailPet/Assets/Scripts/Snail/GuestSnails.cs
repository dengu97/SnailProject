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
        }

        private readonly Transform _parent;
        private readonly Dictionary<string, Guest> _guests = new Dictionary<string, Guest>();

        /// <summary>파츠에 딸린 이펙트를 붙일 곳. 내 달팽이와 같은 것을 쓴다.</summary>
        private readonly SparkField _sparks;

        public GuestField(Transform parent, SparkField sparks = null)
        {
            _parent = parent;
            _sparks = sparks;
        }

        public IEnumerable<Guest> Items => _guests.Values;
        public int Count => _guests.Count;

        /// <summary>
        /// 방에 있는 사람들에 맞춘다. 새로 온 사람은 세우고, 나간 사람은 치우고,
        /// 모습이 바뀐 사람은 다시 세운다. <b>나는 빼고</b> 넘겨야 한다 — 내 달팽이는 본체가 그린다.
        /// </summary>
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
            var (_, _, level, appearance) = SnailShare.ReadCard(card);
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

            return new Guest
            {
                Effects = effects,
                Name = name,
                Look = card,
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

        /// <summary>벽을 따라 걷는다. 자리 계산은 내 달팽이와 같은 기하를 쓴다.</summary>
        public void Tick(float deltaSeconds, ScreenRect box, float pixelsPerWorld)
        {
            foreach (var g in _guests.Values)
            {
                float halfExtent = (g.Bounds.Right - g.Bounds.Left) * 0.5f * g.Scale * pixelsPerWorld;
                float footDepth = 0f;   // 발이 벽에 붙는다. 손님은 늘어나지 않으므로 0 이면 된다

                g.Anchor = BoxWalk.Advance(box, g.Anchor, g.Speed, deltaSeconds, halfExtent);

                var pose = BoxWalk.Evaluate(box, g.Anchor, footDepth, halfExtent);
                if (!pose.Valid) continue;

                g.Screen = pose.RootScreen;
                g.RotationDeg = pose.RotationDeg;
                g.Flip = pose.FlipX;
            }
        }

        public void Clear()
        {
            foreach (var g in _guests.Values) Destroy(g);
            _guests.Clear();
        }

        private static void Destroy(Guest g)
        {
            if (g == null) return;

            // 이펙트는 루트의 자식이 아니라 따로 서 있다. 안 치우면 손님이 나가도 남는다.
            SparkField.Detach(g.Effects);

            if (g.Root != null) Object.Destroy(g.Root.gameObject);
        }
    }
}
