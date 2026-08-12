#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using UnityEngine;

namespace SnailPet.Desktop
{

    /// <summary>
    /// Unity 플레이어 창 자체를 데스크톱 펫용 창으로 바꾼다.
    ///
    /// WinForms 스파이크는 UpdateLayeredWindow 로 비트맵을 직접 밀어 넣었지만,
    /// Unity 는 자기 스왑체인으로 그리므로 그 경로를 쓸 수 없다. 대신
    /// 「테두리 없는 창 + DWM 유리 영역 확장 + 카메라 클리어 알파 0」 조합을 쓴다.
    ///
    /// 주의: 에디터 Play 모드에서는 GetActiveWindow 가 에디터 창을 돌려주므로
    /// 적용하지 않는다. 반드시 빌드된 플레이어에서 확인할 것.
    /// </summary>
    public static class TransparentWindow
    {
        public static IntPtr Hwnd { get; private set; }
        public static bool Applied { get; private set; }
        public static string LastError { get; private set; }

        /// <summary>가상 화면 전체를 덮는 투명·항상 위·클릭 통과 창으로 만든다.</summary>
        public static bool Apply(bool clickThrough = true)
        {
            LastError = null;

            if (Application.isEditor)
            {
                LastError = "에디터에서는 적용하지 않습니다 (빌드된 플레이어에서 확인하세요).";
                return false;
            }

            IntPtr hwnd = Win32.GetActiveWindow();
            if (hwnd == IntPtr.Zero)
            {
                LastError = "GetActiveWindow 가 0 을 돌려줬습니다.";
                return false;
            }
            Hwnd = hwnd;

            // 1) 테두리·타이틀바 제거
            Win32.SetWindowLong(hwnd, Win32.GWL_STYLE, Win32.WS_POPUP | Win32.WS_VISIBLE);

            // 2) 확장 스타일: 레이어드 + 도구 창 + 비활성화. 클릭 통과는 선택.
            int ex = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            ex |= Win32.WS_EX_LAYERED | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE;
            if (clickThrough) ex |= Win32.WS_EX_TRANSPARENT;
            else              ex &= ~Win32.WS_EX_TRANSPARENT;
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, ex);

            // 3) 창 전체를 DWM 유리 영역으로. 이게 있어야 알파 0 픽셀이 실제로 뚫린다.
            var margins = new Win32.MARGINS
            {
                cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1
            };
            int hr = Win32.DwmExtendFrameIntoClientArea(hwnd, ref margins);
            if (hr != 0)
            {
                LastError = "DwmExtendFrameIntoClientArea 실패 (hr=0x" + hr.ToString("X8") + ")";
                return false;
            }

            // 4) 가상 화면 전체를 덮고 항상 위로
            var v = VirtualScreen;
            Win32.SetWindowPos(hwnd, Win32.HWND_TOPMOST, v.Left, v.Top, v.Width, v.Height,
                Win32.SWP_FRAMECHANGED | Win32.SWP_SHOWWINDOW | Win32.SWP_NOACTIVATE);

            Applied = true;
            return true;
        }

