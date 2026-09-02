using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 칸 늘리기. 달팽이 칸과 부화기 칸을 코인으로 늘린다.
    ///
    /// 시트(SlotSellData)는 <b>몇 번째 확장인가</b>로 값을 적는다. 첫 확장이 500, 두 번째가
    /// 600 … 하는 식이라 값이 칸마다 다르다 — 그래서 「한 개당 값 × 수량」으로는 못 센다.
    /// 여러 칸을 한 번에 살 때는 <see cref="TotalCost"/> 로 다음 칸들의 값을 더해서 낸다.
    ///
    /// 시작 칸 수는 GameConfig 가 들고 있다(<see cref="Data.Config.StartSnailSlot"/>).
    /// 그래서 「지금 몇 번째 확장인가」는 <c>지금 칸 수 − 시작 칸 수 + 1</c> 이다.
    /// </summary>
    public static class SlotSell
    {
        /// <summary>이 종류가 시작할 때 갖는 칸 수.</summary>
        public static int Start(SlotType type) =>
            type == SlotType.Snail ? Config.StartSnailSlot : Config.StartEggSlot;

        /// <summary>이 종류를 몇 번까지 늘릴 수 있나. 시트에 적힌 확장 수다.</summary>
        public static int MaxPlus(SlotType type)
        {
            int max = 0;
            foreach (var row in GameData.SlotSellData)
                if (row != null && row.SlotType == type && row.PlusSlotCount > max) max = row.PlusSlotCount;

            return max;
        }

        /// <summary>이 종류가 가질 수 있는 칸 수의 끝.</summary>
        public static int Max(SlotType type) => Start(type) + MaxPlus(type);

        /// <summary><paramref name="nth"/> 번째 확장의 값. 그런 칸이 없으면 null.</summary>
        public static SlotSellDataRow Of(SlotType type, int nth)
        {
            foreach (var row in GameData.SlotSellData)
                if (row != null && row.SlotType == type && row.PlusSlotCount == nth) return row;

            return null;
        }

        /// <summary>
        /// 지금 <paramref name="have"/> 칸에서 <paramref name="count"/> 칸을 더 살 때의 총액.
        /// 한 칸이라도 살 수 없으면(끝까지 늘렸거나 값이 안 적혔으면) -1.
        /// </summary>
        public static long TotalCost(SlotType type, int have, int count)
        {
            if (count <= 0) return -1;

            int nth = have - Start(type) + 1;      // 다음에 살 확장의 번호
            long sum = 0;

            for (int i = 0; i < count; i++)
            {
                var row = Of(type, nth + i);
                if (row == null || !row.CostCount.HasValue || row.CostCount.Value <= 0) return -1;

                sum += row.CostCount.Value;
            }
            return sum;
        }

        /// <summary>값을 치르는 아이템. 다음에 살 칸의 것을 쓴다 — 시트가 칸마다 따로 적을 수 있다.</summary>
        public static int CostItem(SlotType type, int have)
        {
            var row = Of(type, have - Start(type) + 1);
            return row != null && row.CostItem.HasValue ? row.CostItem.Value : 0;
        }

        /// <summary>지금 몇 칸까지 더 살 수 있나.</summary>
        public static int Room(SlotType type, int have) => System.Math.Max(0, Max(type) - have);
    }
}
