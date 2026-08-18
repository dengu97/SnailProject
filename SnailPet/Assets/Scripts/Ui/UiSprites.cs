using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Ui
{
    /// <summary>
    /// UI 도형을 코드로 만든다.
    ///
    /// 목업의 패널·게이지·뱃지·슬롯은 전부 「둥근 사각형 + 채우기 + 테두리」다.
    /// 이걸 그림으로 뽑으면 안 된다. 디폴트와 최대화에서 크기가 달라지는데
    /// 고정 크기 비트맵을 늘리면 모서리가 뭉개진다.
    ///
    /// 그래서 9-슬라이스 스프라이트를 만든다. 모서리만 원본 크기로 찍고 가운데는
    /// 늘리므로 어떤 크기에서도 선명하다. 흰색으로 만들어 Image.color 로 물들인다.
    ///
    /// 채우기와 테두리를 한 장에 담지 않는 이유: Image.color 는 스프라이트 전체에
    /// 곱해져서 둘을 따로 칠할 수 없다. 두 장을 겹친다.
    /// </summary>
    public static class UiSprites
    {
        private static readonly Dictionary<int, Sprite> _cache = new Dictionary<int, Sprite>();

        /// <summary>
        /// UI 도형의 역할. 코드가 만드는 것은 어디까지나 <b>임시</b>이고,
        /// 같은 이름의 아트가 Resources/Ui/Shape 에 있으면 그쪽이 이긴다.
        /// 아트를 넣어도 배치 코드는 한 줄도 바뀌지 않는다.
        /// </summary>
        public enum Shape
        {
            Panel,          // 큰 판
            PanelBorder,    // 판의 테두리 (판과 따로 물들여야 해서 분리돼 있다)
            Slot,           // 목록 행처럼 가로로 긴 홈
            Badge,          // 알약 — 등급·코인
            Fill,           // 게이지 채우기 (포만도)
            FillHappy,      // 게이지 채우기 (행복 지수)
            Button,         // 버튼 배경
            Selection,      // 고른 칸에 덧그리는 테두리 (아트 이름은 slotline)
            RowSelection,   // 고른 달팽이 줄에 덧그리는 테두리 (slotline2). 칸과 모양이 다르다

            // 아래는 아트가 들어오면서 Slot 하나로 쓰던 것을 갈라낸 것들이다.
            // 손그림이라 가로로 긴 홈과 정사각 칸의 모양이 서로 다르다.
            Name,           // 이름칸
            Guage,          // 게이지 트랙 (파일 이름이 guage 다)
            LevelBadge,     // 나이 뱃지
            Slot2,          // 음식·알·상품 그리드의 정사각 칸

            // 최소화 창 (minimumbadge / slot3)
            MinimumBadge,   // 최소화했을 때 남는 띠
            Slot3,          // 그 띠 위의 칸

            Notice,         // 잠깐 떴다 사라지는 안내 문구의 바탕
        }

        /// <summary>역할별 기본 모서리 반지름. 아트가 없을 때만 쓴다.</summary>
        private static int RadiusOf(Shape s) => s switch
        {
            Shape.Panel or Shape.PanelBorder => 6,
            Shape.Slot or Shape.Name or Shape.Slot2 => 6,
            Shape.Badge or Shape.LevelBadge => 6,
            Shape.Fill or Shape.FillHappy or Shape.Guage => 5,
            Shape.Selection => 6,
            Shape.RowSelection => 6,
            _           => 4,
        };

        private static readonly Dictionary<Shape, Sprite> _art = new Dictionary<Shape, Sprite>();

        /// <summary>
        /// 역할 → 파일 이름. enum 이름을 그대로 소문자로 만들면 FillHappy 가 fillhappy 가 되어
        /// fill_happy.png 를 못 찾는다. 어긋나는 것만 여기에 적는다.
        /// </summary>
        private static string FileOf(Shape s) => s switch
        {
            Shape.PanelBorder => "panelborder",
            Shape.FillHappy   => "fill_happy",
            Shape.Selection   => "slotline",   // 고른 칸에 덧그리는 테두리
            Shape.RowSelection => "slotline2",
            _ => s.ToString().ToLowerInvariant(),
        };

        /// <summary>
        /// 역할에 맞는 스프라이트. 아트가 있으면 아트, 없으면 코드 생성.
        /// </summary>
        public static Sprite Of(Shape shape)
        {
            if (_art.TryGetValue(shape, out var art)) return art;

            art = Resources.Load<Sprite>("Ui/Shape/" + FileOf(shape));
            if (art != null) { _fromArt.Add(shape); art = WithBorder(art, BorderOf(shape)); }
            else art = shape switch
            {
                Shape.PanelBorder => Border(RadiusOf(shape), 1),
                Shape.Selection   => Border(RadiusOf(shape), 2),
                Shape.RowSelection => Border(RadiusOf(shape), 2),
                _                 => Fill(RadiusOf(shape)),
            };

            _art[shape] = art;
            return art;
        }

        /// <summary>
        /// 아트에 9-슬라이스 테두리가 안 잡혀 있을 때 코드로 잡아 주는 값 (원본 픽셀).
        ///
        /// 크게 늘어나는 도형만 적는다. 안내 문구 띠는 글자 길이에 따라 원본(79x54)의 세 배까지
        /// 늘어나는데, 테두리가 0 이면 둥근 끝이 통째로 늘어나 납작해진다.
        /// 값은 원본의 곡선이 끝나는 지점을 재서 넣었다.
        ///
        /// 임포터 설정으로 넣는 편이 낫지만 <b>.meta 를 손으로 고치면 유니티가 되돌려 놓는다</b>.
        /// 스프라이트 에디터에서 잡아 주면 그쪽이 이긴다 (아래 <see cref="WithBorder"/> 참고).
        /// </summary>
        private static Vector4 BorderOf(Shape s) => s switch
        {
            Shape.Notice => new Vector4(30, 26, 30, 26),
            _            => Vector4.zero,
        };

        /// <summary>
        /// 테두리가 없는 스프라이트를 같은 그림 · 같은 테두리로 다시 만든다.
        /// 아트에 이미 테두리가 잡혀 있으면 그대로 둔다 — 아트 쪽이 원본이다.
        /// </summary>
        private static Sprite WithBorder(Sprite art, Vector4 border)
        {
            if (art == null || border == Vector4.zero || art.border != Vector4.zero) return art;

            // 9-슬라이스는 FullRect 라야 한다. Tight 로 만들면 잘라 붙이지 못한다.
            return Sprite.Create(art.texture, art.rect, new Vector2(0.5f, 0.5f),
                                 art.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>
        /// 이 도형이 아트에서 왔는가.
        ///
        /// 아트에는 색이 이미 칠해져 있으므로 <c>UiTheme</c> 색으로 물들이면 두 색이 곱해져
        /// 탁해진다. 부르는 쪽은 이 값이 true 면 흰색으로 넘겨 원본 색을 그대로 내보낸다.
        /// </summary>
        public static bool IsArt(Shape shape)
        {
            Of(shape);   // 캐시를 채운다
            return _fromArt.Contains(shape);
        }

        private static readonly HashSet<Shape> _fromArt = new HashSet<Shape>();

        /// <summary>어떤 도형이 아트이고 어떤 것이 코드 생성인지. 리포트에 남겨 확인용으로 쓴다.</summary>
        public static string Describe()
        {
            var sb = new System.Text.StringBuilder();
            foreach (Shape s in System.Enum.GetValues(typeof(Shape)))
            {
                bool fromArt = Resources.Load<Sprite>("Ui/Shape/" + FileOf(s)) != null;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(s).Append(fromArt ? "=아트" : "=생성");
            }
            return sb.ToString();
        }

        /// <summary>속이 찬 둥근 사각형.</summary>
        public static Sprite Fill(int radius) => Get(radius, 0);

        /// <summary>테두리만 있는 둥근 사각형.</summary>
        public static Sprite Border(int radius, int thickness) => Get(radius, Mathf.Max(1, thickness));

        private static Sprite Get(int radius, int thickness)
        {
            radius = Mathf.Clamp(radius, 1, 64);
            int key = radius * 100 + thickness;
            if (_cache.TryGetValue(key, out var s) && s != null) return s;

            s = Build(radius, thickness);
            _cache[key] = s;
            return s;
        }

        private static Sprite Build(int radius, int thickness)
        {
            // 가운데가 몇 픽셀 남도록 여유를 준다. 9-슬라이스는 가장자리만 쓰므로 크기는 이걸로 충분하다.
            int margin = radius + 2;
            int size = margin * 2 + 4;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = thickness > 0 ? $"UiBorder{radius}_{thickness}" : $"UiFill{radius}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[size * size];
            float half = size * 0.5f;
            float inner = half - radius;      // 모서리 원의 중심이 놓이는 사각형의 반폭

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 둥근 사각형까지의 부호 있는 거리. 음수면 안쪽.
                    float dx = Mathf.Abs(x + 0.5f - half) - inner;
                    float dy = Mathf.Abs(y + 0.5f - half) - inner;
                    float d = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) +
                                         Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f))
                              + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;

                    // 거리 0 부근에서 1픽셀에 걸쳐 부드럽게 끊어 계단을 없앤다
                    float a = Mathf.Clamp01(0.5f - d);
                    if (thickness > 0) a -= Mathf.Clamp01(0.5f - (d + thickness));

                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);

            // PPU 1. 스크린 스페이스 캔버스에서 1 텍스처 픽셀 = 화면 1픽셀이 되어
            // 목업의 pt 값을 그대로 써도 두께가 맞는다.
            var border = new Vector4(margin, margin, margin, margin);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                                 1f, 0, SpriteMeshType.FullRect, border);
        }

        public static void ClearCache() => _cache.Clear();
    }
}
