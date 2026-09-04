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
        /// <summary>
        /// 말풍선은 달팽이보다 항상 앞이다. 여기에 시트의 <c>SortOrder</c> 를 더해 앞뒤를 가른다.
        /// 여러 개가 같은 자리에 겹쳐 뜨므로 어느 것이 위로 오는지는 데이터가 정한다.
        /// </summary>
        public const int BaseSortingOrder = 10000;

        /// <summary>
        /// 나타날 때의 「뽀잉」.
        ///
        /// 말풍선을 그리는 곳이 둘(<see cref="SnailBubble"/> 과 <see cref="SnailPresent"/>)이라
        /// 곡선을 한곳에 둔다 — 두 벌이 되면 한쪽만 고치게 된다.
        /// </summary>
        public struct Pop
        {
            /// <summary>뽀잉 하는 시간(초). 눈에 걸리지 않을 만큼 짧다.</summary>
            public const float Seconds = 0.22f;

            /// <summary>나타나기 시작하는 크기. 1 이 제 크기다.</summary>
            private const float From = 0.55f;

            /// <summary>넘치는 양. 0 이면 그냥 커지기만 한다.</summary>
            private const float Overshoot = 1.9f;

            /// <summary>흐른 시간. 음수면 끝났다는 뜻이다.</summary>
            private float _t;

            /// <summary>방금 나타났다. 처음부터 다시 부풀린다.</summary>
            public void Restart() => _t = 0f;

            /// <summary>사라졌다. 다음에 나타날 때 다시 부풀린다.</summary>
            public void Stop() => _t = -1f;

            /// <summary>이번 프레임의 배율. 다 끝났으면 1 이다.</summary>
            public float Step(float deltaSeconds)
            {
                if (_t < 0f) return 1f;

                _t += deltaSeconds;
                if (_t >= Seconds) { _t = -1f; return 1f; }

                // 제 크기를 한 번 지나쳤다가 되돌아오는 곡선(back-out).
                // 되돌아오는 구간이 있어야 「뽀잉」으로 읽힌다 — 커지기만 하면 밋밋하다.
                float k = _t / Seconds - 1f;
                float eased = 1f + (Overshoot + 1f) * k * k * k + Overshoot * k * k;
                return Mathf.LerpUnclamped(From, 1f, eased);
            }
        }

        private readonly Transform _root;
        private readonly SpriteRenderer _renderer;

        /// <summary>제 크기. 뽀잉 하는 동안 여기에 배율을 곱한다.</summary>
        private Vector3 _baseScale = Vector3.one;

        private Pop _pop;

        /// <summary>보이는 크기(월드 단위). 여러 개를 쌓을 때 간격 계산에 쓴다.</summary>
        public float HalfWidthWorld { get; private set; }
        public float HalfHeightWorld { get; private set; }

        /// <param name="token">BubbleData 토큰. 시트에 있으면 거기 크기·그림을 쓴다.</param>
        /// <param name="fallbackKey">시트에 그 토큰이 없을 때 쓸 그림 이름.</param>
        /// <param name="defaultSize">시트에 없을 때 쓸 ResourceSize.</param>
        public SnailBubble(Transform parent, string token, string fallbackKey, float defaultSize)
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
            _renderer.sortingOrder = BaseSortingOrder + (row != null ? row.SortOrder : 0);
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

            _baseScale = go.transform.localScale;
        }

        public bool Visible => _renderer != null && _renderer.enabled;

        /// <summary>
        /// 그 자리·각도에 띄운다. 각도는 달팽이 자세를 그대로 받는다.
        ///
        /// 나타나는 <b>그 순간</b>에 뽀잉이 시작된다 — 안 보이다가 보이게 된 프레임을 잡는다.
        /// 크기를 재 둔 값(<see cref="HalfWidthWorld"/>)은 건드리지 않는다. 그것으로 여러 개를
        /// 쌓을 자리를 잡는데, 부푸는 동안 같이 흔들리면 옆의 말풍선까지 들썩인다.
        /// </summary>
        public void Place(Vector3 worldPosition, float rotationDeg, bool visible)
        {
            bool was = _renderer.enabled;
            _renderer.enabled = visible && _renderer.sprite != null;

            if (!_renderer.enabled)
            {
                _pop.Stop();
                _root.localScale = _baseScale;
                return;
            }

            if (!was) _pop.Restart();

            _root.position = worldPosition;
            _root.localRotation = Quaternion.Euler(0f, 0f, rotationDeg);
            _root.localScale = _baseScale * _pop.Step(Time.deltaTime);
        }
    }
}
