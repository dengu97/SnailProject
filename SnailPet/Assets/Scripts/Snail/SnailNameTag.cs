using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 남의 달팽이 머리 위에 뜨는 이름표.
    ///
    /// 자리와 각도는 말풍선과 같은 규칙으로 부르는 쪽이 정해 넘긴다 — 달팽이가 붙은 벽을 따라
    /// 같이 돌아가야 하기 때문이다. 달팽이 루트의 자식으로 넣지 않는 것도 말풍선과 같은
    /// 이유다: 루트에는 좌우 반전과 몸통 변형이 걸려 있어 글자까지 뒤집히고 늘어난다.
    ///
    /// 바탕화면 위에 그리는 글자라 <b>배경색을 고를 수 없다.</b> 어두운 글자를 한 장 뒤에 깔아
    /// 밝은 바탕에서도 읽히게 한다.
    /// </summary>
    public sealed class SnailNameTag
    {
        /// <summary>말풍선보다 앞. 겹쳐도 이름이 가려지지 않는다.</summary>
        private const int SortingOrder = SnailBubble.BaseSortingOrder + 100;

        /// <summary>글자를 뽑아 두는 크기. 크게 뽑아 작게 줄여야 또렷하다.</summary>
        private const int FontSize = 48;

        /// <summary>화면에서의 한 줄 높이(px).</summary>
        private const float HeightPx = 10f;

        /// <summary>그림자를 밀어 두는 거리(px).</summary>
        private const float ShadowPx = 1f;

        private readonly Transform _root;
        private readonly TextMesh _text, _shadow;

        public SnailNameTag(Transform parent, Font font)
        {
            var go = new GameObject("NameTag");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            _root = go.transform;

            _shadow = Make(_root, font, new Color(0f, 0f, 0f, 0.55f), SortingOrder);
            _text   = Make(_root, font, Color.white, SortingOrder + 1);

            // 한 줄 높이가 1 로컬 단위이므로, 로컬 거리 d 는 화면에서 d x HeightPx 픽셀이 된다.
            float d = ShadowPx / HeightPx;
            _shadow.transform.localPosition = new Vector3(d * 0.5f, -d, 0f);

            Font.textureRebuilt += OnFontRebuilt;
        }

        private static TextMesh Make(Transform parent, Font font, Color color, int order)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<TextMesh>();
            t.font = font;
            t.fontSize = FontSize;
            t.characterSize = 10f / FontSize;      // 한 줄 높이가 정확히 1 로컬 단위가 된다
            t.anchor = TextAnchor.LowerCenter;
            t.alignment = TextAlignment.Center;
            t.color = color;

            // TextMesh 는 글꼴을 꽂아도 머티리얼이 따라오지 않는다. 안 넣으면 분홍색으로 나온다.
            var r = go.GetComponent<MeshRenderer>();
            if (font != null) r.sharedMaterial = font.material;
            r.sortingOrder = order;

            return t;
        }

        /// <summary>
        /// 글꼴 아틀라스가 다시 뽑혔다. 글자를 다시 얹어 메시를 새로 만든다.
        ///
        /// 동적 글꼴은 <b>쓰는 글자만</b> 아틀라스에 담아 두고, 자리가 모자라면 통째로 다시
        /// 뽑는다. 그러면 예전 아틀라스 좌표로 만들어 둔 메시는 엉뚱한 글자를 가리킨다.
        /// UGUI 의 Text 는 이 알림을 받아 스스로 다시 그리지만 <see cref="TextMesh"/> 는 안 한다.
        /// </summary>
        private void OnFontRebuilt(Font font)
        {
            if (_text == null || font != _text.font) return;

            string had = _text.text;
            if (string.IsNullOrEmpty(had)) return;

            _text.text = _shadow.text = "";
            _text.text = _shadow.text = had;
        }

        /// <summary>
        /// 그 자리·각도에 띄운다. 이름이 비어 있으면 아무것도 안 그린다 —
        /// 이름을 안 지은 달팽이에 빈 이름표만 떠 있으면 지저분하다.
        /// </summary>
        /// <param name="pixelsPerWorld">글자 크기를 화면 픽셀로 맞추는 데 쓴다.</param>
        public void Place(Vector3 worldPosition, float rotationDeg, float pixelsPerWorld,
                          string name, bool visible)
        {
            bool on = visible && !string.IsNullOrEmpty(name) && pixelsPerWorld > 0f;
            if (_root == null) return;

            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            if (!on) return;

            if (_text.text != name) { _text.text = name; _shadow.text = name; }

            _root.position = worldPosition;
            _root.localRotation = Quaternion.Euler(0f, 0f, rotationDeg);

            float s = HeightPx / pixelsPerWorld;
            _root.localScale = new Vector3(s, s, 1f);
        }

        public void Dispose()
        {
            // static 이벤트라 안 떼면 죽은 이름표가 계속 매달린다.
            Font.textureRebuilt -= OnFontRebuilt;

            if (_root != null) Object.Destroy(_root.gameObject);
        }
    }
}
