using System;
using System.IO;
using System.Text;
using SnailPet.Data;
using SnailPet.Snail;
using SnailPet.Ui;
using UnityEngine;
using SnailPet.Desktop;   // ScreenRect / BoxWalk 는 플랫폼 의존이 없다

namespace SnailPet
{
    /// <summary>
    /// 씬을 따로 만들지 않고 코드로 전부 구성한다.
    /// 씬 에셋을 손으로 편집할 이유가 아직 없고, 씬이 없어도 빌드가 되게 하려는 목적.
    /// </summary>
    public static class SnailPetBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("~SnailPetRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SnailPetRunner>();
        }
    }

    public sealed class SnailPetRunner : MonoBehaviour
    {
        /// <summary>
        /// 안전장치. 클릭 통과 + 항상 위 + 테두리 없는 전체 화면 창은 마우스로 닫을 수 없다.
        /// 반드시 스스로 종료되게 둘 것.
        /// </summary>
        private const float AutoQuitSeconds = 40f;

        /// <summary>데모용. 먹이를 자동으로 떨어뜨릴지. 다른 것을 관찰할 때는 꺼 둔다.</summary>
        private const bool DemoFoodEnabled = false;

        /// <summary>데모용. 이 간격마다 먹이를 하나 떨어뜨린다.</summary>
        private const float DemoFoodSeconds = 5f;

        /// <summary>레벨업 3600초·포만 감소 120초를 눈으로 보려면 시간을 당겨야 한다. 40초 실행 = 약 80분.</summary>
        private const float DemoTimeScale = 120f;

        /// <summary>
        /// 데모용 이동 속도 배수.
        ///
        /// 실제 속도(24~252px/s)로는 3840px 짜리 화면에서 모서리까지 가는 데만 100초가 넘어
        /// 40초 실행 안에 모서리 도는 것을 한 번도 볼 수 없다. 관찰용으로만 당긴다.
        /// </summary>
        private const float DemoSpeedScale = 5f;

        /// <summary>데모용 시작 위치. 1 에 가까울수록 모서리 바로 앞에서 시작한다.</summary>
        private const float DemoStartT = 0.94f;

        /// <summary>데모용 시작 벽. 특정 벽에서의 모습을 확인할 때 바꾼다.</summary>
        private const BoxEdge DemoStartEdge = BoxEdge.Bottom;

        /// <summary>
        /// 데모용. 박스를 화면 안쪽으로 이만큼(px) 줄인다.
        /// 모서리는 화면 맨 끝이라 도는 모습이 잘렸는데, 줄이면 화면 안에서 볼 수 있다.
        /// </summary>
        private const int DemoBoxInset = 0;

        /// <summary>
        /// 달팽이가 기어다닐 박스를 무엇으로 삼을지.
        ///
        /// false = 화면 전체. 달팽이가 바탕화면 테두리를 한 바퀴 돈다.
        /// true  = 활성 창. 기획서 「창 반응」 항목용이며 창을 옮기면 달팽이도 따라온다.
        ///         구현과 검증은 끝나 있고 이 값만 바꾸면 동작한다.
        /// </summary>
        private const bool UseActiveWindowAsBox = false;

        private readonly StringBuilder _log = new StringBuilder();
        private Camera _cam;
        private Transform _snail;
        private float _t;
        private bool _diagDone;

        private SnailAppearance _appearance;
        private SnailBounds _bounds;      // 스케일 적용 전 (월드 단위)
        private float _visibleWidth = 1f; // 스케일 적용 전 몸통 가로 (월드 단위)
        private float _scale = 1f;
        private SnailGrowth _growth;
        private SnailComposer.Composed _composed;

        private enum PetState { Wander, Seek, Eat }
        private PetState _state = PetState.Wander;
        private FoodField _food;
        private FoodItem _target;
        private FoodDataRow[] _droppable;
        private float _nextFoodAt = 2f;
        private float _eatFlashUntil;
        private string _lastBuffs = "없음";

        private SnailPresent _present;

        /// <summary>보유 상태. 달팽이 목록·알·코인·음식이 전부 여기에 있다.</summary>
        private PlayerState _player;
        private bool _wasMouseDown;
        private bool _cursorOnSnail;
        private float _claimFlashUntil;

        /// <summary>낙하 가속도(px/s^2). 먹이와 같은 값을 써서 같은 무게감으로 떨어진다.</summary>
        private const float Gravity = FoodField.Gravity;

        private enum DragTarget { None, Snail, Food }
        private DragTarget _drag = DragTarget.None;
        private FoodItem _dragFood;
        private Vector2 _grabOffset;

        /// <summary>
        /// 달팽이가 벽을 벗어난 상태. 들려 있거나 떨어지는 중이다.
        /// 이때는 앵커 대신 이 발 좌표(가상 화면 px)가 위치를 결정한다.
        /// </summary>
        private bool _snailFalling;
        private Vector2 _snailFootScreen;
        private float _snailVelY;

        private bool SnailFree => _drag == DragTarget.Snail || _snailFalling;

        // ── 벽에서 떼는 연출 ──
        // 발은 벽에 붙은 채 몸만 딸려오다가, 임계점을 넘으면 툭 떨어진다.
        // 늘어남·저항·반동을 값 하나(_stretch)에 감쇠 스프링으로 태워 한 번에 처리한다.

        /// <summary>이만큼(px) 당기면 떨어진다.</summary>
        private const float PeelThreshold = 72f;

        /// <summary>임계점에서의 몸통 신장 비율. 0.35 면 35% 늘어난다.</summary>
        private const float PeelMaxStretch = 0.35f;

        /// <summary>떼는 동안 벽을 따라 끌려가며 기울어지는 최대 각(도).</summary>
        private const float PeelMaxLeanDeg = 18f;

        /// <summary>떨어지는 순간 되튕기는 양. 음수라 잠깐 움츠러든다.</summary>
        private const float PopRecoil = -0.22f;

        private const float SpringStiffness = 320f;
        private const float SpringDamping = 12f;

        // ── 들고 다닐 때 ──
        // 손이 움직이면 몸이 못 따라오고 늘어졌다가 흔들린다.
        // 속도를 그대로 스프링 목표로 주면 지연도 잔진동도 공짜로 나온다.
        private const float CarryStretchPerSpeed = 0.00020f;   // 약 1400px/s 에서 최대
        private const float CarryMaxStretch = 0.28f;
        private const float CarryLeanPerSpeed = 0.014f;
        private const float CarryMaxLeanDeg = 22f;

        /// <summary>떨어지는 동안 아래로 늘어지는 정도.</summary>
        private const float FallStretchPerSpeed = 0.00014f;
        private const float FallMaxStretch = 0.20f;

        /// <summary>착지 충격 → 찌그러짐. 속도(px/s)에 곱해 스프링에 속도로 꽂는다.</summary>
        private const float LandingSquashPerSpeed = 0.0028f;
        private const float LandingSquashMax = 4.5f;

        // ── 기어 다닐 때의 상시 출렁임 ──
        // 시간이 아니라 <b>이동한 거리</b>로 위상을 돌린다. 그래야 느린 달팽이는 느리게,
        // 버프로 빨라지면 빠르게 출렁이고, 멈춰 있으면 아예 멈춘다.

        /// <summary>이 거리(px)마다 한 번 출렁인다.</summary>
        private const float WobbleWavelength = 46f;
        private const float WobbleStretch = 0.045f;
        private const float WobbleLeanDeg = 2.2f;

        /// <summary>이 속도(px/s)에서 출렁임이 최대가 된다.</summary>
        private const float WobbleFullSpeed = 120f;

        /// <summary>모서리를 도는 동안 진행 방향으로 기울어지는 정도. 관성으로 몸이 먼저 넘어간다.</summary>
        private const float TurnLeanDeg = 14f;

        /// <summary>모서리를 도는 동안 눌리는 정도. 발을 오므리며 몸이 주저앉는다.</summary>
        private const float TurnSquash = -0.10f;

        private float _wobblePhase;

        // ── 발바닥 ──
        // 전부 몸 크기에 대한 비율로 둔다. 레벨이 올라 몸이 커져도 상대 비율이 유지된다.

        /// <summary>몸 높이 중 발바닥으로 볼 비율. 이 위로는 발 변형이 안 간다.</summary>
        private const float FootBandFraction = 0.35f;

        /// <summary>모서리 꺾임이 퍼지는 폭. 몸통 가로폭 대비.</summary>
        private const float CornerSpanFraction = 0.5f;

        /// <summary>
        /// 모서리에서 발바닥을 접을지.
        ///
        /// 기하는 이렇다. 모서리를 축으로 한쪽은 지나온 벽 각으로, 반대쪽은 갈 벽 각으로
        /// 돌리면 발바닥이 정확히 두 벽에 눕는다. 모서리에서의 거리가 보존되므로
        /// 발바닥이 늘어나지도 않는다.
        ///
        /// 여기까지 오는 데 세 가지가 필요했고, 셋 다 갖춰지기 전에는 모양이 안 났다.
        ///  1. 껍질이 <b>붙은 지점의 변형</b>을 따라갈 것 (SnailDeform.RigidPose).
        ///     예전에는 LeanDeg 로만 돌아서 휜 몸을 못 따라가 사이가 벌어졌다
        ///  2. 접힘 띠가 넉넉할 것. 얇으면 발바닥 한 조각만 꺾여 떨어져 나온 것처럼 보인다
        ///  3. 아트 발바닥이 평평할 것. 물결치면 같은 발바닥인데도 접히는 세기가 달라진다
        /// </summary>
        private const bool CornerFoldEnabled = true;

        /// <summary>물결 하나의 길이. 몸통 가로폭 대비.</summary>
        private const float WaveLengthFraction = 0.30f;

        /// <summary>물결이 부푸는 높이. 몸통 가로폭 대비.</summary>
        private const float WaveAmplitudeFraction = 0.045f;

        /// <summary>들렸을 때 발이 처지는 깊이. 몸 높이 대비.</summary>
        private const float DangleDepthFraction = 0.10f;

        /// <summary>들고 흔들 때 발이 쏠리는 양. 몸통 가로폭 대비.</summary>
        private const float DangleSwayFraction = 0.10f;

        /// <summary>이 손 속도(px/s)에서 쏠림이 최대가 된다.</summary>
        private const float DangleSwayFullSpeed = 900f;

        private SnailDeform _deform;
        private float _sway, _swayVel;

        /// <summary>스프라이트가 뒤집히거나 0 이 되지 않게 막는 한계.</summary>
        private const float MinStretch = -0.45f, MaxStretch = 0.60f;

        /// <summary>누르고 있지만 아직 안 떨어진 상태.</summary>
        private bool _peeling;
        private Vector2 _grabScreen;

        private float _stretch, _stretchVel, _stretchTarget;   // 0 = 평소, 양수 = 늘어남
        private float _lean, _leanVel, _leanTarget;            // 몸통이 기울어지는 각(도)

        private Vector2 _handVel, _lastCursor;
        private bool _hasLastCursor;

        /// <summary>껍질 중심의 로컬 y. 몸이 늘어난 만큼 껍질을 평행이동시킬 때 쓴다.</summary>
        private float _shellCenterLocalY;

        private int _vLeft, _vTop, _vWidth, _vHeight;
        private string _status = "";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private BoxAnchor _anchor;
        private ScreenRect _box;
#endif

        private void Say(string s) { _log.AppendLine(s); Debug.Log(s); }

        private void Awake()
        {
            Application.runInBackground = true;
            Say("=== SnailPet ===");
            Say("Unity " + Application.unityVersion + " / " + SystemInfo.operatingSystem);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var v = TransparentWindow.VirtualScreen;
            _vLeft = v.Left; _vTop = v.Top; _vWidth = v.Width; _vHeight = v.Height;
            Say("가상 화면: " + v);
#else
            _vLeft = 0; _vTop = 0; _vWidth = Screen.width; _vHeight = Screen.height;
#endif
            Screen.SetResolution(_vWidth, _vHeight, FullScreenMode.Windowed);

            SetupCamera();
            SetupSnail();
            SetupUi();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bool ok = TransparentWindow.Apply(clickThrough: true);
            Say("[3] 투명 창 적용 ..... " + (ok ? "OK" : "실패: " + TransparentWindow.LastError));
            Say("[4] 클릭 통과 ....... " + (TransparentWindow.IsClickThrough() ? "OK" : "미적용"));

            _anchor = new BoxAnchor { Edge = DemoStartEdge, T = DemoStartT, Forward = true };
            _box = ResolveBox();
            Say("[5] 박스 ............ " + BoxName + "  " + _box);
#endif
            _status = "달팽이·먹이를 끌어 옮길 수 있습니다. 놓으면 아래로 떨어집니다.";
            Say("");
            Say("→ " + AutoQuitSeconds + "초 뒤 자동 종료.");
            WriteReport();
        }

        private void SetupCamera()
        {
            var camGo = new GameObject("SnailCamera");
            camGo.transform.SetParent(transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = Screen.height * 0.5f;      // 1 world unit = 1 px
            _cam.transform.position = new Vector3(0, 0, -10f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);  // 알파 0 이 핵심
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
        }

        private void SetupSnail()
        {
            var rng = new System.Random();
            _player = SaveFile.Load();

            if (_player != null)
            {
                Say("[1] 이어하기 ......... 세이브에서 불러옴");
                Say("      " + _player.Active.Appearance);
            }
            else
            {
                _player = new PlayerState();

                var eggs = GameData.EggData;
                var egg = eggs[rng.Next(eggs.Length)];
                var first = _player.AddSnail(SnailHatchery.Hatch(egg.Id, rng), egg.RarityType);
                GiveStartingBelongings();

                Say("[1] 부화 ............. " + GameData.TokenById[egg.Id] + " (" + egg.RarityType + ")");
                Say("      " + first.Appearance);
            }

            ActivateSnail(_player.Active);

            _food = new FoodField(transform);
            _present = new SnailPresent(transform);

            Say("[2] 성장 ............. " + _growth);
            Say(string.Format("      환산: Speed 1 = {0}px/s, Size 1 = {1}px  (레벨 1~{2})",
                SnailGrowth.PixelsPerSpeedUnit, SnailGrowth.PixelsPerSizeUnit, SnailGrowth.MaxLevel));
            var top = GameData.LevelData[GameData.LevelData.Length - 1];
            Say(string.Format("      최고 레벨: 속도 {0}px/s, 크기 {1}px",
                top.Speed * SnailGrowth.PixelsPerSpeedUnit, top.Size * SnailGrowth.PixelsPerSizeUnit));
            Say("      보유 ........... " + _player);
            Say("      세이브 ......... " + SaveFile.Path);
            Say($"      (데모 배속 {DemoTimeScale}배라 {AutoQuitSeconds:0}초 실행이 " +
                $"약 {AutoQuitSeconds * DemoTimeScale / 60f:0}분치로 굳습니다. " +
                "처음부터 보려면 위 파일을 지우세요)");
        }

        /// <summary>
        /// 나가기 전에 보유 상태를 적는다.
        ///
        /// 여기 한 곳에서만 저장한다. 상태를 바꾸는 곳마다 부르면 저장 누락이 생기고,
        /// 성장은 매 프레임 바뀌므로 어차피 나갈 때 한 번은 적어야 한다.
        /// </summary>
        private void OnApplicationQuit()
        {
            SaveFile.Save(_player);
        }

        /// <summary>
        /// 첫 실행 지급. 상점이 없어 스스로 구할 방법이 아직 없으므로 손에 쥐어 준다.
        /// 상점이 생기면 여기서 주는 양을 줄이거나 없앤다.
        /// </summary>
        private void GiveStartingBelongings()
        {
            foreach (var e in GameData.EggData) { _player.Eggs.Add(e.Id); _player.Eggs.Add(e.Id); }

            // 아트가 없는 음식은 화면에 떨어뜨릴 수 없어 줘도 쓸 수가 없다
            int n = 1;
            foreach (var f in GameData.FoodData)
                if (!string.IsNullOrEmpty(f.ResourceKey)) _player.Items.Add(f.Id, n++);

            _player.Items.Add(PlayerState.CoinItemId, 100);
        }

        /// <summary>
        /// 목록의 개체 하나를 화면에 낸다.
        ///
        /// 외형이 통째로 바뀌므로 합성부터 다시 한다. 발선·껍질 중심·발바닥 프로파일은
        /// 전부 그 개체의 몸통에서 실측한 값이라 같이 다시 재야 한다 —
        /// 이전 달팽이의 값을 물려받으면 발이 벽에서 뜨거나 파고든다.
        /// 벽 위의 자리(<see cref="_anchor"/>)는 그대로 두어 있던 곳에서 바뀐다.
        /// </summary>
        private void ActivateSnail(OwnedSnail snail)
        {
            if (snail == null) return;

            if (_composed != null) Destroy(_composed.Root);

            _player.ActiveId = snail.Id;
            _appearance = snail.Appearance;
            _growth     = snail.Growth;
            _rarity     = snail.Rarity;

            _composed = SnailComposer.Build(_appearance);
            _composed.Root.transform.SetParent(transform, false);
            _snail = _composed.Root.transform;

            _bounds = SnailMetrics.Measure(_appearance);
            _visibleWidth = _bounds.Right - _bounds.Left;
            if (!_bounds.Measured || _visibleWidth < 0.01f) _visibleWidth = 1f;

            _shellCenterLocalY = MeasureShellCenterY();

            _deform = new SnailDeform { Foot = _bounds.Foot };
            MeasureSole();

            ApplyGrowth();

            // 먹으러 가던 목표는 이 개체의 것이 아니다
            _target = null;
        }

        /// <summary>
        /// 발바닥 선을 몇 등분해서 잴지. 부화할 때 한 번만 재므로 넉넉히 잡아도 된다.
        /// 격자 정점(가로 25줄)뿐 아니라 껍질 자세 계산에서도 임의의 x 로 조회한다.
        /// </summary>
        private const int SoleSamples = 48;

        private SnailUi _ui;
        private RarityType _rarity = RarityType.Common;
        private SnailPortrait _portrait;
        private bool _cursorOnUi;

        /// <summary>
        /// 위젯 UI. 목록·음식·알은 전부 <see cref="_player"/> 를 그대로 비춘다.
        /// 상태를 바꾼 쪽에서 Refresh* 를 불러 다시 그린다 — UI 는 스스로 아무것도 안 들고 있다.
        /// </summary>
        private void SetupUi()
        {
            _ui = SnailUi.Create(transform);

            _ui.Rename   += () => Say("      [UI] 이름 수정");
            _ui.Detail   += () => Say("      [UI] 상세정보");
            _ui.Wardrobe += () => Say("      [UI] 옷장");
            _ui.Gene     += () => Say("      [UI] 유전 정보");
            _ui.Sell     += () => Say("      [UI] 판매");
            _ui.Settings += () => Say("      [UI] 설정");
            _ui.Close    += () => Say("      [UI] 최소화");
            _ui.Maximize += () => Say("      [UI] 최대화");
            _ui.TabChanged += i => Say($"      [UI] 탭 {i}");
            _ui.SwapTo     += SwapSnail;

            // 「먹이기」는 즉시 먹이지 않는다. 화면에 떨어뜨리고 달팽이가 기어가서 먹는다.
            _ui.FeedFood += DropFoodFromUi;

            _ui.PutEgg       += PutEggInIncubator;
            _ui.ClaimHatched += ClaimHatched;
            _ui.GoShop       += () => _ui.SetTab(3);
            _ui.BuyProduct   += BuyFromShop;

            SnailPortrait.ExcludeFrom(_cam);

            RefreshEggs();
            RefreshFoods();
            RefreshSnail();

            var pick = Shop.Today(System.DateTime.Now);
            _ui.SetTodayPick(pick);
            Say("      오늘의 추천: " + (pick == null ? "없음" : Shop.NameOf(pick) + " " + pick.CostCount + "코인"));

            Say("[7] UI ............. 디폴트 패널");
            Say("      도형: " + UiSprites.Describe());
            var size = SnailUi.PortraitSize;
            Say($"      초상: {size.x}x{size.y} (레이어 {SnailPortrait.Layer})");
        }

        /// <summary>보유 음식을 음식 탭에 반영한다.</summary>
        private void RefreshFoods() => _ui.SetFoods(_player.OwnedFoods());

        /// <summary>
        /// 활성 개체를 상세 패널과 목록에 반영한다.
        /// 초상은 외형이 바뀔 때만 다시 찍으면 되므로 <paramref name="reshoot"/> 로 가른다.
        /// </summary>
        private void RefreshSnail(bool reshoot = true)
        {
            var rows = new (string, RarityType, int, bool)[_player.Snails.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                var s = _player.Snails[i];
                rows[i] = (s.Name, s.Rarity, s.Growth.Level, s.Id == _player.ActiveId);
            }
            _ui.SetRows(rows);

            // 이름은 아직 지을 방법이 없어 비워 둔다 — UI 가 「이름 없음」으로 채운다.
            var active = _player.Active;
            _ui.SetSnail(active?.Name, _rarity, _growth.Level);
            _ui.SetCoin(_player.Coins);

            if (!reshoot) return;

            // 패널 가운데의 달팽이 모습. 살아있는 쪽은 벽을 따라 돌아가 있고 변형 중이라
            // 비출 수 없다. 같은 외형으로 정지 복제본을 만들어 전용 카메라로 찍는다.
            _portrait?.Dispose();
            var size = SnailUi.PortraitSize;
            _portrait = new SnailPortrait(transform, _appearance, _bounds, size.x, size.y);
            _ui.SetPortrait(_portrait.Texture);
        }

        /// <summary>상점에서 「구매하기」를 눌렀다. 오늘의 할인 칸에서 누른 것만 깎아 준다.</summary>
        private void BuyFromShop(int shopId, bool discounted)
        {
            var result = Shop.TryBuy(_player, shopId, discounted);
            if (result != Shop.Result.Ok)
            {
                string why = result switch
                {
                    Shop.Result.NotEnough     => "코인이 모자랍니다",
                    Shop.Result.NoPrice       => "가격이 없습니다",
                    _                         => "그런 상품이 없습니다",
                };
                Say($"      [UI] 구매 실패 ({shopId}, 할인 {discounted}): {why}  보유 {_player.Coins}코인");
                return;
            }

            // 산 것이 음식일 수도 알일 수도 있어 양쪽을 다 새로 그린다
            RefreshFoods();
            RefreshEggs();
            _ui.SetCoin(_player.Coins);
            _ui.RefreshShop();

            Say($"      [UI] 구매: {shopId}{(discounted ? " (할인가)" : "")} → {_player}");
        }

        /// <summary>목록에서 다른 달팽이를 골랐다.</summary>
        private void SwapSnail(int listIndex)
        {
            if (!_player.SetActiveByIndex(listIndex)) return;

            ActivateSnail(_player.Snails[listIndex]);
            RefreshSnail();
            Say($"      [UI] {listIndex}번 달팽이로 교체 → {_appearance}");
        }

        /// <summary>
        /// 몸통의 발바닥 선을 실측해 변형에 물린다.
        ///
        /// 최하단 한 점을 발선으로 쓰면 머리 쪽이 들린 몸통에서 그 부분만 변형이 약해진다.
        /// 실측한 선을 기준으로 삼으면 몸통 모양과 무관하게 발바닥 전체가 고르게 접힌다.
        /// </summary>
        private void MeasureSole()
        {
            foreach (var p in _appearance.Parts)
            {
                if (p.Type != PartsType.Body) continue;

                var sprite = SnailComposer.Load(SnailComposer.LinePath(p.Type, p.ResourceKey));
                if (sprite == null) continue;
                if (!SnailMetrics.TryMeasureSole(sprite, SoleSamples, out var sole,
                                                 out float minX, out float maxX)) continue;

                _deform.SetSole(sole, minX, maxX);

                float lo = float.MaxValue, hi = float.MinValue;
                foreach (var v in sole) { if (v < lo) lo = v; if (v > hi) hi = v; }
                Say($"      발바닥 선: {p.ResourceKey} 기복 {hi - lo:0}  (기준 발선 {_bounds.Foot:0})");
                return;
            }

            Say("      경고: 발바닥 선을 재지 못해 최하단 한 줄을 씁니다.");
        }

        /// <summary>
        /// 껍질의 세로 중심(로컬). 몸이 늘어나도 껍질은 안 늘어나므로,
        /// 그 높이에서의 변위만큼 껍질을 통째로 밀어 몸에 붙어 있게 만든다.
        /// 껍질이 없으면 몸통 중간을 대신 쓴다.
        /// </summary>
        private float MeasureShellCenterY()
        {
            foreach (var p in _appearance.Parts)
            {
                if (p.Type != PartsType.Shell) continue;
                var sprite = SnailComposer.Load(SnailComposer.LinePath(p.Type, p.ResourceKey));
                if (sprite != null && SnailMetrics.TryMeasure(sprite, out var e))
                    return (e.Bottom + e.Top) * 0.5f;
            }
            return (_bounds.Foot + _bounds.Top) * 0.5f;
        }

        /// <summary>
        /// 늘어남과 기울기를 실제 트랜스폼에 적용한다.
        ///
        /// 발을 피벗으로 삼는 것이 핵심이다. 발 근처는 거의 안 움직이고 멀수록 크게 움직여
        /// 「발은 붙어 있는데 몸이 딸려온다」가 뼈대 없이 나온다.
        ///
        /// 세로로 늘 때 가로를 같은 비율로 줄이는 것(부피 보존)이 「말랑하다」로 읽히게 하는
        /// 거의 전부다. 이게 없으면 그냥 스프라이트가 커졌다 작아졌다 하는 것으로 보인다.
        ///
        /// 껍질은 단단해야 하므로 늘어나지 않는다. 몸이 껍질 높이에서 만든 변위·회전만
        /// 그대로 받아 통째로 따라간다.
        /// </summary>
        private void ApplyDeform()
        {
            if (_composed == null || _deform == null) return;

            _deform.Foot = _bounds.Foot;
            _deform.Stretch = _stretch;
            _deform.LeanDeg = _lean;
            _deform.HalfWidth = _visibleWidth * 0.5f;

            // 좌우 반전된 달팽이는 자식의 회전도 거울로 보이므로 각도를 미리 되돌린다
            _deform.Mirrored = _snail != null && _snail.localScale.x < 0f;

            StepCornerDeform();

            foreach (var s in _composed.Soft) s.Apply(_deform);

            var rigid = _composed.GroupOrNull(PartsLayer.RigidGroup);
            if (rigid != null)
            {
                // 껍질은 안 휜다. 껍질 중심이 간 자리로 통째로 옮기고 같이 기울기만 한다.
                _deform.RigidPose(_shellCenterLocalY, out var pos, out var rot);
                rigid.localScale = Vector3.one;
                rigid.localRotation = rot;
                rigid.localPosition = pos;
            }
        }

        /// <summary>
        /// 모서리에서 발바닥을 접는다. 지나온 벽과 갈 벽에 각각 눕혀 붙인다.
        ///
        /// 각도를 손으로 유도하지 않는 것이 요점이다. 네 벽 x 양방향 x 좌우반전이면
        /// 부호 조합이 열여섯 가지라 반드시 어딘가 틀린다. 대신 두 벽의 진행 방향을
        /// <b>달팽이 로컬 좌표로 역변환</b>해서 실제 각을 재면, 회전·반전·스케일이
        /// 트랜스폼에 이미 들어 있으므로 전부 저절로 맞는다.
        /// </summary>
        private void StepCornerDeform()
        {
            _deform.Cornering = false;
            if (!CornerFoldEnabled || _snail == null || SnailFree || _anchor.Turn <= 0f) return;

            BoxWalk.EdgeSegment(_box, _anchor.Edge, out var start, out var dir, out float len);
            Vector2 corner = _anchor.TurnToNext ? start + dir * len : start;

            BoxEdge to = _anchor.TurnToNext ? BoxWalk.Next(_anchor.Edge) : BoxWalk.Prev(_anchor.Edge);
            BoxWalk.EdgeSegment(_box, to, out _, out var dirTo, out _);

            // 뒤로 걸으면 두 벽 모두 진행 방향이 반대다
            Vector2 nearScreen = _anchor.TurnToNext ?  dir   : -dir;
            Vector2 farScreen  = _anchor.TurnToNext ?  dirTo : -dirTo;

            Vector2 bodyLocal = ScreenDirToLocal(corner, BoxWalk.Tangent(_box, _anchor));
            if (bodyLocal == Vector2.zero) return;

            Vector3 originLocal = _snail.InverseTransformPoint(VirtualToWorld(corner.x, corner.y));

            _deform.Cornering     = true;
            _deform.CornerX       = originLocal.x;
            _deform.CornerSpanX   = _visibleWidth * CornerSpanFraction;
            _deform.CornerFarSign = bodyLocal.x >= 0f ? 1f : -1f;
            _deform.CornerNearDeg = Vector2.SignedAngle(bodyLocal, ScreenDirToLocal(corner, nearScreen));
            _deform.CornerFarDeg  = Vector2.SignedAngle(bodyLocal, ScreenDirToLocal(corner, farScreen));
        }

        /// <summary>화면 방향 벡터를 달팽이 로컬 방향으로. 두 점을 옮겨 그 차이를 본다.</summary>
        private Vector2 ScreenDirToLocal(Vector2 at, Vector2 screenDir)
        {
            Vector3 a = _snail.InverseTransformPoint(VirtualToWorld(at.x, at.y));
            Vector3 b = _snail.InverseTransformPoint(
                VirtualToWorld(at.x + screenDir.x * 64f, at.y + screenDir.y * 64f));

            var d = new Vector2(b.x - a.x, b.y - a.y);
            return d.sqrMagnitude > 1e-9f ? d.normalized : Vector2.zero;
        }

        /// <summary>
        /// 벽에서 떨어진 발. 붙잡을 곳이 없어 축 늘어지고, 손을 움직이면 뒤늦게 따라온다.
        /// </summary>
        private void StepDangle(float deltaTime)
        {
            bool free = SnailFree;

            float depthWant = free ? (_bounds.Top - _bounds.Foot) * DangleDepthFraction : 0f;
            _deform.DangleDepth = Mathf.MoveTowards(_deform.DangleDepth, depthWant,
                                                    (_bounds.Top - _bounds.Foot) * 0.6f * deltaTime);

            // 손이 오른쪽으로 가면 발은 왼쪽에 남는다. 로컬 x 방향은 반전 여부에 달렸다.
            float toLocal = (_snail != null && _snail.localScale.x < 0f) ? -1f : 1f;
            float swayWant = free
                ? Mathf.Clamp(-_handVel.x / DangleSwayFullSpeed, -1f, 1f)
                  * _visibleWidth * DangleSwayFraction * toLocal
                : 0f;

            Spring(ref _sway, ref _swayVel, swayWant, deltaTime);
            _deform.DangleSway = free ? _sway : 0f;
        }

        /// <summary>
        /// 발바닥 물결. 기어갈 때 발바닥을 따라 근육 파동이 지나간다.
        ///
        /// 위상을 시간이 아니라 <b>이동 거리</b>로 돌리므로, 물결이 지나가는 속도와
        /// 달팽이가 나아가는 속도가 저절로 맞는다. 멈추면 물결도 멈춘다.
        /// </summary>
        private void StepFootWave(bool crawling, float deltaTime)
        {
            float speed = crawling ? WalkSpeed : 0f;

            // 로컬 단위로 환산해서 몸 크기가 바뀌어도 물결의 상대 크기가 유지되게 한다
            _deform.FootBand      = (_bounds.Top - _bounds.Foot) * FootBandFraction;
            _deform.WaveLength    = _visibleWidth * WaveLengthFraction;
            _deform.WaveDirection = _anchor.Forward ? 1f : -1f;

            float want = crawling ? _visibleWidth * WaveAmplitudeFraction : 0f;
            _deform.WaveAmplitude = Mathf.MoveTowards(_deform.WaveAmplitude, want,
                                                      _visibleWidth * 0.12f * deltaTime);

            if (speed > 0f && _deform.WaveLength > 0f)
                _deform.WavePhase += speed / (_scale * _deform.WaveLength) * deltaTime;
        }

        /// <summary>변형 그룹이 의도대로 나뉘었는지 확인용. 스켈레톤은 이 루트들에 붙는다.</summary>
        private static string DescribeGroups(SnailComposer.Composed c)
        {
            if (c == null) return "(없음)";
            var sb = new StringBuilder();
            foreach (var kv in c.Groups)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(kv.Value.name).Append(' ').Append(kv.Value.childCount).Append("장");
            }
            return sb.ToString();
        }

        /// <summary>실제로 걷는 속도. 데모 배수가 걸려 있으므로 이동과 출렁임이 같은 값을 봐야 한다.</summary>
        private float WalkSpeed => _growth.PixelsPerSecond * DemoSpeedScale;

        /// <summary>레벨이 바뀌면 크기를 다시 맞춘다. 속도는 매 프레임 읽으므로 여긴 안 건드린다.</summary>
        private void ApplyGrowth()
        {
            _scale = _growth.SizePixels / _visibleWidth;
        }

        private float _shownFull = -1f, _shownHappy = -1f;

        /// <summary>
        /// 포만도·행복도 막대를 따라가게 한다.
        /// 값이 눈에 띄게 바뀔 때만 다시 그린다 — 막대 너비를 매 프레임 건드리면
        /// UGUI 가 레이아웃을 통째로 다시 계산한다.
        /// </summary>
        private void RefreshGauges()
        {
            float full  = (float)_growth.FullPercent;
            float happy = (float)_growth.HappyPercent;

            if (Mathf.Abs(full - _shownFull) < 0.005f && Mathf.Abs(happy - _shownHappy) < 0.005f) return;

            _shownFull = full;
            _shownHappy = happy;
            _ui.SetGauges(full, happy);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            _cam.orthographicSize = Screen.height * 0.5f;

            if (_growth.Tick(Time.deltaTime, DemoTimeScale))
            {
                ApplyGrowth();
                RefreshSnail(reshoot: false);   // 나이(레벨)가 패널과 목록에 같이 나온다
                Say("      레벨업! → " + _growth);
            }

            RefreshGauges();

            // 버프가 걸리고 풀리는 순간을 놓치지 않게 변화만 기록한다
            string buffs = _growth.Buffs.Signature;
            if (buffs != _lastBuffs)
            {
                Say($"      버프 변화: {_lastBuffs} → {buffs}  " +
                    $"(포만 {_growth.FullPercent * 100:0}% 행복 {_growth.HappyPercent * 100:0}%)");
                _lastBuffs = buffs;
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // 매 프레임 다시 읽는다. 창 모드일 때 창을 옮기면 달팽이가 자동으로 따라붙고,
            // 화면 모드일 때는 해상도·모니터 구성이 바뀌어도 알아서 맞춰진다.
            _box = ResolveBox();

            float pxPerWorldY = _vHeight / Mathf.Max(1f, 2f * _cam.orthographicSize);
            float pxPerWorldX = _vWidth  / Mathf.Max(1f, 2f * _cam.orthographicSize * _cam.aspect);
            float px = (pxPerWorldX + pxPerWorldY) * 0.5f;   // 회전하면 두 축이 섞이므로 평균을 쓴다

            float footDepth = Mathf.Abs(_bounds.Foot) * _scale * px;
            float halfExtent = BoxWalk.HalfExtent(_bounds.Left * _scale * px, _bounds.Right * _scale * px);

            // 먹이는 어디에 놓아도 바닥까지 떨어진다
            DemoDropFood();
            _food.Tick(Time.deltaTime, _box.Bottom);
            UpdateFoodTransforms(px);

            StepDrag(footDepth, halfExtent);

            SnailPose pose;
            if (SnailFree)
            {
                UpdateFreeSnail(footDepth, halfExtent);

                // 들려 있거나 떨어지는 동안은 벽에 붙어 있지 않으므로 똑바로 세운다
                pose = new SnailPose
                {
                    RootScreen = _snailFootScreen - new Vector2(0f, footDepth),
                    RotationDeg = 0f,
                    FlipX = _anchor.Forward,
                    Valid = true,
                };
            }
            else
            {
                if (!_peeling) StepBehaviour(halfExtent, Time.deltaTime);   // 잡고 있는 동안엔 안 기어간다
                pose = BoxWalk.Evaluate(_box, _anchor, footDepth, halfExtent);
                LogTurn();
            }

            TickIncubator(Time.deltaTime);

            // 벽에 붙어 실제로 나아가고 있을 때만 발바닥에 물결이 지나간다
            StepFootWave(!SnailFree && !_peeling, Time.deltaTime);
            StepDangle(Time.deltaTime);

            if (pose.Valid)
            {
                _snail.position = VirtualToWorld(pose.RootScreen.x, pose.RootScreen.y);
                _snail.localRotation = Quaternion.Euler(0f, 0f, pose.RotationDeg);
                _snail.localScale = new Vector3(pose.FlipX ? -_scale : _scale, _scale, 1f);

                StepPresent(pose, footDepth, px);
            }

            // 반전 여부를 읽어야 하므로 루트 스케일이 정해진 뒤에 적용한다
            ApplyDeform();
#endif
            if (!_diagDone && _t > 1f) { LogDiagnostics(); _diagDone = true; }

            if (_t >= AutoQuitSeconds || Input.GetKeyDown(KeyCode.Escape))
            {
                // 리포트가 이미 나간 뒤에 저장하면 결과를 남길 수 없다. 먼저 적고 확인한다.
                SaveFile.Save(_player);
                Say("[8] 저장 ............. " + _player);
                WriteReport();
                Application.Quit();
            }
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        /// <summary>
        /// 배가 고프면 가장 가까운 먹이 쪽으로, 아니면 하던 방향으로 계속 기어간다.
        ///
        /// 달팽이는 벽에만 붙어 있고 먹이는 항상 바닥에 있으므로, 둘 다 「둘레 좌표」
        /// 위의 한 점이 된다. 어느 쪽으로 돌아야 가까운지가 뺄셈 한 번으로 정해진다.
        /// </summary>
        private void StepBehaviour(float halfExtent, float deltaTime)
        {
            bool hungry = _growth.FullPoint < _growth.Current.NeedFullPoint;
            _target = null;

            if (hungry && _food.Count > 0)
            {
                float myP = BoxWalk.ToPerimeter(_box, _anchor, halfExtent);
                _target = _food.FindNearestLanded(_box, myP, out float delta);

                if (_target != null)
                {
                    _anchor.Forward = delta >= 0f;                 // 가까운 쪽으로 방향을 튼다

                    // 몸이 먹이에 닿으면 먹는다
                    if (Mathf.Abs(delta) <= _target.HalfWidth + halfExtent * 0.5f)
                    {
                        Eat(_target);
                        return;
                    }
                }
            }

            _state = _target != null ? PetState.Seek : PetState.Wander;
            _anchor = BoxWalk.Advance(_box, _anchor, WalkSpeed, deltaTime, halfExtent);
        }

        /// <summary>모서리를 실제로 돌았는지 리포트에 남긴다. 화면을 못 볼 때 이것으로 확인한다.</summary>
        private bool _wasTurning;
        private void LogTurn()
        {
            bool turning = _anchor.Turn > 0f;
            if (turning == _wasTurning) return;
            _wasTurning = turning;

            if (turning)
            {
                var to = _anchor.TurnToNext ? BoxWalk.Next(_anchor.Edge) : BoxWalk.Prev(_anchor.Edge);
                Say($"      [{_t:0.0}s] 모서리 진입: {_anchor.Edge} → {to}");
            }
            else Say($"      [{_t:0.0}s] 모서리 통과 완료 → {_anchor.Edge} 벽");
        }

        private void Eat(FoodItem item)
        {
            _growth.Feed(item.Data.FullPoint, item.Data.HappyPoint, item.Data.BuffId);
            _food.Consume(item);
            _state = PetState.Eat;
            _eatFlashUntil = _t + 1.2f;

            Say($"      먹음: {Loc.ById(item.Data.NameId)} (+포만 {item.Data.FullPoint} +행복 {item.Data.HappyPoint}) → {_growth}");
        }

        // ── 부화기 ──

        private void RefreshEggs()
        {
            _ui.SetEggs(_player.Eggs.ToArray());
            _ui.SetIncubator(_player.Incubator);
        }

        /// <summary>목록의 알을 눌렀다. 빈 칸이 있으면 넣는다.</summary>
        private void PutEggInIncubator(int listIndex)
        {
            if (listIndex < 0 || listIndex >= _player.Eggs.Count) return;
            int eggId = _player.Eggs[listIndex];

            int slot = _player.PutEggInIncubator(listIndex);
            if (slot < 0) { Say("      [UI] 부화기가 가득 찼습니다"); return; }

            RefreshEggs();

            var row = GameData.EggDataById[eggId];
            Say($"      [UI] {Loc.ById(row.NameId)} 를 {slot}번 칸에 넣음 ({row.HatchTime}초)");
        }

        /// <summary>다 된 칸을 눌렀다. 아직이면 아무 일도 없다.</summary>
        private void ClaimHatched(int slot)
        {
            int eggId = _player.TakeHatched(slot);
            if (eggId == 0)
            {
                if (slot >= 0 && slot < _player.Incubator.Length && _player.Incubator[slot].eggId != 0)
                    Say($"      [UI] {slot}번 칸은 아직 {_player.Incubator[slot].remain:0}초 남았습니다");
                return;
            }

            if (!GameData.EggDataById.TryGetValue(eggId, out var egg)) return;

            var born = _player.AddSnail(SnailHatchery.Hatch(egg.Id, new System.Random()), egg.RarityType);
            RefreshEggs();
            RefreshSnail(reshoot: false);   // 화면에 나와 있는 개체는 그대로다

            Say($"      [UI] 부화! {GameData.TokenById[eggId]} → {born.Appearance}");
            Say($"      보유 달팽이 {_player.Snails.Count}마리");
        }

        /// <summary>부화 시간을 흘린다. 데모 배속을 그대로 쓴다 — 안 그러면 40초 안에 안 깬다.</summary>
        private void TickIncubator(float deltaTime)
        {
            if (_player.TickIncubator(deltaTime * DemoTimeScale))
                _ui.SetIncubator(_player.Incubator);
        }

        /// <summary>
        /// UI 의 「먹이기」. 즉시 먹이지 않고 화면에 떨어뜨린다.
        ///
        /// 달팽이가 배고프면 기어가서 먹는 흐름이 이미 있고, 떨어진 먹이는 드래그로
        /// 옮길 수도 있다. 즉시 먹이면 그게 다 사라진다.
        /// 떨어뜨리는 자리는 위젯을 피해 화면 가운데 위쪽으로 한다.
        /// </summary>
        private void DropFoodFromUi(int foodId)
        {
            if (!GameData.FoodDataById.TryGetValue(foodId, out var data))
            {
                Say($"      [UI] 먹이기: 알 수 없는 음식 {foodId}");
                return;
            }

            if (_player.Items.CountOf(foodId) <= 0)
            {
                Say($"      [UI] 먹이기: {Loc.ById(data.NameId)} 를 가지고 있지 않습니다");
                return;
            }

            float x = Mathf.Lerp(_box.Left, _box.Right, UnityEngine.Random.Range(0.3f, 0.6f));
            float y = _box.Top + UnityEngine.Random.Range(60f, 200f);

            // 떨어뜨리지 못하면 가방에서 빼지 않는다. 리소스가 없는 음식은 화면에 못 나온다.
            var dropped = _food.Drop(data, x, y);
            if (dropped == null)
            {
                Say($"      [UI] 먹이기: {Loc.ById(data.NameId)} 는 리소스가 없어 못 떨어뜨림");
                return;
            }

            _player.Items.Add(foodId, -1);
            RefreshFoods();
            Say($"      [UI] 먹이기: {Loc.ById(data.NameId)} 를 x={x:0} 에 떨어뜨림 " +
                $"(남은 {_player.Items.CountOf(foodId)}개)");
        }

        /// <summary>데모용. 실제로는 유저가 상점에서 사서 원하는 위치에 떨어뜨린다.</summary>
        private void DemoDropFood()
        {
            if (!DemoFoodEnabled || _t < _nextFoodAt) return;
            _nextFoodAt = _t + DemoFoodSeconds;

            // 아트가 있는 먹이만 놓을 수 있다
            if (_droppable == null)
            {
                var list = new System.Collections.Generic.List<FoodDataRow>();
                foreach (var f in GameData.FoodData)
                    if (!string.IsNullOrEmpty(f.ResourceKey)) list.Add(f);
                _droppable = list.ToArray();
                if (_droppable.Length == 0)
                    Say("      경고: ResourceKey 가 있는 먹이가 없어 아무것도 떨어뜨릴 수 없습니다.");
            }
            if (_droppable.Length == 0) return;

            var data = _droppable[UnityEngine.Random.Range(0, _droppable.Length)];
            float x = UnityEngine.Random.Range(_box.Left + 120f, _box.Right - 120f);
            float y = _box.Top + UnityEngine.Random.Range(60f, 260f);          // 공중에서 떨어뜨린다
            var dropped = _food.Drop(data, x, y);
            if (dropped != null) Say($"      먹이 투하: {Loc.ById(data.NameId)} @ x={x:0}");
        }

        /// <summary>먹이의 화면 좌표를 월드로 옮긴다. 바닥면이 ScreenY 에 오도록 보정한다.</summary>
        private void UpdateFoodTransforms(float pxPerWorld)
        {
            foreach (var f in _food.Items)
            {
                if (f.Root == null) continue;
                float bottomOffsetPx = _food.BottomOffsetOf(f) * pxPerWorld;   // 음수
                f.Root.position = VirtualToWorld(f.ScreenX, f.ScreenY + bottomOffsetPx);
            }
        }

        /// <summary>
        /// 집기·끌기·놓기.
        ///
        /// 창이 포커스를 갖지 않으므로 버튼 상태는 전역으로 읽는다.
        /// 놓으면 달팽이든 먹이든 <b>아래로만</b> 떨어진다. 벽에 스냅하지 않는다.
        /// </summary>
        private void StepDrag(float footDepth, float halfExtent)
        {
            bool down = TransparentWindow.IsLeftMouseDown();
            bool hasCursor = TransparentWindow.TryGetCursor(out int cx, out int cy);
            var cursor = new Vector2(cx, cy);

            // 손 속도. 프레임 단위로 튀므로 지수 평활을 한 겹 씌운다.
            if (hasCursor && _hasLastCursor && Time.deltaTime > 0f)
            {
                var raw = (cursor - _lastCursor) / Time.deltaTime;
                _handVel = Vector2.Lerp(_handVel, raw, 1f - Mathf.Exp(-18f * Time.deltaTime));
            }
            else if (!hasCursor) _handVel = Vector2.zero;
            _lastCursor = cursor;
            _hasLastCursor = hasCursor;

            // UI 를 누른 것을 달팽이·먹이를 집은 것으로 오해하면 안 된다.
            // 위젯이 화면 오른쪽 아래에 있어 달팽이와 겹치는 자리다.
            _cursorOnUi = hasCursor && _ui != null
                       && _ui.ContainsCursor(cx, cy, _vLeft, _vTop, _vHeight);

            if (down && !_wasMouseDown && hasCursor && !_cursorOnUi)
            {
                if (CursorOnSnail())
                {
                    // 선물이 준비돼 있으면 누른 순간 받는다. 그러고도 계속 집어 들 수 있다.
                    if (_present.Ready
                        && _present.TryClaim(_growth.Current, _player.Items, out int itemId, out int count))
                    {
                        _claimFlashUntil = _t + 1.5f;
                        _ui.SetCoin(_player.Coins);
                        string name = GameData.TokenById.TryGetValue(itemId, out string t) ? t : itemId.ToString();
                        Say($"      선물 수령: {name} x{count}  → 가방: {_player.Items}");
                    }

                    // 바로 들리지 않는다. 먼저 벽에서 떼어내야 한다.
                    if (!_snailFalling)
                    {
                        _peeling = true;
                        _grabScreen = cursor;
                        Say("      달팽이를 잡았습니다. 당기면 떨어집니다.");
                    }
                }
                else
                {
                    var f = _food.FindAt(cx, cy);
                    if (f != null)
                    {
                        _drag = DragTarget.Food;
                        _dragFood = f;
                        f.Held = true;
                        _grabOffset = new Vector2(f.ScreenX, f.ScreenY) - cursor;
                    }
                }
            }

            // ── 저항 단계: 발은 붙어 있고 몸만 딸려온다 ──
            _stretchTarget = 0f;
            _leanTarget = 0f;

            if (_peeling && hasCursor)
            {
                // 모서리를 도는 중에도 맞도록 벽이 아니라 앵커 기준으로 읽는다
                var n = BoxWalk.OutwardNormal(_anchor);
                var dir = BoxWalk.Tangent(_box, _anchor);
                Vector2 pull = cursor - _grabScreen;

                float away  = Vector2.Dot(pull, -n);      // 벽에서 멀어지는 성분
                float along = Vector2.Dot(pull, dir);     // 벽을 따라가는 성분

                _stretchTarget = Mathf.Clamp01(away / PeelThreshold) * PeelMaxStretch;
                _leanTarget = Mathf.Clamp(along / PeelThreshold, -1f, 1f) * -PeelMaxLeanDeg;

                // 커서가 얼마나 갔는지가 아니라 몸이 실제로 얼마나 늘어났는지로 판정한다.
                // 확 잡아채도 스프링이 따라잡을 때까지는 안 떨어지므로
                // 「쭉 늘어나는 것을 보고 나서 툭」이 항상 보인다.
                if (away >= PeelThreshold && _stretch >= PeelMaxStretch * 0.9f)
                {
                    _peeling = false;
                    _drag = DragTarget.Snail;
                    _snailFalling = false;
                    _snailVelY = 0f;
                    _snailFootScreen = CurrentFootScreen(footDepth, halfExtent);
                    _grabOffset = _snailFootScreen - cursor;
                    _stretchVel += PopRecoil * SpringStiffness * 0.05f;   // 툭 하고 되튕긴다
                    Say("      벽에서 떨어졌습니다.");
                }
            }
            else if (_drag == DragTarget.Snail)
            {
                // 들고 흔드는 중. 위로 채면 늘어지고, 내리누르면 눌린다.
                // 화면 y 는 아래가 +, 그래서 위로 가는 손은 -y 다.
                _stretchTarget = Mathf.Clamp(-_handVel.y * CarryStretchPerSpeed,
                                             -CarryMaxStretch * 0.7f, CarryMaxStretch);
                _leanTarget = Mathf.Clamp(_handVel.x * CarryLeanPerSpeed,
                                          -CarryMaxLeanDeg, CarryMaxLeanDeg);
            }
            else if (_snailFalling)
            {
                _stretchTarget = Mathf.Clamp(_snailVelY * FallStretchPerSpeed, 0f, FallMaxStretch);
            }
            else
            {
                // 벽에 붙어 기어가는 중
                float speed = WalkSpeed;
                _wobblePhase += speed * Time.deltaTime / WobbleWavelength;

                float amp = Mathf.Clamp01(speed / WobbleFullSpeed);
                float w = _wobblePhase * Mathf.PI * 2f;
                _stretchTarget = Mathf.Sin(w) * WobbleStretch * amp;
                _leanTarget    = Mathf.Cos(w) * WobbleLeanDeg * amp;

                // 모서리를 도는 동안은 진행 방향으로 기울며 눌린다.
                // _lean 이 음수면 머리가 진행 방향(+dir)으로 넘어간다.
                if (_anchor.Turn > 0f)
                {
                    float k = Mathf.Sin(Mathf.Clamp01(_anchor.Turn) * Mathf.PI);
                    _stretchTarget += TurnSquash * k;
                    _leanTarget    += (_anchor.TurnToNext ? -1f : 1f) * TurnLeanDeg * k;
                }
            }

            if (!down && _wasMouseDown && _peeling)
            {
                _peeling = false;                 // 덜 당기고 놓으면 그대로 붙어 있는다
                Say("      놓았습니다. 벽에 그대로 붙어 있습니다.");
            }

            if (!down && _wasMouseDown && _drag != DragTarget.None)
            {
                if (_drag == DragTarget.Snail)
                {
                    _snailFalling = true;      // 놓으면 아래로 떨어진다
                    _snailVelY = 0f;
                }
                else if (_dragFood != null)
                {
                    _dragFood.Held = false;
                    _dragFood.VelocityY = 0f;
                }
                _drag = DragTarget.None;
                _dragFood = null;
            }

            if (hasCursor)
            {
                if (_drag == DragTarget.Snail)
                {
                    _snailFootScreen = ClampFoot(cursor + _grabOffset, halfExtent);
                }
                else if (_drag == DragTarget.Food && _dragFood != null)
                {
                    var p = cursor + _grabOffset;
                    _dragFood.ScreenX = Mathf.Clamp(p.x, _box.Left + _dragFood.HalfWidth,
                                                         _box.Right - _dragFood.HalfWidth);
                    _dragFood.ScreenY = Mathf.Clamp(p.y, _box.Top + _dragFood.Height, _box.Bottom);
                }
            }

            _wasMouseDown = down;

            // 저항·늘어남·반동이 전부 이 스프링 하나에서 나온다.
            // 목표로 곧장 가지 않고 따라붙으므로 당길 때 저항이 생기고,
            // 목표가 0 으로 돌아가면 지나쳤다 돌아오며 출렁인다.
            Spring(ref _stretch, ref _stretchVel, _stretchTarget, Time.deltaTime);
            Spring(ref _lean,    ref _leanVel,    _leanTarget,    Time.deltaTime);

            // 세게 튕겼을 때 스프라이트가 뒤집히거나 납작해지지 않게만 막는다
            if (_stretch < MinStretch) { _stretch = MinStretch; if (_stretchVel < 0f) _stretchVel = 0f; }
            if (_stretch > MaxStretch) { _stretch = MaxStretch; if (_stretchVel > 0f) _stretchVel = 0f; }
        }

        private static void Spring(ref float value, ref float velocity, float target, float dt)
        {
            velocity += (target - value) * SpringStiffness * dt;
            velocity *= Mathf.Exp(-SpringDamping * dt);
            value += velocity * dt;
        }

        /// <summary>지금 벽에 붙어 있는 달팽이의 발 지점(가상 화면 px).</summary>
        private Vector2 CurrentFootScreen(float footDepth, float halfExtent)
        {
            var pose = BoxWalk.Evaluate(_box, _anchor, footDepth, halfExtent);
            return pose.RootScreen + BoxWalk.OutwardNormal(_anchor) * footDepth;
        }

        private Vector2 ClampFoot(Vector2 foot, float halfExtent)
        {
            foot.x = Mathf.Clamp(foot.x, _box.Left + halfExtent, _box.Right - halfExtent);
            foot.y = Mathf.Clamp(foot.y, _box.Top, _box.Bottom);
            return foot;
        }

        /// <summary>떨어지는 중이면 중력을 먹이고, 바닥에 닿으면 아래 벽에 다시 붙인다.</summary>
        private void UpdateFreeSnail(float footDepth, float halfExtent)
        {
            if (!_snailFalling) return;

            _snailVelY += Gravity * Time.deltaTime;
            _snailFootScreen.y += _snailVelY * Time.deltaTime;

            if (_snailFootScreen.y < _box.Bottom) return;

            // 착지 충격을 스프링에 속도로 꽂는다. 빨리 떨어질수록 크게 찌그러졌다 돌아온다.
            _stretchVel -= Mathf.Min(_snailVelY * LandingSquashPerSpeed, LandingSquashMax);

            _snailFootScreen.y = _box.Bottom;
            _snailVelY = 0f;
            _snailFalling = false;

            // 떨어진 자리에서 아래 벽에 붙는다
            float p = BoxWalk.BottomXToPerimeter(_box, _snailFootScreen.x);
            _anchor = BoxWalk.FromPerimeter(_box, p, _anchor.Forward, halfExtent);
            Say("      착지 → 아래 벽에 붙었습니다.");
        }

        /// <summary>
        /// 선물 타이머와 말풍선.
        ///
        /// 말풍선은 달팽이 머리 위 — 즉 벽에서 박스 안쪽으로 — 띄운다.
        /// 천장에 매달려 있어도 화면 안에 들어온다.
        ///
        /// 벽을 따라 같이 회전하므로 띄우는 간격도 항상 말풍선의 세로 반폭이 된다.
        /// (안 돌리면 옆벽에서는 가로 반폭을 써야 해서 벽마다 간격이 달라졌다.)
        /// </summary>
        private void StepPresent(SnailPose pose, float footDepth, float px)
        {
            _present.Tick(Time.deltaTime * DemoTimeScale, _growth.Current);

            var n = BoxWalk.OutwardNormal(_anchor);
            Vector2 foot = pose.RootScreen + n * footDepth;

            float bodyDepth = (_bounds.Top - _bounds.Foot) * _scale * px;   // 발에서 등까지
            float bubbleHalf = _present.HalfHeightWorld * px;
            Vector2 bubbleScreen = foot - n * (bodyDepth + BubbleGapPx + bubbleHalf);

            bool visible = _present.Ready;
            _present.Place(VirtualToWorld(bubbleScreen.x, bubbleScreen.y), pose.RotationDeg, visible);

            if (visible && !_bubbleLogged)
            {
                _bubbleLogged = true;
                Say($"      [{_t:0.0}s] 말풍선 표시: 발 화면({foot.x:0},{foot.y:0}) → 말풍선 화면({bubbleScreen.x:0},{bubbleScreen.y:0})");
                Say($"                    몸깊이 {bodyDepth:0} + 간격 {BubbleGapPx:0} + 반높이 {bubbleHalf:0} = {bodyDepth + BubbleGapPx + bubbleHalf:0}px 안쪽");
                Say($"                    {_present.Describe()}");
            }

            // 잡을 수 있는 것 위에 있거나 끌고 있는 동안에만 클릭 통과를 끈다.
            // 그 외에는 계속 통과시켜 바탕화면 작업을 방해하지 않는다.
            _cursorOnSnail = CursorOnSnail();
            bool hasCursor = TransparentWindow.TryGetCursor(out int cx, out int cy);
            bool onFood = hasCursor && _food.FindAt(cx, cy) != null;

            // _cursorOnUi 는 StepDrag 에서 이미 이번 프레임 값으로 갱신됐다
            TransparentWindow.SetClickThrough(
                !(_cursorOnSnail || onFood || _cursorOnUi || _drag != DragTarget.None));
        }

        private const float BubbleGapPx = SnailPresent.BubbleGap;
        private bool _bubbleLogged;

        /// <summary>
        /// 커서가 달팽이 몸통 위에 있는가.
        ///
        /// 달팽이는 벽에 따라 회전하고 진행 방향에 따라 좌우로 뒤집히며 레벨에 따라 크기가 변한다.
        /// 화면 좌표에서 그 셋을 다시 계산하는 대신, 커서를 달팽이 로컬 좌표로 역변환해
        /// 실측 몸통 경계와 그대로 비교한다. 회전·반전·스케일이 자동으로 반영된다.
        /// </summary>
        private bool CursorOnSnail()
        {
            if (_snail == null || !_bounds.Measured) return false;
            if (!TransparentWindow.TryGetCursor(out int cx, out int cy)) return false;

            Vector3 local = _snail.InverseTransformPoint(VirtualToWorld(cx, cy));
            return local.x >= _bounds.Left && local.x <= _bounds.Right
                && local.y >= _bounds.Foot && local.y <= _bounds.Top;
        }

        private ScreenRect ResolveBox()
        {
            var box = UseActiveWindowAsBox ? ActiveWindowBox.Resolve(TransparentWindow.Hwnd)
                                           : TransparentWindow.VirtualScreen;
            box.Left += DemoBoxInset; box.Right  -= DemoBoxInset;
            box.Top  += DemoBoxInset; box.Bottom -= DemoBoxInset;
            return box;
        }

        private static string BoxName =>
            UseActiveWindowAsBox ? ActiveWindowBox.CurrentTitle : "(화면 전체)";
#endif

        /// <summary>
        /// 가상 화면 px → 월드 좌표.
        /// 창 크기와 백버퍼 해상도가 어긋나 있어도 화면 안에 들어오도록
        /// 절대 픽셀이 아니라 0..1 정규화를 거쳐 카메라 범위에 매핑한다.
        /// </summary>
        private Vector3 VirtualToWorld(float sx, float sy)
        {
            float u = (sx - _vLeft) / Mathf.Max(1, _vWidth);
            float v = (sy - _vTop)  / Mathf.Max(1, _vHeight);
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            return new Vector3(Mathf.Lerp(-halfW, halfW, u), Mathf.Lerp(halfH, -halfH, v), 0f);
        }

        private void LogDiagnostics()
        {
            Say("");
            Say("[6] 렌더 진단");
            Say($"      Screen        : {Screen.width}x{Screen.height} (요청 {_vWidth}x{_vHeight})");
            Say($"      변형 그룹     : {DescribeGroups(_composed)}");
            Say($"      몸통 경계     : L{_bounds.Left:0} R{_bounds.Right:0} 발{_bounds.Foot:0} T{_bounds.Top:0} (스케일 전)");
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Say($"      박스          : {BoxName}  {_box}");
            Say($"      앵커          : {_anchor.Edge} T={_anchor.T:0.00} 회전={BoxWalk.RotationOf(_anchor.Edge)}도");
            Say($"      달팽이 위치   : {_snail.position}");
#endif
            WriteReport();
        }

        private void OnGUI()
        {
            float remain = Mathf.Max(0f, AutoQuitSeconds - _t);

            bool applied = false;
            string boxName = "-", edgeName = "-";
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            applied = TransparentWindow.Applied;
            boxName = BoxName;
            edgeName = _anchor.Turn > 0f
                ? _anchor.Edge + " → " + (_anchor.TurnToNext ? BoxWalk.Next(_anchor.Edge) : BoxWalk.Prev(_anchor.Edge))
                  + " 모서리 도는 중 " + (_anchor.Turn * 100f).ToString("0") + "%"
                : _anchor.Edge + " (회전 " + BoxWalk.RotationOf(_anchor.Edge) + "도)";
#endif
            if (!applied)
            {
                var warn = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22, wordWrap = true,
                    normal = { textColor = new Color(1f, 0.45f, 0.45f) }
                };
                GUI.color = new Color(0.25f, 0f, 0f, 0.9f);
                GUI.DrawTexture(new Rect(20, 20, 1000, 130), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(36, 32, 970, 110),
                    "투명 창이 적용되지 않았습니다.\n" +
#if UNITY_EDITOR
                    "에디터 Play 모드에서는 확인할 수 없습니다. 메뉴 SnailPet → 2. 빌드 & 실행 으로 확인하세요.\n" +
                    "(배경이 검은 것은 정상입니다)",
#else
                    "빌드된 플레이어인데 실패했습니다. unity-probe-result.txt 를 확인하세요.",
#endif
                    warn);
            }

            // HUD 는 화면 하단에 둔다. 위에 두면 달팽이가 위쪽 벽을 걸을 때 가린다.
            var style = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            float h = 108f;
            float y = Screen.height - h - 20f;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(20, y, 980, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(32, y + 6,  960, 22), _status, style);
            GUI.Label(new Rect(32, y + 28, 960, 22), _growth.ToString(), style);
            string act = _state switch
            {
                PetState.Seek => "먹이로 이동 중" + (_target != null ? " (" + Loc.ById(_target.Data.NameId) + ")" : ""),
                PetState.Eat  => "먹는 중",
                _             => (_growth.FullPoint < _growth.Current.NeedFullPoint ? "배고픔 (먹이 없음)" : "배회 중"),
            };
            if (_t < _eatFlashUntil) act = "냠냠!";
            if (_t < _claimFlashUntil) act = "선물 획득!";
            if (_drag == DragTarget.Snail)     act = "들려 있음";
            else if (_drag == DragTarget.Food) act = "먹이 옮기는 중";
            else if (_snailFalling)            act = "떨어지는 중";
            else if (_peeling)                 act = $"떼는 중 ({_stretch / PeelMaxStretch * 100:0}%)";
            GUI.Label(new Rect(32, y + 50, 960, 22),
                act + "   |   벽: " + edgeName + "   먹이 " + (_food != null ? _food.Count : 0) + "개", style);
            GUI.Label(new Rect(32, y + 72, 960, 22),
                (_present != null ? _present + "   가방: " + _player.Items + "   |   " : "") +
                "자동 종료까지 " + remain.ToString("0.0") + "초 (ESC)", style);
        }

        private void WriteReport()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                File.WriteAllText(Path.Combine(dir, "unity-probe-result.txt"),
                                  _log.ToString(), new UTF8Encoding(false));
            }
            catch { /* 리포트 저장 실패는 결과에 영향 없음 */ }
        }
    }
}
