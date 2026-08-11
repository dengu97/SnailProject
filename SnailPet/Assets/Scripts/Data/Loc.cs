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
        /// <summary>지금 언어. en 열이 추가되면 여기에 따라 고른다.</summary>
        public static string Language = "kr";

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

            return string.IsNullOrEmpty(row.Kr) ? token : row.Kr;
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

            if (GameData.LanguageDataById.TryGetValue(id, out var row) && !string.IsNullOrEmpty(row.Kr))
                return row.Kr;

            string token = GameData.TokenById.TryGetValue(id, out string t) ? t : "#" + id;
            Warn(token, "LanguageData 에 없음 (id " + id + ")");
            return token;
        }

        /// <summary>자리표시자가 있는 글자. 예: `[레벨]` = "{0}살" → Format("[레벨]", 3) → "3살".</summary>
        public static string Format(string token, params object[] args)
        {
            string text = Text(token);
            try { return string.Format(text, args); }
            catch (System.FormatException)
            {
                // 번역문에 중괄호를 잘못 넣으면 여기로 온다. 앱을 죽일 일은 아니다.
                Warn(token, "자리표시자가 인자와 맞지 않음: " + text);
                return text;
            }
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
