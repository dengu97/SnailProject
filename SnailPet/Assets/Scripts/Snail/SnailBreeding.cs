using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 방에서 알이 생기는 규칙.
    ///
    /// 부모는 <b>내 달팽이 + 방 안의 달팽이 하나</b>다. 알을 낳는 그 순간의 부모로 정해지고,
    /// 태어날 모습은 알에 실려 다닌다 — 부화할 때쯤이면 그 방에 아무도 안 남아 있을 수 있다.
    ///
    /// 파츠는 부위마다 두 부모의 것을 놓고 <see cref="PartsDataRow.AppearWeight"/> 비중으로 하나를
    /// 고른다. 색은 고른 파츠에 딸려 온다 — 파츠와 색을 따로 뽑으면 부모 어느 쪽에도 없던
    /// 조합이 나온다.
    ///
    /// 돌연변이(<see cref="Config.MutationWeight"/>)는 아직 안 건다. 요청에 없었다.
    /// </summary>
    public static class SnailBreeding
    {
        /// <summary>두 부모를 섞는다. 한쪽이 비어 있으면 다른 쪽을 그대로 물려준다.</summary>
        public static SnailAppearance Cross(SnailAppearance a, SnailAppearance b, System.Random rng)
        {
            rng ??= new System.Random();

            // 부위마다 후보를 모은다. 악세서리는 타고난 것이 아니므로 뺀다 —
            // 남에게서 받은 한 장은 입은 채로 오기 때문에 여기서 걸러야 한다.
            var byType = new Dictionary<PartsType, List<SnailPartRef>>();
            Collect(byType, a);
            Collect(byType, b);

            var child = new SnailAppearance();
            foreach (var kv in byType)
            {
                if (kv.Value.Count == 0) continue;
                child.Parts.Add(Pick(kv.Value, rng));
            }
            return child;
        }

        private static void Collect(Dictionary<PartsType, List<SnailPartRef>> byType, SnailAppearance look)
        {
            if (look == null) return;

            foreach (var p in look.Parts)
            {
                if (p.Accessory.HasValue) continue;
                if (!GameData.PartsDataById.ContainsKey(p.PartsId)) continue;

                if (!byType.TryGetValue(p.Type, out var list))
                    byType[p.Type] = list = new List<SnailPartRef>();
                list.Add(p);
            }
        }

        /// <summary>AppearWeight 비중 추첨. 가중치가 전부 0 이면 균등하게 뽑는다.</summary>
        private static SnailPartRef Pick(List<SnailPartRef> candidates, System.Random rng)
        {
            if (candidates.Count == 1) return candidates[0];

            long total = 0;
            foreach (var c in candidates) total += WeightOf(c);
            if (total <= 0) return candidates[rng.Next(candidates.Count)];

            long r = (long)(rng.NextDouble() * total);
            foreach (var c in candidates)
            {
                long w = WeightOf(c);
                if (r < w) return c;
                r -= w;
            }
            return candidates[candidates.Count - 1];
        }

        private static long WeightOf(SnailPartRef part) =>
            GameData.PartsDataById.TryGetValue(part.PartsId, out var row) && row.AppearWeight > 0
                ? row.AppearWeight : 0;

        /// <summary>
        /// 낳은 알이 어느 행인가. 부모 중 <b>높은 등급</b>을 따라간다.
        /// 그 등급의 알이 데이터에 없으면 (전설 등) 있는 것 중 가장 높은 것으로 내린다.
        /// 알 행은 껍데기일 뿐이고 태어날 모습은 이미 정해져 있지만, 부화 시간과
        /// 개체의 등급이 여기서 나온다.
        /// </summary>
        public static EggDataRow EggFor(RarityType mine, RarityType theirs)
        {
            var want = mine >= theirs ? mine : theirs;

            EggDataRow best = null;
            foreach (var e in GameData.EggData)
            {
                if (e.RarityType == want) return e;
                if (e.RarityType < want && (best == null || e.RarityType > best.RarityType)) best = e;
            }
            return best ?? (GameData.EggData.Length > 0 ? GameData.EggData[0] : null);
        }

        /// <summary>
        /// 이번에 재볼 확률. 실패가 쌓인 만큼 올라가고, 낳으면 부르는 쪽이 0 으로 되돌린다.
        /// </summary>
        public static double ChanceAfter(int fails) =>
            Config.CreateEggPercent + Config.CreateEggPlusPercent * fails;

        /// <summary>오늘이 며칠인가. 하루치 개수를 세는 데만 쓴다.</summary>
        public static string Today => System.DateTime.Now.ToString("yyyyMMdd");
    }
}
