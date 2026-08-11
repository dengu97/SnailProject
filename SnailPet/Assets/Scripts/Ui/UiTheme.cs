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

            /// <summary>지금 나와 있는 달팽이의 모습. 등급 뱃지와 나이 뱃지 사이를 채운다.</summary>
            public static readonly RectInt Portrait  = new RectInt(16, 44, 141, 80);

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

        /// <summary>
        /// 최대화 상태에서 왼쪽에 붙는 목록 패널과 탭.
        ///
        /// 오른쪽 상세 패널은 디폴트 상태와 <b>완전히 같은 레이아웃</b>이다 (목업에서 확인).
        /// 그래서 최대화는 「디폴트 패널 왼쪽에 목록을 덧붙이는 것」으로 끝난다.
        /// 좌표는 목록 패널 왼쪽 위가 원점.
        /// </summary>
        public static class Max
        {
            /// <summary>탭 4개. 달팽이 정보 · 음식 목록 · 보유중인 알 · 상점.</summary>
            public static readonly RectInt[] Tabs =
            {
                new RectInt( 12, -25, 28, 25),
                new RectInt( 46, -25, 28, 25),
                new RectInt( 81, -25, 28, 25),
                new RectInt(116, -25, 28, 25),
            };

            public const int RowCount = 4;
            public static readonly RectInt Row = new RectInt(10, 27, 155, 40);
            public const int RowStep = 47;

            // ── 음식 목록 (탭 2) ──
            // 목업의 5번째 줄이 패널 아래로 잘려 있다. 「더 있다」는 표시라 세로로 스크롤한다.

            /// <summary>스크롤이 보이는 영역. 이 안에서만 그려지고 넘치면 잘린다.</summary>
            public static readonly RectInt FoodView = new RectInt(0, 30, PanelW, 184);

            public const int FoodCols = 4;
            public const int FoodStepX = 39, FoodStepY = 40;
            public static readonly RectInt FoodSlot = new RectInt(12, 4, 32, 32);

            /// <summary>미리 만들어 두는 칸 수. 이보다 많은 음식은 안 보인다.</summary>
            public const int FoodSlotPool = 32;

            /// <summary>칸 오른쪽 아래에 붙는 수량.</summary>
            public static readonly RectInt FoodCount = new RectInt(14, 20, 20, 12);

            // 아래는 행 왼쪽 위가 원점
            public static readonly RectInt RowThumb  = new RectInt(  5,  4, 32, 32);
            public static readonly RectInt RowName   = new RectInt( 42,  3, 92, 16);
            public static readonly RectInt RowRarity = new RectInt( 42, 21, 32, 14);
            public static readonly RectInt RowAge    = new RectInt( 78, 21, 32, 14);
            public static readonly RectInt RowSwap   = new RectInt(137,  9, 22, 22);
        }

        /// <summary>음식 탭의 오른쪽 상세 패널. 패널 왼쪽 위가 원점.</summary>
        public static class Food
        {
            public static readonly RectInt Favorite = new RectInt(  9,   8,  15, 15);
            public static readonly RectInt Name     = new RectInt(  0,   4, PanelW, 21);
            public static readonly RectInt Rarity   = new RectInt( 69,  28,  35, 12);
            public static readonly RectInt Preview  = new RectInt( 48,  40,  78, 71);

            public static readonly RectInt FullIcon   = new RectInt( 31, 121, 19, 18);
            public static readonly RectInt FullValue  = new RectInt( 45, 124, 38, 12);
            public static readonly RectInt HappyIcon  = new RectInt( 87, 121, 19, 18);
            public static readonly RectInt HappyValue = new RectInt(102, 124, 38, 12);

            public static readonly RectInt Info = new RectInt( 20, 148, 137, 36);

            public static readonly RectInt Feed  = new RectInt( 20, 190, 63, 22);
            public static readonly RectInt Buy   = new RectInt(102, 190, 24, 22);
            public static readonly RectInt Sell  = new RectInt(131, 190, 24, 22);
        }

        // ── 최대화에서 쓰는 색 ──
        public static readonly Color TabOn   = Hex("FFD966");   // 선택된 탭
        public static readonly Color TabOff  = Accent;          // 나머지 탭 (설정 버튼과 같은 색)
        public static readonly Color RowSlot = Hex("4472C4");   // 목록 행의 썸네일 자리
        public static readonly Color Selected = Hex("FF0000");  // 선택된 음식 칸의 테두리

        /// <summary>알 탭의 부화기 패널. 패널 왼쪽 위가 원점.</summary>
        public static class Egg
        {
            public static readonly RectInt Title = new RectInt(0, 4, PanelW, 21);

            /// <summary>부화 칸. 지금은 3개, 나중에 UnlockData 로 늘어난다.</summary>
            public static readonly RectInt[] Slots =
            {
                new RectInt( 12, 34, 47, 46),
                new RectInt( 64, 34, 47, 46),
                new RectInt(116, 34, 47, 46),
            };

            public static readonly RectInt Empty = new RectInt(10, 100, PanelW - 20, 20);
            public static readonly RectInt Buy   = new RectInt(137, 189, 25, 22);
        }

        /// <summary>화면 모서리에서 띄우는 여백.</summary>
        public const int ScreenMargin = 16;
    }
}
