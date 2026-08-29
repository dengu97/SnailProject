using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SnailPet.Ui
{
    /// <summary>
    /// 휠 스크롤을 부드럽게 굴린다.
    ///
    /// 유니티 <see cref="ScrollRect"/> 는 휠 한 칸마다 내용을 <b>그 자리에서 즉시</b> 옮긴다
    /// (관성은 손으로 끌었을 때만 붙는다). 그래서 휠로 굴리면 뚝뚝 끊겨 뻑뻑하게 느껴진다.
    ///
    /// 여기서는 <b>가야 할 자리</b>를 따로 들고 매 프레임 그리로 다가간다. ScrollRect 쪽 휠은
    /// <c>scrollSensitivity = 0</c> 으로 막아 두고(부르는 쪽이 그렇게 맞춘다) 이쪽만 움직인다.
    /// 손으로 끄는 것은 ScrollRect 가 그대로 맡는다 — 끌기 시작하면 따라가기를 접는다.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class UiSmoothScroll : MonoBehaviour, IScrollHandler, IBeginDragHandler
    {
        /// <summary>휠 한 칸이 옮기는 거리(px). 목록 한 줄이 28~48 이라 한 줄 남짓이다.</summary>
        public float Step = 40f;

        /// <summary>목표를 따라가는 빠르기. 클수록 빨리 붙고 덜 미끄럽다.</summary>
        public float Speed = 16f;

        private ScrollRect _scroll;
        private float _target;
        private bool _chasing;

        private void Awake() => _scroll = GetComponent<ScrollRect>();

        /// <summary>더 갈 수 있는 거리. 내용이 창보다 짧으면 0 이다.</summary>
        private float MaxY
        {
            get
            {
                if (_scroll == null || _scroll.content == null || _scroll.viewport == null) return 0f;
                return Mathf.Max(0f, _scroll.content.rect.height - _scroll.viewport.rect.height);
            }
        }

        public void OnScroll(PointerEventData e)
        {
            if (_scroll == null || _scroll.content == null) return;

            // 새로 굴리기 시작할 때는 지금 자리에서 출발한다. 이어서 굴리면 목표가 쌓인다.
            if (!_chasing) _target = _scroll.content.anchoredPosition.y;

            _target = Mathf.Clamp(_target - e.scrollDelta.y * Step, 0f, MaxY);
            _chasing = true;
        }

        /// <summary>손으로 끌기 시작하면 휠이 가려던 자리는 없던 일이 된다.</summary>
        public void OnBeginDrag(PointerEventData e) => _chasing = false;

        private void LateUpdate()
        {
            if (!_chasing || _scroll == null || _scroll.content == null) return;

            var p = _scroll.content.anchoredPosition;

            // 프레임 길이에 안 휘둘리는 감쇠. dt 가 튀어도 목표를 지나치지 않는다.
            p.y = Mathf.Lerp(p.y, _target, 1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));

            if (Mathf.Abs(p.y - _target) < 0.4f) { p.y = _target; _chasing = false; }

            _scroll.content.anchoredPosition = p;
        }
    }
}
