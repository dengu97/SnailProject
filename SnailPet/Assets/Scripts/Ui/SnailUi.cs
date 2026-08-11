using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using At = SnailPet.Ui.UiTheme.At;
using Max = SnailPet.Ui.UiTheme.Max;
using Fd = SnailPet.Ui.UiTheme.Food;

namespace SnailPet.Ui
{
    /// <summary>
    /// 데스크톱 위젯 UI. 목업의 「디폴트」와 「최대화」 두 상태를 만든다.
    ///
    /// 최대화는 상세 패널 왼쪽에 목록 패널과 탭을 덧붙이는 것이다. 오른쪽 상세 패널은
    /// 디폴트와 완전히 같은 레이아웃이라(목업에서 확인) 그대로 재사용한다.
    /// 위젯이 화면 오른쪽에 붙어 있어 목록이 열려도 상세 패널은 제자리에 남는다.
    ///
    /// 씬 없이 전부 코드로 만드는 것은 달팽이 쪽과 같은 방침이다.
    /// 글꼴은 TextMeshPro 대신 OS 한글 폰트를 런타임에 잡아 쓴다. TMP 는 한글
    /// 아틀라스를 따로 구워야 하는데, 여기서 얻을 게 없는 준비 비용이다.
    /// </summary>
    public sealed class SnailUi : MonoBehaviour
    {
        /// <summary>
        /// UI 에 나가는 글자의 언어 키. 여기 없는 글자가 화면에 있으면 안 된다.
        /// 값은 LanguageData 시트에 있고, 없는 키는 토큰이 그대로 화면에 나와 바로 눈에 띈다.
        /// </summary>
        private static class Keys
        {
            public const string Age    = "[레벨]";     // "{0}살"
            public const string NoName = "[이름없음]";
            public const string SnailList = "[달팽이목록]";
            public const string FoodList  = "[음식목록]";
            public const string EggList   = "[보유중인알]";
            public const string Shop      = "[상점]";
            public const string Feed      = "[먹이기]";
            public const string Incubator = "[부화기]";
            public const string NoEgg     = "[부화시킬알없음]";
            public const string HatchDone = "[부화완료]";
        }

        /// <summary>한 화면에 위젯이 두 개 이상 뜰 일이 없으므로 정렬 순서는 고정.</summary>
        private const int CanvasSortOrder = 100;

        private Font _font;
        [SerializeField] private RectTransform _widget;      // 패널 + 밖으로 걸치는 버튼까지 감싸는 상자
        [SerializeField] private RectTransform _listRoot, _detailRoot;
        [SerializeField] private RectTransform _panel;

        [SerializeField] private Text _nameText, _rarityText, _ageText, _coinText;
        [SerializeField] private Image _rarityBadge, _rarityIcon;
        [SerializeField] private RectTransform _fullFill, _happyFill;

        public event Action Rename, Detail, Wardrobe, Gene, Sell, Settings, Close, Maximize;
        public event Action<int> TabChanged, SwapTo;

        [SerializeField] private Image[] _tabs;
        [SerializeField] private ListRow[] _rows;
        [SerializeField] private Text _listTitle;
        private int _tab;

        /// <summary>편집용 프리팹의 위치. 메뉴 「SnailPet → 5. UI 프리팹 생성」 이 여기에 만든다.</summary>
        public const string PrefabResource = "Ui/SnailUi";

        /// <summary>
        /// 프리팹이 있으면 그것을 쓰고, 없으면 코드로 짓는다.
        ///
        /// 프리팹이 원본이다. 레이아웃을 손으로 옮기셨다면 그 편집본이 그대로 화면에 나온다.
        /// 코드 빌더는 프리팹을 처음 만들 때와, 프리팹이 없을 때의 대비책으로만 남는다.
        /// </summary>
        public static SnailUi Create(Transform parent)
        {
            var prefab = Resources.Load<GameObject>(PrefabResource);
            if (prefab != null)
            {
                var instance = Instantiate(prefab, parent, false);
                instance.name = "SnailUi";
                return instance.GetComponent<SnailUi>();
            }

            Debug.Log("[SnailPet] UI 프리팹이 없어 코드로 만듭니다. " +
                      "메뉴 SnailPet > 5. UI 프리팹 생성 으로 편집 가능한 프리팹을 만들 수 있습니다.");

            var go = new GameObject("SnailUi");
            go.transform.SetParent(parent, false);
            var self = go.AddComponent<SnailUi>();
            self.Build();
            self.Bind();
            return self;
        }

#if UNITY_EDITOR
        /// <summary>에디터에서 프리팹을 구울 때만 쓴다.</summary>
        public static SnailUi BuildForPrefab(GameObject host)
        {
            var self = host.AddComponent<SnailUi>();
            self.Build();
            return self;
        }
#endif

        /// <summary>
        /// 프리팹 인스턴스가 살아날 때의 마무리.
        ///
        /// 두 가지는 프리팹에 저장할 수 없어 여기서 채운다.
        ///  · 글꼴 — OS 폰트라 에셋이 아니다. 안 채우면 한글이 네모로 나온다
        ///  · 코드가 만든 도형 스프라이트 — 런타임 생성물이라 직렬화되지 않는다
        /// 둘 다 <b>비어 있을 때만</b> 채우므로 프리팹에서 갈아 끼운 것은 살아남는다.
        /// </summary>
        private void Bind()
        {
            _font ??= LoadKoreanFont();

            foreach (var t in GetComponentsInChildren<Text>(true))
                t.font = _font;

            foreach (var s in GetComponentsInChildren<UiShapeRef>(true))
            {
                var img = s.GetComponent<Image>();
                if (img != null && img.sprite == null) img.sprite = UiSprites.Of(s.Shape);
            }

            EnsureEventSystem();
            SetTab(_tab);
        }

