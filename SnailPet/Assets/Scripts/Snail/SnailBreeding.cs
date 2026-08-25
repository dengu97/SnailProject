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
    /// 돌연변이는 <b>부모와 같은 저울에 올라간다</b> — 부위마다 부모 둘의 AppearWeight 옆에
    /// <see cref="Config.MutationWeight"/> 를 한 칸 더 놓고 추첨해서, 그 칸에 걸리면 부모
    /// 어느 쪽도 아닌 파츠가 나온다. 무엇이 나오는지는 부모 파츠가 속한
    /// <c>PartsData.MutationGroup</c> 이 정한다(2026-08-21).
    /// </summary>
    public static class SnailBreeding
    {
        /// <summary>두 부모를 섞는다. 한쪽이 비어 있으면 다른 쪽을 그대로 물려준다.</summary>
        public static SnailAppearance Cross(SnailAppearance a, SnailAppearance b, System.Random rng) =>
            Cross(a, b, rng, out _);

        /// <summary>섞은 결과와, 그중 몇 부위가 돌연변이였는지.</summary>
        public static SnailAppearance Cross(SnailAppearance a, SnailAppearance b, System.Random rng,
                                            out int mutations)
        {
            mutations = 0;
            var child = CrossInner(a, b, rng, ref mutations);
            return child;
        }

        private static SnailAppearance CrossInner(SnailAppearance a, SnailAppearance b, System.Random rng,
                                                  ref int mutations)
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

                child.Parts.Add(Pick(kv.Value, kv.Key, rng, out bool mutated));
                if (mutated) mutations++;
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

        /// <summary>
        /// 부위 하나를 정한다. 저울에는 부모 파츠들의 AppearWeight 와 돌연변이 몫이 같이 올라간다.
        /// 돌연변이 칸에 걸렸는데 뽑을 그룹이 없으면 없던 셈 치고 부모 것을 물려준다.
        /// </summary>
        private static SnailPartRef Pick(List<SnailPartRef> candidates, PartsType type,
                                         System.Random rng, out bool mutated)
        {
            mutated = false;

            long parents = 0;
            foreach (var c in candidates) parents += WeightOf(c);

            long mutation = System.Math.Max(0, Config.MutationWeight);
            long total = parents + mutation;
            if (total <= 0) return candidates[rng.Next(candidates.Count)];

            long r = (long)(rng.NextDouble() * total);

            // 부모 몫을 먼저 훑는다. 다 지나가고도 남으면 그게 돌연변이 칸이다.
            foreach (var c in candidates)
            {
                long w = WeightOf(c);
                if (r < w) return c;
                r -= w;
            }

            var mutant = Mutate(candidates, type, rng);
            if (mutant.HasValue) { mutated = true; return mutant.Value; }

            return candidates[candidates.Count - 1];
        }

        /// <summary>
        /// 돌연변이 파츠 하나를 뽑는다. 후보는 <b>부모 둘의 MutationGroup 을 합친 것</b> 안에서,
        /// <b>같은 부위</b>인 행들이다 — 그룹에 다른 부위가 섞여 있어도 껍질이 몸통으로 바뀌면 안 된다.
        /// 뽑을 것이 없으면(부모 파츠에 그룹이 안 적혀 있으면) null.
        ///
        /// 부모가 쓰던 행도 그 그룹에 있으면 후보에 그대로 남는다. 색만 다른 행이 여럿인
        /// 구성이라, 「같은 껍질의 다른 색」이 나오는 것도 돌연변이의 한 결과로 본다.
        /// </summary>
        private static SnailPartRef? Mutate(List<SnailPartRef> parents, PartsType type, System.Random rng)
        {
            var groups = new HashSet<int>();
            foreach (var p in parents)
                if (GameData.PartsDataById.TryGetValue(p.PartsId, out var row) && row.MutationGroup.HasValue)
                    groups.Add(row.MutationGroup.Value);

            if (groups.Count == 0) return null;

            var pool = new List<PartsDataRow>();
            long total = 0;
            foreach (var row in GameData.PartsData)
            {
                if (row.PartsType != type) continue;
                if (!row.MutationGroup.HasValue || !groups.Contains(row.MutationGroup.Value)) continue;

                pool.Add(row);
                total += MutationWeightOf(row);
            }
            if (pool.Count == 0) return null;

            var picked = pool[pool.Count - 1];
            if (total <= 0) picked = pool[rng.Next(pool.Count)];
            else
            {
                long r = (long)(rng.NextDouble() * total);
                foreach (var row in pool)
                {
                    long w = MutationWeightOf(row);
                    if (r < w) { picked = row; break; }
                    r -= w;
                }
            }

            string color = null;
            if (picked.IsUseColor && picked.Colors != null && picked.Colors.Length > 0)
                color = picked.Colors[rng.Next(picked.Colors.Length)];

            return new SnailPartRef
            {
                PartsId = picked.Id,
                Type = picked.PartsType,
                ResourceKey = picked.ResourceKey,
                ColorKey = color,
            };
        }

        /// <summary>그 행의 돌연변이 가중치. 안 적혀 있으면 컨피그의 값을 쓴다.</summary>
        private static long MutationWeightOf(PartsDataRow row) =>
            System.Math.Max(0, row.MutationWeight ?? Config.MutationWeight);

        private static long WeightOf(SnailPartRef part) =>
            GameData.PartsDataById.TryGetValue(part.PartsId, out var row) && row.AppearWeight > 0
                ? row.AppearWeight : 0;

        /// <summary>
        /// 섞은 결과의 등급. <b>파츠 중 가장 높은 등급</b>이다.
        ///
        /// 부모의 등급이 아니라 실제로 물려받은 파츠를 본다 — 에픽 부모에게서 나왔어도
        /// 일반 파츠만 물려받았으면 일반이고, 반대로 돌연변이로 높은 파츠가 하나 나오면
        /// 그 알은 그 등급이 된다(2026-08-22 결정).
        /// </summary>
        public static RarityType RarityOf(SnailAppearance look)
        {
            var best = RarityType.Common;
            if (look == null) return best;

            foreach (var p in look.Parts)
            {
                if (p.Accessory.HasValue) continue;
                if (!GameData.PartsDataById.TryGetValue(p.PartsId, out var row)) continue;

                if (row.RarityType > best) best = row.RarityType;
            }
            return best;
        }

        /// <summary>
        /// 그 등급의 알 행. 데이터에 그 등급이 없으면 (전설 등) 있는 것 중 가장 높은 것으로 내린다.
        /// 알 행은 껍데기일 뿐이고 태어날 모습은 이미 정해져 있지만, 부화 시간과
        /// 개체의 등급이 여기서 나온다.
        /// </summary>
        public static EggDataRow EggFor(RarityType want)
        {
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
