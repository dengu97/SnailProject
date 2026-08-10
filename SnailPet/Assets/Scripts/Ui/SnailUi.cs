using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using At = SnailPet.Ui.UiTheme.At;

namespace SnailPet.Ui
{
    /// <summary>
    /// 데스크톱 위젯 UI. 지금은 목업의 「디폴트」 상태만 만든다.
    ///
    /// 씬 없이 전부 코드로 만드는 것은 달팽이 쪽과 같은 방침이다.
    /// 글꼴은 TextMeshPro 대신 OS 한글 폰트를 런타임에 잡아 쓴다. TMP 는 한글
    /// 아틀라스를 따로 구워야 하는데, 여기서 얻을 게 없는 준비 비용이다.
    /// </summary>
    public sealed class SnailUi : MonoBehaviour
    {
        /// <summary>한 화면에 위젯이 두 개 이상 뜰 일이 없으므로 정렬 순서는 고정.</summary>
        private const int CanvasSortOrder = 100;

        private Font _font;
        private RectTransform _widget;      // 패널 + 밖으로 걸치는 버튼까지 감싸는 상자
        private RectTransform _panel;

        private Text _nameText, _rarityText, _ageText, _coinText;
        private RectTransform _fullFill, _happyFill;

        public event Action Rename, Detail, Wardrobe, Gene, Sell, Settings, Close, Maximize;

        public static SnailUi Create(Transform parent)
        {
            var go = new GameObject("SnailUi");
            go.transform.SetParent(parent, false);
            var self = go.AddComponent<SnailUi>();
            self.Build();
            return self;
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
            EnsureEventSystem();

            // 위젯 상자를 화면 오른쪽 아래에 붙인다. 코인 줄이 패널 위로 올라가므로 그만큼 키운다.
            _widget = NewRect("Widget", (RectTransform)transform);
            _widget.anchorMin = _widget.anchorMax = _widget.pivot = new Vector2(1f, 0f);
            _widget.sizeDelta = new Vector2(At.Close.xMax, UiTheme.PanelH - At.Coin.y);
            _widget.anchoredPosition = new Vector2(-UiTheme.ScreenMargin, UiTheme.ScreenMargin);

            _panel = Panel(_widget, new RectInt(0, -At.Coin.y, UiTheme.PanelW, UiTheme.PanelH));

            BuildHeader();
            BuildGauges();
            BuildActions();
            BuildOutside();

            SetSnail("달팽이 이름", "에픽", 0);
            SetGauges(0.62f, 0.28f);
            SetCoin(5000);
        }

        /// <summary>이름칸 · 이름 수정 · 등급 뱃지.</summary>
        private void BuildHeader()
        {
            Box(_panel, At.NameField, UiTheme.Slot, 5, "NameField");
            IconButton(_panel, At.RenameBtn, "icon_rename", "Rename", () => Rename?.Invoke());

            var name = At.NameField;
            _nameText = Label(_panel, new RectInt(name.x + 22, name.y, name.width - 26, name.height),
                              "달팽이 이름", 13, UiTheme.Ink);

            Box(_panel, At.Rarity, UiTheme.BadgeDark, 6, "RarityBadge");
            _rarityText = Label(_panel, At.Rarity, "에픽", 9, UiTheme.OnBadge);
        }

        /// <summary>나이 뱃지 · 포만도 · 행복 지수.</summary>
        private void BuildGauges()
        {
            Box(_panel, At.Age, UiTheme.Slot, 6, "AgeBadge");
            _ageText = Label(_panel, At.Age, "00살", 9, UiTheme.Ink);

            _fullFill  = Gauge(At.FullBar,  At.FullIcon,  "icon_food",  UiTheme.GaugeFull,  "Full");
            _happyFill = Gauge(At.HappyBar, At.HappyIcon, "icon_happy", UiTheme.GaugeHappy, "Happy");
        }