        private void Awake()
        {
            // 코드로 지은 경우에는 Create 가 직접 부른다. 두 번 불려도 문제는 없다.
            if (_widget != null) Bind();
        }

        // ── 짓기 ──

        private void Build()
        {
            _font = LoadKoreanFont();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortOrder;

            // 픽셀 단위로 그대로 배치한다. 목업 좌표가 곧 픽셀이라 스케일러를 끼우면 오히려 어긋난다.
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // 9-슬라이스 테두리 두께는 sprite.pixelsPerUnit / referencePixelsPerUnit 로 환산된다.
            // 기본값 100 을 그대로 두면 PPU 1 짜리 도형의 테두리가 100배로 계산돼
            // 잘리지 않고 통째로 늘어난다. 둥근 사각형이 타원이 되고 전부 뭉개진다.
            scaler.referencePixelsPerUnit = 1f;
            gameObject.AddComponent<GraphicRaycaster>();

            // 위젯 상자를 화면 오른쪽 아래에 붙인다. 코인 줄이 패널 위로 올라가므로 그만큼 키운다.
            // 폭은 최대화 기준으로 잡아 둔다. 오른쪽에 붙어 있으므로 목록이 열려도
            // 상세 패널은 화면에서 제자리에 남고 왼쪽으로만 자란다.
            _widget = NewRect("Widget", (RectTransform)transform);
            _widget.anchorMin = _widget.anchorMax = _widget.pivot = new Vector2(1f, 0f);
            _widget.sizeDelta = new Vector2(UiTheme.PanelW + At.Close.xMax, UiTheme.PanelH - At.Coin.y);
            _widget.anchoredPosition = new Vector2(-UiTheme.ScreenMargin, UiTheme.ScreenMargin);

            _listRoot = NewRect("List", _widget);
            Place(_listRoot, new RectInt(0, 0, UiTheme.PanelW, UiTheme.PanelH - At.Coin.y));

            _detailRoot = NewRect("Detail", _widget);
            Place(_detailRoot, new RectInt(UiTheme.PanelW, 0, At.Close.xMax, UiTheme.PanelH - At.Coin.y));

            _panel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));

            BuildHeader();
            BuildGauges();
            BuildActions();
            BuildOutside();
            BuildList();
            BuildFoodDetail();
            BuildEggPanel();

            SetMaximized(false);

