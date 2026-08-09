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
    }
}
#endif
