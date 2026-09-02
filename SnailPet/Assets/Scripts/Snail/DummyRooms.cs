using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 스팀 없이도 파티 화면을 손볼 수 있게 세워 두는 가짜 방. <c>DummyData</c> 가 원본이다.
    ///
    /// 같은 <c>RoomNumber</c> 가 한 방이고, 이름은 그 방의 <b>첫 줄에 적힌 것만</b> 읽는다 —
    /// 같은 방에 이름이 여럿 적혀 있으면 어느 것이 맞는지 알 수 없기 때문이다.
    /// 줄마다의 <c>PartsId01~04</c> 가 그 사람의 달팽이 외형이다.
    ///
    /// 나가는 꼴을 <see cref="SteamHub.MemberLooks"/> 와 <b>똑같이</b> 맞춰 두었다.
    /// 그래야 받는 쪽이 진짜 방인지 더미인지 몰라도 된다.
    /// </summary>
    public static class DummyRooms
    {
        /// <summary>세울 방이 있는가. 시트가 비어 있으면 더미도 없다.</summary>
        public static bool Any => GameData.DummyData.Length > 0;

        /// <summary>
        /// 방 번호들. <b>시트에 나온 차례를 지킨다</b> — 목록에서 누른 줄 번호가 곧 이 차례다.
        /// </summary>
        public static int[] Numbers()
        {
            var rooms = new List<int>();
            foreach (var r in GameData.DummyData)
                if (!rooms.Contains(r.RoomNumber)) rooms.Add(r.RoomNumber);

            return rooms.ToArray();
        }

        /// <summary>그 방의 이름. 어느 줄에도 안 적혀 있으면 빈 문자열이다.</summary>
        public static string NameOf(int roomNumber)
        {
            foreach (var r in GameData.DummyData)
                if (r.RoomNumber == roomNumber && !string.IsNullOrEmpty(r.RoomName)) return r.RoomName;

            return "";
        }

        /// <summary>방 목록에 걸 이름들. <see cref="Numbers"/> 와 차례가 같다.</summary>
        public static string[] Names()
        {
            var rooms = Numbers();
            var names = new string[rooms.Length];
            for (int i = 0; i < rooms.Length; i++) names[i] = NameOf(rooms[i]);

            return names;
        }

        /// <summary>
        /// 그 방 사람들. 등급은 파츠에서 나오고, 크기·속도는 시트의 <c>Level</c> 이 정한다
        /// (남의 달팽이가 제 크기로 걷는 것과 같은 길이다).
        /// </summary>
        public static (string name, string look, bool me)[] MemberLooks(int roomNumber)
        {
            var rows = new List<(string, string, bool)>();

            foreach (var r in GameData.DummyData)
            {
                if (r.RoomNumber != roomNumber) continue;

                var look = LookOf(r);

                // 앞은 유저 이름(NickName), 카드 안의 이름은 <b>달팽이 이름</b>이다.
                // 참가자 줄이 그 둘을 따로 보여 준다.
                rows.Add((r.NickName,
                          SnailShare.WriteCard(r.SnailName, SnailBreeding.RarityOf(look), r.Level, look),
                          false));
            }
            return rows.ToArray();
        }

        /// <summary>PartsId 넷을 외형으로 세운다. 색을 쓰는 파츠는 첫 색을 입힌다.</summary>
        private static SnailAppearance LookOf(DummyDataRow row)
        {
            var look = new SnailAppearance();

            Wear(look, row.PartsId01);
            Wear(look, row.PartsId02);
            Wear(look, row.PartsId03);
            Wear(look, row.PartsId04);

            return look;
        }

        private static void Wear(SnailAppearance look, int partsId)
        {
            // 빈 칸이거나 지워진 파츠면 그 부위만 빠진다. 부화한 개체와 같은 규칙이다.
            if (partsId == 0 || !GameData.PartsDataById.TryGetValue(partsId, out var row)) return;

            look.Parts.Add(new SnailPartRef
            {
                PartsId = row.Id,
                Type = row.PartsType,
                ResourceKey = row.ResourceKey,
                ColorKey = row.IsUseColor && row.Colors != null && row.Colors.Length > 0
                         ? row.Colors[0] : null,
            });
        }
    }
}