        /// <summary>
        /// 게이지 한 줄. 트랙 위에 채우기를 얹고, 왼쪽 끝에 아이콘 칸을 올린다.
        /// 채우기는 <b>왼쪽부터</b> 찬다. 목업에서는 오른쪽에 붙어 있는데,
        /// 게이지는 왼쪽부터 차는 것이 표준이라 그렇게 뒀다.
        /// </summary>
        private RectTransform Gauge(RectInt bar, RectInt icon, string iconKey, Color fillColor, string name)
        {
            Box(_panel, bar, UiTheme.Slot, 6, name + "Track");

            const int inset = 2;
            var fill = Box(_panel, new RectInt(bar.x + inset, bar.y + inset,
                                               bar.width - inset * 2, bar.height - inset * 2),
                           fillColor, 5, name + "Fill");

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
            IconButton(_widget, Above(At.Settings), "icon_settings", "Settings",
                       () => Settings?.Invoke(), UiTheme.Accent);

            var coin = Above(At.Coin);
            Box(_widget, coin, UiTheme.Slot, 8, "CoinPill");
            Icon(_widget, new RectInt(coin.x + 4, coin.y + 6, 22, 22), "icon_coin",
                 Color.white, "CoinIcon").raycastTarget = false;
            _coinText = Label(_widget, new RectInt(coin.x + 30, coin.y, coin.width - 34, coin.height),
                              "5,000", 12, UiTheme.Ink);

            // 닫기·최대화 아이콘은 아직 없다. 들어오면 IconButton 으로 바꾼다.
            PlaceholderButton(Above(At.Close),    "Close",    "✕", () => Close?.Invoke());
            PlaceholderButton(Above(At.Maximize), "Maximize", "＋", () => Maximize?.Invoke());
        }

        /// <summary>위젯 상자 기준 좌표로 옮긴다. 목업은 패널 왼쪽 위가 원점이라 코인 줄만큼 내려 준다.</summary>
        private static RectInt Above(RectInt r) => new RectInt(r.x, r.y - At.Coin.y, r.width, r.height);

        /// <summary>아이콘이 아직 없는 버튼. 모양만 맞춰 두고 눌리기는 한다.</summary>
        private void PlaceholderButton(RectInt r, string name, string glyph, Action fire)
        {
            var rt = NewRect(name, _widget);
            Place(rt, r);

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiSprites.Fill(6);
            img.type = Image.Type.Sliced;
            img.color = UiTheme.PanelBorder;

            Label(rt, new RectInt(0, 0, r.width, r.height), glyph, 16, Color.white);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => fire?.Invoke());
        }

        // ── 값 넣기 ──

        public void SetSnail(string name, string rarity, int age)
        {
            _nameText.text = name;
            _rarityText.text = rarity;
            _ageText.text = age + "살";
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
            if (_widget == null) return false;

            float x = virtualX - vLeft;
            float y = vHeight - (virtualY - vTop);     // 유니티 화면 좌표로

            var c = new Vector3[4];
            _widget.GetWorldCorners(c);                // 오버레이 캔버스에서는 월드 = 화면 픽셀
            return x >= c[0].x && x <= c[2].x && y >= c[0].y && y <= c[2].y;
        }

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

        private Image Box(RectTransform parent, RectInt r, Color color, int radius, string name)
        {
            var rt = NewRect(name, parent);
            Place(rt, r);

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiSprites.Fill(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private RectTransform Panel(RectTransform parent, RectInt r)
        {
            var fill = Box(parent, r, UiTheme.PanelFill, UiTheme.PanelRadius, "Panel");
            fill.raycastTarget = true;      // 패널 위에서는 클릭이 바탕화면으로 새면 안 된다

            var line = NewRect("Border", (RectTransform)fill.transform);
            line.anchorMin = Vector2.zero; line.anchorMax = Vector2.one;
            line.offsetMin = Vector2.zero; line.offsetMax = Vector2.zero;

            var img = line.gameObject.AddComponent<Image>();
            img.sprite = UiSprites.Border(UiTheme.PanelRadius, UiTheme.PanelBorderPx);
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
            img.sprite = Resources.Load<Sprite>("Ui/Icon/" + key);
            img.color = color;
            img.preserveAspect = true;
            if (img.sprite == null)
                Debug.LogWarning("[SnailPet] UI 아이콘을 찾지 못했습니다: Ui/Icon/" + key);
            return img;
        }

        private Button IconButton(RectTransform parent, RectInt r, string key, string name,
                                  Action fire, Color? background = null)
        {
            var rt = NewRect(name, parent);
            Place(rt, r);

            var img = rt.gameObject.AddComponent<Image>();
            if (background.HasValue)
            {
                img.sprite = UiSprites.Fill(4);
                img.type = Image.Type.Sliced;
                img.color = background.Value;
            }
            else
            {
                img.color = new Color(0f, 0f, 0f, 0f);   // 배경이 없어도 클릭은 받아야 한다
            }

            int pad = background.HasValue ? 4 : 1;
            Icon(rt, new RectInt(pad, pad, r.width - pad * 2, r.height - pad * 2),
                 key, UiTheme.Ink, "Glyph").raycastTarget = false;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => fire?.Invoke());
            return btn;
        }
    }
}
