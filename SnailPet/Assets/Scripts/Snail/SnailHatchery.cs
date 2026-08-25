using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 알에서 개체를 만든다.
    ///
    /// EggData.PartsGroupIds 에 적힌 <b>모든 그룹의 파츠를 하나의 풀로 합친다</b> (union).
    /// 그룹은 누적 해금 개념이라, 레어 그룹에 껍질만 있어도 몸·눈·더듬이는 일반 그룹에서 채워진다.
    /// (기획서 「개발 명세」의 "1개 ID 무작위 선택" 문구는 실제 사양과 다르다)
    /// </summary>
    public static class SnailHatchery
    {
        public static SnailAppearance Hatch(int eggId, System.Random rng = null)
        {
            rng ??= new System.Random();

            if (!GameData.EggDataById.TryGetValue(eggId, out var egg))
            {
                UnityEngine.Debug.LogError("[SnailPet] 알 데이터를 찾을 수 없습니다: " + eggId);
                return new SnailAppearance();
            }
            return HatchFromGroups(egg.PartsGroupIds, rng);
        }

        public static SnailAppearance HatchFromGroups(IReadOnlyList<int> groupIds, System.Random rng)
        {
            var appearance = new SnailAppearance();

            // 그룹에 속한 파츠를 타입별로 모은다
            var byType = new Dictionary<PartsType, List<PartsDataRow>>();
            foreach (var p in GameData.PartsData)
            {
                if (!Contains(groupIds, p.PartsGroupId)) continue;
                if (!byType.TryGetValue(p.PartsType, out var list))
                    byType[p.PartsType] = list = new List<PartsDataRow>();
                list.Add(p);
            }

            foreach (var kv in byType)
            {
                var picked = PickWeighted(kv.Value, rng);
                if (picked == null) continue;

                string color = null;
                if (picked.IsUseColor && picked.Colors != null && picked.Colors.Length > 0)
                    color = picked.Colors[rng.Next(picked.Colors.Length)];

                appearance.Parts.Add(new SnailPartRef
                {
                    PartsId = picked.Id,
                    Type = picked.PartsType,
                    ResourceKey = picked.ResourceKey,
                    ColorKey = color
                });
            }
            return appearance;
        }

        /// <summary>
        /// 이 알에서 그 부위에 나올 수 있는 파츠와 각각의 확률(0~1). <b>등급 높은 순</b>으로 준다.
        ///
        /// 뽑기와 같은 규칙으로 세야 하므로 <see cref="PickWeighted"/> 바로 옆에 둔다 —
        /// 뽑는 방식이 바뀌면 보여 주는 숫자도 같이 고쳐야 한다.
        /// </summary>
        public static List<(PartsDataRow Part, double Chance)> Chances(int eggId, PartsType type)
        {
            var list = new List<(PartsDataRow, double)>();
            if (!GameData.EggDataById.TryGetValue(eggId, out var egg)) return list;

            var pool = new List<PartsDataRow>();
            foreach (var p in GameData.PartsData)
                if (p.PartsType == type && Contains(egg.PartsGroupIds, p.PartsGroupId)) pool.Add(p);

            if (pool.Count == 0) return list;

            long total = 0;
            foreach (var p in pool) total += p.AppearWeight > 0 ? p.AppearWeight : 0;

            foreach (var p in pool)
            {
                // 가중치가 전부 0 이면 뽑기도 균등하다. 보여 주는 숫자도 같아야 한다.
                double chance = total > 0
                              ? (p.AppearWeight > 0 ? p.AppearWeight : 0) / (double)total
                              : 1.0 / pool.Count;
                list.Add((p, chance));
            }

            // 등급이 높은 것부터. 같은 등급이면 잘 나오는 것부터.
            list.Sort((a, b) =>
            {
                int byRarity = ((int)b.Item1.RarityType).CompareTo((int)a.Item1.RarityType);
                return byRarity != 0 ? byRarity : b.Item2.CompareTo(a.Item2);
            });
            return list;
        }

        private static bool Contains(IReadOnlyList<int> ids, int value)
        {
            if (ids == null) return false;
            for (int i = 0; i < ids.Count; i++) if (ids[i] == value) return true;
            return false;
        }

        /// <summary>AppearWeight 비중 추첨. 가중치가 전부 0 이면 균등하게 뽑는다.</summary>
        private static PartsDataRow PickWeighted(List<PartsDataRow> candidates, System.Random rng)
        {
            if (candidates == null || candidates.Count == 0) return null;

            long total = 0;
            foreach (var c in candidates) total += c.AppearWeight > 0 ? c.AppearWeight : 0;
            if (total <= 0) return candidates[rng.Next(candidates.Count)];

            long r = (long)(rng.NextDouble() * total);
            foreach (var c in candidates)
            {
                long w = c.AppearWeight > 0 ? c.AppearWeight : 0;
                if (r < w) return c;
                r -= w;
            }
            return candidates[candidates.Count - 1];
        }
    }
}
