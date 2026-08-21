using System.Text;

namespace SnailPet.Data
{
    /// <summary>
    /// 자리표시자에 넣은 말에 맞춰 뒤따르는 조사를 고른다.
    ///
    /// 「{0}를 보냈습니다」 에 받침 있는 이름이 들어가면 「이름 없음<b>를</b>」 이 된다.
    /// 시트에 「을(를)」 처럼 둘 다 적어 두는 방법도 있지만 그건 화면에 그대로 나온다.
    /// 그래서 넣는 순간에 마지막 글자의 받침을 보고 하나를 고른다.
    ///
    /// <b>자리표시자 바로 뒤에 붙은 것만</b> 손댄다. 그것도 조사 뒤가 문장 끝이거나
    /// 한글이 아닐 때만이다 — 「{0}이름을」 처럼 낱말이 이어지는 것을 조사로 착각하면 안 된다.
    /// </summary>
    public static class Josa
    {
        /// <summary>받침 있을 때 · 없을 때. 「으로」는 ㄹ 받침이 예외라 따로 다룬다.</summary>
        private static readonly string[][] Pairs =
        {
            new[] { "을", "를" },
            new[] { "이", "가" },
            new[] { "은", "는" },
            new[] { "과", "와" },
            new[] { "아", "야" },
        };

        /// <summary>시트에 이미 「을(를)」 꼴로 적혀 있는 것도 하나로 줄여 준다.</summary>
        private static readonly string[][] Hedges =
        {
            new[] { "을(를)", "을", "를" },
            new[] { "를(을)", "을", "를" },
            new[] { "이(가)", "이", "가" },
            new[] { "가(이)", "이", "가" },
            new[] { "은(는)", "은", "는" },
            new[] { "는(은)", "은", "는" },
            new[] { "과(와)", "과", "와" },
            new[] { "와(과)", "과", "와" },
        };

        /// <summary>
        /// 자리표시자를 채우면서 조사를 고친다. 우리가 다룰 수 없는 꼴(<c>{0:0.#}</c> 등)이면
        /// 그냥 <see cref="string.Format(string, object[])"/> 에 넘긴다.
        /// </summary>
        public static string Format(string template, object[] args)
        {
            if (string.IsNullOrEmpty(template) || args == null || args.Length == 0)
                return template;

            var sb = new StringBuilder(template.Length + 16);

            for (int i = 0; i < template.Length; )
            {
                char c = template[i];

                // 중괄호를 글자로 쓰려면 두 번 적는다. string.Format 과 같은 규칙이다.
                if (c == '{' && i + 1 < template.Length && template[i + 1] == '{') { sb.Append('{'); i += 2; continue; }
                if (c == '}' && i + 1 < template.Length && template[i + 1] == '}') { sb.Append('}'); i += 2; continue; }

                if (c != '{') { sb.Append(c); i++; continue; }

                int close = template.IndexOf('}', i + 1);
                if (close < 0) { sb.Append(c); i++; continue; }

                string inner = template.Substring(i + 1, close - i - 1);
                if (!int.TryParse(inner, out int index) || index < 0 || index >= args.Length)
                    return string.Format(template, args);

                string value = args[index] == null ? "" : args[index].ToString();
                sb.Append(value);

                i = close + 1;
                i += Fix(sb, template, i, value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 자리표시자 뒤에 조사가 붙어 있으면 골라 넣는다. 넘긴 글자 수를 돌려준다(없으면 0).
        /// </summary>
        private static int Fix(StringBuilder sb, string template, int at, string value)
        {
            if (at >= template.Length || value.Length == 0) return 0;

            bool batchim = HasFinalConsonant(value, out bool rieul);

            foreach (var h in Hedges)
                if (Follows(template, at, h[0]))
                {
                    sb.Append(batchim ? h[1] : h[2]);
                    return h[0].Length;
                }

            // 「으로/로」는 ㄹ 받침이 예외다. 「방울로」 이지 「방울으로」 가 아니다.
            if (Follows(template, at, "으로") || Follows(template, at, "로"))
            {
                bool with = batchim && !rieul;
                sb.Append(with ? "으로" : "로");
                return Follows(template, at, "으로") ? 2 : 1;
            }

            foreach (var p in Pairs)
                if (Follows(template, at, p[0]) || Follows(template, at, p[1]))
                {
                    sb.Append(batchim ? p[0] : p[1]);
                    return 1;
                }

            return 0;
        }

        /// <summary>
        /// 그 자리에 이 조사가 있는가. <b>조사 뒤가 문장 끝이거나 한글이 아닐 때만</b> 조사로 본다.
        /// 「{0}이름을」 의 「이」 를 주격 조사로 착각하지 않으려는 것이다.
        /// </summary>
        private static bool Follows(string template, int at, string josa)
        {
            if (at + josa.Length > template.Length) return false;
            if (string.CompareOrdinal(template, at, josa, 0, josa.Length) != 0) return false;

            int next = at + josa.Length;
            return next >= template.Length || !IsHangul(template[next]);
        }

        private static bool IsHangul(char c) => c >= 0xAC00 && c <= 0xD7A3;

        /// <summary>
        /// 마지막 글자에 받침이 있는가. 한글과 숫자만 본다 —
        /// 그 밖(영문 등)은 받침 없음으로 두고, 필요해지면 그때 표를 늘리면 된다.
        /// </summary>
        private static bool HasFinalConsonant(string value, out bool rieul)
        {
            rieul = false;
            if (string.IsNullOrEmpty(value)) return false;

            char last = value[value.Length - 1];

            if (IsHangul(last))
            {
                int jong = (last - 0xAC00) % 28;
                rieul = jong == 8;                  // ㄹ
                return jong != 0;
            }

            if (last >= '0' && last <= '9')
            {
                // 0영 1일 2이 3삼 4사 5오 6육 7칠 8팔 9구
                bool[] has = { true, true, false, true, false, false, true, true, true, false };
                bool[] isRieul = { false, true, false, false, false, false, false, true, true, false };

                int n = last - '0';
                rieul = isRieul[n];
                return has[n];
            }

            return false;
        }
    }
}
