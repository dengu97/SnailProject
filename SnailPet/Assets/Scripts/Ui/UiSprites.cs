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
            Slot,           // 안쪽 홈 — 이름칸·게이지 트랙·목록 행·음식 슬롯
            Badge,          // 알약 — 등급·나이·코인
            Fill,           // 게이지 채우기
            Button,         // 버튼 배경
        }

        /// <summary>역할별 기본 모서리 반지름. 아트가 없을 때만 쓴다.</summary>
        private static int RadiusOf(Shape s) => s switch
        {
            Shape.Panel or Shape.PanelBorder => 6,
            Shape.Slot  => 6,
            Shape.Badge => 6,
            Shape.Fill  => 5,
            _           => 4,
        };

        private static readonly Dictionary<Shape, Sprite> _art = new Dictionary<Shape, Sprite>();

        /// <summary>
        /// 역할에 맞는 스프라이트. 아트가 있으면 아트, 없으면 코드 생성.
        /// </summary>
        public static Sprite Of(Shape shape)
        {
            if (_art.TryGetValue(shape, out var art)) return art;

            art = Resources.Load<Sprite>("Ui/Shape/" + shape.ToString().ToLowerInvariant());
            if (art == null)
                art = shape == Shape.PanelBorder ? Border(RadiusOf(shape), 1) : Fill(RadiusOf(shape));

            _art[shape] = art;
            return art;
        }

        /// <summary>어떤 도형이 아트이고 어떤 것이 코드 생성인지. 리포트에 남겨 확인용으로 쓴다.</summary>
        public static string Describe()
        {
            var sb = new System.Text.StringBuilder();
            foreach (Shape s in System.Enum.GetValues(typeof(Shape)))
            {
                bool fromArt = Resources.Load<Sprite>("Ui/Shape/" + s.ToString().ToLowerInvariant()) != null;
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
