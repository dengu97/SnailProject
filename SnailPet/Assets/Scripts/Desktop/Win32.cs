#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;

namespace SnailPet.Desktop
{
    /// <summary>
    /// 데스크톱 상주 펫에 필요한 Win32 호출 모음.
    /// Tools/spike/DesktopShellProbe/Win32.cs 와 동일한 내용이며,
    /// UnityEngine 의존성이 없도록 유지한다 (스파이크와 양방향으로 옮길 수 있게).
    /// </summary>
    internal static class Win32
    {
        // ── 윈도우 스타일 ──
        public const int GWL_STYLE   = -16;
        public const int GWL_EXSTYLE = -20;

        public const int WS_POPUP   = unchecked((int)0x80000000);
        public const int WS_VISIBLE = 0x10000000;

        public const int WS_EX_LAYERED     = 0x00080000; // 레이어드 윈도우 (per-pixel alpha 전제)
        public const int WS_EX_TRANSPARENT = 0x00000020; // 마우스 입력이 뒤 창으로 통과
        public const int WS_EX_TOOLWINDOW  = 0x00000080; // Alt-Tab / 작업표시줄에 안 뜸
        public const int WS_EX_NOACTIVATE  = 0x08000000; // 클릭해도 포커스를 뺏지 않음

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004,
                          SWP_NOACTIVATE = 0x0010, SWP_FRAMECHANGED = 0x0020, SWP_SHOWWINDOW = 0x0040;

        // ── 가상 화면 (멀티 모니터에서 좌표가 음수가 될 수 있다) ──
        public const int SM_XVIRTUALSCREEN = 76, SM_YVIRTUALSCREEN = 77,
                         SM_CXVIRTUALSCREEN = 78, SM_CYVIRTUALSCREEN = 79;

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x, y; public POINT(int a, int b) { x = a; y = b; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width  { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }

        /// <summary>DwmExtendFrameIntoClientArea 용. 전부 -1 이면 창 전체가 유리(투명) 영역이 된다.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight; }

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        /// <summary>지금 사용자가 보고 있는 창. 정의상 맨 앞이라 가려질 걱정이 없다.</summary>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT p);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public const int VK_LBUTTON = 0x01;

        /// <summary>
        /// 창에 포커스가 없어도 눌림을 읽을 수 있다.
        /// 펫 창은 WS_EX_NOACTIVATE 라 포커스를 갖지 않으므로 Unity 의 Input 대신 이쪽을 쓴다.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

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
#endif
