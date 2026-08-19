#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace SnailPet.Desktop
{
    /// <summary>
    /// 마우스 휠을 <b>창 밖에서</b> 읽는다.
    ///
    /// 이 창은 WS_EX_NOACTIVATE 라 포커스를 절대 못 받는데, 윈도우는 WM_MOUSEWHEEL 을
    /// 포커스를 가진 창에만 보낸다. 그래서 유니티 Input 에는 휠이 영영 안 들어오고
    /// 목록이 휠로 안 굴러간다. 버튼·커서를 GetAsyncKeyState 로 읽은 것과 같은 사정이다.
    ///
    /// <b>훅은 반드시 전용 스레드에서 돌린다.</b> 저수준 마우스 훅은 「훅을 건 스레드의 메시지
    /// 큐」로 배달되므로, 게임 메인 스레드에 걸면 <b>시스템 전체의 마우스 입력이 그 스레드의
    /// 프레임 속도에 묶인다</b> — 60fps 면 모든 마우스 이벤트가 최대 16ms 씩 밀려 PC 전체가
    /// 버벅이는 것처럼 느껴진다. 실제로 그렇게 만들었다가 되돌린 자리다(2026-08-19).
    ///
    /// 콜백은 값만 더하고 즉시 돌려준다. 여기서 무거운 일을 하면 마우스가 끊긴다.
    /// </summary>
    public static class MouseWheelHook
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_QUIT = 0x0012;
        private const float WheelDelta = 120f;      // 한 칸

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int x, y;
            public uint mouseData;      // 위쪽 16비트가 휠 값
            public uint flags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam, lParam;
            public uint time;
            public int x, y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private static Thread _thread;
        private static uint _threadId;
        private static IntPtr _hook;
        private static HookProc _proc;      // 델리게이트를 살려 둬야 한다. GC 가 걷어 가면 훅이 죽는다.

        private static readonly object _lock = new object();
        private static float _pending;

        public static bool Active => _thread != null;

        public static void Install()
        {
            if (Active) return;

            _thread = new Thread(Pump) { IsBackground = true, Name = "SnailPet.WheelHook" };
            _thread.Start();
        }

        /// <summary>전용 스레드. 훅을 걸고 메시지를 돌리다가 WM_QUIT 을 받으면 정리하고 끝난다.</summary>
        private static void Pump()
        {
            _threadId = GetCurrentThreadId();
            _proc = OnMouse;
            _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(null), 0);

            if (_hook == IntPtr.Zero)
            {
                Debug.LogWarning("[SnailPet] 휠 훅을 걸지 못했습니다 (오류 " + Marshal.GetLastWin32Error() + ")");
                return;
            }

            // 저수준 훅은 이 스레드의 메시지 큐로 배달된다. 큐를 돌려야 콜백이 불린다.
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0) { }

            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
            _proc = null;
        }

        public static void Remove()
        {
            if (!Active) return;

            if (_threadId != 0) PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread.Join(500);

            _thread = null;
            _threadId = 0;
            lock (_lock) _pending = 0f;
        }

        /// <summary>지난 프레임 이후 굴린 양(칸 단위). 읽으면 0 으로 돌아간다.</summary>
        public static float Take()
        {
            lock (_lock)
            {
                float v = _pending;
                _pending = 0f;
                return v;
            }
        }

        private static IntPtr OnMouse(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (int)wParam == WM_MOUSEWHEEL)
            {
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                float turns = (short)(data.mouseData >> 16) / WheelDelta;

                lock (_lock) _pending += turns;
            }

            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }
    }
}
#endif
