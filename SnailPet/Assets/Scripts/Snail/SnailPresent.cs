using SnailPet.Data;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 시간마다 쌓이는 선물. LevelData.CoinCoolTime 마다 준비되고,
    /// 유저가 말풍선을 누르면 ItemId 를 ItemCount 만큼 받는다.
    ///
    /// 준비된 것을 알려주지 않으면 유저가 알 방법이 없으므로 달팽이 위에 말풍선을 띄운다.
    /// 말풍선은 달팽이가 벽에 매달려 뒤집혀 있어도 <b>똑바로</b> 서 있어야 하므로
    /// 달팽이 루트에 붙이지 않고 따로 배치한다.
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

        /// <summary>토큰 → BubbleData → 리소스 키.</summary>
        public static string ResolveBubbleResource(string token)
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
            return row.ResourceKey;
        }

        /// <summary>화면에 보일 말풍선 가로 크기(px).</summary>
        public const float BubblePixels = 92f;

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
            string key = ResolveBubbleResource(CoinBubbleToken);
            var sprite = string.IsNullOrEmpty(key)
                       ? null
                       : SnailComposer.Load(SnailComposer.ResourceRoot + "/Ui/" + key);

            var go = new GameObject("PresentBubble");
            go.transform.SetParent(parent, false);
            _bubble = go.transform;

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = 10000;      // 달팽이보다 항상 앞
            _renderer.enabled = false;

            if (sprite != null && SnailMetrics.TryMeasure(sprite, out var e) && e.Width > 0.01f)
            {
                float scale = BubblePixels / e.Width;
                go.transform.localScale = new Vector3(scale, scale, 1f);
                HalfWidthWorld  = e.Width  * scale * 0.5f;
                HalfHeightWorld = e.Height * scale * 0.5f;
            }
            else
            {
                HalfWidthWorld = HalfHeightWorld = BubblePixels * 0.5f;
                if (sprite == null)
                    Debug.LogWarning("[SnailPet] 말풍선 리소스를 찾지 못했습니다: " + CoinBubbleToken);
            }
        }

        /// <summary>레벨이 바뀌면 주기도 바뀐다. 남은 시간은 이어서 센다.</summary>
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

        /// <summary>말풍선을 그 위치에 띄운다. 회전은 주지 않는다 (항상 똑바로).</summary>
        public void Place(Vector3 worldPosition, bool visible)
        {
            _renderer.enabled = visible && _renderer.sprite != null;
            if (visible) _bubble.position = worldPosition;
        }

        public override string ToString() =>
            Ready ? "선물 준비됨" : $"다음 선물까지 {Remaining:0}초";
    }
}
