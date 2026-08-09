using System;
using System.IO;
using System.Text;
using SnailPet.Data;
using SnailPet.Snail;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using SnailPet.Desktop;
#endif

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
        private const float AutoQuitSeconds = 25f;

        private const float SnailPixels = 220f;   // 화면에 보일 달팽이 가로 크기(px)
        private const float WalkSeconds = 12f;    // 표면 한 번 횡단하는 시간

        private readonly StringBuilder _log = new StringBuilder();
        private Camera _cam;
        private Transform _snail;
        private float _t;
        private bool _diagDone;

        private SnailAppearance _appearance;
        private SnailBounds _bounds;      // 스케일 적용 전 (월드 단위)
        private float _scale = 1f;

        private int _vLeft, _vTop, _vWidth, _vHeight;
        private float _walkFrom, _walkTo, _walkY;
        private string _walkTitle = "(없음)";
        private string _status = "";

        private void Say(string s) { _log.AppendLine(s); Debug.Log(s); }

        private void Awake()
        {
            Application.runInBackground = true;
            Say("=== SnailPet ===");
            Say("Unity " + Application.unityVersion + " / " + SystemInfo.operatingSystem);
            Say("그래픽 API: " + SystemInfo.graphicsDeviceType);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var v = TransparentWindow.VirtualScreen;
            _vLeft = v.Left; _vTop = v.Top; _vWidth = v.Width; _vHeight = v.Height;
            Say("가상 화면: " + v);
#else
            _vLeft = 0; _vTop = 0; _vWidth = Screen.width; _vHeight = Screen.height;
            Say("Windows 가 아니므로 투명 창을 적용하지 않습니다.");
#endif

            // 창만 키우고 렌더 해상도를 두면 백버퍼가 기본값으로 남아 좌표 매핑이 틀어진다
            Screen.SetResolution(_vWidth, _vHeight, FullScreenMode.Windowed);

            SetupCamera();
            SetupSnail();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bool ok = TransparentWindow.Apply(clickThrough: true);
            Say("[2] 투명 창 적용 ..... " + (ok ? "OK" : "실패: " + TransparentWindow.LastError));
            Say("[3] 클릭 통과 ....... " + (TransparentWindow.IsClickThrough() ? "OK" : "미적용"));

            var surfaces = WindowSurfaces.Collect(TransparentWindow.Hwnd);
            Say("[4] 창 표면 수집 .... " + surfaces.Count + "개");
            for (int i = 0; i < Mathf.Min(6, surfaces.Count); i++) Say("      " + surfaces[i]);

            Surface best = null;
            foreach (var s in surfaces) if (best == null || s.Width > best.Width) best = s;
            if (best != null)
            {
                _walkFrom = best.Left; _walkTo = best.Right; _walkY = best.Top;
                _walkTitle = best.Title;
            }
            else PickWalkFallback();
#else
            PickWalkFallback();
#endif
            _status = "달팽이 뒤로 바탕화면이 보이면 성공. 검은 사각형이면 실패.";
            Say("");
            Say("→ " + _walkTitle + " 위를 걸어갑니다. " + AutoQuitSeconds + "초 뒤 자동 종료.");
            WriteReport();
        }

        private void PickWalkFallback()
        {
            _walkFrom = _vLeft;
            _walkTo = _vLeft + _vWidth;
            _walkY = _vTop + _vHeight - 80;
            _walkTitle = "(화면 하단)";
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

        /// <summary>데이터에서 알을 하나 골라 부화시키고, 그 결과를 합성한다.</summary>
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
            float visibleWidth = _bounds.Right - _bounds.Left;
            _scale = (_bounds.Measured && visibleWidth > 0.01f) ? SnailPixels / visibleWidth : 1f;
            _snail.localScale = new Vector3(_scale, _scale, 1f);

            Say(string.Format("      몸통 실측: 가로 {0:0}px, 발선 {1:0.0} (스케일 {2:0.000})",
                visibleWidth * _scale, _bounds.Foot * _scale, _scale));
        }

        private void Update()
        {
            _t += Time.deltaTime;
            _cam.orthographicSize = Screen.height * 0.5f;

            // 월드 → 가상 화면 px 환산 (해상도가 어긋나 있어도 맞도록 매번 계산)
            float pxPerWorldY = _vHeight / Mathf.Max(1f, 2f * _cam.orthographicSize);
            float pxPerWorldX = _vWidth  / Mathf.Max(1f, 2f * _cam.orthographicSize * _cam.aspect);

            float leftPx   = _bounds.Left  * _scale * pxPerWorldX;   // 음수
            float rightPx  = _bounds.Right * _scale * pxPerWorldX;   // 양수
            float footPx   = _bounds.Foot  * _scale * pxPerWorldY;   // 음수
            float topPx    = _bounds.Top   * _scale * pxPerWorldY;   // 양수

            // 가로: 걷는 구간을 몸 크기만큼 안쪽으로 들이고, 화면 밖으로도 못 나가게
            float screenMinX = _vLeft - leftPx;
            float screenMaxX = _vLeft + _vWidth - rightPx;
            float from = Mathf.Clamp(_walkFrom - leftPx,  screenMinX, screenMaxX);
            float to   = Mathf.Clamp(_walkTo   - rightPx, screenMinX, screenMaxX);
            if (to < from)
            {
                float mid = Mathf.Clamp((_walkFrom + _walkTo) * 0.5f, screenMinX, screenMaxX);
                from = to = mid;
            }

            float phase = Mathf.PingPong(_t / WalkSeconds, 1f);
            float sx = Mathf.Lerp(from, to, phase);

            // 세로: 발이 표면 위에 놓이도록. 화면 위/아래로도 못 나가게.
            // 화면 y 는 아래로, 월드 y 는 위로 증가하므로 부호가 뒤집힌다.
            float sy = Mathf.Clamp(_walkY + footPx, _vTop + topPx, _vTop + _vHeight + footPx);

            _snail.position = VirtualToWorld(sx, sy);

            // 진행 방향으로 뒤집기 (달팽이 아트는 왼쪽을 본다)
            float dir = Mathf.Sin(Mathf.PI * 2f * (_t / (WalkSeconds * 2f)));
            var s = _snail.localScale;
            s.x = Mathf.Abs(s.x) * (dir >= 0 ? -1f : 1f);
            _snail.localScale = s;

            if (!_diagDone && _t > 1f) { LogDiagnostics(); _diagDone = true; }

            if (_t >= AutoQuitSeconds || Input.GetKeyDown(KeyCode.Escape))
            {
                WriteReport();
                Application.Quit();
            }
        }

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
            var p = _snail.position;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            bool inside = Mathf.Abs(p.x) <= halfW && Mathf.Abs(p.y) <= halfH;

            Say("");
            Say("[5] 렌더 진단");
            Say($"      Screen        : {Screen.width}x{Screen.height} (요청 {_vWidth}x{_vHeight})");
            Say($"      카메라        : ortho={halfH:0.0} aspect={_cam.aspect:0.000}");
            Say($"      달팽이 위치   : ({p.x:0.0}, {p.y:0.0})  {(inside ? "화면 안" : "화면 밖 ← 원인")}");
            Say($"      레이어 수     : {_snail.childCount}장");
            Say($"      몸통 경계     : {(_bounds.Measured ? "실측" : "미측정")} " +
                $"L{_bounds.Left:0} R{_bounds.Right:0} 발{_bounds.Foot:0} T{_bounds.Top:0} (스케일 전)");
            Say($"      걷는 구간     : x={_walkFrom:0}..{_walkTo:0}, y={_walkY:0}");
            WriteReport();
        }

        private void OnGUI()
        {
            float remain = Mathf.Max(0f, AutoQuitSeconds - _t);

            bool applied = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            applied = TransparentWindow.Applied;
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

            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
            float y = applied ? 20f : 165f;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(20, y, 940, 112), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(32, y + 8,  920, 24), _status, style);
            GUI.Label(new Rect(32, y + 30, 920, 24), "부화 결과: " + _appearance, style);
            GUI.Label(new Rect(32, y + 54, 920, 24), "걷는 중: " + _walkTitle, style);
            GUI.Label(new Rect(32, y + 78, 920, 24),
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
