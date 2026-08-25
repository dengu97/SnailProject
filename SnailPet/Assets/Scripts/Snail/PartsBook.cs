using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>모아 둔 파츠 한 칸.</summary>
    public sealed class PartEntry
    {
        public int PartsId;

        /// <summary>보상을 받았는가. 모은 것과 받은 것은 다르다 — 도감에서 눌러 받는다.</summary>
        public bool RewardTaken;
    }

    /// <summary>
    /// 파츠(외형) 도감.
    ///
    /// 달팽이 도감(<see cref="GuideBook"/>)과 다른 점은 <b>칸이 파츠 하나</b>라는 것뿐이다.
    /// 한 번이라도 가졌으면 채워지고, 그 달팽이를 팔아도 도감에는 남는다 —
    /// 「무엇을 봤는가」의 기록이지 「무엇을 가지고 있는가」가 아니다.
    /// </summary>
    public static class PartsBook
    {
        /// <summary>이 파츠를 모으면 주는 것들. 시트의 RewardId01~03 을 순서대로 읽는다.</summary>
        public static List<(int itemId, int count)> RewardsOf(PartsDataRow row)
        {
            var list = new List<(int, int)>();
            if (row == null) return list;

            Add(row.RewardId01, row.RewardCount01);
            Add(row.RewardId02, row.RewardCount02);
            Add(row.RewardId03, row.RewardCount03);
            return list;

            void Add(int? itemId, int? count)
            {
                if (!itemId.HasValue || itemId.Value == 0) return;
                if (!count.HasValue || count.Value <= 0) return;

                list.Add((itemId.Value, count.Value));
            }
        }

        /// <summary>줄 차례. 등급이 높은 것부터, 같으면 시트 순서대로.</summary>
        public static List<PartsDataRow> Sorted(PartsType type)
        {
            var list = new List<PartsDataRow>();
            foreach (var p in GameData.PartsData)
                if (p.PartsType == type) list.Add(p);

            list.Sort((a, b) =>
            {
                int byRarity = ((int)b.RarityType).CompareTo((int)a.RarityType);
                return byRarity != 0 ? byRarity : a.Id.CompareTo(b.Id);
            });
            return list;
        }
    }
}
