using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SnailPet.Data;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 보유 상태를 디스크에 적고 다시 읽는다.
    ///
    /// 개체와 파츠는 <b>IdMap 의 번호</b>로 적는다. 아트 파일 이름(ResourceKey)은 바뀔 수
    /// 있지만 번호는 세이브와 묶인 신원이라 안 바뀐다 — 이름으로 적으면 아트를 정리하는
    /// 순간 남의 달팽이가 된다.
    ///
    /// JsonUtility 는 Dictionary 와 튜플을 다루지 못해서, 그대로 옮길 수 있는 모양의
    /// 클래스를 따로 둔다. 여기 필드 이름은 세이브 파일의 열쇠이므로 바꾸면 기존 파일을
    /// 못 읽는다.
    /// </summary>
    public static class SaveFile
    {
        /// <summary>세이브 형식 번호. 모양을 바꿀 때 올리고, 읽을 때 다르면 버린다.</summary>
        public const int Version = 1;

        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

        [Serializable]
        private sealed class PartDto
        {
            public int parts;
            public string color;
        }

        [Serializable]
        private sealed class SnailDto
        {
            public int id;
            public string name;
            public int rarity;
            public int level;
            public double full, happy, progress;
            public PartDto[] parts;

            /// <summary>
            /// 장착한 악세서리. 나중에 더한 칸이라 옛 세이브에는 없는데,
            /// JsonUtility 가 없는 필드를 빈 배열로 두므로 형식 번호를 올릴 필요가 없다.
            /// </summary>
            public int[] equipped;
        }

        [Serializable]
        private sealed class ItemDto
        {
            public int id;
            public long count;
        }

        [Serializable]
        private sealed class SlotDto
        {
            public int eggId;
            public double remain;
        }

        [Serializable]
        private sealed class RootDto
        {
            public int version;
            public int activeId;
            public SnailDto[] snails;
            public int[] eggs;
            public ItemDto[] items;
            public SlotDto[] incubator;

            /// <summary>즐겨찾기한 음식. 나중에 더한 칸이라 옛 세이브에는 없다.</summary>
            public int[] favorites;

            /// <summary>
            /// 설정 화면의 값. 역시 나중에 더한 칸이라 옛 세이브에는 없다.
            /// 없으면 JsonUtility 가 전부 false·0 을 넣으므로, 읽는 쪽에서 기본값을 가른다.
            /// </summary>
            public PlayerOptions options;
            public bool hasOptions;
        }

        public static void Save(PlayerState player)
        {
            if (player == null) return;

            var root = new RootDto
            {
                version = Version,
                activeId = player.ActiveId,
                eggs = player.Eggs.ToArray(),
                favorites = player.Favorites.ToArray(),
                options = player.Options,
                hasOptions = true,
                snails = new SnailDto[player.Snails.Count],
                incubator = new SlotDto[player.Incubator.Length],
            };

            for (int i = 0; i < player.Snails.Count; i++)
            {
                var s = player.Snails[i];
                var parts = new PartDto[s.Appearance.Parts.Count];
                for (int j = 0; j < parts.Length; j++)
                    parts[j] = new PartDto
                    {
                        parts = s.Appearance.Parts[j].PartsId,
                        color = s.Appearance.Parts[j].ColorKey,
                    };

                root.snails[i] = new SnailDto
                {
                    id = s.Id,
                    name = s.Name,
                    rarity = (int)s.Rarity,
                    level = s.Growth.Level,
                    full = s.Growth.FullPoint,
                    happy = s.Growth.HappyPoint,
                    progress = s.Growth.LevelUpProgress,
                    parts = parts,
                    equipped = s.Equipped.ToArray(),
                };
            }

            for (int i = 0; i < root.incubator.Length; i++)
                root.incubator[i] = new SlotDto
                {
                    eggId = player.Incubator[i].eggId,
                    remain = player.Incubator[i].remain,
                };

            var items = new List<ItemDto>();
            foreach (var kv in player.Items.Entries)
                items.Add(new ItemDto { id = kv.Key, count = kv.Value });
            root.items = items.ToArray();

            try
            {
                // JSON 은 BOM 이 있으면 파서에 따라 거부당한다
                File.WriteAllText(Path, JsonUtility.ToJson(root, true), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SnailPet] 세이브를 쓰지 못했습니다: " + e.Message);
            }
        }

        /// <summary>세이브가 없거나 읽을 수 없으면 null. 부르는 쪽이 새로 시작한다.</summary>
        public static PlayerState Load()
        {
            if (!File.Exists(Path)) return null;

            RootDto root;
            try { root = JsonUtility.FromJson<RootDto>(File.ReadAllText(Path)); }
            catch (Exception e) { return Discard("읽지 못했습니다: " + e.Message); }

            if (root == null) return Discard("내용이 비어 있습니다");
            if (root.version != Version) return Discard($"형식이 {root.version} 라 지금({Version})과 다릅니다");

            var player = new PlayerState();

            if (root.snails != null)
                foreach (var s in root.snails)
                {
                    var snail = new OwnedSnail
                    {
                        Id = s.id,
                        Name = s.name,
                        Rarity = (RarityType)s.rarity,
                        Appearance = new SnailAppearance(),
                        Growth = new SnailGrowth(),
                    };

                    if (s.parts != null)
                        foreach (var p in s.parts)
                        {
                            // 데이터에서 빠진 파츠는 되살릴 수 없다. 그 부위만 빠진 채로 둔다.
                            if (!GameData.PartsDataById.TryGetValue(p.parts, out var row))
                            {
                                Debug.LogWarning($"[SnailPet] 세이브의 파츠 {p.parts} 가 데이터에 없습니다");
                                continue;
                            }
                            snail.Appearance.Parts.Add(new SnailPartRef
                            {
                                PartsId = row.Id,
                                Type = row.PartsType,
                                ResourceKey = row.ResourceKey,
                                // JsonUtility 는 null 을 "" 로 적는다. 색을 안 쓰는 파츠는
                                // 갓 부화한 개체와 똑같이 null 로 되돌려 놓는다.
                                ColorKey = string.IsNullOrEmpty(p.color) ? null : p.color,
                            });
                        }

                    // 데이터에서 빠진 악세서리는 되살리지 않는다. 그냥 안 낀 상태가 된다.
                    if (s.equipped != null)
                        foreach (int id in s.equipped)
                            if (GameData.AccessoriesDataById.ContainsKey(id)) snail.Equipped.Add(id);

                    snail.Growth.Restore(s.level, s.full, s.happy, s.progress);
                    player.RestoreSnail(snail);
                }

            if (root.eggs != null) player.Eggs.AddRange(root.eggs);
            if (root.favorites != null) player.Favorites.AddRange(root.favorites);

            // 설정이 없던 시절의 세이브는 전부 꺼진 것처럼 읽힌다. 그러면 알림 셋이
            // 꺼진 채로 시작해 기본값과 어긋나므로, 적혀 있을 때만 가져온다.
            player.Options = root.hasOptions ? root.options : PlayerOptions.Default;
            if (root.items != null) foreach (var it in root.items) player.Items.Add(it.id, it.count);

            if (root.incubator != null)
                for (int i = 0; i < player.Incubator.Length && i < root.incubator.Length; i++)
                    player.Incubator[i] = (root.incubator[i].eggId, root.incubator[i].remain);

            player.ActiveId = root.activeId;

            // 몸통이 없으면 발선을 잴 수 없어 합성이 무너진다. 그런 개체는 낼 수 없다.
            if (player.Active == null || !player.Active.Appearance.TryGetBody(out _))
                return Discard("되살린 개체에 몸통이 없습니다");

            return player;
        }

        /// <summary>못 읽은 세이브는 지우지 않고 옆으로 치운다. 원인을 볼 수 있어야 한다.</summary>
        private static PlayerState Discard(string why)
        {
            Debug.LogWarning("[SnailPet] 세이브를 버립니다 — " + why);
            try
            {
                string aside = Path + ".bad";
                if (File.Exists(aside)) File.Delete(aside);
                File.Move(Path, aside);
            }
            catch (Exception e) { Debug.LogWarning("[SnailPet] 치워두지도 못했습니다: " + e.Message); }
            return null;
        }
    }
}
