using UnityEngine;

namespace SnailPet.Ui
{
    /// <summary>
    /// 이 Image 가 어떤 도형 역할인지 남겨 둔다.
    ///
    /// 프리팹에는 <b>런타임에 만든 스프라이트를 저장할 수 없다.</b> 아트가 없어 코드가
    /// 생성한 도형은 프리팹을 다시 불러오면 비어 있게 된다. 그때 역할을 알아야 다시 채운다.
    ///
    /// 비어 있을 때<b>만</b> 채운다. 프리팹에서 직접 스프라이트를 갈아 끼우셨다면
    /// 그 값이 남아 있으므로 건드리지 않는다.
    /// </summary>
    public sealed class UiShapeRef : MonoBehaviour
    {
        public UiSprites.Shape Shape;
    }
}
