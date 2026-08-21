using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SnailPet.Pipeline
{
    public enum FieldKind { Int, NullableInt, Double, NullableDouble, Bool, String, DateTime, ListInt, ListString, Enum, Unknown }

    /// <summary>2행에 적힌 타입 문자열을 해석한 결과.</summary>
    public sealed class FieldType
    {
        public FieldKind Kind;
        public string EnumName;      // Kind == Enum 일 때만
        public string Raw;

        public static FieldType Parse(string raw)
        {
            var t = new FieldType { Raw = raw ?? "", Kind = FieldKind.Unknown };
            string s = (raw ?? "").Trim();
            if (s.Length == 0) return t;

            var m = Regex.Match(s, @"^enum\s*<\s*(\w+)\s*>$", RegexOptions.IgnoreCase);
            if (m.Success) { t.Kind = FieldKind.Enum; t.EnumName = m.Groups[1].Value; return t; }

            m = Regex.Match(s, @"^list\s*<\s*(\w+)\s*>$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string inner = m.Groups[1].Value.ToLowerInvariant();
                t.Kind = inner == "int" ? FieldKind.ListInt : FieldKind.ListString;
                return t;
            }

            switch (s.ToLowerInvariant())
            {
                case "int":            t.Kind = FieldKind.Int; break;
                case "nullableint":    t.Kind = FieldKind.NullableInt; break;
                case "double":
                case "float":          t.Kind = FieldKind.Double; break;

                // 빈 칸을 0 과 구분해야 하는 소수. 「안 적음」이 기본값을 뜻하는 열에 쓴다.
                case "nullabledouble":
                case "nullablefloat":  t.Kind = FieldKind.NullableDouble; break;
                case "bool":           t.Kind = FieldKind.Bool; break;
                case "string":
                case "nullablestring": t.Kind = FieldKind.String; break;
                case "datetime":       t.Kind = FieldKind.DateTime; break;
            }
            return t;
        }

        public string CsType => Kind switch
        {
            FieldKind.Int         => "int",
            FieldKind.NullableInt => "int?",
            FieldKind.Double      => "double",
            FieldKind.NullableDouble => "double?",
            FieldKind.Bool        => "bool",
            FieldKind.String      => "string",
            FieldKind.DateTime    => "System.DateTime",
            FieldKind.ListInt     => "int[]",
            FieldKind.ListString  => "string[]",
            FieldKind.Enum        => EnumName,
            _                     => "string"
        };
    }

    public sealed class Column
    {
        public int Index;
        public string Name;          // 3행
        public FieldType Type;       // 2행
        public bool IsComment;       // '#' 로 시작하는 열은 버린다
    }

    public sealed class Table
    {
        public string Name;
        public List<Column> Columns = new List<Column>();
        public List<string[]> Rows = new List<string[]>();   // 데이터 행(4행부터), 열 인덱스는 원본 그대로
        public List<int> ExcelRowNumbers = new List<int>();
    }

    /// <summary>
    /// [상추] 같은 토큰을 정수 ID 로 바꾼다.
    ///
    /// 이 매핑은 세이브 데이터와 묶이는 직렬화 신원이므로 절대 재배치하면 안 된다.
    /// 한번 부여한 번호는 고정하고, 새 토큰만 뒤에 붙인다. 그래서 파일로 남겨 커밋한다.
    /// (EnumValue 를 노출 순서로 재활용하면 안 되는 것과 같은 이유)
    /// </summary>
    public sealed class IdRegistry
    {
        private readonly Dictionary<string, int> _map = new Dictionary<string, int>(StringComparer.Ordinal);
        private int _next = 1;
        public bool Changed { get; private set; }

        public IReadOnlyDictionary<string, int> Map => _map;

        public static bool IsToken(string s) =>
            !string.IsNullOrEmpty(s) && s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']';

        public int Resolve(string token)
        {
            if (_map.TryGetValue(token, out int id)) return id;
            id = _next++;
            _map[token] = id;
            Changed = true;
            return id;
        }

        public bool TryGet(string token, out int id) => _map.TryGetValue(token, out id);

        public static IdRegistry Load(string path)
        {
            var reg = new IdRegistry();
            if (!System.IO.File.Exists(path)) return reg;

            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                var m = Regex.Match(line, @"""(?<k>(?:[^""\\]|\\.)*)""\s*:\s*(?<v>\d+)");
                if (!m.Success) continue;
                string key = Unescape(m.Groups["k"].Value);
                if (key == "$next") { reg._next = int.Parse(m.Groups["v"].Value); continue; }
                int v = int.Parse(m.Groups["v"].Value);
                reg._map[key] = v;
                if (v >= reg._next) reg._next = v + 1;
            }
            return reg;
        }

        public void Save(string path)
        {
            var keys = new List<string>(_map.Keys);
            keys.Sort((a, b) => _map[a].CompareTo(_map[b]));   // 번호 순 = 추가된 순

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"_comment\": \"자동 생성. 번호는 세이브 데이터와 묶이므로 재배치·삭제 금지.\",");
            sb.AppendLine("  \"$next\": " + _next + ",");
            for (int i = 0; i < keys.Count; i++)
                sb.AppendLine("  \"" + Escape(keys[i]) + "\": " + _map[keys[i]] + (i < keys.Count - 1 ? "," : ""));
            sb.AppendLine("}");

            string dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(false));
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string Unescape(string s) => s.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }

    public static class Values
    {
        /// <summary>"2026년 7월 29일 06:00:00" 같은 한글 표기와 일반 형식을 모두 받는다.</summary>
        public static bool TryParseDate(string s, out DateTime dt)
        {
            dt = default;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var m = Regex.Match(s, @"^\s*(\d{4})\s*년\s*(\d{1,2})\s*월\s*(\d{1,2})\s*일\s*(?:(\d{1,2}):(\d{2})(?::(\d{2}))?)?\s*$");
            if (m.Success)
            {
                int y = int.Parse(m.Groups[1].Value), mo = int.Parse(m.Groups[2].Value), d = int.Parse(m.Groups[3].Value);
                int hh = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0;
                int mi = m.Groups[5].Success ? int.Parse(m.Groups[5].Value) : 0;
                int ss = m.Groups[6].Success ? int.Parse(m.Groups[6].Value) : 0;
                try { dt = new DateTime(y, mo, d, hh, mi, ss); return true; } catch { return false; }
            }

            // 엑셀이 날짜 시리얼로 저장한 경우
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double serial) && serial > 1)
            {
                try { dt = DateTime.FromOADate(serial); return true; } catch { return false; }
            }

            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)
                || DateTime.TryParse(s, new CultureInfo("ko-KR"), DateTimeStyles.None, out dt);
        }

        public static bool TryParseBool(string s, out bool b)
        {
            b = false;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            if (s == "1") { b = true;  return true; }
            if (s == "0") { b = false; return true; }
            return bool.TryParse(s, out b);
        }

        public static string[] SplitList(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            var parts = s.Split(',');
            var outp = new List<string>(parts.Length);
            foreach (var p in parts)
            {
                string t = p.Trim();
                if (t.Length > 0) outp.Add(t);
            }
            return outp.ToArray();
        }
    }
}
