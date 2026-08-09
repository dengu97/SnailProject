using System;
using System.Collections.Generic;

namespace DesktopShellProbe
{
    /// <summary>달팽이가 기어오를 수 있는 수평 표면 하나 (창의 위쪽 테두리).</summary>
    internal sealed class Surface
    {
        public IntPtr Hwnd;
        public string Title;
        public int Left, Right, Top;
        public int Width => Right - Left;
        public override string ToString() =>
            $"y={Top,5}  x={Left,5}..{Right,-5} (폭 {Width,4})  {Title}";
    }

    /// <summary>
    /// 열려 있는 창들의 위쪽 테두리를 걸을 수 있는 표면 목록으로 수집한다.
    /// Unity 로 그대로 이식 가능 (UnityEngine 의존 없음).
    /// </summary>
    internal static class WindowSurfaces
    {
        /// <param name="selfHwnd">펫 자신의 창 (표면에서 제외)</param>
        /// <param name="minWidth">이보다 좁은 창은 걸어다닐 가치가 없으므로 버린다</param>
        public static List<Surface> Collect(IntPtr selfHwnd, int minWidth = 120)
        {
            var found = new List<Surface>();

            Win32.EnumWindows((hwnd, _) =>
            {
                if (hwnd == selfHwnd) return true;
                if (!Win32.IsWindowVisible(hwnd)) return true;
                if (Win32.IsIconic(hwnd)) return true;

                // UWP 앱은 안 보이는 상태로도 살아있다 (cloaked). 걸러내지 않으면 유령 표면이 생긴다.
                if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
                    && cloaked != 0) return true;

                int len = Win32.GetWindowTextLengthW(hwnd);
                if (len <= 0) return true;                      // 제목 없는 창은 대부분 껍데기다
                var buf = new char[len + 1];
                int got = Win32.GetWindowTextW(hwnd, buf, buf.Length);
                string title = new string(buf, 0, Math.Max(0, got));
                if (string.IsNullOrWhiteSpace(title)) return true;

                // 소유된 창(대화상자·툴팁·그림자 헬퍼)은 독립 창이 아니다
                if (Win32.GetWindow(hwnd, Win32.GW_OWNER) != IntPtr.Zero) return true;

                int ex = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
                // 클릭 통과 오버레이(NVIDIA 오버레이 등)와 도구 창은 표면이 아니다.
                // 우리 펫도 정확히 이 스타일을 쓰므로, 다른 펫 인스턴스도 자동으로 걸러진다.
                if ((ex & Win32.WS_EX_TRANSPARENT) != 0) return true;
                if ((ex & Win32.WS_EX_TOOLWINDOW)  != 0) return true;

                if (IsShellClass(hwnd)) return true;            // 바탕화면·작업표시줄

                // 그림자 여백이 없는 실제 보이는 경계를 쓴다
                Win32.RECT r;
                if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS,
                        out r, System.Runtime.InteropServices.Marshal.SizeOf<Win32.RECT>()) != 0)
                {
                    if (!Win32.GetWindowRect(hwnd, out r)) return true;
                }

                if (r.Width < minWidth || r.Height <= 0) return true;

                found.Add(new Surface { Hwnd = hwnd, Title = title, Left = r.Left, Right = r.Right, Top = r.Top });
                return true;
            }, IntPtr.Zero);

            // EnumWindows 는 z-order 앞에서 뒤 순서로 준다. 위쪽 표면부터 보고 싶으니 y 로 정렬.
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
