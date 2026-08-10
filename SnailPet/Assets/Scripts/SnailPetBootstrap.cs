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
        private Inventory _inventory;
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

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bool ok = TransparentWindow.Apply(clickThrough: true);
            Say("[3] 투명 창 적용 ..... " + (ok ? "OK" : "실패: " + TransparentWindow.LastError));
            Say("[4] 클릭 통과 ....... " + (TransparentWindow.IsClickThrough() ? "OK" : "미적용"));

            _anchor = new BoxAnchor { Edge = BoxEdge.Bottom, T = 0.05f, Forward = true };
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
            var eggs = GameData.EggData;
            var egg = eggs[rng.Next(eggs.Length)];
            _appearance = SnailHatchery.Hatch(egg.Id, rng);

            Say("[1] 부화 ............. " + GameData.TokenById[egg.Id] + " (" + egg.RarityType + ")");
            Say("      " + _appearance);

            _composed = SnailComposer.Build(_appearance);
            _composed.Root.transform.SetParent(transform, false);
            _snail = _composed.Root.transform;

            _bounds = SnailMetrics.Measure(_appearance);
            _visibleWidth = _bounds.Right - _bounds.Left;
            if (!_bounds.Measured || _visibleWidth < 0.01f) _visibleWidth = 1f;

            _shellCenterLocalY = MeasureShellCenterY();

            _growth = new SnailGrowth();
            ApplyGrowth();
            _food = new FoodField(transform);
            _inventory = new Inventory();
            _present = new SnailPresent(transform);

            Say("[2] 성장 ............. " + _growth);
            Say(string.Format("      환산: Speed 1 = {0}px/s, Size 1 = {1}px  (레벨 1~{2})",
                SnailGrowth.PixelsPerSpeedUnit, SnailGrowth.PixelsPerSizeUnit, SnailGrowth.MaxLevel));
            var top = GameData.LevelData[GameData.LevelData.Length - 1];
            Say(string.Format("      최고 레벨: 속도 {0}px/s, 크기 {1}px",
                top.Speed * SnailGrowth.PixelsPerSpeedUnit, top.Size * SnailGrowth.PixelsPerSizeUnit));
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
            if (_composed == null) return;

            float sy = 1f + _stretch;
            float sx = 1f / Mathf.Sqrt(Mathf.Max(0.2f, sy));

            // 좌우 반전된 달팽이는 자식의 회전도 거울로 보이므로 각도를 미리 되돌린다
            float deg = _lean * (_snail != null && _snail.localScale.x < 0f ? -1f : 1f);

            var rot   = Quaternion.Euler(0f, 0f, deg);
            var scale = new Vector3(sx, sy, 1f);
            var foot  = new Vector3(0f, _bounds.Foot, 0f);

            // 발 f 를 중심으로 늘리고 기울인다:  p' = R*(S*(p-f)) + f
            var soft = _composed.GroupOrNull(PartsLayer.DeformGroupOf(PartsType.Body));
            if (soft != null)
            {
                soft.localScale = scale;
                soft.localRotation = rot;
                soft.localPosition = foot - rot * Vector3.Scale(scale, foot);
            }

            var rigid = _composed.GroupOrNull(PartsLayer.RigidGroup);
            if (rigid != null)
            {
                // 껍질 중심이 몸을 따라간 자리로 강체 이동. 스케일은 주지 않는다.
                var c = new Vector3(0f, _shellCenterLocalY, 0f);
                Vector3 moved = rot * Vector3.Scale(scale, c - foot) + foot;
                rigid.localScale = Vector3.one;
                rigid.localRotation = rot;
                rigid.localPosition = moved - rot * c;
            }
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
            }

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
            _growth.Feed(item.Data.FullPoint, item.Data.HappyPoint, item.Data.BuffId);
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

            if (down && !_wasMouseDown && hasCursor)
            {
                if (CursorOnSnail())
                {
                    // 선물이 준비돼 있으면 누른 순간 받는다. 그러고도 계속 집어 들 수 있다.
                    if (_present.Ready
                        && _present.TryClaim(_growth.Current, _inventory, out int itemId, out int count))
                    {
                        _claimFlashUntil = _t + 1.5f;
                        string name = GameData.TokenById.TryGetValue(itemId, out string t) ? t : itemId.ToString();
                        Say($"      선물 수령: {name} x{count}  → 가방: {_inventory}");
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
                var n = BoxWalk.OutwardNormal(_anchor.Edge);
                BoxWalk.EdgeSegment(_box, _anchor.Edge, out _, out var dir, out _);
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
            return pose.RootScreen + BoxWalk.OutwardNormal(_anchor.Edge) * footDepth;
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
        /// 천장에 매달려 있어도 화면 안에 들어오고, 회전은 주지 않아 항상 똑바로 선다.
        /// </summary>
        private void StepPresent(SnailPose pose, float footDepth, float px)
        {
            _present.Tick(Time.deltaTime * DemoTimeScale, _growth.Current);

            var n = BoxWalk.OutwardNormal(_anchor.Edge);
            Vector2 foot = pose.RootScreen + n * footDepth;

            float bodyDepth = (_bounds.Top - _bounds.Foot) * _scale * px;   // 발에서 등까지
            float bubbleHalf = _present.HalfHeightWorld * px;
            Vector2 bubbleScreen = foot - n * (bodyDepth + BubbleGapPx + bubbleHalf);

            bool visible = _present.Ready;
            _present.Place(VirtualToWorld(bubbleScreen.x, bubbleScreen.y), visible);

            // 잡을 수 있는 것 위에 있거나 끌고 있는 동안에만 클릭 통과를 끈다.
            // 그 외에는 계속 통과시켜 바탕화면 작업을 방해하지 않는다.
            _cursorOnSnail = CursorOnSnail();
            bool onFood = TransparentWindow.TryGetCursor(out int cx, out int cy)
                       && _food.FindAt(cx, cy) != null;

            TransparentWindow.SetClickThrough(!(_cursorOnSnail || onFood || _drag != DragTarget.None));
        }

        private const float BubbleGapPx = SnailPresent.BubbleGap;

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
            if (_t < _claimFlashUntil) act = "선물 획득!";
            if (_drag == DragTarget.Snail)     act = "들려 있음";
            else if (_drag == DragTarget.Food) act = "먹이 옮기는 중";
            else if (_snailFalling)            act = "떨어지는 중";
            else if (_peeling)                 act = $"떼는 중 ({_stretch / PeelMaxStretch * 100:0}%)";
            GUI.Label(new Rect(32, y + 50, 960, 22),
                act + "   |   벽: " + edgeName + "   먹이 " + (_food != null ? _food.Count : 0) + "개", style);
            GUI.Label(new Rect(32, y + 72, 960, 22),
                (_present != null ? _present + "   가방: " + _inventory + "   |   " : "") +
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
