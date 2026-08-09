using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SnailPet.Pipeline
{
    public sealed class EnumDef
    {
        public string Name;
        public List<(string name, int value)> Members = new List<(string, int)>();
    }

    /// <summary>
    /// SnailData.xlsx → 생성 코드 + 이식 가능한 JSON.
    ///
    /// 규약: 1행=대상, 2행=타입, 3행=변수명, 4행부터 데이터.
    /// 데이터에 오류가 있으면 아무것도 생성하지 않고 0 이 아닌 코드로 종료한다(빌드 게이트).
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // --root <경로> : 기본은 실행 파일 위에서 SnailData.xlsx 를 찾아 올라간다.
            //                 CI 나 테스트에서 다른 트리를 가리킬 때 쓴다.
            string root = null;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--root") { root = Path.GetFullPath(args[i + 1]); break; }
            root ??= FindRoot();

            if (root == null) { Console.WriteLine("SnailData.xlsx 가 있는 프로젝트 루트를 찾지 못했습니다."); return 1; }
            if (!File.Exists(Path.Combine(root, "SnailData.xlsx")))
            { Console.WriteLine("SnailData.xlsx 가 없습니다: " + root); return 1; }

            string xlsx      = Path.Combine(root, "SnailData.xlsx");
            string idMapPath = Path.Combine(root, "Tools", "DataPipeline", "IdMap.json");
            string csOut     = Path.Combine(root, "SnailPet", "Assets", "Scripts", "Generated", "GameData.g.cs");
            string jsonOut   = Path.Combine(root, "Data", "gamedata.json");
            // 아트는 Unity 프로젝트 안에 있다 (단일 소스). 옮기면 여기와 ExportSnailData.ps1 만 고치면 된다.
            const string artRoot = "SnailPet/Assets/Resources/Snail";
            string resDir    = Path.Combine(root, artRoot.Replace('/', Path.DirectorySeparatorChar));
            bool check       = Array.IndexOf(args, "--check") >= 0;   // 생성 없이 검증만

            Console.WriteLine("입력: " + xlsx);

            var sheets = Xlsx.Read(xlsx);
            var enums  = BuildEnums(sheets);
            var tables = BuildTables(sheets);
            var ids    = IdRegistry.Load(idMapPath);

            // 토큰을 먼저 전부 등록해야 상호 참조 검증이 가능하다
            RegisterTokens(tables, ids);

            var report = Validator.Run(tables, enums, ids, resDir, artRoot);
            report.Print();

            if (report.ErrorCount > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"오류 {report.ErrorCount}건이므로 생성을 중단합니다. (파일은 하나도 쓰지 않았습니다)");
                return 2;
            }

            if (check)
            {
                Console.WriteLine();
                Console.WriteLine("--check 모드: 검증만 수행했고 파일은 쓰지 않았습니다.");
                return 0;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(csOut));
            Directory.CreateDirectory(Path.GetDirectoryName(jsonOut));

            // .cs 는 BOM 을 붙인다. 게임 텍스트가 한글 문자열 리터럴로 들어가는데,
            // BOM 이 없으면 도구에 따라 ANSI 로 잘못 읽어 문자열이 깨질 수 있다.
            // JSON 은 반대로 BOM 이 있으면 파서가 거부하는 경우가 있어 붙이지 않는다.
            File.WriteAllText(csOut,   CodeGen.EmitCSharp(tables, enums, ids), new UTF8Encoding(true));
            File.WriteAllText(jsonOut, CodeGen.EmitJson(tables, enums, ids),   new UTF8Encoding(false));

            if (ids.Changed) ids.Save(idMapPath);

            Console.WriteLine();
            Console.WriteLine("생성 완료");
            Console.WriteLine("  " + Rel(root, csOut));
            Console.WriteLine("  " + Rel(root, jsonOut));
            if (ids.Changed) Console.WriteLine("  " + Rel(root, idMapPath) + "  (새 ID 추가됨)");
            Console.WriteLine($"  테이블 {tables.Count}개 · enum {enums.Count}개 · ID {ids.Map.Count}개");
            return 0;
        }

        private static string Rel(string root, string p) =>
            p.StartsWith(root) ? p.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/') : p;

        private static string FindRoot()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "SnailData.xlsx"))) return d.FullName;
                d = d.Parent;
            }
            return null;
        }

        /// <summary>EnumData 시트에서 enum 정의를 뽑는다.</summary>
        private static List<EnumDef> BuildEnums(List<Sheet> sheets)
        {
            var result = new List<EnumDef>();
            var sheet = sheets.Find(s => s.Name == "EnumData");
            if (sheet == null) return result;

            var byName = new Dictionary<string, EnumDef>(StringComparer.Ordinal);

            // 헤더에서 열 위치를 찾는다 (열 순서가 바뀌어도 견디도록)
            var header = sheet.Row(3);
            int cType = IndexOf(header, "EnumType"), cName = IndexOf(header, "EnumName"), cVal = IndexOf(header, "EnumValue");
            if (cType < 0 || cName < 0 || cVal < 0) return result;

            for (int r = 4; r <= sheet.RowCount; r++)
            {
                var row = sheet.Row(r);
                string t = Get(row, cType), n = Get(row, cName), v = Get(row, cVal);
                if (string.IsNullOrEmpty(t) || string.IsNullOrEmpty(n)) continue;
                if (!int.TryParse(v, out int value)) continue;

                if (!byName.TryGetValue(t, out var def))
                {
                    def = new EnumDef { Name = t };
                    byName[t] = def;
                    result.Add(def);
                }
                def.Members.Add((n, value));
            }
            return result;
        }

        private static List<Table> BuildTables(List<Sheet> sheets)
        {
            var tables = new List<Table>();
            foreach (var s in sheets)
            {
                if (s.RowCount < 3) continue;
                var typeRow = s.Row(2);
                var nameRow = s.Row(3);

                var table = new Table { Name = s.Name };
                for (int c = 0; c < s.ColumnCount; c++)
                {
                    string name = Get(nameRow, c);
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name.StartsWith("#")) continue;             // 주석 열

                    // 같은 이름이 또 나오면 뒤에 숫자를 붙여 충돌을 막는다
                    string unique = name;
                    int dup = 1;
                    while (table.Columns.Exists(x => x.Name == unique)) unique = name + (++dup);

                    table.Columns.Add(new Column
                    {
                        Index = c,
                        Name = unique,
                        Type = FieldType.Parse(Get(typeRow, c))
                    });
                }
                if (table.Columns.Count == 0) continue;

                for (int r = 4; r <= s.RowCount; r++)
                {
                    var row = s.Row(r);
                    bool any = false;
                    foreach (var col in table.Columns)
                        if (!string.IsNullOrEmpty(Get(row, col.Index))) { any = true; break; }
                    if (!any) continue;

                    table.Rows.Add(row);
                    table.ExcelRowNumbers.Add(r);
                }
                tables.Add(table);
            }
            return tables;
        }

        /// <summary>모든 시트를 훑어 [토큰] 을 ID 로 등록한다.</summary>
        private static void RegisterTokens(List<Table> tables, IdRegistry ids)
        {
            foreach (var t in tables)
                foreach (var row in t.Rows)
                    foreach (var col in t.Columns)
                    {
                        string v = Get(row, col.Index);
                        if (string.IsNullOrEmpty(v)) continue;

                        if (col.Type.Kind == FieldKind.ListInt || col.Type.Kind == FieldKind.ListString)
                        {
                            foreach (var item in Values.SplitList(v))
                                if (IdRegistry.IsToken(item)) ids.Resolve(item);
                        }
                        else if (IdRegistry.IsToken(v)) ids.Resolve(v);
                    }
        }

        public static string Get(string[] row, int i) =>
            (row != null && i >= 0 && i < row.Length && row[i] != null) ? row[i] : "";

        private static int IndexOf(string[] row, string name)
        {
            for (int i = 0; i < row.Length; i++)
                if (string.Equals(row[i], name, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
