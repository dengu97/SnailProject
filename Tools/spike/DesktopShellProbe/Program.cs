using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DesktopShellProbe
{
    /// <summary>
    /// 데스크톱 상주 펫이 성립하는지 확인하는 스파이크.
    /// 검증 항목:
    ///   1) 안티에일리어싱된 알파가 살아있는 투명 창 (색상 키 방식이 아닌 per-pixel alpha)
    ///   2) 항상 위 + 작업표시줄/Alt-Tab 에 안 뜸
    ///   3) 클릭이 뒤 창으로 통과
    ///   4) 다른 창의 위쪽 테두리를 걸을 수 있는 표면으로 수집
    ///   5) 그 표면 위를 실제로 걸어가기
    /// </summary>
    internal static class Program
    {
        [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);

        private static readonly StringBuilder Log = new StringBuilder();
        private static void Say(string s) { Log.AppendLine(s); Console.WriteLine(s); }

        [STAThread]
        private static int Main(string[] args)
        {
            AttachConsole(-1); // 부모 PowerShell 콘솔에 붙어서 결과를 보이게 한다

            string root = FindProjectRoot();

            // --export <경로> [크기] : 합성 결과를 PNG 로만 저장하고 종료 (Unity 로 넘기는 용도)
            if (args.Length >= 2 && args[0] == "--export")
            {
                int px = (args.Length >= 3 && int.TryParse(args[2], out int q)) ? q : 512;
                try
                {
                    using (var bmp = BuildSnail(root, px))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
                        bmp.Save(args[1], ImageFormat.Png);
                        Console.WriteLine($"저장: {args[1]} ({px}x{px}, 반투명 픽셀 {CountPartialAlpha(bmp)}개)");
                    }
                    return 0;
                }
                catch (Exception ex) { Console.WriteLine("실패: " + ex.Message); return 1; }
            }

            int seconds = 10;
            if (args.Length > 0 && int.TryParse(args[0], out int s)) seconds = Math.Max(2, Math.Min(60, s));

            Say("=== 데스크톱 셸 스파이크 ===");
            Say($"프로젝트 루트: {(root ?? "(못 찾음)")}");
            Say($"OS: {Environment.OSVersion.VersionString}   .NET: {Environment.Version}");
            Say("");

            Bitmap snail;
            try
            {
                snail = BuildSnail(root, 220);
                Say($"[1] 파츠 합성 ....... OK  ({snail.Width}x{snail.Height}, 반투명 픽셀 {CountPartialAlpha(snail)}개)");
            }
            catch (Exception ex)
            {
                Say($"[1] 파츠 합성 ....... 실패: {ex.Message}");
                Flush(root);
                return 1;
            }

            int passed = 1, total = 5;
            using (var form = new PetForm(snail))
            {
                form.Show();
                Application.DoEvents();

                IntPtr h = form.Handle;
                int ex2 = Win32.GetWindowLong(h, Win32.GWL_EXSTYLE);
                bool layered     = (ex2 & Win32.WS_EX_LAYERED)     != 0;
                bool transparent = (ex2 & Win32.WS_EX_TRANSPARENT) != 0;
                bool toolwindow  = (ex2 & Win32.WS_EX_TOOLWINDOW)  != 0;

                Say($"[2] 투명 창 스타일 .. {(layered ? "OK" : "실패")}  (WS_EX_LAYERED)");
                if (layered) passed++;

                Say($"[3] 항상 위/숨김 .... {(toolwindow ? "OK" : "실패")}  (WS_EX_TOOLWINDOW, TopMost={form.TopMost})");
                if (toolwindow && form.TopMost) passed++;

                // 클릭 통과 검증: 펫 한가운데 좌표에서 창을 찾았을 때 우리 창이 아니어야 한다
                var center = new Win32.POINT(form.Left + form.Width / 2, form.Top + form.Height / 2);
                IntPtr hit = Win32.WindowFromPoint(center);
                bool clickThrough = transparent && hit != h;
                Say($"[4] 클릭 통과 ....... {(clickThrough ? "OK" : "실패")}  " +
                    $"(WS_EX_TRANSPARENT={transparent}, 중심점 히트테스트 → {(hit == h ? "펫 자신 (통과 안 됨)" : "뒤 창")})");
                if (clickThrough) passed++;

                var surfaces = WindowSurfaces.Collect(h);
                Say($"[5] 창 표면 수집 .... {(surfaces.Count > 0 ? "OK" : "실패")}  ({surfaces.Count}개 발견)");
                if (surfaces.Count > 0) passed++;
                foreach (var sf in surfaces.GetRange(0, Math.Min(8, surfaces.Count)))
                    Say($"      {sf}");
                Say("");

                // 가장 넓은 표면 위를 걸어간다
                Surface walk = null;
                foreach (var sf in surfaces) if (walk == null || sf.Width > walk.Width) walk = sf;

                if (walk != null)
                {
                    Say($"→ 가장 넓은 표면 위를 {seconds}초간 걸어갑니다: {walk.Title}");
                    form.WalkAlong(walk, seconds);
                }
                else
                {
                    Say($"→ 걸을 표면이 없어 화면 하단을 {seconds}초간 걸어갑니다.");
                    var scr = Screen.PrimaryScreen.WorkingArea;
                    form.WalkAlong(new Surface { Left = scr.Left, Right = scr.Right, Top = scr.Bottom, Title = "(작업 영역 하단)" }, seconds);
                }

                Say("");
                Say($"=== 결과: {passed}/{total} 통과 ===");
            }

            Flush(root);
            return passed == total ? 0 : 2;
        }

        private static void Flush(string root)
        {
            try
            {
                string p = Path.Combine(root ?? AppContext.BaseDirectory, "Tools", "spike", "probe-result.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(p));
                File.WriteAllText(p, Log.ToString(), new UTF8Encoding(false));
                Console.WriteLine($"\n리포트 저장: {p}");
            }
            catch { /* 리포트 저장 실패는 스파이크 결과에 영향 없음 */ }
        }

        /// <summary>Resource 폴더가 보일 때까지 상위로 올라간다.</summary>
        private static string FindProjectRoot()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (Directory.Exists(Path.Combine(d.FullName, "Resource"))) return d.FullName;
                d = d.Parent;
            }
            return null;
        }

        /// <summary>
        /// SortOrder(뒤→앞) 대로 색상 레이어 + 선화 레이어를 쌓는다.
        /// 현재 EnumData 기준: Shell 100, Body 200, Feeler 300, Eyes 400.
        /// </summary>
        private static Bitmap BuildSnail(string root, int size)
        {
            if (root == null) throw new DirectoryNotFoundException("Resource 폴더를 찾지 못했습니다.");

            var layers = new (string type, string line, string color)[]
            {
                ("Shell",  "commonshell01",  "commonshell01_c01"),
                ("Body",   "commonbody01",   "commonbody01_c01"),
                ("Feeler", "commonfeeler01", "commonfeeler01_c01"),
                ("Eyes",   "commoneyes01",   null),
            };

            var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(canvas))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);
                foreach (var (type, line, color) in layers)
                {
                    if (color != null)
                        Draw(g, Path.Combine(root, "Resource", type, "Color", color + ".png"), size);
                    Draw(g, Path.Combine(root, "Resource", type, line + ".png"), size);
                }
            }
            return canvas;
        }

        private static void Draw(Graphics g, string path, int size)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"리소스 없음: {path}");
            using (var img = Image.FromFile(path))
                g.DrawImage(img, new Rectangle(0, 0, size, size));
        }

        /// <summary>알파가 0 도 255 도 아닌 픽셀 수 — per-pixel alpha 가 실제로 살아있는지의 증거.</summary>
        private static int CountPartialAlpha(Bitmap bmp)
        {
            int n = 0;
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* p = (byte*)data.Scan0;
                    for (int y = 0; y < bmp.Height; y++)
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            byte a = p[y * data.Stride + x * 4 + 3];
                            if (a != 0 && a != 255) n++;
                        }
                }
            }
            finally { bmp.UnlockBits(data); }
            return n;
        }
    }
}
