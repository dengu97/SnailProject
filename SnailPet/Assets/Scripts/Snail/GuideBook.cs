using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 도감 한 칸의 기록. 채운 것만 들어 있다.
    ///
    /// 채운 순간의 모습을 그대로 적어 둔다 — 그 달팽이를 나중에 팔거나 악세서리를 바꿔도
    /// 도감 그림은 그때 그대로여야 한다. 필드 이름이 곧 세이브 열쇠라 바꾸면 옛 세이브를 못 읽는다.
    /// </summary>
    public sealed class GuideEntry
    {
        public int GuideId;

        /// <summary>보상을 받았는가. 채운 것과 받은 것은 다르다 — 완성 팝업에서 받는다.</summary>
        public bool RewardTaken;

        /// <summary>채운 순간의 파츠와 색.</summary>
        public readonly List<SnailPartRef> Look = new List<SnailPartRef>();
    }

    /// <summary>
    /// 도감. 어떤 달팽이가 어느 칸을 채우는지 판정하고, 채운 것을 기록한다.
    ///
    /// 판정 규칙은 SnailGuide 시트가 정한다.
    ///  · 파츠 칸(최대 4개)에 적힌 파츠를 <b>전부</b> 가지고 있어야 한다.
    ///  · 색이 같이 적혀 있으면 그 색이어야 하고, 비어 있으면 <b>아무 색이나</b> 된다.
    ///  · 적히지 않은 부위는 무엇이든 상관없다.
    /// </summary>
    public static class GuideBook
    {
        /// <summary>이 달팽이가 이 칸을 채우는가.</summary>
        public static bool Matches(SnailGuideRow row, SnailAppearance look)
        {
            if (row == null || look == null) return false;

            return Has(look, row.PartsId01, row.ColorId01)
                && Has(look, row.PartsId02, row.ColorId02)
                && Has(look, row.PartsId03, row.ColorId03)
                && Has(look, row.PartsId04, row.ColorId04);
        }

        /// <summary>
        /// 그 파츠를 (색까지 맞춰) 가지고 있는가.
        /// 파츠 칸이 비어 있으면 조건이 없는 것이므로 참이다.
        /// </summary>
        private static bool Has(SnailAppearance look, int? partsId, string colorKey)
        {
            if (!partsId.HasValue || partsId.Value == 0) return true;

            foreach (var p in look.Parts)
            {
                if (p.PartsId != partsId.Value) continue;
                if (string.IsNullOrEmpty(colorKey)) return true;      // 색은 아무거나
                if (p.ColorKey == colorKey) return true;
            }
            return false;
        }

        /// <summary>
        /// 가진 달팽이를 훑어 새로 채워진 칸을 기록한다. 새로 채운 칸들을 돌려준다.
        ///
        /// 「가지게 되는 순간」이 채우는 시점이라, 부화·구매 뒤와 불러온 직후에 부르면 된다.
        /// 한 번 채운 칸은 그 달팽이를 팔아도 남는다.
        /// </summary>
        public static List<SnailGuideRow> Scan(PlayerState player)
        {
            var filled = new List<SnailGuideRow>();
            if (player == null) return filled;

            foreach (var row in GameData.SnailGuide)
            {
                if (row == null || player.FindGuide(row.Id) != null) continue;

                foreach (var snail in player.Snails)
                {
                    // 타고난 파츠로만 본다. 갈아입는 악세서리는 그 개체의 특징이 아니다.
                    if (!Matches(row, snail.Appearance)) continue;

                    var entry = new GuideEntry { GuideId = row.Id };
                    entry.Look.AddRange(snail.Appearance.Parts);
                    player.Guides.Add(entry);

                    filled.Add(row);
                    break;
                }
            }
            return filled;
        }

        /// <summary>보상 한 벌. 비어 있는 칸은 건너뛴다.</summary>
        public static List<(int itemId, int count)> RewardsOf(SnailGuideRow row)
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
    }
}
