using System;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using SnailPet.Desktop;
#endif

namespace SnailPet
{
    /// <summary>
    /// 씬을 따로 만들지 않고 코드로 전부 구성한다.
    /// 스파이크 단계에서는 씬 에셋을 손으로 편집할 이유가 없고, 씬이 없어도 빌드가 되게 하려는 목적.
    ///
    /// 확인하려는 것: Unity 플레이어 창에서 per-pixel alpha 가 살아나는가.
    /// (OS 레벨에서 되는 것은 Tools/spike 에서 이미 5/5 로 확인됨)
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

        private const float SnailPixels = 200f;   // 화면에 보일 달팽이 크기(px)
        private const float WalkSeconds = 12f;    // 표면 한 번 횡단하는 시간

        private readonly StringBuilder _log = new StringBuilder();
        private Camera _cam;
        private Transform _snail;
        private float _t;

        private int _vLeft, _vTop, _vWidth, _vHeight;
        private float _walkFrom, _walkTo, _walkY;
        private string _walkTitle = "(없음)";
        private string _status = "";

        private void Say(string s) { _log.AppendLine(s); Debug.Log(s); }

        private void Awake()
        {
            Application.runInBackground = true;
            Say("=== Unity 투명 창 스파이크 ===");
            Say("Unity " + Application.unityVersion + " / " + SystemInfo.operatingSystem);
            Say("그래픽 API: " + SystemInfo.graphicsDeviceType);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var v = TransparentWindow.VirtualScreen;
            _vLeft = v.Left; _vTop = v.Top; _vWidth = v.Width; _vHeight = v.Height;
            Say(string.Format("가상 화면: x={0}..{1}, y={2}..{3} ({4}x{5})",
                v.Left, v.Right, v.Top, v.Bottom, v.Width, v.Height));
#else
            _vLeft = 0; _vTop = 0; _vWidth = Screen.width; _vHeight = Screen.height;
            Say("Windows 가 아니므로 투명 창을 적용하지 않습니다.");
#endif

            // 창만 키우고 렌더 해상도를 그대로 두면 백버퍼가 기본값(1280x720)으로 남아
            // 종횡비가 어긋나고 좌표 매핑이 통째로 틀어진다. 창 크기와 반드시 맞춰야 한다.
            Screen.SetResolution(_vWidth, _vHeight, FullScreenMode.Windowed);
            Say(string.Format("해상도 요청: {0}x{1} (현재 {2}x{3}, 실제 적용은 다음 프레임)",
                _vWidth, _vHeight, Screen.width, Screen.height));

            SetupCamera();
            SetupSnail();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            bool ok = TransparentWindow.Apply(clickThrough: true);
            Say("[1] 투명 창 적용 ..... " + (ok ? "OK" : "실패: " + TransparentWindow.LastError));
            Say("[2] 클릭 통과 ....... " + (TransparentWindow.IsClickThrough() ? "OK" : "미적용"));

            var surfaces = WindowSurfaces.Collect(TransparentWindow.Hwnd);
            Say("[3] 창 표면 수집 .... " + (surfaces.Count > 0 ? "OK" : "0개") + "  (" + surfaces.Count + "개)");
            for (int i = 0; i < Mathf.Min(8, surfaces.Count); i++) Say("      " + surfaces[i]);

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
            _status = "달팽이 뒤로 바탕화면이 그대로 보이면 성공. 검은 사각형이 보이면 실패.";
            Say("");
            Say("→ " + _walkTitle + " 위를 걸어갑니다. " + AutoQuitSeconds + "초 뒤 자동 종료.");
            WriteReport();
        }