            SetSnail("달팽이 이름", SnailPet.Data.RarityType.Epic, 0);
            SetGauges(0.62f, 0.28f);
            SetCoin(5000);
        }

        /// <summary>이름칸 · 이름 수정 · 등급 뱃지.</summary>
        private void BuildHeader()
        {
            Box(_panel, At.NameField, UiTheme.Slot, UiSprites.Shape.Slot, "NameField");
            IconButton(_panel, At.RenameBtn, "icon_rename", "Rename", () => Rename?.Invoke());

            var name = At.NameField;
            _nameText = Label(_panel, new RectInt(name.x + 22, name.y, name.width - 26, name.height),
                              "달팽이 이름", 13, UiTheme.Ink);

            // 등급은 EnumData.IconResourceKey 의 아이콘으로 보여 준다.
            // 키가 비어 있거나 파일이 없으면 알약에 enum 이름을 띄운다 — 빠진 것이 눈에 띄게.
            _rarityBadge = Box(_panel, At.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _rarityText  = Label(_panel, At.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_rarityText);

            // 등급 아트는 글자까지 그려진 가로형 알약이라 뱃지 자리를 통째로 쓴다.
            _rarityIcon = Icon(_panel, At.Rarity, null, Color.white, "RarityIcon");
            _rarityIcon.raycastTarget = false;
            _rarityIcon.enabled = false;

            // 달팽이 모습이 들어갈 자리. 그림은 부트스트랩이 초상 텍스처를 넘겨 준다.
            var rt = NewRect("Portrait", _panel);
            Place(rt, At.Portrait);
            _portrait = rt.gameObject.AddComponent<RawImage>();
            _portrait.raycastTarget = false;
            _portrait.enabled = false;
        }

        [SerializeField] private RawImage _portrait;

        /// <summary>패널 가운데에 띄울 달팽이 모습.</summary>
        public void SetPortrait(Texture texture)
        {
            if (_portrait == null) return;
            _portrait.texture = texture;
            _portrait.enabled = texture != null;
        }

        /// <summary>초상 텍스처를 만들 때 쓸 크기(px).</summary>
        public static Vector2Int PortraitSize => new Vector2Int(At.Portrait.width, At.Portrait.height);

        /// <summary>나이 뱃지 · 포만도 · 행복 지수.</summary>
        private void BuildGauges()
        {
            Box(_panel, At.Age, UiTheme.Slot, UiSprites.Shape.Badge, "AgeBadge");
            _ageText = Label(_panel, At.Age, "00살", 9, UiTheme.Ink);

            _fullFill  = Gauge(At.FullBar,  At.FullIcon,  "icon_food",  UiTheme.GaugeFull,
                               UiSprites.Shape.Fill,      "Full");
            _happyFill = Gauge(At.HappyBar, At.HappyIcon, "icon_happy", UiTheme.GaugeHappy,
                               UiSprites.Shape.FillHappy, "Happy");
        }

        /// <summary>
        /// 게이지 한 줄. 트랙 위에 채우기를 얹고, 왼쪽 끝에 아이콘 칸을 올린다.
        /// 채우기는 <b>왼쪽부터</b> 찬다. 목업에서는 오른쪽에 붙어 있는데,
        /// 게이지는 왼쪽부터 차는 것이 표준이라 그렇게 뒀다.
        /// </summary>
        private RectTransform Gauge(RectInt bar, RectInt icon, string iconKey, Color fillColor,
                                    UiSprites.Shape fillShape, string name)
        {
            Box(_panel, bar, UiTheme.Slot, UiSprites.Shape.Slot, name + "Track");

            const int inset = 2;
            var fill = Box(_panel, new RectInt(bar.x + inset, bar.y + inset,
                                               bar.width - inset * 2, bar.height - inset * 2),
                           fillColor, fillShape, name + "Fill");

            // 왼쪽을 축으로 가로만 줄였다 늘렸다 한다
            var rt = (RectTransform)fill.transform;
            rt.pivot = new Vector2(0f, 1f);

            Icon(_panel, icon, iconKey, UiTheme.Ink, name + "Icon").raycastTarget = false;
            return rt;
        }

        /// <summary>하단 액션 4개. 상세정보 · 옷장 · 유전정보 · 판매.</summary>
        private void BuildActions()
        {
            var keys  = new[] { "icon_detail", "icon_wardrobe", "icon_gene", "icon_sell" };
            var names = new[] { "Detail", "Wardrobe", "Gene", "Sell" };
            var fires = new Action[] { () => Detail?.Invoke(), () => Wardrobe?.Invoke(),
                                       () => Gene?.Invoke(),   () => Sell?.Invoke() };

            for (int i = 0; i < keys.Length; i++)
                IconButton(_panel, At.Actions[i], keys[i], names[i], fires[i]);
        }

        /// <summary>패널 밖으로 걸치는 것들. 설정 · 코인 · 닫기 · 최대화.</summary>
        private void BuildOutside()
        {
            IconButton(_detailRoot, Above(At.Settings), "icon_settings", "Settings",
                       () => Settings?.Invoke(), UiTheme.Accent);

            var coin = Above(At.Coin);
            Box(_detailRoot, coin, UiTheme.Slot, UiSprites.Shape.Badge, "CoinPill");
            Icon(_detailRoot, new RectInt(coin.x + 4, coin.y + 6, 22, 22), "icon_coin",
                 Color.white, "CoinIcon").raycastTarget = false;
            _coinText = Label(_detailRoot, new RectInt(coin.x + 30, coin.y, coin.width - 34, coin.height),
                              "5,000", 12, UiTheme.Ink);

            // 이 둘은 다른 아이콘과 달리 아트에 색이 들어 있다. 물들이면 안 된다.
            IconButton(_detailRoot, Above(At.Close),    "btn_close",    "Close",
                       () => { SetMaximized(false); Close?.Invoke(); }, tint: Color.white);
            IconButton(_detailRoot, Above(At.Maximize), "btn_maximize", "Maximize",
                       () => { SetMaximized(true); Maximize?.Invoke(); }, tint: Color.white);
        }

        /// <summary>위젯 상자 기준 좌표로 옮긴다. 목업은 패널 왼쪽 위가 원점이라 코인 줄만큼 내려 준다.</summary>
        private static RectInt Above(RectInt r) => new RectInt(r.x, r.y - At.Coin.y, r.width, r.height);

        /// <summary>
        /// 최대화에서 왼쪽에 붙는 목록. 탭 4개 + 목록 패널 + 행 4개.
        /// 행 내용은 아직 더미다. 실제 보유 목록이 생기면 <see cref="SetRows"/> 로 채운다.
        /// </summary>
        private void BuildList()
        {
            var tabKeys  = new[] { "icon_snail", "icon_food", "icon_egg", "icon_shop" };
            var tabNames = new[] { "TabSnail", "TabFood", "TabEgg", "TabShop" };

            _tabs = new Image[Max.Tabs.Length];
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                var btn = IconButton(_listRoot, Above(Max.Tabs[i]), tabKeys[i], tabNames[i],
                                     () => SetTab(index), UiTheme.TabOff);
                _tabs[i] = btn.GetComponent<Image>();
            }

            var panel = Panel(_listRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _listTitle = Label(panel, new RectInt(0, 8, UiTheme.PanelW, 16), "", 12, UiTheme.Ink);

            BuildFoodGrid(panel);

            _rows = new ListRow[Max.RowCount];
            for (int i = 0; i < _rows.Length; i++)
            {
                var r = Max.Row;
                _rows[i] = BuildRow(panel, new RectInt(r.x, r.y + i * Max.RowStep, r.width, r.height), i);
            }

            SetTab(0);
        }

        // ── 음식 탭 ──

        [Serializable]
        public sealed class GridSlot
        {
            public RectTransform Root;
            public Image Icon, Frame;
            public Text Count;
            public Button Button;
        }

        [SerializeField] private RectTransform _foodPanel, _foodGridRoot, _foodContent;
        [SerializeField] private GridSlot[] _foodSlots;
        [SerializeField] private RectTransform _eggGridRoot, _eggContent;
        [SerializeField] private GridSlot[] _eggSlots;
        [SerializeField] private Image _foodIcon;
        [SerializeField] private Text _foodName, _foodFull, _foodHappy, _foodInfo;
        [SerializeField] private Image _foodRarityBadge, _foodRarityIcon;
        [SerializeField] private Text _foodRarityText;

        private int[] _foodIds = new int[0];
        private int _selectedFood = -1;

        public event Action<int> FoodSelected, FeedFood;

        /// <summary>
        /// 음식 그리드. 목업의 5번째 줄이 잘려 있어 세로로 스크롤한다.
        ///
        /// 칸은 미리 만들어 두고 보유량에 따라 켜고 끈다. 매번 만들고 지우면
        /// 프리팹으로 구울 수 없고, 스크롤 중에 GC 가 튄다.
        /// </summary>
        private void BuildFoodGrid(RectTransform panel)
        {
            BuildGrid(panel, "FoodGrid", SelectFood, out _foodGridRoot, out _foodContent, out _foodSlots);
            BuildGrid(panel, "EggGrid",  SelectEgg,  out _eggGridRoot,  out _eggContent,  out _eggSlots);
        }

        /// <summary>
        /// 스크롤되는 4열 그리드. 음식과 알이 목업에서 같은 자리·같은 크기라 그대로 공유한다.
        /// </summary>
        private void BuildGrid(RectTransform panel, string name, Action<int> onClick,
                               out RectTransform root, out RectTransform content, out GridSlot[] slots)
        {
            root = NewRect(name, panel);
            Place(root, Max.FoodView);
            root.gameObject.SetActive(false);

            // 넘치는 부분을 잘라 낸다. 이게 없으면 패널 밖으로 흘러나온다.
            root.gameObject.AddComponent<RectMask2D>();

            content = NewRect("Content", root);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(UiTheme.PanelW, Max.FoodView.height);

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = root;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            slots = new GridSlot[Max.FoodSlotPool];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = BuildGridSlot(content, i, onClick);
        }

        /// <summary>내용 높이를 줄 수에 맞춘다. 이게 스크롤 범위를 정한다.</summary>
        private static void FitContent(RectTransform content, int count)
        {
            int rows = Mathf.CeilToInt(count / (float)Max.FoodCols);
            content.sizeDelta = new Vector2(UiTheme.PanelW,
                Mathf.Max(Max.FoodView.height, Max.FoodSlot.y + rows * Max.FoodStepY));
        }

        private GridSlot BuildGridSlot(RectTransform content, int index, Action<int> onClick)
        {
            var s = Max.FoodSlot;
            var at = new RectInt(s.x + index % Max.FoodCols * Max.FoodStepX,
                                 s.y + index / Max.FoodCols * Max.FoodStepY, s.width, s.height);

            var root = NewRect("Slot" + index, content);
            Place(root, at);

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = UiSprites.Of(UiSprites.Shape.Slot);
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.RowSlot;
            root.gameObject.AddComponent<UiShapeRef>().Shape = UiSprites.Shape.Slot;

            var slot = new GridSlot { Root = root };
            slot.Icon = Icon(root, new RectInt(2, 2, s.width - 4, s.height - 4), null, Color.white, "Icon");
            slot.Icon.raycastTarget = false;

            // 선택 표시. 목업에서 고른 칸에 빨간 테두리가 둘린다.
            slot.Frame = NewRect("Frame", root).gameObject.AddComponent<Image>();
            var fr = (RectTransform)slot.Frame.transform;
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
            slot.Frame.sprite = UiSprites.Of(UiSprites.Shape.Selection);
            slot.Frame.type = Image.Type.Sliced;
            slot.Frame.gameObject.AddComponent<UiShapeRef>().Shape = UiSprites.Shape.Selection;
            slot.Frame.color = UiTheme.Selected;
            slot.Frame.raycastTarget = false;
            slot.Frame.enabled = false;

            slot.Count = Label(root, Max.FoodCount, "", 9, UiTheme.Ink);
            slot.Count.alignment = TextAnchor.LowerRight;

            int captured = index;
            slot.Button = root.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = bg;
            slot.Button.onClick.AddListener(() => onClick(captured));

            root.gameObject.SetActive(false);
            return slot;
        }

        /// <summary>음식 탭의 오른쪽 상세 패널. 달팽이 상세와 자리를 바꿔 가며 쓴다.</summary>
        private void BuildFoodDetail()
        {
            _foodPanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _foodPanel.gameObject.SetActive(false);

            Icon(_foodPanel, Fd.Favorite, "icon_favorite", UiTheme.Ink, "Favorite").raycastTarget = false;
            _foodName = Label(_foodPanel, Fd.Name, "", 12, UiTheme.Ink);

            _foodRarityBadge = Box(_foodPanel, Fd.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _foodRarityText  = Label(_foodPanel, Fd.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_foodRarityText);
            _foodRarityIcon = Icon(_foodPanel, Fd.Rarity, null, Color.white, "RarityIcon");
            _foodRarityIcon.raycastTarget = false;
            _foodRarityIcon.enabled = false;

            Box(_foodPanel, Fd.Preview, UiTheme.Slot, UiSprites.Shape.Slot, "Preview");
            _foodIcon = Icon(_foodPanel, Fd.Preview, null, Color.white, "PreviewIcon");
            _foodIcon.raycastTarget = false;

            Box(_foodPanel, Fd.FullIcon, UiTheme.Slot, UiSprites.Shape.Badge, "FullIconBox");
            Icon(_foodPanel, Fd.FullIcon, "icon_food", UiTheme.Ink, "FullIcon").raycastTarget = false;
            Box(_foodPanel, Fd.FullValue, UiTheme.Slot, UiSprites.Shape.Badge, "FullValue");
            _foodFull = Label(_foodPanel, Fd.FullValue, "", 9, UiTheme.Ink);

            Box(_foodPanel, Fd.HappyIcon, UiTheme.Slot, UiSprites.Shape.Badge, "HappyIconBox");
            Icon(_foodPanel, Fd.HappyIcon, "icon_happy", UiTheme.Ink, "HappyIcon").raycastTarget = false;
            Box(_foodPanel, Fd.HappyValue, UiTheme.Slot, UiSprites.Shape.Badge, "HappyValue");
            _foodHappy = Label(_foodPanel, Fd.HappyValue, "", 9, UiTheme.Ink);

            Box(_foodPanel, Fd.Info, UiTheme.Slot, UiSprites.Shape.Slot, "InfoBox");
            _foodInfo = Label(_foodPanel, new RectInt(Fd.Info.x + 4, Fd.Info.y, Fd.Info.width - 8, Fd.Info.height),
                              "", 8, UiTheme.Ink);
            _foodInfo.horizontalOverflow = HorizontalWrapMode.Wrap;

            var feed = Box(_foodPanel, Fd.Feed, UiTheme.Slot, UiSprites.Shape.Button, "FeedButton");
            feed.raycastTarget = true;
            Label(_foodPanel, Fd.Feed, SnailPet.Data.Loc.Text(Keys.Feed), 10, UiTheme.Ink);
            var feedBtn = feed.gameObject.AddComponent<Button>();
            feedBtn.targetGraphic = feed;
            feedBtn.onClick.AddListener(() =>
            {
                if (_selectedFood >= 0 && _selectedFood < _foodIds.Length)
                    FeedFood?.Invoke(_foodIds[_selectedFood]);
            });

            IconButton(_foodPanel, Fd.Buy,  "icon_shop", "Buy",  () => Settings?.Invoke());
            IconButton(_foodPanel, Fd.Sell, "icon_sell", "Sell", () => Sell?.Invoke());
        }

        /// <summary>보유 음식을 채운다. (음식 Id, 개수) 목록.</summary>
        public void SetFoods((int foodId, int count)[] foods)
        {
            _foodIds = new int[foods?.Length ?? 0];

            for (int i = 0; i < _foodSlots.Length; i++)
            {
                bool has = foods != null && i < foods.Length;
                _foodSlots[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                _foodIds[i] = foods[i].foodId;
                var row = SnailPet.Data.GameData.FoodDataById.TryGetValue(foods[i].foodId, out var f) ? f : null;

                _foodSlots[i].Icon.sprite = FoodSprite(row);
                _foodSlots[i].Icon.enabled = _foodSlots[i].Icon.sprite != null;
                _foodSlots[i].Count.text = foods[i].count > 1 ? foods[i].count.ToString() : "";
            }

            // 내용 높이를 줄 수에 맞춘다. 이게 스크롤 범위를 정한다.
            int rows = Mathf.CeilToInt((_foodIds.Length) / (float)Max.FoodCols);
            _foodContent.sizeDelta = new Vector2(UiTheme.PanelW,
                Mathf.Max(Max.FoodView.height, Max.FoodSlot.y + rows * Max.FoodStepY));

            SelectFood(_foodIds.Length > 0 ? 0 : -1);
        }

        private static Sprite FoodSprite(SnailPet.Data.FoodDataRow row) =>
            row == null || string.IsNullOrEmpty(row.ResourceKey)
                ? null : Resources.Load<Sprite>("Snail/Food/" + row.ResourceKey);

        /// <summary>칸을 고른다. 상세 패널이 그 음식으로 바뀐다.</summary>
        public void SelectFood(int index)
        {
            _selectedFood = index;
            for (int i = 0; i < _foodSlots.Length; i++)
                _foodSlots[i].Frame.enabled = i == index;

            if (index < 0 || index >= _foodIds.Length) return;

            var row = SnailPet.Data.GameData.FoodDataById.TryGetValue(_foodIds[index], out var f) ? f : null;
            if (row == null) return;

            _foodName.text  = SnailPet.Data.Loc.ById(row.NameId);
            _foodInfo.text  = SnailPet.Data.Loc.ById(row.InfoId);
            _foodFull.text  = row.FullPoint.ToString("0");
            _foodHappy.text = row.HappyPoint.ToString("0");

            _foodIcon.sprite = FoodSprite(row);
            _foodIcon.enabled = _foodIcon.sprite != null;

            // 음식에는 등급이 없다. 자리는 목업에 있으므로 비워 둔다.
            _foodRarityBadge.enabled = false;
            _foodRarityIcon.enabled = false;
            _foodRarityText.text = "";

            FoodSelected?.Invoke(_foodIds[index]);
        }

        // ── 알 탭 ──

        [Serializable]
        public sealed class HatchSlot
        {
            public RectTransform Root;
            public Image Egg;
            public Text Plus;
            public Text Timer;
            public Button Button;
        }

        [SerializeField] private RectTransform _eggPanel;
        [SerializeField] private HatchSlot[] _hatchSlots;
        [SerializeField] private Text _eggEmpty;

        private int[] _eggIds = new int[0];

        /// <summary>알을 부화기에 넣기 · 부화한 달팽이 수령 · 상점으로 가기.</summary>
        public event Action<int> PutEgg, ClaimHatched;
        public event Action GoShop;

        /// <summary>
        /// 부화기. 칸 3개와 각 칸의 남은 시간.
        ///
        /// 칸 수는 나중에 해금으로 늘어난다(UnlockData). 지금은 3개만 만들고,
        /// 늘릴 때는 <see cref="UiTheme.Egg.Slots"/> 에 자리만 더 적으면 된다.
        /// </summary>
        private void BuildEggPanel()
        {
            _eggPanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _eggPanel.gameObject.SetActive(false);

            Label(_eggPanel, UiTheme.Egg.Title, SnailPet.Data.Loc.Text(Keys.Incubator), 12, UiTheme.Ink);

            _hatchSlots = new HatchSlot[UiTheme.Egg.Slots.Length];
            for (int i = 0; i < _hatchSlots.Length; i++)
                _hatchSlots[i] = BuildHatchSlot(i);

            // 알이 하나도 없을 때만 보이는 안내
            _eggEmpty = Label(_eggPanel, UiTheme.Egg.Empty, SnailPet.Data.Loc.Text(Keys.NoEgg), 10, UiTheme.Slot);

            IconButton(_eggPanel, UiTheme.Egg.Buy, "icon_egg", "BuyEgg", () => GoShop?.Invoke());
        }

        private HatchSlot BuildHatchSlot(int index)
        {
            var at = UiTheme.Egg.Slots[index];
            var root = NewRect("Hatch" + index, _eggPanel);
            Place(root, at);

            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = UiSprites.Of(UiSprites.Shape.Button);
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.Slot;
            root.gameObject.AddComponent<UiShapeRef>().Shape = UiSprites.Shape.Button;

            var slot = new HatchSlot { Root = root };

            // 빈 칸의 +. 아이콘이 따로 없어 글자로 그린다.
            // 스프라이트 없는 Image 를 쓰면 색으로 꽉 찬 사각형이 나온다.
            slot.Plus = Label(root, new RectInt(0, 0, at.width, at.height), "+", 26, UiTheme.Slot);

            slot.Egg = Icon(root, new RectInt((at.width - 26) / 2, 8, 26, 26), null, Color.white, "Egg");
            slot.Egg.raycastTarget = false;

            slot.Timer = Label(root, new RectInt(0, at.height - 16, at.width, 14), "", 9, UiTheme.Ink);

            int captured = index;
            slot.Button = root.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = bg;
            slot.Button.onClick.AddListener(() => ClaimHatched?.Invoke(captured));
            return slot;
        }

        /// <summary>보유 알. 같은 등급이어도 낱개로 나열한다 — 뭐가 나올지 모르니 하나씩 봐야 한다.</summary>
        public void SetEggs(int[] eggIds)
        {
            _eggIds = eggIds ?? new int[0];

            for (int i = 0; i < _eggSlots.Length; i++)
            {
                bool has = i < _eggIds.Length;
                _eggSlots[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                var row = SnailPet.Data.GameData.EggDataById.TryGetValue(_eggIds[i], out var e) ? e : null;
                _eggSlots[i].Icon.sprite = EggSprite(row);
                _eggSlots[i].Icon.enabled = _eggSlots[i].Icon.sprite != null;
                _eggSlots[i].Count.text = "";      // 낱개라 수량이 없다
                _eggSlots[i].Frame.enabled = false;
            }

            FitContent(_eggContent, _eggIds.Length);
            if (_eggEmpty != null) _eggEmpty.enabled = _eggIds.Length == 0;
        }

        private static Sprite EggSprite(SnailPet.Data.EggDataRow row) =>
            row == null || string.IsNullOrEmpty(row.ResourceKey)
                ? null : Resources.Load<Sprite>("Snail/Egg/" + row.ResourceKey);

        /// <summary>목록의 알을 눌렀다. 빈 부화 칸에 넣어 달라고 알린다.</summary>
        private void SelectEgg(int index)
        {
            if (index >= 0 && index < _eggIds.Length) PutEgg?.Invoke(index);
        }

        /// <summary>
        /// 부화기 상태를 그린다. 칸마다 (알 Id, 남은 초). 알 Id 가 0 이면 빈 칸,
        /// 남은 초가 0 이하면 부화 완료다.
        /// </summary>
        public void SetIncubator((int eggId, double remain)[] slots)
        {
            for (int i = 0; i < _hatchSlots.Length; i++)
            {
                var s = _hatchSlots[i];
                bool filled = slots != null && i < slots.Length && slots[i].eggId > 0;

                s.Plus.enabled = !filled;
                s.Egg.enabled = filled;
                s.Timer.text = "";

                if (!filled) continue;

                var row = SnailPet.Data.GameData.EggDataById.TryGetValue(slots[i].eggId, out var e) ? e : null;
                s.Egg.sprite = EggSprite(row);
                s.Egg.enabled = s.Egg.sprite != null;

                double remain = slots[i].remain;
                s.Timer.text = remain > 0
                             ? System.TimeSpan.FromSeconds(remain).ToString(@"mm\:ss")
                             : SnailPet.Data.Loc.Text(Keys.HatchDone);
            }
        }

        /// <summary>목록 한 줄. 썸네일 · 이름 · 등급 · 나이 · 교체 버튼.</summary>
        [Serializable]
        public sealed class ListRow
        {
            public RectTransform Root;
            public bool Filled;
            public Image Thumb;
            public Image RarityBadge, RarityIcon;
            public Text Name, Rarity, Age;
            public Button Swap;
        }

        private ListRow BuildRow(RectTransform parent, RectInt at, int index)
        {
            var rowRt = NewRect("Row" + index, parent);
            Place(rowRt, at);

            var bg = rowRt.gameObject.AddComponent<Image>();
            bg.sprite = UiSprites.Of(UiSprites.Shape.Slot);
            bg.type = Image.Type.Sliced;
            bg.color = UiTheme.Slot;

            var row = new ListRow
            {
                Root   = rowRt,
                Thumb  = Box(rowRt, Max.RowThumb, UiTheme.RowSlot, UiSprites.Shape.Slot, "Thumb"),
                Name   = Label(rowRt, Max.RowName, "", 11, UiTheme.Ink),
                Rarity = null,
                Age    = null,
            };

            row.RarityBadge = Box(rowRt, Max.RowRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            row.Rarity = Label(rowRt, Max.RowRarity, "", 8, UiTheme.OnBadge);
            Shrink(row.Rarity);
            row.RarityIcon = Icon(rowRt, Max.RowRarity, null, Color.white, "RarityIcon");
            row.RarityIcon.raycastTarget = false;
            row.RarityIcon.enabled = false;

            Box(rowRt, Max.RowAge, UiTheme.Slot, UiSprites.Shape.Badge, "AgeBadge");
            row.Age = Label(rowRt, Max.RowAge, "", 8, UiTheme.Ink);

            int captured = index;
            row.Swap = IconButton(rowRt, Max.RowSwap, "icon_swap", "Swap", () => SwapTo?.Invoke(captured));
            return row;
        }

        /// <summary>지금 나와 있는 달팽이는 교체 버튼이 없다 (목업 주석).</summary>
        public void SetRows((string name, SnailPet.Data.RarityType rarity, int age, bool isActive)[] rows)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                bool has = rows != null && i < rows.Length;
                _rows[i].Filled = has;
                _rows[i].Root.gameObject.SetActive(has && _tab == 0);
                if (!has) continue;

                var r = rows[i];
                _rows[i].Name.text   = string.IsNullOrWhiteSpace(r.name)
                                     ? SnailPet.Data.Loc.Text(Keys.NoName) : r.name;
                ApplyRarity(_rows[i].RarityIcon, _rows[i].RarityBadge, _rows[i].Rarity, r.rarity);
                _rows[i].Age.text    = SnailPet.Data.Loc.Format(Keys.Age, r.age);
                _rows[i].Swap.gameObject.SetActive(!r.isActive);
            }
        }

        /// <summary>탭 선택. 지금은 색만 바뀌고 내용은 그대로다.</summary>
        public void SetTab(int index)
        {
            _tab = Mathf.Clamp(index, 0, _tabs.Length - 1);

            // 탭이 왼쪽 목록과 오른쪽 상세를 함께 바꾼다. 둘은 항상 같은 것을 보여 줘야 한다.
            bool food = _tab == 1, egg = _tab == 2;
            if (_foodGridRoot != null) _foodGridRoot.gameObject.SetActive(food);
            if (_foodPanel != null)    _foodPanel.gameObject.SetActive(food);
            if (_eggGridRoot != null)  _eggGridRoot.gameObject.SetActive(egg);
            if (_eggPanel != null)     _eggPanel.gameObject.SetActive(egg);
            if (_panel != null)        _panel.gameObject.SetActive(!food && !egg);
            foreach (var r in _rows) if (r?.Root != null) r.Root.gameObject.SetActive(!food && !egg && r.Filled);
            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i].color = i == _tab ? UiTheme.TabOn : UiTheme.TabOff;

            string[] titles = { Keys.SnailList, Keys.FoodList, Keys.EggList, Keys.Shop };
            _listTitle.text = SnailPet.Data.Loc.Text(titles[_tab]);
            TabChanged?.Invoke(_tab);
        }

        /// <summary>목록을 펼칠지. 상세 패널은 화면에서 제자리에 남는다.</summary>
        public void SetMaximized(bool on)
        {
            Maximized = on;
            _listRoot.gameObject.SetActive(on);
        }

        public bool Maximized { get; private set; }

        // ── 값 넣기 ──

        /// <param name="name">비어 있으면 「이름 없음」이 나간다.</param>
        public void SetSnail(string name, SnailPet.Data.RarityType rarity, int age)
        {
            _nameText.text = string.IsNullOrWhiteSpace(name)
                           ? SnailPet.Data.Loc.Text(Keys.NoName) : name;
            _ageText.text = SnailPet.Data.Loc.Format(Keys.Age, age);

            SetRarity(rarity);
        }

        /// <summary>
        /// 등급 표시. 아이콘이 있으면 아이콘만, 없으면 알약에 enum 이름을 띄운다.
        /// 아트가 들어오는 대로 자동으로 아이콘 쪽으로 넘어간다.
        /// </summary>
        private void SetRarity(SnailPet.Data.RarityType rarity) =>
            ApplyRarity(_rarityIcon, _rarityBadge, _rarityText, rarity);

        /// <summary>
        /// 등급 표시 한 벌. 상세 패널과 목록 행이 같은 뱃지를 쓰므로 여기로 모은다.
        ///
        /// 아이콘이 있으면 아이콘만 남기고 알약과 글자를 끈다. 등급 아트에 글자가
        /// 이미 그려져 있어 알약을 같이 두면 겹친다.
        /// </summary>
        private static void ApplyRarity(Image icon, Image badge, Text text, SnailPet.Data.RarityType rarity)
        {
            string key = SnailPet.Data.Enums.IconOf(rarity);
            var sprite = string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>("Ui/Icon/" + key);

            if (sprite == null && !string.IsNullOrEmpty(key))
                Debug.LogWarning($"[SnailPet] 등급 아이콘을 찾지 못했습니다: Ui/Icon/{key} " +
                                 $"(EnumData 의 RarityType.{rarity} 행)");

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            if (badge != null) badge.enabled = sprite == null;
            if (text != null)  text.text = sprite == null ? rarity.ToString() : "";
        }

        public void SetGauges(float fullRatio, float happyRatio)
        {
            Fill(_fullFill,  At.FullBar,  fullRatio);
            Fill(_happyFill, At.HappyBar, happyRatio);
        }

        private static void Fill(RectTransform fill, RectInt bar, float ratio)
        {
            if (fill == null) return;
            float max = bar.width - 4;
            fill.sizeDelta = new Vector2(Mathf.Max(0f, max * Mathf.Clamp01(ratio)), fill.sizeDelta.y);
        }

        public void SetCoin(long amount) => _coinText.text = amount.ToString("N0");

        /// <summary>
        /// 커서가 위젯 위에 있는가. 창의 클릭 통과를 끌지 정하는 데 쓴다.
        /// 좌표는 가상 화면(왼쪽 위 원점).
        /// </summary>
        public bool ContainsCursor(int virtualX, int virtualY, int vLeft, int vTop, int vHeight)
        {
            var es = EventSystem.current;
            if (es == null) return false;

            // 사각형으로 재면 안 된다. 위젯 상자는 최대화 기준으로 잡혀 있어서 목록을 접었을 때
            // 비어 있는 왼쪽 절반까지 UI 로 잡히고, 그 위에서 바탕화면 클릭이 막힌다.
            // 레이캐스터에 물어보면 실제로 그려진 것만 걸린다.
            _pointer ??= new PointerEventData(es);
            _pointer.position = new Vector2(virtualX - vLeft, vHeight - (virtualY - vTop));

            _hits.Clear();
            es.RaycastAll(_pointer, _hits);
            return _hits.Count > 0;
        }

        private PointerEventData _pointer;
        private readonly System.Collections.Generic.List<RaycastResult> _hits =
            new System.Collections.Generic.List<RaycastResult>();

        // ── 잡동사니 ──

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();

            var module = go.AddComponent<StandaloneInputModule>();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // 창이 포커스를 못 갖는 탓에 유니티 Input 에는 마우스가 안 들어온다. Win32 에서 직접 읽는다.
            module.inputOverride = go.AddComponent<Win32UiInput>();
#endif
        }

        /// <summary>한글이 나오는 글꼴. 내장 Arial 에는 한글 글리프가 없어 네모로 나온다.</summary>
        private static Font LoadKoreanFont()
        {
            foreach (var n in new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "굴림", "Batang" })
            {
                var f = Font.CreateDynamicFontFromOSFont(n, 14);
                if (f != null) return f;
            }

            Debug.LogWarning("[SnailPet] 한글 글꼴을 찾지 못했습니다. 글자가 네모로 나올 수 있습니다.");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>칸을 넘치는 글자를 줄여 맞춘다. 등급 이름이 아이콘으로 바뀌기 전까지의 임시 표시용.</summary>
        private static void Shrink(Text t)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 5;
            t.resizeTextMaxSize = t.fontSize;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>
        /// 목업 좌표(왼쪽 위 원점, y 아래로)를 UGUI 좌표(왼쪽 아래 원점, y 위로)로 옮겨 배치한다.
        /// 목업 값을 그대로 적어 넣을 수 있게 하려는 것이 요점이다.
        /// </summary>
        private static void Place(RectTransform rt, RectInt r)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(r.width, r.height);
            rt.anchoredPosition = new Vector2(r.x, -r.y);
        }

        private Image Box(RectTransform parent, RectInt r, Color color, UiSprites.Shape shape, string name)
        {
            var rt = NewRect(name, parent);
            Place(rt, r);

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiSprites.Of(shape);
            rt.gameObject.AddComponent<UiShapeRef>().Shape = shape;
            img.type = Image.Type.Sliced;
            // 아트에는 색이 이미 칠해져 있다. 거기에 테마색을 곱하면 탁해진다.
            // 게이지 채우기만 예외 — 포만/행복 두 색이 필요해 어쩔 수 없이 물들인다.
            img.color = UiSprites.IsArt(shape) ? Color.white : color;
            img.raycastTarget = false;
            return img;
        }

        private RectTransform Panel(RectTransform parent, RectInt r)
        {
            var fill = Box(parent, r, UiTheme.PanelFill, UiSprites.Shape.Panel, "Panel");
            fill.raycastTarget = true;      // 패널 위에서는 클릭이 바탕화면으로 새면 안 된다

            // 아트 판에는 테두리가 이미 그려져 있다. 위에 또 얹으면 두 겹이 된다.
            if (UiSprites.IsArt(UiSprites.Shape.Panel)) return (RectTransform)fill.transform;

            var line = NewRect("Border", (RectTransform)fill.transform);
            line.anchorMin = Vector2.zero; line.anchorMax = Vector2.one;
            line.offsetMin = Vector2.zero; line.offsetMax = Vector2.zero;

            var img = line.gameObject.AddComponent<Image>();
            img.sprite = UiSprites.Of(UiSprites.Shape.PanelBorder);
            img.type = Image.Type.Sliced;
            img.color = UiTheme.PanelBorder;
            img.raycastTarget = false;

            return (RectTransform)fill.transform;
        }

        private Text Label(RectTransform parent, RectInt r, string text, int size, Color color)
        {
            var rt = NewRect("Text", parent);
            Place(rt, r);

            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private Image Icon(RectTransform parent, RectInt r, string key, Color color, string name)
        {
            var rt = NewRect(name, parent);
            Place(rt, r);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.preserveAspect = true;

            // key 가 null 이면 나중에 채울 자리다. 경고하지 않는다.
            if (key != null)
            {
                img.sprite = Resources.Load<Sprite>("Ui/Icon/" + key);
                if (img.sprite == null)
                    Debug.LogWarning("[SnailPet] UI 아이콘을 찾지 못했습니다: Ui/Icon/" + key);
            }
            return img;
        }

        /// <param name="tint">아이콘 색. 실루엣 아이콘은 기본값(먹색), 색이 들어 있는 아트는 흰색.</param>
        private Button IconButton(RectTransform parent, RectInt r, string key, string name,
                                  Action fire, Color? background = null, Color? tint = null)
        {
            var rt = NewRect(name, parent);
            Place(rt, r);

            var img = rt.gameObject.AddComponent<Image>();
            if (background.HasValue)
            {
                img.sprite = UiSprites.Of(UiSprites.Shape.Button);
                img.type = Image.Type.Sliced;
                img.color = background.Value;
            }
            else
            {
                img.color = new Color(0f, 0f, 0f, 0f);   // 배경이 없어도 클릭은 받아야 한다
            }

            int pad = background.HasValue ? 4 : 1;
            Icon(rt, new RectInt(pad, pad, r.width - pad * 2, r.height - pad * 2),
                 key, tint ?? UiTheme.Ink, "Glyph").raycastTarget = false;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => fire?.Invoke());
            return btn;
        }
    }
}
