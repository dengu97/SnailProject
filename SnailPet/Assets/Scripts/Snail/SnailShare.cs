using System.Collections.Generic;
using SnailPet.Data;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 남에게 보여 줄 달팽이 하나를 글자로 바꾸고 되돌린다.
    ///
    /// 멀티는 <b>비동기</b>라 위치를 맞추지 않는다. 그래서 오갈 것은 「어떻게 생겼는가」뿐이고,
    /// 로비 멤버 데이터에 문자열 하나로 실어 보내면 끝난다 — P2P 패킷이 필요 없다.
    ///
    /// 형식은 <c>파츠Id:색키</c> 를 <c>|</c> 로 이은 것이다. 색을 안 쓰는 파츠는 색 자리가 빈다.
    /// 이름 같은 것은 스팀이 이미 들고 있으므로 여기 담지 않는다.
    ///
    /// 파츠 Id 를 쓰는 이유는 세이브와 같다 — 아트 파일 이름은 바뀌어도 IdMap 의 번호는 안 바뀐다.
    /// 상대가 나보다 옛 데이터를 쓰고 있으면 모르는 Id 가 올 수 있는데, 그 부위만 빼고 그린다.
    /// </summary>
    public static class SnailShare
    {
        private const char PartSep = '|', ColorSep = ':';

        /// <summary>로비 멤버 데이터의 키. 양쪽이 같은 이름을 봐야 한다.</summary>
        public const string Key = "snail";

        /// <summary>머리말과 파츠를 가르는 글자. 파츠 쪽에서 안 쓰는 것이어야 한다.</summary>
        private const char HeadSep = '\n';

        /// <summary>
        /// 남에게 보여 줄 한 장. 이름·등급·레벨·외형을 한 문자열에 담는다.
        /// 스팀 닉네임은 스팀이 이미 들고 있으므로 여기 넣지 않는다.
        ///
        /// 레벨이 필요한 것은 <b>남의 달팽이도 제 크기와 속도로 걷게</b> 하기 위해서다.
        /// 크기·속도는 LevelData 가 정하므로 레벨 하나만 오면 나머지는 각자 데이터에서 읽는다.
        /// </summary>
        public static string WriteCard(string name, RarityType rarity, int level, SnailAppearance look) =>
            (name ?? "") + HeadSep + rarity + HeadSep + level + HeadSep + Write(look);

        /// <summary>
        /// 받은 한 장을 되돌린다. 외형을 못 읽으면 look 이 null 이다.
        /// 레벨을 안 실어 보내던 때의 글자도 읽히며, 그때는 level 이 0(모름) 이다.
        /// </summary>
        public static (string name, RarityType rarity, int level, SnailAppearance look) ReadCard(string text)
        {
            if (string.IsNullOrEmpty(text)) return ("", RarityType.Common, 0, null);

            var parts = text.Split(HeadSep);

            // 머리말이 없으면 맨 처음 형식(외형만)이다. 그건 그대로 외형으로 읽는다.
            if (parts.Length < 3) return ("", RarityType.Common, 0, Read(text));

            System.Enum.TryParse(parts[1], out RarityType rarity);

            // 레벨 칸이 붙기 전의 글자는 세 칸이고 마지막이 외형이다
            if (parts.Length < 4) return (parts[0], rarity, 0, Read(parts[2]));

            int.TryParse(parts[2], out int level);
            return (parts[0], rarity, level, Read(parts[3]));
        }

        private static string Write(SnailAppearance look)
        {
            if (look == null || look.Parts.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            foreach (var p in look.Parts)
            {
                if (sb.Length > 0) sb.Append(PartSep);
                sb.Append(p.PartsId).Append(ColorSep).Append(p.ColorKey ?? "");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 외형 부분만 되돌린다. 못 읽으면 null — 부르는 쪽이 실루엣을 쓰면 된다.
        ///
        /// <b>한 장(카드) 전체를 여기 넣으면 안 된다.</b> 머리말이 첫 파츠에 붙어 그 부위가
        /// 통째로 빠진다(껍질 없는 달팽이). 밖에서는 <see cref="ReadCard"/> 만 쓰도록 감춰 둔다.
        /// </summary>
        private static SnailAppearance Read(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var look = new SnailAppearance();
            foreach (var chunk in text.Split(PartSep))
            {
                int cut = chunk.IndexOf(ColorSep);
                if (cut < 0) continue;

                if (!int.TryParse(chunk.Substring(0, cut), out int id)) continue;
                if (!GameData.PartsDataById.TryGetValue(id, out var row)) continue;   // 모르는 파츠는 뺀다

                string color = chunk.Substring(cut + 1);
                look.Parts.Add(new SnailPartRef
                {
                    PartsId = row.Id,
                    Type = row.PartsType,
                    ResourceKey = row.ResourceKey,
                    ColorKey = SnailParts.KeepColor(row, color),
                });
            }

            return look.Parts.Count > 0 ? look : null;
        }
    }
}
