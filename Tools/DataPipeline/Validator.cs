using System;
using System.Collections.Generic;
using System.IO;

namespace SnailPet.Pipeline
{
    public sealed class Issue
    {
        public bool IsError;
        public string Where;
        public string Message;
        public string Group;      // 같은 원인끼리 묶는 키. 없으면 Message 로 묶는다.
    }

    public sealed class Report
    {
        public readonly List<Issue> Issues = new List<Issue>();
        public int ErrorCount, WarnCount;

        public void Error(string where, string msg, string group = null)
        { Issues.Add(new Issue { IsError = true,  Where = where, Message = msg, Group = group }); ErrorCount++; }

        public void Warn(string where, string msg, string group = null)
        { Issues.Add(new Issue { IsError = false, Where = where, Message = msg, Group = group }); WarnCount++; }

        /// <summary>
        /// 한 원인이 수십 행에서 터지면 로그가 도배된다. 같은 원인은 한 줄로 접고
        /// 발생 위치 몇 개만 예시로 보여준다. 전부 stdout 으로 내보내 순서가 섞이지 않게 한다.
        /// </summary>
        public void Print()
        {
            PrintGroup(true,  "오류");
            PrintGroup(false, "경고");

            Console.WriteLine();
            Console.WriteLine($"검증: 오류 {ErrorCount} · 경고 {WarnCount}");
        }

        private void PrintGroup(bool errors, string label)
        {
            var order = new List<string>();
            var buckets = new Dictionary<string, List<Issue>>(StringComparer.Ordinal);

            foreach (var i in Issues)
            {
                if (i.IsError != errors) continue;
                string key = i.Group ?? i.Message;
                if (!buckets.TryGetValue(key, out var list))
                {
                    buckets[key] = list = new List<Issue>();
                    order.Add(key);
                }
                list.Add(i);
            }
            if (order.Count == 0) return;

            Console.WriteLine();
            foreach (var key in order)
            {
                var list = buckets[key];
                var head = list[0];

                if (list.Count == 1)
                {
                    Console.WriteLine($"  [{label}] {head.Where}: {head.Message}");
                    continue;
                }

                Console.WriteLine($"  [{label}] {head.Message}  ({list.Count}건)");
                int show = Math.Min(3, list.Count);
                var sb = new System.Text.StringBuilder("         ");
                for (int i = 0; i < show; i++) sb.Append((i > 0 ? ", " : "") + list[i].Where);
                if (list.Count > show) sb.Append($" 외 {list.Count - show}건");
                Console.WriteLine(sb.ToString());
            }
        }
    }

    /// <summary>
    /// 데이터가 코드로 바뀌기 전에 걸러야 할 것들.
    /// 프리뷰 툴이 브라우저에서 하던 검사를 빌드 게이트로 옮긴 것이다.
    /// </summary>
    public static class Validator
    {
        public static Report Run(List<Table> tables, List<EnumDef> enums, IdRegistry ids, string resourceDir, string artRoot)
        {
            var rep = new Report();

            var enumByName = new Dictionary<string, EnumDef>(StringComparer.Ordinal);
            foreach (var e in enums) enumByName[e.Name] = e;

            CheckEnums(rep, enums);
            CheckColumns(rep, tables, enumByName);
            CheckValues(rep, tables, enumByName, ids);
            CheckDuplicateIds(rep, tables);
            CheckCrossReferences(rep, tables, ids);
            if (Directory.Exists(resourceDir)) CheckResources(rep, tables, resourceDir, artRoot);

            return rep;
        }

        private static void CheckEnums(Report rep, List<EnumDef> enums)
        {
            foreach (var e in enums)
            {
                var seenName = new HashSet<string>(StringComparer.Ordinal);
                var seenVal = new Dictionary<int, string>();
                foreach (var (name, value) in e.Members)
                {
                    if (!seenName.Add(name))
                        rep.Error("EnumData", $"{e.Name}.{name} 이 중복 정의되었습니다.");
                    if (seenVal.TryGetValue(value, out string other))
                        rep.Error("EnumData", $"{e.Name} 의 값 {value} 를 {other} 와 {name} 이 공유합니다. EnumValue 는 직렬화 신원이라 겹치면 안 됩니다.");
                    else seenVal[value] = name;
                }
            }
        }

        private static void CheckColumns(Report rep, List<Table> tables, Dictionary<string, EnumDef> enums)
        {
            foreach (var t in tables)
                foreach (var c in t.Columns)
                {
                    if (c.Type.Kind == FieldKind.Unknown)
                        rep.Error($"{t.Name}.{c.Name}", $"타입 <{c.Type.Raw}> 을 해석할 수 없습니다.");
                    else if (c.Type.Kind == FieldKind.Enum && !enums.ContainsKey(c.Type.EnumName))
                        rep.Error($"{t.Name}.{c.Name}", $"enum <{c.Type.EnumName}> 이 EnumData 에 정의돼 있지 않습니다.");
                }
        }

