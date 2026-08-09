#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;

namespace SnailPet.Desktop
{
    /// <summary>
    /// 달팽이가 기어다닐 「박스」를 고른다.
    ///
    /// 기획서가 말하는 대상은 <b>활성 창</b>이다. 활성 창은 정의상 맨 앞이라
    /// 다른 창에 가려질 수 없고, 그래서 가려진 구간을 계산할 필요가 없다.
    /// 쓸 만한 활성 창이 없으면 화면 전체를 박스로 삼는다.
    /// </summary>
    public static class ActiveWindowBox
    {
        /// <summary>이 값보다 작은 창은 기어다닐 가치가 없다.</summary>
        public const int MinSize = 160;

        public static IntPtr CurrentHwnd { get; private set; }
        public static string CurrentTitle { get; private set; } = "(화면)";

        /// <summary>
        /// 현재 박스. 활성 창이 화면 밖으로 걸쳐 있으면 화면과 교집합을 취해
        /// 달팽이가 보이지 않는 곳으로 가지 않게 한다.
        /// </summary>
        private static IntPtr _lastGood;

        public static ScreenRect Resolve(IntPtr selfHwnd)
        {
            var screen = TransparentWindow.VirtualScreen;

            // 1) 지금 활성인 창
            if (TryBox(Win32.GetForegroundWindow(), selfHwnd, screen, out var box, out var hwnd))
            {
                _lastGood = hwnd;
                CurrentHwnd = hwnd;
                CurrentTitle = TitleOf(hwnd);
                return box;
            }

            // 2) 활성 창이 쓸 수 없으면(바탕화면 클릭, 펫 자신이 앞에 온 경우 등)
            //    마지막으로 쓸 만했던 창에 그대로 머무른다. 매번 화면 전체로 튀는 것보다 자연스럽다.
            if (TryBox(_lastGood, selfHwnd, screen, out box, out hwnd))
            {
                CurrentHwnd = hwnd;
                CurrentTitle = TitleOf(hwnd) + " (직전 활성)";
                return box;
            }

            // 3) 그것도 없으면 화면을 박스로 삼는다
            _lastGood = IntPtr.Zero;
            CurrentHwnd = IntPtr.Zero;
            CurrentTitle = "(화면 전체)";
            return screen;
        }

        private static bool TryBox(IntPtr hwnd, IntPtr selfHwnd, ScreenRect screen,
                                   out ScreenRect box, out IntPtr resolved)
        {
            box = default; resolved = IntPtr.Zero;
            if (!IsUsable(hwnd, selfHwnd)) return false;
            if (!TryGetBounds(hwnd, out var r)) return false;

            var clipped = Intersect(r, screen);
            if (clipped.Width < MinSize || clipped.Height < MinSize) return false;

            box = clipped; resolved = hwnd;
            return true;
        }

        private static bool IsUsable(IntPtr hwnd, IntPtr selfHwnd)
        {
            if (hwnd == IntPtr.Zero || hwnd == selfHwnd) return false;
            if (!Win32.IsWindowVisible(hwnd)) return false;
            if (Win32.IsIconic(hwnd)) return false;

            if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
                && cloaked != 0) return false;

            int ex = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            // 클릭 통과 오버레이는 창이 아니다. 우리 펫도 이 스타일을 쓰므로 자기 자신도 걸러진다.
            if ((ex & Win32.WS_EX_TRANSPARENT) != 0) return false;

            return !IsShellClass(hwnd);
        }

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
                    return true;
                default:
                    return cls.EndsWith("ShadowWnd", StringComparison.Ordinal);
            }
        }

        private static string TitleOf(IntPtr hwnd)
        {
            int len = Win32.GetWindowTextLengthW(hwnd);
            if (len <= 0) return "(제목 없음)";
            var buf = new char[len + 1];
            int got = Win32.GetWindowTextW(hwnd, buf, buf.Length);
            return new string(buf, 0, Math.Max(0, got));
        }

        /// <summary>그림자 여백이 없는 실제 보이는 경계를 쓴다.</summary>
        private static bool TryGetBounds(IntPtr hwnd, out ScreenRect rect)
        {
            rect = default;
            Win32.RECT r;
            if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS,
                    out r, Marshal.SizeOf(typeof(Win32.RECT))) != 0)
            {
                if (!Win32.GetWindowRect(hwnd, out r)) return false;
            }
            rect = new ScreenRect { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
            return rect.Width > 0 && rect.Height > 0;
        }

        private static ScreenRect Intersect(ScreenRect a, ScreenRect b) => new ScreenRect
        {
            Left   = Math.Max(a.Left,   b.Left),
            Top    = Math.Max(a.Top,    b.Top),
            Right  = Math.Min(a.Right,  b.Right),
            Bottom = Math.Min(a.Bottom, b.Bottom),
        };
    }
}
#endif
