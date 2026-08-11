using UnityEngine;

namespace SnailPet.Ui
{
    /// <summary>
    /// 이 Text 가 어떤 언어 키에서 온 글자인지 남겨 둔다.
    ///
    /// 프리팹에는 <b>구울 때의 글자가 그대로 굳는다.</b> 그래서 나중에 시트에 번역을
    /// 채워도 화면에는 옛 값이 계속 나온다 — 실제로 상점 탭의 여섯 글자가 토큰인 채로
    /// 남아 있었다. 프리팹은 번역본의 스냅샷이 되어서는 안 된다.
    ///
    /// 스프라이트를 <see cref="UiShapeRef"/> 로 되살리는 것과 같은 이유다.
    /// 다만 이쪽은 <b>비어 있지 않아도 덮어쓴다</b> — 시트가 원본이라, 프리팹에서 손으로
    /// 고쳐도 다음 실행에 되돌아간다. 글자를 바꾸려면 LanguageData 를 고쳐야 한다.
    /// </summary>
    public sealed class UiTextRef : MonoBehaviour
    {
        public string Token;
    }
}
