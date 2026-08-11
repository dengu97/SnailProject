using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>달팽이 한 부위. 색상을 쓰지 않는 파츠(눈 등)는 ColorKey 가 null.</summary>
    public struct SnailPartRef
    {
        /// <summary>
        /// PartsData 의 행 Id. 세이브에 적히는 신원이라 ResourceKey 대신 이것을 쓴다 —
        /// 아트 파일 이름은 바뀔 수 있지만 IdMap 의 번호는 안 바뀐다.
        /// </summary>
        public int PartsId;

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
        private static Dictionary<PartsType, int> _deform;

        /// <summary>DeformGroup 이 비어 있는 파츠. 변형되지 않는 강체다.</summary>
        public const int RigidGroup = 0;

        private static void EnsureIndex()
        {
            if (_order != null) return;
            _order = new Dictionary<PartsType, int>();
            _deform = new Dictionary<PartsType, int>();

            foreach (var e in GameData.EnumData)
            {
                if (e.EnumType != "PartsType") continue;
                if (!System.Enum.TryParse(e.EnumName, out PartsType t)) continue;
                _order[t] = e.SortOrder ?? e.EnumValue;
                _deform[t] = e.DeformGroup ?? RigidGroup;
            }
        }

        public static int SortOrderOf(PartsType type)
        {
            EnsureIndex();
            return _order.TryGetValue(type, out int v) ? v : 0;
        }

        /// <summary>
        /// 이 파츠가 어느 변형 그룹을 따르는가. 0 이면 강체.
        /// 같은 그룹의 파츠는 하나의 스켈레톤에 스키닝되어 함께 출렁이고,
        /// 강체는 뼈에 매달려 위치·회전만 따라간다 (껍질·악세서리).
        /// </summary>
        public static int DeformGroupOf(PartsType type)
        {
            EnsureIndex();
            return _deform.TryGetValue(type, out int v) ? v : RigidGroup;
        }
    }
}
