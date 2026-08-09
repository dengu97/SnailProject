using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>달팽이 한 부위. 색상을 쓰지 않는 파츠(눈 등)는 ColorKey 가 null.</summary>
    public struct SnailPartRef
    {
        public PartsType Type;
        public string ResourceKey;
        public string ColorKey;

        public override string ToString() =>
            ColorKey == null ? ResourceKey : ResourceKey + " (" + ColorKey + ")";
    }

    /// <summary>개체 하나의 외형. 부화 결과이자 합성의 입력.</summary>
    public sealed class SnailAppearance
    {
        public readonly List<SnailPartRef> Parts = new List<SnailPartRef>();

        /// <summary>몸통은 발선 계산의 기준이라 따로 찾을 일이 많다.</summary>
        public bool TryGetBody(out SnailPartRef body)
        {
            foreach (var p in Parts)
                if (p.Type == PartsType.Body) { body = p; return true; }
            body = default;
            return false;
        }

        public override string ToString() => string.Join(", ", Parts);
    }

    /// <summary>
    /// 레이어 순서는 EnumData.SortOrder 에서 읽는다 (오름차순이 뒤 → 앞).
    /// 값이 없으면 EnumValue 로 떨어지지만, 그건 직렬화 신원이라 순서와 무관하므로
    /// 정상 상태가 아니다. 파이프라인 검증에서 걸러진다.
    /// </summary>
    public static class PartsLayer
    {
        private static Dictionary<PartsType, int> _order;

        public static int SortOrderOf(PartsType type)
        {
            if (_order == null)
            {
                _order = new Dictionary<PartsType, int>();
                foreach (var e in GameData.EnumData)
                {
                    if (e.EnumType != "PartsType") continue;
                    if (!System.Enum.TryParse(e.EnumName, out PartsType t)) continue;
                    _order[t] = e.SortOrder ?? e.EnumValue;
                }
            }
            return _order.TryGetValue(type, out int v) ? v : 0;
        }
    }
}
