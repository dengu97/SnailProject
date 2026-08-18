using SnailPet.Data;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 시간마다 쌓이는 선물. LevelData.CoinCoolTime 마다 준비되고,
    /// 유저가 말풍선을 누르면 ItemId 를 ItemCount 만큼 받는다.
    ///
    /// 준비된 것을 알려주지 않으면 유저가 알 방법이 없으므로 달팽이 위에 말풍선을 띄운다.
    /// 말풍선은 달팽이가 붙은 벽을 따라 <b>같이 돌아간다</b>. 옆벽을 탈 때 혼자 똑바로 서 있으면
    /// 달팽이에 딸린 것으로 안 보인다.
    ///
    /// 그래도 달팽이 루트의 자식으로 넣지는 않는다. 루트에는 좌우 반전과 몸통 변형이 걸려 있어
    /// 말풍선까지 뒤집히고 늘어난다. 위치와 회전만 받아 따로 배치한다.
    /// </summary>
    public sealed class SnailPresent
    {
        /// <summary>
        /// 코인 말풍선의 토큰.
        ///
        /// 지금은 선물이 항상 코인이라 코드가 이 하나를 직접 고른다. 나중에 선물 종류가
        /// 늘어나면 LevelData 의 ItemId 옆에 BubbleId 열을 두고 거기서 읽는 것이 자연스럽다.
        /// </summary>
        public const string CoinBubbleToken = "[코인]";

        /// <summary>토큰 → BubbleData 행.</summary>
        public static BubbleDataRow ResolveBubble(string token)
        {
            if (!GameData.IdByToken.TryGetValue(token, out int id))
            {
                Debug.LogWarning("[SnailPet] 알 수 없는 말풍선 토큰: " + token);
                return null;
            }
            if (!GameData.BubbleDataById.TryGetValue(id, out var row))
            {
                Debug.LogWarning("[SnailPet] BubbleData 에 없는 말풍선: " + token);
                return null;
            }
            return row;
        }

        /// <summary>
        /// BubbleData.ResourceSize 1 당 화면 픽셀.
        ///
        /// 지금 화면에 뜨는 크기를 기준값 10 으로 잡았으므로, 92px / 10 = 9.2 다.
        /// 데이터에서 20 을 주면 두 배로 커진다.
        /// </summary>
        public const float PixelsPerSize = 9.2f;

        /// <summary>ResourceSize 를 못 읽었을 때 쓰는 크기.</summary>
        public const float DefaultSize = 10f;

        /// <summary>달팽이 머리 위로 얼마나 띄울지(px).</summary>
        public const float BubbleGap = 18f;

        private readonly Transform _bubble;
        private readonly SpriteRenderer _renderer;

        /// <summary>말풍선의 보이는 크기(월드 단위). 히트 판정에 쓴다.</summary>
        public float HalfWidthWorld { get; private set; }
        public float HalfHeightWorld { get; private set; }

        public bool Ready { get; private set; }
        public double Remaining { get; private set; }

        public SnailPresent(Transform parent)
        {
            var row = ResolveBubble(CoinBubbleToken);
            var sprite = row == null || string.IsNullOrEmpty(row.ResourceKey)
                       ? null
                       : SnailComposer.Load(SnailComposer.ResourceRoot + "/Ui/" + row.ResourceKey);

            // 크기는 데이터가 정한다. 값이 없거나 0 이면 지금 크기(10)를 쓴다.
            float size = row != null && row.ResourceSize > 0 ? (float)row.ResourceSize : DefaultSize;
            float pixels = size * PixelsPerSize;

            var go = new GameObject("PresentBubble");
            go.transform.SetParent(parent, false);
            _bubble = go.transform;

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = 10000;      // 달팽이보다 항상 앞
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
                if (sprite == null)
                    Debug.LogWarning("[SnailPet] 말풍선 리소스를 찾지 못했습니다: " + CoinBubbleToken);
            }
        }

        /// <summary>레벨이 바뀌면 주기도 바뀐다. 남은 시간은 이어서 센다.</summary>
        /// <summary>기다리지 않고 바로 받을 수 있게 한다. 확인용 치트가 쓴다.</summary>
        public void MakeReady()
        {
            Remaining = 0;
            Ready = true;
        }

        public void Tick(double deltaSeconds, LevelDataRow level)
        {
            if (Ready) return;

            double cool = level != null ? level.CoinCoolTime : 0;
            if (cool <= 0) { Ready = true; Remaining = 0; return; }

            if (Remaining <= 0 && !_started) { Remaining = cool; _started = true; }

            Remaining -= deltaSeconds;
            if (Remaining <= 0)
            {
                Remaining = 0;
                Ready = true;
            }
        }

        private bool _started;

        /// <summary>말풍선을 누른 순간. 아이템을 지급하고 다음 주기를 시작한다.</summary>
        public bool TryClaim(LevelDataRow level, Inventory inventory, out int itemId, out int count)
        {
            itemId = 0; count = 0;
            if (!Ready || level == null) return false;

            itemId = level.ItemId;
            count = level.ItemCount;
            if (itemId <= 0 || count <= 0) return false;

            inventory.Add(itemId, count);

            Ready = false;
            Remaining = level.CoinCoolTime;
            _started = true;
            return true;
        }

        /// <summary>
        /// 말풍선을 그 위치·각도에 띄운다.
        /// 각도는 달팽이 자세를 그대로 받으므로 모서리를 도는 동안에도 같이 부드럽게 돌아간다.
        /// </summary>
        public void Place(Vector3 worldPosition, float rotationDeg, bool visible)
        {
            _renderer.enabled = visible && _renderer.sprite != null;
            if (!visible) return;

            _bubble.position = worldPosition;
            _bubble.localRotation = Quaternion.Euler(0f, 0f, rotationDeg);
        }

        /// <summary>
        /// 커서가 말풍선 위에 있는가.
        ///
        /// 자리·회전·크기가 이미 트랜스폼에 들어 있으므로 달팽이 히트 판정과 같은 방법으로
        /// 커서를 로컬로 역변환해 잰다. <b>안 보이면 안 잡힌다</b> — 설정에서 코인 알림을 끄면
        /// 말풍선이 안 뜨고, 그때는 달팽이를 눌러 받는다.
        /// </summary>
        public bool Contains(Vector3 world)
        {
            if (_renderer == null || !_renderer.enabled || _renderer.sprite == null) return false;

            var b = _renderer.sprite.bounds;
            Vector3 local = _bubble.InverseTransformPoint(world);
            return local.x >= b.min.x && local.x <= b.max.x
                && local.y >= b.min.y && local.y <= b.max.y;
        }

        /// <summary>화면에 안 보일 때 원인을 좁히기 위한 진단.</summary>
        public string Describe() =>
            $"sprite={( _renderer.sprite != null ? _renderer.sprite.name : "없음")} " +
            $"enabled={_renderer.enabled} pos={_bubble.position} scale={_bubble.localScale} " +
            $"rot={_bubble.localRotation.eulerAngles.z:0} order={_renderer.sortingOrder} " +
            $"halfW={HalfWidthWorld:0.0} halfH={HalfHeightWorld:0.0}";

        public override string ToString() =>
            Ready ? "선물 준비됨" : $"다음 선물까지 {Remaining:0}초";
    }
}