        private static void CheckValues(Report rep, List<Table> tables, Dictionary<string, EnumDef> enums, IdRegistry ids)
        {
            foreach (var t in tables)
                for (int r = 0; r < t.Rows.Count; r++)
                {
                    var row = t.Rows[r];
                    string where = $"{t.Name} {t.ExcelRowNumbers[r]}행";

                    foreach (var c in t.Columns)
                    {
                        string v = Program.Get(row, c.Index);
                        if (string.IsNullOrEmpty(v)) continue;

                        switch (c.Type.Kind)
                        {
                            case FieldKind.Int:
                            case FieldKind.NullableInt:
                                if (!IdRegistry.IsToken(v) && !int.TryParse(v, out _))
                                    rep.Error(where,
                                        $"{t.Name}.{c.Name} 이 정수형인데 [토큰] 도 정수도 아닌 값이 들어 있습니다 (예: <{v}>). " +
                                        "리소스 키 같은 문자열이면 타입을 string 으로 바꾸세요.",
                                        $"int:{t.Name}.{c.Name}");
                                break;

                            case FieldKind.Double:
                                if (!double.TryParse(v, System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out _))
                                    rep.Error(where, $"{c.Name} 의 값 <{v}> 을 숫자로 읽을 수 없습니다.");
                                break;

                            case FieldKind.Bool:
                                if (!Values.TryParseBool(v, out _))
                                    rep.Error(where, $"{c.Name} 의 값 <{v}> 을 참/거짓으로 읽을 수 없습니다 (0 또는 1).");
                                break;

                            case FieldKind.DateTime:
                                if (!Values.TryParseDate(v, out _))
                                    rep.Error(where, $"{c.Name} 의 값 <{v}> 을 날짜로 읽을 수 없습니다.");
                                break;

                            case FieldKind.Enum:
                                if (enums.TryGetValue(c.Type.EnumName, out var def)
                                    && !def.Members.Exists(m => string.Equals(m.name, v, StringComparison.Ordinal)))
                                    rep.Error(where, $"{c.Name} 의 값 <{v}> 이 enum {c.Type.EnumName} 에 없습니다.");
                                break;

                            case FieldKind.ListInt:
                                foreach (var item in Values.SplitList(v))
                                    if (!IdRegistry.IsToken(item) && !int.TryParse(item, out _))
                                        rep.Error(where,
                                            $"{t.Name}.{c.Name} 이 list<int> 인데 [토큰] 도 정수도 아닌 값이 들어 있습니다 (예: <{item}>). " +
                                            "문자열 목록이면 타입을 list<string> 으로 바꾸세요.",
                                            $"listint:{t.Name}.{c.Name}");
                                break;
                        }
                    }
                }
        }

        private static void CheckDuplicateIds(Report rep, List<Table> tables)
        {
            foreach (var t in tables)
            {
                var idCol = t.Columns.Find(c => c.Name == "Id");
                if (idCol == null) continue;

                // 같은 Id 가 여러 행에 걸치는 게 정상인 시트(뽑기 테이블 등)는 건너뛴다
                bool idIsKey = !t.Name.Equals("GachaData", StringComparison.Ordinal);
                if (!idIsKey) continue;

                var seen = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int r = 0; r < t.Rows.Count; r++)
                {
                    string v = Program.Get(t.Rows[r], idCol.Index);
                    if (string.IsNullOrEmpty(v)) { rep.Error($"{t.Name} {t.ExcelRowNumbers[r]}행", "Id 가 비어 있습니다."); continue; }
                    if (seen.TryGetValue(v, out int prev))
                        rep.Error($"{t.Name} {t.ExcelRowNumbers[r]}행",
                                  $"Id <{v}> 가 {prev}행과 중복입니다. Id 로 딕셔너리를 만들면 한 행이 유실됩니다.");
                    else seen[v] = t.ExcelRowNumbers[r];
                }
            }
        }

        /// <summary>상점·뽑기·이벤트가 실제로 존재하는 아이템을 가리키는지 본다.</summary>
        private static void CheckCrossReferences(Report rep, List<Table> tables, IdRegistry ids)
        {
            var defined = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in new[] { "FoodData", "EggData", "ItemData", "AccessoriesData", "BuffData", "PartsData" })
            {
                var t = tables.Find(x => x.Name == name);
                var idCol = t?.Columns.Find(c => c.Name == "Id");
                if (idCol == null) continue;
                foreach (var row in t.Rows)
                {
                    string v = Program.Get(row, idCol.Index);
                    if (!string.IsNullOrEmpty(v)) defined.Add(v);
                }
            }

            // PartsGroupId 는 별도 네임스페이스
            var groups = new HashSet<string>(StringComparer.Ordinal);
            var parts = tables.Find(x => x.Name == "PartsData");
            var gCol = parts?.Columns.Find(c => c.Name == "PartsGroupId");
            if (gCol != null)
                foreach (var row in parts.Rows)
                {
                    string v = Program.Get(row, gCol.Index);
                    if (!string.IsNullOrEmpty(v)) groups.Add(v);
                }

