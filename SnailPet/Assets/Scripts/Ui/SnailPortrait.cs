using System.Collections.Generic;
using SnailPet.Snail;
using UnityEngine;

namespace SnailPet.Ui
{
    /// <summary>
    /// UI 패널에 띄울 달팽이 초상.
    ///
    /// 화면을 돌아다니는 달팽이를 그대로 비출 수는 없다. 그쪽은 벽에 따라 돌아가 있고
    /// 늘어났다 줄었다 하며 좌우로 뒤집힌다. 초상에는 <b>똑바로 선 정지 상태</b>가 필요하다.
    ///
    /// 그래서 같은 외형으로 복제본을 하나 더 만들어 전용 레이어에 두고, 그 레이어만 찍는
    /// 카메라로 렌더 텍스처에 그린다. 변형은 살아있는 쪽에만 적용되므로 복제본은 가만히 있는다.
    /// </summary>
    public sealed class SnailPortrait
    {
        /// <summary>초상 전용 레이어. 메인 카메라는 이 레이어를 빼고 찍는다.</summary>
        public const int Layer = 31;

        /// <summary>본 장면과 겹치지 않게 멀리 떨어뜨린다. 레이어로도 갈리지만 이중으로 막는다.</summary>
        private const float FarAway = 100000f;

        /// <summary>
        /// 초상 여러 개가 동시에 살아 있을 때 서로를 찍지 않게 자리를 비켜 주는 간격.
        ///
        /// 레이어는 하나(31)뿐이라 카메라의 cullingMask 로는 남의 복제본을 못 가린다.
        /// 자리를 떼어 놓아 서로의 시야에서 벗어나게 하는 것이 유일한 방법이다.
        /// 파츠 캔버스가 1200px 이므로 5000 이면 네 배 넘는 여유가 있다.
        ///
        /// 전에는 전부 같은 자리에 있었는데, 상세·옷장·유전정보가 모두 같은 개체라
        /// 똑같은 그림이 겹쳐 한 마리로 보이는 바람에 드러나지 않았다. 부화 팝업이
        /// 처음으로 다른 개체를 띄우면서 두 마리가 겹쳐 보였다.
        /// </summary>
        private const float Spacing = 5000f;

        /// <summary>쓰고 있는 자리. 버리면 반납해 다시 쓴다 — 안 그러면 좌표가 계속 멀어져 정밀도가 떨어진다.</summary>
        private static readonly HashSet<int> _usedSlots = new HashSet<int>();

        private readonly int _slot;

        private static int TakeSlot()
        {
            int i = 0;
            while (_usedSlots.Contains(i)) i++;
            _usedSlots.Add(i);
            return i;
        }

        /// <summary>화면 크기의 몇 배로 그릴지. 고해상도에서 UI 를 키워도 견디게 여유를 둔다.</summary>
        private const int Supersample = 2;

        /// <summary>몸 주위 여백. 0.04 이면 4% 띄운다.</summary>
        private const float Margin = 0.04f;

        public RenderTexture Texture { get; private set; }

        private readonly GameObject _root;
        private readonly Camera _camera;

        public SnailPortrait(Transform parent, SnailAppearance appearance, SnailBounds bounds,
                             int widthPx, int heightPx)
        {
            _slot = TakeSlot();

            _root = new GameObject("SnailPortrait");
            _root.transform.SetParent(parent, false);
            _root.transform.position = new Vector3(FarAway + _slot * Spacing, 0f, 0f);

            var composed = SnailComposer.Build(appearance, "PortraitSnail");
            composed.Root.transform.SetParent(_root.transform, false);
            SetLayerRecursive(composed.Root, Layer);

            Texture = new RenderTexture(Mathf.Max(1, widthPx * Supersample),
                                        Mathf.Max(1, heightPx * Supersample), 0,
                                        RenderTextureFormat.ARGB32)
            {
                name = "SnailPortraitRT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
            };
            Texture.Create();

            var camGo = new GameObject("PortraitCamera");
            camGo.transform.SetParent(_root.transform, false);

            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.cullingMask = 1 << Layer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.targetTexture = Texture;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.depth = -10;                 // 본 카메라보다 먼저

            Frame(bounds, widthPx / (float)heightPx);
        }

        /// <summary>몸이 화면에 꽉 차되 잘리지 않게 카메라를 맞춘다.</summary>
        private void Frame(SnailBounds b, float aspect)
        {
            float w = Mathf.Max(1f, b.Right - b.Left);
            float h = Mathf.Max(1f, b.Top - b.Foot);

            // 가로가 모자라면 가로가, 세로가 모자라면 세로가 기준이 된다
            float half = Mathf.Max(h * 0.5f, w * 0.5f / Mathf.Max(0.0001f, aspect));
            _camera.orthographicSize = half * (1f + Margin);

            // 몸의 한가운데를 화면 한가운데에 둔다
            _camera.transform.localPosition =
                new Vector3((b.Left + b.Right) * 0.5f, (b.Foot + b.Top) * 0.5f, -100f);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>메인 카메라가 초상 복제본을 같이 찍지 않게 한다.</summary>
        public static void ExcludeFrom(Camera camera)
        {
            if (camera != null) camera.cullingMask &= ~(1 << Layer);
        }

        public void Dispose()
        {
            _usedSlots.Remove(_slot);
            if (_camera != null) _camera.targetTexture = null;
            if (Texture != null) { Texture.Release(); Object.Destroy(Texture); Texture = null; }
            if (_root != null) Object.Destroy(_root);
        }
    }
}
