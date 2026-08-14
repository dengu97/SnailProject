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

        /// <summary>버튼 아트(갈색) 위에 얹는 글자. 부화기 타이머도 같은 색을 쓴다.</summary>
        public static readonly Color OnButton    = Hex("E3D3BD");

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

            // 주의: 프리팹에는 이 표로 나타낼 수 없는 손 조정이 더 들어 있다 —
            // Portrait 0.7배, 아래 Actions 넷 1.2배, CoinIcon 0.8배의 localScale.
            // 여기 좌표는 그 프리팹에서 옮겨 온 것이지만 배율까지는 담지 못하므로,
            // 다시 구우면 크기가 1배로 돌아간다. 굽기 전에 Tools/Diff-PrefabLayout.ps1 로 확인할 것.

            /// <summary>하단 액션 4개. 상세정보 · 옷장 · 유전정보 · 판매.</summary>
            public static readonly RectInt[] Actions =
            {
                new RectInt( 16, 186, 24, 22),
                new RectInt( 78, 186, 24, 22),
                new RectInt(105, 186, 24, 22),
                new RectInt(132, 186, 24, 22),
            };

            // 패널 밖에 걸치는 것들 (y 가 음수면 패널 위)
            public static readonly RectInt Settings = new RectInt(  9, -23, 28, 25);

            /// <summary>
            /// 코인 줄이 차지하는 띠. <b>이 값의 y 가 위젯 상자를 얼마나 위로 늘릴지를 정한다</b> —
            /// 패널 위치와 <see cref="Above"/> 의 기준이 전부 여기에 매여 있으므로 함부로 바꾸지 말 것.
            /// 알약을 옮기고 싶으면 아래 <see cref="CoinPill"/> 셋을 고치면 된다.
            /// </summary>
            public static readonly RectInt Coin     = new RectInt( 47, -31, 90, 35);

            // 코인 알약과 그 안의 아이콘·숫자. 위 띠 안에서의 자리이며 위젯 상자 기준이다.
            // (프리팹에서 손으로 맞춘 값을 옮겨 온 것이라, 다시 구워도 그대로 나온다.)
            public static readonly RectInt CoinPill = new RectInt(54, 10, 63, 21);
            public static readonly RectInt CoinIcon = new RectInt(41,  0, 22, 22);
            public static readonly RectInt CoinText = new RectInt(72, 10, 45, 21);
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
                new RectInt(  6, -22, 28, 25),
                new RectInt( 37, -22, 28, 25),
                new RectInt( 69, -22, 28, 25),
                new RectInt(100, -22, 28, 25),
            };

            /// <summary>
            /// 목록이 보이는 영역. 이 안에서만 그려지고 넘치면 잘린다.
            /// 제목(y 8~24) 바로 아래에서 시작해 패널 끝까지 쓴다.
            /// 목업의 4행이 다 보이고 다섯째 줄이 살짝 걸쳐 「더 있다」가 드러난다.
            /// </summary>
            public static readonly RectInt RowView = new RectInt(0, 25, PanelW, PanelH - 25);

            public static readonly RectInt Row = new RectInt(10, 27, 155, 40);
            public const int RowStep = 47;

            /// <summary>미리 만들어 두는 행 수. 이보다 많은 달팽이는 안 보인다.</summary>
            public const int RowPool = 24;

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
        public static readonly Color Discount = Hex("FF0000");  // 할인가 (목업의 빨간 글자)

        /// <summary>
        /// 상점 탭. 두 단계다 — 카테고리를 고르고, 그 안의 상품을 고른다.
        ///
        /// 목업을 재 보니 <b>카테고리 행은 달팽이 목록 행과, 상품 그리드는 음식 그리드와
        /// 자리·크기가 같다</b> (행 155x40 step 46.3 · 칸 31.5 step 39.2x40).
        /// 그래서 여기에는 오른쪽 패널 좌표만 적는다.
        /// 상품 상세도 <see cref="Food"/> 와 같고 하단 버튼만 다르다.
        /// </summary>
        public static class Shop
        {
            /// <summary>카테고리 행 안에서의 이름. 왼쪽 정렬이다.</summary>
            public static readonly RectInt CategoryName = new RectInt(6, 10, 98, 21);

            // ── 오늘의 추천 (카테고리를 고르기 전) ──
            public static readonly RectInt Title    = new RectInt( 0,   4, PanelW, 21);
            public static readonly RectInt Rarity   = new RectInt(69,  45,     35, 12);
            public static readonly RectInt Preview  = new RectInt(48,  55,     78, 71);
            public static readonly RectInt Name     = new RectInt( 0, 127, PanelW, 21);
            public static readonly RectInt PickBuy  = new RectInt(53, 187,     63, 22);

            // 「[코인] 5,000 3,500」 한 줄. 원가에는 취소선이 그이고 할인가는 빨갛다.
            public static readonly RectInt PickCoin = new RectInt(36, 152, 20, 20);
            public static readonly RectInt PickWas  = new RectInt(58, 151, 42, 22);
            public static readonly RectInt PickNow  = new RectInt(102, 151, 42, 22);

            /// <summary>취소선. 가로 폭은 글자 폭에 맞춰 런타임에 줄인다.</summary>
            public static readonly RectInt PickStrike = new RectInt(58, 161, 42, 1);

            /// <summary>할인이 아닐 때는 가격 하나만 가운데에 놓는다.</summary>
            public static readonly RectInt PickOnly = new RectInt(66, 151, 42, 22);

            // ── 상품 상세의 하단 (구매하기 + 가격이 한 버튼 안에 들어간다) ──
            // Buy 만 패널 기준이고 나머지 셋은 <b>버튼 안</b> 좌표다.
            // 살 것이 없을 때 버튼을 통째로 끄면 글자와 코인도 같이 사라져야 한다.
            public static readonly RectInt Buy      = new RectInt(31, 190, 110, 22);
            public static readonly RectInt BuyLabel = new RectInt( 0,   0,  55, 22);
            public static readonly RectInt BuyCoin  = new RectInt(55,   2,  18, 18);
            public static readonly RectInt BuyCost  = new RectInt(75,   0,  35, 22);

            /// <summary>뒤로 가기. 목업에서 닫기 X 자리에 화살표가 들어온다.</summary>
            public static readonly RectInt Back = At.Close;
        }

        /// <summary>
        /// 옷장. 탭이 아니라 상세 패널의 「옷장」 버튼으로 들어가는 모드다.
        /// 들어가면 왼쪽은 목록 대신 옷장이 되고 오른쪽은 입은 모습이 된다.
        ///
        /// 이름칸·연필·등급·닫기는 <see cref="At"/> 와 좌표가 소수점까지 같아 그대로 쓴다.
        /// 그리드도 음식 그리드와 칸 크기·간격이 같고 시작 높이만 다르다 —
        /// 위에 부위 필터 줄이 한 줄 들어가기 때문이다.
        /// </summary>
        public static class Wardrobe
        {
            /// <summary>
            /// 부위 필터. 눌러서 켜고 끄며 <b>여러 개를 동시에</b> 켤 수 있다 (기본 전부 ON).
            /// 칸 수는 EnumData 의 AccessoriesType 행 수를 따라간다.
            /// </summary>
            public static readonly RectInt Filter = new RectInt(12, 25, 33, 16);
            public const int FilterStep = 38;

            /// <summary>보유 악세서리 그리드. 필터 줄 아래에서 시작한다.</summary>
            public static readonly RectInt View = new RectInt(0, 48, PanelW, PanelH - 48);

            // ── 오른쪽: 입은 모습 ──
            public static readonly RectInt Preview = new RectInt(28, 38, 125, 105);

            /// <summary>지금 낀 것들을 모아 보여 주는 상자.</summary>
            public static readonly RectInt WornBox   = new RectInt(  9, 155, PanelW - 18, 55);
            public static readonly RectInt WornTitle = new RectInt(  0, 155, PanelW,      16);
            public static readonly RectInt WornSlot  = new RectInt( 14, 174, 32, 32);
            public const int WornStep = 38;
        }

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

            /// <summary>
            /// 「부화시킬 알이 없습니다」. 목업은 부화기 패널에 두었지만, 알을 하나도
            /// 안 가졌을 때 비는 것은 왼쪽 목록이라 그쪽으로 옮겼다(2026-08-13 결정).
            /// 좌표는 왼쪽 패널 기준이며, 그리드 안에 넣을 때 <see cref="Max.FoodView"/> 만큼 당긴다.
            /// </summary>
            public static readonly RectInt Empty = new RectInt(10, 100, PanelW - 20, 20);
            public static readonly RectInt Buy   = new RectInt(137, 189, 25, 22);
        }

        /// <summary>
        /// 달팽이 상세보기. 상세 패널의 유전정보 버튼으로 들어가는 모드다.
        /// 왼쪽은 파츠마다 설명까지 펼친 목록, 오른쪽은 초상 아래 한 줄짜리 목록이다.
        ///
        /// 이름칸·연필·등급·닫기는 <see cref="At"/> 와 같고, 초상 자리는 옷장과 같다.
        /// </summary>
        public static class Gene
        {
            // ── 왼쪽: 「보유 특징」 ──
            // 행 하나가 동그란 썸네일 + 가로로 긴 바로 되어 있고, 썸네일이 바 왼쪽에 걸친다.
            public static readonly RectInt Row = new RectInt(7, 27, 158, 47);
            public const int RowStep = 48;

            // 아래는 행 왼쪽 위가 원점
            public static readonly RectInt RowThumb  = new RectInt(  0,  7, 25, 25);
            public static readonly RectInt RowBar    = new RectInt( 19,  0, 140, 40);
            // 글자는 썸네일(0~25)을 피해 시작한다. 목업 값(x=20)은 그림 위로 올라와 있었다.
            public static readonly RectInt RowName   = new RectInt( 30,  3,  85, 18);
            public static readonly RectInt RowInfo   = new RectInt( 30, 18, 128, 18);
            public static readonly RectInt RowRarity = new RectInt(117,  4, 35, 12);

            // ── 오른쪽: 초상 + 한 줄짜리 목록 ──
            public static readonly RectInt Preview = new RectInt(27, 38, 125, 105);

            public static readonly RectInt Slim = new RectInt(20, 141, 145, 17);
            public const int SlimStep = 18;

            // 아래는 줄 왼쪽 위가 원점
            public static readonly RectInt SlimThumb  = new RectInt( 0, 0, 16, 16);
            public static readonly RectInt SlimBar    = new RectInt(10, 2, 123, 14);
            public static readonly RectInt SlimRarity = new RectInt(18, 3, 30, 11);
            public static readonly RectInt SlimName   = new RectInt(52, 0, 90, 17);
        }

        /// <summary>
        /// 구매·판매를 묻는 팝업. 목업에서 둘은 <b>제목과 가격 부호만</b> 다르므로 하나로 만든다.
        /// 좌표는 팝업 패널 왼쪽 위가 원점. 위젯과 달리 화면 한가운데에 뜬다.
        /// </summary>
        public static class Popup
        {
            public const int W = 241, H = 145;

            public static readonly RectInt Title = new RectInt(0, 19, W, 21);

            // 수량 조절. 아직 +/- 아트가 없어 글자로 그린다.
            public static readonly RectInt Minus  = new RectInt( 76, 56, 16, 16);
            public static readonly RectInt Count  = new RectInt( 93, 56, 52, 18);
            public static readonly RectInt Plus   = new RectInt(148, 56, 16, 16);

            // 값. 코인이 알약 왼쪽 끝에 걸쳐 놓인다.
            public static readonly RectInt CostPill = new RectInt(77, 83, 85, 18);
            public static readonly RectInt CostIcon = new RectInt(73, 82, 21, 21);
            public static readonly RectInt CostText = new RectInt(95, 83, 65, 18);

            public static readonly RectInt No  = new RectInt( 37, 112, 63, 22);
            public static readonly RectInt Yes = new RectInt(135, 112, 63, 22);

            /// <summary>닫기 X. 패널 오른쪽 위에 걸친다.</summary>
            public static readonly RectInt Close = new RectInt(220, -6, 28, 28);

            // ── 이름 변경 ── 같은 판을 쓰고 가운데만 다르다 (목업에서 크기·닫기 자리가 같다)
            public static readonly RectInt RenameTitle = new RectInt( 0,  25, W, 21);
            public static readonly RectInt RenameField = new RectInt(42,  62, 161, 20);
            public static readonly RectInt RenameOk    = new RectInt(91, 112,  63, 22);

            // ── 알 부화 ── 역시 같은 판이다. 목업(UI.pptx 12쪽)을 실측해 그대로 옮겼다.
            // 그 슬라이드는 왼쪽이 연출 중, 오른쪽이 결과이며 판·닫기·버튼 자리가 서로 같다.
            public static readonly RectInt HatchTitle  = new RectInt(  0,  13, W, 21);
            public static readonly RectInt HatchOk     = new RectInt( 92, 110, 63, 22);

            /// <summary>연출 중에 흔들리는 알.</summary>
            public static readonly RectInt HatchEgg    = new RectInt(102,  50, 38, 38);

            /// <summary>
            /// 연출이 끝나고 나오는 갓 태어난 달팽이.
            /// 목업 실측은 (80, 25, 86, 73) 인데 띄워 보니 너무 커서 70% 로 줄였다.
            /// 가운데는 그대로 두고 크기만 줄인 값이다.
            /// </summary>
            public static readonly RectInt HatchSnail  = new RectInt( 93,  36, 60, 51);
            public static readonly RectInt HatchRarity = new RectInt(105,  90, 35, 12);
        }

        /// <summary>
        /// 설정 화면. 옷장·상세보기처럼 좌우 패널을 통째로 쓴다 (UI.pptx 13쪽 실측).
        /// 왼쪽은 이 달팽이에 걸리는 설정, 오른쪽은 게임 전체 설정이다.
        /// </summary>
        public static class Setting
        {
            public const int RowW = 155, RowH = 28;
            public const int LeftX = 10, RightX = 9;

            /// <summary>왼쪽은 구역이 둘이라 행 간격이 일정하지 않다. 그래서 y 를 그대로 적는다.</summary>
            public static readonly RectInt EggTitle    = new RectInt(7, 29, 98, 19);
            public static readonly RectInt BubbleTitle = new RectInt(7, 83, 98, 19);
            public static readonly int[] LeftRows  = { 49, 103, 136, 169 };

            public static readonly int[] RightRows = { 33, 68, 102, 137, 171 };

            // 행 안쪽 (행 왼쪽 위가 원점)
            public static readonly RectInt Label = new RectInt(  6, 4, 119, 19);
            public static readonly RectInt Check = new RectInt(133, 8,  13, 13);
            public static readonly RectInt Arrow = new RectInt(134, 9,   8, 11);
        }

        /// <summary>화면 모서리에서 띄우는 여백.</summary>
        public const int ScreenMargin = 16;
    }
}
