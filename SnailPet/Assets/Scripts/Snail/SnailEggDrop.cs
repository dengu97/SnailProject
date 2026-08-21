using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 방에서 낳아 화면 구석에 놓인 알.
    ///
    /// 똥과 같은 방식이다 — 자리는 <b>화면 좌표</b>로 들고 있고 월드로 옮기는 것은 부르는 쪽이
    /// 한다. 시간이 지나도 안 사라지고, 유저가 눌러야 치워진다. 다른 점은 눌렀을 때 주는 것이
    /// 코인이 아니라 알 자체라는 것과, 태어날 모습을 여기까지 들고 온다는 것이다.
    ///
    /// 벽에 붙는 똥과 달리 구석에 그냥 놓이므로 회전도 반전도 없다.
    /// </summary>
    public sealed class EggField
    {
        /// <summary>알 아트가 있는 곳. UI 의 알 칸이 쓰는 곳과 같다.</summary>
        private const string ArtFolder = "Snail/Egg/";

        /// <summary>누른 뒤 이만큼 동안 흐려지다 사라진다. 똥과 같다.</summary>
        private const float FadeSeconds = 0.35f;

        public sealed class Egg
        {
            public Transform Root;
            public SpriteRenderer Renderer;
            public Vector2 Screen;        // 화면 좌표 (y 는 아래가 +)
            public Vector2 Half;          // 화면 픽셀 반지름. 구석에서 들여놓는 데 쓴다

            /// <summary>회수하면 이 알이 목록으로 들어간다.</summary>
            public int EggId;

            /// <summary>물려받은 모습. 낳을 때 정해져 여기까지 실려 온다.</summary>
            public SnailAppearance Gene;

            // ── 등장 연출 ──

            /// <summary>화면에서 보일 가로 크기(px). 그림을 갈아 끼울 때 배율을 다시 잡는 데 쓴다.</summary>
            public float Pixels;

            /// <summary>평소 그림과 연출 칸의 배율. 연출은 알 자체 크기를 맞춘 값이라 따로 든다.</summary>
            public float StillScale, PopScale;

            /// <summary>연출이 끝나면 돌아갈 평소 그림.</summary>
            public Sprite Still;

            /// <summary>등장 연출 칸들. 없으면 null 이고 그때는 처음부터 평소 그림이다.</summary>
            public Sprite[] Pop;

            /// <summary>연출이 시작된 뒤 흐른 시간과 지금 보이는 칸.</summary>
            public float PopTime;
            public int PopShown = -2;   // -2 = 쉬는 중

            public float Fade = -1f;      // 0 이상이면 치워지는 중
        }

        private readonly Transform _parent;
        private readonly List<Egg> _items = new List<Egg>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        public EggField(Transform parent) { _parent = parent; }

        public IReadOnlyList<Egg> Items => _items;
        public int Count => _items.Count;

        /// <param name="quiet">없어도 되는 그림이면 true. 등장 연출은 있으면 쓰고 없으면 만다.</param>
        private Sprite Art(string key, bool quiet = false)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_sprites.TryGetValue(key, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(ArtFolder + key);
            if (sprite == null && !quiet) Debug.LogWarning("[SnailPet] 알 아트를 찾지 못했습니다: " + ArtFolder + key);

            _sprites[key] = sprite;
            return sprite;
        }

        /// <summary>한 개 놓는다. 아트가 없으면 아무것도 안 만들고 null 을 준다.</summary>
        public Egg Spawn(int eggId, string art, SnailAppearance gene, Vector2 screen, float pixels)
        {
            var still = Art(art);
            if (still == null) return null;

            // 등장 연출은 <그림이름>_pop 이 있으면 그걸 쓴다. 데이터에 칸을 두지 않는 것은
            // 애니메이션 파츠와 같은 생각이다 — 파일이 있으면 있는 것이다.
            var pop = SnailComposer.FramesOf(Art(art + PopSuffix, quiet: true));

            var go = new GameObject("Egg");
            go.transform.SetParent(_parent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8000;      // 똥과 같은 층. 달팽이가 지나가면 가려진다

            var egg = new Egg
            {
                Root = go.transform,
                Renderer = sr,
                Screen = screen,
                EggId = eggId,
                Gene = gene,
                Pixels = pixels,
                Still = still,
                Pop = pop,
                StillScale = ScaleFor(still, pixels),
            };

            // 놓이는 자리는 <b>평소 그림</b>으로 잡는다. 연출 칸을 기준으로 잡으면
            // 통통 튀는 동안 알이 구석에서 밀려났다가 끝날 때 제자리로 튄다.
            float ppu = Mathf.Max(0.0001f, still.pixelsPerUnit);
            egg.Half = new Vector2(still.rect.width, still.rect.height) / ppu * egg.StillScale * 0.5f;

            if (pop != null)
            {
                // 연출 그림은 캔버스 안에서 알이 차지하는 비율이 평소 그림과 다를 수 있다.
                // 칸 너비에 맞추면 끝나는 순간 크기가 튀므로, <b>알 자체</b>가 같은 크기로
                // 보이도록 배율을 잡는다. 칸끼리의 크기 변화(통통 튀는 것)는 그대로 남는다.
                egg.PopScale = MatchScale(pop[0], still, egg.StillScale);
                ReportPop(art, still, pop);
            }

            ShowIdle(egg);      // 놓이자마자 한 번 튀고, 그 뒤로는 이 모습으로 쉰다

            _items.Add(egg);
            return egg;
        }

        /// <summary>등장 연출 그림의 이름 뒤에 붙는 것.</summary>
        private const string PopSuffix = "_pop";

        private static readonly HashSet<string> _reported = new HashSet<string>();

        /// <summary>
        /// 연출 그림과 평소 그림이 <b>칸 안에서 차지하는 비율</b>을 한 번 알린다.
        /// 이 둘이 많이 다르면 연출이 끝나는 순간 알 크기가 튄다 — 화면을 안 보고도 알 수 있게 남긴다.
        /// </summary>
        private static void ReportPop(string art, Sprite still, Sprite[] pop)
        {
            if (!_reported.Add(art)) return;

            string Fill(Sprite s) =>
                SnailMetrics.TryMeasure(s, out var e) && s.rect.width > 0
                    ? (100f * e.Width * s.pixelsPerUnit / s.rect.width).ToString("0") + "%"
                    : "?";

            Debug.Log($"[SnailPet] 알 등장 연출: {art}{PopSuffix} {pop.Length}칸 " +
                      $"({PopEvery:0}초마다 왕복 {2 * pop.Length - 2}걸음 · {PopFps:0}fps) · " +
                      $"칸을 채운 비율 연출 {Fill(pop[0])} vs 평소 {Fill(still)}");
        }

        /// <summary>등장 연출의 재생 속도(초당 칸).</summary>
        private const float PopFps = 14f;

        /// <summary>
        /// 뽀잉거리는 주기(초). 한 바퀴 돌고 나면 다음 차례까지 쉰다.
        /// 쉬지 않고 계속 튀면 구석에서 정신이 없다.
        /// </summary>
        private const float PopEvery = 5f;

        /// <summary>
        /// 쉬는 동안 보여 줄 그림. <b>-1 이면 평소 그림(원래 아이콘)</b>이고,
        /// 0 이상이면 연출 시트의 그 칸이다(2 = 세 번째 칸).
        /// </summary>
        private const int PopIdleFrame = -1;

        /// <summary>쉬는 중이라는 표시. 칸 번호와 섞이지 않게 음수를 쓴다.</summary>
        private const int IdleShown = -2;

        /// <summary>쉬는 모습으로 둔다. 연출이 없는 알은 그냥 평소 그림이다.</summary>
        private static void ShowIdle(Egg egg)
        {
            egg.PopShown = IdleShown;

            if (egg.Pop == null || PopIdleFrame < 0) Show(egg, egg.Still, egg.StillScale);
            else Show(egg, egg.Pop[Mathf.Clamp(PopIdleFrame, 0, egg.Pop.Length - 1)], egg.PopScale);
        }

        /// <summary>그림을 갈아 끼운다. 배율은 부르는 쪽이 정한다.</summary>
        private static void Show(Egg egg, Sprite sprite, float scale)
        {
            if (sprite == null) return;

            egg.Renderer.sprite = sprite;
            egg.Root.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>캔버스 가로가 화면에서 pixels 이 되는 배율.</summary>
        private static float ScaleFor(Sprite sprite, float pixels)
        {
            float ppu = Mathf.Max(0.0001f, sprite.pixelsPerUnit);
            float world = sprite.rect.width / ppu;
            return world > 0.0001f ? pixels / world : 1f;
        }

        /// <summary>
        /// <paramref name="sprite"/> 안의 그림이 <paramref name="like"/> 안의 그림과
        /// 같은 크기로 보이게 하는 배율. 캔버스가 아니라 <b>불투명한 부분</b>을 견준다.
        /// 잴 수 없으면 캔버스 기준으로 물러선다.
        /// </summary>
        private static float MatchScale(Sprite sprite, Sprite like, float likeScale)
        {
            if (!SnailMetrics.TryMeasure(sprite, out var mine) || mine.Width <= 0.0001f ||
                !SnailMetrics.TryMeasure(like, out var theirs) || theirs.Width <= 0.0001f)
                return ScaleFor(sprite, likeScale * (like.rect.width / Mathf.Max(0.0001f, like.pixelsPerUnit)));

            return theirs.Width * likeScale / mine.Width;
        }

        /// <summary>
        /// 그 자리에 있는 알. 없으면 null. 판정은 똥과 같다 —
        /// 커서를 알의 로컬 좌표로 역변환해 스프라이트 경계와 비교한다.
        /// 치워지는 중인 것은 잡히지 않는다. 두 번 눌러 두 개를 받으면 안 된다.
        /// </summary>
        public Egg FindAt(Vector3 world)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var e = _items[i];
                if (e.Root == null || e.Fade >= 0f || e.Renderer.sprite == null) continue;

                var b = e.Renderer.sprite.bounds;
                Vector3 local = e.Root.InverseTransformPoint(world);
                if (local.x >= b.min.x && local.x <= b.max.x
                 && local.y >= b.min.y && local.y <= b.max.y) return e;
            }
            return null;
        }

        /// <summary>회수했다. 바로 없어지지 않고 스르륵 흐려진다.</summary>
        public void Remove(Egg egg)
        {
            if (egg != null && egg.Fade < 0f) egg.Fade = 0f;
        }

        /// <summary>아직 회수 안 한 알들. 나갈 때 적어 두는 쪽이 쓴다.</summary>
        public IEnumerable<Egg> Pending()
        {
            foreach (var e in _items) if (e.Fade < 0f) yield return e;
        }

        /// <summary>흐려지는 것만 진행시킨다. 나머지는 그대로 놓여 있는다.</summary>
        public void Tick(float deltaSeconds)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var e = _items[i];
                if (e.Root == null) { _items.RemoveAt(i); continue; }

                StepPop(e, deltaSeconds);
                if (e.Fade < 0f) continue;

                e.Fade += deltaSeconds;
                if (e.Fade >= FadeSeconds)
                {
                    Object.Destroy(e.Root.gameObject);
                    _items.RemoveAt(i);
                    continue;
                }

                var col = e.Renderer.color;
                e.Renderer.color = new Color(col.r, col.g, col.b, 1f - e.Fade / FadeSeconds);
            }
        }

        /// <summary>
        /// 놓여 있는 동안 <see cref="PopEvery"/> 마다 한 번씩 뽀잉거린다.
        ///
        /// <b>갔다가 되돌아온다</b> — 5칸이면 123454321 이라 되돌아오는 칸은 그리지 않아도 된다.
        /// 한 바퀴가 끝나면 다음 차례까지 첫 칸으로 쉰다. 유저가 주울 때까지 이걸 되풀이한다 —
        /// 구석에 놓인 알을 알아채게 하려는 것이지 계속 튀게 하려는 것이 아니다.
        /// </summary>
        private static void StepPop(Egg egg, float deltaSeconds)
        {
            if (egg.Pop == null) return;

            int n = egg.Pop.Length;
            if (n < 2) return;

            egg.PopTime += deltaSeconds;

            int cycle = 2 * n - 2;              // 5칸이면 8걸음(0..4..0)
            int step = (int)(egg.PopTime % PopEvery * PopFps);

            // 한 바퀴를 다 돌았으면 다음 차례까지 쉰다.
            // 쉬는 동안 보여 줄 그림은 PopIdleFrame 이 정한다.
            if (step >= cycle)
            {
                if (egg.PopShown != IdleShown) ShowIdle(egg);
                return;
            }

            int frame = step < n ? step : cycle - step;
            if (frame == egg.PopShown) return;

            egg.PopShown = frame;
            Show(egg, egg.Pop[frame], egg.PopScale);
        }

        public void Clear()
        {
            foreach (var e in _items)
                if (e.Root != null) Object.Destroy(e.Root.gameObject);
            _items.Clear();
        }
    }
}
