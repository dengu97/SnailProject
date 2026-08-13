using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using At = SnailPet.Ui.UiTheme.At;
using Max = SnailPet.Ui.UiTheme.Max;
using Fd = SnailPet.Ui.UiTheme.Food;
using Sh = SnailPet.Ui.UiTheme.Shop;
using Pop = SnailPet.Ui.UiTheme.Popup;
using ShopRow = SnailPet.Data.ShopDataRow;

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

            public const string AskBuy  = "[구매문구]";   // "{0}을(를) 구매할까요?"
            public const string AskSell = "[판매문구]";
            public const string Yes       = "[동의]";
            public const string AskRename = "[이름변경]";   // "이름을 변경합니다."
            public const string DoRename  = "[변경]";
            public const string No      = "[거부]";

            public const string Wardrobe = "[옷장]";
            public const string Traits   = "[보유특징]";
            public const string Worn     = "[장착중]";

            /// <summary>악세서리 부위 이름. 부위를 늘리면 여기에도 한 줄 더해야 한다.</summary>
            public static string PartOf(SnailPet.Data.AccessoriesType t) => t switch
            {
                SnailPet.Data.AccessoriesType.Hat  => "[모자]",
                SnailPet.Data.AccessoriesType.Bag  => "[가방]",
                SnailPet.Data.AccessoriesType.Mask => "[마스크]",
                SnailPet.Data.AccessoriesType.Etc  => "[기타]",
                _ => t.ToString(),
            };

            public const string Today    = "[오늘의추천]";
            public const string BuyIt    = "[구매하기]";
            public const string Preparing = "[준비중]";

            /// <summary>상점 카테고리 이름. <see cref="SnailPet.Data.CategoryType"/> 순서와 맞춰야 한다.</summary>
            public static string CategoryOf(SnailPet.Data.CategoryType c) => c switch
            {
                SnailPet.Data.CategoryType.Food        => "[음식]",
                SnailPet.Data.CategoryType.Egg         => "[달팽이알]",
                SnailPet.Data.CategoryType.Accessories => "[악세서리]",
                SnailPet.Data.CategoryType.Market      => "[자유시장]",
                _ => "[상점]",
            };
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

        // 버튼은 전부 여기에 붙잡아 둔다. 프리팹에 onClick 이 저장되지 않아
        // 살아날 때마다 Rewire 가 다시 붙여야 하기 때문이다.
        [SerializeField] private Button[] _tabBtns, _actionBtns;
        [SerializeField] private Button _renameBtn, _settingsBtn, _closeBtn, _maximizeBtn;
        [SerializeField] private Button _feedBtn, _foodBuyBtn, _foodSellBtn, _eggShopBtn;
        [SerializeField] private Button _pickBuyBtn, _shopBuyBtn, _backBtn;
        [SerializeField] private ListRow[] _rows;
        [SerializeField] private RectTransform _rowGridRoot, _rowContent;
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

            // 구울 때의 글자가 프리팹에 굳어 있다. 시트가 원본이므로 항상 다시 읽는다.
            foreach (var t in GetComponentsInChildren<UiTextRef>(true))
            {
                var text = t.GetComponent<Text>();
                if (text != null && !string.IsNullOrEmpty(t.Token))
                    text.text = SnailPet.Data.Loc.Text(t.Token);
            }

            Rewire();
            EnsureEventSystem();

            // 프리팹에는 편집용으로 펼친 채 구워져 있다. 실행은 접힌 상태로 시작한다.
            SetMaximized(false);
            HidePopup();
            SetTab(_tab);
        }

        /// <summary>
        /// 버튼에 할 일을 붙인다.
        ///
        /// <b>프리팹에는 onClick 이 저장되지 않는다.</b> AddListener 로 단 것은 런타임 전용이라
        /// 굽는 순간 사라진다 — 실제로 프리팹으로 바꾼 뒤 화면의 버튼 146개가 전부 아무 일도
        /// 하지 않고 있었다. 컴파일도 되고 그림도 제대로 나와서 눈치채기 어려웠다.
        ///
        /// 그래서 배선을 짓기에서 떼어 여기 한 곳에 둔다. 코드로 짓든 프리팹에서 살아나든
        /// 항상 이 함수를 지나므로 두 경로가 어긋날 수 없다.
        /// </summary>
        private void Rewire()
        {
            Hook(_renameBtn,   () => Rename?.Invoke());
            Hook(_settingsBtn, () => Settings?.Invoke());
            Hook(_closeBtn,    () => { SetMaximized(false); Close?.Invoke(); });
            Hook(_maximizeBtn, () => { SetMaximized(true);  Maximize?.Invoke(); });

            // 하단 액션 4개. 순서는 BuildActions 의 이름 배열과 같다.
            var actions = new UnityEngine.Events.UnityAction[]
            {
                () => Detail?.Invoke(), () => Wardrobe?.Invoke(),
                () => Gene?.Invoke(),   () => Sell?.Invoke(),
            };
            for (int i = 0; i < Count(_actionBtns) && i < actions.Length; i++) Hook(_actionBtns[i], actions[i]);

            for (int i = 0; i < Count(_tabBtns); i++) { int k = i; Hook(_tabBtns[i], () => SetTab(k)); }
            for (int i = 0; i < Count(_rows); i++)    { int k = i; Hook(_rows[i]?.Swap, () => SwapTo?.Invoke(k)); }

            for (int i = 0; i < Count(_foodSlots); i++) { int k = i; Hook(_foodSlots[i]?.Button, () => SelectFood(k)); }
            for (int i = 0; i < Count(_eggSlots); i++)  { int k = i; Hook(_eggSlots[i]?.Button,  () => SelectEgg(k)); }
            for (int i = 0; i < Count(_shopSlots); i++) { int k = i; Hook(_shopSlots[i]?.Button, () => SelectShopSlot(k)); }

            for (int i = 0; i < Count(_hatchSlots); i++) { int k = i; Hook(_hatchSlots[i]?.Button, () => ClaimHatched?.Invoke(k)); }
            for (int i = 0; i < Count(_shopCats); i++)   { int k = i; Hook(_shopCats[i]?.Button,   () => EnterShopCategory(k)); }

            Hook(_feedBtn, () =>
            {
                if (_selectedFood >= 0 && _selectedFood < _foodIds.Length) FeedFood?.Invoke(_foodIds[_selectedFood]);
            });
            Hook(_favoriteBtn, () =>
            {
                if (_selectedFood >= 0 && _selectedFood < _foodIds.Length)
                    ToggleFavorite?.Invoke(_foodIds[_selectedFood]);
            });
            Hook(_foodBuyBtn,  () => GoShop?.Invoke());
            Hook(_foodSellBtn, () =>
            {
                if (_selectedFood >= 0 && _selectedFood < _foodIds.Length) SellFood?.Invoke(_foodIds[_selectedFood]);
            });
            Hook(_eggShopBtn,  () => GoShop?.Invoke());

            Hook(_pickBuyBtn, () => { if (_pickId > 0) BuyProduct?.Invoke(_pickId, true); });
            Hook(_shopBuyBtn, () =>
            {
                if (_selectedShop >= 0 && _selectedShop < _shopIds.Length)
                    BuyProduct?.Invoke(_shopIds[_selectedShop], false);
            });
            Hook(_backBtn, LeaveShopCategory);

            // 팝업
            Hook(_popupMinus, () => StepPopup(-1));
            Hook(_popupPlus,  () => StepPopup(+1));
            Hook(_popupNo,    HidePopup);
            Hook(_popupClose, HidePopup);
            Hook(_renameOk, () =>
            {
                string name = _renameField != null ? _renameField.text : "";
                HidePopup();
                Renamed?.Invoke(name);
            });
            Hook(_popupYes,   () =>
            {
                int id = _popupItemId, qty = _popupQty;
                HidePopup();
                PopupConfirmed?.Invoke(id, qty);
            });

            // 옷장
            Hook(_wardrobeRenameBtn, () => Rename?.Invoke());
            Hook(_geneRenameBtn, () => Rename?.Invoke());
            for (int i = 0; i < Count(_filters); i++)
            {
                int k = i;
                Hook(_filters[i]?.Button, () => ToggleFilter(k));
            }
            for (int i = 0; i < Count(_wardrobeSlots); i++)
            {
                int k = i;
                Hook(_wardrobeSlots[i]?.Button, () =>
                {
                    if (k < _wardrobeIds.Length) ToggleEquip?.Invoke(_wardrobeIds[k]);
                });
            }
            // 「장착중」 칸을 누르면 그 부위를 벗는다
            for (int i = 0; i < Count(_wornSlots); i++)
            {
                int k = i;
                Hook(_wornSlots[i]?.Button, () =>
                {
                    if (k < _wornIds.Length && _wornIds[k] != 0) ToggleEquip?.Invoke(_wornIds[k]);
                });
            }
        }

        private static int Count(System.Array a) => a?.Length ?? 0;

        /// <summary>같은 버튼에 두 번 붙지 않게 지우고 단다. 프리팹 인스턴스는 비어 있지만 코드로 지은 것은 아니다.</summary>
        private static void Hook(Button btn, UnityEngine.Events.UnityAction fire)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(fire);
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
            BuildShopPanels();
            BuildWardrobePanel();
            BuildGenePanel();
            BuildPopup();

            // 프리팹에는 목록을 펼친 채로 굽는다. 접힌 채로 구우면 프리팹을 열었을 때
            // 왼쪽 절반이 통째로 안 보여 배치를 손볼 수가 없다.
            // 실행할 때는 Bind 가 다시 접으므로 게임이 펼친 채로 시작하지는 않는다.
            SetMaximized(true);

            SetSnail("달팽이 이름", SnailPet.Data.RarityType.Epic, 0);
            SetGauges(0.62f, 0.28f);
            SetCoin(5000);
        }

        /// <summary>이름칸 · 이름 수정 · 등급 뱃지.</summary>
        private void BuildHeader()
        {
            Box(_panel, At.NameField, UiTheme.Slot, UiSprites.Shape.Name, "NameField");
            _renameBtn = IconButton(_panel, At.RenameBtn, "icon_rename", "Rename");

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
            BakeRarity(_rarityIcon, _rarityBadge, _rarityText);

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
            Box(_panel, At.Age, UiTheme.Slot, UiSprites.Shape.LevelBadge, "AgeBadge");
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
            Box(_panel, bar, UiTheme.Slot, UiSprites.Shape.Guage, name + "Track");

            const int inset = 2;
            var fill = Box(_panel, new RectInt(bar.x + inset, bar.y + inset,
                                               bar.width - inset * 2, bar.height - inset * 2),
                           fillColor, fillShape, name + "Fill");

            // 왼쪽을 축으로 가로만 줄였다 늘렸다 한다
            var rt = (RectTransform)fill.transform;
            rt.pivot = new Vector2(0f, 1f);

            Icon(_panel, icon, iconKey, Color.white, name + "Icon").raycastTarget = false;
            return rt;
        }

        /// <summary>하단 액션 4개. 상세정보 · 옷장 · 유전정보 · 판매.</summary>
        private void BuildActions()
        {
            var keys  = new[] { "icon_detail", "icon_wardrobe", "icon_gene", "icon_sell" };
            var names = new[] { "Detail", "Wardrobe", "Gene", "Sell" };

            _actionBtns = new Button[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                _actionBtns[i] = IconButton(_panel, At.Actions[i], keys[i], names[i]);
        }

        /// <summary>패널 밖으로 걸치는 것들. 설정 · 코인 · 닫기 · 최대화.</summary>
        private void BuildOutside()
        {
            _settingsBtn = IconButton(_detailRoot, Above(At.Settings), "icon_settings", "Settings");

            Box(_detailRoot, At.CoinPill, UiTheme.Slot, UiSprites.Shape.Badge, "CoinPill");
            Icon(_detailRoot, At.CoinIcon, "icon_coin", Color.white, "CoinIcon").raycastTarget = false;
            _coinText = Label(_detailRoot, At.CoinText, "5,000", 12, UiTheme.Ink);

            // 이 둘은 다른 아이콘과 달리 아트에 색이 들어 있다. 물들이면 안 된다.
            _closeBtn = IconButton(_detailRoot, Above(At.Close), "btn_close", "Close", tint: Color.white);
            _maximizeBtn = IconButton(_detailRoot, Above(At.Maximize), "btn_maximize", "Maximize", tint: Color.white);
        }

        /// <summary>위젯 상자 기준 좌표로 옮긴다. 목업은 패널 왼쪽 위가 원점이라 코인 줄만큼 내려 준다.</summary>
        private static RectInt Above(RectInt r) => new RectInt(r.x, r.y - At.Coin.y, r.width, r.height);

        private static readonly string[] TabKeys = { "tab_snail", "tab_food", "tab_egg", "tab_shop" };

        /// <summary>
        /// 탭 아트. 고른 탭과 안 고른 탭은 <b>그림 자체가 다르다</b> — 아트에 종이 모양
        /// 배경이 들어 있어 색으로 물들이면 탭 전체가 그 색이 되기 때문이다.
        /// 아직 <c>_on</c> 아트가 없으면 그냥 같은 그림을 쓴다. 그동안은 선택이 안 보인다.
        /// </summary>
        private static string TabArt(int index, bool on)
        {
            if (index < 0 || index >= TabKeys.Length) return null;
            if (!on) return TabKeys[index];

            string key = TabKeys[index] + "_on";
            return Resources.Load<Sprite>("Ui/Icon/" + key) != null ? key : TabKeys[index];
        }

        /// <summary>
        /// 최대화에서 왼쪽에 붙는 목록. 탭 4개 + 목록 패널 + 스크롤되는 행 목록.
        /// 내용은 <see cref="SetRows"/> 로 들어온다.
        /// </summary>
        private void BuildList()
        {
            var tabNames = new[] { "TabSnail", "TabFood", "TabEgg", "TabShop" };

            _tabs = new Image[Max.Tabs.Length];
            _tabBtns = new Button[Max.Tabs.Length];
            for (int i = 0; i < _tabs.Length; i++)
            {
                // 탭 아트에는 종이 모양 배경이 들어 있어 Button 도형을 따로 깔지 않는다.
                _tabBtns[i] = IconButton(_listRoot, Above(Max.Tabs[i]), TabArt(i, false), tabNames[i]);
                _tabs[i] = _tabBtns[i].transform.Find("Glyph").GetComponent<Image>();
            }

            var panel = Panel(_listRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _listTitle = Label(panel, new RectInt(0, 8, UiTheme.PanelW, 16), "", 12, UiTheme.Ink);

            BuildFoodGrid(panel);

            BuildShopCategories(panel);
            BuildWardrobeList(panel);
            BuildGeneList(panel);
            BuildScrollView(panel, "SnailList", Max.RowView, out _rowGridRoot, out _rowContent);

            _rows = new ListRow[Max.RowPool];
            for (int i = 0; i < _rows.Length; i++)
            {
                // 행 좌표는 패널 기준이라 스크롤 영역 안에서는 그만큼 당겨 놓는다
                var r = Max.Row;
                _rows[i] = BuildRow(_rowContent,
                    new RectInt(r.x, r.y - Max.RowView.y + i * Max.RowStep, r.width, r.height), i);
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

        [SerializeField] private Button _favoriteBtn;
        [SerializeField] private Image _favoriteIcon;

        public event Action<int> FoodSelected, FeedFood;

        /// <summary>즐겨찾기 별을 눌렀다. 고른 음식의 Id 가 나간다.</summary>
        public event Action<int> ToggleFavorite;

        /// <summary>음식 상세의 「판매」. 달팽이 판매(Sell)와는 다른 것이라 따로 둔다.</summary>
        public event Action<int> SellFood;

        private static string FavoriteArt(bool on) => on ? "icon_favorite_on" : "icon_favorite_off";

        /// <summary>고른 음식이 즐겨찾기인지. 별 그림을 갈아 끼운다.</summary>
        public void SetFavorite(bool on)
        {
            if (_favoriteIcon == null) return;
            var sprite = Resources.Load<Sprite>("Ui/Icon/" + FavoriteArt(on));
            _favoriteIcon.sprite = sprite;
            _favoriteIcon.enabled = sprite != null;
        }

        /// <summary>
        /// 음식 그리드. 목업의 5번째 줄이 잘려 있어 세로로 스크롤한다.
        ///
        /// 칸은 미리 만들어 두고 보유량에 따라 켜고 끈다. 매번 만들고 지우면
        /// 프리팹으로 구울 수 없고, 스크롤 중에 GC 가 튄다.
        /// </summary>
        private void BuildFoodGrid(RectTransform panel)
        {
            BuildGrid(panel, "FoodGrid", out _foodGridRoot, out _foodContent, out _foodSlots);
            BuildGrid(panel, "EggGrid",  out _eggGridRoot,  out _eggContent,  out _eggSlots);
        }

        /// <summary>
        /// 스크롤되는 4열 그리드. 음식과 알이 목업에서 같은 자리·같은 크기라 그대로 공유한다.
        /// </summary>
        private void BuildGrid(RectTransform panel, string name,
                               out RectTransform root, out RectTransform content, out GridSlot[] slots)
        {
            BuildScrollView(panel, name, Max.FoodView, out root, out content);

            slots = new GridSlot[Max.FoodSlotPool];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = BuildGridSlot(content, i);
        }

        /// <summary>
        /// 세로로만 스크롤되는 영역. 음식·알 그리드와 달팽이 목록이 같은 것을 쓴다.
        ///
        /// 내용 높이는 채울 때 정해지므로 여기서는 보이는 만큼만 잡아 둔다.
        /// 그 높이가 곧 스크롤 범위라, 안 늘리면 아무리 넣어도 안 밀린다.
        /// </summary>
        private void BuildScrollView(RectTransform panel, string name, RectInt at,
                                     out RectTransform root, out RectTransform content)
        {
            root = NewRect(name, panel);
            Place(root, at);
            root.gameObject.SetActive(false);

            // 넘치는 부분을 잘라 낸다. 이게 없으면 패널 밖으로 흘러나온다.
            root.gameObject.AddComponent<RectMask2D>();

            content = NewRect("Content", root);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(UiTheme.PanelW, at.height);

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = root;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
        }

        /// <summary>내용 높이를 줄 수에 맞춘다. 이게 스크롤 범위를 정한다.</summary>
        private static void FitContent(RectTransform content, int count)
        {
            int rows = Mathf.CeilToInt(count / (float)Max.FoodCols);
            content.sizeDelta = new Vector2(UiTheme.PanelW,
                Mathf.Max(Max.FoodView.height, Max.FoodSlot.y + rows * Max.FoodStepY));
        }

        private GridSlot BuildGridSlot(RectTransform content, int index)
        {
            var s = Max.FoodSlot;
            var at = new RectInt(s.x + index % Max.FoodCols * Max.FoodStepX,
                                 s.y + index / Max.FoodCols * Max.FoodStepY, s.width, s.height);

            var root = NewRect("Slot" + index, content);
            Place(root, at);

            var bg = Backdrop(root.gameObject, UiSprites.Shape.Slot2, UiTheme.RowSlot);

            var slot = new GridSlot { Root = root };
            slot.Icon = Icon(root, new RectInt(2, 2, s.width - 4, s.height - 4), null, Color.white, "Icon");
            slot.Icon.raycastTarget = false;

            // 고른 칸에 덧그리는 테두리(slotline). 칸 위에 같은 크기로 겹친다.
            slot.Frame = NewRect("Frame", root).gameObject.AddComponent<Image>();
            Fill((RectTransform)slot.Frame.transform);
            slot.Frame.sprite = UiSprites.Of(UiSprites.Shape.Selection);
            slot.Frame.type = Image.Type.Sliced;
            slot.Frame.gameObject.AddComponent<UiShapeRef>().Shape = UiSprites.Shape.Selection;
            slot.Frame.color = UiSprites.IsArt(UiSprites.Shape.Selection) ? Color.white : UiTheme.Selected;
            slot.Frame.raycastTarget = false;
            slot.Frame.enabled = false;

            slot.Count = Label(root, Max.FoodCount, "", 9, UiTheme.Ink);
            slot.Count.alignment = TextAnchor.LowerRight;

            slot.Button = root.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = bg;

            root.gameObject.SetActive(false);
            return slot;
        }

        /// <summary>음식 탭의 오른쪽 상세 패널. 달팽이 상세와 자리를 바꿔 가며 쓴다.</summary>
        private void BuildFoodDetail()
        {
            _foodPanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _foodPanel.gameObject.SetActive(false);

            // 즐겨찾기 별. 켜짐/꺼짐이 그림 자체가 달라 색으로 표시하지 않는다.
            _favoriteBtn = IconButton(_foodPanel, Fd.Favorite, FavoriteArt(false), "Favorite");
            _favoriteIcon = _favoriteBtn.transform.Find("Glyph").GetComponent<Image>();
            _foodName = Label(_foodPanel, Fd.Name, "", 12, UiTheme.Ink);

            _foodRarityBadge = Box(_foodPanel, Fd.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _foodRarityText  = Label(_foodPanel, Fd.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_foodRarityText);
            _foodRarityIcon = Icon(_foodPanel, Fd.Rarity, null, Color.white, "RarityIcon");
            _foodRarityIcon.raycastTarget = false;

            // 음식에는 등급이 없다. 자리는 목업에 있으므로 비운 모습 그대로 굽는다 —
            // 안 그러면 프리팹에 어두운 알약만 남아 실제 화면과 달라진다.
            _foodRarityIcon.enabled = false;
            _foodRarityBadge.enabled = false;
            _foodRarityText.text = "";

            Box(_foodPanel, Fd.Preview, UiTheme.Slot, UiSprites.Shape.Slot, "Preview");
            _foodIcon = Icon(_foodPanel, Fd.Preview, null, Color.white, "PreviewIcon");
            _foodIcon.raycastTarget = false;

            Icon(_foodPanel, Fd.FullIcon, "icon_food", Color.white, "FullIcon").raycastTarget = false;
            Box(_foodPanel, Fd.FullValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "FullValue");
            _foodFull = Label(_foodPanel, Fd.FullValue, "", 9, UiTheme.Ink);

            Icon(_foodPanel, Fd.HappyIcon, "icon_happy", Color.white, "HappyIcon").raycastTarget = false;
            Box(_foodPanel, Fd.HappyValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "HappyValue");
            _foodHappy = Label(_foodPanel, Fd.HappyValue, "", 9, UiTheme.Ink);

            Box(_foodPanel, Fd.Info, UiTheme.Slot, UiSprites.Shape.Slot, "InfoBox");
            _foodInfo = Label(_foodPanel, new RectInt(Fd.Info.x + 4, Fd.Info.y, Fd.Info.width - 8, Fd.Info.height),
                              "", 8, UiTheme.Ink);
            _foodInfo.horizontalOverflow = HorizontalWrapMode.Wrap;

            var feed = Box(_foodPanel, Fd.Feed, UiTheme.Slot, UiSprites.Shape.Button, "FeedButton");
            feed.raycastTarget = true;
            LocLabel(_foodPanel, Fd.Feed, Keys.Feed, 10, UiTheme.Ink);
            _feedBtn = feed.gameObject.AddComponent<Button>();
            _feedBtn.targetGraphic = feed;

            _foodBuyBtn  = IconButton(_foodPanel, Fd.Buy,  "btn_shop", "Buy");
            _foodSellBtn = IconButton(_foodPanel, Fd.Sell, "icon_sell", "Sell");
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

            LocLabel(_eggPanel, UiTheme.Egg.Title, Keys.Incubator, 12, UiTheme.Ink);

            _hatchSlots = new HatchSlot[UiTheme.Egg.Slots.Length];
            for (int i = 0; i < _hatchSlots.Length; i++)
                _hatchSlots[i] = BuildHatchSlot(i);

            // 알이 하나도 없을 때만 보이는 안내
            _eggEmpty = LocLabel(_eggPanel, UiTheme.Egg.Empty, Keys.NoEgg, 10, UiTheme.Slot);

            _eggShopBtn = IconButton(_eggPanel, UiTheme.Egg.Buy, "btn_shop", "BuyEgg");
        }

        private HatchSlot BuildHatchSlot(int index)
        {
            var at = UiTheme.Egg.Slots[index];
            var root = NewRect("Hatch" + index, _eggPanel);
            Place(root, at);

            var bg = Backdrop(root.gameObject, UiSprites.Shape.Button, UiTheme.Slot);

            var slot = new HatchSlot { Root = root };

            // 빈 칸의 +. 아이콘이 따로 없어 글자로 그린다.
            // 스프라이트 없는 Image 를 쓰면 색으로 꽉 찬 사각형이 나온다.
            slot.Plus = Label(root, new RectInt(0, 0, at.width, at.height), "+", 26, UiTheme.Slot);

            slot.Egg = Icon(root, new RectInt((at.width - 26) / 2, 8, 26, 26), null, Color.white, "Egg");
            slot.Egg.raycastTarget = false;

            slot.Timer = Label(root, new RectInt(0, at.height - 16, at.width, 14), "", 9, UiTheme.Ink);

            slot.Button = root.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = bg;
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
            public Image Thumb;
            public Image RarityBadge, RarityIcon;
            public Text Name, Rarity, Age;
            public Button Swap;
        }

        private ListRow BuildRow(RectTransform parent, RectInt at, int index)
        {
            var rowRt = NewRect("Row" + index, parent);
            Place(rowRt, at);

            var bg = Backdrop(rowRt.gameObject, UiSprites.Shape.Slot, UiTheme.Slot);

            var row = new ListRow
            {
                Root   = rowRt,
                Thumb  = Box(rowRt, Max.RowThumb, UiTheme.RowSlot, UiSprites.Shape.Slot2, "Thumb"),
                Name   = Label(rowRt, Max.RowName, "", 11, UiTheme.Ink),
                Rarity = null,
                Age    = null,
            };

            row.RarityBadge = Box(rowRt, Max.RowRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            row.Rarity = Label(rowRt, Max.RowRarity, "", 8, UiTheme.OnBadge);
            Shrink(row.Rarity);
            row.RarityIcon = Icon(rowRt, Max.RowRarity, null, Color.white, "RarityIcon");
            row.RarityIcon.raycastTarget = false;
            BakeRarity(row.RarityIcon, row.RarityBadge, row.Rarity);

            Box(rowRt, Max.RowAge, UiTheme.Slot, UiSprites.Shape.LevelBadge, "AgeBadge");
            row.Age = Label(rowRt, Max.RowAge, "", 8, UiTheme.Ink);

            int captured = index;
            row.Swap = IconButton(rowRt, Max.RowSwap, "icon_swap", "Swap");
            return row;
        }

        /// <summary>지금 나와 있는 달팽이는 교체 버튼이 없다 (목업 주석).</summary>
        public void SetRows((string name, SnailPet.Data.RarityType rarity, int age, bool isActive)[] rows)
        {
            int count = rows?.Length ?? 0;
            if (count > _rows.Length)
                Debug.LogWarning($"[SnailPet] 달팽이 {count}마리 중 {_rows.Length}마리만 목록에 나옵니다 " +
                                 $"(UiTheme.Max.RowPool)");

            // 내용 높이가 스크롤 범위를 정한다. 안 늘리면 5마리째부터 밀리지 않는다.
            _rowContent.sizeDelta = new Vector2(UiTheme.PanelW,
                Mathf.Max(Max.RowView.height,
                          Max.Row.y - Max.RowView.y + Mathf.Min(count, _rows.Length) * Max.RowStep));

            for (int i = 0; i < _rows.Length; i++)
            {
                bool has = i < count;
                _rows[i].Root.gameObject.SetActive(has);
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

            // 탭을 누르면 옷장·상세보기에서 빠져나온다. 둘 다 왼쪽 패널을 통째로 쓰기 때문이다.
            if (_inWardrobe || _inGene)
            {
                _inWardrobe = false;
                _inGene = false;
                if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
                if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);
                if (_geneRoot != null)      _geneRoot.gameObject.SetActive(false);
                if (_genePanel != null)     _genePanel.gameObject.SetActive(false);
            }

            // 탭이 왼쪽 목록과 오른쪽 상세를 함께 바꾼다. 둘은 항상 같은 것을 보여 줘야 한다.
            bool food = _tab == 1, egg = _tab == 2, shop = _tab == 3;
            if (_foodGridRoot != null) _foodGridRoot.gameObject.SetActive(food);
            if (_foodPanel != null)    _foodPanel.gameObject.SetActive(food);
            if (_eggGridRoot != null)  _eggGridRoot.gameObject.SetActive(egg);
            if (_eggPanel != null)     _eggPanel.gameObject.SetActive(egg);
            if (_panel != null)        _panel.gameObject.SetActive(!food && !egg && !shop);
            if (_rowGridRoot != null)  _rowGridRoot.gameObject.SetActive(!food && !egg && !shop);

            // 상점은 들어올 때마다 카테고리 목록에서 시작한다
            _shopCat = -1;
            ApplyShopStage();
            // 아트가 배경까지 들고 있어 색으로는 구분할 수 없다. 그림을 갈아 끼운다.
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null) continue;
                var sprite = Resources.Load<Sprite>("Ui/Icon/" + TabArt(i, i == _tab));
                if (sprite != null) { _tabs[i].sprite = sprite; _tabs[i].color = Color.white; }
                else _tabs[i].color = i == _tab ? UiTheme.TabOn : UiTheme.TabOff;
            }

            string[] titles = { Keys.SnailList, Keys.FoodList, Keys.EggList, Keys.Shop };
            _listTitle.text = SnailPet.Data.Loc.Text(titles[_tab]);
            TabChanged?.Invoke(_tab);
        }

        // ── 상점 탭 ──
        //
        // 두 단계다. 카테고리를 고르면 그 안의 상품 그리드로 들어가고, 뒤로 가면 돌아온다.
        // 카테고리 행은 달팽이 목록 행과, 상품 그리드는 음식 그리드와 자리가 같아
        // (목업 실측) 같은 부품을 쓴다.

        [Serializable]
        public sealed class ShopCategory
        {
            public RectTransform Root;
            public Text Name;
            public Button Button;
        }

        [SerializeField] private RectTransform _shopCatRoot;
        [SerializeField] private ShopCategory[] _shopCats;
        [SerializeField] private RectTransform _shopGridRoot, _shopGridContent;
        [SerializeField] private GridSlot[] _shopSlots;

        [SerializeField] private RectTransform _shopPanel, _shopItemPanel;
        [SerializeField] private RectTransform _shopBack;
        [SerializeField] private Image _pickIcon, _pickRarityBadge, _pickRarityIcon, _pickStrike;
        [SerializeField] private Text _pickName, _pickRarityText, _pickCost, _pickWas;
        [SerializeField] private Image _shopIcon, _shopRarityBadge, _shopRarityIcon;
        [SerializeField] private Text _shopName, _shopRarityText, _shopInfo, _shopCost;
        [SerializeField] private Text _shopFull, _shopHappy;
        [SerializeField] private RectTransform _shopStats;   // 포만·행복 묶음. 음식이 아니면 숨긴다

        /// <summary>-1 이면 카테고리 목록, 아니면 그 카테고리의 상품 그리드.</summary>
        private int _shopCat = -1;
        private int[] _shopIds = new int[0];
        private int _selectedShop = -1;
        private int _pickId;

        /// <summary>
        /// 상품을 사겠다고 눌렀다. ShopData 의 Id 와, 오늘의 할인 칸에서 눌렀는지가 나간다.
        /// 할인가는 그 칸에서만 적용되므로 어디서 눌렀는지를 같이 알려야 한다.
        /// </summary>
        public event Action<int, bool> BuyProduct;

        private void BuildShopCategories(RectTransform panel)
        {
            // 카테고리는 넷뿐이라 패널에 그대로 들어간다. 스크롤을 붙일 이유가 없다.
            _shopCatRoot = NewRect("ShopCategories", panel);
            Place(_shopCatRoot, Max.RowView);
            _shopCatRoot.gameObject.SetActive(false);

            var cats = SnailPet.Snail.Shop.Categories;
            _shopCats = new ShopCategory[cats.Length];

            for (int i = 0; i < cats.Length; i++)
            {
                var r = Max.Row;
                var at = new RectInt(r.x, r.y - Max.RowView.y + i * Max.RowStep, r.width, r.height);

                var root = NewRect("Category" + i, _shopCatRoot);
                Place(root, at);

                var bg = Backdrop(root.gameObject, UiSprites.Shape.Slot, UiTheme.Slot);

                var name = LocLabel(root, UiTheme.Shop.CategoryName, Keys.CategoryOf(cats[i]), 11, UiTheme.Ink);
                name.alignment = TextAnchor.MiddleLeft;

                var btn = root.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;

                _shopCats[i] = new ShopCategory { Root = root, Name = name, Button = btn };
            }

            BuildGrid(panel, "ShopGrid", out _shopGridRoot, out _shopGridContent, out _shopSlots);
        }

        /// <summary>오늘의 추천 패널과 상품 상세 패널. 둘 중 하나만 떠 있다.</summary>
        private void BuildShopPanels()
        {
            // ── 오늘의 추천 ──
            _shopPanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _shopPanel.gameObject.SetActive(false);

            LocLabel(_shopPanel, Sh.Title, Keys.Today, 12, UiTheme.Ink);

            _pickRarityBadge = Box(_shopPanel, Sh.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "PickRarity");
            _pickRarityText  = Label(_shopPanel, Sh.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_pickRarityText);
            _pickRarityIcon = Icon(_shopPanel, Sh.Rarity, null, Color.white, "PickRarityIcon");
            _pickRarityIcon.raycastTarget = false;
            BakeRarity(_pickRarityIcon, _pickRarityBadge, _pickRarityText);

            Box(_shopPanel, Sh.Preview, UiTheme.Slot, UiSprites.Shape.Slot, "PickPreview");
            _pickIcon = Icon(_shopPanel, Sh.Preview, null, Color.white, "PickIcon");
            _pickIcon.raycastTarget = false;

            _pickName = Label(_shopPanel, Sh.Name, "", 12, UiTheme.Ink);
            Icon(_shopPanel, Sh.PickCoin, "icon_coin", Color.white, "PickCoinIcon").raycastTarget = false;

            // 원가에는 취소선이 그이고 할인가는 빨갛다 (목업).
            // 취소선은 아트가 아니라 얇은 사각형이다 — 스프라이트 없는 Image 가 색으로 꽉 차는
            // 성질을 여기서는 일부러 쓴다.
            _pickWas = Label(_shopPanel, Sh.PickWas, "", 11, UiTheme.Slot);
            _pickStrike = NewRect("PickStrike", _shopPanel).gameObject.AddComponent<Image>();
            Place((RectTransform)_pickStrike.transform, Sh.PickStrike);
            _pickStrike.color = UiTheme.Slot;
            _pickStrike.raycastTarget = false;
            _pickCost = Label(_shopPanel, Sh.PickNow, "", 11, UiTheme.Discount);

            var pickBuy = Box(_shopPanel, Sh.PickBuy, UiTheme.Slot, UiSprites.Shape.Button, "PickBuy");
            pickBuy.raycastTarget = true;
            LocLabel(_shopPanel, Sh.PickBuy, Keys.BuyIt, 10, UiTheme.Ink);
            _pickBuyBtn = pickBuy.gameObject.AddComponent<Button>();
            _pickBuyBtn.targetGraphic = pickBuy;

            // ── 상품 상세 ── 음식 상세와 같은 자리를 쓰고 하단 버튼만 다르다
            _shopItemPanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _shopItemPanel.gameObject.SetActive(false);

            _shopName = Label(_shopItemPanel, Fd.Name, "", 12, UiTheme.Ink);

            _shopRarityBadge = Box(_shopItemPanel, Fd.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _shopRarityText  = Label(_shopItemPanel, Fd.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_shopRarityText);
            _shopRarityIcon = Icon(_shopItemPanel, Fd.Rarity, null, Color.white, "RarityIcon");
            _shopRarityIcon.raycastTarget = false;
            BakeRarity(_shopRarityIcon, _shopRarityBadge, _shopRarityText);

            Box(_shopItemPanel, Fd.Preview, UiTheme.Slot, UiSprites.Shape.Slot, "Preview");
            _shopIcon = Icon(_shopItemPanel, Fd.Preview, null, Color.white, "PreviewIcon");
            _shopIcon.raycastTarget = false;

            // 포만·행복은 음식에만 있다. 한 상자에 묶어 통째로 껐다 켠다.
            _shopStats = NewRect("Stats", _shopItemPanel);
            _shopStats.anchorMin = Vector2.zero; _shopStats.anchorMax = Vector2.one;
            _shopStats.offsetMin = Vector2.zero; _shopStats.offsetMax = Vector2.zero;

            Icon(_shopStats, Fd.FullIcon, "icon_food", Color.white, "FullIcon").raycastTarget = false;
            Box(_shopStats, Fd.FullValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "FullValue");
            _shopFull = Label(_shopStats, Fd.FullValue, "", 9, UiTheme.Ink);

            Icon(_shopStats, Fd.HappyIcon, "icon_happy", Color.white, "HappyIcon").raycastTarget = false;
            Box(_shopStats, Fd.HappyValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "HappyValue");
            _shopHappy = Label(_shopStats, Fd.HappyValue, "", 9, UiTheme.Ink);

            Box(_shopItemPanel, Fd.Info, UiTheme.Slot, UiSprites.Shape.Slot, "InfoBox");
            _shopInfo = Label(_shopItemPanel, new RectInt(Fd.Info.x + 4, Fd.Info.y, Fd.Info.width - 8, Fd.Info.height),
                              "", 8, UiTheme.Ink);
            _shopInfo.horizontalOverflow = HorizontalWrapMode.Wrap;

            var buy = Box(_shopItemPanel, Sh.Buy, UiTheme.Slot, UiSprites.Shape.Button, "BuyButton");
            buy.raycastTarget = true;

            // 글자와 가격은 버튼의 자식이다. 살 것이 없을 때 버튼을 끄면 같이 사라져야 한다.
            var buyRt = (RectTransform)buy.transform;
            LocLabel(buyRt, Sh.BuyLabel, Keys.BuyIt, 10, UiTheme.Ink);
            Icon(buyRt, Sh.BuyCoin, "icon_coin", Color.white, "BuyCoinIcon").raycastTarget = false;
            _shopCost = Label(buyRt, Sh.BuyCost, "", 10, UiTheme.Ink);
            _shopCost.alignment = TextAnchor.MiddleLeft;

            _shopBuyBtn = buy.gameObject.AddComponent<Button>();
            _shopBuyBtn.targetGraphic = buy;

            // 뒤로 가기. 목업에서 닫기 X 자리에 화살표가 들어온다.
            // btn_back 아트가 아직 없어 글자로 그린다 — 스프라이트 없는 Image 는 색 사각형이 된다.
            var back = Box(_detailRoot, Above(Sh.Back), UiTheme.PanelBorder, UiSprites.Shape.Button, "Back");
            back.raycastTarget = true;
            Label(_detailRoot, Above(Sh.Back), "←", 16, Color.white);
            _backBtn = back.gameObject.AddComponent<Button>();
            _backBtn.targetGraphic = back;
            _shopBack = (RectTransform)back.transform;
            _shopBack.gameObject.SetActive(false);
        }

        /// <summary>카테고리를 골랐다. 그 안의 상품 그리드로 들어간다.</summary>
        private void EnterShopCategory(int index)
        {
            var cats = SnailPet.Snail.Shop.Categories;
            if (index < 0 || index >= cats.Length) return;

            _shopCat = index;
            var products = SnailPet.Snail.Shop.ProductsOf(cats[index]);

            _shopIds = new int[Mathf.Min(products.Length, _shopSlots.Length)];
            for (int i = 0; i < _shopSlots.Length; i++)
            {
                bool has = i < _shopIds.Length;
                _shopSlots[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                _shopIds[i] = products[i].Id;
                _shopSlots[i].Icon.sprite = ProductSprite(products[i]);
                _shopSlots[i].Icon.enabled = _shopSlots[i].Icon.sprite != null;
                _shopSlots[i].Count.text = products[i].ItemCount > 1 ? products[i].ItemCount.ToString() : "";
                _shopSlots[i].Frame.enabled = false;
            }
            FitContent(_shopGridContent, _shopIds.Length);

            _listTitle.text = SnailPet.Data.Loc.Text(Keys.CategoryOf(cats[index]));
            ApplyShopStage();
            SelectShopSlot(_shopIds.Length > 0 ? 0 : -1);
        }

        private void LeaveShopCategory()
        {
            _shopCat = -1;
            _listTitle.text = SnailPet.Data.Loc.Text(Keys.Shop);
            ApplyShopStage();
        }

        /// <summary>
        /// 카테고리 단계인지 상품 단계인지에 따라 양쪽 패널을 맞춘다.
        /// 짓는 도중에도 <see cref="SetTab"/> 을 거쳐 불리므로 아직 없는 것은 건너뛴다.
        /// </summary>
        private void ApplyShopStage()
        {
            bool shop = _tab == 3;
            bool inCategory = shop && _shopCat >= 0;

            if (_shopCatRoot != null)   _shopCatRoot.gameObject.SetActive(shop && !inCategory);
            if (_shopGridRoot != null)  _shopGridRoot.gameObject.SetActive(inCategory);
            if (_shopPanel != null)     _shopPanel.gameObject.SetActive(shop && !inCategory);
            if (_shopItemPanel != null) _shopItemPanel.gameObject.SetActive(inCategory);

            // 뒤로가 나올 때는 닫기가 그 자리를 비켜 준다
            if (_shopBack != null)  _shopBack.gameObject.SetActive(inCategory);
            if (_closeBtn != null)  _closeBtn.gameObject.SetActive(!inCategory);
        }

        private void SelectShopSlot(int index)
        {
            _selectedShop = index;
            for (int i = 0; i < _shopSlots.Length; i++)
                _shopSlots[i].Frame.enabled = i == index;

            // 상품이 없는 카테고리(자유시장)를 열면 고를 것이 없다.
            // 그냥 두면 직전에 보던 상품이 그대로 남아 그걸 살 수 있는 것처럼 보인다.
            if (index < 0 || index >= _shopIds.Length)
            {
                _shopName.text = SnailPet.Data.Loc.Text(Keys.Preparing);
                _shopInfo.text = "";
                _shopCost.text = "";
                _shopIcon.enabled = false;
                _shopRarityBadge.enabled = false;
                _shopRarityIcon.enabled = false;
                _shopRarityText.text = "";
                _shopStats.gameObject.SetActive(false);
                if (_shopBuyBtn != null) _shopBuyBtn.gameObject.SetActive(false);
                return;
            }
            if (_shopBuyBtn != null) _shopBuyBtn.gameObject.SetActive(true);

            ShopRow row = null;
            foreach (var r in SnailPet.Data.GameData.ShopData)
                if (r.Id == _shopIds[index]) { row = r; break; }
            if (row == null) return;

            _shopName.text = SnailPet.Snail.Shop.NameOf(row);
            _shopCost.text = row.CostCount.HasValue ? row.CostCount.Value.ToString("N0") : "-";
            _shopIcon.sprite = ProductSprite(row);
            _shopIcon.enabled = _shopIcon.sprite != null;

            ApplyRarity(_shopRarityIcon, _shopRarityBadge, _shopRarityText, row.RarityType);

            // 포만·행복은 음식에만 있다
            bool isFood = row.CategoryType == SnailPet.Data.CategoryType.Food
                       && SnailPet.Data.GameData.FoodDataById.TryGetValue(row.Id, out var food);
            _shopStats.gameObject.SetActive(isFood);
            if (isFood)
            {
                var f = SnailPet.Data.GameData.FoodDataById[row.Id];
                _shopFull.text  = f.FullPoint.ToString("0");
                _shopHappy.text = f.HappyPoint.ToString("0");
                _shopInfo.text  = SnailPet.Data.Loc.ById(f.InfoId);
            }
            else _shopInfo.text = InfoOf(row);
        }

        private static string InfoOf(ShopRow row)
        {
            switch (row.CategoryType)
            {
                case SnailPet.Data.CategoryType.Egg:
                    return SnailPet.Data.GameData.EggDataById.TryGetValue(row.Id, out var e)
                         ? SnailPet.Data.Loc.ById(e.InfoId) : string.Empty;
                case SnailPet.Data.CategoryType.Accessories:
                    return SnailPet.Data.GameData.AccessoriesDataById.TryGetValue(row.Id, out var a)
                         ? SnailPet.Data.Loc.ById(a.InfoId) : string.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>상품 그림. 카테고리마다 아트가 있는 폴더가 다르다.</summary>
        private static Sprite ProductSprite(ShopRow row)
        {
            string folder = row.CategoryType switch
            {
                SnailPet.Data.CategoryType.Food        => "Food",
                SnailPet.Data.CategoryType.Egg         => "Egg",
                SnailPet.Data.CategoryType.Accessories => "Accessories",
                _ => "Item",
            };

            string key = row.CategoryType switch
            {
                SnailPet.Data.CategoryType.Food =>
                    SnailPet.Data.GameData.FoodDataById.TryGetValue(row.Id, out var f) ? f.ResourceKey : null,
                SnailPet.Data.CategoryType.Egg =>
                    SnailPet.Data.GameData.EggDataById.TryGetValue(row.Id, out var e) ? e.ResourceKey : null,
                SnailPet.Data.CategoryType.Accessories =>
                    SnailPet.Data.GameData.AccessoriesDataById.TryGetValue(row.Id, out var a) ? a.ResourceKey : null,
                _ => SnailPet.Data.GameData.ItemDataById.TryGetValue(row.Id, out var i) ? i.ResourceKey : null,
            };

            return string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>("Snail/" + folder + "/" + key);
        }

        /// <summary>
        /// 오늘의 할인을 그린다. 할인 중이면 원가에 취소선을 긋고 할인가를 빨갛게 옆에 놓는다.
        /// 할인 상품이 하나도 없으면 row 가 null 로 들어온다.
        /// </summary>
        public void SetTodayPick(ShopRow row)
        {
            _pickId = row?.Id ?? 0;

            bool has = row != null;
            _pickIcon.enabled = has;
            _pickCost.enabled = has;
            _pickWas.enabled = has;
            _pickStrike.enabled = has;
            if (_pickBuyBtn != null) _pickBuyBtn.gameObject.SetActive(has);

            if (!has)
            {
                _pickName.text = SnailPet.Data.Loc.Text(Keys.Preparing);
                _pickRarityBadge.enabled = false;
                _pickRarityIcon.enabled = false;
                _pickRarityText.text = "";
                return;
            }

            _pickName.text = SnailPet.Snail.Shop.NameOf(row);
            _pickIcon.sprite = ProductSprite(row);
            _pickIcon.enabled = _pickIcon.sprite != null;
            ApplyRarity(_pickRarityIcon, _pickRarityBadge, _pickRarityText, row.RarityType);

            bool sale = SnailPet.Snail.Shop.IsDiscounted(row);
            _pickWas.enabled = sale;
            _pickStrike.enabled = sale;

            if (sale)
            {
                _pickWas.text  = row.CostCount.Value.ToString("N0");
                _pickCost.text = row.DiscountCostCount.Value.ToString("N0");
                Place((RectTransform)_pickCost.transform, Sh.PickNow);

                // 취소선을 글자 폭에 맞춘다. 칸 폭 그대로면 짧은 숫자에서 허공까지 그어진다.
                var line = (RectTransform)_pickStrike.transform;
                float w = Mathf.Min(_pickWas.preferredWidth + 2f, Sh.PickStrike.width);
                line.sizeDelta = new Vector2(w, Sh.PickStrike.height);
            }
            else
            {
                // 할인이 아니면 가격 하나만 가운데에. 색도 되돌린다.
                _pickCost.text = row.CostCount.HasValue ? row.CostCount.Value.ToString("N0") : "-";
                Place((RectTransform)_pickCost.transform, Sh.PickOnly);
            }
            _pickCost.color = sale ? UiTheme.Discount : UiTheme.Ink;
        }

        /// <summary>산 뒤에 그리드의 수량 표시를 새로 그린다. 상품이 늘거나 줄 수 있다.</summary>
        public void RefreshShop()
        {
            if (_shopCat >= 0) EnterShopCategory(_shopCat);
        }

        // ── 옷장 ──
        //
        // 탭이 아니라 상세 패널의 「옷장」 버튼으로 들어가는 모드다. 들어가면 왼쪽은
        // 목록 대신 옷장(부위 필터 + 보유 악세서리)이 되고, 오른쪽은 입은 모습이 된다.
        // 탭을 누르면 빠져나온다.

        [Serializable]
        public sealed class FilterChip
        {
            public RectTransform Root;
            public Image Box;
            public Text Label;
            public Button Button;
            public bool On;
        }

        [SerializeField] private RectTransform _wardrobeRoot, _wardrobeContent, _wardrobePanel;
        [SerializeField] private GridSlot[] _wardrobeSlots;
        [SerializeField] private FilterChip[] _filters;
        [SerializeField] private RawImage _wardrobePreview;
        [SerializeField] private GridSlot[] _wornSlots;

        private int[] _wardrobeIds = new int[0];
        private bool _inWardrobe;

        /// <summary>악세서리를 끼거나 뺐다. AccessoriesData 의 Id 가 나간다.</summary>
        public event Action<int> ToggleEquip;

        public bool InWardrobe => _inWardrobe;

        private static SnailPet.Data.AccessoriesType[] Parts =>
            (SnailPet.Data.AccessoriesType[])Enum.GetValues(typeof(SnailPet.Data.AccessoriesType));

        /// <summary>왼쪽 옷장: 부위 필터 한 줄 + 보유 악세서리 그리드.</summary>
        private void BuildWardrobeList(RectTransform panel)
        {
            _wardrobeRoot = NewRect("Wardrobe", panel);
            Place(_wardrobeRoot, new RectInt(0, 0, UiTheme.PanelW, UiTheme.PanelH));
            _wardrobeRoot.gameObject.SetActive(false);

            var parts = Parts;
            _filters = new FilterChip[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var f = UiTheme.Wardrobe.Filter;
                var at = new RectInt(f.x + i * UiTheme.Wardrobe.FilterStep, f.y, f.width, f.height);

                var root = NewRect("Filter" + parts[i], _wardrobeRoot);
                Place(root, at);

                var box = Backdrop(root.gameObject, UiSprites.Shape.LevelBadge, UiTheme.Slot);
                var label = LocLabel(root, new RectInt(0, 0, at.width, at.height), Keys.PartOf(parts[i]), 8, UiTheme.Ink);

                var btn = root.gameObject.AddComponent<Button>();
                btn.targetGraphic = box;

                _filters[i] = new FilterChip { Root = root, Box = box, Label = label, Button = btn, On = true };
            }

            BuildScrollView(_wardrobeRoot, "WardrobeGrid", UiTheme.Wardrobe.View,
                            out var gridRoot, out _wardrobeContent);
            gridRoot.gameObject.SetActive(true);   // 옷장 루트가 통째로 켜고 꺼진다

            _wardrobeSlots = new GridSlot[Max.FoodSlotPool];
            for (int i = 0; i < _wardrobeSlots.Length; i++)
                _wardrobeSlots[i] = BuildGridSlot(_wardrobeContent, i);
        }

        /// <summary>오른쪽 옷장 패널: 이름·등급 + 입은 모습 + 지금 낀 것들.</summary>
        private void BuildWardrobePanel()
        {
            _wardrobePanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _wardrobePanel.gameObject.SetActive(false);

            Box(_wardrobePanel, At.NameField, UiTheme.Slot, UiSprites.Shape.Name, "NameField");
            _wardrobeName = Label(_wardrobePanel, At.NameField, "", 12, UiTheme.Ink);
            _wardrobeRenameBtn = IconButton(_wardrobePanel, At.RenameBtn, "icon_rename", "Rename");

            _wardrobeRarityBadge = Box(_wardrobePanel, At.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _wardrobeRarityText  = Label(_wardrobePanel, At.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_wardrobeRarityText);
            _wardrobeRarityIcon = Icon(_wardrobePanel, At.Rarity, null, Color.white, "RarityIcon");
            _wardrobeRarityIcon.raycastTarget = false;
            BakeRarity(_wardrobeRarityIcon, _wardrobeRarityBadge, _wardrobeRarityText);

            // 입은 모습. 초상과 같은 방식이지만 옷장은 세로로 더 긴 자리를 쓴다.
            var pv = NewRect("Preview", _wardrobePanel);
            Place(pv, UiTheme.Wardrobe.Preview);
            _wardrobePreview = pv.gameObject.AddComponent<RawImage>();
            _wardrobePreview.raycastTarget = false;

            Box(_wardrobePanel, UiTheme.Wardrobe.WornBox, UiTheme.Slot, UiSprites.Shape.Slot, "WornBox");
            LocLabel(_wardrobePanel, UiTheme.Wardrobe.WornTitle, Keys.Worn, 9, UiTheme.Ink);

            // 낀 것을 부위별로 한 칸씩 보여 준다. 부위가 늘면 칸도 같이 는다.
            var parts = Parts;
            _wornSlots = new GridSlot[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var w = UiTheme.Wardrobe.WornSlot;
                var at = new RectInt(w.x + i * UiTheme.Wardrobe.WornStep, w.y, w.width, w.height);

                var root = NewRect("Worn" + parts[i], _wardrobePanel);
                Place(root, at);

                var bg = Backdrop(root.gameObject, UiSprites.Shape.Slot2, UiTheme.RowSlot);
                var slot = new GridSlot { Root = root, Button = root.gameObject.AddComponent<Button>() };
                slot.Button.targetGraphic = bg;
                slot.Icon = Icon(root, new RectInt(2, 2, at.width - 4, at.height - 4), null, Color.white, "Icon");
                slot.Icon.raycastTarget = false;
                slot.Count = Label(root, new RectInt(0, 0, at.width, at.height), "", 8, UiTheme.Ink);
                _wornSlots[i] = slot;
            }
        }

        [SerializeField] private Text _wardrobeName, _wardrobeRarityText;
        [SerializeField] private Image _wardrobeRarityBadge, _wardrobeRarityIcon;
        [SerializeField] private Button _wardrobeRenameBtn;

        /// <summary>옷장에 들어가거나 나온다.</summary>
        public void OpenWardrobe(bool on)
        {
            _inWardrobe = on;
            if (on)
            {
                // 옷장과 상세보기는 같은 자리를 쓰므로 하나만 떠 있어야 한다.
                // ApplyGene 을 부르면 안 된다 — 꺼질 때 SetTab 으로 되돌리기 때문이다.
                _inGene = false;
                if (_geneRoot != null)  _geneRoot.gameObject.SetActive(false);
                if (_genePanel != null) _genePanel.gameObject.SetActive(false);
                SetMaximized(true);
            }
            ApplyWardrobe();
        }

        private void ApplyWardrobe()
        {
            if (_wardrobeRoot == null) return;

            _wardrobeRoot.gameObject.SetActive(_inWardrobe);
            _wardrobePanel.gameObject.SetActive(_inWardrobe);

            // 옷장에 있는 동안에는 목록·그리드·상세가 전부 물러난다
            if (_inWardrobe)
            {
                if (_rowGridRoot != null)  _rowGridRoot.gameObject.SetActive(false);
                if (_foodGridRoot != null) _foodGridRoot.gameObject.SetActive(false);
                if (_eggGridRoot != null)  _eggGridRoot.gameObject.SetActive(false);
                if (_shopCatRoot != null)  _shopCatRoot.gameObject.SetActive(false);
                if (_shopGridRoot != null) _shopGridRoot.gameObject.SetActive(false);
                if (_panel != null)        _panel.gameObject.SetActive(false);
                if (_foodPanel != null)    _foodPanel.gameObject.SetActive(false);
                if (_eggPanel != null)     _eggPanel.gameObject.SetActive(false);
                if (_shopPanel != null)    _shopPanel.gameObject.SetActive(false);
                if (_shopItemPanel != null)_shopItemPanel.gameObject.SetActive(false);
                _listTitle.text = SnailPet.Data.Loc.Text(Keys.Wardrobe);
            }
            else SetTab(_tab);   // 있던 탭으로 되돌린다
        }

        /// <summary>입은 모습을 그릴 텍스처. 장착이 바뀔 때마다 다시 찍어 넣는다.</summary>
        public void SetWardrobePreview(Texture texture)
        {
            if (_wardrobePreview == null) return;
            _wardrobePreview.texture = texture;
            _wardrobePreview.enabled = texture != null;
        }

        public static Vector2Int WardrobePreviewSize =>
            new Vector2Int(UiTheme.Wardrobe.Preview.width, UiTheme.Wardrobe.Preview.height);

        /// <summary>
        /// 옷장 내용을 채운다.
        /// <paramref name="owned"/> 는 (악세서리 Id, 개수), <paramref name="equipped"/> 는 낀 것들.
        /// </summary>
        public void SetWardrobe(string name, SnailPet.Data.RarityType rarity,
                                (int accessoryId, int count)[] owned, int[] equipped)
        {
            _wardrobeName.text = string.IsNullOrWhiteSpace(name)
                               ? SnailPet.Data.Loc.Text(Keys.NoName) : name;
            ApplyRarity(_wardrobeRarityIcon, _wardrobeRarityBadge, _wardrobeRarityText, rarity);

            // 꺼 놓은 부위는 목록에서 빠진다
            var shown = new System.Collections.Generic.List<int>();
            if (owned != null)
                foreach (var o in owned)
                    if (SnailPet.Data.GameData.AccessoriesDataById.TryGetValue(o.accessoryId, out var row)
                        && FilterOn(row.AccessoriesType))
                        shown.Add(o.accessoryId);

            _wardrobeIds = shown.ToArray();

            for (int i = 0; i < _wardrobeSlots.Length; i++)
            {
                bool has = i < _wardrobeIds.Length;
                _wardrobeSlots[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                int id = _wardrobeIds[i];
                _wardrobeSlots[i].Icon.sprite = AccessorySprite(id);
                _wardrobeSlots[i].Icon.enabled = _wardrobeSlots[i].Icon.sprite != null;

                // 낀 것은 빨간 테두리와 「장착중」으로 표시한다 (목업)
                bool worn = equipped != null && Array.IndexOf(equipped, id) >= 0;
                _wardrobeSlots[i].Frame.enabled = worn;
                _wardrobeSlots[i].Count.text = worn ? SnailPet.Data.Loc.Text(Keys.Worn) : "";
                _wardrobeSlots[i].Count.alignment = TextAnchor.MiddleCenter;
            }
            FitContent(_wardrobeContent, _wardrobeIds.Length);

            // 아래쪽 「장착중」 칸은 부위 순서대로 고정이다
            var parts = Parts;
            for (int i = 0; i < _wornSlots.Length && i < parts.Length; i++)
            {
                int id = 0;
                if (equipped != null)
                    foreach (int e in equipped)
                        if (SnailPet.Data.GameData.AccessoriesDataById.TryGetValue(e, out var row)
                            && row.AccessoriesType == parts[i]) { id = e; break; }

                _wornSlots[i].Icon.sprite = id == 0 ? null : AccessorySprite(id);
                _wornSlots[i].Icon.enabled = _wornSlots[i].Icon.sprite != null;
                _wornSlots[i].Count.text = "";
            }
            _wornIds = WornIds(equipped);
        }

        private int[] _wornIds = new int[0];

        private int[] WornIds(int[] equipped)
        {
            var parts = Parts;
            var ids = new int[parts.Length];
            if (equipped == null) return ids;

            for (int i = 0; i < parts.Length; i++)
                foreach (int e in equipped)
                    if (SnailPet.Data.GameData.AccessoriesDataById.TryGetValue(e, out var row)
                        && row.AccessoriesType == parts[i]) { ids[i] = e; break; }
            return ids;
        }

        private bool FilterOn(SnailPet.Data.AccessoriesType type)
        {
            var parts = Parts;
            for (int i = 0; i < parts.Length && i < Count(_filters); i++)
                if (parts[i] == type) return _filters[i] == null || _filters[i].On;
            return true;
        }

        private static readonly System.Collections.Generic.Dictionary<int, Sprite> _accIcons =
            new System.Collections.Generic.Dictionary<int, Sprite>();

        /// <summary>
        /// 칸에 넣을 악세서리 그림.
        ///
        /// 악세서리 아트는 달팽이 파츠와 같은 1200x1200 공용 캔버스에 「얹힐 자리 그대로」
        /// 그려져 있다. 그대로 32px 칸에 넣으면 대부분이 빈 여백이라 그림이 점만 해진다.
        /// 그래서 알파가 있는 부분만 잘라 새 스프라이트를 만든다. 한 번 만들면 캐시한다.
        /// </summary>
        private static Sprite AccessorySprite(int accessoryId)
        {
            if (_accIcons.TryGetValue(accessoryId, out var cached)) return cached;

            Sprite icon = null;
            if (SnailPet.Data.GameData.AccessoriesDataById.TryGetValue(accessoryId, out var row)
                && !string.IsNullOrEmpty(row.ResourceKey))
            {
                var full = Resources.Load<Sprite>("Snail/Accessories/" + row.ResourceKey);
                if (full != null)
                    icon = SnailPet.Snail.SnailMetrics.TryGetTightRect(full, out var tight)
                         ? Sprite.Create(full.texture, tight, new Vector2(0.5f, 0.5f), full.pixelsPerUnit)
                         : full;
            }

            _accIcons[accessoryId] = icon;
            return icon;
        }

        /// <summary>부위 필터를 켜고 끈다. 꺼진 것은 색을 죽여 표시한다.</summary>
        private void ToggleFilter(int index)
        {
            if (index < 0 || index >= Count(_filters)) return;
            _filters[index].On = !_filters[index].On;
            PaintFilters();
            FilterChanged?.Invoke();
        }

        private void PaintFilters()
        {
            for (int i = 0; i < Count(_filters); i++)
            {
                if (_filters[i] == null) continue;
                bool on = _filters[i].On;
                _filters[i].Box.color = on ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                _filters[i].Label.color = on ? UiTheme.Ink : UiTheme.Slot;
            }
        }

        /// <summary>필터가 바뀌었으니 목록을 다시 달라는 신호.</summary>
        public event Action FilterChanged;

        // ── 달팽이 상세보기 ──
        //
        // 옷장과 같은 모드다. 상세 패널의 유전정보 버튼으로 들어가고 탭을 누르면 나온다.
        // 왼쪽은 파츠마다 설명까지 펼치고, 오른쪽은 초상 아래 한 줄씩 늘어놓는다.

        [Serializable]
        public sealed class GeneRow
        {
            public RectTransform Root;
            public Image Thumb, RarityBadge, RarityIcon;
            public Text Name, Info, Rarity;
        }

        [SerializeField] private RectTransform _geneRoot, _genePanel;
        [SerializeField] private GeneRow[] _geneRows, _geneSlims;
        [SerializeField] private RawImage _genePreview;
        [SerializeField] private Text _geneName, _geneRarityText;
        [SerializeField] private Image _geneRarityBadge, _geneRarityIcon;
        [SerializeField] private Button _geneRenameBtn;

        private bool _inGene;
        public bool InGene => _inGene;

        /// <summary>한 마리가 가질 수 있는 파츠 수. 목업이 넷이고 지금 데이터도 넷이다.</summary>
        private const int GeneRowCount = 4;

        private void BuildGeneList(RectTransform panel)
        {
            _geneRoot = NewRect("Gene", panel);
            Place(_geneRoot, new RectInt(0, 0, UiTheme.PanelW, UiTheme.PanelH));
            _geneRoot.gameObject.SetActive(false);

            _geneRows = new GeneRow[GeneRowCount];
            for (int i = 0; i < _geneRows.Length; i++)
            {
                var r = UiTheme.Gene.Row;
                var at = new RectInt(r.x, r.y + i * UiTheme.Gene.RowStep, r.width, r.height);

                var root = NewRect("Trait" + i, _geneRoot);
                Place(root, at);

                var row = new GeneRow { Root = root };
                Box(root, UiTheme.Gene.RowBar, UiTheme.Slot, UiSprites.Shape.Slot, "Bar");

                // 썸네일은 부위 아이콘이다. 아트에 동그란 배경이 그려져 있어 칸을 깔지 않는다.
                row.Thumb = Icon(root, UiTheme.Gene.RowThumb, null, Color.white, "Thumb");
                row.Thumb.raycastTarget = false;
                BakePartIcon(row.Thumb, i);

                row.Name = Label(root, UiTheme.Gene.RowName, "", 10, UiTheme.Ink);
                row.Name.alignment = TextAnchor.MiddleLeft;

                row.Info = Label(root, UiTheme.Gene.RowInfo, "", 8, UiTheme.Ink);
                row.Info.alignment = TextAnchor.MiddleLeft;
                row.Info.horizontalOverflow = HorizontalWrapMode.Wrap;

                row.RarityBadge = Box(root, UiTheme.Gene.RowRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
                row.Rarity = Label(root, UiTheme.Gene.RowRarity, "", 8, UiTheme.OnBadge);
                Shrink(row.Rarity);
                row.RarityIcon = Icon(root, UiTheme.Gene.RowRarity, null, Color.white, "RarityIcon");
                row.RarityIcon.raycastTarget = false;
                BakeRarity(row.RarityIcon, row.RarityBadge, row.Rarity);

                _geneRows[i] = row;
            }
        }

        private void BuildGenePanel()
        {
            _genePanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _genePanel.gameObject.SetActive(false);

            Box(_genePanel, At.NameField, UiTheme.Slot, UiSprites.Shape.Name, "NameField");
            _geneName = Label(_genePanel, At.NameField, "", 12, UiTheme.Ink);
            _geneRenameBtn = IconButton(_genePanel, At.RenameBtn, "icon_rename", "Rename");

            _geneRarityBadge = Box(_genePanel, At.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _geneRarityText  = Label(_genePanel, At.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_geneRarityText);
            _geneRarityIcon = Icon(_genePanel, At.Rarity, null, Color.white, "RarityIcon");
            _geneRarityIcon.raycastTarget = false;
            BakeRarity(_geneRarityIcon, _geneRarityBadge, _geneRarityText);

            var pv = NewRect("Preview", _genePanel);
            Place(pv, UiTheme.Gene.Preview);
            _genePreview = pv.gameObject.AddComponent<RawImage>();
            _genePreview.raycastTarget = false;

            _geneSlims = new GeneRow[GeneRowCount];
            for (int i = 0; i < _geneSlims.Length; i++)
            {
                var s = UiTheme.Gene.Slim;
                var at = new RectInt(s.x, s.y + i * UiTheme.Gene.SlimStep, s.width, s.height);

                var root = NewRect("Slim" + i, _genePanel);
                Place(root, at);

                var row = new GeneRow { Root = root };
                Box(root, UiTheme.Gene.SlimBar, UiTheme.Slot, UiSprites.Shape.Slot, "Bar");
                row.Thumb = Icon(root, UiTheme.Gene.SlimThumb, null, Color.white, "Thumb");
                row.Thumb.raycastTarget = false;
                BakePartIcon(row.Thumb, i);

                row.RarityBadge = Box(root, UiTheme.Gene.SlimRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
                row.Rarity = Label(root, UiTheme.Gene.SlimRarity, "", 7, UiTheme.OnBadge);
                Shrink(row.Rarity);
                row.RarityIcon = Icon(root, UiTheme.Gene.SlimRarity, null, Color.white, "RarityIcon");
                row.RarityIcon.raycastTarget = false;
                BakeRarity(row.RarityIcon, row.RarityBadge, row.Rarity);

                row.Name = Label(root, UiTheme.Gene.SlimName, "", 9, UiTheme.Ink);
                row.Name.alignment = TextAnchor.MiddleLeft;

                _geneSlims[i] = row;
            }
        }

        /// <summary>상세보기에 들어가거나 나온다.</summary>
        public void OpenGene(bool on)
        {
            _inGene = on;
            if (on)
            {
                _inWardrobe = false;
                if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
                if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);
                SetMaximized(true);
            }
            ApplyGene();
        }

        private void ApplyGene()
        {
            if (_geneRoot == null) return;

            _geneRoot.gameObject.SetActive(_inGene);
            _genePanel.gameObject.SetActive(_inGene);

            if (!_inGene) { SetTab(_tab); return; }

            // 상세보기에 있는 동안에는 목록·그리드·상세가 전부 물러난다
            if (_rowGridRoot != null)   _rowGridRoot.gameObject.SetActive(false);
            if (_foodGridRoot != null)  _foodGridRoot.gameObject.SetActive(false);
            if (_eggGridRoot != null)   _eggGridRoot.gameObject.SetActive(false);
            if (_shopCatRoot != null)   _shopCatRoot.gameObject.SetActive(false);
            if (_shopGridRoot != null)  _shopGridRoot.gameObject.SetActive(false);
            if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
            if (_panel != null)         _panel.gameObject.SetActive(false);
            if (_foodPanel != null)     _foodPanel.gameObject.SetActive(false);
            if (_eggPanel != null)      _eggPanel.gameObject.SetActive(false);
            if (_shopPanel != null)     _shopPanel.gameObject.SetActive(false);
            if (_shopItemPanel != null) _shopItemPanel.gameObject.SetActive(false);
            if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);

            _listTitle.text = SnailPet.Data.Loc.Text(Keys.Traits);
        }

        /// <summary>초상. 옷장과 같은 크기라 같은 것을 쓸 수 있다.</summary>
        public void SetGenePreview(Texture texture)
        {
            if (_genePreview == null) return;
            _genePreview.texture = texture;
            _genePreview.enabled = texture != null;
        }

        public static Vector2Int GenePreviewSize =>
            new Vector2Int(UiTheme.Gene.Preview.width, UiTheme.Gene.Preview.height);

        /// <summary>파츠 목록을 채운다. 양쪽 패널이 같은 목록을 다른 모양으로 보여 준다.</summary>
        public void SetGene(string name, SnailPet.Data.RarityType rarity, int[] partsIds)
        {
            _geneName.text = string.IsNullOrWhiteSpace(name)
                           ? SnailPet.Data.Loc.Text(Keys.NoName) : name;
            ApplyRarity(_geneRarityIcon, _geneRarityBadge, _geneRarityText, rarity);

            int count = partsIds?.Length ?? 0;
            for (int i = 0; i < GeneRowCount; i++)
            {
                bool has = i < count;
                _geneRows[i].Root.gameObject.SetActive(has);
                _geneSlims[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                var row = SnailPet.Data.GameData.PartsDataById.TryGetValue(partsIds[i], out var p) ? p : null;
                string pname = row == null ? "" : SnailPet.Data.Loc.ById(row.NameId);
                string pinfo = row == null ? "" : SnailPet.Data.Loc.ById(row.InfoId);
                var prar = row?.RarityType ?? SnailPet.Data.RarityType.Common;

                _geneRows[i].Name.text = pname;
                _geneRows[i].Info.text = pinfo;
                ApplyRarity(_geneRows[i].RarityIcon, _geneRows[i].RarityBadge, _geneRows[i].Rarity, prar);
                ApplyPartIcon(_geneRows[i].Thumb, row?.PartsType);

                _geneSlims[i].Name.text = pname;
                ApplyRarity(_geneSlims[i].RarityIcon, _geneSlims[i].RarityBadge, _geneSlims[i].Rarity, prar);
                ApplyPartIcon(_geneSlims[i].Thumb, row?.PartsType);
            }
        }

        /// <summary>
        /// 부위 썸네일. 어느 그림을 쓸지는 EnumData 의 IconResourceKey 가 정한다 —
        /// 등급 아이콘과 같은 길이라 부위가 늘어도 코드는 그대로다.
        /// 시트가 비어 있으면(아직 아트가 없는 부위) 아무것도 그리지 않는다.
        /// </summary>
        private static void ApplyPartIcon(Image icon, SnailPet.Data.PartsType? type)
        {
            if (icon == null) return;

            string key = type == null ? null : SnailPet.Data.Enums.IconOf(type.Value);
            var sprite = string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>("Ui/Icon/" + key);

            if (sprite == null && !string.IsNullOrEmpty(key))
                Debug.LogWarning($"[SnailPet] 부위 아이콘을 찾지 못했습니다: Ui/Icon/{key} " +
                                 $"(EnumData 의 PartsType.{type} 행)");

            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        /// <summary>
        /// 프리팹에 썸네일의 채워진 모습을 굽는다. <see cref="BakeRarity"/> 와 같은 이유다 —
        /// 비워 두면 프리팹에서는 썸네일이 아예 안 보여 배치를 손보기 어렵다.
        /// 여기 순서는 굽기용 임시일 뿐이고, 실행하면 그 달팽이가 실제로 가진 부위로 덮인다.
        /// </summary>
        private static void BakePartIcon(Image icon, int index) =>
            ApplyPartIcon(icon, BakeParts[index % BakeParts.Length]);

        private static readonly SnailPet.Data.PartsType[] BakeParts =
        {
            SnailPet.Data.PartsType.Shell,  SnailPet.Data.PartsType.Body,
            SnailPet.Data.PartsType.Feeler, SnailPet.Data.PartsType.Eyes,
        };

        // ── 구매·판매 팝업 ──
        //
        // 목업에서 구매와 판매는 제목과 가격 부호만 다르므로 하나로 만든다.
        // 위젯 안이 아니라 화면 한가운데에 뜨고, 떠 있는 동안 뒤를 가린다.

        [SerializeField] private RectTransform _popup, _popupBlocker;
        [SerializeField] private Text _popupTitle, _popupCount, _popupCost;
        [SerializeField] private Button _popupMinus, _popupPlus, _popupYes, _popupNo, _popupClose;

        private int _popupQty = 1, _popupMax = 1;
        private double _popupUnit;      // 한 개당 값. 판매면 음수로 들어온다.
        private int _popupItemId;

        /// <summary>팝업에서 「네」를 눌렀다. (아이템 Id, 수량).</summary>
        public event Action<int, int> PopupConfirmed;

        /// <summary>
        /// 팝업이 떠 있는가.
        ///
        /// 켜고 끄는 것은 덮개(_popupBlocker)이므로 그쪽을 봐야 한다. 안쪽 판을 보면
        /// activeSelf 가 제 플래그만 보기 때문에 덮개를 꺼도 계속 true 로 남는다 —
        /// 그러면 팝업을 닫은 뒤에도 클릭 통과가 꺼진 채라 펫이 바탕화면을 막는다.
        /// </summary>
        public bool PopupOpen => _popupBlocker != null && _popupBlocker.gameObject.activeSelf;

        private void BuildPopup()
        {
            // 위젯을 덮어 뒤쪽 클릭이 안 먹게 한다. 투명해도 raycastTarget 이면 막힌다.
            //
            // 화면 전체가 아니라 위젯에 붙이는 것이 중요하다. 이 창은 모니터 두 대를 덮는
            // 가상 화면이라(Screen 이 3840x1084 로 나온다) 화면 한가운데가 모니터 경계에
            // 온다. 목업에서도 팝업은 위젯 위에 떠 있다.
            _popupBlocker = NewRect("Popup", _widget);
            _popupBlocker.anchorMin = Vector2.zero; _popupBlocker.anchorMax = Vector2.one;
            _popupBlocker.offsetMin = Vector2.zero; _popupBlocker.offsetMax = Vector2.zero;
            var shade = _popupBlocker.gameObject.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.35f);
            _popupBlocker.gameObject.SetActive(false);

            // 위젯 한가운데
            _popup = NewRect("Panel", _popupBlocker);
            _popup.anchorMin = _popup.anchorMax = _popup.pivot = new Vector2(0.5f, 0.5f);
            _popup.sizeDelta = new Vector2(Pop.W, Pop.H);
            _popup.anchoredPosition = Vector2.zero;

            var bg = Backdrop(_popup.gameObject, UiSprites.Shape.Panel, UiTheme.PanelFill);
            bg.raycastTarget = true;

            // 닫기는 두 모습이 공유한다. 나머지는 묶음별로 갈아 끼운다.
            _popupClose = IconButton(_popup, Pop.Close, "btn_close", "Close", tint: Color.white);

            _buyGroup = Fill(NewRect("BuyGroup", _popup));
            _popupTitle = Label(_buyGroup, Pop.Title, "", 12, UiTheme.Ink);

            // +/- 아트가 아직 없어 글자로 그린다. 아트가 들어오면 IconButton 으로 바꾸면 된다
            // (btn_minus / btn_plus). 스프라이트 없는 Image 를 쓰면 색 사각형이 된다.
            _popupMinus = StepButton(_buyGroup, Pop.Minus, "−", "Minus");
            _popupPlus  = StepButton(_buyGroup, Pop.Plus,  "+", "Plus");

            Box(_buyGroup, Pop.Count, UiTheme.Slot, UiSprites.Shape.LevelBadge, "CountBox");
            _popupCount = Label(_buyGroup, Pop.Count, "1", 11, UiTheme.Ink);

            Box(_buyGroup, Pop.CostPill, UiTheme.Slot, UiSprites.Shape.LevelBadge, "CostPill");
            Icon(_buyGroup, Pop.CostIcon, "icon_coin", Color.white, "CostIcon").raycastTarget = false;
            _popupCost = Label(_buyGroup, Pop.CostText, "", 11, UiTheme.Ink);

            _popupNo  = TextButton(_buyGroup, Pop.No,  Keys.No,  "No");
            _popupYes = TextButton(_buyGroup, Pop.Yes, Keys.Yes, "Yes");

            BuildRenameGroup();
        }

        /// <summary>부모를 가득 채우게 편다.</summary>
        private static RectTransform Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        [SerializeField] private RectTransform _renameGroup, _buyGroup;
        [SerializeField] private InputField _renameField;
        [SerializeField] private Button _renameOk;

        /// <summary>이름을 바꿨다. 빈 문자열이면 「이름 없음」으로 되돌린 것이다.</summary>
        public event Action<string> Renamed;

        /// <summary>
        /// 이름 변경도 같은 판을 쓴다. 목업에서 크기와 닫기 자리가 구매·판매와 같다.
        /// 가운데만 갈아 끼우려고 두 묶음으로 나눠 하나씩 켠다.
        /// </summary>
        private void BuildRenameGroup()
        {
            _renameGroup = Fill(NewRect("RenameGroup", _popup));
            _renameGroup.gameObject.SetActive(false);

            LocLabel(_renameGroup, Pop.RenameTitle, Keys.AskRename, 12, UiTheme.Ink);

            var box = Box(_renameGroup, Pop.RenameField, UiTheme.Slot, UiSprites.Shape.Name, "Field");
            box.raycastTarget = true;

            // 글자는 InputField 의 <b>자식</b>이어야 한다. 형제로 두면 선택은 되는데
            // 글자가 안 들어간다 — 캐럿과 표시가 그 자식을 기준으로 돌기 때문이다.
            var field = (RectTransform)box.transform;
            var text = Label(field, new RectInt(6, 0, Pop.RenameField.width - 12, Pop.RenameField.height),
                             "", 11, UiTheme.Ink);
            text.supportRichText = false;

            _renameField = box.gameObject.AddComponent<InputField>();
            _renameField.textComponent = text;
            _renameField.characterLimit = 12;      // 이름칸이 131px 이라 그 이상은 잘린다
            _renameField.lineType = InputField.LineType.SingleLine;

            var okBox = Box(_renameGroup, Pop.RenameOk, UiTheme.Slot, UiSprites.Shape.Button, "Ok");
            okBox.raycastTarget = true;
            LocLabel(_renameGroup, Pop.RenameOk, Keys.DoRename, 10, UiTheme.Ink);
            _renameOk = okBox.gameObject.AddComponent<Button>();
            _renameOk.targetGraphic = okBox;
        }

        /// <summary>
        /// 이름 변경 팝업을 띄운다.
        /// 글자를 받아야 하므로 <b>여는 동안만</b> 창이 키보드 포커스를 빌린다.
        /// </summary>
        public void ShowRename(string current)
        {
            _buyGroup.gameObject.SetActive(false);
            _renameGroup.gameObject.SetActive(true);
            _popupBlocker.gameObject.SetActive(true);

            _renameField.text = current ?? "";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // 포커스를 먼저 빌린다. 반대로 하면 창이 활성화되면서 EventSystem 의 선택이
            // 풀려 글자가 들어가지 않는다.
            SnailPet.Desktop.TransparentWindow.SetKeyboardFocus(true);
#endif
            StartCoroutine(FocusFieldNextFrame());
        }

        /// <summary>
        /// 창이 활성화된 <b>다음 프레임에</b> 입력칸을 잡는다.
        /// 같은 프레임에 잡으면 활성화 처리가 그 선택을 지운다.
        /// </summary>
        private System.Collections.IEnumerator FocusFieldNextFrame()
        {
            yield return null;
            if (_renameField == null || !_renameGroup.gameObject.activeInHierarchy) yield break;

            EventSystem.current?.SetSelectedGameObject(_renameField.gameObject);
            _renameField.Select();
            _renameField.ActivateInputField();
            _renameField.caretPosition = _renameField.text.Length;
        }

        /// <summary>수량 조절 버튼. 아트가 없어 배경 도형 + 글자로 만든다.</summary>
        private Button StepButton(RectTransform parent, RectInt at, string glyph, string name)
        {
            var box = Box(parent, at, UiTheme.Slot, UiSprites.Shape.Button, name);
            box.raycastTarget = true;
            Label(parent, at, glyph, 14, UiTheme.Ink);

            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box;
            return btn;
        }

        private Button TextButton(RectTransform parent, RectInt at, string token, string name)
        {
            var box = Box(parent, at, UiTheme.Slot, UiSprites.Shape.Button, name);
            box.raycastTarget = true;
            LocLabel(parent, at, token, 10, UiTheme.Ink);

            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box;
            return btn;
        }

        /// <summary>
        /// 팝업을 띄운다.
        /// <paramref name="unitCost"/> 는 한 개당 값이며 <b>판매면 음수</b>로 넣는다 —
        /// 목업의 -5,000 이 그것이다.
        /// <paramref name="max"/> 는 살 수 있는/팔 수 있는 최대 수량.
        /// </summary>
        public void ShowPopup(bool selling, int itemId, string itemName, double unitCost, int max)
        {
            _popupItemId = itemId;
            _popupUnit = unitCost;
            _popupMax = Mathf.Max(1, max);
            _popupQty = 1;

            _buyGroup.gameObject.SetActive(true);
            _renameGroup.gameObject.SetActive(false);
            _popupTitle.text = SnailPet.Data.Loc.Format(selling ? Keys.AskSell : Keys.AskBuy, itemName);
            _popupBlocker.gameObject.SetActive(true);
            PaintPopup();
        }

        public void HidePopup()
        {
            if (_popupBlocker != null) _popupBlocker.gameObject.SetActive(false);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // 이름 입력 때문에 빌린 포커스를 돌려준다. 안 빌렸으면 아무 일도 없다.
            SnailPet.Desktop.TransparentWindow.SetKeyboardFocus(false);
#endif
        }

        private void StepPopup(int delta)
        {
            _popupQty = Mathf.Clamp(_popupQty + delta, 1, _popupMax);
            PaintPopup();
        }

        private void PaintPopup()
        {
            _popupCount.text = _popupQty.ToString();

            // 합계는 반올림이 아니라 버림. 판매값이 2.5 처럼 소수라 두 개를 팔면 5 가 되어야 한다.
            double total = _popupUnit * _popupQty;
            long shown = (long)(total < 0 ? -System.Math.Floor(-total) : System.Math.Floor(total));
            _popupCost.text = shown.ToString("N0");

            // 더 못 올리거나 못 내리면 눌러도 소용없다는 것을 보인다
            if (_popupMinus != null) _popupMinus.interactable = _popupQty > 1;
            if (_popupPlus != null)  _popupPlus.interactable  = _popupQty < _popupMax;
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
        /// <summary>
        /// 프리팹에 등급 아이콘의 「보통」 모습을 굽는다.
        ///
        /// 등급은 런타임에 <see cref="ApplyRarity"/> 가 꽂는데, 그러면 프리팹을 열었을 때는
        /// 아이콘 자리에 뒤에 깔린 어두운 알약만 보인다. 실제 화면과 달라 배치를 손보기
        /// 어렵다. 그래서 굽는 시점에 한 번 채워 둔다 — 실행하면 진짜 등급으로 덮인다.
        /// </summary>
        private static void BakeRarity(Image icon, Image badge, Text text) =>
            ApplyRarity(icon, badge, text, SnailPet.Data.RarityType.Common);

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
        /// <summary>
        /// UI 글꼴을 넣어 두는 곳. 여기에 .ttf / .otf 를 하나 떨어뜨리면 그것이 쓰인다.
        ///
        /// 파일 이름은 상관없다 — 폴더에서 처음 찾은 것을 쓴다. 두 벌 이상 두면
        /// 어느 것이 걸릴지 알 수 없으니 하나만 둘 것.
        /// </summary>
        public const string FontFolder = "Ui/Font";

        /// <summary>
        /// 글꼴을 찾는다. 프로젝트에 넣어 둔 것이 있으면 그것, 없으면 OS 글꼴.
        ///
        /// OS 글꼴은 <b>대비책</b>이다. 맑은 고딕은 어느 PC에나 있어 글자가 네모로
        /// 나오는 일은 없지만, 손그림 UI 와는 결이 맞지 않는다.
        ///
        /// 레거시 Text 는 TTF 를 그때그때 렌더하므로 한글 아틀라스를 미리 구울 필요가 없다.
        /// TMP 를 안 쓰기로 한 것이 여기서 값을 한다.
        /// </summary>
        private static Font LoadKoreanFont()
        {
            var art = Resources.LoadAll<Font>(FontFolder);
            if (art != null && art.Length > 0 && art[0] != null)
            {
                if (art.Length > 1)
                    Debug.LogWarning($"[SnailPet] {FontFolder} 에 글꼴이 {art.Length}벌 있습니다. " +
                                     $"{art[0].name} 를 씁니다 — 하나만 두세요.");
                return art[0];
            }

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

        /// <summary>
        /// 같은 자리에 놓되 피벗만 가운데로 둔다.
        ///
        /// <b>Image.preserveAspect 는 피벗을 기준으로 정렬한다.</b> 목업 좌표를 그대로 쓰려고
        /// 피벗을 왼쪽 위로 잡아 두었더니, 가로로 넓은 그림은 남는 세로 공간이 전부 아래로
        /// 몰려 칸 위쪽에 붙어 보였다 (상추처럼 납작할수록 심하다).
        /// 아이콘은 그림이 칸 한가운데 있어야 하므로 이쪽을 쓴다.
        /// </summary>
        private static void PlaceCentered(RectTransform rt, RectInt r)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(r.width, r.height);
            rt.anchoredPosition = new Vector2(r.x + r.width * 0.5f, -(r.y + r.height * 0.5f));
        }

        private Image Box(RectTransform parent, RectInt r, Color color, UiSprites.Shape shape, string name)
        {
            var rt = NewRect(name, parent);
            Place(rt, r);

            var img = Backdrop(rt.gameObject, shape, color);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// 이미 있는 오브젝트에 도형 배경을 붙인다. 버튼처럼 루트가 먼저 필요한 곳에서 쓴다.
        ///
        /// 아트에는 색이 이미 칠해져 있다. 거기에 테마색을 곱하면 탁해진다 —
        /// 실제로 목록 행과 부화 칸이 이 규칙을 지나치는 바람에 연한 아트가
        /// 거무튀튀하게 나오고 있었다. 배경을 까는 곳은 반드시 여기를 지날 것.
        /// (게이지 채우기만 예외로 물들인다. 포만/행복 두 색이 필요하다.)
        /// </summary>
        private static Image Backdrop(GameObject go, UiSprites.Shape shape, Color color)
        {
            var img = go.AddComponent<Image>();
            img.sprite = UiSprites.Of(shape);
            img.type = Image.Type.Sliced;
            img.color = UiSprites.IsArt(shape) ? Color.white : color;
            go.AddComponent<UiShapeRef>().Shape = shape;
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

        /// <summary>
        /// 언어 키에서 오는 붙박이 글자. 어디서 왔는지 <see cref="UiTextRef"/> 로 남겨
        /// 프리팹에서 살아날 때 시트를 다시 읽게 한다 — 안 그러면 구울 때의 글자가 굳는다.
        /// 값이 바뀌는 글자(이름·수량 등)는 그냥 <see cref="Label"/> 로 만들고 코드가 채운다.
        /// </summary>
        private Text LocLabel(RectTransform parent, RectInt r, string token, int size, Color color)
        {
            var t = Label(parent, r, SnailPet.Data.Loc.Text(token), size, color);
            t.gameObject.AddComponent<UiTextRef>().Token = token;
            return t;
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
            PlaceCentered(rt, r);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.preserveAspect = true;

            // key 가 null 이면 나중에 채울 자리다. 경고하지 않는다.
            if (key != null)
            {
                img.sprite = Resources.Load<Sprite>("Ui/Icon/" + key);
                if (img.sprite == null)
                {
                    // 스프라이트 없는 Image 는 색으로 꽉 찬 사각형이 된다. 아트가 빠졌을 때
                    // 화면에 덩어리가 나오느니 아무것도 안 나오는 편이 낫다.
                    img.enabled = false;
                    Debug.LogWarning("[SnailPet] UI 아이콘을 찾지 못했습니다: Ui/Icon/" + key);
                }
            }
            return img;
        }

        /// <param name="tint">아이콘 색. 실루엣 아이콘은 기본값(먹색), 색이 들어 있는 아트는 흰색.</param>
        /// <summary>
        /// 아이콘 버튼을 만들기만 한다. 할 일은 <see cref="Rewire"/> 가 붙인다 —
        /// 여기서 붙이면 프리팹으로 구울 때 사라진다.
        /// </summary>
        private Button IconButton(RectTransform parent, RectInt r, string key, string name,
                                  Color? background = null, Color? tint = null)
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

            // 아이콘 아트에는 배경과 색이 이미 들어 있다. 물들이면 통째로 그 색이 되므로
            // 흰색으로 넘겨 원본을 그대로 낸다. 예전 단색 실루엣 시절에는 Ink 로 칠했었다.
            int pad = background.HasValue ? 4 : 1;
            Icon(rt, new RectInt(pad, pad, r.width - pad * 2, r.height - pad * 2),
                 key, tint ?? Color.white, "Glyph").raycastTarget = false;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }
    }
}
