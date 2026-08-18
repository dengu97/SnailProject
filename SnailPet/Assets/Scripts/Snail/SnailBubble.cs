using SnailPet.Data;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 달팽이 위에 뜨는 말풍선 하나.
    ///
    /// 자리·각도는 부르는 쪽이 정해서 <see cref="Place"/> 로 넘긴다 — 달팽이가 붙은 벽을 따라
    /// 같이 돌아가야 하고, 여러 개가 뜰 때는 서로 쌓아 올려야 하기 때문이다.
    ///
    /// 달팽이 루트의 자식으로 넣지 않는 것은 선물 말풍선과 같은 이유다. 루트에는 좌우 반전과
    /// 몸통 변형이 걸려 있어 말풍선까지 뒤집히고 늘어난다.
    /// </summary>
    public sealed class SnailBubble
    {
        private readonly Transform _root;
        private readonly SpriteRenderer _renderer;

        /// <summary>보이는 크기(월드 단위). 여러 개를 쌓을 때 간격 계산에 쓴다.</summary>
        public float HalfWidthWorld { get; private set; }
        public float HalfHeightWorld { get; private set; }

        /// <param name="token">BubbleData 토큰. 시트에 있으면 거기 크기·그림을 쓴다.</param>
        /// <param name="fallbackKey">시트에 그 토큰이 없을 때 쓸 그림 이름.</param>
        /// <param name="defaultSize">시트에 없을 때 쓸 ResourceSize.</param>
        public SnailBubble(Transform parent, string token, string fallbackKey, float defaultSize, int sortingOrder)
        {
            var row = GameData.IdByToken.TryGetValue(token, out int id)
                   && GameData.BubbleDataById.TryGetValue(id, out var r) ? r : null;

            string key = row != null && !string.IsNullOrEmpty(row.ResourceKey) ? row.ResourceKey : fallbackKey;
            float size = row != null && row.ResourceSize > 0 ? (float)row.ResourceSize : defaultSize;
            float pixels = size * SnailPresent.PixelsPerSize;

            var sprite = string.IsNullOrEmpty(key)
                       ? null : SnailComposer.Load(SnailComposer.ResourceRoot + "/Ui/" + key);

            var go = new GameObject("Bubble_" + key);
            go.transform.SetParent(parent, false);
            _root = go.transform;

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = sortingOrder;
            _renderer.enabled = false;

            if (sprite != null && SnailMetrics.TryMeasure(sprite, out var e) && e.Width > 0.01f)
            {
                float scale = pixels / e.Width;
                go.transform.localScale = new Vector3(scale, scale, 1f);
                HalfWidthWorld  = e.Width  * scale * 0.5f;
                HalfHeightWorld = e.Height * scale * 0.5f;
            }
            else
            {
                HalfWidthWorld = HalfHeightWorld = pixels * 0.5f;
                if (sprite == null) Debug.LogWarning("[SnailPet] 말풍선 리소스를 찾지 못했습니다: " + key);
            }
        }

        public bool Visible => _renderer != null && _renderer.enabled;

        /// <summary>그 자리·각도에 띄운다. 각도는 달팽이 자세를 그대로 받는다.</summary>
        public void Place(Vector3 worldPosition, float rotationDeg, bool visible)
        {
            _renderer.enabled = visible && _renderer.sprite != null;
            if (!visible) return;

            _root.position = worldPosition;
            _root.localRotation = Quaternion.Euler(0f, 0f, rotationDeg);
        }
    }
}
