using UnityEngine;

namespace SnailPet.Ui
{
    /// <summary>
    /// 이펙트 하나를 렌더 텍스처에 담아 UI 위에 얹는다.
    ///
    /// 파티클은 월드에 그려지는데 UI 는 화면에 덧그리는 캔버스라, 그리는 순서를 아무리 올려도
    /// 팝업 위로는 못 올라온다. 그래서 초상(<see cref="SnailPortrait"/>)이 쓰는 방법을 그대로
    /// 쓴다 — 전용 레이어에 세워 두고 전용 카메라로 찍어 그림으로 받는다.
    ///
    /// 초상과 다른 점은 <b>몸에 맞춰 잡지 않는다</b>는 것이다. 이펙트는 달팽이 칸을 넉넉히
    /// 덮어야 하므로, 보이는 범위를 밖에서 정해 받는다.
    /// </summary>
    public sealed class EffectView : System.IDisposable
    {
        /// <summary>초상과 같은 레이어. 메인 카메라가 빼고 찍는 레이어가 이것뿐이다.</summary>
        private const int Layer = SnailPortrait.Layer;

        /// <summary>
        /// 초상 복제본들과 멀찍이 떨어뜨린다.
        ///
        /// 레이어가 같아 카메라의 cullingMask 로는 서로를 못 가린다. 초상은 x 축으로 줄지어
        /// 서므로 이쪽은 <b>y 로</b> 비켜 둔다 — 그러면 몇 개가 떠 있든 겹칠 일이 없다.
        /// </summary>
        private const float FarAway = 100000f;

        /// <summary>화면 크기의 몇 배로 그릴지. 초상과 같은 이유로 여유를 둔다.</summary>
        private const int Supersample = 2;

        public RenderTexture Texture { get; private set; }

        private readonly GameObject _root;
        private readonly Camera _camera;

        /// <param name="fx">세울 이펙트. 이 안으로 들어오고, 버릴 때 같이 사라진다.</param>
        /// <param name="widthPx">그림으로 받을 크기(UI 픽셀).</param>
        /// <param name="viewUnits">
        /// 세로로 몇 월드 유닛을 담을지. <b>이펙트 크기를 정하는 손잡이다</b> —
        /// 작게 줄수록 당겨 찍어 크게 나온다. 이펙트를 건드리지 않고 여기서 맞춘다.
        /// </param>
        public EffectView(Transform parent, GameObject fx, int widthPx, int heightPx, float viewUnits)
        {
            _root = new GameObject("EffectView");
            _root.transform.SetParent(parent, false);
            _root.transform.position = new Vector3(0f, FarAway, 0f);

            if (fx != null)
            {
                fx.transform.SetParent(_root.transform, false);
                fx.transform.localPosition = Vector3.zero;
                SetLayerRecursive(fx, Layer);
            }

            Texture = new RenderTexture(Mathf.Max(1, widthPx * Supersample),
                                        Mathf.Max(1, heightPx * Supersample), 0,
                                        RenderTextureFormat.ARGB32)
            {
                name = "EffectViewRT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
            };
            Texture.Create();

            var camGo = new GameObject("EffectCamera");
            camGo.transform.SetParent(_root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -100f);

            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Max(1f, viewUnits) * 0.5f;
            _camera.cullingMask = 1 << Layer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.targetTexture = Texture;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.depth = -10;                 // 본 카메라보다 먼저

            // 이펙트는 움직이므로 매 프레임 다시 찍어야 한다. 다만 팝업이 떠 있는 동안만
            // 찍으면 되므로, 카메라는 꺼 두고 띄운 쪽이 Redraw 를 부른다.
            _camera.enabled = false;
            Redraw();
        }

        /// <summary>한 장 찍는다. 꺼진 카메라도 이렇게 부르면 그 자리에서 그린다.</summary>
        public void Redraw()
        {
            if (_camera == null) return;
            if (Texture != null && !Texture.IsCreated()) Texture.Create();

            _camera.Render();
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        public void Dispose()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (Texture != null) { Texture.Release(); Object.Destroy(Texture); Texture = null; }

            if (_root != null)
            {
                _root.SetActive(false);      // 초상과 같은 이유 — Destroy 는 프레임 끝에야 실제로 지운다
                Object.Destroy(_root);
            }
        }
    }
}
