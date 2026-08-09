#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SnailPet.Desktop
{
    /// <summary>달팽이가 기어오를 수 있는 수평 표면 하나 (창의 위쪽 테두리). 좌표는 가상 화면 px.</summary>
    public sealed class Surface
    {
        public IntPtr Hwnd;
        public string Title;
        public int Left, Right, Top;
        public int Width { get { return Right - Left; } }

        public override string ToString()
        {
            return string.Format("y={0,5}  x={1,6}..{2,-6} (폭 {3,4})  {4}", Top, Left, Right, Width, Title);
        }
    }

    /// <summary>
    /// 열려 있는 창들의 위쪽 테두리를 걸을 수 있는 표면 목록으로 수집한다.
    /// Tools/spike 에서 실제 검증을 마친 필터를 그대로 옮겨왔다.
    /// </summary>
    public static class WindowSurfaces
    {
        public static List<Surface> Collect(IntPtr selfHwnd, int minWidth = 120)
        {
            var found = new List<Surface>();

            Win32.EnumWindows((hwnd, _) =>
            {
                if (hwnd == selfHwnd) return true;
                if (!Win32.IsWindowVisible(hwnd)) return true;
                if (Win32.IsIconic(hwnd)) return true;

                // UWP 앱은 안 보이는 상태로도 살아있다 (cloaked). 걸러내지 않으면 유령 표면이 생긴다.
                int cloaked;
                if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_CLOAKED, out cloaked, sizeof(int)) == 0
                    && cloaked != 0) return true;

                int len = Win32.GetWindowTextLengthW(hwnd);
                if (len <= 0) return true;                      // 제목 없는 창은 대부분 껍데기다
                var buf = new char[len + 1];
                int got = Win32.GetWindowTextW(hwnd, buf, buf.Length);
                string title = new string(buf, 0, Math.Max(0, got));
                if (string.IsNullOrEmpty(title.Trim())) return true;

                // 소유된 창(대화상자·툴팁·그림자 헬퍼)은 독립 창이 아니다
                if (Win32.GetWindow(hwnd, Win32.GW_OWNER) != IntPtr.Zero) return true;

                int ex = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
                // 클릭 통과 오버레이(NVIDIA 오버레이 등)와 도구 창은 표면이 아니다.
                // 펫 자신도 이 스타일을 쓰므로 다른 펫 인스턴스가 자동으로 걸러진다.
                if ((ex & Win32.WS_EX_TRANSPARENT) != 0) return true;
                if ((ex & Win32.WS_EX_TOOLWINDOW)  != 0) return true;

                if (IsShellClass(hwnd)) return true;            // 바탕화면·작업표시줄

                // 그림자 여백이 없는 실제 보이는 경계를 쓴다
                Win32.RECT r;
                if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS,
                        out r, Marshal.SizeOf(typeof(Win32.RECT))) != 0)
                {
                    if (!Win32.GetWindowRect(hwnd, out r)) return true;
                }

                if (r.Width < minWidth || r.Height <= 0) return true;

                found.Add(new Surface { Hwnd = hwnd, Title = title, Left = r.Left, Right = r.Right, Top = r.Top });
                return true;
            }, IntPtr.Zero);

            found.Sort((a, b) => a.Top.CompareTo(b.Top));
            return found;
        }

        /// <summary>바탕화면(Progman/WorkerW)과 작업표시줄 계열은 걸어다닐 창이 아니다.</summary>
        private static bool IsShellClass(IntPtr hwnd)
        {
            var buf = new char[256];
            int n = Win32.GetClassNameW(hwnd, buf, buf.Length);
            if (n <= 0) return false;
            string cls = new string(buf, 0, n);
            switch (cls)
            {
                case "Progman":
                case "WorkerW":
                case "Shell_TrayWnd":
                case "Shell_SecondaryTrayWnd":
                case "Windows.UI.Core.CoreWindow":
                    return true;
                default:
                    return cls.EndsWith("ShadowWnd", StringComparison.Ordinal);
            }
        }
    }
}
#endif
