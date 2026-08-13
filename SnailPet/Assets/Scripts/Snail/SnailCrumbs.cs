using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 먹을 때 사방으로 튀는 부스러기.
    ///
    /// 먹이와 같은 방식이다 — 위치는 <b>화면 좌표</b>로 들고 있고, 실제 트랜스폼에 옮기는 것은
    /// 부르는 쪽이 한다. 이 창은 가상 화면(모니터 여러 대)을 덮으므로 화면↔월드 환산이
    /// 한 곳에만 있어야 하기 때문이다.
    ///
    /// 아트는 Ui/Icon 에 있어 픽셀당 단위가 달팽이 아트(PPU 1)와 다르다. 그래서 크기는
    /// 실측해서 「화면에서 몇 픽셀로 보일지」로 맞춘다 — 먹이·말풍선이 쓰는 방법과 같다.
    /// </summary>
    public sealed class CrumbField
    {
        private const string ArtPath = "Ui/Icon/food_parts";

        /// <summary>사라지기 직전 이만큼의 시간 동안 흐려진다.</summary>
        private const float FadeSeconds = 0.5f;

        public sealed class Crumb
        {
            public Transform Root;
            public SpriteRenderer Renderer;
            public Vector2 Screen;      // 화면 좌표 (y 는 아래가 +)
            public Vector2 Velocity;    // 초당 픽셀
            public float Life, Age;
            public float Spin;          // 초당 도
            public float Spun;          // 지금까지 돈 각
        }

        private readonly Transform _parent;
        private readonly List<Crumb> _items = new List<Crumb>();
        private Sprite _sprite;
        private bool _looked;

        public CrumbField(Transform parent) { _parent = parent; }

        public IReadOnlyList<Crumb> Items => _items;
        public int Count => _items.Count;

        private Sprite Art()
        {
            if (_looked) return _sprite;

            _looked = true;
            _sprite = Resources.Load<Sprite>(ArtPath);
            if (_sprite == null)
                Debug.LogWarning("[SnailPet] 부스러기 아트를 찾지 못했습니다: " + ArtPath);
            return _sprite;
        }

        /// <summary>
        /// 한 조각 튀긴다.
        /// </summary>
        /// <param name="screen">튀기 시작하는 화면 좌표.</param>
        /// <param name="pixels">화면에서 보일 크기(가로 픽셀).</param>
        /// <param name="speed">초당 픽셀. 방향은 위쪽 반원 안에서 아무렇게나 잡는다.</param>
        /// <param name="life">몇 초 뒤에 사라질지.</param>
        public void Spawn(Vector2 screen, float pixels, float speed, float life)
        {
            var sprite = Art();
            if (sprite == null) return;

            var go = new GameObject("Crumb");
            go.transform.SetParent(_parent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 9000;         // 달팽이보다 앞, 말풍선보다 뒤

            // 크기는 스프라이트의 가로로 맞춘다. 달팽이·먹이처럼 픽셀을 읽어 「보이는 부분」을
            // 재면 안 된다 — Ui/Icon 아래 텍스처는 읽기가 꺼져 있어(임포터가 Resources/Snail
            // 만 다룬다) GetPixels32 가 예외를 던지고, 조각이 아예 안 만들어진다.
            float world = sprite.rect.width / Mathf.Max(0.0001f, sprite.pixelsPerUnit);
            float scale = world > 0.0001f ? pixels / world : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            // 위쪽 반원으로 튄다. 아래로 쏘면 바로 바닥에 박혀 안 보인다.
            float deg = Random.Range(20f, 160f);
            var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), -Mathf.Sin(deg * Mathf.Deg2Rad));

            _items.Add(new Crumb
            {
                Root = go.transform,
                Renderer = sr,
                Screen = screen,
                Velocity = dir * speed,
                Life = life,
                Spin = Random.Range(-180f, 180f),
            });
        }

        /// <summary>
        /// 날아가고, 늙고, 다 살면 사라진다.
        ///
        /// 중력은 먹이보다 훨씬 약하게 준다. 먹이는 화면 위에서 떨어뜨리는 것이라 1600 이
        /// 맞지만, 부스러기는 몇십 픽셀을 튀는 것이라 같은 값을 쓰면 최고 높이가 2~3px 이
        /// 되어 튀는 것이 아예 안 보인다.
        ///
        /// <paramref name="floorY"/> 에 닿으면 멈춘다. 안 그러면 바닥을 뚫고 내려가
        /// 화면 밖으로 사라진다 — 달팽이는 보통 아래 벽에 붙어 있다.
        /// </summary>
        public void Tick(float deltaSeconds, float gravity, float floorY)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var c = _items[i];
                c.Age += deltaSeconds;

                if (c.Age >= c.Life || c.Root == null)
                {
                    if (c.Root != null) Object.Destroy(c.Root.gameObject);
                    _items.RemoveAt(i);
                    continue;
                }

                c.Velocity = new Vector2(c.Velocity.x, c.Velocity.y + gravity * deltaSeconds);
                c.Screen += c.Velocity * deltaSeconds;

                // 바닥에 닿으면 한 번 튕겼다 멈춘다. 구르지 않게 가로 속도도 같이 죽인다.
                if (c.Screen.y >= floorY)
                {
                    c.Screen = new Vector2(c.Screen.x, floorY);
                    c.Velocity = c.Velocity.y > 60f
                               ? new Vector2(c.Velocity.x * 0.4f, -c.Velocity.y * 0.35f)
                               : Vector2.zero;
                }

                // 멈춘 뒤에는 돌지 않는다
                if (c.Velocity != Vector2.zero) c.Spun += c.Spin * deltaSeconds;
                c.Root.localRotation = Quaternion.Euler(0f, 0f, c.Spun);

                // 끝에서만 흐려진다. 처음부터 흐리면 튀는 것이 안 보인다.
                float left = c.Life - c.Age;
                float a = left >= FadeSeconds ? 1f : Mathf.Clamp01(left / FadeSeconds);
                var col = c.Renderer.color;
                c.Renderer.color = new Color(col.r, col.g, col.b, a);
            }
        }

        public void Clear()
        {
            foreach (var c in _items)
                if (c.Root != null) Object.Destroy(c.Root.gameObject);
            _items.Clear();
        }
    }
}
