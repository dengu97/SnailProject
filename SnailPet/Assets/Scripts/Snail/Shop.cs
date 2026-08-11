using System;
using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 상점에서 파는 것과 사는 절차.
    ///
    /// 화면이 아니라 여기가 「무엇을 파는가」를 정한다. UI 는 이 결과를 그리기만 한다.
    /// </summary>
    public static class Shop
    {
        /// <summary>
        /// 상점에 내보내는 카테고리. 목업의 순서 그대로다.
        ///
        /// <see cref="CategoryType.Item"/> 은 뺐다. 목업에 없기도 하고, 데이터가
        /// 「코인 1개로 코인 50개」라 그대로 팔면 코인이 무한히 늘어난다.
        /// <see cref="CategoryType.Market"/> 은 유저끼리 거래하는 자유시장 자리라
        /// 아직 상품이 없다 — 칸만 있고 비어 있는 것이 맞다.
        /// </summary>
        public static readonly CategoryType[] Categories =
        {
            CategoryType.Food,
            CategoryType.Egg,
            CategoryType.Accessories,
            CategoryType.Market,
        };

        public static ShopDataRow[] ProductsOf(CategoryType category)
        {
            var list = new List<ShopDataRow>();
            foreach (var row in GameData.ShopData)
                if (row.CategoryType == category) list.Add(row);
            return list.ToArray();
        }

        /// <summary>
        /// 오늘의 추천 한 개.
        ///
        /// 날짜를 씨앗으로 고르므로 하루 동안 고정되고 다음 날 바뀐다. 무작위로 두면
        /// 패널을 열 때마다 달라져 「오늘의」가 되지 않는다.
        /// 아직 할인 데이터가 없어 정가 그대로 보여 준다.
        /// </summary>
        public static ShopDataRow Today(DateTime now)
        {
            var pool = new List<ShopDataRow>();
            foreach (var c in Categories)
                if (c != CategoryType.Market) pool.AddRange(ProductsOf(c));

            if (pool.Count == 0) return null;
            int days = (int)(now.Date - new DateTime(2026, 1, 1)).TotalDays;
            return pool[((days % pool.Count) + pool.Count) % pool.Count];
        }

        /// <summary>이 상품이 무엇인지 이름·아이콘을 찾을 수 있는 형태로.</summary>
        public static string NameOf(ShopDataRow row)
        {
            if (row == null) return string.Empty;
            switch (row.CategoryType)
            {
                case CategoryType.Food:
                    return GameData.FoodDataById.TryGetValue(row.Id, out var f) ? Loc.ById(f.NameId) : string.Empty;
                case CategoryType.Egg:
                    return GameData.EggDataById.TryGetValue(row.Id, out var e) ? Loc.ById(e.NameId) : string.Empty;
                case CategoryType.Accessories:
                    return GameData.AccessoriesDataById.TryGetValue(row.Id, out var a) ? Loc.ById(a.NameId) : string.Empty;
                default:
                    return GameData.ItemDataById.TryGetValue(row.Id, out var i) ? Loc.ById(i.NameId) : string.Empty;
            }
        }

        /// <summary>왜 못 샀는지. 화면에 띄우지는 않고 로그로만 쓴다.</summary>
        public enum Result { Ok, NoSuchProduct, NoPrice, NotEnough }

        /// <summary>
        /// 산다. 코인을 먼저 빼고 물건을 넣는다.
        ///
        /// 알은 개수로 뭉치면 안 되므로 <see cref="PlayerState.Eggs"/> 에 낱개로 넣고,
        /// 나머지는 아이템 개수로 들어간다. 악세서리도 지금은 개수로만 들고 있는다 —
        /// 옷장이 생기면 거기서 이 개수를 읽으면 된다.
        /// </summary>
        public static Result TryBuy(PlayerState player, int shopId)
        {
            if (player == null) return Result.NoSuchProduct;

            ShopDataRow row = null;
            foreach (var r in GameData.ShopData)
                if (r.Id == shopId) { row = r; break; }

            if (row == null) return Result.NoSuchProduct;
            if (!row.CostItem.HasValue || !row.CostCount.HasValue || row.CostCount.Value <= 0)
                return Result.NoPrice;

            if (!player.Items.TrySpend(row.CostItem.Value, row.CostCount.Value))
                return Result.NotEnough;

            int count = row.ItemCount > 0 ? row.ItemCount : 1;
            if (row.CategoryType == CategoryType.Egg)
                for (int i = 0; i < count; i++) player.Eggs.Add(row.Id);
            else
                player.Items.Add(row.Id, count);

            return Result.Ok;
        }
    }
}
