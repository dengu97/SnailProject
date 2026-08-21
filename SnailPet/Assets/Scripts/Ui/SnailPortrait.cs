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

        /// <summary>
        /// 얼굴만 담을 때 몸 세로의 얼마를 보여 줄지. 1 이면 통째로 들어간다.
        /// 목록 썸네일이 정사각형이라 이 값이 곧 잘려 나가는 정도를 정한다.
        /// </summary>
        private const float HeadCrop = 0.75f;

        /// <summary>눈을 못 찾았을 때 쓸 세로 위치. 0 = 발바닥, 1 = 머리 끝.</summary>
        private const float HeadY = 0.72f;

        /// <param name="headOnly">얼굴만 당겨 찍는다 (목록 썸네일용).</param>
        public SnailPortrait(Transform parent, SnailAppearance appearance, SnailBounds bounds,
                             int widthPx, int heightPx, bool headOnly = false)
        {
            _slot = TakeSlot();

            _root = new GameObject("SnailPortrait");
            _root.transform.SetParent(parent, false);
            _root.transform.position = new Vector3(FarAway + _slot * Spacing, 0f, 0f);

            var composed = SnailComposer.Build(appearance, "PortraitSnail");
            composed.Root.transform.SetParent(_root.transform, false);
            SetLayerRecursive(composed.Root, Layer);

            // 초상은 한 번 찍고 마는 정지 그림이다. 애니메이션 파츠를 그냥 두면 찍는 순간의
            // 칸이 걸려 목록 썸네일마다 다른 칸이 나온다. 첫 칸에서 세워 둔다.
            var flip = composed.Root.GetComponent<SnailFlipbook>();
            if (flip != null) flip.enabled = false;

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

            // 매 프레임 다시 찍을 이유가 없다. 복제본은 변형을 안 받아 가만히 있고,
            // 외형이 바뀌면 부르는 쪽이 이 초상을 버리고 새로 만든다(옷장이 그렇게 한다).
            // 그래서 카메라를 꺼 두고 필요할 때만 손으로 찍는다.
            _camera.enabled = false;

            if (headOnly) FrameHead(appearance, bounds);
            else          Frame(bounds, widthPx / (float)heightPx);

            Redraw();
        }

        /// <summary>
        /// 한 장 찍는다. 꺼진 카메라도 이렇게 부르면 그 자리에서 렌더 텍스처에 그린다.
        /// </summary>
        public void Redraw()
        {
            if (_camera != null) _camera.Render();
        }

        /// <summary>
        /// 그림이 아직 살아 있는지 보고, 잃었으면 다시 찍는다.
        ///
        /// 렌더 텍스처는 그래픽 장치가 리셋되면(드라이버 갱신·절전 복귀 등) 내용을 잃는다.
        /// 매 프레임 찍던 시절에는 다음 프레임에 저절로 메워졌지만, 한 번만 찍게 된 뒤로는
        /// 그대로 비어 버린다. 이 펫은 하루 종일 떠 있으므로 겪을 수 있는 일이다.
        /// </summary>
        public void EnsureDrawn()
        {
            if (Texture == null || Texture.IsCreated()) return;

            Texture.Create();
            Redraw();
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

        /// <summary>
        /// 얼굴만 정사각형으로 당겨 잡는다.
        ///
        /// 어디가 얼굴인지는 <b>눈 파츠를 재서</b> 정한다. 눈은 진행 방향 쪽에 붙어 있어
        /// 몸통 한가운데를 쓰면 얼굴이 한쪽으로 치우친다. 눈이 없는 개체(데이터상 가능)는
        /// 몸 위쪽을 쓴다.
        /// </summary>
        private void FrameHead(SnailAppearance appearance, SnailBounds b)
        {
            float cx = (b.Left + b.Right) * 0.5f;
            float cy = Mathf.Lerp(b.Foot, b.Top, HeadY);

            foreach (var p in appearance.Parts)
            {
                if (p.Type != SnailPet.Data.PartsType.Eyes) continue;

                var sprite = SnailComposer.LoadFrame(SnailComposer.LinePath(p.Type, p.ResourceKey));
                if (sprite != null && SnailMetrics.TryMeasure(sprite, out var e))
                {
                    cx = (e.Left + e.Right) * 0.5f;
                    cy = (e.Bottom + e.Top) * 0.5f;
                }
                break;
            }

            _camera.orthographicSize = Mathf.Max(1f, (b.Top - b.Foot) * HeadCrop * 0.5f);
            _camera.transform.localPosition = new Vector3(cx, cy, -100f);
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

            if (_root != null)
            {
                // 끄는 것이 먼저다. Destroy 는 프레임 끝에야 실제로 지우므로, 버리자마자 같은
                // 자리에 새 초상을 만들어 그 자리에서 찍으면 아직 살아 있는 이 복제본이 같이
                // 찍힌다. 매 프레임 찍던 시절에는 다음 프레임에 저절로 메워져 드러나지 않았다.
                _root.SetActive(false);
                Object.Destroy(_root);
            }
        }
    }
}
