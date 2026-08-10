using UnityEngine;

namespace SnailPet.Ui
{
    /// <summary>
    /// 목업에서 그대로 뽑아낸 색과 치수.
    ///
    /// 색은 눈대중이 아니라 UI.pptx 를 렌더해서 픽셀을 찍은 값이고,
    /// 치수는 파워포인트 도형의 좌표를 그대로 옮긴 값이다.
    /// 슬라이드1(예상 사이즈)로 환산하면 <b>디자인 1pt ≈ 1080p 화면의 1px</b> 이라
    /// 목업 좌표를 픽셀로 그대로 쓸 수 있다.
    /// </summary>
    public static class UiTheme
    {
        public static Color Hex(string rrggbb)
        {
            ColorUtility.TryParseHtmlString("#" + rrggbb, out var c);
            return c;
        }

        // ── 색 ──
        public static readonly Color PanelFill   = Hex("D0CECE");
        public static readonly Color PanelBorder = Hex("2F528F");
        public static readonly Color Slot        = Hex("AFABAB");   // 이름칸·게이지 트랙·나이 뱃지
        public static readonly Color BadgeDark   = Hex("767171");   // 등급 뱃지
        public static readonly Color GaugeFull   = Hex("A9D18E");   // 포만도
        public static readonly Color GaugeHappy  = Hex("F88888");   // 행복 지수
        public static readonly Color Accent      = Hex("F4B183");   // 설정 버튼·비선택 탭
        public static readonly Color Ink         = Hex("1A1A1A");   // 아이콘·본문 글자
        public static readonly Color OnBadge     = Color.white;

        // ── 치수 (목업 좌표) ──
        public const int PanelW = 173, PanelH = 220;
        public const int PanelRadius = 6, PanelBorderPx = 1;

        /// <summary>패널 왼쪽 위를 원점으로 한 각 요소의 위치·크기.</summary>
        public static class At
        {
            public static readonly RectInt NameField = new RectInt(20, 10, 131, 22);
            public static readonly RectInt RenameBtn = new RectInt(23, 13, 16, 16);
            public static readonly RectInt Rarity    = new RectInt(67, 29, 35, 12);
            public static readonly RectInt Age       = new RectInt(68, 126, 35, 12);

            public static readonly RectInt FullIcon  = new RectInt(16, 141, 19, 18);
            public static readonly RectInt FullBar   = new RectInt(29, 145, 127, 12);
            public static readonly RectInt HappyIcon = new RectInt(16, 163, 19, 18);
            public static readonly RectInt HappyBar  = new RectInt(29, 167, 127, 12);

            /// <summary>하단 액션 4개. 상세정보 · 옷장 · 유전정보 · 판매.</summary>
            public static readonly RectInt[] Actions =
            {
                new RectInt( 16, 188, 24, 22),
                new RectInt( 47, 188, 24, 22),
                new RectInt(102, 188, 24, 22),
                new RectInt(132, 188, 24, 22),
            };

            // 패널 밖에 걸치는 것들 (y 가 음수면 패널 위)
            public static readonly RectInt Settings = new RectInt(  9, -25, 28, 25);
            public static readonly RectInt Coin     = new RectInt( 47, -31, 90, 35);
            public static readonly RectInt Close    = new RectInt(152,  -9, 28, 28);
            public static readonly RectInt Maximize = new RectInt(152,  19, 28, 28);
        }

        /// <summary>화면 모서리에서 띄우는 여백.</summary>
        public const int ScreenMargin = 16;
    }
}
