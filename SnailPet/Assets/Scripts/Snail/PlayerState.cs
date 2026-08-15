using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>유저가 가진 달팽이 한 마리. 화면에 나와 있는 개체도 이 중 하나다.</summary>
    public sealed class OwnedSnail
    {
        /// <summary>개체 신원. 목록에서 고르고 교체할 때 이것으로 가리킨다.</summary>
        public int Id;

        /// <summary>아직 이름을 못 지었으면 null. UI 가 「이름 없음」으로 채운다.</summary>
        public string Name;

        /// <summary>나온 알의 등급을 그대로 물려받는다.</summary>
        public RarityType Rarity;

        /// <summary>타고난 외형. 부화할 때 정해지고 바뀌지 않는다.</summary>
        public SnailAppearance Appearance;

        public SnailGrowth Growth;

        /// <summary>
        /// 장착한 악세서리의 AccessoriesData.Id. 부위마다 하나만 낄 수 있다.
        ///
        /// 타고난 외형과 섞지 않는다 — 외형은 신원이고 이건 갈아입는 것이라,
        /// 섞으면 세이브에서 「타고난 것」과 「입은 것」을 구분할 수 없다.
        /// </summary>
        public readonly List<int> Equipped = new List<int>();

        /// <summary>화면에 그릴 것 = 타고난 파츠 + 장착한 악세서리.</summary>
        public SnailAppearance Dressed()
        {
            var dressed = new SnailAppearance();
            dressed.Parts.AddRange(Appearance.Parts);

            foreach (int id in Equipped)
            {
                if (!GameData.AccessoriesDataById.TryGetValue(id, out var row)) continue;
                dressed.Parts.Add(new SnailPartRef
                {
                    PartsId = row.Id,
                    Accessory = row.AccessoriesType,
                    ResourceKey = row.ResourceKey,
                });
            }
            return dressed;
        }

        /// <summary>지금 그 부위에 낀 악세서리. 없으면 0.</summary>
        public int EquippedAt(AccessoriesType type)
        {
            foreach (int id in Equipped)
                if (GameData.AccessoriesDataById.TryGetValue(id, out var row) && row.AccessoriesType == type)
                    return id;
            return 0;
        }

        /// <summary>
        /// 끼우거나 뺀다. 이미 낀 것을 다시 누르면 벗는다 (목업의 「다시 누르면 장착해제」).
        /// 같은 부위에 다른 것을 끼우면 먼저 것이 빠진다.
        /// 외형이 바뀌었으면 true — 부르는 쪽이 다시 합성해야 한다.
        /// </summary>
        public bool ToggleEquip(int accessoryId)
        {
            if (!GameData.AccessoriesDataById.TryGetValue(accessoryId, out var row)) return false;

            if (Equipped.Remove(accessoryId)) return true;   // 끼고 있던 것 → 벗기

            int worn = EquippedAt(row.AccessoriesType);
            if (worn != 0) Equipped.Remove(worn);
            Equipped.Add(accessoryId);
            return true;
        }
    }

    /// <summary>
    /// 유저가 가진 것 전부.
    ///
    /// 지금까지 달팽이 목록·음식·알이 화면마다 따로 만든 더미였고, 그래서 부화시킨
    /// 달팽이가 어디에도 들어가지 못했다. 그걸 담을 곳이다.
    ///
    /// 코인과 음식은 개수만 있으면 되므로 <see cref="Inventory"/> 하나에 같이 넣는다.
    /// 선물 지급이 이미 아이템 Id 로 들어오고 있어 그 경로를 그대로 쓴다.
    /// 반면 알과 달팽이는 같은 종류라도 낱개로 구분해야 해서 목록으로 둔다 —
    /// 알은 어떤 파츠가 나올지 알 수 없으니 개수로 뭉치면 안 된다.
    /// </summary>
    /// <summary>
    /// 설정 화면의 값 한 벌.
    ///
    /// UI 가 아니라 여기에 두는 것은 세이브 때문이다 — 세이브 층이 UI 를 참조하면
    /// 저장 형식이 화면 코드에 매인다. UI 는 이 값을 받아 그리고, 바뀌면 알리기만 한다.
    /// 필드 이름이 곧 세이브 파일의 열쇠라 바꾸면 옛 세이브를 못 읽는다.
    /// </summary>
    [System.Serializable]
    public struct PlayerOptions
    {
        public bool NoEggs;                                  // 알 생성 금지
        public bool HungryBubble, CareBubble, CoinBubble;    // 말풍선 알림 3종
        public bool AlwaysMax;                               // UI 항상 최대화

        /// <summary>0 = x1, 1 = x1.5, 2 = x2. 목업이 이 셋만 준다.</summary>
        public int ScaleStep;

        /// <summary>목업의 「보이는게 디폴트 값」 — 알림 셋은 켜짐, 나머지는 꺼짐.</summary>
        public static PlayerOptions Default =>
            new PlayerOptions { HungryBubble = true, CareBubble = true, CoinBubble = true };

        public float Scale => ScaleStep == 1 ? 1.5f : ScaleStep == 2 ? 2f : 1f;
    }

    public sealed class PlayerState
    {
        /// <summary>부화 칸 수. UnlockData 로 늘어나면 이 값이 바뀐다.</summary>
        public const int IncubatorSlots = 3;

        /// <summary>설정 화면의 값. 개체가 아니라 유저의 것이라 달팽이를 바꿔도 그대로다.</summary>
        public PlayerOptions Options = PlayerOptions.Default;

        /// <summary>채운 도감. 개체를 팔아도 남으므로 달팽이 목록과 따로 든다.</summary>
        public readonly List<GuideEntry> Guides = new List<GuideEntry>();

        public GuideEntry FindGuide(int guideId)
        {
            foreach (var g in Guides)
                if (g.GuideId == guideId) return g;
            return null;
        }

        /// <summary>화폐 아이템의 토큰. 말풍선 아트인 `[코인]` 과는 다른 행이다.</summary>
        public const string CoinToken = "[팽이코인]";

        public readonly List<OwnedSnail> Snails = new List<OwnedSnail>();

        /// <summary>보유 알. 같은 등급이어도 낱개로 들고 있는다.</summary>
        public readonly List<int> Eggs = new List<int>();

        /// <summary>코인과 음식. 아이템 Id 로 개수를 센다.</summary>
        public readonly Inventory Items = new Inventory();

        /// <summary>
        /// 즐겨찾기해 둔 음식. 개체가 아니라 <b>유저</b>의 것이라 달팽이를 바꿔도 그대로다.
        /// 지금은 별이 켜지는 표시까지만 한다.
        /// </summary>
        public readonly List<int> Favorites = new List<int>();

        public bool IsFavorite(int foodId) => Favorites.Contains(foodId);

        /// <summary>별을 눌렀다. 켜져 있으면 끄고 꺼져 있으면 켠다.</summary>
        public void ToggleFavorite(int foodId)
        {
            if (foodId <= 0) return;
            if (!Favorites.Remove(foodId)) Favorites.Add(foodId);
        }

        /// <summary>부화 중인 칸. eggId 가 0 이면 빈 칸이다.</summary>
        public readonly (int eggId, double remain)[] Incubator = new (int, double)[IncubatorSlots];

        /// <summary>화면에 나와 있는 개체의 <see cref="OwnedSnail.Id"/>.</summary>
        public int ActiveId;

        private int _nextId = 1;

        private static int _coinItemId = -1;
        public static int CoinItemId
        {
            get
            {
                if (_coinItemId < 0)
                    _coinItemId = GameData.IdByToken.TryGetValue(CoinToken, out int id) ? id : 0;
                return _coinItemId;
            }
        }

        public long Coins => Items.CountOf(CoinItemId);

        /// <summary>지금 화면에 나와 있는 개체. 한 마리도 없으면 null.</summary>
        public OwnedSnail Active
        {
            get
            {
                foreach (var s in Snails) if (s.Id == ActiveId) return s;
                return Snails.Count > 0 ? Snails[0] : null;
            }
        }

        public OwnedSnail AddSnail(SnailAppearance appearance, RarityType rarity)
        {
            var snail = new OwnedSnail
            {
                Id = _nextId++,
                Rarity = rarity,
                Appearance = appearance,
                Growth = new SnailGrowth(),
            };
            Snails.Add(snail);
            if (ActiveId == 0) ActiveId = snail.Id;
            return snail;
        }

        /// <summary>
        /// 세이브에서 되돌릴 때만 쓴다. 신원을 새로 매기지 않고 그대로 살린다 —
        /// 활성 개체가 <see cref="ActiveId"/> 로 적혀 있어서 번호가 바뀌면 못 찾는다.
        /// </summary>
        public void RestoreSnail(OwnedSnail snail)
        {
            Snails.Add(snail);
            if (snail.Id >= _nextId) _nextId = snail.Id + 1;
        }

        /// <summary>목록의 몇 번째를 화면에 낼지. 이미 그 개체면 false 를 돌려준다.</summary>
        public bool SetActiveByIndex(int index)
        {
            if (index < 0 || index >= Snails.Count) return false;
            if (Snails[index].Id == ActiveId) return false;
            ActiveId = Snails[index].Id;
            return true;
        }

        /// <summary>보유 악세서리를 옷장이 쓰는 (아이템, 개수) 목록으로. 0개는 빼고 낸다.</summary>
        public (int accessoryId, int count)[] OwnedAccessories()
        {
            var list = new List<(int, int)>();
            foreach (var a in GameData.AccessoriesData)
            {
                long n = Items.CountOf(a.Id);
                if (n > 0) list.Add((a.Id, (int)n));
            }
            return list.ToArray();
        }

        /// <summary>보유 음식을 UI 가 쓰는 (아이템, 개수) 목록으로. 0개는 빼고 낸다.</summary>
        public (int foodId, int count)[] OwnedFoods()
        {
            var list = new List<(int, int)>();
            foreach (var f in GameData.FoodData)
            {
                long n = Items.CountOf(f.Id);
                if (n > 0) list.Add((f.Id, (int)n));
            }
            return list.ToArray();
        }

        /// <summary>부화기의 남은 시간을 흘린다. 다 된 칸이 하나라도 생기면 true.</summary>
        public bool TickIncubator(double deltaSeconds)
        {
            bool changed = false;
            for (int i = 0; i < Incubator.Length; i++)
            {
                if (Incubator[i].eggId == 0 || Incubator[i].remain <= 0) continue;

                double before = Incubator[i].remain;
                Incubator[i].remain = System.Math.Max(0, before - deltaSeconds);

                // 초가 바뀔 때만 다시 그린다. 매 프레임 갱신할 이유가 없다.
                if ((int)before != (int)Incubator[i].remain || Incubator[i].remain <= 0) changed = true;
            }
            return changed;
        }

        /// <summary>목록의 알 하나를 빈 칸에 넣는다. 넣은 칸 번호, 못 넣었으면 -1.</summary>
        public int PutEggInIncubator(int listIndex)
        {
            if (listIndex < 0 || listIndex >= Eggs.Count) return -1;

            int slot = System.Array.FindIndex(Incubator, h => h.eggId == 0);
            if (slot < 0) return -1;

            int eggId = Eggs[listIndex];
            if (!GameData.EggDataById.TryGetValue(eggId, out var row)) return -1;

            Eggs.RemoveAt(listIndex);
            Incubator[slot] = (eggId, row.HatchTime);
            return slot;
        }

        /// <summary>다 된 칸을 비우고 그 알의 Id 를 돌려준다. 아직이면 0.</summary>
        public int TakeHatched(int slot)
        {
            if (slot < 0 || slot >= Incubator.Length) return 0;
            if (Incubator[slot].eggId == 0 || Incubator[slot].remain > 0) return 0;

            int eggId = Incubator[slot].eggId;
            Incubator[slot] = (0, 0);
            return eggId;
        }

        public override string ToString() =>
            $"달팽이 {Snails.Count}마리, 알 {Eggs.Count}개, 코인 {Coins}, 가방: {Items}";
    }
}