        /// <summary>모든 모니터를 포함하는 경계. 주 모니터 왼쪽에 모니터가 있으면 Left 가 음수다.</summary>
        public static ScreenRect VirtualScreen
        {
            get
            {
                int x = Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN);
                int y = Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN);
                int w = Win32.GetSystemMetrics(Win32.SM_CXVIRTUALSCREEN);
                int h = Win32.GetSystemMetrics(Win32.SM_CYVIRTUALSCREEN);
                return new ScreenRect { Left = x, Top = y, Right = x + w, Bottom = y + h };
            }
        }

        /// <summary>클릭 통과 여부를 실제 스타일 비트로 다시 읽어 확인한다.</summary>
        public static bool IsClickThrough()
        {
            if (Hwnd == IntPtr.Zero) return false;
            return (Win32.GetWindowLong(Hwnd, Win32.GWL_EXSTYLE) & Win32.WS_EX_TRANSPARENT) != 0;
        }

        /// <summary>
        /// 클릭 통과를 켜고 끈다.
        ///
        /// 펫은 평소 클릭이 전부 뒤로 통과해야 바탕화면 작업을 방해하지 않는다.
        /// 하지만 말풍선처럼 눌러야 하는 것이 떠 있는 동안에는 그 위에서만 통과를 꺼서
        /// 클릭이 뒤 창으로 새지 않게 해야 한다. 매 프레임 커서 위치로 판단해 토글한다.
        /// </summary>
        public static void SetClickThrough(bool enabled)
        {
            if (Hwnd == IntPtr.Zero) return;

            int ex = Win32.GetWindowLong(Hwnd, Win32.GWL_EXSTYLE);
            bool now = (ex & Win32.WS_EX_TRANSPARENT) != 0;
            if (now == enabled) return;                 // 매 프레임 SetWindowLong 을 때리지 않는다

            if (enabled) ex |=  Win32.WS_EX_TRANSPARENT;
            else         ex &= ~Win32.WS_EX_TRANSPARENT;
            Win32.SetWindowLong(Hwnd, Win32.GWL_EXSTYLE, ex);
        }

        /// <summary>글자를 받는 동안 포커스를 빌리기 전에 있던 창. 돌려줄 곳이다.</summary>
        private static IntPtr _focusReturnTo;

        /// <summary>
        /// 키보드 포커스를 잠깐 빌린다. 이름 입력처럼 <b>글자를 받아야 할 때만</b> 켠다.
        ///
        /// 이 창은 평소 WS_EX_NOACTIVATE 라 절대 포커스를 갖지 않는다. 그래서 마우스는
        /// GetAsyncKeyState 로 우회해 읽지만, 글자는 그럴 수 없다 — 한글은 IME 조합이
        /// 필요하고 IME 는 포커스를 가진 창에만 붙는다.
        ///
        /// 끌 때는 빌리기 전에 앞에 있던 창으로 돌려준다. 안 그러면 쓰던 작업이 뒤로 밀린 채
        /// 남는다.
        /// </summary>
        public static void SetKeyboardFocus(bool on)
        {
            if (Hwnd == IntPtr.Zero) return;

            int ex = Win32.GetWindowLong(Hwnd, Win32.GWL_EXSTYLE);
            bool now = (ex & Win32.WS_EX_NOACTIVATE) == 0;
            if (now == on) return;

            if (on)
            {
                _focusReturnTo = Win32.GetForegroundWindow();
                if (_focusReturnTo == Hwnd) _focusReturnTo = IntPtr.Zero;

                // 포커스를 받으려면 클릭도 통과시키면 안 된다
                ex &= ~Win32.WS_EX_NOACTIVATE;
                ex &= ~Win32.WS_EX_TRANSPARENT;
                Win32.SetWindowLong(Hwnd, Win32.GWL_EXSTYLE, ex);

                Steal(_focusReturnTo);
                return;
            }

            ex |= Win32.WS_EX_NOACTIVATE;
            Win32.SetWindowLong(Hwnd, Win32.GWL_EXSTYLE, ex);

            // 돌려줄 때도 붙였다 떼야 한다. 그냥 SetForegroundWindow 만 부르면
            // 포커스가 펫에 남아, 이름을 고친 뒤 쓰던 창이 뒤로 밀린 채 끝난다.
            if (_focusReturnTo != IntPtr.Zero) Give(_focusReturnTo);
            _focusReturnTo = IntPtr.Zero;
        }

        /// <summary>빌린 포커스를 원래 창으로 돌려준다.</summary>
        private static void Give(IntPtr to)
        {
            uint me = Win32.GetCurrentThreadId();
            uint other = Win32.GetWindowThreadProcessId(to, IntPtr.Zero);

            bool attached = other != 0 && other != me && Win32.AttachThreadInput(me, other, true);
            try
            {
                Win32.BringWindowToTop(to);
                Win32.SetForegroundWindow(to);
            }
            finally
            {
                if (attached) Win32.AttachThreadInput(me, other, false);
            }
        }

        /// <summary>
        /// 포그라운드 잠금을 넘어 포커스를 가져온다.
        ///
        /// 그냥 SetForegroundWindow 를 부르면 뒤에 있는 프로세스라 조용히 무시된다.
        /// 지금 앞에 있는 창의 입력 큐에 잠깐 붙으면 같은 큐로 취급되어 넘겨받을 수 있다.
        /// 붙였으면 반드시 뗀다 — 붙어 있는 동안 두 스레드의 입력 상태가 묶인다.
        /// </summary>
        private static void Steal(IntPtr from)
        {
            uint me = Win32.GetCurrentThreadId();
            uint other = from != IntPtr.Zero ? Win32.GetWindowThreadProcessId(from, IntPtr.Zero) : 0;

            bool attached = other != 0 && other != me && Win32.AttachThreadInput(me, other, true);
            try
            {
                Win32.BringWindowToTop(Hwnd);
                Win32.SetForegroundWindow(Hwnd);
                Win32.SetFocus(Hwnd);
            }
            finally
            {
                if (attached) Win32.AttachThreadInput(me, other, false);
            }
        }

        /// <summary>커서의 가상 화면 좌표.</summary>
        public static bool TryGetCursor(out int x, out int y)
        {
            x = y = 0;
            if (!Win32.GetCursorPos(out var p)) return false;
            x = p.x; y = p.y;
            return true;
        }

        /// <summary>왼쪽 버튼이 지금 눌려 있는가. 포커스가 없어도 읽힌다.</summary>
        public static bool IsLeftMouseDown() => (Win32.GetAsyncKeyState(Win32.VK_LBUTTON) & 0x8000) != 0;
    }
}
#endif
