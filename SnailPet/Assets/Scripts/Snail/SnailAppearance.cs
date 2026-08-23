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

        /// <summary>
        /// 악세서리면 그 부위, 타고난 파츠면 null.
        ///
        /// 악세서리는 <see cref="PartsType"/> 이 아니라 <see cref="AccessoriesType"/> 인데,
        /// SortOrder 는 같은 숫자 축을 쓴다 (모자 250 · 가방 260 이 몸통 200 과 더듬이 300 사이).
        /// 그래서 그리는 쪽에서는 둘을 구분할 필요가 없고, 아래 세 값만 보면 된다.
        /// </summary>
        public AccessoriesType? Accessory;

        public int SortOrder => Accessory.HasValue
            ? PartsLayer.SortOrderOf(Accessory.Value) : PartsLayer.SortOrderOf(Type);

        public int DeformGroup => Accessory.HasValue
            ? PartsLayer.DeformGroupOf(Accessory.Value) : PartsLayer.DeformGroupOf(Type);

        /// <summary>아트가 있는 폴더. 악세서리는 부위별로 나뉘어 있지 않고 한 곳에 모여 있다.</summary>
        public string Folder => Accessory.HasValue ? "Accessories" : Type.ToString();

        public override string ToString() =>
            ColorKey == null ? ResourceKey : ResourceKey + " (" + ColorKey + ")";
    }

    /// <summary>파츠 데이터를 되살릴 때 쓰는 잣대.</summary>
    public static class SnailParts
    {
        /// <summary>
        /// 되살릴 색. <b>그 파츠가 지금도 쓰는 색일 때만</b> 살린다.
        ///
        /// 색은 세이브와 남의 카드에 <b>이름으로</b> 적혀 있어서, 아트와 시트에서 그 색이
        /// 빠져도 글자는 그대로 남는다. 그러면 없는 그림을 찾다가 그 부위가 비어 보인다.
        /// 데이터에 없는 색은 「색 안 씀」으로 돌려 선화만 그리게 한다.
        /// </summary>
        public static string KeepColor(PartsDataRow row, string color)
        {
            if (row == null || string.IsNullOrEmpty(color)) return null;
            if (!row.IsUseColor || row.Colors == null) return null;

            foreach (var c in row.Colors)
                if (c == color) return color;

            return null;
        }
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

        // 악세서리도 같은 숫자 축을 쓴다. 표만 따로 둔다.
        private static Dictionary<AccessoriesType, int> _accOrder;
        private static Dictionary<AccessoriesType, int> _accDeform;

        /// <summary>DeformGroup 이 비어 있는 파츠. 변형되지 않는 강체다.</summary>
        public const int RigidGroup = 0;

        private static void EnsureIndex()
        {
            if (_order != null) return;
            _order = new Dictionary<PartsType, int>();
            _deform = new Dictionary<PartsType, int>();

            _accOrder = new Dictionary<AccessoriesType, int>();
            _accDeform = new Dictionary<AccessoriesType, int>();

            foreach (var e in GameData.EnumData)
            {
                if (e.EnumType == "PartsType" && System.Enum.TryParse(e.EnumName, out PartsType t))
                {
                    _order[t] = e.SortOrder ?? e.EnumValue;
                    _deform[t] = e.DeformGroup ?? RigidGroup;
                }
                else if (e.EnumType == "AccessoriesType" && System.Enum.TryParse(e.EnumName, out AccessoriesType a))
                {
                    _accOrder[a] = e.SortOrder ?? e.EnumValue;
                    _accDeform[a] = e.DeformGroup ?? RigidGroup;
                }
            }
        }

        public static int SortOrderOf(AccessoriesType type)
        {
            EnsureIndex();
            return _accOrder.TryGetValue(type, out int v) ? v : 0;
        }

        /// <summary>악세서리는 지금 전부 강체다. 몸이 늘어나도 모자는 안 늘어난다.</summary>
        public static int DeformGroupOf(AccessoriesType type)
        {
            EnsureIndex();
            return _accDeform.TryGetValue(type, out int v) ? v : RigidGroup;
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
