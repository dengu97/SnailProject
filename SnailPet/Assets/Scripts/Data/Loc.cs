using UnityEngine;

namespace SnailPet.Data
{
    /// <summary>
    /// UI 에 나가는 글자를 LanguageData 에서 가져온다.
    ///
    /// 코드에 한글을 직접 쓰면 나중에 영어를 붙일 때 전부 찾아다녀야 한다.
    /// 토큰으로만 부르고 실제 문자열은 테이블에 둔다.
    ///
    /// 지금은 kr 열 하나뿐이라 <see cref="Language"/> 를 봐도 갈 곳이 없지만,
    /// en 열이 생기면 이 클래스 한 곳만 고치면 된다.
    /// </summary>
    public static class Loc
    {
        public const string Korean = "kr", English = "en";

        /// <summary>지금 언어. LanguageData 의 어느 칸을 읽을지 정한다.</summary>
        public static string Language = Korean;

        /// <summary>
        /// 그 행에서 지금 언어의 글. 그 칸이 비어 있으면 <b>한글로 되돌린다</b> —
        /// 번역이 덜 된 줄을 빈칸으로 두면 화면에서 그 자리가 통째로 사라진다.
        /// </summary>
        private static string Pick(LanguageDataRow row) =>
            Language == English && !string.IsNullOrEmpty(row.En) ? row.En : row.Kr;

        /// <summary>
        /// 토큰에 해당하는 글자. 없으면 토큰을 그대로 돌려준다.
        /// 화면에 `[레벨]` 이 보이면 테이블에 그 행이 없다는 뜻이다.
        /// </summary>
        public static string Text(string token)
        {
            if (string.IsNullOrEmpty(token)) return string.Empty;

            if (!GameData.IdByToken.TryGetValue(token, out int id))
            {
                Warn(token, "알 수 없는 토큰");
                return token;
            }
            if (!GameData.LanguageDataById.TryGetValue(id, out var row))
            {
                Warn(token, "LanguageData 에 없음");
                return token;
            }

            string text = Pick(row);
            return string.IsNullOrEmpty(text) ? token : text;
        }

        /// <summary>
        /// 이미 ID 로 들어온 글자. 시트의 NameId · InfoId 열이 이 형태다.
        /// 못 찾으면 토큰을, 그것도 없으면 `#숫자` 를 돌려준다 — 화면에 뜨면 바로 눈에 띈다.
        /// </summary>
        public static string ById(int id)
        {
            // 0 은 「아직 안 적음」이다. 시트의 빈 칸이 그대로 0 으로 들어온다.
            // 화면에 #0 을 띄우느니 비워 둔다. 다만 한 번은 알린다.
            if (id <= 0) { Warn("#0", "비어 있는 Id 를 글자로 찾으려 했습니다"); return string.Empty; }

            if (GameData.LanguageDataById.TryGetValue(id, out var row))
            {
                string text = Pick(row);
                if (!string.IsNullOrEmpty(text)) return text;
            }

            string token = GameData.TokenById.TryGetValue(id, out string t) ? t : "#" + id;
            Warn(token, "LanguageData 에 없음 (id " + id + ")");
            return token;
        }

        /// <summary>
        /// 자리표시자가 있는 글자. 예: `[레벨]` = "{0}살" → Format("[레벨]", 3) → "3살".
        /// 자리표시자 뒤에 조사가 붙어 있으면 넣은 말의 받침에 맞춰 골라 준다 (<see cref="Josa"/>).
        /// </summary>
        public static string Format(string token, params object[] args)
        {
            string text = Text(token);
            try { return Josa.Format(text, args); }
            catch (System.FormatException)
            {
                // 번역문에 중괄호를 잘못 넣으면 여기로 온다. 앱을 죽일 일은 아니다.
                Warn(token, "자리표시자가 인자와 맞지 않음: " + text);
                return text;
            }
        }

        /// <summary>
        /// 한글 문구로 토큰을 되찾는다. 그런 문구가 없거나 <b>둘 이상이 같은 문구를 쓰면</b> null.
        ///
        /// 프리팹에 구워진 글자에는 어느 토큰으로 지었는지가 안 남아 있다. 언어를 바꾸려면
        /// 그걸 알아야 하므로 구울 때의 한글로 거꾸로 찾는다. 겹치는 문구는 어느 쪽인지 알 수
        /// 없으니 건드리지 않는다 — 잘못 붙이면 엉뚱한 글자로 바뀐다.
        /// </summary>
        public static string TokenOfKorean(string korean)
        {
            if (string.IsNullOrEmpty(korean)) return null;

            _byKorean ??= BuildByKorean();
            return _byKorean.TryGetValue(korean, out string token) ? token : null;
        }

        private static System.Collections.Generic.Dictionary<string, string> _byKorean;

        private static System.Collections.Generic.Dictionary<string, string> BuildByKorean()
        {
            var map = new System.Collections.Generic.Dictionary<string, string>();

            foreach (var row in GameData.LanguageData)
            {
                if (row == null || string.IsNullOrEmpty(row.Kr)) continue;
                if (!GameData.TokenById.TryGetValue(row.Id, out string token)) continue;

                // 겹치면 자리만 잡아 두고 값을 비운다. 아래에서 null 은 「모르겠다」로 읽힌다.
                map[row.Kr] = map.ContainsKey(row.Kr) ? null : token;
            }
            return map;
        }

        private static readonly System.Collections.Generic.HashSet<string> _warned =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>같은 토큰으로 매 프레임 경고가 쏟아지지 않게 한 번만 알린다.</summary>
        private static void Warn(string token, string why)
        {
            if (!_warned.Add(token)) return;
            Debug.LogWarning($"[SnailPet] 언어 키 {token}: {why}");
        }
    }
}
