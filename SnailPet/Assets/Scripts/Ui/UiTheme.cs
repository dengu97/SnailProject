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

        /// <summary>가진 것이 없어 죽여 놓는 그림. 아트 색은 그대로 두고 투명도만 낮춘다.</summary>
        public static readonly Color Faded       = new Color(1f, 1f, 1f, 0.35f);

        // ── 치수 (목업 좌표) ──
        public const int PanelW = 173, PanelH = 220;

        /// <summary>
        /// 왼쪽 목록 판의 폭. 오른쪽에 스프링 제본이 붙은 그림(panel2)이라 그만큼 넓다.
        /// 왼쪽 끝은 그대로 두고 오른쪽으로만 자라, 남는 20px 이 상세 판 위로 겹친다 —
        /// 그 겹치는 자리가 노트의 가운데 제본이 된다. 그림 비율대로면 186 인데, 그러면 두 판의
        /// 너덜한 가장자리 사이에 바탕화면이 비치는 틈이 9px 남아 그만큼 더 물렸다.
        /// </summary>
        public const int ListPanelW = 193;
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

            // 게이지는 알 칸이 들어오면서 짧아졌다. 오른쪽 끝(29+85=114)에서 칸까지 6px 이 뜬다.
            public static readonly RectInt FullIcon  = new RectInt(16, 141, 19, 18);
            public static readonly RectInt FullBar   = new RectInt(29, 145, 85, 12);
            public static readonly RectInt HappyIcon = new RectInt(16, 163, 19, 18);
            public static readonly RectInt HappyBar  = new RectInt(29, 167, 85, 12);

            /// <summary>
            /// 오늘 낳을 수 있는 알. 게이지 두 줄 오른쪽에 붙는 네모 칸으로,
            /// 알 그림 아래에 「남은 수 / 전체 수」가 앉는다.
            /// </summary>
            public static readonly RectInt EggBox   = new RectInt(120, 139, 36, 42);
            public static readonly RectInt EggIcon  = new RectInt(128, 142, 20, 20);
            public static readonly RectInt EggCount = new RectInt(120, 165, 36, 12);

            // 주의: 프리팹에는 이 표로 나타낼 수 없는 손 조정이 더 들어 있다 —
            // Portrait 0.7배, 아래 Actions 넷 1.2배, CoinIcon 0.8배의 localScale.
            // 여기 좌표는 그 프리팹에서 옮겨 온 것이지만 배율까지는 담지 못하므로,
            // 다시 구우면 크기가 1배로 돌아간다. 굽기 전에 Tools/Diff-PrefabLayout.ps1 로 확인할 것.

            /// <summary>
            /// 파츠(외형) 도감. 달팽이 도감 버튼 바로 옆이다 —
            /// 아래 <see cref="Actions"/> 첫 칸과 둘째 칸 사이의 빈자리에 들어간다.
            /// </summary>
            public static readonly RectInt PartsBook = new RectInt(45, 186, 24, 22);

            /// <summary>
            /// 하단 액션 4개. 상세정보 · 옷장 · 유전정보 · 판매.
            ///
            /// 사이에 <see cref="PartsBook"/> 이 끼어 실제로는 <b>다섯 개가 한 줄</b>이다.
            /// 그래서 x 는 16부터 132까지 <b>29씩</b> 고르게 나눠 놓는다 —
            /// 프리팹에서 손으로 옮기다 31·31·27·27 로 들쭉날쭉해진 것을 되돌린 값이다(2026-08-27).
            /// 양 끝(16·132)은 그대로 두어 줄 전체의 자리는 안 움직인다.
            /// </summary>
            public static readonly RectInt[] Actions =
            {
                new RectInt( 16, 186, 24, 22),
                new RectInt( 74, 186, 24, 22),
                new RectInt(103, 186, 24, 22),
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

            /// <summary>
            /// 짝꿍 슬롯. 닫기·최대화와 같은 x 라 오른쪽 가장자리에 나란히 걸친다.
            /// 비어 있으면 +, 놓여 있으면 그 달팽이 얼굴이 들어간다.
            /// </summary>
            public static readonly RectInt MateSlot = new RectInt(152, 97, 28, 28);
        }

        /// <summary>
        /// 최소화 창. 코인 줄만 남기고 패널 자리에 띠 하나가 들어선다.
        /// 좌표는 상세 패널과 같은 기준(위젯 상자의 왼쪽 위)이라 띠가 패널 자리에 딱 맞는다.
        ///
        /// 칸 크기는 아트에서 그대로 옮겼다 — 띠(minimumbadge)가 400x131 이고 그 위에
        /// slot3(119x116) 셋이 얹히므로, 띠를 줄인 비율(0.3)을 그대로 곱했다.
        /// </summary>
        public static class Mini
        {
            /// <summary>
            /// 띠. y 는 코인 줄 바로 아래 — 패널이 시작하던 자리다.
            /// x 는 코인 아이콘 왼쪽 끝에 맞췄다 (패널보다 좁아 오른쪽에 붙이면 화면 구석에서
            /// 코인 줄만 따로 노는 모양이 된다). 코인 줄은 최소화하는 동안 이 띠 가운데로
            /// 옮겨진다 — SnailUi.CenterCoinRow 참고.
            /// </summary>
            public static readonly RectInt Bar = new RectInt(At.CoinIcon.x, -At.Coin.y, 120, 39);

            /// <summary>첫 칸. 띠 안에서의 자리다.</summary>
            public static readonly RectInt Slot = new RectInt(3, 2, 36, 35);

            /// <summary>칸 사이 간격(칸 폭 + 틈).</summary>
            public const int SlotStep = 39;

            /// <summary>칸 오른쪽 아래에 붙는 개수 뱃지. 음식 그리드와 같은 크기다.</summary>
            public static readonly RectInt Count = new RectInt(21, 20, 15, 15);

            /// <summary>「삭제」 문구. 칸 한가운데에 가로로 걸친다.</summary>
            public static readonly RectInt Ask = new RectInt(2, 11, 32, 13);

            /// <summary>빈 칸 수. 마지막 자리에는 최대화 버튼이 들어간다.</summary>
            public const int Slots = 2;
        }

        /// <summary>
        /// 멀티플레이어. 옷장·도감처럼 좌우 패널을 통째로 쓴다.
        ///
        /// 목업(PPT 20·21쪽)은 실행 화면 캡처 위에 그린 것이라 캡처 배율(약 1.15배)이 섞여 있다.
        /// 그래서 값을 그대로 옮기지 않고 <b>다른 화면의 관례에 맞춰 반올림</b>했다 —
        /// 여백은 목록 패널과 같게, 줄 높이·간격은 목업 비율대로.
        /// </summary>
        public static class Multi
        {
            // ── 왼쪽: 탭 둘 + 목록 ──
            // 둘을 붙여 놓고 패널 한가운데에 앉힌다: (173 - (64*2 + 5)) / 2 = 20
            public static readonly RectInt FriendTab = new RectInt(20, 3, 64, 23);
            public static readonly RectInt LobbyTab  = new RectInt(89, 3, 64, 23);

            /// <summary>
            /// 목록이 보이는 영역. 탭 줄 아래부터 패널 끝까지다.
            /// 여기서 잘리고 넘치면 스크롤된다 — 줄 수가 6을 넘어도 밖으로 새지 않는다.
            /// (좌표는 <see cref="Max.RowView"/> 안이므로 그 높이에서 뺀다)
            /// </summary>
            public static readonly RectInt View = new RectInt(0, 30, PanelW, Max.RowView.height - 30);

            /// <summary>목록 줄. 자리는 <b>스크롤 내용 기준</b>이다.</summary>
            public static readonly RectInt Row = new RectInt(11, 4, 151, 26);
            public const int RowStep = 28;

            /// <summary>
            /// 미리 만들어 두는 줄 수. 한 번에 보이는 것은 다섯 남짓이고 나머지는 굴려서 본다.
            /// <b>스크롤 범위는 이 수를 넘으면 안 된다</b> — 넘기면 줄이 없는 빈 곳까지 굴러가
            /// 목록이 사라진 것처럼 보인다.
            /// </summary>
            public const int RowCount = 20;

            // 아래 둘은 <b>줄 왼쪽 위가 원점</b>이다 (목록 행·유전정보 줄과 같은 규칙).
            public static readonly RectInt RowName   = new RectInt(  8, 3, 112, 20);
            public static readonly RectInt RowButton = new RectInt(126, 3,  20, 20);

            // ── 오른쪽: 「방」 ──
            public static readonly RectInt Title  = new RectInt(0, 8, PanelW, 21);
            public static readonly RectInt Button = new RectInt(14, 40, 145, 23);
            public const int ButtonStep = 29;

            // ── 오른쪽: 방에 들어간 뒤 ──
            /// <summary>방 이름 줄과 그 오른쪽의 나가기.</summary>
            public static readonly RectInt RoomName = new RectInt(14, 8, 118, 23);
            public static readonly RectInt RoomOut  = new RectInt(136, 8, 23, 23);

            /// <summary>방 이름을 고치는 연필. 이름칸 안 왼쪽에 얹는다 (달팽이 이름칸과 같은 꼴).</summary>
            public static readonly RectInt RoomRename = new RectInt(17, 11, 16, 16);

            /// <summary>
            /// 방 코드. 이름 바로 밑에 가운데로 놓는다. 누르면 복사되므로 손가락이 닿을 만큼은 둔다.
            /// </summary>
            public static readonly RectInt RoomCode = new RectInt(51, 33, 70, 17);

            /// <summary>
            /// 참가자 줄. 달팽이 그림 + 이름 + 돋보기. max 5.
            /// <b>방 코드(RoomCode) 아래에서 시작해야 한다</b> — 예전에는 코드 위로 올라와 가렸다.
            /// 다섯 줄이 패널 안에 들어오도록 간격도 같이 좁혔다.
            /// </summary>
            public static readonly RectInt Member     = new RectInt(14, 54, 145, 32);
            public const int MemberStep = 33, MemberCount = 5;

            // 아래는 줄 왼쪽 위가 원점이다.
            // 이름은 두 줄이다 — 위가 스팀 닉네임(작게), 아래가 달팽이 이름.
            public static readonly RectInt MemberFace  = new RectInt(  4, 2, 28, 28);
            public static readonly RectInt MemberSteam = new RectInt( 38, 3, 78, 13);
            public static readonly RectInt MemberName  = new RectInt( 38, 15, 78, 15);
            public static readonly RectInt MemberZoom  = new RectInt(120, 6, 20, 20);
        }

        /// <summary>
        /// 잠깐 떴다 사라지는 안내 문구. 글자 길이에 맞춰 <b>가로로만</b> 늘어나고
        /// 높이와 여백은 고정이다.
        /// </summary>
        public static class Notice
        {
            public const int Height = 26;
            public const int PadX = 14;
            public const int MinWidth = 60;

            /// <summary>
            /// 띠의 최대 폭. <b>펼쳤을 때와 접었을 때가 다르다.</b>
            ///
            /// 띠는 위젯 안에서 가운데를 잡는데, 접으면 그 가운데가 오른쪽 상세 판이라
            /// 오른쪽으로 93px 밖에 안 남는다. 320 을 그대로 쓰면 화면 밖으로 넘친다.
            /// 접었을 때는 186(=93×2)까지만 쓰고, 넘치는 글은 두 줄로 접는다.
            /// </summary>
            public const int MaxWidth = 320;
            public const int MaxWidthFolded = 186;
            public const int FontSize = 12;

            /// <summary>한 줄이 늘어날 때마다 띠가 이만큼 높아진다.</summary>
            public const int LineStep = 15;

            /// <summary>몇 줄까지 접을지. 그보다 길면 넘친다 — 문구를 줄일 일이다.</summary>
            public const int MaxLines = 2;

            /// <summary>
            /// 위젯 안에서의 자리.
            ///
            /// 세로는 위젯 한가운데를 <b>앵커로</b> 잡는다 — 최소화로 상자가 줄어도 따라온다.
            /// 가로는 두 가지다. 위젯 상자는 <b>최대화 기준 폭</b>이라 상자의 한가운데는 접었을 때
            /// 빈 왼쪽 절반에 떨어진다. 그래서 접었을 때는 오른쪽 끝에서 상세 판 한가운데까지
            /// 되돌아오고(<see cref="Offset"/>), 펼쳤을 때는 두 판 사이(노트 제본)로 간다
            /// (<see cref="OffsetMax"/>).
            /// </summary>
            public static readonly Vector2 Anchor = new Vector2(1f, 0.5f);
            public static readonly Vector2 Offset = new Vector2(-(At.Close.xMax - PanelW * 0.5f), 0f);
            public static readonly Vector2 OffsetMax = new Vector2(-At.Close.xMax, 0f);
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
                new RectInt(132, -22, 28, 25),   // 파티(멀티플레이어)
            };

            /// <summary>
            /// 목록이 보이는 영역. 이 안에서만 그려지고 넘치면 잘린다.
            /// 제목(y 8~24) 바로 아래에서 시작해 패널 끝까지 쓴다.
            /// 목업의 4행이 다 보이고 다섯째 줄이 살짝 걸쳐 「더 있다」가 드러난다.
            /// </summary>
            public static readonly RectInt RowView = new RectInt(0, 25, PanelW, PanelH - 25);

            /// <summary>목록이 비었을 때 한가운데에 뜨는 안내. 음식·알이 같은 자리를 쓴다.</summary>
            public static readonly RectInt Empty = new RectInt(10, 100, PanelW - 20, 20);

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

            /// <summary>
            /// 수량이 앉는 동그란 배지. 칸(32x32) 오른쪽 아래 모서리에 살짝 걸친다.
            /// 글자만 얹으면 아이콘 그림과 선택 테두리에 묻혀 안 보인다.
            /// </summary>
            public static readonly RectInt FoodCountBadge = new RectInt(19, 19, 15, 15);

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
            public static readonly RectInt Favorite = new RectInt( 20,   8,  15, 15);   // 9 는 스프링에 물렸다
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
        // 할인가. 목업의 순빨강(FF0000)은 갈색 판 위에서 너무 튀어 글자가 안 읽혔다.
        public static readonly Color Discount = Hex("AB5352");

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
            // 원가는 코인 아이콘에서 조금 떼어 놓는다 — 붙여 두면 아이콘에 먹힌 것처럼 보인다.
            public static readonly RectInt PickCoin = new RectInt(36, 152, 20, 20);
            public static readonly RectInt PickWas  = new RectInt(64, 151, 42, 22);
            public static readonly RectInt PickNow  = new RectInt(106, 151, 42, 22);

            /// <summary>취소선. 가로 폭은 글자 폭에 맞춰 런타임에 줄인다.</summary>
            public static readonly RectInt PickStrike = new RectInt(64, 161, 42, 1);

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

            /// <summary>
            /// 알 상세의 「확률」 버튼. 음식의 포만·행복이 앉는 자리에 들어간다 —
            /// 알에는 그 값이 없어 비어 있던 칸이다.
            /// </summary>
            public static readonly RectInt Rates = new RectInt(59, 118, 55, 20);
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
            // 메인 상세의 초상과 같은 자리·크기를 쓴다. 화면을 오갈 때 달팽이가 제자리에
            // 있어야 자연스럽다. (실제 배율까지 맞추는 것은 SnailUi 가 살아날 때 한다 —
            // 메인 초상은 프리팹에서 손으로 줄여 놓았고 그 조정까지 따라가야 하기 때문이다.)
            public static readonly RectInt Preview = At.Portrait;

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

            /// <summary>
            /// 부화 칸. 지금은 3개, 나중에 UnlockData 로 늘어난다.
            ///
            /// 왼쪽 끝은 공책 스프링이 지나가는 자리라 비워 둔다 — 12 에 두었더니 첫 칸이
            /// 스프링에 물렸다. 칸을 조금 줄이고 안쪽으로 밀어 스프링을 피한다(2026-08-29).
            /// 프리팹에는 예전 자리로 구워져 있어 SnailUi 가 살아날 때 다시 놓는다.
            /// </summary>
            public static readonly RectInt[] Slots =
            {
                new RectInt( 25, 34, 42, 41),
                new RectInt( 72, 34, 42, 41),
                new RectInt(119, 34, 42, 41),
            };

            /// <summary>
            /// 「부화시킬 알이 없습니다」. 목업은 부화기 패널에 두었지만, 알을 하나도
            /// 안 가졌을 때 비는 것은 왼쪽 목록이라 그쪽으로 옮겼다(2026-08-13 결정).
            /// 좌표는 왼쪽 패널 기준이며, 그리드 안에 넣을 때 <see cref="Max.FoodView"/> 만큼 당긴다.
            /// </summary>
            public static readonly RectInt Empty = Max.Empty;
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
            // 옷장과 같은 이유로 메인 상세의 초상 자리를 그대로 쓴다.
            public static readonly RectInt Preview = At.Portrait;

            /// <summary>
            /// 파츠 한 줄. 도감의 파츠 목록(<see cref="Guide.PartRow"/>)·설명과 같은 높이에서
            /// 시작한다 — 세 화면이 같은 자리를 나눠 쓰므로 하나를 옮기면 나머지도 옮겨야 한다.
            /// </summary>
            public static readonly RectInt Slim = new RectInt(20, 134, 145, 17);
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

            /// <summary>
            /// 예/아니오만 있는 물음 팝업의 문구.
            ///
            /// 제목 자리(<see cref="Title"/>)에 두면 21px 높이 한가운데에 맞춰져 <b>글이 위에 붙어</b>
            /// 보인다. 여기서는 위 여백부터 버튼 위까지를 통째로 차지해, 두 줄짜리 문구도
            /// 버튼 위 공간의 한가운데에 온다.
            /// </summary>
            public static readonly RectInt Ask = new RectInt(0, 14, W, No.y - 18);

            /// <summary>닫기 X. 패널 오른쪽 위에 걸친다.</summary>
            public static readonly RectInt Close = new RectInt(220, -6, 28, 28);

            // ── 이름 변경 ── 같은 판을 쓰고 가운데만 다르다 (목업에서 크기·닫기 자리가 같다)
            public static readonly RectInt RenameTitle = new RectInt( 0,  25, W, 21);
            public static readonly RectInt RenameField = new RectInt(42,  62, 161, 20);
            public static readonly RectInt RenameOk    = new RectInt(91, 112,  63, 22);

            // ── 도감 완성 ── 판이 조금 더 크다 (목업 16쪽). 알 부화와 자리를 겹쳐 쓰되
            // 이름 뱃지가 하나 더 붙고 확인 버튼이 그만큼 내려간다.
            public const int GuideDoneH = 170;

            // ── 남의 달팽이 한 장 (방 목록의 돋보기) ──
            //
            // 왼쪽에 초상·등급, 오른쪽에 파츠 넉 줄. 그래서 판이 기본보다 크다.
            public const int GuestH = 160;

            public static readonly RectInt GuestSteam  = new RectInt(  0,  10, W, 22);
            public static readonly RectInt GuestName   = new RectInt( 66,  36, 110, 22);
            public static readonly RectInt GuestFace   = new RectInt( 20,  66, 62, 62);
            public static readonly RectInt GuestRarity = new RectInt( 34, 132, 35, 12);

            /// <summary>파츠 넉 줄. 유전정보 줄과 같은 모양이다.</summary>
            public static readonly RectInt GuestPartRow    = new RectInt( 96, 66, 123, 14);
            public const int GuestPartStep = 18, GuestPartCount = 4;

            public static readonly RectInt GuestPartIcon   = new RectInt( 86, 64, 16, 16);
            public static readonly RectInt GuestPartRarity = new RectInt(103, 67, 30, 11);
            public static readonly RectInt GuestPartName   = new RectInt(131, 65, 88, 17);

            // ── 짝꿍 슬롯 ──
            //
            // 제목·설명 아래에 달팽이 목록이 들어가고 바닥에 버튼 둘이 선다.
            // 목록 줄은 달팽이 목록과 같은 모양(Max.Row*)을 그대로 쓴다.

            public const int MateH = 216;

            public static readonly RectInt MateTitle = new RectInt( 0, 12, W, 21);
            public static readonly RectInt MateInfo  = new RectInt(16, 36, W - 32, 24);

            /// <summary>목록이 보이는 영역. 이 안에서만 그려지고 넘치면 잘린다.</summary>
            public static readonly RectInt MateView  = new RectInt(34, 64, 173, 108);

            /// <summary>줄 하나. 자리는 목록 안쪽 기준이고 크기는 달팽이 목록과 같다.</summary>
            public static readonly RectInt MateRow   = new RectInt(9, 4, 155, 40);

            /// <summary>미리 만들어 두는 줄 수. 자격이 되는 달팽이가 이보다 많으면 안 보인다.</summary>
            public const int MateRowPool = 12;

            /// <summary>자격이 되는 달팽이가 없을 때 한가운데에 뜨는 안내.</summary>
            public static readonly RectInt MateEmpty = new RectInt(20, 104, W - 40, 20);

            public static readonly RectInt MateClear = new RectInt( 22, 180, 90, 24);
            public static readonly RectInt MateOk    = new RectInt(129, 180, 90, 24);

            /// <summary>도움말 물음표. 닫기 X 의 반대쪽인 왼쪽 위 모서리에 걸친다(목업).</summary>
            public static readonly RectInt MateHelp  = new RectInt(6, 3, 24, 24);

            // ── 도움말 ──
            //
            // 컨텐츠마다 물음표를 누르면 뜬다. 무엇이 적히는지는 ContentsGuide 시트가 정하고,
            // 글 길이가 제각각이라 <b>자리는 글자 높이를 재서 런타임에 쌓는다</b>.
            // 그래서 여기 있는 것은 판 크기와 글이 들어갈 상자뿐이다.

            public const int HelpH = 216;

            public static readonly RectInt HelpTitle = new RectInt( 0, 14, W, 24);
            public static readonly RectInt HelpView  = new RectInt(20, 46, W - 40, 156);

            /// <summary>글 한 덩이(소제목+본문)의 가로. 세로는 글에 따라 정해진다.</summary>
            public const int HelpBlockW = W - 48;

            /// <summary>소제목과 본문 사이, 그리고 덩이와 덩이 사이의 틈(px).</summary>
            public const int HelpLead = 2, HelpGap = 9;

            /// <summary>미리 만들어 두는 덩이 수. 한 GroupId 의 줄이 이보다 많으면 안 보인다.</summary>
            public const int HelpBlockPool = 8;

            // ── 알 등장 확률 ──
            //
            // 위에 부위 토글 넉 줄(껍질·몸·더듬이·얼굴), 아래에 그 부위의 파츠 목록.
            // 한 부위에 파츠가 마흔 가까이 되므로 목록은 굴려서 본다.

            public const int RatesH = 236;

            public static readonly RectInt RatesTitle = new RectInt( 0, 12, W, 22);
            public static readonly RectInt RatesTab   = new RectInt(12, 40, 52, 18);
            public const int RatesTabStep = 55;

            public static readonly RectInt RatesView  = new RectInt(14, 66, W - 28, 154);

            /// <summary>줄 하나. 자리는 목록 안쪽 기준이다.</summary>
            public static readonly RectInt RatesRow = new RectInt(3, 3, 207, 22);

            // 줄 안쪽. 이름 · 등급 · 확률 순으로 앉는다 (줄 왼쪽 위가 원점).
            public static readonly RectInt RatesName    = new RectInt( 12, 0,  95, 22);
            public static readonly RectInt RatesRarity  = new RectInt(112, 4,  46, 14);
            public static readonly RectInt RatesPercent = new RectInt(162, 0,  34, 22);
            public const int RatesRowStep = 26;

            /// <summary>미리 만들어 두는 줄 수. 한 부위의 파츠가 이보다 많으면 안 보인다.</summary>
            public const int RatesRowPool = 48;

            public static readonly RectInt DoneName = new RectInt(57, 107, 131, 22);
            public static readonly RectInt DoneOk   = new RectInt(92, 137,  63, 22);

            // ── 보상 수령 ── 받은 것들을 가운데로 모아 보여 준다 (목업 17쪽).
            public static readonly RectInt RewardSlot = new RectInt(0, 53, 32, 32);
            public const int RewardStep = 37;

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
        /// 달팽이 도감. 옷장·상세보기처럼 좌우 패널을 통째로 쓴다 (UI.pptx 14쪽 실측).
        /// 왼쪽은 도감 목록, 오른쪽은 고른 칸의 상세다.
        /// </summary>
        public static class Guide
        {
            // ── 왼쪽: 목록 ──
            public static readonly RectInt Row = new RectInt(11, 29, 151, 32);
            public const int RowStep = 40;

            /// <summary>미리 만들어 두는 줄 수. 이보다 많은 도감은 안 보인다.</summary>
            public const int RowPool = 24;

            // 아래는 줄 왼쪽 위가 원점
            public static readonly RectInt RowName   = new RectInt(  4, 6, 100, 19);
            public static readonly RectInt RowRarity = new RectInt(110, 9,  37, 14);

            /// <summary>채운 줄에 찍는 도장. 아트가 들어오면 icon_complete 로 바뀐다.</summary>
            public static readonly RectInt RowDone   = new RectInt( 92, 8,  16, 16);

            // ── 오른쪽: 고른 칸 ──
            public static readonly RectInt Title  = new RectInt(21, 11, 131, 22);

            /// <summary>등급은 이름 바로 밑에 가운데로.</summary>
            public static readonly RectInt Rarity = new RectInt(69, 35, 35, 12);

            /// <summary>
            /// 안 채운 칸의 실루엣 자리. 목업 크기 그대로다.
            ///
            /// 채운 칸의 달팽이는 이 자리가 아니라 <see cref="At.Portrait"/> 를 따른다 —
            /// 화면마다 달팽이 크기가 달라 보이면 안 되기 때문이다. 실루엣은 「무엇이
            /// 들어올 자리인가」를 보여 주는 그림이라 크게 두는 편이 낫다.
            /// </summary>
            public static readonly RectInt Image  = new RectInt(28, 28, 125, 105);

            /// <summary>실루엣 오른쪽의 전환 버튼. 설명↔파츠 목록을 오간다.</summary>
            public static readonly RectInt Toggle = new RectInt(130, 106, 23, 23);

            /// <summary>
            /// 채운 칸에 이름 위로 찍히는 도장(icon_complete2).
            /// 목록 줄의 <see cref="RowDone"/> 와 짝이지만 그림이 다르다 — 이쪽은 가로로 긴 스탬프다.
            /// </summary>
            public static readonly RectInt Done = new RectInt(6, 2, 58, 42);

            // ── 아래쪽: 설명과 보상 (기본 상태) ──

            /// <summary>설명 뒤에 까는 홈. 글자보다 사방으로 조금씩 넓다.</summary>
            public static readonly RectInt InfoBox = new RectInt(12, 132, 149, 36);
            public static readonly RectInt Info    = new RectInt(16, 137, 144, 27);

            public static readonly RectInt Reward = new RectInt(31, 176, 32, 32);
            public const int RewardStep = 37, RewardCount = 3;

            // ── 아래쪽: 파츠 목록 (상세 상태) ──
            // 설명·보상과 같은 자리를 나눠 쓴다. 전환 버튼으로 갈아 끼운다.
            //
            // 배치는 <b>유전정보 오른쪽 줄과 같다</b>. 따로 적어 두었더니 도감 쪽만 등급 뱃지가
            // 동그란 아이콘에 겹쳐 있었다(2026-08-18). 그쪽 값을 그대로 옮겨 오므로
            // 한쪽만 고쳐져 다시 어긋나는 일이 없다 — 줄 기준 좌표를 줄 자리에 더하기만 한다.
            private static RectInt InRow(RectInt at) =>
                new RectInt(Gene.Slim.x + at.x, Gene.Slim.y + at.y, at.width, at.height);

            public static readonly RectInt PartRow    = InRow(Gene.SlimBar);
            public const int PartStep = Gene.SlimStep, PartCount = 4;

            public static readonly RectInt PartIcon   = InRow(Gene.SlimThumb);
            public static readonly RectInt PartRarity = InRow(Gene.SlimRarity);
            public static readonly RectInt PartName   = InRow(Gene.SlimName);
        }

        /// <summary>
        /// 파츠(외형) 도감. 달팽이 도감과 같은 좌우 구성이다.
        ///
        /// 왼쪽 위에 부위 토글 넷, 그 아래에 그 부위의 파츠 목록.
        /// 오른쪽은 고른 파츠 하나 — 실루엣 위에 그 파츠만 얹고, 설명과 보상을 보여 준다.
        /// </summary>
        public static class PartsGuide
        {
            // ── 왼쪽: 부위 토글 + 목록 ──
            public static readonly RectInt Type = new RectInt(16, 10, 21, 21);
            public const int TypeStep = 37;

            /// <summary>목록이 보이는 영역. 토글 줄 아래부터 패널 끝까지다.</summary>
            public static readonly RectInt View = new RectInt(0, 38, PanelW, PanelH - 38);

            /// <summary>줄 하나. 자리는 <b>스크롤 내용 기준</b>이다.</summary>
            public static readonly RectInt Row = new RectInt(11, 4, 151, 24);
            public const int RowStep = 28;

            /// <summary>
            /// 미리 만들어 두는 줄 수. 지금 가장 많은 부위(껍질)가 52개다.
            ///
            /// <b>모자라면 남는 파츠는 목록에 아예 안 나온다.</b> 안 나오면 고를 수도 없어
            /// 그 파츠의 보상을 받을 길이 없고, 레드닷은 켜진 채로 남는다 — 48줄이던 때
            /// 껍질이 52개로 늘어 실제로 이 일이 있었다. 시트가 커지면 여기도 키울 것.
            /// </summary>
            public const int RowPool = 56;

            // 아래 셋은 줄 왼쪽 위가 원점
            public static readonly RectInt RowName   = new RectInt(  8, 3, 100, 18);
            public static readonly RectInt RowRarity = new RectInt(108, 5,  37, 14);

            /// <summary>받을 보상이 있다는 표시. 줄 오른쪽 위 모서리에 걸친다.</summary>
            public static readonly RectInt RowDot    = new RectInt(145, 0,  6, 6);

            /// <summary>도감 버튼 위의 같은 표시. 버튼 오른쪽 위에 걸친다.</summary>
            public static readonly RectInt ButtonDot = new RectInt( 19, -1,  6, 6);

            /// <summary>부위 토글 위의 같은 표시. 토글이 21px 이라 그 오른쪽 위다.</summary>
            public static readonly RectInt TypeDot   = new RectInt( 16, -1,  6, 6);

            // ── 오른쪽: 고른 파츠 ──
            public static readonly RectInt Title  = new RectInt(21, 11, 131, 22);

            /// <summary>
            /// 실루엣과 파츠가 겹쳐 앉는 자리. 파츠 아트와 실루엣은 <b>같은 1200x1200 캔버스</b>라
            /// 같은 사각형에 겹쳐 놓기만 하면 제자리에 온다 — 합성이 쓰는 규칙과 같다.
            /// </summary>
            public static readonly RectInt Image  = new RectInt(24, 40, 125, 105);

            public static readonly RectInt Rarity = new RectInt(69, 132, 35, 12);

            public static readonly RectInt InfoBox = new RectInt(12, 148, 149, 20);
            public static readonly RectInt Info    = new RectInt(16, 150, 141, 16);

            /// <summary>
            /// 보상 칸 셋. <b>크기와 간격은 달팽이 도감과 같아야 한다</b> —
            /// 가운데로 모아 놓는 <c>SnailUi.FillRewardSlots</c> 가 그쪽 값으로 자리를 다시 잡는다.
            /// 여기만 줄였더니 상자는 32 인데 그림은 28 짜리로 남아 왼쪽 위로 쏠렸다.
            /// </summary>
            public static readonly RectInt Reward = new RectInt(31, 174, 32, 32);
            public const int RewardStep = 37, RewardCount = 3;

            /// <summary>아직 아무것도 안 고른 상태에서 한가운데 뜨는 안내.</summary>
            public static readonly RectInt Empty = new RectInt(16, 100, PanelW - 32, 20);
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
