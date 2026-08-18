using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 먹고 나서 싸 놓는 똥.
    ///
    /// 부스러기와 같은 방식이다 — 자리는 <b>화면 좌표</b>로 들고 있고 월드로 옮기는 것은
    /// 부르는 쪽이 한다. 다만 부스러기와 달리 시간이 지나도 사라지지 않는다.
    /// 유저가 눌러야 치워진다.
    ///
    /// 달팽이는 박스 안쪽 벽을 타고 다니므로, 쌀 때의 회전과 좌우 반전을 그대로 물려받아야
    /// 벽에 붙어 있는 것으로 보인다. 옆벽에서 싼 똥이 혼자 똑바로 서 있으면 공중에 뜬다.
    ///
    /// 아트는 Ui/Icon 에 있어 픽셀을 읽을 수 없다(임포터가 Resources/Snail 만 다룬다).
    /// 그래서 크기는 부스러기와 같은 방법으로 스프라이트 치수에서 환산한다.
    /// </summary>
    public sealed class PoopField
    {
        /// <summary>똥 아트가 있는 곳. <c>FoodData.PoopResourceKey</c> 가 이 아래의 파일 이름이다.</summary>
        private const string ArtFolder = "Ui/Icon/";

        /// <summary>누른 뒤 이만큼 동안 흐려지다 사라진다.</summary>
        private const float FadeSeconds = 0.35f;

        public sealed class Poop
        {
            public Transform Root;
            public SpriteRenderer Renderer;
            public Vector2 Screen;        // 화면 좌표 (y 는 아래가 +)
            public float RotationDeg;
            public float HalfHeight;      // 화면 픽셀. 벽에서 띄우는 데 쓴다
            public float Fade = -1f;      // 0 이상이면 치워지는 중
        }

        private readonly Transform _parent;
        private readonly List<Poop> _items = new List<Poop>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        public PoopField(Transform parent) { _parent = parent; }

        public IReadOnlyList<Poop> Items => _items;
        public int Count => _items.Count;

        private Sprite Art(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_sprites.TryGetValue(key, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(ArtFolder + key);
            if (sprite == null) Debug.LogWarning("[SnailPet] 똥 아트를 찾지 못했습니다: " + ArtFolder + key);

            _sprites[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// 한 덩이 놓는다. 아트가 없으면 아무것도 안 만들고 null 을 준다.
        /// </summary>
        /// <param name="art">그림 이름(<c>FoodData.PoopResourceKey</c>).</param>
        /// <param name="screen">놓을 화면 좌표. 벽에서 띄우는 것은 부르는 쪽이 <see cref="Poop.HalfHeight"/> 로 한다.</param>
        /// <param name="pixels">화면에서 보일 크기(가로 픽셀).</param>
        /// <param name="rotationDeg">달팽이 자세. 벽을 따라 같이 돌아간다.</param>
        /// <param name="flip">달팽이가 보던 방향.</param>
        public Poop Spawn(string art, Vector2 screen, float pixels, float rotationDeg, bool flip)
        {
            var sprite = Art(art);
            if (sprite == null) return null;

            var go = new GameObject("Poop");
            go.transform.SetParent(_parent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8000;      // 달팽이 뒤. 지나가면 몸에 가려진다

            float ppu = Mathf.Max(0.0001f, sprite.pixelsPerUnit);
            float world = sprite.rect.width / ppu;
            float scale = world > 0.0001f ? pixels / world : 1f;
            go.transform.localScale = new Vector3(flip ? -scale : scale, scale, 1f);

            var poop = new Poop
            {
                Root = go.transform,
                Renderer = sr,
                Screen = screen,
                RotationDeg = rotationDeg,
                HalfHeight = sprite.rect.height / ppu * scale * 0.5f,
            };

            _items.Add(poop);
            return poop;
        }

        /// <summary>
        /// 그 자리에 있는 똥. 없으면 null.
        ///
        /// 달팽이 히트 판정과 같은 방법이다 — 회전·반전·크기를 화면 좌표에서 다시 계산하는 대신
        /// 커서를 똥의 로컬 좌표로 역변환해 스프라이트 경계와 그대로 비교한다.
        /// 치워지는 중인 것은 잡히지 않는다. 두 번 눌러 코인을 두 번 받으면 안 된다.
        /// </summary>
        public Poop FindAt(Vector3 world)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var p = _items[i];
                if (p.Root == null || p.Fade >= 0f || p.Renderer.sprite == null) continue;

                var b = p.Renderer.sprite.bounds;
                Vector3 local = p.Root.InverseTransformPoint(world);
                if (local.x >= b.min.x && local.x <= b.max.x
                 && local.y >= b.min.y && local.y <= b.max.y) return p;
            }
            return null;
        }

        /// <summary>치우기 시작한다. 바로 없어지지 않고 스르륵 흐려진다.</summary>
        public void Remove(Poop poop)
        {
            if (poop != null && poop.Fade < 0f) poop.Fade = 0f;
        }

        /// <summary>흐려지는 것만 진행시킨다. 나머지는 그대로 붙어 있는다.</summary>
        public void Tick(float deltaSeconds)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var p = _items[i];
                if (p.Root == null) { _items.RemoveAt(i); continue; }
                if (p.Fade < 0f) continue;

                p.Fade += deltaSeconds;
                if (p.Fade >= FadeSeconds)
                {
                    Object.Destroy(p.Root.gameObject);
                    _items.RemoveAt(i);
                    continue;
                }

                var col = p.Renderer.color;
                p.Renderer.color = new Color(col.r, col.g, col.b, 1f - p.Fade / FadeSeconds);
            }
        }

        public void Clear()
        {
            foreach (var p in _items)
                if (p.Root != null) Object.Destroy(p.Root.gameObject);
            _items.Clear();
        }
    }
}
