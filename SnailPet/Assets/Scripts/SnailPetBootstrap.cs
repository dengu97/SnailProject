using System;
using System.IO;
using System.Text;
using SnailPet.Data;
using SnailPet.Snail;
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

        /// <summary>데모용. 이 간격마다 먹이를 하나 떨어뜨린다.</summary>
        private const float DemoFoodSeconds = 5f;

        /// <summary>레벨업 3600초·포만 감소 120초를 눈으로 보려면 시간을 당겨야 한다. 40초 실행 = 약 80분.</summary>
        private const float DemoTimeScale = 120f;

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

        private enum PetState { Wander, Seek, Eat }
        private PetState _state = PetState.Wander;
        private FoodField _food;
        private FoodItem _target;
        private FoodDataRow[] _droppable;
        private float _nextFoodAt = 2f;
        private float _eatFlashUntil;

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

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bool ok = TransparentWindow.Apply(clickThrough: true);
            Say("[3] 투명 창 적용 ..... " + (ok ? "OK" : "실패: " + TransparentWindow.LastError));
            Say("[4] 클릭 통과 ....... " + (TransparentWindow.IsClickThrough() ? "OK" : "미적용"));

            _anchor = new BoxAnchor { Edge = BoxEdge.Bottom, T = 0.05f, Forward = true };
            _box = ResolveBox();
            Say("[5] 박스 ............ " + BoxName + "  " + _box);
#endif
            _status = "화면 안쪽 테두리를 따라 한 바퀴 돕니다.";
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
            var eggs = GameData.EggData;
            var egg = eggs[rng.Next(eggs.Length)];
            _appearance = SnailHatchery.Hatch(egg.Id, rng);

            Say("[1] 부화 ............. " + GameData.TokenById[egg.Id] + " (" + egg.RarityType + ")");
            Say("      " + _appearance);

            var root = SnailComposer.Build(_appearance);
            root.transform.SetParent(transform, false);
            _snail = root.transform;

            _bounds = SnailMetrics.Measure(_appearance);
            _visibleWidth = _bounds.Right - _bounds.Left;
            if (!_bounds.Measured || _visibleWidth < 0.01f) _visibleWidth = 1f;

            _growth = new SnailGrowth();
            ApplyGrowth();
            _food = new FoodField(transform);

            Say("[2] 성장 ............. " + _growth);
            Say(string.Format("      환산: Speed 1 = {0}px/s, Size 1 = {1}px  (레벨 1~{2})",
                SnailGrowth.PixelsPerSpeedUnit, SnailGrowth.PixelsPerSizeUnit, SnailGrowth.MaxLevel));
            var top = GameData.LevelData[GameData.LevelData.Length - 1];
            Say(string.Format("      최고 레벨: 속도 {0}px/s, 크기 {1}px",
                top.Speed * SnailGrowth.PixelsPerSpeedUnit, top.Size * SnailGrowth.PixelsPerSizeUnit));
        }

        /// <summary>레벨이 바뀌면 크기를 다시 맞춘다. 속도는 매 프레임 읽으므로 여기선 안 건드린다.</summary>
        private void ApplyGrowth()
        {
            _scale = _growth.SizePixels / _visibleWidth;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            _cam.orthographicSize = Screen.height * 0.5f;

            if (_growth.Tick(Time.deltaTime, DemoTimeScale))
            {
                ApplyGrowth();
                Say("      레벨업! → " + _growth);
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

            StepBehaviour(halfExtent, Time.deltaTime);

            var pose = BoxWalk.Evaluate(_box, _anchor, footDepth, halfExtent);
            if (pose.Valid)
            {
                _snail.position = VirtualToWorld(pose.RootScreen.x, pose.RootScreen.y);
                _snail.localRotation = Quaternion.Euler(0f, 0f, pose.RotationDeg);
                _snail.localScale = new Vector3(pose.FlipX ? -_scale : _scale, _scale, 1f);
            }
#endif
            if (!_diagDone && _t > 1f) { LogDiagnostics(); _diagDone = true; }

            if (_t >= AutoQuitSeconds || Input.GetKeyDown(KeyCode.Escape))
            {
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
            _anchor = BoxWalk.Advance(_box, _anchor, _growth.PixelsPerSecond, deltaTime, halfExtent);
        }

        private void Eat(FoodItem item)
        {
            _growth.Feed(item.Data.FullPoint, item.Data.HappyPoint);
            _food.Consume(item);
            _state = PetState.Eat;
            _eatFlashUntil = _t + 1.2f;

            Say($"      먹음: {item.Data.Name} (+포만 {item.Data.FullPoint} +행복 {item.Data.HappyPoint}) → {_growth}");
        }

        /// <summary>데모용. 실제로는 유저가 상점에서 사서 원하는 위치에 떨어뜨린다.</summary>
        private void DemoDropFood()
        {
            if (_t < _nextFoodAt) return;
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
            if (dropped != null) Say($"      먹이 투하: {data.Name} @ x={x:0}");
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

        private ScreenRect ResolveBox() =>
            UseActiveWindowAsBox ? ActiveWindowBox.Resolve(TransparentWindow.Hwnd)
                                 : TransparentWindow.VirtualScreen;

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
            Say($"      레이어 수     : {_snail.childCount}장");
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
            edgeName = _anchor.Edge + " (회전 " + BoxWalk.RotationOf(_anchor.Edge) + "도)";
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
                PetState.Seek => "먹이로 이동 중" + (_target != null ? " (" + _target.Data.Name + ")" : ""),
                PetState.Eat  => "먹는 중",
                _             => (_growth.FullPoint < _growth.Current.NeedFullPoint ? "배고픔 (먹이 없음)" : "배회 중"),
            };
            if (_t < _eatFlashUntil) act = "냠냠!";
            GUI.Label(new Rect(32, y + 50, 960, 22),
                act + "   |   벽: " + edgeName + "   먹이 " + (_food != null ? _food.Count : 0) + "개", style);
            GUI.Label(new Rect(32, y + 72, 960, 22),
                "자동 종료까지 " + remain.ToString("0.0") + "초 (ESC 로 즉시 종료)", style);
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
