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

            public float Fade = -1f;      // 0 이상이면 치워지는 중
        }

        private readonly Transform _parent;
        private readonly List<Egg> _items = new List<Egg>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        public EggField(Transform parent) { _parent = parent; }

        public IReadOnlyList<Egg> Items => _items;
        public int Count => _items.Count;

        private Sprite Art(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_sprites.TryGetValue(key, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(ArtFolder + key);
            if (sprite == null) Debug.LogWarning("[SnailPet] 알 아트를 찾지 못했습니다: " + ArtFolder + key);

            _sprites[key] = sprite;
            return sprite;
        }

        /// <summary>한 개 놓는다. 아트가 없으면 아무것도 안 만들고 null 을 준다.</summary>
        public Egg Spawn(int eggId, string art, SnailAppearance gene, Vector2 screen, float pixels)
        {
            var sprite = Art(art);
            if (sprite == null) return null;

            var go = new GameObject("Egg");
            go.transform.SetParent(_parent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8000;      // 똥과 같은 층. 달팽이가 지나가면 가려진다

            float ppu = Mathf.Max(0.0001f, sprite.pixelsPerUnit);
            float world = sprite.rect.width / ppu;
            float scale = world > 0.0001f ? pixels / world : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var egg = new Egg
            {
                Root = go.transform,
                Renderer = sr,
                Screen = screen,
                EggId = eggId,
                Gene = gene,
                Half = new Vector2(sprite.rect.width, sprite.rect.height) / ppu * scale * 0.5f,
            };

            _items.Add(egg);
            return egg;
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

        public void Clear()
        {
            foreach (var e in _items)
                if (e.Root != null) Object.Destroy(e.Root.gameObject);
            _items.Clear();
        }
    }
}
