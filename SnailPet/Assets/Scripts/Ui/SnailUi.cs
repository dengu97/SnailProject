using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using At = SnailPet.Ui.UiTheme.At;
using Max = SnailPet.Ui.UiTheme.Max;
using Fd = SnailPet.Ui.UiTheme.Food;
using Sh = SnailPet.Ui.UiTheme.Shop;
using Pop = SnailPet.Ui.UiTheme.Popup;
using Set = SnailPet.Ui.UiTheme.Setting;
using Options = SnailPet.Snail.PlayerOptions;
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
            public const string NoFood    = "[안내_보유음식]";
            public const string NoAccessory = "[안내_보유악세]";
            public const string HatchDone = "[부화완료]";

            public const string AskBuy  = "[구매문구]";   // "{0}을(를) 구매할까요?"
            public const string AskSell = "[판매문구]";
            public const string Yes       = "[동의]";
            public const string AskRename = "[이름변경]";   // "이름을 변경합니다."
            public const string DoRename  = "[변경]";
            public const string No      = "[거부]";

            public const string Hatched = "[부화문구]";   // "알이 부화했습니다!"
            public const string Confirm = "[확인]";

            // 도감 완성·보상 수령 팝업
            public const string GuideDone = "[도감완성]";
            public const string RewardGot = "[보상수령]";

            // 설정 화면
            public const string SnailSetting = "[달팽이설정]";
            public const string Setting      = "[설정]";
            public const string Korean       = "[한글]";
            public const string Update       = "[업데이트]";
            public const string UiScale      = "[UI크기]";      // "UI크기(x{0})"
            public const string AlwaysMax    = "[UI최대화]";
            public const string EggSection   = "[알관련]";
            public const string NoEggs       = "[알생성금지]";
            public const string BubbleSection = "[말풍선알림]";
            public const string HungryBubble = "[배고픔알림]";
            public const string CareBubble   = "[관심알림]";
            public const string CoinBubble   = "[코인알림]";
            public const string Quit         = "[종료]";

            public const string Wardrobe = "[옷장]";

            // 멀티플레이어
            public const string Multiplayer = "[멀티플레이어]";
            public const string FriendList  = "[친구목록]";
            public const string LobbyList   = "[로비목록]";
            public const string Room        = "[방]";
            public const string MakeRoom    = "[방만들기]";
            public const string JoinById    = "[로비ID로진입]";
            public const string JoinRandom  = "[랜덤방으로진입]";
            public const string LobbyIdAsk  = "[안내_로비ID]";

            /// <summary>빈 즐겨찾기 칸을 눌렀을 때의 안내.</summary>
            public const string NoticeFavorite = "[안내_즐겨찾기]";

            /// <summary>즐겨찾기가 꽉 찼는데 또 켜려고 했을 때.</summary>
            public const string NoticeFavoriteFull = "[안내_즐겨찾기2]";

            /// <summary>개수가 0 인 즐겨찾기 칸에 뜨는 「삭제」.</summary>
            public const string Delete = "[삭제]";

            /// <summary>코인이 모자라 못 살 때.</summary>
            public const string NoticeNoCoins = "[안내_재화부족]";

            /// <summary>구매가 끝났을 때.</summary>
            public const string NoticePurchased = "[안내_구매완료]";

            /// <summary>도감 목록의 제목. 시트에 넣기 전에는 화면에 토큰이 그대로 나온다.</summary>
            public const string Guide = "[달팽이도감]";
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

            // 프리팹에 없던 화면은 살아날 때 지어 붙인다.
            //
            // 프리팹이 원본이므로 원래는 다시 구워야 새 화면이 나온다. 그런데 다시 구우면
            // 손으로 맞춘 배치(배율·소수점 좌표·글자색)가 전부 사라진다 — 그래서 굽는 대신
            // 없는 것만 런타임에 짓는다. 나중에 다시 구우면 이 조건이 저절로 거짓이 된다.
            if (_hatchGroup == null) BuildHatchGroup();

            // 도감 완성·보상 묶음도 팝업 판의 자식이라 프리팹에는 없다.
            if (_doneGroup == null) BuildGuideDoneGroup();
            if (_rewardGroup == null) BuildRewardGroup();
            if (_guestGroup == null) BuildGuestGroup();

            // 상품 미리보기 뒤의 달팽이 실루엣. 상점 패널은 프리팹에 있으므로 여기서 붙인다.
            if (_pickShape == null) _pickShape = ShapeBehind(_pickIcon, "PickShape");
            if (_shopShape == null) _shopShape = ShapeBehind(_shopIcon, "PreviewShape");

            // 상점 악세서리 부위 필터도 프리팹에는 없다. 상품 그리드와 같은 판에 붙인다.
            if (_shopFilterRoot == null && _shopGridRoot != null)
                BuildShopFilters((RectTransform)_shopGridRoot.parent);

            // 「보유중인 장식품이 없습니다」. 격자가 아니라 옷장 루트에 붙여 스크롤과 무관하게 둔다.
            if (_wardrobeEmpty == null && _wardrobeRoot != null)
                _wardrobeEmpty = LocLabel(_wardrobeRoot, Max.Empty, Keys.NoAccessory, 10, UiTheme.Slot);

            // 「보유중인 음식이 없습니다」도 프리팹에는 없다. 목록 쪽과 상세 쪽 둘 다 붙인다.
            if (_foodEmpty == null && _foodGridRoot != null)
                _foodEmpty = LocLabel(_foodGridRoot, InGrid(Max.Empty), Keys.NoFood, 10, UiTheme.Slot);
            if (_foodEmptyDetail == null && _foodPanel != null)
                _foodEmptyDetail = LocLabel(_foodPanel, Max.Empty, Keys.NoFood, 10, UiTheme.Slot);

            // 최소화 창과 안내 문구도 프리팹에는 없다.
            if (_miniRoot == null) BuildMini();

            // 파티 탭은 프리팹에 없다 (탭이 넷일 때 구웠다). 모자란 것만 지어 붙인다.
            FitTabs();

            // 이름 변경 팝업의 제목·확인 글자. 프리팹에는 필드로 안 잡혀 있어 이름으로 찾는다
            // (로비ID 로 열 때 글자를 갈아 끼워야 한다).
            if (_renameTitle == null || _renameOkText == null) FindRenameTexts();
            if (_notice == null) BuildNotice();

            // 끌어 옮기기. 위젯 루트에 하나만 붙으면 자식 전부에 걸린다 (UiDragMove 참고).
            if (_widget != null && _widget.GetComponent<UiDragMove>() == null)
                _widget.gameObject.AddComponent<UiDragMove>();

            // 최소화에서 되돌아올 때 쓸 크기. 프리팹에서 손댔을 수 있으니 코드 값이 아니라
            // 지금 값을 기억한다.
            if (_widget != null) _widgetSize = _widget.sizeDelta;

            // 설정 화면은 상세보기와 같은 목록 패널에 붙는다. 그 패널은 필드가 아니라
            // 지을 때의 지역 변수라, 이미 거기 붙어 있는 상세보기를 통해 찾는다.
            if (_settingsRoot == null && _geneRoot != null)
                BuildSettingsList((RectTransform)_geneRoot.parent);
            if (_settingsPanel == null) BuildSettingsPanel();

            // 도감도 프리팹에 없다. 상세보기와 같은 목록 패널에 붙인다.
            if (_guideRoot == null && _geneRoot != null) BuildGuideList((RectTransform)_geneRoot.parent);
            if (_guidePanel == null) BuildGuidePanel();

            // 멀티플레이어도 마찬가지다. 진입 버튼은 설정 기어 오른쪽에 붙인다.
            if (_multiRoot == null && _geneRoot != null) BuildMultiList((RectTransform)_geneRoot.parent);
            if (_multiPanel == null) BuildMultiPanel();

            // 「부화시킬 알이 없습니다」는 프리팹에 부화기 패널에 구워져 있다. 비는 쪽은
            // 왼쪽 목록이므로 그리로 옮긴다. 다시 구우면 처음부터 그 자리에 지어진다.
            if (_eggEmpty != null && _eggGridRoot != null && _eggEmpty.transform.parent != _eggGridRoot)
            {
                _eggEmpty.transform.SetParent(_eggGridRoot, false);
                Place((RectTransform)_eggEmpty.transform, InGrid(UiTheme.Egg.Empty));
            }

            // 목록 행도 프리팹에 구워질 때는 썸네일 그림 자리가 없었고, 이름은 가운데 정렬로
            // 굳어 있다. 둘 다 여기서 맞춘다.
            for (int i = 0; i < Count(_rows); i++)
            {
                if (_rows[i] == null || _rows[i].Root == null) continue;

                if (_rows[i].Face == null) _rows[i].Face = FaceView(_rows[i].Root, Max.RowThumb);
                if (_rows[i].Name != null) _rows[i].Name.alignment = TextAnchor.MiddleLeft;

                // 선택 테두리도 프리팹에는 없다. 줄 위에 얹혀야 하므로 맨 뒤에 붙이고,
                // 교체 버튼은 그보다 더 위로 올린다.
                if (_rows[i].Frame == null) _rows[i].Frame = RowFrame(_rows[i].Root);
                _rows[i].Frame.transform.SetAsLastSibling();
                if (_rows[i].Swap != null) _rows[i].Swap.transform.SetAsLastSibling();

                // 줄을 누르는 버튼도 프리팹에는 없다. 배경이 이미 있으니 그걸 표적으로 쓴다.
                if (_rows[i].Button == null)
                {
                    _rows[i].Button = _rows[i].Root.gameObject.GetComponent<Button>()
                                   ?? _rows[i].Root.gameObject.AddComponent<Button>();
                    _rows[i].Button.targetGraphic = _rows[i].Root.GetComponent<Image>();
                }
            }

            // 상세보기 파츠 줄의 글자 자리는 프리팹에 예전 값으로 구워져 있다. 지금 값으로 다시 놓는다.
            // (손으로 옮긴 배치를 덮지 않도록, 자리를 옮긴 것만 이렇게 짚어서 되놓는다.)
            for (int i = 0; i < Count(_geneRows); i++)
            {
                if (_geneRows[i] == null) continue;
                if (_geneRows[i].Name != null) Place((RectTransform)_geneRows[i].Name.transform, UiTheme.Gene.RowName);
                if (_geneRows[i].Info != null) Place((RectTransform)_geneRows[i].Info.transform, UiTheme.Gene.RowInfo);
            }

            // 오른쪽 한 줄짜리 목록도 프리팹에 구워져 있어 UiTheme 를 고쳐도 안 움직인다.
            // 도감 파츠 목록과 같은 높이로 맞추려고 올린 값이라 여기서 다시 놓는다.
            // (구울 때의 값과 같은지 확인하고 옮긴 것이다 — 손으로 만진 자리가 아니다)
            for (int i = 0; i < Count(_geneSlims); i++)
            {
                if (_geneSlims[i]?.Root == null) continue;

                var s = UiTheme.Gene.Slim;
                Place(_geneSlims[i].Root, new RectInt(s.x, s.y + i * UiTheme.Gene.SlimStep, s.width, s.height));
            }

            // 프리팹에는 덮개가 반투명 검정으로 구워져 있다. 이제 어둡게 하는 일은
            // 아트의 색이 맡으므로 덮개는 투명하게 두고 클릭만 막는다.
            if (_popupBlocker != null)
            {
                var shade = _popupBlocker.GetComponent<Image>();
                if (shade != null) shade.color = new Color(0f, 0f, 0f, 0f);
            }

            // 프리팹에는 포만·행복 아이콘이 값 알약보다 먼저 구워져 있어 알약에 가린다.
            // 짓는 순서는 고쳤지만 프리팹은 그대로이므로 살아날 때 앞으로 올린다.
            BringToFront(_foodPanel, "FullIcon", "HappyIcon");
            BringToFront(_shopStats, "FullIcon", "HappyIcon");

            // 하단 액션 첫 칸은 도감으로 바뀌었다. 프리팹에는 예전 돋보기가 구워져 있다.
            if (Count(_actionBtns) > 0) SetGlyph(_actionBtns[0], "icon_book");

            // 음식 상세의 판매도 테두리 없는 아트로 바뀌었다. 프리팹에는 icon_sell 이 구워져 있다.
            SetGlyph(_foodSellBtn, "btn_sell");

            // 상점 뒤로가기도 아트가 들어왔다. 프리팹에는 버튼 도형 + 「←」 글자로 굳어 있다.
            FitBackButton();

            // 칸의 수량 배지는 프리팹에 없다. 칸마다 붙이고 글자도 그 위로 옮긴다.
            FitCountBadges(_foodSlots);
            FitCountBadges(_eggSlots);
            FitCountBadges(_shopSlots);
            FitCountBadges(_wardrobeSlots);
            FitCountBadges(_wornSlots);

            // 오늘의 할인 줄에서 원가와 취소선을 코인 아이콘에서 조금 떼어 놓았다.
            // 프리팹에는 예전 자리로 굳어 있다. (할인가는 띄울 때마다 다시 놓으므로 그대로 둔다)
            if (_pickWas != null) Place((RectTransform)_pickWas.transform, Sh.PickWas);
            if (_pickStrike != null) Place((RectTransform)_pickStrike.transform, Sh.PickStrike);

            // 이름칸도 옷장·상세보기와 같은 모양으로 맞춘다. 프리팹에는 예전 자리·크기로 굳어 있다.
            if (_nameText != null)
            {
                Place((RectTransform)_nameText.transform, At.NameField);
                _nameText.fontSize = 12;
            }

            // 옷장·상세보기의 달팽이는 메인 상세의 초상과 같은 자리에 같은 크기로 나와야 한다.
            // 좌표뿐 아니라 배율까지 복사하는 이유: 메인 초상은 프리팹에서 손으로 줄여 둔
            // 상태라(0.7배) 좌표만 맞추면 크기가 어긋난다. 나중에 메인을 다시 조정해도 따라온다.
            MatchToPortrait(_wardrobePreview);
            MatchToPortrait(_genePreview);
            MatchToPortrait(_guideImage);   // 실루엣은 목업 크기를 그대로 둔다

            AttachPressEffects();
            PaintButtonLabels();

            // 부화기 타이머도 버튼 글자와 같은 색이다. 프리팹에는 먹색으로 굳어 있다.
            for (int i = 0; i < Count(_hatchSlots); i++)
                if (_hatchSlots[i]?.Timer != null) _hatchSlots[i].Timer.color = UiTheme.OnButton;

            Rewire();
            EnsureEventSystem();

            // 닫기와 최대화는 항상 맨 앞에 둔다. UGUI 는 형제 순서대로 그리는데, 탭마다 뜨는
            // 패널들이 BuildOutside 보다 뒤에 지어져 있어 그대로 두면 달팽이 탭 말고는 이 둘이
            // 패널에 가려진다. 그림만의 문제가 아니라 패널이 클릭까지 먹는다.
            // (프리팹의 순서를 고치려면 다시 구워야 하므로 살아날 때마다 여기서 올린다.)
            if (_closeBtn != null)    _closeBtn.transform.SetAsLastSibling();
            if (_maximizeBtn != null) _maximizeBtn.transform.SetAsLastSibling();

            // 뒤로가기도 마찬가지다. 상점 패널보다 먼저 지어져 있어 옷장·상세보기 패널이
            // 그 위를 덮었고, 그래서 눌리지 않았다.
            if (_shopBack != null) _shopBack.SetAsLastSibling();

            // 좌우 패널을 통째로 쓰는 화면들은 프리팹에 <b>켜진 채로</b> 저장돼 있을 수 있다.
            // (편집하려고 켜 보고 그대로 저장하면 그렇게 된다. 실제로 유전정보가 그 상태였고,
            //  목록 행 사이 틈으로 그 줄들이 비쳐 보였다.)
            // 시작은 언제나 달팽이 목록이므로 여기서 확실히 내린다.
            _inGene = _inWardrobe = _inSettings = _inGuide = false;
            if (_geneRoot != null)      _geneRoot.gameObject.SetActive(false);
            if (_genePanel != null)     _genePanel.gameObject.SetActive(false);
            if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
            if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);
            if (_settingsRoot != null)  _settingsRoot.gameObject.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
            if (_guideRoot != null)     _guideRoot.gameObject.SetActive(false);
            if (_guidePanel != null)    _guidePanel.gameObject.SetActive(false);

            // 프리팹에는 편집용으로 펼친 채 구워져 있다. 실행은 접힌 상태로 시작한다.
            SetMaximized(false);
            HidePopup();
            SetTab(_tab);
        }

        /// <summary>
        /// 버튼 글자를 밝은 색으로 맞춘다. 프리팹에는 예전 먹색으로 굳어 있다.
        /// (굵게도 해 봤지만 이 글꼴·크기에서는 뭉쳐 보여 되돌렸다. 그래서 굵기를 보통으로
        ///  분명히 되돌려 둔다 — 프리팹에 굵게 저장돼 있어도 여기서 풀린다.)
        ///
        /// 가려내는 기준은 <b>배경이 버튼 아트인가</b>다. 목록 행이나 설정 행도 Button 이지만
        /// 밝은 칸 도형을 쓰므로, 거기까지 밝은 글자로 바꾸면 배경에 묻혀 안 보인다.
        ///
        /// 글자는 버튼의 자식일 수도(상점 구매) 형제일 수도(팝업 버튼) 있어서 둘 다 훑는다.
        /// </summary>
        private void PaintButtonLabels()
        {
            var buttonArt = UiSprites.Of(UiSprites.Shape.Button);
            if (buttonArt == null) return;

            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (!(button.targetGraphic is Image bg) || bg.sprite != buttonArt) continue;

                var rt = (RectTransform)button.transform;

                foreach (var text in button.GetComponentsInChildren<Text>(true)) Paint(text);

                if (rt.parent == null) continue;
                foreach (Transform sibling in rt.parent)
                {
                    if (sibling == rt) continue;
                    var text = sibling.GetComponent<Text>();
                    if (text != null && sibling is RectTransform srt && CenterInside(rt, srt)) Paint(text);
                }
            }

            void Paint(Text t)
            {
                t.color = UiTheme.OnButton;
                t.fontStyle = FontStyle.Normal;
            }
        }

        /// <summary>아이콘 버튼의 그림을 갈아 끼운다. 그림은 「Glyph」라는 자식이다.</summary>
        private static void SetGlyph(Button button, string key)
        {
            var glyph = button != null ? button.transform.Find("Glyph") : null;
            var image = glyph != null ? glyph.GetComponent<Image>() : null;
            if (image == null) return;

            var sprite = Resources.Load<Sprite>("Ui/Icon/" + key);
            if (sprite == null)
            {
                Debug.LogWarning("[SnailPet] 버튼 아이콘을 찾지 못했습니다: Ui/Icon/" + key);
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.enabled = true;
        }

        /// <summary>
        /// 상점 뒤로가기를 아트로 바꾼다.
        ///
        /// 프리팹에는 아트가 없던 시절의 모습(버튼 도형 + 「←」 글자)이 구워져 있다.
        /// 도형 자리에 그림을 넣고, 그 위에 겹쳐 있던 화살표 글자를 끈다.
        /// </summary>
        private void FitBackButton()
        {
            if (_shopBack == null) return;

            var image = _shopBack.GetComponent<Image>();
            var sprite = Resources.Load<Sprite>("Ui/Icon/btn_back");
            if (image == null || sprite == null) return;

            // 이미 그림이면(코드로 지은 경우) 손댈 것이 없다
            if (image.sprite == sprite) return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;

            if (_shopBack.parent == null) return;
            foreach (Transform sibling in _shopBack.parent)
            {
                if (sibling == _shopBack) continue;

                var text = sibling.GetComponent<Text>();
                if (text != null && sibling is RectTransform srt && CenterInside(_shopBack, srt))
                    text.enabled = false;
            }
        }

        /// <summary>
        /// 프리팹에 구워진 칸에 수량 배지를 붙이고, 글자를 그 위에 흰색으로 다시 놓는다.
        ///
        /// 배지와 글자는 <b>선택 테두리보다 뒤</b>에 있어야 위에 그려진다. 고른 칸에서
        /// 수량이 테두리에 묻히던 것이 이 문제였다.
        /// </summary>
        private void FitCountBadges(GridSlot[] slots)
        {
            for (int i = 0; i < Count(slots); i++)
            {
                var slot = slots[i];
                if (slot?.Root == null || slot.Count == null) continue;

                if (slot.CountBg == null) slot.CountBg = CountBadge(slot.Root, Max.FoodCountBadge);
                else
                {
                    // 프리팹에는 동그란 그림(icon_circle)이 구워져 있다. 늘어나는 도형으로 갈아 끼운다.
                    slot.CountBg.sprite = UiSprites.Of(UiSprites.Shape.SlotCount);
                    slot.CountBg.type = Image.Type.Sliced;
                    slot.CountBg.preserveAspect = false;
                    slot.CountBg.color = UiSprites.IsArt(UiSprites.Shape.SlotCount) ? Color.white : UiTheme.BadgeDark;
                    PlaceRight((RectTransform)slot.CountBg.transform, Max.FoodCountBadge);
                }
                slot.CountBg.raycastTarget = false;
                slot.CountBg.transform.SetAsLastSibling();

                PlaceRight((RectTransform)slot.Count.transform, Max.FoodCountBadge);
                slot.Count.alignment = TextAnchor.MiddleCenter;
                slot.Count.color = UiTheme.OnBadge;
                slot.Count.transform.SetAsLastSibling();

                SetSlotCount(slot, slot.Count.text);
            }
        }

        /// <summary>
        /// 메인 상세의 초상과 같은 자리·크기·배율로 맞춘다.
        ///
        /// 옷장·상세보기 패널은 메인 상세 패널과 원점·크기가 같으므로 값을 그대로 옮기면 된다.
        /// </summary>
        private void MatchToPortrait(Graphic view)
        {
            if (view == null || _portrait == null) return;

            var src = (RectTransform)_portrait.transform;
            var dst = (RectTransform)view.transform;

            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.sizeDelta = src.sizeDelta;
            dst.anchoredPosition = src.anchoredPosition;
            dst.localScale = src.localScale;
        }

        /// <summary>안쪽 것의 한가운데가 바깥 칸 안에 있는가.</summary>
        private static bool CenterInside(RectTransform outer, RectTransform inner)
        {
            var o = new Vector3[4]; outer.GetWorldCorners(o);
            var i = new Vector3[4]; inner.GetWorldCorners(i);

            var c = (i[0] + i[2]) * 0.5f;
            return c.x >= Mathf.Min(o[0].x, o[2].x) && c.x <= Mathf.Max(o[0].x, o[2].x)
                && c.y >= Mathf.Min(o[0].y, o[2].y) && c.y <= Mathf.Max(o[0].y, o[2].y);
        }

        /// <summary>이름으로 찾아 형제들 맨 앞으로 올린다. UGUI 는 형제 순서대로 그린다.</summary>
        private static void BringToFront(RectTransform parent, params string[] names)
        {
            if (parent == null) return;

            foreach (var name in names)
            {
                var found = parent.Find(name);
                if (found != null) found.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 눌린 반응을 버튼마다 붙인다.
        ///
        /// 컴포넌트는 프리팹에 이미 구워져 있을 수도, 없을 수도 있으므로 없는 것만 붙인다.
        /// 화면을 고르는 것들(탭 · 상점 카테고리 · 설정)은 뺀다 — 그 자리는 「눌렸다」보다
        /// 「지금 여기」가 중요해서, 선택 표시와 겹치면 오히려 산만해진다.
        /// </summary>
        private void AttachPressEffects()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button == null || IsMenuButton(button)) continue;
                if (button.GetComponent<UiPressEffect>() == null)
                    button.gameObject.AddComponent<UiPressEffect>();
            }
        }

        private bool IsMenuButton(Button button)
        {
            // 설정도 화면을 고르는 버튼이라 탭과 같은 부류로 본다.
            if (button == _settingsBtn) return true;

            for (int i = 0; i < Count(_tabBtns); i++)
                if (_tabBtns[i] == button) return true;

            for (int i = 0; i < Count(_shopCats); i++)
                if (_shopCats[i] != null && _shopCats[i].Button == button) return true;

            return false;
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
            Hook(_closeBtn,    () =>
            {
                // 달팽이 화면에서는 X 가 아니라 최소화 버튼이다 (ShrinksOnClose).
                // 나가는 것이므로 여기서도 골라 둔 달팽이는 푼다.
                if (ShrinksOnClose)
                {
                    ResetPick();
                    SetMinimized(true);
                    Close?.Invoke();
                    return;
                }

                // X 는 어디서 눌러도 달팽이 화면으로 돌아간 다음 접는다. 음식·알·상점 탭이나
                // 옷장·설정에 있던 그대로 접으면, 다시 펼쳤을 때 엉뚱한 화면이 열려 어색하다.
                //
                // 되돌아가는 것이 아니라 나가는 것이므로 골라 둔 달팽이도 푼다.
                // (뒤로가기는 반대로 고른 것을 그대로 두고 보던 화면으로 돌아간다)
                SetTab(0);
                ResetPick();

                // 「항상 최대화」가 막는 것은 접는 동작 하나다. 그래서 설정에서 되돌아가는 것은
                // 막지 않고, 접기만 건너뛴다.
                if (_options.AlwaysMax) return;

                SetMaximized(false);
                Close?.Invoke();
            });
            Hook(_maximizeBtn, () => { SetMaximized(true);  Maximize?.Invoke(); });

            // 최소화 창의 최대화는 처음 모습(접힌 달팽이 정보)으로 되돌린다. 목록까지 펼치지는 않는다.
            Hook(_miniMaxBtn,  () => SetMinimized(false));

            for (int i = 0; i < Count(_miniSlots); i++) { int k = i; Hook(_miniSlots[i]?.Button, () => PressFavorite(k)); }

            // 하단 액션 4개. 순서는 BuildActions 의 이름 배열과 같다.
            var actions = new UnityEngine.Events.UnityAction[]
            {
                () => Detail?.Invoke(), () => Wardrobe?.Invoke(),
                () => Gene?.Invoke(),   () => Sell?.Invoke(),
            };
            for (int i = 0; i < Count(_actionBtns) && i < actions.Length; i++) Hook(_actionBtns[i], actions[i]);

            for (int i = 0; i < Count(_tabBtns); i++) { int k = i; Hook(_tabBtns[i], () => SetTab(k)); }
            for (int i = 0; i < Count(_rows); i++)
            {
                int k = i;
                Hook(_rows[i]?.Swap, () => SwapTo?.Invoke(k));
                Hook(_rows[i]?.Button, () => PickRow(k));      // 줄을 누르면 정보만 본다
            }

            for (int i = 0; i < Count(_guideRows); i++) { int k = i; Hook(_guideRows[i]?.Button, () => PickGuide(k)); }
            Hook(_guideToggle, ToggleGuideDetail);

            Hook(_friendTab,      () => SetMultiTab(false));
            Hook(_lobbyTab,       () => SetMultiTab(true));
            Hook(_makeRoomBtn,    () => MakeRoom?.Invoke());
            Hook(_joinIdBtn,      () => JoinById?.Invoke());
            Hook(_joinRandomBtn,  () => JoinRandom?.Invoke());
            Hook(_roomOutBtn,     () => LeaveRoom?.Invoke());
            for (int i = 0; i < Count(_multiRows); i++)
            {
                int k = i;
                Hook(_multiRows[i]?.Action, () => { if (_onLobbyTab) EnterLobby?.Invoke(k); else InviteFriend?.Invoke(k); });
            }
            for (int i = 0; i < Count(_members); i++) { int k = i; Hook(_members[i]?.Zoom, () => ViewMember?.Invoke(k)); }

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
            // 음식 상세의 「구매」는 그 음식이 골라진 채로 상점을 연다. 상점에서 다시 찾게 하지 않는다.
            Hook(_foodBuyBtn,  () =>
            {
                OpenShop(_selectedFood >= 0 && _selectedFood < _foodIds.Length ? _foodIds[_selectedFood] : 0);
                GoShop?.Invoke();
            });
            Hook(_foodSellBtn, () =>
            {
                if (_selectedFood >= 0 && _selectedFood < _foodIds.Length) SellFood?.Invoke(_foodIds[_selectedFood]);
            });
            Hook(_eggShopBtn,  () => { OpenShop(); GoShop?.Invoke(); });

            Hook(_pickBuyBtn, () => { if (_pickId > 0) BuyProduct?.Invoke(_pickId); });
            Hook(_shopBuyBtn, () =>
            {
                if (_selectedShop >= 0 && _selectedShop < _shopIds.Length)
                    BuyProduct?.Invoke(_shopIds[_selectedShop]);
            });
            Hook(_backBtn, GoBack);

            // 팝업
            Hook(_popupMinus, () => StepPopup(-1));
            Hook(_popupPlus,  () => StepPopup(+1));
            Hook(_popupNo,    HidePopup);
            Hook(_popupClose, HidePopup);

            // 부화 팝업의 「확인」. X 와 하는 일이 같다 — 닫고 목록으로 돌아간다.
            Hook(_hatchOk,    HidePopup);

            // 도감 완성 → 확인 → 보상 수령 → 확인. 보상은 받는 쪽이 준다.
            Hook(_doneOk,   () => GuideDoneConfirmed?.Invoke());
            Hook(_rewardOk, () => { HidePopup(); RewardClosed?.Invoke(); });

            // 설정. 언어는 지금 한글뿐이라 일부러 아무 일도 하지 않는다.
            Hook(_noEggsBtn,    () => { _options.NoEggs       = !_options.NoEggs;       ChangeOptions(); });
            Hook(_hungryBtn,    () => { _options.HungryBubble = !_options.HungryBubble; ChangeOptions(); });
            Hook(_careBtn,      () => { _options.CareBubble   = !_options.CareBubble;   ChangeOptions(); });
            Hook(_coinBtn,      () => { _options.CoinBubble   = !_options.CoinBubble;   ChangeOptions(); });
            Hook(_alwaysMaxBtn, () => { _options.AlwaysMax    = !_options.AlwaysMax;    ChangeOptions(); });
            Hook(_scaleBtn,     () => { _options.ScaleStep    = (_options.ScaleStep + 1) % 3; ChangeOptions(); });
            Hook(_updateBtn,    () => UpdatePressed?.Invoke());
            Hook(_quitBtn,      () => QuitPressed?.Invoke());
            Hook(_renameOk, () =>
            {
                string text = _renameField != null ? _renameField.text : "";
                bool lobby = _renameForLobby;
                HidePopup();

                // 같은 팝업을 이름 변경과 로비ID 입력이 나눠 쓴다. 무엇으로 열었는지로 가른다.
                if (lobby) LobbyIdEntered?.Invoke(text);
                else       Renamed?.Invoke(text);
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
            for (int i = 0; i < Count(_shopFilters); i++) { int k = i; Hook(_shopFilters[i]?.Button, () => ToggleShopFilter(k)); }
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
            // UI 크기 설정은 이 스케일러의 scaleFactor 로 건다 (ApplyUiOptions 참고).
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler = scaler;

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
            BuildMini();
            BuildList();
            BuildFoodDetail();
            BuildEggPanel();
            BuildShopPanels();
            BuildWardrobePanel();
            BuildGenePanel();
            BuildGuidePanel();
            BuildSettingsPanel();
            BuildPopup();
            BuildNotice();      // 팝업 위에도 떠야 하므로 맨 나중에 짓는다

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

            // 이름칸은 옷장·상세보기와 같은 모양으로 둔다. 예전에는 이름 수정 버튼을 피해
            // 오른쪽으로 밀고 글자도 한 단계 컸는데, 화면을 오갈 때 이름이 움직여 보였다.
            _nameText = Label(_panel, At.NameField, "달팽이 이름", 12, UiTheme.Ink);

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
            // 첫 칸은 도감 자리다. 예전 돋보기(icon_detail)는 하는 일이 없어 그림만 갈았다.
            var keys  = new[] { "icon_book", "icon_wardrobe", "icon_gene", "icon_sell" };
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

        [Serializable]
        public sealed class MiniSlot
        {
            public RectTransform Root;
            public Image Food;
            public Text Plus;
            public Image CountBg;
            public Text Count;
            public Image AskBg;
            public Text Ask;
            public Button Button;
        }

        [SerializeField] private RectTransform _miniRoot;
        [SerializeField] private Button _miniMaxBtn;
        [SerializeField] private MiniSlot[] _miniSlots;

        private int[] _favoriteIds = new int[0];
        private int[] _favoriteCounts = new int[0];

        /// <summary>
        /// 최소화 창. 띠 하나에 즐겨찾기 칸 둘과 최대화 버튼이 얹힌다.
        /// </summary>
        private void BuildMini()
        {
            var bar = UiTheme.Mini.Bar;

            _miniRoot = NewRect("Mini", _detailRoot);
            Place(_miniRoot, bar);

            // 띠 위에서는 클릭이 바탕화면으로 새면 안 된다 (패널과 같은 이유)
            Box(_miniRoot, new RectInt(0, 0, bar.width, bar.height),
                UiTheme.PanelFill, UiSprites.Shape.MinimumBadge, "MiniBadge").raycastTarget = true;

            var slot = UiTheme.Mini.Slot;
            _miniSlots = new MiniSlot[UiTheme.Mini.Slots];
            for (int i = 0; i < _miniSlots.Length; i++) _miniSlots[i] = BuildMiniSlot(Nth(slot, i), i);

            _miniMaxBtn = IconButton(_miniRoot, Nth(slot, UiTheme.Mini.Slots),
                                     "btn_maximize", "MiniMaximize", tint: Color.white);

            _miniRoot.gameObject.SetActive(false);

            static RectInt Nth(RectInt slot, int i) =>
                new RectInt(slot.x + UiTheme.Mini.SlotStep * i, slot.y, slot.width, slot.height);
        }

        /// <summary>
        /// 즐겨찾기 칸 하나. 등록돼 있으면 음식 그림이, 비어 있으면 부화기처럼 + 가 뜬다.
        /// </summary>
        private MiniSlot BuildMiniSlot(RectInt at, int index)
        {
            var root = NewRect("MiniSlot" + index, _miniRoot);
            Place(root, at);

            var bg = Backdrop(root.gameObject, UiSprites.Shape.Slot3, UiTheme.Slot);
            var slot = new MiniSlot { Root = root };

            slot.Plus = Label(root, new RectInt(0, 0, at.width, at.height), "+", 20, UiTheme.Ink);

            const int pad = 5;
            slot.Food = Icon(root, new RectInt(pad, pad, at.width - pad * 2, at.height - pad * 2),
                             null, Color.white, "Food");
            slot.Food.raycastTarget = false;

            // 개수는 음식 그리드와 같은 모양이다. 숫자가 길면 왼쪽으로 늘어난다.
            slot.CountBg = CountBadge(root, UiTheme.Mini.Count);
            slot.Count = Label(root, UiTheme.Mini.Count, "", 9, UiTheme.OnBadge);
            PlaceRight((RectTransform)slot.Count.transform, UiTheme.Mini.Count);

            // 개수가 0 인 즐겨찾기를 눌렀을 때 뜨는 「삭제」. 개수 뱃지보다 나중에 지어 그 위에 온다.
            slot.AskBg = Box(root, UiTheme.Mini.Ask, UiTheme.BadgeDark, UiSprites.Shape.Badge, "AskBg");
            slot.Ask = LocLabel(root, UiTheme.Mini.Ask, Keys.Delete, 8, UiTheme.OnBadge);
            slot.AskBg.enabled = false;
            slot.Ask.enabled = false;

            slot.Button = root.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = bg;
            return slot;
        }

        /// <summary>
        /// 즐겨찾기 칸을 채운다. 등록한 순서 그대로 놓인다.
        /// 가진 것이 없으면 그림을 흐리게 두고 개수에 0 을 띄운다 — 눌러도 안 나간다는 표시다.
        /// </summary>
        public void SetFavorites((int foodId, int count)[] foods)
        {
            _favoriteIds = new int[foods?.Length ?? 0];
            _favoriteCounts = new int[_favoriteIds.Length];

            for (int i = 0; i < Count(_miniSlots); i++)
            {
                var slot = _miniSlots[i];
                if (slot == null) continue;

                bool has = foods != null && i < foods.Length;
                if (has) { _favoriteIds[i] = foods[i].foodId; _favoriteCounts[i] = foods[i].count; }

                var row = has && SnailPet.Data.GameData.FoodDataById.TryGetValue(foods[i].foodId, out var f)
                        ? f : null;
                int count = has ? foods[i].count : 0;

                if (slot.Food != null)
                {
                    slot.Food.sprite = FoodSprite(row);
                    slot.Food.enabled = slot.Food.sprite != null;
                    slot.Food.color = count > 0 ? Color.white : UiTheme.Faded;
                }

                if (slot.Plus != null)    slot.Plus.enabled = !has;
                if (slot.Count != null)   slot.Count.text = has ? count.ToString() : "";
                if (slot.CountBg != null) slot.CountBg.enabled = has && slot.CountBg.sprite != null;
                FitCount(slot.Count, slot.CountBg);
            }

            // 목록이 바뀌면 물어보던 것도 없던 일이 된다
            AskDelete(-1);
        }

        /// <summary>
        /// 즐겨찾기 칸을 눌렀다.
        ///
        ///  · 비었으면 → 어디서 등록하는지 안내
        ///  · 가진 것이 있으면 → 바로 먹이기
        ///  · 가진 것이 없으면 → 「삭제」를 띄우고, 한 번 더 누르면 즐겨찾기에서 뺀다
        ///
        /// 개수가 0 이면 음식 목록에 그 음식이 안 나와 별을 다시 누를 길이 없다.
        /// 그래서 등록을 푸는 길을 여기에 둔다.
        /// </summary>
        private void PressFavorite(int index)
        {
            if (index < 0 || index >= _favoriteIds.Length)
            {
                ShowNotice(SnailPet.Data.Loc.Text(Keys.NoticeFavorite));
                return;
            }

            int id = _favoriteIds[index];
            if (_favoriteCounts != null && index < _favoriteCounts.Length && _favoriteCounts[index] > 0)
            {
                AskDelete(-1);
                FeedFood?.Invoke(id);
                return;
            }

            // 두 번째로 누른 것이면 뺀다. 빼는 길은 별과 같다 — 켜져 있으면 꺼진다.
            if (_favoriteAsk == index) { AskDelete(-1); ToggleFavorite?.Invoke(id); return; }

            AskDelete(index);
        }

        /// <summary>「삭제」를 띄울 칸. −1 이면 아무 데도 안 띄운다.</summary>
        private int _favoriteAsk = -1;

        private void AskDelete(int index)
        {
            _favoriteAsk = index;

            for (int i = 0; i < Count(_miniSlots); i++)
            {
                var slot = _miniSlots[i];
                if (slot == null) continue;

                bool on = i == index;
                if (slot.AskBg != null) slot.AskBg.enabled = on;
                if (slot.Ask != null)   slot.Ask.enabled = on;
            }
        }

        /// <summary>즐겨찾기가 꽉 찼다고 알린다. 문구는 UI 가 들고 있다.</summary>
        public void NoticeFavoriteFull() => ShowNotice(SnailPet.Data.Loc.Text(Keys.NoticeFavoriteFull));

        /// <summary>재화가 모자란다고 알린다. 문구는 UI 가 들고 있다.</summary>
        public void NoticeNoCoins() => ShowNotice(SnailPet.Data.Loc.Text(Keys.NoticeNoCoins));

        /// <summary>구매가 끝났다고 알린다. 문구는 UI 가 들고 있다.</summary>
        public void NoticePurchased() => ShowNotice(SnailPet.Data.Loc.Text(Keys.NoticePurchased));

        /// <summary>위젯 상자 기준 좌표로 옮긴다. 목업은 패널 왼쪽 위가 원점이라 코인 줄만큼 내려 준다.</summary>
        private static RectInt Above(RectInt r) => new RectInt(r.x, r.y - At.Coin.y, r.width, r.height);

        private static readonly string[] TabKeys = { "tab_snail", "tab_food", "tab_egg", "tab_shop", "tab_party" };

        private static readonly string[] TabNames =
            { "TabSnail", "TabFood", "TabEgg", "TabShop", "TabParty" };

        /// <summary>
        /// 탭 줄을 지금 개수에 맞춘다.
        ///
        /// <b>프리팹에는 탭이 넷일 때의 모습이 구워져 있다.</b> 그래서 코드에 다섯째(파티)를
        /// 더해도 살아날 때는 안 생긴다 — 배열이 프리팹에서 오기 때문이다.
        /// 모자란 만큼만 지어 붙이고, 이미 있는 넷은 건드리지 않는다.
        /// </summary>
        private void FitTabs()
        {
            int have = Count(_tabBtns);
            if (have >= Max.Tabs.Length && Count(_tabs) >= Max.Tabs.Length) return;

            var btns = new Button[Max.Tabs.Length];
            var arts = new Image[Max.Tabs.Length];
            for (int i = 0; i < have && i < btns.Length; i++)
            {
                btns[i] = _tabBtns[i];
                arts[i] = i < Count(_tabs) ? _tabs[i] : null;
            }

            var parent = have > 0 && _tabBtns[0] != null
                       ? (RectTransform)_tabBtns[0].transform.parent : _listRoot;

            for (int i = have; i < btns.Length; i++)
            {
                // 탭 아트에는 종이 모양 배경이 들어 있어 Button 도형을 따로 깔지 않는다.
                btns[i] = IconButton(parent, Above(Max.Tabs[i]), TabArt(i, false), TabNames[i]);
                arts[i] = btns[i].transform.Find("Glyph").GetComponent<Image>();
            }

            _tabBtns = btns;
            _tabs = arts;
        }

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
            _tabs = new Image[Max.Tabs.Length];
            _tabBtns = new Button[Max.Tabs.Length];
            FitTabs();

            var panel = Panel(_listRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _listTitle = Label(panel, new RectInt(0, 8, UiTheme.PanelW, 16), "", 12, UiTheme.Ink);

            BuildFoodGrid(panel);

            BuildShopCategories(panel);
            BuildWardrobeList(panel);
            BuildGeneList(panel);
            BuildGuideList(panel);
            BuildSettingsList(panel);
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

            /// <summary>수량이 앉는 동그란 배지. 글자만으로는 그림·테두리에 묻힌다.</summary>
            public Image CountBg;

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
        /// <summary>패널 좌표를 스크롤 영역 안의 좌표로 바꾼다. 영역이 패널 위쪽에서 내려와 있다.</summary>
        private static RectInt InGrid(RectInt at) =>
            new RectInt(at.x, at.y - Max.FoodView.y, at.width, at.height);

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

        /// <summary>
        /// 칸의 수량을 적는다. 빈 문자열이면 배지도 같이 감춘다 —
        /// 알처럼 수량이 없는 칸에 빈 동그라미만 남으면 안 된다.
        /// </summary>
        /// <summary>
        /// 오른쪽 끝을 고정한 채 놓는다. 개수 뱃지처럼 <b>글자에 따라 가로로 늘어나는</b> 것에 쓴다.
        /// 피벗이 오른쪽이라 sizeDelta.x 만 키우면 왼쪽으로 자란다.
        /// </summary>
        private static void PlaceRight(RectTransform rt, RectInt r)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(r.width, r.height);
            rt.anchoredPosition = new Vector2(r.xMax, -r.y);
        }

        /// <summary>
        /// 칸의 개수 뱃지.
        ///
        /// 동그란 그림(icon_circle)은 두 자리만 넘어가도 숫자가 삐져나왔다. 늘어나는
        /// 9-슬라이스 도형(slot_count)을 깔고, 오른쪽 끝을 칸 모서리에 고정한 채 왼쪽으로 늘린다.
        /// 실제 폭은 <see cref="FitCount"/> 가 글자를 재서 정한다.
        /// </summary>
        private Image CountBadge(RectTransform root, RectInt at)
        {
            var bg = Box(root, at, UiTheme.BadgeDark, UiSprites.Shape.SlotCount, "CountBg");
            PlaceRight((RectTransform)bg.transform, at);
            return bg;
        }

        /// <summary>개수 글자 좌우 여백(px).</summary>
        private const int CountPadX = 4;

        /// <summary>뱃지 폭을 글자에 맞춘다. 한 자리면 정사각형, 길어지면 그만큼 왼쪽으로 늘어난다.</summary>
        private static void FitCount(Text count, Image bg)
        {
            if (count == null) return;

            var rt = (RectTransform)count.transform;
            float h = rt.sizeDelta.y;
            float w = Mathf.Max(h, count.preferredWidth + CountPadX * 2f);

            rt.sizeDelta = new Vector2(w, h);
            if (bg != null)
            {
                var brt = (RectTransform)bg.transform;
                brt.sizeDelta = new Vector2(w, brt.sizeDelta.y);
            }
        }

        /// <summary>
        /// 칸 배경을 등급 그림으로 바꾼다. 어느 그림인지는 EnumData 의 SlotResourceKey 가 정한다.
        /// 시트에 키가 없거나 아트를 못 찾으면 기본 칸으로 둔다 — 등급이 없는 것도 있다.
        /// </summary>
        private static void SetSlotRarity(GridSlot slot, SnailPet.Data.RarityType rarity) =>
            SetSlotArt(slot?.Root, rarity, UiSprites.Shape.Slot2);

        /// <summary>
        /// 배경을 등급 그림으로 바꾼다. 못 찾으면 <paramref name="fallback"/> 도형으로 둔다.
        /// 칸(정사각)과 목록 행(가로로 긴 바)은 기본 도형이 다르므로 그것만 갈라 받는다 —
        /// 등급 아트는 9-슬라이스라 어느 쪽 크기에도 맞는다.
        /// </summary>
        private static void SetSlotArt(RectTransform root, SnailPet.Data.RarityType rarity,
                                       UiSprites.Shape fallback)
        {
            if (root == null) return;

            var img = root.GetComponent<Image>();
            if (img == null) return;

            img.sprite = UiSprites.SlotByKey(SnailPet.Data.Enums.SlotOf(rarity)) ?? UiSprites.Of(fallback);
            img.type = Image.Type.Sliced;
            img.color = Color.white;      // 아트에 색이 들어 있다. 물들이면 탁해진다
        }

        private static void SetSlotCount(GridSlot slot, string text)
        {
            if (slot == null || slot.Count == null) return;

            slot.Count.text = text;
            if (slot.CountBg != null) slot.CountBg.enabled = !string.IsNullOrEmpty(text)
                                                          && slot.CountBg.sprite != null;

            FitCount(slot.Count, slot.CountBg);
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

            // 수량은 테두리보다 뒤에 지어야 위에 온다. 고른 칸에서 가려지지 않게 하려는 것이다.
            slot.CountBg = CountBadge(root, Max.FoodCountBadge);
            slot.Count = Label(root, Max.FoodCountBadge, "", 9, UiTheme.OnBadge);
            PlaceRight((RectTransform)slot.Count.transform, Max.FoodCountBadge);

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

            // 값 알약이 아이콘 자리를 조금 물고 들어온다. 아이콘을 나중에 지어 위에 오게 한다
            // (메인 패널 게이지도 같은 순서다).
            Box(_foodPanel, Fd.FullValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "FullValue");
            _foodFull = Label(_foodPanel, Fd.FullValue, "", 9, UiTheme.Ink);
            Icon(_foodPanel, Fd.FullIcon, "icon_food", Color.white, "FullIcon").raycastTarget = false;

            Box(_foodPanel, Fd.HappyValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "HappyValue");
            _foodHappy = Label(_foodPanel, Fd.HappyValue, "", 9, UiTheme.Ink);
            Icon(_foodPanel, Fd.HappyIcon, "icon_happy", Color.white, "HappyIcon").raycastTarget = false;

            Box(_foodPanel, Fd.Info, UiTheme.Slot, UiSprites.Shape.Slot, "InfoBox");
            _foodInfo = Label(_foodPanel, new RectInt(Fd.Info.x + 4, Fd.Info.y, Fd.Info.width - 8, Fd.Info.height),
                              "", 8, UiTheme.Ink);
            _foodInfo.horizontalOverflow = HorizontalWrapMode.Wrap;

            var feed = Box(_foodPanel, Fd.Feed, UiTheme.Slot, UiSprites.Shape.Button, "FeedButton");
            feed.raycastTarget = true;
            LocLabel(_foodPanel, Fd.Feed, Keys.Feed, 10, UiTheme.OnButton);
            _feedBtn = feed.gameObject.AddComponent<Button>();
            _feedBtn.targetGraphic = feed;

            _foodBuyBtn  = IconButton(_foodPanel, Fd.Buy,  "btn_shop", "Buy");
            _foodSellBtn = IconButton(_foodPanel, Fd.Sell, "btn_sell", "Sell");
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

                SetSlotRarity(_foodSlots[i], row != null ? row.RarityType : SnailPet.Data.RarityType.Common);
                _foodSlots[i].Icon.sprite = FoodSprite(row);
                _foodSlots[i].Icon.enabled = _foodSlots[i].Icon.sprite != null;
                SetSlotCount(_foodSlots[i], foods[i].count > 1 ? foods[i].count.ToString() : "");
            }

            // 하나도 없으면 상세에 마지막으로 보던 음식이 남는다. 양쪽 다 안내로 바꾼다.
            bool empty = _foodIds.Length == 0;
            if (_foodEmpty != null) _foodEmpty.enabled = empty;
            ShowFoodDetail(!empty);

            // 내용 높이를 줄 수에 맞춘다. 이게 스크롤 범위를 정한다.
            int rows = Mathf.CeilToInt((_foodIds.Length) / (float)Max.FoodCols);
            _foodContent.sizeDelta = new Vector2(UiTheme.PanelW,
                Mathf.Max(Max.FoodView.height, Max.FoodSlot.y + rows * Max.FoodStepY));

            SelectFood(_foodIds.Length > 0 ? 0 : -1);
        }

        [SerializeField] private Text _foodEmpty, _foodEmptyDetail;
        [SerializeField] private Text _wardrobeEmpty;

        /// <summary>
        /// 음식 상세를 통째로 여닫는다.
        ///
        /// 가진 음식이 없을 때 마지막으로 보던 음식이 그대로 남아 있으면 안 된다. 상세는 조각이
        /// 많고 이름도 제각각이라 하나씩 끄는 대신 판의 자식을 통째로 여닫는다.
        /// 안내 문구와 <b>구매 버튼은 남긴다</b> — 음식이 없을 때야말로 상점으로 갈 길이 필요하다.
        /// </summary>
        private void ShowFoodDetail(bool on)
        {
            if (_foodPanel == null) return;

            foreach (Transform child in _foodPanel)
            {
                if (_foodEmptyDetail != null && child == _foodEmptyDetail.transform) continue;
                if (_foodBuyBtn != null && child == _foodBuyBtn.transform) continue;

                child.gameObject.SetActive(on);
            }

            if (_foodEmptyDetail != null) _foodEmptyDetail.enabled = !on;
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

        /// <summary>알을 부화기에 넣기 · 부화한 달팽이 수령.</summary>
        public event Action<int> PutEgg, ClaimHatched;

        /// <summary>
        /// 상점으로 갔다. 화면을 옮기는 것은 UI 가 스스로 하므로(<see cref="OpenShop"/>)
        /// 이건 「갔다」는 알림이다. 받는 쪽이 다시 탭을 옮기면 골라 둔 상품이 풀린다.
        /// </summary>
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

            // 알이 하나도 없을 때만 보이는 안내. 비는 쪽이 왼쪽 목록이라 그 그리드에 붙인다
            // (그리드 자식이라 알 탭에서만 보인다).
            _eggEmpty = LocLabel(_eggGridRoot, InGrid(UiTheme.Egg.Empty), Keys.NoEgg, 10, UiTheme.Slot);

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

            slot.Timer = Label(root, new RectInt(0, at.height - 16, at.width, 14), "", 9, UiTheme.OnButton);

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
                SetSlotRarity(_eggSlots[i], row != null ? row.RarityType : SnailPet.Data.RarityType.Common);
                _eggSlots[i].Icon.sprite = EggSprite(row);
                _eggSlots[i].Icon.enabled = _eggSlots[i].Icon.sprite != null;
                SetSlotCount(_eggSlots[i], "");      // 낱개라 수량이 없다
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

            /// <summary>썸네일 칸 위에 얹는 달팽이 모습. 칸은 배경으로 남는다.</summary>
            public RawImage Face;

            /// <summary>고른 줄에 덧그리는 테두리(slotline2).</summary>
            public Image Frame;

            /// <summary>줄 전체를 누르는 버튼. 오른쪽에 그 달팽이 정보를 띄운다.</summary>
            public Button Button;

            public Image RarityBadge, RarityIcon;
            public Text Name, Rarity, Age;
            public Button Swap;
        }

        /// <summary>
        /// 목록 썸네일에 들어가는 달팽이 모습. 부트스트랩이 찍어 넘긴 렌더 텍스처를 받는다.
        /// 그림이 없을 때를 대비해 아래 칸 도형은 그대로 두고 그 위에 얹는다.
        /// </summary>
        /// <summary>
        /// 지금 나와 있는 달팽이 줄에 덧그리는 테두리. 줄 위에 같은 크기로 겹친다.
        /// 칸(slotline)과 줄(slotline2)은 모양이 달라 아트를 따로 쓴다.
        /// </summary>
        private Image RowFrame(RectTransform parent)
        {
            var frame = NewRect("Frame", parent).gameObject.AddComponent<Image>();
            Fill((RectTransform)frame.transform);

            frame.sprite = UiSprites.Of(UiSprites.Shape.RowSelection);
            frame.type = Image.Type.Sliced;
            frame.gameObject.AddComponent<UiShapeRef>().Shape = UiSprites.Shape.RowSelection;
            frame.color = UiSprites.IsArt(UiSprites.Shape.RowSelection) ? Color.white : UiTheme.Selected;
            frame.raycastTarget = false;
            frame.enabled = false;
            return frame;
        }

        private RawImage FaceView(RectTransform parent, RectInt at)
        {
            var rt = NewRect("Face", parent);
            Place(rt, at);

            var img = rt.gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
            img.enabled = false;      // 텍스처가 들어올 때 켠다
            return img;
        }

        /// <summary>목록 썸네일을 찍을 크기. 목업이 정사각형이다.</summary>
        public static Vector2Int RowThumbSize =>
            new Vector2Int(Max.RowThumb.width, Max.RowThumb.height);

        // ── 목록에서 고르기 ──
        //
        // 줄을 누르면 오른쪽에 그 달팽이의 정보가 뜬다. 화면을 도는 달팽이는 그대로다 —
        // 바꾸는 것은 교체 버튼만 한다. 고른 줄에는 테두리가 얹힌다.
        //
        // 다른 탭에 갔다 오면 화면의 달팽이로 돌아간다. 어느 달팽이를 보고 있었는지까지
        // 기억하면, 탭을 옮겼다 왔을 때 화면과 정보가 다른 채로 남아 헷갈린다.

        private int _selectedRow = -1, _activeRow = -1;

        /// <summary>목록에서 달팽이를 골랐다. 몇 번째 줄인지를 준다.</summary>
        public event Action<int> SnailPicked;

        private void PickRow(int index)
        {
            if (index < 0 || index >= Count(_rows)) return;

            _selectedRow = index;
            PaintRowFrames();
            SnailPicked?.Invoke(index);
        }

        /// <summary>화면에 나와 있는 달팽이 줄로 되돌린다. 탭을 옮길 때 부른다.</summary>
        public void ResetPick()
        {
            _selectedRow = -1;
            PaintRowFrames();
        }

        /// <summary>고른 줄에만 테두리를 켠다. 고른 것이 없으면 화면의 달팽이 줄이다.</summary>
        private void PaintRowFrames()
        {
            int on = _selectedRow >= 0 ? _selectedRow : _activeRow;

            for (int i = 0; i < Count(_rows); i++)
                if (_rows[i]?.Frame != null) _rows[i].Frame.enabled = i == on;
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
                Face   = FaceView(rowRt, Max.RowThumb),
                Name   = Label(rowRt, Max.RowName, "", 11, UiTheme.Ink),
                Rarity = null,
                Age    = null,
            };

            // 이름은 왼쪽 정렬. 가운데 정렬이면 이름 길이에 따라 시작점이 들쭉날쭉해진다
            // (상세보기의 파츠 줄과 같은 규칙).
            row.Name.alignment = TextAnchor.MiddleLeft;

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

            // 줄 자체도 누를 수 있다. 누르면 오른쪽에 그 달팽이 정보가 뜬다 —
            // 화면의 달팽이를 바꾸는 것은 교체 버튼만 한다.
            // 교체 버튼은 자식이라 더 위에 있어 그쪽을 누르면 줄이 안 눌린다.
            row.Button = rowRt.gameObject.AddComponent<Button>();
            row.Button.targetGraphic = bg;

            // 테두리는 줄 위에 얹히므로 늦게 그린다. 다만 교체 버튼은 그보다 더 위여야 한다 —
            // 테두리가 버튼을 가로지르면 눌러도 되는 것처럼 안 보인다.
            row.Frame = RowFrame(rowRt);
            row.Swap.transform.SetAsLastSibling();
            return row;
        }

        /// <summary>지금 나와 있는 달팽이는 교체 버튼이 없다 (목업 주석).</summary>
        public void SetRows((string name, SnailPet.Data.RarityType rarity, int age, bool isActive, Texture face)[] rows)
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
                // 달팽이 그림이 들어가는 정사각 칸만 등급 색으로 바꾼다 (줄 배경은 그대로 둔다)
                if (_rows[i].Thumb != null)
                    SetSlotArt((RectTransform)_rows[i].Thumb.transform, r.rarity, UiSprites.Shape.Slot2);
                _rows[i].Age.text    = SnailPet.Data.Loc.Format(Keys.Age, r.age);
                _rows[i].Swap.gameObject.SetActive(!r.isActive);

                if (_rows[i].Face != null)
                {
                    _rows[i].Face.texture = r.face;
                    _rows[i].Face.enabled = r.face != null;
                }

                // 지금 나와 있는 달팽이가 몇 번째 줄인지 기억해 둔다. 고른 것이 없거나
                // 목록이 줄어 사라졌으면 이 줄로 돌아간다.
                if (r.isActive) _activeRow = i;
            }

            if (_selectedRow >= count) _selectedRow = -1;
            PaintRowFrames();
        }

        /// <summary>탭 선택. 지금은 색만 바뀌고 내용은 그대로다.</summary>
        public void SetTab(int index)
        {
            int was = _tab;
            _tab = Mathf.Clamp(index, 0, _tabs.Length - 1);

            // 옷장·상세보기에서 뒤로가기로 나오면 같은 탭으로 돌아온다. 그때는 「카테고리를
            // 옮긴 것」이 아니므로 고른 달팽이를 풀지 않는다 — 보던 정보 그대로 돌아가야 한다.
            bool moved = _tab != was;

            // 탭을 누르면 옷장·상세보기·설정에서 빠져나온다. 셋 다 왼쪽 패널을 통째로 쓰기 때문이다.
            if (_inWardrobe || _inGene || _inSettings || _inGuide)
            {
                _inWardrobe = false;
                _inGene = false;
                _inSettings = false;
                _inGuide = false;
                if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
                if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);
                if (_geneRoot != null)      _geneRoot.gameObject.SetActive(false);
                if (_genePanel != null)     _genePanel.gameObject.SetActive(false);
                if (_settingsRoot != null)  _settingsRoot.gameObject.SetActive(false);
                if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
                if (_guideRoot != null)     _guideRoot.gameObject.SetActive(false);
                if (_guidePanel != null)    _guidePanel.gameObject.SetActive(false);
            }

            // 탭이 왼쪽 목록과 오른쪽 상세를 함께 바꾼다. 둘은 항상 같은 것을 보여 줘야 한다.
            // 파티(멀티플레이어)는 다섯째 탭이다 — 옷장·도감처럼 얹히는 화면이 아니라 탭 자신이다.
            bool food = _tab == 1, egg = _tab == 2, shop = _tab == 3, multi = _tab == 4;
            _inMulti = multi;

            if (_foodGridRoot != null) _foodGridRoot.gameObject.SetActive(food);
            if (_foodPanel != null)    _foodPanel.gameObject.SetActive(food);
            if (_eggGridRoot != null)  _eggGridRoot.gameObject.SetActive(egg);
            if (_eggPanel != null)     _eggPanel.gameObject.SetActive(egg);
            if (_multiRoot != null)    _multiRoot.gameObject.SetActive(multi);
            if (_multiPanel != null)   _multiPanel.gameObject.SetActive(multi);
            if (_panel != null)        _panel.gameObject.SetActive(!food && !egg && !shop && !multi);
            if (_rowGridRoot != null)  _rowGridRoot.gameObject.SetActive(!food && !egg && !shop && !multi);

            if (multi) PaintMultiTabs();

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

            string[] titles = { Keys.SnailList, Keys.FoodList, Keys.EggList, Keys.Shop, Keys.Multiplayer };
            _listTitle.text = SnailPet.Data.Loc.Text(titles[_tab]);
            RefreshClose();      // 설정에서 나왔으면 X 가 다시 접는 버튼이 된다

            if (!moved) return;

            // 탭을 옮기면 골라 둔 달팽이가 풀린다. 돌아왔을 때 화면의 달팽이와 오른쪽 정보가
            // 다르면 어느 쪽이 지금 나와 있는 것인지 헷갈린다.
            ResetPick();
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
        public event Action<int> BuyProduct;

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
            BuildShopFilters(panel);
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
            _pickShape = ShapeBehind(_pickIcon, "PickShape");

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
            _shopShape = ShapeBehind(_shopIcon, "PreviewShape");

            // 포만·행복은 음식에만 있다. 한 상자에 묶어 통째로 껐다 켠다.
            _shopStats = NewRect("Stats", _shopItemPanel);
            _shopStats.anchorMin = Vector2.zero; _shopStats.anchorMax = Vector2.one;
            _shopStats.offsetMin = Vector2.zero; _shopStats.offsetMax = Vector2.zero;

            Box(_shopStats, Fd.FullValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "FullValue");
            _shopFull = Label(_shopStats, Fd.FullValue, "", 9, UiTheme.Ink);
            Icon(_shopStats, Fd.FullIcon, "icon_food", Color.white, "FullIcon").raycastTarget = false;

            Box(_shopStats, Fd.HappyValue, UiTheme.Slot, UiSprites.Shape.LevelBadge, "HappyValue");
            _shopHappy = Label(_shopStats, Fd.HappyValue, "", 9, UiTheme.Ink);
            Icon(_shopStats, Fd.HappyIcon, "icon_happy", Color.white, "HappyIcon").raycastTarget = false;

            Box(_shopItemPanel, Fd.Info, UiTheme.Slot, UiSprites.Shape.Slot, "InfoBox");
            _shopInfo = Label(_shopItemPanel, new RectInt(Fd.Info.x + 4, Fd.Info.y, Fd.Info.width - 8, Fd.Info.height),
                              "", 8, UiTheme.Ink);
            _shopInfo.horizontalOverflow = HorizontalWrapMode.Wrap;

            var buy = Box(_shopItemPanel, Sh.Buy, UiTheme.Slot, UiSprites.Shape.Button, "BuyButton");
            buy.raycastTarget = true;

            // 글자와 가격은 버튼의 자식이다. 살 것이 없을 때 버튼을 끄면 같이 사라져야 한다.
            var buyRt = (RectTransform)buy.transform;
            LocLabel(buyRt, Sh.BuyLabel, Keys.BuyIt, 10, UiTheme.OnButton);
            Icon(buyRt, Sh.BuyCoin, "icon_coin", Color.white, "BuyCoinIcon").raycastTarget = false;
            _shopCost = Label(buyRt, Sh.BuyCost, "", 10, UiTheme.OnButton);
            _shopCost.alignment = TextAnchor.MiddleLeft;

            _shopBuyBtn = buy.gameObject.AddComponent<Button>();
            _shopBuyBtn.targetGraphic = buy;

            // 뒤로 가기. 목업에서 닫기 X 자리에 화살표가 들어온다.
            // btn_back 아트가 아직 없어 글자로 그린다 — 스프라이트 없는 Image 는 색 사각형이 된다.
            // 아트가 들어오기 전에는 버튼 도형에 「←」 글자를 얹어 두었다. 이제 그림이 있다.
            _backBtn = IconButton(_detailRoot, Above(Sh.Back), "btn_back", "Back", tint: Color.white);
            _shopBack = (RectTransform)_backBtn.transform;
            _shopBack.gameObject.SetActive(false);
        }

        // ── 상점 악세서리 부위 필터 ──
        //
        // 옷장과 같은 칩을 상품 그리드 위에 얹는다. 악세서리 카테고리에서만 뜨고,
        // 그동안 그리드는 옷장과 같은 자리(필터 줄 아래)로 내려간다.

        [SerializeField] private RectTransform _shopFilterRoot;
        [SerializeField] private FilterChip[] _shopFilters;

        /// <summary>필터 줄 하나. 부위마다 칩이 하나씩이고 여러 개를 동시에 켤 수 있다.</summary>
        private void BuildShopFilters(RectTransform panel)
        {
            _shopFilterRoot = Fill(NewRect("ShopFilters", panel));
            _shopFilterRoot.gameObject.SetActive(false);

            var parts = Parts;
            _shopFilters = new FilterChip[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                var f = UiTheme.Wardrobe.Filter;
                var at = new RectInt(f.x + i * UiTheme.Wardrobe.FilterStep, f.y, f.width, f.height);

                var root = NewRect("Filter" + parts[i], _shopFilterRoot);
                Place(root, at);

                var box = Backdrop(root.gameObject, UiSprites.Shape.LevelBadge, UiTheme.Slot);
                var label = LocLabel(root, new RectInt(0, 0, at.width, at.height), Keys.PartOf(parts[i]), 8, UiTheme.Ink);

                var btn = root.gameObject.AddComponent<Button>();
                btn.targetGraphic = box;

                _shopFilters[i] = new FilterChip { Root = root, Box = box, Label = label, Button = btn, On = true };
            }
        }

        /// <summary>상점 필터를 눌렀다. 그리드만 다시 채우면 된다 — 고른 상품은 첫 칸으로 돌아간다.</summary>
        private void ToggleShopFilter(int index)
        {
            if (index < 0 || index >= Count(_shopFilters)) return;

            _shopFilters[index].On = !_shopFilters[index].On;
            PaintFilters(_shopFilters);
            FillShopGrid();
            SelectShopSlot(_shopIds.Length > 0 ? 0 : -1);
        }

        /// <summary>지금 카테고리가 악세서리인가. 필터 줄은 그때만 뜬다.</summary>
        private bool InAccessoryCategory
        {
            get
            {
                var cats = SnailPet.Snail.Shop.Categories;
                return _shopCat >= 0 && _shopCat < cats.Length
                    && cats[_shopCat] == SnailPet.Data.CategoryType.Accessories;
            }
        }

        /// <summary>
        /// 상품 그리드를 지금 카테고리·필터로 다시 채운다.
        /// 악세서리에서만 부위 필터가 걸리고, 나머지 카테고리는 전부 그대로 나온다.
        /// </summary>
        private void FillShopGrid()
        {
            var cats = SnailPet.Snail.Shop.Categories;
            if (_shopCat < 0 || _shopCat >= cats.Length) return;

            bool accessory = InAccessoryCategory;
            var shown = new System.Collections.Generic.List<ShopRow>();

            foreach (var p in SnailPet.Snail.Shop.ProductsOf(cats[_shopCat]))
            {
                if (accessory
                    && SnailPet.Data.GameData.AccessoriesDataById.TryGetValue(p.Id, out var a)
                    && !FilterOn(_shopFilters, a.AccessoriesType)) continue;

                shown.Add(p);
                if (shown.Count >= _shopSlots.Length) break;
            }

            _shopIds = new int[shown.Count];
            for (int i = 0; i < _shopSlots.Length; i++)
            {
                bool has = i < shown.Count;
                _shopSlots[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                _shopIds[i] = shown[i].Id;
                SetSlotRarity(_shopSlots[i], shown[i].RarityType);
                _shopSlots[i].Icon.sprite = ProductSprite(shown[i]);
                _shopSlots[i].Icon.enabled = _shopSlots[i].Icon.sprite != null;
                SetSlotCount(_shopSlots[i], shown[i].ItemCount > 1 ? shown[i].ItemCount.ToString() : "");
                _shopSlots[i].Frame.enabled = false;
            }
            FitContent(_shopGridContent, _shopIds.Length);
        }

        /// <summary>카테고리를 골랐다. 그 안의 상품 그리드로 들어간다.</summary>
        private void EnterShopCategory(int index)
        {
            var cats = SnailPet.Snail.Shop.Categories;
            if (index < 0 || index >= cats.Length) return;

            _shopCat = index;
            FillShopGrid();

            _listTitle.text = SnailPet.Data.Loc.Text(Keys.CategoryOf(cats[index]));
            ApplyShopStage();
            SelectShopSlot(_shopIds.Length > 0 ? 0 : -1);
        }

        /// <summary>
        /// 상점을 연다. <paramref name="itemId"/> 를 파는 곳이 있으면 그 카테고리로 들어가
        /// 그 상품을 골라 둔다. 음식 상세의 「구매」가 이 길로 들어온다.
        ///
        /// 파는 물건이 아니거나(예: 잡은 음식이 상점에 없음) 0 이면 상점 첫 화면까지만 간다.
        /// </summary>
        public void OpenShop(int itemId = 0)
        {
            SetTab(3);
            if (itemId == 0) return;

            var cats = SnailPet.Snail.Shop.Categories;
            for (int c = 0; c < cats.Length; c++)
            {
                var products = SnailPet.Snail.Shop.ProductsOf(cats[c]);
                for (int i = 0; i < products.Length; i++)
                {
                    // ShopData 의 Id 가 곧 파는 아이템의 Id 다 (그림도 그것으로 찾는다).
                    if (products[i].Id != itemId) continue;

                    EnterShopCategory(c);
                    if (i < _shopIds.Length) SelectShopSlot(i);   // 칸보다 뒤에 있으면 첫 칸에 둔다
                    return;
                }
            }
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

            bool filtered = inCategory && InAccessoryCategory;

            if (_shopCatRoot != null)   _shopCatRoot.gameObject.SetActive(shop && !inCategory);
            if (_shopFilterRoot != null) _shopFilterRoot.gameObject.SetActive(filtered);
            if (_shopGridRoot != null)
            {
                _shopGridRoot.gameObject.SetActive(inCategory);

                // 필터 줄이 뜨면 그리드는 옷장과 같은 자리(줄 아래)로 내려간다
                Place(_shopGridRoot, filtered ? UiTheme.Wardrobe.View : Max.FoodView);
            }
            if (_shopPanel != null)     _shopPanel.gameObject.SetActive(shop && !inCategory);
            if (_shopItemPanel != null) _shopItemPanel.gameObject.SetActive(inCategory);

            RefreshBackButton();
        }

        /// <summary>
        /// 뒤로가기와 X 중 무엇이 보일지 정한다. 둘은 같은 자리를 쓰므로 하나만 나온다.
        ///
        /// 뒤로갈 곳이 있는 화면은 셋이다 — 상점 상품 단계, 옷장, 상세보기.
        /// 셋 다 「들어온 자리」가 분명해서 X 로 나가는 것보다 되돌아가는 편이 맞다.
        /// </summary>
        private void RefreshBackButton()
        {
            bool back = (_tab == 3 && _shopCat >= 0) || _inWardrobe || _inGene || _inGuide;

            if (_shopBack != null) _shopBack.gameObject.SetActive(back);
            if (_closeBtn != null) _closeBtn.gameObject.SetActive(!back);
        }

        /// <summary>
        /// 뒤로가기. 지금 어느 화면에 있느냐에 따라 돌아갈 곳이 다르다.
        /// 옷장·상세보기에서는 고른 달팽이를 그대로 둔 채 정보 화면으로 돌아간다.
        /// </summary>
        private void GoBack()
        {
            if (_inWardrobe) { OpenWardrobe(false); return; }
            if (_inGene)     { OpenGene(false); return; }
            if (_inGuide)    { OpenGuide(false); return; }

            LeaveShopCategory();
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
                if (_shopShape != null) _shopShape.enabled = false;
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

            // 오늘의 할인으로 뽑힌 상품이면 여기서도 할인가다. 파는 값을 한 곳에서 정하므로
            // 「할인 칸에서 산 것만 싸다」 같은 어긋남이 생기지 않는다.
            int cost = SnailPet.Snail.Shop.UnitCost(row);
            bool sale = SnailPet.Snail.Shop.IsTodayPick(row) && SnailPet.Snail.Shop.IsDiscounted(row);

            _shopCost.text = cost > 0 ? cost.ToString("N0") : "-";
            _shopCost.color = sale ? UiTheme.Discount : UiTheme.OnButton;
            _shopIcon.sprite = ProductSprite(row);
            _shopIcon.enabled = _shopIcon.sprite != null;
            ShowShape(_shopShape, _shopIcon, row.CategoryType);

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
        [SerializeField] private Image _pickShape, _shopShape;

        /// <summary>
        /// 상품 미리보기 뒤에 까는 달팽이 실루엣(snail_shape2).
        ///
        /// 악세서리 아트는 파츠와 같은 1200x1200 공유 캔버스에 그려져 있다. 그래서 미리보기
        /// 칸에 통째로 넣으면 그림이 <b>달팽이에 얹힐 자리</b>에 그대로 온다 — 모자가 칸 왼쪽
        /// 위에 작게 뜨는 것이 그 때문이다. 실루엣도 같은 캔버스라 아이콘과 같은 자리에 같은
        /// 크기로 깔기만 하면 저절로 맞는다. 칸 치수를 따로 계산할 필요가 없다.
        ///
        /// 자리는 아이콘에서 그대로 베낀다. 프리팹에서 미리보기를 손으로 옮겨도 따라온다.
        /// </summary>
        private Image ShapeBehind(Image icon, string name)
        {
            if (icon == null) return null;

            var src = (RectTransform)icon.transform;
            var rt = NewRect(name, (RectTransform)src.parent);

            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.sizeDelta = src.sizeDelta;
            rt.anchoredPosition = src.anchoredPosition;
            rt.localScale = src.localScale;
            rt.SetSiblingIndex(src.GetSiblingIndex());   // 아이콘 바로 뒤에 깔린다

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("Ui/Icon/snail_shape2");
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        /// <summary>악세서리일 때만 실루엣을 보인다. 음식이나 알은 얹을 몸이 없다.</summary>
        private static void ShowShape(Image shape, Image icon, SnailPet.Data.CategoryType category)
        {
            if (shape == null) return;

            shape.enabled = category == SnailPet.Data.CategoryType.Accessories
                         && shape.sprite != null
                         && icon != null && icon.enabled;
        }

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
            if (_pickShape != null) _pickShape.enabled = false;
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
            ShowShape(_pickShape, _pickIcon, row.CategoryType);
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
                // 옷장·상세보기·설정은 같은 자리를 쓰므로 하나만 떠 있어야 한다.
                // ApplyGene 을 부르면 안 된다 — 꺼질 때 SetTab 으로 되돌리기 때문이다.
                _inGene = false;
                _inSettings = false;
                if (_geneRoot != null)  _geneRoot.gameObject.SetActive(false);
                if (_genePanel != null) _genePanel.gameObject.SetActive(false);
                if (_settingsRoot != null)  _settingsRoot.gameObject.SetActive(false);
                if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
                _inGuide = false;
                if (_guideRoot != null)  _guideRoot.gameObject.SetActive(false);
                if (_guidePanel != null) _guidePanel.gameObject.SetActive(false);
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
                RefreshClose();
                RefreshBackButton();
            }
            else SetTab(_tab);   // 있던 탭으로 되돌린다 (SetTab 안에서 X 잠금도 다시 정해진다)
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

            // 칸이 하나도 안 뜨면 왜 비었는지 알려 준다. 필터로 다 걸러진 때도 마찬가지다 —
            // 빈 격자만 남는 것보다는 낫다.
            if (_wardrobeEmpty != null) _wardrobeEmpty.enabled = _wardrobeIds.Length == 0;

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
                SetSlotCount(_wardrobeSlots[i], worn ? SnailPet.Data.Loc.Text(Keys.Worn) : "");
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
                SetSlotCount(_wornSlots[i], "");
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

        private bool FilterOn(SnailPet.Data.AccessoriesType type) => FilterOn(_filters, type);

        private static bool FilterOn(FilterChip[] chips, SnailPet.Data.AccessoriesType type)
        {
            var parts = Parts;
            for (int i = 0; i < parts.Length && i < Count(chips); i++)
                if (parts[i] == type) return chips[i] == null || chips[i].On;
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

        private void PaintFilters() => PaintFilters(_filters);

        private static void PaintFilters(FilterChip[] chips)
        {
            for (int i = 0; i < Count(chips); i++)
            {
                if (chips[i] == null) continue;
                bool on = chips[i].On;
                chips[i].Box.color = on ? Color.white : UiTheme.Faded;
                chips[i].Label.color = on ? UiTheme.Ink : UiTheme.Slot;
            }
        }

        /// <summary>필터가 바뀌었으니 목록을 다시 달라는 신호.</summary>
        public event Action FilterChanged;

        // ── 멀티플레이어 ──
        //
        // 옷장·도감처럼 좌우 패널을 통째로 쓴다. 왼쪽은 탭 둘(친구·로비)짜리 목록,
        // 오른쪽은 「방」 버튼 셋. 방에 들어가면 오른쪽이 참가자 목록으로 바뀐다.
        //
        // 지금은 껍데기다 — 목록도 참가자도 게임 쪽이 넣어 주는 대로만 그리고,
        // 누르면 이벤트만 낸다. Steam 이 붙으면 그 이벤트에 실제 동작을 매면 된다.

        [Serializable]
        public sealed class MultiRow
        {
            public RectTransform Root;
            public Text Name;
            public Button Button;      // 줄 전체
            public Button Action;      // 오른쪽 끝 (초대 / 입장)
        }

        [Serializable]
        public sealed class MultiMember
        {
            public RectTransform Root;
            public RawImage Face;
            public Text Name;
            public Button Zoom;
        }

        [SerializeField] private RectTransform _multiRoot, _multiPanel, _multiContent;
        [SerializeField] private Button _friendTab, _lobbyTab;
        [SerializeField] private Image _friendTabBg, _lobbyTabBg;
        [SerializeField] private MultiRow[] _multiRows;
        [SerializeField] private RectTransform _roomGroup, _lobbyGroup;
        [SerializeField] private Button _makeRoomBtn, _joinIdBtn, _joinRandomBtn, _roomOutBtn;
        [SerializeField] private Text _roomName;
        [SerializeField] private MultiMember[] _members;

        private bool _inMulti;
        private bool _onLobbyTab;      // 거짓이면 친구 목록

        /// <summary>지금 로비 목록 탭인가. 목록을 채워 주는 쪽이 무엇을 넣을지 정할 때 쓴다.</summary>
        public bool OnLobbyTab => _onLobbyTab;

        /// <summary>친구를 방에 부른다 / 그 로비에 들어간다. 인자는 줄 번호다.</summary>
        public event Action<int> InviteFriend, EnterLobby;

        /// <summary>방 만들기 · 로비ID로 진입 · 랜덤 진입 · 방 나가기.</summary>
        public event Action MakeRoom, JoinById, JoinRandom, LeaveRoom;

        /// <summary>참가자의 달팽이를 자세히 본다. 인자는 줄 번호다.</summary>
        public event Action<int> ViewMember;

        /// <summary>탭이 바뀌었다. 받는 쪽이 그 목록을 넣어 준다.</summary>
        public event Action<bool> MultiTabChanged;

        private void BuildMultiList(RectTransform panel)
        {
            _multiRoot = NewRect("Multi", panel);
            Place(_multiRoot, Max.RowView);
            _multiRoot.gameObject.SetActive(false);

            _friendTab = TabChip(_multiRoot, UiTheme.Multi.FriendTab, Keys.FriendList, out _friendTabBg, "FriendTab");
            _lobbyTab  = TabChip(_multiRoot, UiTheme.Multi.LobbyTab,  Keys.LobbyList,  out _lobbyTabBg,  "LobbyTab");

            // 줄은 잘리는 영역 안에 넣는다. 안 그러면 6줄이 패널을 뚫고 나간다.
            BuildScrollView(_multiRoot, "MultiList", UiTheme.Multi.View, out var listRoot, out _multiContent);
            listRoot.gameObject.SetActive(true);      // 멀티 루트가 통째로 켜고 꺼진다

            _multiRows = new MultiRow[UiTheme.Multi.RowCount];
            for (int i = 0; i < _multiRows.Length; i++)
            {
                var r = UiTheme.Multi.Row;
                var at = new RectInt(r.x, r.y + i * UiTheme.Multi.RowStep, r.width, r.height);

                var root = NewRect("MultiRow" + i, _multiContent);
                Place(root, at);

                var bg = Backdrop(root.gameObject, UiSprites.Shape.Slot, UiTheme.Slot);
                var row = new MultiRow { Root = root };

                row.Name = Label(root, UiTheme.Multi.RowName, "", 9, UiTheme.Ink);
                row.Name.alignment = TextAnchor.MiddleLeft;

                row.Button = root.gameObject.AddComponent<Button>();
                row.Button.targetGraphic = bg;

                // 그림은 탭에 따라 갈아 끼운다 (친구=초대, 로비=입장)
                row.Action = IconButton(root, UiTheme.Multi.RowButton, "btn_enter", "Action", tint: Color.white);

                _multiRows[i] = row;
            }
        }

        /// <summary>탭 하나. 고르면 밝아지고 아니면 죽는다 (옷장 필터와 같은 표시).</summary>
        private Button TabChip(RectTransform parent, RectInt at, string token, out Image box, string name)
        {
            var root = NewRect(name, parent);
            Place(root, at);

            box = Backdrop(root.gameObject, UiSprites.Shape.LevelBadge, UiTheme.Slot);
            LocLabel(root, new RectInt(0, 0, at.width, at.height), token, 9, UiTheme.Ink);

            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = box;
            return btn;
        }

        private void BuildMultiPanel()
        {
            _multiPanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _multiPanel.gameObject.SetActive(false);

            // ── 방을 고르기 전 ──
            _lobbyGroup = Fill(NewRect("LobbyGroup", _multiPanel));
            LocLabel(_lobbyGroup, UiTheme.Multi.Title, Keys.Room, 12, UiTheme.Ink);

            _makeRoomBtn   = TextButton(_lobbyGroup, UiTheme.Multi.Button, Keys.MakeRoom, "MakeRoom");
            _joinIdBtn     = TextButton(_lobbyGroup, Offset(UiTheme.Multi.Button, UiTheme.Multi.ButtonStep),
                                        Keys.JoinById, "JoinById");
            _joinRandomBtn = TextButton(_lobbyGroup, Offset(UiTheme.Multi.Button, UiTheme.Multi.ButtonStep * 2),
                                        Keys.JoinRandom, "JoinRandom");

            // ── 방에 들어간 뒤 ──
            _roomGroup = Fill(NewRect("RoomGroup", _multiPanel));
            _roomGroup.gameObject.SetActive(false);

            Box(_roomGroup, UiTheme.Multi.RoomName, UiTheme.Slot, UiSprites.Shape.Name, "RoomNameBox");
            _roomName = Label(_roomGroup, UiTheme.Multi.RoomName, "", 10, UiTheme.Ink);
            _roomOutBtn = IconButton(_roomGroup, UiTheme.Multi.RoomOut, "btn_out", "RoomOut", tint: Color.white);

            _members = new MultiMember[UiTheme.Multi.MemberCount];
            for (int i = 0; i < _members.Length; i++)
            {
                var m = UiTheme.Multi.Member;
                var at = new RectInt(m.x, m.y + i * UiTheme.Multi.MemberStep, m.width, m.height);

                var root = NewRect("Member" + i, _roomGroup);
                Place(root, at);
                Backdrop(root.gameObject, UiSprites.Shape.Slot, UiTheme.Slot);

                var member = new MultiMember { Root = root };
                member.Face = FaceView(root, UiTheme.Multi.MemberFace);
                member.Name = Label(root, UiTheme.Multi.MemberName, "", 9, UiTheme.Ink);
                member.Name.alignment = TextAnchor.MiddleLeft;
                member.Zoom = IconButton(root, UiTheme.Multi.MemberZoom, "icon_detail", "Zoom");

                root.gameObject.SetActive(false);
                _members[i] = member;
            }
        }

        /// <summary>
        /// 멀티플레이어로 가거나 나온다. 다섯째 탭이라 탭을 옮기는 것이 전부다 —
        /// 켜고 끄는 일은 <see cref="SetTab"/> 이 다른 탭과 똑같이 처리한다.
        /// </summary>
        public void OpenMulti(bool on) => SetTab(on ? 4 : 0);

        private void SetMultiTab(bool lobby)
        {
            _onLobbyTab = lobby;
            PaintMultiTabs();
            MultiTabChanged?.Invoke(lobby);
        }

        private void PaintMultiTabs()
        {
            if (_friendTabBg != null) _friendTabBg.color = _onLobbyTab ? UiTheme.Faded : Color.white;
            if (_lobbyTabBg != null)  _lobbyTabBg.color  = _onLobbyTab ? Color.white : UiTheme.Faded;

            // 줄 오른쪽 버튼은 탭에 따라 하는 일이 다르다. 그림도 같이 바꾼다.
            for (int i = 0; i < Count(_multiRows); i++)
                SetGlyph(_multiRows[i]?.Action, _onLobbyTab ? "btn_enter" : "icon_swap");
        }

        /// <summary>목록을 채운다. 친구든 로비든 이름 하나짜리 줄이라 같은 것을 쓴다.</summary>
        public void SetMultiRows(string[] names)
        {
            int count = names?.Length ?? 0;
            for (int i = 0; i < Count(_multiRows); i++)
            {
                bool has = i < count;
                _multiRows[i].Root.gameObject.SetActive(has);
                if (has) _multiRows[i].Name.text = names[i];
            }

            // 내용 높이가 스크롤 범위를 정한다.
            //
            // 줄 수가 아니라 <b>실제로 만들어 둔 줄 수</b>로 잡아야 한다. 이름이 20개를 넘어도
            // 줄은 그만큼만 있으므로, 이름 수로 잡으면 줄이 없는 빈 곳까지 굴러가 목록이
            // 통째로 사라진 것처럼 보인다.
            if (_multiContent != null)
            {
                int shown = Mathf.Min(count, Count(_multiRows));
                _multiContent.sizeDelta = new Vector2(UiTheme.PanelW,
                    Mathf.Max(UiTheme.Multi.View.height,
                              UiTheme.Multi.Row.y + shown * UiTheme.Multi.RowStep));
            }
        }

        /// <summary>방에 들어갔는지. 오른쪽이 「방」 버튼과 참가자 목록 사이를 오간다.</summary>
        public void SetRoom(bool inRoom, string name)
        {
            if (_lobbyGroup != null) _lobbyGroup.gameObject.SetActive(!inRoom);
            if (_roomGroup != null)  _roomGroup.gameObject.SetActive(inRoom);
            if (_roomName != null)   _roomName.text = name ?? "";
        }

        /// <summary>방에 있는 달팽이들. (이름, 그림) 이고 최대 5.</summary>
        public void SetMembers((string name, Texture face)[] members)
        {
            int count = members?.Length ?? 0;
            for (int i = 0; i < Count(_members); i++)
            {
                bool has = i < count;
                _members[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                _members[i].Name.text = members[i].name;
                _members[i].Face.texture = members[i].face;
                _members[i].Face.enabled = members[i].face != null;
            }
        }

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
                _inSettings = false;
                _inGuide = false;
                if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
                if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);
                if (_settingsRoot != null)  _settingsRoot.gameObject.SetActive(false);
                if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
                if (_guideRoot != null)     _guideRoot.gameObject.SetActive(false);
                if (_guidePanel != null)    _guidePanel.gameObject.SetActive(false);
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

            // 상세보기에 있는 동안에는 나머지 화면이 전부 물러난다
            HideScreens();
            _geneRoot.gameObject.SetActive(true);
            _genePanel.gameObject.SetActive(true);

            _listTitle.text = SnailPet.Data.Loc.Text(Keys.Traits);
            RefreshClose();
            RefreshBackButton();
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

        // ── 달팽이 도감 ──
        //
        // 옷장·상세보기와 같은 모드다. 하단 액션의 도감 버튼으로 들어가고 뒤로가기로 나온다.
        // 채운 칸은 그때의 모습을 비추고, 안 채운 칸은 공용 실루엣(snail_shape)이 나온다.

        [Serializable]
        public sealed class GuideRow
        {
            public RectTransform Root;
            public Text Name;
            public Image RarityBadge, RarityIcon, Done;
            public Text Rarity;
            public Button Button;
        }

        [SerializeField] private RectTransform _guideRoot, _guidePanel, _guideBasic, _guideDetail;
        [SerializeField] private GuideRow[] _guideRows, _guideParts;
        [SerializeField] private GridSlot[] _guideRewards;
        [SerializeField] private Button _guideToggle;

        /// <summary>파츠 목록을 보고 있는가. 전환 버튼이 오간다.</summary>
        private bool _guideParted;
        [SerializeField] private Text _guideTitle, _guideInfo, _guideRarityText;
        [SerializeField] private Image _guideRarityBadge, _guideRarityIcon, _guideShape;
        [SerializeField] private RawImage _guideImage;

        private bool _inGuide;
        public bool InGuide => _inGuide;

        private int _guidePick;

        /// <summary>도감 칸을 골랐다. 몇 번째 줄인지를 준다.</summary>
        public event Action<int> GuidePicked;

        private void BuildGuideList(RectTransform panel)
        {
            _guideRoot = NewRect("Guide", panel);
            Place(_guideRoot, new RectInt(0, 0, UiTheme.PanelW, UiTheme.PanelH));
            _guideRoot.gameObject.SetActive(false);

            _guideRows = new GuideRow[UiTheme.Guide.RowPool];
            for (int i = 0; i < _guideRows.Length; i++)
            {
                var g = UiTheme.Guide.Row;
                var at = new RectInt(g.x, g.y + i * UiTheme.Guide.RowStep, g.width, g.height);

                var root = NewRect("Guide" + i, _guideRoot);
                Place(root, at);

                var bg = Backdrop(root.gameObject, UiSprites.Shape.Slot, UiTheme.Slot);

                var row = new GuideRow { Root = root };
                row.Name = Label(root, UiTheme.Guide.RowName, "", 10, UiTheme.Ink);
                row.Name.alignment = TextAnchor.MiddleLeft;

                row.RarityBadge = Box(root, UiTheme.Guide.RowRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
                row.Rarity = Label(root, UiTheme.Guide.RowRarity, "", 8, UiTheme.OnBadge);
                Shrink(row.Rarity);
                row.RarityIcon = Icon(root, UiTheme.Guide.RowRarity, null, Color.white, "RarityIcon");
                row.RarityIcon.raycastTarget = false;
                BakeRarity(row.RarityIcon, row.RarityBadge, row.Rarity);

                // 채운 칸에 찍는 도장. 아트가 아직 없어 칸 도형으로 자리만 잡아 둔다 —
                // Ui/Icon 에 icon_complete 를 넣으면 그림으로 바뀐다.
                row.Done = Icon(root, UiTheme.Guide.RowDone, "icon_complete", Color.white, "Done");
                row.Done.raycastTarget = false;
                if (row.Done.sprite == null)
                {
                    row.Done.sprite = UiSprites.Of(UiSprites.Shape.Badge);
                    row.Done.color = UiTheme.Selected;
                    row.Done.enabled = true;
                }

                row.Button = root.gameObject.AddComponent<Button>();
                row.Button.targetGraphic = bg;

                root.gameObject.SetActive(false);
                _guideRows[i] = row;
            }
        }

        private void BuildGuidePanel()
        {
            _guidePanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _guidePanel.gameObject.SetActive(false);

            Box(_guidePanel, UiTheme.Guide.Title, UiTheme.Slot, UiSprites.Shape.Name, "TitleBox");
            _guideTitle = Label(_guidePanel, UiTheme.Guide.Title, "", 11, UiTheme.Ink);

            // 안 채운 칸에 나오는 공용 실루엣. 채웠으면 그 위에 그 달팽이를 비춘다.
            _guideShape = Icon(_guidePanel, UiTheme.Guide.Image, "snail_shape", Color.white, "Shape");
            _guideShape.raycastTarget = false;

            // 채운 달팽이는 메인 상세의 초상과 같은 자리·크기다. 실루엣만 목업 크기를 쓴다.
            var view = NewRect("Snail", _guidePanel);
            Place(view, At.Portrait);
            _guideImage = view.gameObject.AddComponent<RawImage>();
            _guideImage.raycastTarget = false;
            _guideImage.enabled = false;

            // 설명과 보상은 한 묶음, 파츠 목록은 다른 묶음. 같은 자리를 나눠 쓰고 전환 버튼으로 바꾼다.
            _guideBasic = Fill(NewRect("Basic", _guidePanel));
            _guideDetail = Fill(NewRect("Detail", _guidePanel));

            // 설명 뒤에 까는 홈. 설명과 함께 나왔다 들어가야 하므로 같은 묶음에 넣는다.
            Box(_guideBasic, UiTheme.Guide.InfoBox, UiTheme.Slot, UiSprites.Shape.Slot, "InfoBox");

            _guideInfo = Label(_guideBasic, UiTheme.Guide.Info, "", 8, UiTheme.Ink);
            _guideInfo.horizontalOverflow = HorizontalWrapMode.Wrap;

            _guideRewards = new GridSlot[UiTheme.Guide.RewardCount];
            for (int i = 0; i < _guideRewards.Length; i++)
            {
                var r = UiTheme.Guide.Reward;
                var at = new RectInt(r.x + i * UiTheme.Guide.RewardStep, r.y, r.width, r.height);

                var root = NewRect("Reward" + i, _guideBasic);
                Place(root, at);
                Backdrop(root.gameObject, UiSprites.Shape.Slot2, UiTheme.RowSlot);

                var slot = new GridSlot { Root = root };
                slot.Icon = Icon(root, new RectInt(2, 2, at.width - 4, at.height - 4), null, Color.white, "Icon");
                slot.Icon.raycastTarget = false;

                slot.CountBg = CountBadge(root, Max.FoodCountBadge);
                slot.Count = Label(root, Max.FoodCountBadge, "", 9, UiTheme.OnBadge);
                PlaceRight((RectTransform)slot.Count.transform, Max.FoodCountBadge);

                root.gameObject.SetActive(false);
                _guideRewards[i] = slot;
            }

            _guideParts = new GuideRow[UiTheme.Guide.PartCount];
            for (int i = 0; i < _guideParts.Length; i++)
            {
                int dy = i * UiTheme.Guide.PartStep;
                var row = new GuideRow();

                Box(_guideDetail, Offset(UiTheme.Guide.PartRow, dy), UiTheme.Slot, UiSprites.Shape.Slot, "PartBar" + i);

                row.Done = Icon(_guideDetail, Offset(UiTheme.Guide.PartIcon, dy), null, Color.white, "PartIcon" + i);
                row.Done.raycastTarget = false;

                var rarityAt = Offset(UiTheme.Guide.PartRarity, dy);
                row.RarityBadge = Box(_guideDetail, rarityAt, UiTheme.BadgeDark, UiSprites.Shape.Badge, "PartRarity" + i);
                row.Rarity = Label(_guideDetail, rarityAt, "", 7, UiTheme.OnBadge);
                Shrink(row.Rarity);
                row.RarityIcon = Icon(_guideDetail, rarityAt, null, Color.white, "PartRarityIcon" + i);
                row.RarityIcon.raycastTarget = false;
                BakeRarity(row.RarityIcon, row.RarityBadge, row.Rarity);

                row.Name = Label(_guideDetail, Offset(UiTheme.Guide.PartName, dy), "", 8, UiTheme.Ink);
                row.Name.alignment = TextAnchor.MiddleLeft;

                _guideParts[i] = row;
            }

            // 전환 버튼. 기본에서는 돋보기, 파츠 목록에서는 도감 그림이 된다.
            _guideToggle = IconButton(_guidePanel, UiTheme.Guide.Toggle, "icon_detail", "GuideToggle");

            _guideRarityBadge = Box(_guidePanel, UiTheme.Guide.Rarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _guideRarityText  = Label(_guidePanel, UiTheme.Guide.Rarity, "", 9, UiTheme.OnBadge);
            Shrink(_guideRarityText);
            _guideRarityIcon = Icon(_guidePanel, UiTheme.Guide.Rarity, null, Color.white, "RarityIcon");
            _guideRarityIcon.raycastTarget = false;
            BakeRarity(_guideRarityIcon, _guideRarityBadge, _guideRarityText);

            // 완료 도장은 이름 위에 겹친다. 맨 나중에 지어야 이름칸에 안 가린다.
            _guideDone = Icon(_guidePanel, UiTheme.Guide.Done, "icon_complete2", Color.white, "Done");
            _guideDone.raycastTarget = false;
            _guideDone.enabled = false;
        }

        [SerializeField] private Image _guideDone;

        /// <summary>도감에 들어가거나 나온다.</summary>
        public void OpenGuide(bool on)
        {
            _inGuide = on;
            if (on)
            {
                _inWardrobe = false;
                _inGene = false;
                _inSettings = false;
                if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
                if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);
                if (_geneRoot != null)      _geneRoot.gameObject.SetActive(false);
                if (_genePanel != null)     _genePanel.gameObject.SetActive(false);
                if (_settingsRoot != null)  _settingsRoot.gameObject.SetActive(false);
                if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
                SetMaximized(true);
            }
            ApplyGuide();
        }

        private void ApplyGuide()
        {
            if (_guideRoot == null) return;

            _guideRoot.gameObject.SetActive(_inGuide);
            _guidePanel.gameObject.SetActive(_inGuide);

            if (!_inGuide) { SetTab(_tab); return; }

            HideScreens();
            _guideRoot.gameObject.SetActive(true);
            _guidePanel.gameObject.SetActive(true);

            _listTitle.text = SnailPet.Data.Loc.Text(Keys.Guide);
            RefreshClose();
            RefreshBackButton();
            ApplyGuideDetail();
        }

        /// <summary>
        /// 도감 목록을 채운다. 줄마다 (이름, 등급, 채웠는지).
        /// 번호는 목업대로 「1. 이름」 꼴로 앞에 붙인다.
        /// </summary>
        public void SetGuides((string name, SnailPet.Data.RarityType rarity, bool done)[] rows)
        {
            int count = rows?.Length ?? 0;

            for (int i = 0; i < Count(_guideRows); i++)
            {
                bool has = i < count;
                _guideRows[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                var r = rows[i];
                // 이름에 번호가 이미 들어 있다(「1. 아이스크림 달팽이」). 앞에 또 붙이지 않는다.
                _guideRows[i].Name.text = r.name;
                ApplyRarity(_guideRows[i].RarityIcon, _guideRows[i].RarityBadge, _guideRows[i].Rarity, r.rarity);
                if (_guideRows[i].Done != null) _guideRows[i].Done.enabled = r.done;
            }

            if (_guidePick >= count) _guidePick = 0;
        }

        /// <summary>
        /// 고른 칸의 상세를 채운다. <paramref name="look"/> 이 null 이면 아직 안 채운 칸이라
        /// 공용 실루엣이 나온다.
        /// </summary>
        /// <param name="done">채운 칸인가. 이름 위에 완료 도장이 찍힌다.</param>
        public void SetGuideDetail(string name, string info, SnailPet.Data.RarityType rarity,
                                   Texture look, bool done)
        {
            _guideTitle.text = name;
            _guideInfo.text = info;
            ApplyRarity(_guideRarityIcon, _guideRarityBadge, _guideRarityText, rarity);

            _guideImage.texture = look;
            _guideImage.enabled = look != null;
            if (_guideShape != null) _guideShape.enabled = look == null && _guideShape.sprite != null;
            if (_guideDone != null)  _guideDone.enabled  = done && _guideDone.sprite != null;
        }

        /// <summary>같은 모양을 y 만 내려 다시 쓴다. 줄이 여러 개인 곳에서.</summary>
        private static RectInt Offset(RectInt at, int dy) =>
            new RectInt(at.x, at.y + dy, at.width, at.height);

        /// <summary>
        /// 고른 칸의 파츠 목록. (그림, 등급, 이름) 순이며 비어 있는 칸은 감춘다.
        /// 도감이 요구하는 파츠를 적는다 — 채웠든 안 채웠든 그 칸의 정의는 같다.
        /// </summary>
        public void SetGuideParts((SnailPet.Data.PartsType type, SnailPet.Data.RarityType rarity, string name)[] parts)
        {
            int count = parts?.Length ?? 0;

            for (int i = 0; i < Count(_guideParts); i++)
            {
                var row = _guideParts[i];
                bool has = i < count;

                if (row.Name != null) row.Name.text = has ? parts[i].name : "";

                // 부위 아이콘은 상세보기(유전정보)와 같은 길을 쓴다 — EnumData 의 아이콘 키다.
                ApplyPartIcon(row.Done, has ? parts[i].type : (SnailPet.Data.PartsType?)null);

                if (has) ApplyRarity(row.RarityIcon, row.RarityBadge, row.Rarity, parts[i].rarity);
                else
                {
                    if (row.RarityIcon != null)  row.RarityIcon.enabled = false;
                    if (row.RarityBadge != null) row.RarityBadge.enabled = false;
                    if (row.Rarity != null)      row.Rarity.text = "";
                }
            }
        }

        /// <summary>고른 칸의 보상. 비어 있는 칸은 감추고, 남은 것을 가운데로 모은다.</summary>
        public void SetGuideRewards((Sprite icon, int count)[] rewards)
        {
            FillRewardSlots(_guideRewards, rewards, UiTheme.PanelW, UiTheme.Guide.Reward.y);
        }

        /// <summary>
        /// 보상 칸을 채우고 개수에 맞춰 가운데로 모은다.
        ///
        /// 한 개만 주는 칸이 왼쪽에 치우쳐 있으면 빈자리가 커 보인다. 목업도 개수마다
        /// 가운데에 모여 있다.
        /// </summary>
        private void FillRewardSlots(GridSlot[] slots, (Sprite icon, int count)[] rewards, int areaWidth, int y)
        {
            int count = Mathf.Min(rewards?.Length ?? 0, Count(slots));

            var one = UiTheme.Guide.Reward;
            int step = UiTheme.Guide.RewardStep;
            int total = count > 0 ? (count - 1) * step + one.width : 0;
            int left = (areaWidth - total) / 2;

            for (int i = 0; i < Count(slots); i++)
            {
                bool has = i < count;
                slots[i].Root.gameObject.SetActive(has);
                if (!has) continue;

                Place(slots[i].Root, new RectInt(left + i * step, y, one.width, one.height));

                slots[i].Icon.sprite = rewards[i].icon;
                slots[i].Icon.enabled = rewards[i].icon != null;
                SetSlotCount(slots[i], rewards[i].count > 1 ? rewards[i].count.ToString() : "");
            }
        }

        /// <summary>설명·보상과 파츠 목록을 오간다. 버튼 그림도 같이 바뀐다.</summary>
        private void ToggleGuideDetail()
        {
            _guideParted = !_guideParted;
            ApplyGuideDetail();
        }

        private void ApplyGuideDetail()
        {
            if (_guideBasic != null)  _guideBasic.gameObject.SetActive(!_guideParted);
            if (_guideDetail != null) _guideDetail.gameObject.SetActive(_guideParted);

            // 지금 무엇을 보고 있는지가 아니라 「누르면 어디로 가는지」를 그린다
            SetGlyph(_guideToggle, _guideParted ? "icon_book" : "icon_detail");
        }

        /// <summary>도감 그림을 찍을 크기.</summary>
        public static Vector2Int GuideImageSize => PortraitSize;

        private void PickGuide(int index)
        {
            if (index < 0 || index >= Count(_guideRows)) return;

            _guidePick = index;
            GuidePicked?.Invoke(index);
        }

        // ── 설정 화면 ──
        //
        // 옷장·상세보기와 같은 모드다. 설정 버튼으로 들어가고 탭을 누르면 나온다.
        // 왼쪽은 이 달팽이에 걸리는 설정, 오른쪽은 게임 전체 설정이다 (UI.pptx 13쪽).
        //
        // 체크와 ▾ 는 아트가 없어 글자로 그린다 — 수량 조절의 +/- 와 같은 임시 방편이라,
        // 아트가 들어오면 Icon 으로 바꾸면 된다.

        // 설정값 자체는 PlayerOptions 가 들고 있다. 세이브 형식이 화면 코드에 매이지 않게
        // 상태 쪽에 두었고, 여기서는 그리는 일과 바뀌었다고 알리는 일만 한다.

        [SerializeField] private RectTransform _settingsRoot, _settingsPanel;
        [SerializeField] private Button _noEggsBtn, _hungryBtn, _careBtn, _coinBtn;
        [SerializeField] private Button _langBtn, _updateBtn, _scaleBtn, _alwaysMaxBtn, _quitBtn;
        [SerializeField] private Text _noEggsMark, _hungryMark, _careMark, _coinMark, _alwaysMaxMark;
        [SerializeField] private Text _scaleLabel;

        private bool _inSettings;
        public bool InSettings => _inSettings;

        private Options _options = Options.Default;
        public Options CurrentOptions => _options;

        /// <summary>설정이 바뀌었다. 저장과 적용은 받는 쪽이 한다.</summary>
        public event Action<Options> OptionsChanged;

        /// <summary>「업데이트 및 재시작」. 아직 업데이트 체계가 없어 알리기만 한다.</summary>
        public event Action UpdatePressed;

        /// <summary>「종료」. 목업대로 묻지 않고 바로 끈다.</summary>
        public event Action QuitPressed;

        private void BuildSettingsList(RectTransform panel)
        {
            _settingsRoot = NewRect("Settings", panel);
            Place(_settingsRoot, new RectInt(0, 0, UiTheme.PanelW, UiTheme.PanelH));
            _settingsRoot.gameObject.SetActive(false);

            LocLabel(_settingsRoot, Set.EggTitle, Keys.EggSection, 10, UiTheme.Ink)
                .alignment = TextAnchor.MiddleLeft;
            LocLabel(_settingsRoot, Set.BubbleTitle, Keys.BubbleSection, 10, UiTheme.Ink)
                .alignment = TextAnchor.MiddleLeft;

            _noEggsBtn = ToggleRow(_settingsRoot, Set.LeftX, Set.LeftRows[0], Keys.NoEggs,       out _noEggsMark, "NoEggs");
            _hungryBtn = ToggleRow(_settingsRoot, Set.LeftX, Set.LeftRows[1], Keys.HungryBubble, out _hungryMark, "Hungry");
            _careBtn   = ToggleRow(_settingsRoot, Set.LeftX, Set.LeftRows[2], Keys.CareBubble,   out _careMark,   "Care");
            _coinBtn   = ToggleRow(_settingsRoot, Set.LeftX, Set.LeftRows[3], Keys.CoinBubble,   out _coinMark,   "Coin");
        }

        private void BuildSettingsPanel()
        {
            _settingsPanel = Panel(_detailRoot, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));
            _settingsPanel.gameObject.SetActive(false);

            LocLabel(_settingsPanel, new RectInt(0, 4, UiTheme.PanelW, 21), Keys.Setting, 12, UiTheme.Ink);

            // 언어는 지금 한글뿐이라 눌러도 바뀌지 않는다. 목업의 「나중에 추가」가 이것이다.
            _langBtn = SettingRow(_settingsPanel, Set.RightX, Set.RightRows[0], Keys.Korean, out _, "Language");
            Chevron(_settingsPanel, Set.RightX, Set.RightRows[0]);

            _updateBtn = SettingRow(_settingsPanel, Set.RightX, Set.RightRows[1], Keys.Update, out _, "Update");

            // UI 크기는 배수가 글자에 들어가므로 언어 키가 아니라 코드가 채운다.
            _scaleBtn = SettingRow(_settingsPanel, Set.RightX, Set.RightRows[2], null, out _scaleLabel, "UiScale");
            Chevron(_settingsPanel, Set.RightX, Set.RightRows[2]);

            _alwaysMaxBtn = ToggleRow(_settingsPanel, Set.RightX, Set.RightRows[3], Keys.AlwaysMax,
                                      out _alwaysMaxMark, "AlwaysMax");

            _quitBtn = SettingRow(_settingsPanel, Set.RightX, Set.RightRows[4], Keys.Quit, out _, "Quit", centered: true);
        }

        /// <summary>
        /// 설정 행 하나. 가로로 긴 홈에 글자를 얹고 행 전체를 누를 수 있게 한다.
        /// <paramref name="token"/> 이 null 이면 값이 바뀌는 글자라 코드가 채운다.
        /// </summary>
        private Button SettingRow(RectTransform parent, int x, int y, string token,
                                  out Text label, string name, bool centered = false)
        {
            var at = new RectInt(x, y, Set.RowW, Set.RowH);
            var box = Box(parent, at, UiTheme.Slot, UiSprites.Shape.Slot, name);
            box.raycastTarget = true;

            var lr = centered
                   ? new RectInt(at.x, at.y + Set.Label.y, at.width, Set.Label.height)
                   : new RectInt(at.x + Set.Label.x, at.y + Set.Label.y, Set.Label.width, Set.Label.height);

            label = token == null ? Label(parent, lr, "", 10, UiTheme.Ink)
                                  : LocLabel(parent, lr, token, 10, UiTheme.Ink);
            label.alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;

            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box;
            return btn;
        }

        /// <summary>켜고 끄는 행. 오른쪽 끝에 네모와 체크를 얹는다.</summary>
        private Button ToggleRow(RectTransform parent, int x, int y, string token, out Text mark, string name)
        {
            var btn = SettingRow(parent, x, y, token, out _, name);

            Box(parent, new RectInt(x + Set.Check.x, y + Set.Check.y, Set.Check.width, Set.Check.height),
                UiTheme.RowSlot, UiSprites.Shape.Slot2, name + "Box");

            mark = Label(parent, new RectInt(x + Set.Check.x, y + Set.Check.y - 1, Set.Check.width, Set.Check.height),
                         "✓", 11, UiTheme.Ink);
            return btn;
        }

        /// <summary>고르는 행 오른쪽의 ▾.</summary>
        private void Chevron(RectTransform parent, int x, int y) =>
            Label(parent, new RectInt(x + Set.Arrow.x, y + Set.Arrow.y, Set.Arrow.width, Set.Arrow.height),
                  "▾", 10, UiTheme.Ink);

        /// <summary>설정 화면에 들어가거나 나온다.</summary>
        public void OpenSettings(bool on)
        {
            _inSettings = on;
            if (on)
            {
                // 옷장·상세보기와 같은 자리를 쓰므로 하나만 떠 있어야 한다.
                _inWardrobe = false;
                _inGene = false;
                _inGuide = false;
                if (_wardrobeRoot != null)  _wardrobeRoot.gameObject.SetActive(false);
                if (_wardrobePanel != null) _wardrobePanel.gameObject.SetActive(false);
                if (_geneRoot != null)      _geneRoot.gameObject.SetActive(false);
                if (_genePanel != null)     _genePanel.gameObject.SetActive(false);
                if (_guideRoot != null)     _guideRoot.gameObject.SetActive(false);
                if (_guidePanel != null)    _guidePanel.gameObject.SetActive(false);
                SetMaximized(true);
            }
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (_settingsRoot == null) return;

            _settingsRoot.gameObject.SetActive(_inSettings);
            _settingsPanel.gameObject.SetActive(_inSettings);

            if (!_inSettings) { SetTab(_tab); return; }

            // 설정에 있는 동안에는 나머지 화면이 전부 물러난다
            HideScreens();
            _settingsRoot.gameObject.SetActive(true);
            _settingsPanel.gameObject.SetActive(true);

            _listTitle.text = SnailPet.Data.Loc.Text(Keys.SnailSetting);
            PaintSettings();
            RefreshClose();
        }

        /// <summary>지금 값에 맞춰 체크와 글자를 다시 그린다.</summary>
        private void PaintSettings()
        {
            if (_noEggsMark != null)    _noEggsMark.enabled    = _options.NoEggs;
            if (_hungryMark != null)    _hungryMark.enabled    = _options.HungryBubble;
            if (_careMark != null)      _careMark.enabled      = _options.CareBubble;
            if (_coinMark != null)      _coinMark.enabled      = _options.CoinBubble;
            if (_alwaysMaxMark != null) _alwaysMaxMark.enabled = _options.AlwaysMax;

            if (_scaleLabel != null)
                _scaleLabel.text = SnailPet.Data.Loc.Format(Keys.UiScale, _options.Scale.ToString("0.#"));
        }

        /// <summary>
        /// 세이브에서 읽은 값을 넣는다. 이쪽은 <see cref="OptionsChanged"/> 를 내지 않는다 —
        /// 넣자마자 되돌아와 저장이 한 번 더 도는 것을 막는다.
        /// </summary>
        public void SetOptions(Options options)
        {
            _options = options;
            PaintSettings();
            ApplyUiOptions();
        }

        private void ChangeOptions()
        {
            PaintSettings();
            ApplyUiOptions();
            OptionsChanged?.Invoke(_options);
        }

        /// <summary>
        /// UI 가 스스로 거는 값 둘. 나머지는 게임 쪽이 받아서 건다.
        ///
        /// 크기는 <b>캔버스 스케일러</b>로 건다. 위젯의 localScale 로 늘리면 이미 구워진 글자를
        /// 확대하는 셈이라 글자가 뭉개진다 — 동적 폰트는 canvas.scaleFactor 를 보고 그 배율로
        /// 글자를 다시 구우므로 이쪽이라야 x2 에서도 선명하다. 9-슬라이스 도형도 같이 따라온다.
        ///
        /// 「항상 최대화」는 펼치고, 접는 X 를 잠근다. 잠그는 것은 <b>위젯을 접는 X 하나뿐</b>이고
        /// 상점의 뒤로가기와 팝업의 X 는 그대로 둔다 — 그 둘은 접는 버튼이 아니다.
        /// </summary>
        private void ApplyUiOptions()
        {
            if (_scaler == null) _scaler = GetComponent<CanvasScaler>();
            if (_scaler != null) _scaler.scaleFactor = _options.Scale;

            // 예전 방식으로 늘어난 채 살아났을 수 있으니 되돌려 둔다. 둘이 겹치면 배로 커진다.
            if (_widget != null) _widget.localScale = Vector3.one;

            if (_options.AlwaysMax && !Maximized) SetMaximized(true);
            RefreshClose();
        }

        /// <summary>
        /// 달팽이 정보 화면에서 들어가 좌우 패널을 통째로 쓰는 화면들. X 가 여기서는
        /// 접는 버튼이 아니라 되돌아가는 버튼이 된다.
        /// </summary>
        private bool InOverlay => _inSettings || _inWardrobe || _inGene || _inGuide;

        /// <summary>
        /// X 를 잠글지 정한다. 「항상 최대화」가 막는 것은 <b>접는 동작</b>뿐이고,
        /// X 에는 「달팽이 화면으로 돌아가기」가 함께 붙어 있다. 그래서 잠그는 때는
        /// 그 둘이 모두 할 일이 없을 때 — 이미 달팽이 화면에 있는데 접지도 못할 때뿐이다.
        /// </summary>
        private void RefreshClose()
        {
            if (_closeBtn == null) return;

            _closeBtn.interactable = !(_options.AlwaysMax && !InOverlay && _tab == 0);
            SetGlyph(_closeBtn, ShrinksOnClose ? "btn_minize" : "btn_close");

            // 설정도 화면을 고르는 버튼이라 탭처럼 「고른 그림」이 따로 있다.
            // 여기서 갈아 끼우는 이유는 이 함수가 화면이 바뀔 때마다 도는 유일한 곳이기 때문이다
            // (탭 이동·설정 진입/이탈·옷장·유전·도감이 전부 지나간다).
            SetGlyph(_settingsBtn, _inSettings ? "icon_settings_on" : "icon_settings");
        }

        /// <summary>
        /// X 가 최소화 버튼인가.
        ///
        /// 달팽이 정보 화면이면 <b>목록이 펼쳐져 있든 아니든</b> 최소화 버튼이다. 나머지
        /// (음식·알·상점 탭, 옷장·유전·도감·설정)에서는 X 그대로이고 하는 일도 그대로다.
        ///
        /// 처음에는 접혀 있을 때만 최소화로 뒀는데, 다른 탭에 갔다가 달팽이 탭으로 돌아오면
        /// 목록이 펼쳐진 채라 X 가 그대로 남아 있었다. 「화면」이 기준이지 크기가 기준이 아니다.
        /// </summary>
        private bool ShrinksOnClose => !InOverlay && _tab == 0;

        [SerializeField] private CanvasScaler _scaler;

        // ── 구매·판매 팝업 ──
        //
        // 목업에서 구매와 판매는 제목과 가격 부호만 다르므로 하나로 만든다.
        // 위젯 안이 아니라 화면 한가운데에 뜨고, 떠 있는 동안 뒤를 가린다.

        [SerializeField] private RectTransform _popup, _popupBlocker;
        [SerializeField] private Text _popupTitle, _popupCount, _popupCost;
        [SerializeField] private Button _popupMinus, _popupPlus, _popupYes, _popupNo, _popupClose;

        private int _popupQty = 1, _popupMax = 1;
        private double _popupUnit;      // 한 개당 값. 항상 양수이고 부호는 여기서 붙인다.
        private bool _popupSelling;
        private int _popupItemId;

        /// <summary>팝업에서 「네」를 눌렀다. (아이템 Id, 수량).</summary>
        public event Action<int, int> PopupConfirmed;

        // ── 팝업 뒤 어둡게 ──
        //
        // 반투명 검은 사각형을 덮으면 아트 경계와 어긋난다. 판 모서리는 둥글고 아이콘은
        // 제각각인데 덮개는 네모라, 아무리 맞춰도 남는 자리가 생긴다.
        //
        // 그래서 덮지 않고 <b>그리는 것들의 색을 직접 곱해</b> 어둡게 만든다. 아트가 비어
        // 있는 자리는 그대로 비어 있으므로 경계가 정확히 아트를 따른다. 덮개는 투명해지고
        // 클릭을 막는 일만 계속한다.

        /// <summary>얼마나 어두워지는지. 1 이면 그대로, 0 이면 새까맣다.</summary>
        private const float DimAmount = 0.55f;

        private readonly System.Collections.Generic.List<Graphic> _dimmed =
            new System.Collections.Generic.List<Graphic>();
        private readonly System.Collections.Generic.List<Color> _dimBase =
            new System.Collections.Generic.List<Color>();

        private void SetDim(bool on)
        {
            // 되돌리는 것이 먼저다. 겹쳐 걸면 곱이 쌓여 새까매진다.
            for (int i = 0; i < _dimmed.Count; i++)
                if (_dimmed[i] != null) _dimmed[i].color = _dimBase[i];

            _dimmed.Clear();
            _dimBase.Clear();

            if (!on || _widget == null) return;

            foreach (var g in _widget.GetComponentsInChildren<Graphic>(true))
            {
                // 팝업 자신은 밝아야 한다
                if (_popupBlocker != null && g.transform.IsChildOf(_popupBlocker)) continue;

                var c = g.color;
                _dimmed.Add(g);
                _dimBase.Add(c);
                g.color = new Color(c.r * DimAmount, c.g * DimAmount, c.b * DimAmount, c.a);
            }
        }

        /// <summary>팝업을 띄우는 세 곳이 공통으로 지나는 자리.</summary>
        private void OpenBlocker()
        {
            _popupBlocker.gameObject.SetActive(true);
            SetDim(true);
            StartPopupGrow();
        }

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
            // 덮개는 투명하다. 어둡게 하는 일은 SetDim 이 아트의 색으로 하고,
            // 이 판은 클릭을 막는 일만 한다 (알파가 0 이어도 레이캐스트는 걸린다).
            var shade = _popupBlocker.gameObject.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0f);
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
            BuildHatchGroup();
            BuildGuideDoneGroup();
            BuildRewardGroup();
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

            _renameTitle = LocLabel(_renameGroup, Pop.RenameTitle, Keys.AskRename, 12, UiTheme.Ink);

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
            _renameOkText = LocLabel(_renameGroup, Pop.RenameOk, Keys.DoRename, 10, UiTheme.Ink);
            _renameOk = okBox.gameObject.AddComponent<Button>();
            _renameOk.targetGraphic = okBox;
        }

        /// <summary>
        /// 이름 변경 팝업을 띄운다.
        /// 글자를 받아야 하므로 <b>여는 동안만</b> 창이 키보드 포커스를 빌린다.
        /// </summary>
        /// <summary>
        /// 로비 ID 를 받는다. 이름 변경 팝업을 그대로 쓰고 <b>글자만 갈아 끼운다</b> —
        /// 입력칸과 키보드 포커스 처리가 이미 그 팝업에 있어 그대로 얻어 간다.
        /// </summary>
        public void ShowLobbyId()
        {
            ShowRename("");
            _renameForLobby = true;      // ShowRename 이 이름 변경으로 돌려놓으므로 그 뒤에 세운다

            if (_renameTitle != null)  _renameTitle.text  = SnailPet.Data.Loc.Text(Keys.LobbyIdAsk);
            if (_renameOkText != null) _renameOkText.text = SnailPet.Data.Loc.Text(Keys.Confirm);
        }

        [SerializeField] private Text _renameTitle, _renameOkText;

        /// <summary>
        /// 이름 변경 팝업의 제목·확인 글자를 찾아 둔다.
        ///
        /// 프리팹에는 이 둘이 필드로 안 잡혀 있다 (글자를 바꿀 일이 없어서 그냥 지었었다).
        /// 로비ID 로 쓰면서 갈아 끼울 일이 생겼으므로 <b>자리로 가려낸다</b> — 제목은 위쪽,
        /// 확인은 버튼 자리에 있는 글자다. 다시 구우면 필드로 잡히고 이 함수는 지나간다.
        /// </summary>
        private void FindRenameTexts()
        {
            if (_renameGroup == null) return;

            foreach (var text in _renameGroup.GetComponentsInChildren<Text>(true))
            {
                var rt = (RectTransform)text.transform;
                if (_renameTitle == null && Mathf.Abs(rt.anchoredPosition.y + Pop.RenameTitle.y) < 2f)
                    _renameTitle = text;
                else if (_renameOkText == null && Mathf.Abs(rt.anchoredPosition.y + Pop.RenameOk.y) < 2f)
                    _renameOkText = text;
            }
        }

        /// <summary>로비 ID 를 넣고 확인을 눌렀다.</summary>
        public event Action<string> LobbyIdEntered;

        private bool _renameForLobby;

        public void ShowRename(string current)
        {
            _renameForLobby = false;

            // 로비ID 로 열었던 글자가 남아 있을 수 있다. 이름 변경 쪽으로 되돌린다.
            if (_renameTitle != null)  _renameTitle.text  = SnailPet.Data.Loc.Text(Keys.AskRename);
            if (_renameOkText != null) _renameOkText.text = SnailPet.Data.Loc.Text(Keys.DoRename);

            HidePopupGroups();
            _renameGroup.gameObject.SetActive(true);
            OpenBlocker();

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

        // ── 알 부화 팝업 ──
        //
        // 부화 칸을 눌러 받는 순간 뜬다. 알이 말랑거리며 부들거리다가 빛이 판을 덮고,
        // 그 사이에 알을 갓 태어난 달팽이로 갈아 끼운다. 목업의 좌·우 두 그림이 곧
        // 연출 중과 결과이며, 같은 판에서 가운데만 바뀐다.
        //
        // 연출을 AnimationClip 이 아니라 코드로 하는 이유: 클립은 Animator 로 프리팹에
        // 붙는데 이 UI 프리팹은 코드가 굽는 것이라 다시 구우면 사라진다(onClick 과 같은 함정).
        // 게다가 끝나는 시점을 게임 쪽이 알아야 해서 어차피 코드가 상태를 들고 있어야 한다.

        [SerializeField] private RectTransform _hatchGroup, _hatchLight;
        [SerializeField] private Image _hatchEgg, _hatchLightFill, _hatchRarityBadge, _hatchRarityIcon;
        [SerializeField] private RawImage _hatchSnail;
        [SerializeField] private Text _hatchRarityText;
        [SerializeField] private Button _hatchOk;

        private void BuildHatchGroup()
        {
            _hatchGroup = Fill(NewRect("HatchGroup", _popup));
            _hatchGroup.gameObject.SetActive(false);

            LocLabel(_hatchGroup, Pop.HatchTitle, Keys.Hatched, 12, UiTheme.Ink);

            // 알은 아래 가운데를 축으로 눌렸다 늘어난다. 떼는 연출이 발을 축으로 하는 것과
            // 같은 이유다 — 축이 바닥에 있어야 눌리는 것처럼 보인다.
            _hatchEgg = Icon(_hatchGroup, Pop.HatchEgg, null, Color.white, "Egg");
            _hatchEgg.raycastTarget = false;
            _hatchEgg.enabled = false;      // 스프라이트 없는 Image 는 흰 사각형이 된다. 띄울 때 채운다.
            var eggRt = (RectTransform)_hatchEgg.transform;
            eggRt.pivot = new Vector2(0.5f, 0f);
            eggRt.anchoredPosition = new Vector2(Pop.HatchEgg.x + Pop.HatchEgg.width * 0.5f,
                                                 -(Pop.HatchEgg.y + Pop.HatchEgg.height));

            var pv = NewRect("Snail", _hatchGroup);
            Place(pv, Pop.HatchSnail);
            _hatchSnail = pv.gameObject.AddComponent<RawImage>();
            _hatchSnail.raycastTarget = false;

            _hatchRarityBadge = Box(_hatchGroup, Pop.HatchRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _hatchRarityText  = Label(_hatchGroup, Pop.HatchRarity, "", 8, UiTheme.OnBadge);
            Shrink(_hatchRarityText);
            _hatchRarityIcon = Icon(_hatchGroup, Pop.HatchRarity, null, Color.white, "RarityIcon");
            _hatchRarityIcon.raycastTarget = false;
            BakeRarity(_hatchRarityIcon, _hatchRarityBadge, _hatchRarityText);

            _hatchOk = TextButton(_hatchGroup, Pop.HatchOk, Keys.Confirm, "Ok");

            // 빛은 판을 통째로 덮어 알에서 달팽이로 갈아 끼우는 순간을 가린다.
            //
            // 흰 사각형을 그대로 얹으면 판의 둥근 모서리 밖으로 삐져나온다. 그렇다고 판 아트를
            // 흰색으로 물들일 수도 없다 — Image.color 는 곱하기라 흰색을 곱하면 아트 그대로다.
            // 그래서 판 아트를 <b>마스크</b>로 쓰고 그 안에 흰 판을 깐다. 잘리는 모양이 아트의
            // 알파를 그대로 따르므로 판이 어떻게 생겼든 맞는다.
            _hatchLight = Fill(NewRect("Light", _hatchGroup));

            var shape = _hatchLight.gameObject.AddComponent<Image>();
            shape.sprite = UiSprites.Of(UiSprites.Shape.Panel);
            shape.type = Image.Type.Sliced;
            shape.raycastTarget = false;

            var mask = _hatchLight.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;      // 모양만 쓰고 아트 자체는 안 보인다

            var glow = Fill(NewRect("Glow", _hatchLight));
            _hatchLightFill = glow.gameObject.AddComponent<Image>();
            _hatchLightFill.color = new Color(1f, 1f, 1f, 0f);
            _hatchLightFill.raycastTarget = false;
        }

        // ── 도감 완성·보상 팝업 ──
        //
        // 도감이 채워지면 완성 팝업이 뜨고, 확인을 누르면 보상을 주면서 수령 팝업으로 넘어간다.
        // 둘 다 구매·부화 팝업과 같은 판을 쓰고 가운데만 갈아 끼운다. 완성 쪽만 판이 조금 크다.

        [SerializeField] private RectTransform _doneGroup, _rewardGroup;
        [SerializeField] private RawImage _doneImage;
        [SerializeField] private Image _doneRarityBadge, _doneRarityIcon;
        [SerializeField] private Text _doneRarityText, _doneName;
        [SerializeField] private Button _doneOk, _rewardOk;
        [SerializeField] private GridSlot[] _rewardSlots;

        /// <summary>완성 팝업에서 「확인」을 눌렀다. 보상은 받는 쪽이 준다.</summary>
        public event Action GuideDoneConfirmed;

        /// <summary>보상 수령 팝업을 닫았다.</summary>
        public event Action RewardClosed;

        private void BuildGuideDoneGroup()
        {
            _doneGroup = Fill(NewRect("GuideDoneGroup", _popup));
            _doneGroup.gameObject.SetActive(false);

            LocLabel(_doneGroup, Pop.HatchTitle, Keys.GuideDone, 12, UiTheme.Ink);

            var view = NewRect("Snail", _doneGroup);
            Place(view, Pop.HatchSnail);
            _doneImage = view.gameObject.AddComponent<RawImage>();
            _doneImage.raycastTarget = false;

            _doneRarityBadge = Box(_doneGroup, Pop.HatchRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "RarityBadge");
            _doneRarityText  = Label(_doneGroup, Pop.HatchRarity, "", 8, UiTheme.OnBadge);
            Shrink(_doneRarityText);
            _doneRarityIcon = Icon(_doneGroup, Pop.HatchRarity, null, Color.white, "RarityIcon");
            _doneRarityIcon.raycastTarget = false;
            BakeRarity(_doneRarityIcon, _doneRarityBadge, _doneRarityText);

            Box(_doneGroup, Pop.DoneName, UiTheme.Slot, UiSprites.Shape.Name, "NameBox");
            _doneName = Label(_doneGroup, Pop.DoneName, "", 11, UiTheme.Ink);

            _doneOk = TextButton(_doneGroup, Pop.DoneOk, Keys.Confirm, "DoneOk");
        }

        private void BuildRewardGroup()
        {
            _rewardGroup = Fill(NewRect("RewardGroup", _popup));
            _rewardGroup.gameObject.SetActive(false);

            LocLabel(_rewardGroup, Pop.HatchTitle, Keys.RewardGot, 12, UiTheme.Ink);

            _rewardSlots = new GridSlot[UiTheme.Guide.RewardCount];
            for (int i = 0; i < _rewardSlots.Length; i++)
            {
                var root = NewRect("Reward" + i, _rewardGroup);
                Place(root, Pop.RewardSlot);
                Backdrop(root.gameObject, UiSprites.Shape.Slot2, UiTheme.RowSlot);

                var slot = new GridSlot { Root = root };
                slot.Icon = Icon(root, new RectInt(2, 2, Pop.RewardSlot.width - 4, Pop.RewardSlot.height - 4),
                                 null, Color.white, "Icon");
                slot.Icon.raycastTarget = false;

                slot.CountBg = CountBadge(root, Max.FoodCountBadge);
                slot.Count = Label(root, Max.FoodCountBadge, "", 9, UiTheme.OnBadge);
                PlaceRight((RectTransform)slot.Count.transform, Max.FoodCountBadge);

                root.gameObject.SetActive(false);
                _rewardSlots[i] = slot;
            }

            _rewardOk = TextButton(_rewardGroup, Pop.HatchOk, Keys.Confirm, "RewardOk");
        }

        /// <summary>도감을 채웠다고 알린다. 확인을 누르면 <see cref="GuideDoneConfirmed"/> 가 나간다.</summary>
        public void ShowGuideDone(string name, SnailPet.Data.RarityType rarity, Texture look,
                                  (Sprite icon, int count)[] rewards)
        {
            HidePopupGroups();
            _doneGroup.gameObject.SetActive(true);

            // 이 팝업만 판이 조금 크다. 가운데 피벗이라 크기만 바꾸면 그대로 가운데에 남는다.
            _popup.sizeDelta = new Vector2(Pop.W, Pop.GuideDoneH);

            _doneName.text = name;
            _doneImage.texture = look;
            _doneImage.enabled = look != null;
            ApplyRarity(_doneRarityIcon, _doneRarityBadge, _doneRarityText, rarity);

            OpenBlocker();
        }

        /// <summary>받은 보상을 보여 준다.</summary>
        public void ShowRewards((Sprite icon, int count)[] rewards)
        {
            HidePopupGroups();
            _rewardGroup.gameObject.SetActive(true);
            _popup.sizeDelta = new Vector2(Pop.W, Pop.H);

            FillRewardSlots(_rewardSlots, rewards, Pop.W, Pop.RewardSlot.y);
            OpenBlocker();
        }

        // ── 남의 달팽이 한 장 ──
        //
        // 방 목록에서 돋보기를 누르면 뜬다. 팝업 판을 다른 묶음들과 나눠 쓰고,
        // 이 묶음만 판이 조금 크다 (초상 + 파츠 넉 줄이 들어간다).

        [SerializeField] private RectTransform _guestGroup;
        [SerializeField] private Text _guestSteam, _guestName, _guestRarityText;
        [SerializeField] private Image _guestRarityBadge, _guestRarityIcon;
        [SerializeField] private RawImage _guestFace;
        [SerializeField] private GuideRow[] _guestParts;

        private void BuildGuestGroup()
        {
            _guestGroup = Fill(NewRect("GuestGroup", _popup));
            _guestGroup.gameObject.SetActive(false);

            _guestSteam = Label(_guestGroup, Pop.GuestSteam, "", 12, UiTheme.Ink);

            Box(_guestGroup, Pop.GuestName, UiTheme.Slot, UiSprites.Shape.Name, "GuestNameBox");
            _guestName = Label(_guestGroup, Pop.GuestName, "", 10, UiTheme.Ink);

            // 초상 자리. 그림은 부트스트랩이 넘겨 준다.
            Box(_guestGroup, Pop.GuestFace, UiTheme.RowSlot, UiSprites.Shape.Slot2, "GuestFaceBox");
            var face = NewRect("GuestFace", _guestGroup);
            Place(face, Pop.GuestFace);
            _guestFace = face.gameObject.AddComponent<RawImage>();
            _guestFace.raycastTarget = false;
            _guestFace.enabled = false;

            _guestRarityBadge = Box(_guestGroup, Pop.GuestRarity, UiTheme.BadgeDark, UiSprites.Shape.Badge, "GuestRarity");
            _guestRarityText  = Label(_guestGroup, Pop.GuestRarity, "", 9, UiTheme.OnBadge);
            Shrink(_guestRarityText);
            _guestRarityIcon = Icon(_guestGroup, Pop.GuestRarity, null, Color.white, "GuestRarityIcon");
            _guestRarityIcon.raycastTarget = false;
            BakeRarity(_guestRarityIcon, _guestRarityBadge, _guestRarityText);

            // 파츠 줄. 유전정보·도감과 같은 모양이다 (아이콘 + 등급 + 이름).
            _guestParts = new GuideRow[Pop.GuestPartCount];
            for (int i = 0; i < _guestParts.Length; i++)
            {
                int dy = i * Pop.GuestPartStep;
                var row = new GuideRow();

                Box(_guestGroup, Offset(Pop.GuestPartRow, dy), UiTheme.Slot, UiSprites.Shape.Slot, "GuestPartBar" + i);

                row.Done = Icon(_guestGroup, Offset(Pop.GuestPartIcon, dy), null, Color.white, "GuestPartIcon" + i);
                row.Done.raycastTarget = false;

                var rarityAt = Offset(Pop.GuestPartRarity, dy);
                row.RarityBadge = Box(_guestGroup, rarityAt, UiTheme.BadgeDark, UiSprites.Shape.Badge, "GuestPartRarity" + i);
                row.Rarity = Label(_guestGroup, rarityAt, "", 7, UiTheme.OnBadge);
                Shrink(row.Rarity);
                row.RarityIcon = Icon(_guestGroup, rarityAt, null, Color.white, "GuestPartRarityIcon" + i);
                row.RarityIcon.raycastTarget = false;
                BakeRarity(row.RarityIcon, row.RarityBadge, row.Rarity);

                row.Name = Label(_guestGroup, Offset(Pop.GuestPartName, dy), "", 8, UiTheme.Ink);
                row.Name.alignment = TextAnchor.MiddleLeft;

                _guestParts[i] = row;
            }
        }

        /// <summary>
        /// 남의 달팽이 한 장을 띄운다. 닫기 X 로만 닫는다 — 볼 뿐이라 고를 것이 없다.
        /// </summary>
        public void ShowGuestCard(string steamName, string snailName, SnailPet.Data.RarityType rarity,
                                  Texture face,
                                  (SnailPet.Data.PartsType type, SnailPet.Data.RarityType rarity, string name)[] parts)
        {
            if (_guestGroup == null) return;

            HidePopupGroups();
            _guestGroup.gameObject.SetActive(true);

            // 이 묶음만 판이 크다. 가운데 피벗이라 크기만 바꾸면 그대로 가운데에 남는다.
            _popup.sizeDelta = new Vector2(Pop.W, Pop.GuestH);

            _guestSteam.text = steamName ?? "";
            _guestName.text = string.IsNullOrWhiteSpace(snailName)
                            ? SnailPet.Data.Loc.Text(Keys.NoName) : snailName;
            ApplyRarity(_guestRarityIcon, _guestRarityBadge, _guestRarityText, rarity);

            _guestFace.texture = face;
            _guestFace.enabled = face != null;

            int count = parts?.Length ?? 0;
            for (int i = 0; i < Count(_guestParts); i++)
            {
                var row = _guestParts[i];
                bool has = i < count;

                if (row.Name != null) row.Name.text = has ? parts[i].name : "";
                ApplyPartIcon(row.Done, has ? parts[i].type : (SnailPet.Data.PartsType?)null);

                if (has) ApplyRarity(row.RarityIcon, row.RarityBadge, row.Rarity, parts[i].rarity);
                else
                {
                    if (row.RarityIcon != null)  row.RarityIcon.enabled = false;
                    if (row.RarityBadge != null) row.RarityBadge.enabled = false;
                    if (row.Rarity != null)      row.Rarity.text = "";
                }
            }

            OpenBlocker();
        }

        /// <summary>팝업 묶음은 한 번에 하나만 보인다. 판 크기도 기본으로 되돌린다.</summary>
        private void HidePopupGroups()
        {
            if (_buyGroup != null)    _buyGroup.gameObject.SetActive(false);
            if (_renameGroup != null) _renameGroup.gameObject.SetActive(false);
            if (_doneGroup != null)   _doneGroup.gameObject.SetActive(false);
            if (_rewardGroup != null) _rewardGroup.gameObject.SetActive(false);
            if (_guestGroup != null)  _guestGroup.gameObject.SetActive(false);
            ResetHatch();

            if (_popup != null) _popup.sizeDelta = new Vector2(Pop.W, Pop.H);
        }

        private enum HatchPhase { None, Wobble, Flash, Result }

        private HatchPhase _hatchPhase;
        private float _hatchTime;

        /// <summary>부들거리는 시간(초). 목업의 「2~3초」에서 가운데를 잡았다.</summary>
        private const float HatchWobbleTime = 2.4f;

        /// <summary>빛이 차오르는 시간과 걷히는 시간. 차오르는 쪽이 빨라야 전환이 가려진다.</summary>
        private const float HatchFlashIn = 0.18f, HatchFlashOut = 0.42f;

        /// <summary>초상을 몇 픽셀로 찍을지. 부르는 쪽이 렌더 텍스처를 그 크기로 만든다.</summary>
        public static Vector2Int HatchSnailSize =>
            new Vector2Int(Pop.HatchSnail.width, Pop.HatchSnail.height);

        /// <summary>연출이 아직 도는 중인가. 도는 동안에는 닫을 수 없다.</summary>
        public bool HatchPlaying => _hatchPhase == HatchPhase.Wobble || _hatchPhase == HatchPhase.Flash;

        /// <summary>
        /// 알 부화 팝업을 띄우고 연출을 시작한다.
        /// <paramref name="snail"/> 은 갓 태어난 개체를 찍은 렌더 텍스처다.
        /// </summary>
        public void ShowHatch(int eggId, SnailPet.Data.RarityType rarity, Texture snail)
        {
            HidePopupGroups();
            _hatchGroup.gameObject.SetActive(true);
            OpenBlocker();

            var row = SnailPet.Data.GameData.EggDataById.TryGetValue(eggId, out var e) ? e : null;
            _hatchEgg.sprite = EggSprite(row);
            _hatchEgg.enabled = _hatchEgg.sprite != null;

            _hatchSnail.texture = snail;
            ApplyRarity(_hatchRarityIcon, _hatchRarityBadge, _hatchRarityText, rarity);

            _hatchPhase = HatchPhase.Wobble;
            _hatchTime = 0f;
            ShowHatchResult(false);
            ShowHatchButtons(false);
        }

        /// <summary>연출 중과 결과에서 보이는 것이 갈린다. 빛이 덮고 있는 사이에 바꾼다.</summary>
        private void ShowHatchResult(bool done)
        {
            _hatchEgg.gameObject.SetActive(!done);

            _hatchSnail.enabled = done && _hatchSnail.texture != null;
            _hatchRarityBadge.enabled = done && _hatchRarityIcon.sprite == null;
            _hatchRarityIcon.enabled = done && _hatchRarityIcon.sprite != null;
            _hatchRarityText.enabled = done && _hatchRarityIcon.sprite == null;
        }

        /// <summary>
        /// 확인·X 를 보이거나 감춘다.
        ///
        /// 연출 중에는 어차피 못 누르므로 아예 없는 편이 낫다 — 흐릿하게 놓여 있으면
        /// 눌러도 안 되는 버튼을 보여 주는 셈이다. 연출이 끝나면 그때 나타난다.
        /// 못 누르게 하는 것도 같이 걸어 둔다. 보이는 것과 눌리는 것이 어긋나면 안 된다.
        /// </summary>
        private void ShowHatchButtons(bool on)
        {
            if (_hatchOk != null)
            {
                _hatchOk.gameObject.SetActive(on);
                _hatchOk.interactable = on;
            }

            if (_popupClose != null)
            {
                _popupClose.gameObject.SetActive(on);
                _popupClose.interactable = on;
            }
        }

        // ── 팝업 등장 연출 ──
        //
        // 띡 하고 나타나면 어디서 나왔는지 눈이 못 따라간다. 살짝 작게 시작해 조금 넘겼다
        // 제자리로 오면 「스르륵」이 산다. 팝업 판은 피벗이 한가운데라 배율만 주면 된다.

        private const float PopupGrowTime = 0.16f;

        /// <summary>0 이상이면 자라는 중. 다 자라면 −1 로 두고 손대지 않는다.</summary>
        private float _popupGrow = -1f;

        private void StartPopupGrow()
        {
            _popupGrow = 0f;
            ApplyPopupGrow();
        }

        private void ApplyPopupGrow()
        {
            if (_popup == null) return;

            float t = _popupGrow < 0f ? 1f : Mathf.Clamp01(_popupGrow / PopupGrowTime);
            float s = Mathf.LerpUnclamped(0.88f, 1f, EaseOutBack(t));
            _popup.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>끝에서 살짝 넘겼다 돌아오는 곡선. 통통 튀는 느낌을 한 줄로 낸다.</summary>
        private static float EaseOutBack(float t)
        {
            const float k = 1.70158f;
            float u = t - 1f;
            return u * u * ((k + 1f) * u + k) + 1f;
        }

        private void StepPopupGrow()
        {
            if (_popupGrow < 0f) return;

            _popupGrow += Time.deltaTime;
            if (_popupGrow >= PopupGrowTime)
            {
                _popupGrow = -1f;
                if (_popup != null) _popup.localScale = Vector3.one;
                return;
            }
            ApplyPopupGrow();
        }

        // ── 안내 문구 ──
        //
        // 가운데에 잠깐 떴다 사라지는 띠. 즐겨찾기 말고도 쓸 데가 많으므로
        // 「글자만 주면 알아서 뜬다」 하나로 만들어 둔다.

        [SerializeField] private RectTransform _notice;
        [SerializeField] private Text _noticeText;
        [SerializeField] private CanvasGroup _noticeFade;

        private const float NoticeGrow = 0.16f, NoticeHold = 2.4f, NoticeGone = 0.3f;

        /// <summary>0 이상이면 떠 있는 중. 다 지나면 −1 로 두고 손대지 않는다.</summary>
        private float _noticeTime = -1f;

        private void BuildNotice()
        {
            _notice = NewRect("Notice", _widget);
            _notice.anchorMin = _notice.anchorMax = UiTheme.Notice.Anchor;
            _notice.pivot = new Vector2(0.5f, 0.5f);
            _notice.sizeDelta = new Vector2(UiTheme.Notice.MinWidth, UiTheme.Notice.Height);
            _notice.anchoredPosition = UiTheme.Notice.Offset;

            // 알림일 뿐이므로 클릭을 먹지 않는다. 뒤의 버튼이 그대로 눌려야 한다.
            Backdrop(_notice.gameObject, UiSprites.Shape.Notice, UiTheme.PanelFill).raycastTarget = false;

            _noticeText = Label(_notice, new RectInt(0, 0, UiTheme.Notice.MinWidth, UiTheme.Notice.Height),
                                "", UiTheme.Notice.FontSize, UiTheme.Ink);

            // 글자는 띠와 함께 늘어나야 하므로 네 귀퉁이에 매어 둔다
            var rt = (RectTransform)_noticeText.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            _noticeFade = _notice.gameObject.AddComponent<CanvasGroup>();
            _notice.gameObject.SetActive(false);
        }

        /// <summary>
        /// 안내 문구를 잠깐 띄운다. 띠는 글자 길이에 맞춰 늘어난다.
        /// 이미 떠 있으면 그 자리에서 글자만 바뀌고 시간이 다시 흐른다.
        /// </summary>
        public void ShowNotice(string message)
        {
            if (_notice == null || _noticeText == null) return;

            _noticeText.text = message ?? "";

            // preferredWidth 는 레이아웃을 기다리지 않고 그 자리에서 재 준다
            float w = _noticeText.preferredWidth + UiTheme.Notice.PadX * 2;
            _notice.sizeDelta = new Vector2(
                Mathf.Round(Mathf.Clamp(w, UiTheme.Notice.MinWidth, UiTheme.Notice.MaxWidth)),
                UiTheme.Notice.Height);

            _notice.gameObject.SetActive(true);
            _notice.SetAsLastSibling();      // 팝업 위에도 떠야 한다
            _noticeTime = 0f;
            ApplyNotice();
        }

        private void ApplyNotice()
        {
            if (_notice == null) return;

            // 팝업과 같은 곡선으로 스르륵 커진다
            float s = Mathf.LerpUnclamped(0.88f, 1f, EaseOutBack(Mathf.Clamp01(_noticeTime / NoticeGrow)));
            _notice.localScale = new Vector3(s, s, 1f);

            // 마지막 한 자락은 흐려지며 사라진다
            if (_noticeFade != null)
                _noticeFade.alpha = Mathf.Clamp01((NoticeGrow + NoticeHold + NoticeGone - _noticeTime) / NoticeGone);
        }

        private void StepNotice()
        {
            if (_noticeTime < 0f) return;

            _noticeTime += Time.deltaTime;
            if (_noticeTime >= NoticeGrow + NoticeHold + NoticeGone)
            {
                _noticeTime = -1f;
                if (_notice != null) _notice.gameObject.SetActive(false);
                return;
            }
            ApplyNotice();
        }

        private void Update()
        {
            StepPopupGrow();
            StepNotice();
            StepHatch();
        }

        private void StepHatch()
        {
            if (_hatchPhase == HatchPhase.None || _hatchPhase == HatchPhase.Result) return;

            _hatchTime += Time.deltaTime;

            if (_hatchPhase == HatchPhase.Wobble)
            {
                WobbleEgg(_hatchTime);
                if (_hatchTime >= HatchWobbleTime) { _hatchPhase = HatchPhase.Flash; _hatchTime = 0f; }
                return;
            }

            // 빛: 차오르는 동안은 알이 계속 떨고, 다 덮은 순간 달팽이로 바뀐다.
            if (_hatchTime < HatchFlashIn)
            {
                WobbleEgg(HatchWobbleTime + _hatchTime);
                SetLight(_hatchTime / HatchFlashIn);
                return;
            }

            if (_hatchEgg.gameObject.activeSelf) ShowHatchResult(true);

            float t = (_hatchTime - HatchFlashIn) / HatchFlashOut;
            SetLight(1f - t);

            if (t >= 1f)
            {
                SetLight(0f);
                _hatchPhase = HatchPhase.Result;
                ShowHatchButtons(true);      // 빛이 다 걷힌 뒤에 나타난다
            }
        }

        /// <summary>
        /// 부들거림. 좌우로 빠르게 떨면서 박자에 맞춰 세로로 눌렸다 늘어난다.
        /// 늘어나는 만큼 가로를 줄여야 부피가 유지되는 것처럼 보인다.
        /// </summary>
        private void WobbleEgg(float t)
        {
            var rt = (RectTransform)_hatchEgg.transform;

            float beat  = Mathf.Max(0f, Mathf.Sin(t * 3.1f));      // 0..1 로 부풀었다 가라앉는다
            float shake = Mathf.Sin(t * 34f) * 4f * beat;          // 도
            float tall  = 1f + beat * 0.16f;

            rt.localRotation = Quaternion.Euler(0f, 0f, shake);
            rt.localScale = new Vector3(1f / Mathf.Sqrt(tall), tall, 1f);
        }

        private void SetLight(float alpha) =>
            _hatchLightFill.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

        /// <summary>
        /// 팝업을 닫을 때 부화 연출의 흔적을 지운다.
        /// 특히 닫기 버튼은 연출 중에 <b>숨겨</b> 두므로, 안 돌려놓으면 다음에 뜨는
        /// 구매·이름 변경 팝업에 X 가 아예 없다.
        /// </summary>
        private void ResetHatch()
        {
            _hatchPhase = HatchPhase.None;
            _hatchTime = 0f;

            if (_hatchGroup != null) _hatchGroup.gameObject.SetActive(false);
            if (_hatchLightFill != null) SetLight(0f);

            if (_popupClose != null)
            {
                _popupClose.gameObject.SetActive(true);
                _popupClose.interactable = true;
            }

            if (_hatchEgg != null)
            {
                var rt = (RectTransform)_hatchEgg.transform;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }
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
            LocLabel(parent, at, token, 10, UiTheme.OnButton);

            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box;
            return btn;
        }

        /// <summary>
        /// 팝업을 띄운다.
        /// <paramref name="unitCost"/> 는 한 개당 값이며 <b>항상 양수</b>로 넣는다 —
        /// 부호는 여기서 붙인다. 목업에서 마이너스가 붙는 쪽은 코인이 나가는 <b>구매</b>다.
        /// <paramref name="max"/> 는 살 수 있는/팔 수 있는 최대 수량.
        /// </summary>
        public void ShowPopup(bool selling, int itemId, string itemName, double unitCost, int max)
        {
            _popupItemId = itemId;
            _popupSelling = selling;
            _popupUnit = System.Math.Abs(unitCost);
            _popupMax = Mathf.Max(1, max);
            _popupQty = 1;

            HidePopupGroups();
            _buyGroup.gameObject.SetActive(true);
            _popupTitle.text = SnailPet.Data.Loc.Format(selling ? Keys.AskSell : Keys.AskBuy, itemName);
            OpenBlocker();
            PaintPopup();
        }

        public void HidePopup()
        {
            if (_popupBlocker != null) _popupBlocker.gameObject.SetActive(false);
            SetDim(false);
            ResetHatch();
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
            long shown = (long)System.Math.Floor(_popupUnit * _popupQty);

            // 마이너스는 코인이 나가는 쪽, 즉 구매에 붙는다 (목업 3쪽의 -5,000).
            _popupCost.text = (_popupSelling ? "" : "-") + shown.ToString("N0");

            // 더 못 올리거나 못 내리면 눌러도 소용없다는 것을 보인다
            if (_popupMinus != null) _popupMinus.interactable = _popupQty > 1;
            if (_popupPlus != null)  _popupPlus.interactable  = _popupQty < _popupMax;
        }

        /// <summary>
        /// 왼쪽 목록과 오른쪽 상세를 <b>전부</b> 내린다. 한 화면이 다른 화면 위에 겹쳐 보이지
        /// 않게 하려는 것이다. 부르는 쪽은 이걸 먼저 부르고 자기 것만 다시 켠다.
        ///
        /// 예전에는 화면마다 「나머지 끄기」 목록을 손으로 들고 있었다. 그래서 파티 탭을 더했을 때
        /// 설정·도감·상세보기 셋이 그것만 못 꺼서 뒤에 비쳐 보였다 — 목록을 하나로 모아 둔다.
        /// </summary>
        private void HideScreens()
        {
            if (_rowGridRoot != null)    _rowGridRoot.gameObject.SetActive(false);
            if (_foodGridRoot != null)   _foodGridRoot.gameObject.SetActive(false);
            if (_eggGridRoot != null)    _eggGridRoot.gameObject.SetActive(false);
            if (_shopCatRoot != null)    _shopCatRoot.gameObject.SetActive(false);
            if (_shopGridRoot != null)   _shopGridRoot.gameObject.SetActive(false);
            if (_shopFilterRoot != null) _shopFilterRoot.gameObject.SetActive(false);
            if (_multiRoot != null)      _multiRoot.gameObject.SetActive(false);
            if (_wardrobeRoot != null)   _wardrobeRoot.gameObject.SetActive(false);
            if (_geneRoot != null)       _geneRoot.gameObject.SetActive(false);
            if (_guideRoot != null)      _guideRoot.gameObject.SetActive(false);
            if (_settingsRoot != null)   _settingsRoot.gameObject.SetActive(false);

            if (_panel != null)          _panel.gameObject.SetActive(false);
            if (_foodPanel != null)      _foodPanel.gameObject.SetActive(false);
            if (_eggPanel != null)       _eggPanel.gameObject.SetActive(false);
            if (_shopPanel != null)      _shopPanel.gameObject.SetActive(false);
            if (_shopItemPanel != null)  _shopItemPanel.gameObject.SetActive(false);
            if (_multiPanel != null)     _multiPanel.gameObject.SetActive(false);
            if (_wardrobePanel != null)  _wardrobePanel.gameObject.SetActive(false);
            if (_genePanel != null)      _genePanel.gameObject.SetActive(false);
            if (_guidePanel != null)     _guidePanel.gameObject.SetActive(false);
            if (_settingsPanel != null)  _settingsPanel.gameObject.SetActive(false);
        }

        /// <summary>목록을 펼칠지. 상세 패널은 화면에서 제자리에 남는다.</summary>
        public void SetMaximized(bool on)
        {
            Maximized = on;
            _listRoot.gameObject.SetActive(on && !Minimized);

            // 펼친 상태에서는 최대화 버튼이 할 일이 없다. 접는 것은 X 가 맡는다.
            if (_maximizeBtn != null) _maximizeBtn.gameObject.SetActive(!on && !Minimized);
        }

        public bool Maximized { get; private set; }

        /// <summary>
        /// 최소화. 코인 줄과 띠 하나만 남기고 전부 접는다.
        ///
        /// <b>위젯 상자도 같이 줄인다.</b> 안에 있는 것들은 상자의 왼쪽 위에 매여 있고
        /// 상자는 화면 오른쪽 아래에 붙어 있어서, 높이를 줄이면 코인 줄이 띠 위로 따라 내려온다.
        /// 상자만 남기고 내용을 끄면 코인 줄이 화면 위쪽에 혼자 떠 있게 된다.
        /// </summary>
        public void SetMinimized(bool on)
        {
            Minimized = on;

            if (_widget != null)
                _widget.sizeDelta = on ? new Vector2(_widgetSize.x, UiTheme.Mini.Bar.yMax) : _widgetSize;

            if (_miniRoot != null)    _miniRoot.gameObject.SetActive(on);
            if (_settingsBtn != null) _settingsBtn.gameObject.SetActive(!on);
            if (_panel != null)       _panel.gameObject.SetActive(!on);

            AskDelete(-1);
            CenterCoinRow(on);

            // 위로 자란 만큼 화면 밖으로 나갔을 수 있다 (끌어다 위쪽에 두고 최대화한 경우)
            if (_widget != null) _widget.GetComponent<UiDragMove>()?.ClampNow();

            // 최소화 중에는 목록도 최대화 버튼도 나오지 않는다.
            // 되돌아오면 언제나 처음 모습(접힌 달팽이 정보)이다 — 펼쳐 뒀던 목록까지 되살리지는 않는다.
            SetMaximized(on && Maximized);

            if (_closeBtn != null) _closeBtn.gameObject.SetActive(!on);
            RefreshClose();
        }

        public bool Minimized { get; private set; }

        /// <summary>최소화에서 되돌아올 위젯 상자 크기. <see cref="Bind"/> 에서 채운다.</summary>
        private Vector2 _widgetSize = new Vector2(UiTheme.PanelW + At.Close.xMax,
                                                  UiTheme.PanelH - At.Coin.y);

        /// <summary>지금 코인 줄을 오른쪽으로 밀어 둔 양. 되돌릴 때 그대로 뺀다.</summary>
        private float _coinShift;

        /// <summary>
        /// 최소화 동안만 코인 줄을 띠 가운데로 민다. 띠가 패널보다 좁아 제자리에 두면 왼쪽으로 쏠린다.
        ///
        /// 코인 셋(알약·아이콘·숫자)은 프리팹에서 손으로 맞춘 자리라 <b>절대 좌표를 쓰지 않는다</b>.
        /// 지금 자리를 재서 옮길 양만 구하고, 되돌아올 때는 그만큼만 뺀다.
        /// </summary>
        private void CenterCoinRow(bool on)
        {
            var row = CoinRow();
            float want = on ? CoinShift(row) : 0f;
            float delta = want - _coinShift;
            if (Mathf.Approximately(delta, 0f)) return;

            foreach (var rt in row)
                if (rt != null) rt.anchoredPosition += new Vector2(delta, 0f);

            _coinShift = want;
        }

        private RectTransform[] CoinRow() => new[]
        {
            _detailRoot != null ? _detailRoot.Find("CoinIcon") as RectTransform : null,
            _detailRoot != null ? _detailRoot.Find("CoinPill") as RectTransform : null,
            _coinText   != null ? (RectTransform)_coinText.transform : null,
        };

        /// <summary>코인 줄의 지금 폭을 재서 띠 가운데에 놓으려면 얼마나 밀어야 하는지.</summary>
        private static float CoinShift(RectTransform[] row)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var rt in row)
            {
                if (rt == null) continue;

                // 아이콘은 피벗이 가운데이고 배율도 1이 아니다 (프리팹에서 0.8배로 줄여 뒀다)
                float width = rt.rect.width * rt.localScale.x;
                float left  = rt.anchoredPosition.x - rt.pivot.x * width;

                min = Mathf.Min(min, left);
                max = Mathf.Max(max, left + width);
            }
            if (min > max) return 0f;

            var bar = UiTheme.Mini.Bar;
            return bar.x + (bar.width - (max - min)) * 0.5f - min;
        }

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
        ///
        /// <b>화면 크기로 정규화해서 옮긴다.</b> 가상 화면 값과 실제 창 크기가 어긋날 수 있기
        /// 때문이다 — 모니터를 하나 끄면 창은 줄어드는데 부르는 쪽이 든 값은 옛 크기다.
        /// 그대로 빼면 커서가 캔버스 밖으로 나가 <b>UI 가 영영 안 눌린다</b>. 실제로 그렇게
        /// 종료 버튼까지 못 누르는 일이 있었다. (근본 대책은 SnailPetBootstrap.SyncScreen)
        /// </summary>
        public bool ContainsCursor(int virtualX, int virtualY, int vLeft, int vTop, int vWidth, int vHeight)
        {
            var es = EventSystem.current;
            if (es == null) return false;

            // 사각형으로 재면 안 된다. 위젯 상자는 최대화 기준으로 잡혀 있어서 목록을 접었을 때
            // 비어 있는 왼쪽 절반까지 UI 로 잡히고, 그 위에서 바탕화면 클릭이 막힌다.
            // 레이캐스터에 물어보면 실제로 그려진 것만 걸린다.
            _pointer ??= new PointerEventData(es);

            float x = vWidth  > 0 ? (virtualX - vLeft) * (float)Screen.width  / vWidth  : virtualX - vLeft;
            float y = vHeight > 0 ? (virtualY - vTop)  * (float)Screen.height / vHeight : virtualY - vTop;
            _pointer.position = new Vector2(x, Screen.height - y);

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
