#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using SnailPet.Desktop;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SnailPet.Ui
{
    /// <summary>
    /// UGUI 가 쓸 마우스 입력을 Win32 에서 직접 읽어 온다.
    ///
    /// 데스크톱 펫 창은 WS_EX_NOACTIVATE 라 포커스를 절대 갖지 않는다. 그래서
    /// Unity 의 Input 에 마우스가 안 들어오고, 버튼은 눌러도 아무 일이 없다.
    /// 달팽이 클릭을 GetAsyncKeyState 로 처리한 것과 같은 이유다.
    ///
    /// StandaloneInputModule.inputOverride 에 꽂으면 EventSystem 전체가
    /// 이 값을 쓰므로, 버튼·드래그·호버가 전부 평소처럼 동작한다.
    /// </summary>
    public sealed class Win32UiInput : BaseInput
    {
        private bool _down, _wasDown;
        private int _polledFrame = -1;

        /// <summary>
        /// 프레임당 한 번만 읽는다.
        /// Update 에서 읽으면 입력 모듈과의 실행 순서에 따라 눌림 감지가 한 프레임 어긋난다.
        /// 물어볼 때 읽되 같은 프레임이면 캐시를 쓰면 순서와 무관해진다.
        /// </summary>
        private void Poll()
        {
            if (_polledFrame == Time.frameCount) return;
            _polledFrame = Time.frameCount;

            _wasDown = _down;
            _down = TransparentWindow.IsLeftMouseDown();
        }

        public override Vector2 mousePosition
        {
            get
            {
                if (!TransparentWindow.TryGetCursor(out int x, out int y))
                    return base.mousePosition;

                // 가상 화면(왼쪽 위 원점, y 아래로) → 유니티 화면(왼쪽 아래 원점, y 위로)
                var v = TransparentWindow.VirtualScreen;
                return new Vector2(x - v.Left, v.Height - (y - v.Top));
            }
        }

        public override bool mousePresent => true;

        /// <summary>
        /// 휠. 포커스가 없어 WM_MOUSEWHEEL 이 안 오므로 저수준 훅이 모아 둔 값을 쓴다
        /// (MouseWheelHook). 안 걸렸으면 0 이라 굴러가지 않을 뿐 나머지는 그대로 동작한다.
        /// </summary>
        public override Vector2 mouseScrollDelta => new Vector2(0f, MouseWheelHook.Take());

        public override bool GetMouseButton(int button)     { Poll(); return button == 0 && _down; }
        public override bool GetMouseButtonDown(int button) { Poll(); return button == 0 && _down && !_wasDown; }
        public override bool GetMouseButtonUp(int button)   { Poll(); return button == 0 && !_down && _wasDown; }
    }
}
#endif
