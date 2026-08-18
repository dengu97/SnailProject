using UnityEngine;
using UnityEngine.EventSystems;

namespace SnailPet.Ui
{
    /// <summary>
    /// 위젯을 통째로 잡아 끌어 옮긴다.
    ///
    /// 위젯 루트에 하나만 붙인다. UGUI 의 드래그는 <b>눌린 것에서 부모로 거슬러 올라가며</b>
    /// 처리할 놈을 찾으므로, 자식 아무 데나 눌러도 여기까지 온다. 스스로 드래그를 처리하는
    /// 것(목록의 ScrollRect)은 거기서 멈추므로 목록은 그대로 스크롤된다.
    ///
    /// 버튼 위에서 끌어도 된다. 드래그가 시작되는 순간 유니티가 눌러 둔 버튼을 놓아 주므로
    /// (PointerInputModule.ProcessDrag) 손을 뗄 때 그 버튼이 눌리지는 않는다. 반대로 조금만
    /// 움직인 것은 드래그로 치지 않아 평소처럼 클릭된다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiDragMove : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform _rect, _area;
        private Vector2 _grab, _start;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _area = _rect.parent as RectTransform;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _start = _rect.anchoredPosition;
            _grab = LocalPoint(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_area == null) return;
            _rect.anchoredPosition = Clamped(_start + (LocalPoint(e) - _grab));
        }

        /// <summary>크기가 바뀐 뒤 다시 화면 안으로 들인다. 위로 자라면 화면 밖으로 나갈 수 있다.</summary>
        public void ClampNow()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            if (_area == null) _area = _rect.parent as RectTransform;
            if (_area != null) _rect.anchoredPosition = Clamped(_rect.anchoredPosition);
        }

        /// <summary>
        /// 커서를 부모 안의 좌표로 옮긴다. <b>캔버스 스케일이 여기서 걷힌다</b> —
        /// 화면 픽셀 이동량을 그대로 더하면 UI 크기 x1.5 에서 위젯이 커서보다 빨리 달아난다.
        /// (스크린 스페이스 오버레이라 카메라는 null 이다)
        /// </summary>
        private Vector2 LocalPoint(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, null, out var p);
            return p;
        }

        /// <summary>위젯이 화면 밖으로 나가지 않게 붙든다. 한번 놓치면 다시 잡을 수가 없다.</summary>
        private Vector2 Clamped(Vector2 pos)
        {
            var area = _area.rect;
            var size = _rect.rect.size;

            // 앵커가 놓인 자리(부모 안의 좌표). 위젯은 오른쪽 아래에 매여 있다.
            var anchor = new Vector2(Mathf.Lerp(area.xMin, area.xMax, _rect.anchorMin.x),
                                     Mathf.Lerp(area.yMin, area.yMax, _rect.anchorMin.y));

            float left   = anchor.x + pos.x - _rect.pivot.x * size.x;
            float bottom = anchor.y + pos.y - _rect.pivot.y * size.y;

            pos.x += Mathf.Clamp(left,   area.xMin, area.xMax - size.x) - left;
            pos.y += Mathf.Clamp(bottom, area.yMin, area.yMax - size.y) - bottom;
            return pos;
        }
    }
}
