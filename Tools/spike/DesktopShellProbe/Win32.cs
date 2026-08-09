using System;
using System.Runtime.InteropServices;

namespace DesktopShellProbe
{
    /// <summary>
    /// 데스크톱 상주 펫에 필요한 Win32 호출 모음.
    /// 이 파일은 Unity 로 그대로 옮겨갈 수 있도록 UnityEngine 의존성을 두지 않는다.
    /// </summary>
    internal static class Win32
    {
        // ── 윈도우 스타일 ──
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED     = 0x00080000; // 레이어드 윈도우 (per-pixel alpha 전제)
        public const int WS_EX_TRANSPARENT = 0x00000020; // 마우스 입력이 뒤 창으로 통과
        public const int WS_EX_TOOLWINDOW  = 0x00000080; // Alt-Tab / 작업표시줄에 안 뜸
        public const int WS_EX_NOACTIVATE  = 0x08000000; // 클릭해도 포커스를 뺏지 않음

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010, SWP_SHOWWINDOW = 0x0040;

        public const byte AC_SRC_OVER  = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int  ULW_ALPHA    = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x, y; public POINT(int a, int b) { x = a; y = b; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int cx, cy; public SIZE(int a, int b) { cx = a; cy = b; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width  => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
            ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc,
            int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT p);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        // ── 다른 창 탐색 (달팽이가 기어오를 표면) ──
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextW(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLengthW(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassNameW(IntPtr hWnd, [Out] char[] lpClassName, int nMaxCount);

        public const uint GW_OWNER = 4;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        /// <summary>
        /// DWM 확장 프레임 경계. Win10/11 에서 GetWindowRect 는 보이지 않는 그림자 여백을
        /// 포함하므로, 실제 눈에 보이는 테두리를 얻으려면 이쪽을 써야 한다.
        /// </summary>
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        public const int DWMWA_CLOAKED = 14;

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
            out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
            out int pvAttribute, int cbAttribute);
    }
}