        /// <summary>걸어다닐 창을 못 찾았을 때는 화면 하단을 쓴다.</summary>
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
            // 1 world unit = 1 px. 해상도 변경은 다음 프레임에 반영되므로 Update 에서 매번 다시 맞춘다.
            _cam.orthographicSize = Screen.height * 0.5f;
            _cam.transform.position = new Vector3(0, 0, -10f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);   // 알파 0 이 핵심
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
        }

        private void SetupSnail()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "snail_preview.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = false;
            try
            {
                if (File.Exists(path)) loaded = tex.LoadImage(File.ReadAllBytes(path));
                else Say("스프라이트 없음: " + path);
            }
            catch (Exception e) { Say("스프라이트 로드 실패: " + e.Message); }

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Say("[0] 스프라이트 ....... " + (loaded ? "OK (" + tex.width + "x" + tex.height + ")" : "실패"));

            var go = new GameObject("Snail");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                      new Vector2(0.5f, 0.5f), 1f);
            float scale = SnailPixels / Mathf.Max(1, tex.width);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            _snail = go.transform;
        }

        private void Update()
        {
            _t += Time.deltaTime;

            // 해상도 변경이 반영되는 시점이 한 프레임 뒤라, 매 프레임 다시 맞춘다
            _cam.orthographicSize = Screen.height * 0.5f;

            // 표면 위를 왕복
            float phase = Mathf.PingPong(_t / WalkSeconds, 1f);
            float sx = Mathf.Lerp(_walkFrom, _walkTo, phase);

            // 창이 화면 맨 위에 붙어 있으면(y=0) 그 위에 올려놓는 순간 화면 밖으로 나간다.
            // 항상 보이도록 가상 화면 안으로 가둔다.
            float half = SnailPixels * 0.5f;
            float sy = Mathf.Clamp(_walkY - SnailPixels * 0.42f,
                                   _vTop + half, _vTop + _vHeight - half);

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
        /// 창 크기와 백버퍼 해상도가 어긋나 있어도 화면 안에 들어오도록,
        /// 절대 픽셀이 아니라 0..1 정규화 좌표를 거쳐 카메라 범위에 매핑한다.
        /// </summary>
        private Vector3 VirtualToWorld(float sx, float sy)
        {
            float u = (sx - _vLeft) / Mathf.Max(1, _vWidth);    // 0..1 (좌 → 우)
            float v = (sy - _vTop)  / Mathf.Max(1, _vHeight);   // 0..1 (상 → 하)
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            return new Vector3(Mathf.Lerp(-halfW, halfW, u), Mathf.Lerp(halfH, -halfH, v), 0f);
        }

        private bool _diagDone;

        /// <summary>안 보일 때 원인을 찾을 수 있도록 실제 수치를 남긴다.</summary>
        private void LogDiagnostics()
        {
            var p = _snail.position;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            bool inside = Mathf.Abs(p.x) <= halfW && Mathf.Abs(p.y) <= halfH;
            var sr = _snail.GetComponent<SpriteRenderer>();

            Say("");
            Say("[4] 렌더 진단");
            Say(string.Format("      Screen        : {0}x{1} (요청 {2}x{3})",
                Screen.width, Screen.height, _vWidth, _vHeight));
            Say(string.Format("      카메라        : ortho={0:0.0} aspect={1:0.000} → 가시범위 x±{2:0} y±{3:0}",
                halfH, _cam.aspect, halfW, halfH));
            Say(string.Format("      달팽이 위치   : ({0:0.0}, {1:0.0})  {2}",
                p.x, p.y, inside ? "화면 안" : "화면 밖 ← 원인"));
            Say(string.Format("      스프라이트    : bounds={0} 보임={1}",
                sr.bounds.size, sr.isVisible));
            Say(string.Format("      걷는 구간     : x={0:0}..{1:0}, y={2:0}", _walkFrom, _walkTo, _walkY));
            WriteReport();
        }

        private void OnGUI()
        {
            float remain = Mathf.Max(0f, AutoQuitSeconds - _t);

            bool applied = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            applied = TransparentWindow.Applied;
#endif

            // 투명 창이 적용되지 않았으면 그 사실을 크게 알린다.
            // (에디터 Play 모드로 돌리면 배경이 검게 나오는데, 이걸 실패로 오해하기 쉽다)
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

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            float y = applied ? 20f : 165f;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(20, y, 900, 90), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(32, y + 8,  880, 24), _status, style);
            GUI.Label(new Rect(32, y + 32, 880, 24), "걷는 중: " + _walkTitle, style);
            GUI.Label(new Rect(32, y + 56, 880, 24),
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
