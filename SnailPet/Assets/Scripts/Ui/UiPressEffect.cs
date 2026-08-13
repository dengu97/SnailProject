using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SnailPet.Ui
{
    /// <summary>
    /// 누르면 살짝 작아졌다 돌아오는 반응.
    ///
    /// 색 전환만으로는 표가 안 난다 — 버튼 아트에 색이 이미 들어 있어 물들여도 잘 안 보인다.
    /// 그래서 크기로 반응을 준다.
    ///
    /// 같이 움직일 것을 찾아 두는 이유: 이 UI 는 버튼마다 구조가 다르다. 아이콘 버튼은
    /// 그림이 자식이라 버튼만 줄이면 같이 줄지만, 글자 버튼과 설정 행은 글자·체크 표시가
    /// <b>형제</b>로 얹혀 있어 버튼만 줄이면 배경만 쪼그라들고 글자는 그대로 남는다.
    /// 그래서 붙을 때 「내 위에 얹힌 형제 그래픽」을 모아 함께 줄인다.
    ///
    /// 프리팹에서 손으로 배율을 준 버튼이 있으므로(하단 액션 1.2배) 반드시 <b>원래 배율에
    /// 곱한다</b>. 절대값으로 넣으면 그 조정이 눌릴 때마다 사라진다.
    /// </summary>
    public sealed class UiPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>눌렸을 때의 배율.</summary>
        public const float PressedScale = 0.92f;

        /// <summary>돌아오는 빠르기. 클수록 탁 붙는다.</summary>
        private const float Speed = 18f;

        private readonly List<RectTransform> _group = new List<RectTransform>();
        private readonly List<Vector3> _base = new List<Vector3>();
        private readonly List<Vector2> _basePos = new List<Vector2>();

        private bool _down;
        private float _now = 1f;

        private void Awake()
        {
            Collect();
        }

        /// <summary>함께 줄일 것을 모은다. 나 자신과, 내 칸 안에 들어 있는 형제 그래픽.</summary>
        private void Collect()
        {
            _group.Clear();
            _base.Clear();
            _basePos.Clear();

            var me = transform as RectTransform;
            if (me == null) return;

            Add(me);
            if (me.parent == null) return;

            foreach (Transform sibling in me.parent)
            {
                if (sibling == me) continue;

                var rt = sibling as RectTransform;
                if (rt == null || rt.GetComponent<Graphic>() == null) continue;

                // 다른 버튼은 제 반응이 따로 있다. 건드리면 두 번 줄어든다.
                if (rt.GetComponent<Button>() != null) continue;

                if (CenterInside(me, rt)) Add(rt);
            }
        }

        private void Add(RectTransform rt)
        {
            _group.Add(rt);
            _base.Add(rt.localScale);
            _basePos.Add(rt.anchoredPosition);
        }

        /// <summary>
        /// 배율이 <paramref name="s"/> 일 때의 자리. 한가운데가 제자리에 남도록 민다.
        ///
        /// 크기만 줄이면 <b>피벗</b> 쪽으로 쪼그라든다. 이 UI 는 목업 좌표를 그대로 쓰려고
        /// 피벗을 왼쪽 위에 두므로 그냥 두면 왼쪽 위로 빨려 들어간다. 피벗에서 한가운데까지의
        /// 거리가 배율만큼 줄어드니, 줄어든 만큼 되밀어 주면 한가운데가 고정된다.
        /// </summary>
        private static Vector2 PlaceFor(RectTransform rt, Vector2 basePos, float s)
        {
            var p = rt.pivot;
            var size = rt.rect.size;
            var toCenter = new Vector2((0.5f - p.x) * size.x, (0.5f - p.y) * size.y);
            return basePos + toCenter * (1f - s);
        }

        /// <summary>형제의 한가운데가 내 칸 안에 있는가. 글자칸이 버튼보다 넓은 경우가 있어 한가운데로 본다.</summary>
        private static bool CenterInside(RectTransform outer, RectTransform inner)
        {
            var o = new Vector3[4]; outer.GetWorldCorners(o);
            var i = new Vector3[4]; inner.GetWorldCorners(i);

            var c = (i[0] + i[2]) * 0.5f;
            return c.x >= Mathf.Min(o[0].x, o[2].x) && c.x <= Mathf.Max(o[0].x, o[2].x)
                && c.y >= Mathf.Min(o[0].y, o[2].y) && c.y <= Mathf.Max(o[0].y, o[2].y);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 제자리로 다 돌아온 상태에서만 기준을 다시 잡는다. 돌아오는 중에 또 누르면
            // 줄어든 값이 기준으로 굳는다.
            if (_now >= 0.999f)
                for (int i = 0; i < _group.Count; i++)
                    if (_group[i] != null) { _base[i] = _group[i].localScale; _basePos[i] = _group[i].anchoredPosition; }

            _down = true;
        }

        public void OnPointerUp(PointerEventData eventData) => _down = false;

        private void OnDisable()
        {
            // 눌린 채로 화면이 바뀌면 작아진 상태로 굳는다. 꺼질 때 되돌려 둔다.
            _down = false;
            _now = 1f;
            Apply();
        }

        private void Update()
        {
            float want = _down ? PressedScale : 1f;
            if (Mathf.Approximately(_now, want)) return;

            // 프레임 간격과 무관하게 같은 속도로 붙는다
            _now = Mathf.Lerp(_now, want, 1f - Mathf.Exp(-Speed * Time.deltaTime));
            if (Mathf.Abs(_now - want) < 0.001f) _now = want;

            Apply();
        }

        private void Apply()
        {
            for (int i = 0; i < _group.Count; i++)
            {
                var rt = _group[i];
                if (rt == null) continue;

                rt.localScale = _base[i] * _now;
                rt.anchoredPosition = PlaceFor(rt, _basePos[i], _now);
            }
        }
    }
}
