using UnityEngine;

namespace SnailPet.Ui
{
    /// <summary>
    /// 칸에 씌운 둥근 테두리를 <b>그 칸을 따라</b> 켜고 끈다.
    ///
    /// 테두리는 칸의 자식이 아니라 형제다(<see cref="SnailUi"/> 의 AddSlotFrames 참고).
    /// 그래서 칸을 끄면 저 혼자 남는다 — 빈 그리드 칸 스무 개에 테두리만 둥둥 떠 있었다.
    ///
    /// 켜고 끄는 곳이 여러 군데(알·음식·상점·옷장)라 부르는 쪽마다 짝지어 두면 언젠가
    /// 빠뜨린다. 칸 쪽에 붙여 두면 유니티가 알아서 짝을 맞춰 준다.
    /// </summary>
    public sealed class UiSlotFrame : MonoBehaviour
    {
        public GameObject Frame;

        private void OnEnable()  { if (Frame != null) Frame.SetActive(true); }
        private void OnDisable() { if (Frame != null) Frame.SetActive(false); }
    }
}
