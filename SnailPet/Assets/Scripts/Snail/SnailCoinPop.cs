using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 선물을 받을 때 달팽이 위로 뽀잉 하고 떠오르는 코인.
    ///
    /// 한 장짜리 그림이 아니라 가로로 이어 붙인 프레임 시트(600x100, 6칸)를 돌려 쓴다.
    /// 자르는 것은 런타임에 <see cref="Sprite.Create(Texture2D, Rect, Vector2, float)"/> 로 한다 —
    /// 텍스처의 픽셀을 읽지 않으므로 Read/Write 설정과 무관하다 (Ui/Icon 아래는 꺼져 있다).
    ///
    /// 위치는 부스러기와 같이 <b>화면 좌표</b>로 들고 있고, 월드로 옮기는 것은 부르는 쪽이 한다.
    /// </summary>
    public sealed class CoinPop
    {
        private const string ArtPath = "Ui/Icon/coin_motion";

        /// <summary>시트의 칸 수. 600x100 을 6칸으로 자른다.</summary>
        private const int FrameCount = 6;

        /// <summary>초당 몇 칸 넘길지. 6칸이면 0.43초에 한 바퀴 돈다.</summary>
        private const float Fps = 14f;

        /// <summary>뜨는 동안 커지는 시간과, 사라지며 흐려지는 구간(수명 대비 비율).</summary>
        private const float PopTime = 0.12f, FadeFrom = 0.6f;

        public sealed class Coin
        {
            public Transform Root;
            public SpriteRenderer Renderer;
            public Vector2 From;        // 시작한 화면 좌표
            public Vector2 Screen;      // 지금 화면 좌표
            public float Rise;          // 총 몇 px 떠오를지
            public float Pixels;        // 화면에서 보일 크기
            public float Life, Age;
        }

        private readonly Transform _parent;
        private readonly List<Coin> _items = new List<Coin>();
        private Sprite[] _frames;
        private bool _looked;

        public CoinPop(Transform parent) { _parent = parent; }

        public IReadOnlyList<Coin> Items => _items;
        public int Count => _items.Count;

        private Sprite[] Frames()
        {
            if (_looked) return _frames;
            _looked = true;

            var texture = Resources.Load<Texture2D>(ArtPath);
            if (texture == null)
            {
                Debug.LogWarning("[SnailPet] 코인 모션 아트를 찾지 못했습니다: " + ArtPath);
                return null;
            }

            int w = texture.width / FrameCount;
            _frames = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                // 피벗을 한가운데로 둬야 도는 코인이 제자리에서 돈다.
                // PPU 100 이면 한 칸이 월드 1 단위라 크기 계산이 「보일 픽셀 = 배율」이 된다.
                _frames[i] = Sprite.Create(texture, new Rect(i * w, 0f, w, texture.height),
                                           new Vector2(0.5f, 0.5f), w);
            }
            return _frames;
        }

        /// <param name="screen">떠오르기 시작할 화면 좌표(달팽이 머리 위).</param>
        /// <param name="pixels">화면에서 보일 크기.</param>
        /// <param name="rise">이만큼 위로 떠오른다.</param>
        public void Pop(Vector2 screen, float pixels, float rise, float life)
        {
            var frames = Frames();
            if (frames == null) return;

            var go = new GameObject("CoinPop");
            go.transform.SetParent(_parent, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 9500;      // 부스러기보다 앞, 말풍선보다 뒤

            _items.Add(new Coin
            {
                Root = go.transform,
                Renderer = sr,
                From = screen,
                Screen = screen,
                Rise = rise,
                Pixels = pixels,
                Life = life,
            });
        }

        /// <summary>돌고, 떠오르고, 흐려지다 사라진다.</summary>
        public void Tick(float deltaSeconds)
        {
            var frames = _frames;

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

                float t = Mathf.Clamp01(c.Age / c.Life);

                // 처음에 빠르게 솟았다가 천천히 멎는다. 화면 y 는 아래가 + 라 빼야 위로 간다.
                c.Screen = new Vector2(c.From.x, c.From.y - c.Rise * (1f - (1f - t) * (1f - t)));

                // 뽀잉: 나오자마자 살짝 커진다
                float pop = Mathf.Clamp01(c.Age / PopTime);
                float scale = c.Pixels * Mathf.Lerp(0.6f, 1f, pop);
                c.Root.localScale = new Vector3(scale, scale, 1f);

                if (frames != null)
                    c.Renderer.sprite = frames[Mathf.FloorToInt(c.Age * Fps) % frames.Length];

                float a = t < FadeFrom ? 1f : 1f - (t - FadeFrom) / (1f - FadeFrom);
                var col = c.Renderer.color;
                c.Renderer.color = new Color(col.r, col.g, col.b, Mathf.Clamp01(a));
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
