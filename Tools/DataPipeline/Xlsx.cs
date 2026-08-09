using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace SnailPet.Pipeline
{
    /// <summary>한 시트의 셀 격자. 값이 없는 칸은 null.</summary>
    public sealed class Sheet
    {
        public string Name;
        public List<string[]> Rows = new List<string[]>();   // 0-based. 엑셀 1행 = Rows[0]
        public int ColumnCount;

        /// <summary>엑셀 행 번호(1-based)로 접근. 없으면 빈 배열.</summary>
        public string[] Row(int excelRow)
        {
            int i = excelRow - 1;
            return (i >= 0 && i < Rows.Count) ? Rows[i] : new string[ColumnCount];
        }

        public int RowCount => Rows.Count;
    }

    /// <summary>
    /// 외부 의존성 없이 xlsx 를 읽는다. OOXML 은 zip + XML 이므로
    /// System.IO.Compression 과 XmlReader 만으로 충분하다.
    /// Excel 이 파일을 잠그고 있어도 되도록 복사본을 읽는다.
    /// </summary>
    public static class Xlsx
    {
        public static List<Sheet> Read(string path)
        {
            string work = Path.Combine(Path.GetTempPath(), "snail_" + Guid.NewGuid().ToString("N") + ".xlsx");
            File.Copy(path, work, true);
            try
            {
                using var zip = ZipFile.OpenRead(work);

                var shared = ReadSharedStrings(zip);
                var sheetRefs = ReadWorkbook(zip);

                var result = new List<Sheet>();
                foreach (var (name, target) in sheetRefs)
                {
                    var entry = zip.GetEntry(target);
                    if (entry == null) continue;
                    result.Add(ReadSheet(entry, name, shared));
                }
                return result;
            }
            finally
            {
                try { File.Delete(work); } catch { /* 임시파일 삭제 실패는 무시 */ }
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var list = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return list;

            using var s = entry.Open();
            using var r = XmlReader.Create(s, new XmlReaderSettings { IgnoreWhitespace = false });

            var sb = new StringBuilder();
            bool inSi = false, inT = false;
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Element)
                {
                    if (r.LocalName == "si") { inSi = true; sb.Clear(); }
                    else if (r.LocalName == "t" && inSi) inT = true;
                }
                else if (r.NodeType == XmlNodeType.Text && inT) sb.Append(r.Value);
                else if (r.NodeType == XmlNodeType.EndElement)
                {
                    if (r.LocalName == "t") inT = false;
                    else if (r.LocalName == "si") { list.Add(sb.ToString()); inSi = false; }
                }
            }
            return list;
        }

        /// <summary>시트 이름과 xl/ 내부 경로 쌍을 워크북 순서대로 돌려준다.</summary>
        private static List<(string name, string target)> ReadWorkbook(ZipArchive zip)
        {
            // r:id -> target
            var rels = new Dictionary<string, string>();
            var relEntry = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (relEntry != null)
            {
                using var s = relEntry.Open();
                using var r = XmlReader.Create(s);
                while (r.Read())
                    if (r.NodeType == XmlNodeType.Element && r.LocalName == "Relationship")
                    {
                        string id = r.GetAttribute("Id");
                        string tg = r.GetAttribute("Target");
                        if (id != null && tg != null) rels[id] = tg;
                    }
            }

            var sheets = new List<(string, string)>();
            var wbEntry = zip.GetEntry("xl/workbook.xml");
            if (wbEntry == null) return sheets;

            using (var s = wbEntry.Open())
            using (var r = XmlReader.Create(s))
            {
                while (r.Read())
                    if (r.NodeType == XmlNodeType.Element && r.LocalName == "sheet")
                    {
                        string name = r.GetAttribute("name");
                        string rid = r.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
                                     ?? r.GetAttribute("r:id");
                        if (name == null || rid == null || !rels.TryGetValue(rid, out string target)) continue;
                        if (!target.StartsWith("xl/")) target = "xl/" + target.TrimStart('/');
                        sheets.Add((name, target));
                    }
            }
            return sheets;
        }

        private static Sheet ReadSheet(ZipArchiveEntry entry, string name, List<string> shared)
        {
            var sheet = new Sheet { Name = name };
            var rows = new SortedDictionary<int, SortedDictionary<int, string>>();
            int maxCol = 0;

            using (var s = entry.Open())
            using (var r = XmlReader.Create(s))
            {
                int curRow = 0, curCol = 0;
                string cellType = null;
                bool inValue = false, inInline = false;
                var text = new StringBuilder();

                while (r.Read())
                {
                    switch (r.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (r.LocalName)
                            {
                                case "row":
                                    curRow = int.TryParse(r.GetAttribute("r"), out int rn) ? rn : curRow + 1;
                                    break;
                                case "c":
                                    curCol = ColumnIndex(r.GetAttribute("r"));
                                    cellType = r.GetAttribute("t");
                                    text.Clear();
                                    break;
                                case "v":
                                    inValue = true; break;
                                case "is":
                                    inInline = true; break;
                                case "t":
                                    if (inInline) inValue = true; break;
                            }
                            break;

                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                            if (inValue) text.Append(r.Value);
                            break;

                        case XmlNodeType.EndElement:
                            switch (r.LocalName)
                            {
                                case "v": inValue = false; break;
                                case "t": if (inInline) inValue = false; break;
                                case "is": inInline = false; break;
                                case "c":
                                {
                                    string raw = text.ToString();
                                    if (raw.Length > 0)
                                    {
                                        string val = cellType == "s" && int.TryParse(raw, out int si)
                                                     && si >= 0 && si < shared.Count
                                                   ? shared[si] : raw;
                                        val = val.Trim();
                                        if (val.Length > 0)
                                        {
                                            if (!rows.TryGetValue(curRow, out var cells))
                                                rows[curRow] = cells = new SortedDictionary<int, string>();
                                            cells[curCol] = val;
                                            if (curCol > maxCol) maxCol = curCol;
                                        }
                                    }
                                    break;
                                }
                            }
                            break;
                    }
                }
            }

            sheet.ColumnCount = maxCol + 1;
            int last = rows.Count > 0 ? Last(rows.Keys) : 0;
            for (int i = 1; i <= last; i++)
            {
                var arr = new string[sheet.ColumnCount];
                if (rows.TryGetValue(i, out var cells))
                    foreach (var kv in cells)
                        if (kv.Key < arr.Length) arr[kv.Key] = kv.Value;
                sheet.Rows.Add(arr);
            }
            return sheet;
        }

        private static int Last(SortedDictionary<int, SortedDictionary<int, string>>.KeyCollection keys)
        {
            int last = 0;
            foreach (int k in keys) last = k;
            return last;
        }

        /// <summary>"BC12" → 54 (0-based 열 인덱스)</summary>
        private static int ColumnIndex(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return 0;
            int n = 0;
            foreach (char ch in cellRef)
            {
                if (ch >= 'A' && ch <= 'Z') n = n * 26 + (ch - 'A' + 1);
                else if (ch >= 'a' && ch <= 'z') n = n * 26 + (ch - 'a' + 1);
                else break;
            }
            return n - 1;
        }
    }
}
