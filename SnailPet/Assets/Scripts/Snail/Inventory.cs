using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 유저가 가진 아이템. 아직 저장은 없고 실행 중에만 유지된다.
    /// 상점·뽑기·보상이 전부 여기로 들어오게 된다.
    /// </summary>
    public sealed class Inventory
    {
        private readonly Dictionary<int, long> _counts = new Dictionary<int, long>();

        public long CountOf(int itemId) => _counts.TryGetValue(itemId, out long v) ? v : 0;

        /// <summary>가진 것 전부. 세이브에 적을 때 쓴다.</summary>
        public IEnumerable<KeyValuePair<int, long>> Entries => _counts;

        public void Add(int itemId, long amount)
        {
            if (itemId <= 0 || amount == 0) return;
            _counts.TryGetValue(itemId, out long cur);
            long next = cur + amount;
            if (next <= 0) _counts.Remove(itemId);
            else _counts[itemId] = next;
        }

        public bool TrySpend(int itemId, long amount)
        {
            if (amount <= 0) return true;
            if (CountOf(itemId) < amount) return false;
            Add(itemId, -amount);
            return true;
        }

        /// <summary>로그·HUD 용. 토큰 이름으로 보여준다.</summary>
        public override string ToString()
        {
            if (_counts.Count == 0) return "빈 가방";
            var sb = new System.Text.StringBuilder();
            foreach (var kv in _counts)
            {
                if (sb.Length > 0) sb.Append(", ");
                string name = GameData.TokenById.TryGetValue(kv.Key, out string t) ? t : kv.Key.ToString();
                sb.Append(name).Append(' ').Append(kv.Value);
            }
            return sb.ToString();
        }
    }
}
