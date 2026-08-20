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

        // ── 알 낳기 ──
        //
        // 이 셋은 개체에 딸린 값이다. 달팽이를 바꾸면 각자 제 몫을 따로 센다.

        /// <summary>다음으로 재볼 때까지 남은 초. 방에 있는 동안만 줄어든다.</summary>
        public double EggCooldown;

        /// <summary>낳지 못한 횟수. 다음 확률이 그만큼 올라가고, 낳으면 0 으로 돌아간다.</summary>
        public int EggFails;

        /// <summary>오늘 낳은 개수와 그 「오늘」이 언제인지(yyyyMMdd). 날이 바뀌면 다시 0 이다.</summary>
        public int EggsToday;
        public string EggDay;

        /// <summary>
        /// 오늘 더 낳을 수 있는가. 날이 바뀌었으면 세던 것을 여기서 접는다 —
        /// 자정에 맞춰 깨워 줄 사람이 없으므로 물어볼 때 확인하는 편이 맞다.
        /// </summary>
        public bool CanLayToday()
        {
            string today = SnailBreeding.Today;
            if (EggDay != today) { EggDay = today; EggsToday = 0; }

            return EggsToday < Config.CreateEggCount;
        }

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
    /// 가지고 있는 알 하나.
    ///
    /// 상점에서 산 알은 <see cref="Gene"/> 가 없다 — 부화할 때 그 알의 그룹에서 뽑는다.
    /// 방에서 낳은 알은 부모를 섞은 결과를 여기 싣고 다닌다. 부화할 때쯤이면 그 부모가
    /// 방에 없을 수 있으므로, 낳는 순간에 정해 두지 않으면 되찾을 길이 없다.
    /// </summary>
    public sealed class OwnedEgg
    {
        public int EggId;
        public SnailAppearance Gene;
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
        public readonly List<OwnedEgg> Eggs = new List<OwnedEgg>();

        /// <summary>
        /// 화면 구석에 놓인 채 아직 회수 안 한 알.
        ///
        /// 눌러야 <see cref="Eggs"/> 로 들어가는데, 그때까지 30분짜리 결과를 들고 있는
        /// 것이므로 나갔다 오면 없어지면 곤란하다. 화면에 있는 것은 게임 쪽이 그리고,
        /// 여기는 나갈 때와 들어올 때만 오간다.
        /// </summary>
        public readonly List<OwnedEgg> LooseEggs = new List<OwnedEgg>();

        /// <summary>목록에 그릴 알의 행 Id 만. UI 는 그림과 등급만 있으면 된다.</summary>
        public int[] EggIds()
        {
            var ids = new int[Eggs.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = Eggs[i].EggId;
            return ids;
        }

        public void AddEgg(int eggId, SnailAppearance gene = null) =>
            Eggs.Add(new OwnedEgg { EggId = eggId, Gene = gene });

        public int CountEggs(int eggId)
        {
            int n = 0;
            foreach (var e in Eggs) if (e.EggId == eggId) n++;
            return n;
        }

        /// <summary>
        /// 그 종류의 알 하나를 뺀다. <b>물려받은 것이 없는 알부터</b> 뺀다 —
        /// 같은 값이면 낳은 알보다 산 알을 먼저 내보내는 편이 손해가 적다.
        /// </summary>
        public bool RemoveEgg(int eggId)
        {
            for (int i = 0; i < Eggs.Count; i++)
                if (Eggs[i].EggId == eggId && Eggs[i].Gene == null) { Eggs.RemoveAt(i); return true; }

            for (int i = 0; i < Eggs.Count; i++)
                if (Eggs[i].EggId == eggId) { Eggs.RemoveAt(i); return true; }

            return false;
        }

        /// <summary>코인과 음식. 아이템 Id 로 개수를 센다.</summary>
        public readonly Inventory Items = new Inventory();

        /// <summary>
        /// 즐겨찾기해 둔 음식. 개체가 아니라 <b>유저</b>의 것이라 달팽이를 바꿔도 그대로다.
        /// 최소화 창의 칸에 <b>등록한 순서 그대로</b> 놓이므로 순서를 흩뜨리면 안 된다.
        /// </summary>
        public readonly List<int> Favorites = new List<int>();

        /// <summary>등록할 수 있는 개수. 최소화 창의 칸 수와 같다.</summary>
        public const int MaxFavorites = 2;

        public bool IsFavorite(int foodId) => Favorites.Contains(foodId);

        /// <summary>
        /// 별을 눌렀다. 켜져 있으면 끄고 꺼져 있으면 켠다.
        /// 자리가 없어 켜지 못하면 false — 부르는 쪽이 안내 문구를 띄운다.
        /// </summary>
        public bool ToggleFavorite(int foodId)
        {
            if (foodId <= 0) return false;
            if (Favorites.Remove(foodId)) return true;
            if (Favorites.Count >= MaxFavorites) return false;

            Favorites.Add(foodId);
            return true;
        }

        /// <summary>부화 중인 칸. eggId 가 0 이면 빈 칸이다.</summary>
        public readonly (int eggId, double remain)[] Incubator = new (int, double)[IncubatorSlots];

        /// <summary>
        /// 칸에 들어 있는 알이 물려받은 모습. 칸 번호로 나란히 놓는다 —
        /// 칸은 자리가 고정이라 목록처럼 밀리지 않는다.
        /// </summary>
        public readonly SnailAppearance[] IncubatorGenes = new SnailAppearance[IncubatorSlots];

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

            var egg = Eggs[listIndex];
            if (!GameData.EggDataById.TryGetValue(egg.EggId, out var row)) return -1;

            Eggs.RemoveAt(listIndex);
            Incubator[slot] = (egg.EggId, row.HatchTime);
            IncubatorGenes[slot] = egg.Gene;
            return slot;
        }

        /// <summary>
        /// 다 된 칸을 비우고 그 알을 돌려준다. 아직이면 eggId 가 0.
        /// 물려받은 모습이 있으면 같이 나온다 — 부화는 그걸 그대로 쓴다.
        /// </summary>
        public (int eggId, SnailAppearance gene) TakeHatched(int slot)
        {
            if (slot < 0 || slot >= Incubator.Length) return (0, null);
            if (Incubator[slot].eggId == 0 || Incubator[slot].remain > 0) return (0, null);

            int eggId = Incubator[slot].eggId;
            var gene = IncubatorGenes[slot];

            Incubator[slot] = (0, 0);
            IncubatorGenes[slot] = null;
            return (eggId, gene);
        }

        public override string ToString() =>
            $"달팽이 {Snails.Count}마리, 알 {Eggs.Count}개, 코인 {Coins}, 가방: {Items}";
    }
}