            void CheckRef(string table, string column, HashSet<string> pool, string poolDesc)
            {
                var t = tables.Find(x => x.Name == table);
                var c = t?.Columns.Find(x => x.Name == column);
                if (c == null) return;

                for (int r = 0; r < t.Rows.Count; r++)
                {
                    string v = Program.Get(t.Rows[r], c.Index);
                    if (string.IsNullOrEmpty(v)) continue;
                    string where = $"{table} {t.ExcelRowNumbers[r]}행";

                    foreach (var item in (c.Type.Kind == FieldKind.ListInt || c.Type.Kind == FieldKind.ListString)
                                         ? Values.SplitList(v) : new[] { v })
                    {
                        if (!IdRegistry.IsToken(item)) continue;
                        if (!pool.Contains(item))
                            rep.Error(where,
                                $"{table}.{column} 이 참조하는 <{item}> 의 {poolDesc} 를 찾을 수 없습니다. 삭제된 항목을 가리키고 있습니다.",
                                $"ref:{table}.{column}");
                    }
                }
            }

            const string ItemDef = "정의(FoodData / EggData / ItemData / AccessoriesData 등)";
            CheckRef("ShopData",  "Id",             defined, ItemDef);
            CheckRef("ShopData",  "CostItem",       defined, ItemDef);
            CheckRef("ShopData",  "SellItem",       defined, ItemDef);
            CheckRef("GachaData", "Id2",            defined, ItemDef);
            CheckRef("EventData", "ShopItemIds",    defined, ItemDef);
            CheckRef("FoodData",  "BuffId",         defined, ItemDef);
            CheckRef("EggData",   "PartsGroupIds",  groups,  "PartsData.PartsGroupId 정의");
        }

        /// <summary>ResourceKey 가 가리키는 파일이 실제로 있는지.</summary>
        private static void CheckResources(Report rep, List<Table> tables, string resourceDir, string artRoot)
        {
            bool Exists(string sub) => File.Exists(Path.Combine(resourceDir, sub.Replace('/', Path.DirectorySeparatorChar)));

            // 파츠: {artRoot}/{PartsType}/{ResourceKey}.png, 색상은 .../Color/{key}.png
            var parts = tables.Find(x => x.Name == "PartsData");
            if (parts != null)
            {
                var cType = parts.Columns.Find(c => c.Name == "PartsType");
                var cRes  = parts.Columns.Find(c => c.Name == "ResourceKey");
                var cCol  = parts.Columns.Find(c => c.Name == "Colors");
                for (int r = 0; r < parts.Rows.Count; r++)
                {
                    var row = parts.Rows[r];
                    string where = $"PartsData {parts.ExcelRowNumbers[r]}행";
                    string type = Program.Get(row, cType?.Index ?? -1);
                    string key  = Program.Get(row, cRes?.Index ?? -1);
                    if (type.Length > 0 && key.Length > 0 && !Exists($"{type}/{key}.png"))
                        rep.Error(where, $"선화 리소스 {artRoot}/{type}/{key}.png 가 없습니다.");

                    foreach (var ck in Values.SplitList(Program.Get(row, cCol?.Index ?? -1)))
                        if (type.Length > 0 && !Exists($"{type}/Color/{ck}.png"))
                            rep.Error(where, $"색상 리소스 {artRoot}/{type}/Color/{ck}.png 가 없습니다.");
                }
            }

            var colors = tables.Find(x => x.Name == "PartsColorData");
            if (colors != null)
            {
                var cType = colors.Columns.Find(c => c.Name == "PartsType");
                var cKey  = colors.Columns.Find(c => c.Name == "ColorResourceKey");
                for (int r = 0; r < colors.Rows.Count; r++)
                {
                    string type = Program.Get(colors.Rows[r], cType?.Index ?? -1);
                    string key  = Program.Get(colors.Rows[r], cKey?.Index ?? -1);
                    if (type.Length > 0 && key.Length > 0 && !Exists($"{type}/Color/{key}.png"))
                        rep.Warn($"PartsColorData {colors.ExcelRowNumbers[r]}행",
                                 $"{artRoot}/{type}/Color/{key}.png 가 없습니다. 아직 아트가 안 나온 색이면 무시해도 됩니다.");
                }
            }

            var eggs = tables.Find(x => x.Name == "EggData");
            if (eggs != null)
            {
                var cKey = eggs.Columns.Find(c => c.Name == "ResourceKey");
                for (int r = 0; r < eggs.Rows.Count; r++)
                {
                    string key = Program.Get(eggs.Rows[r], cKey?.Index ?? -1);
                    if (key.Length > 0 && !Exists($"Egg/{key}.png"))
                        rep.Error($"EggData {eggs.ExcelRowNumbers[r]}행", $"{artRoot}/Egg/{key}.png 가 없습니다.");
                }
            }
        }
    }
}
