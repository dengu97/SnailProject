using System.Collections.Generic;

namespace SnailPet.Data
{
    /// <summary>
    /// EnumData 시트에서 enum 값에 딸린 표시 정보를 꺼낸다.
    ///
    /// enum 자체는 생성된 C# 타입이지만, 화면에 보일 아이콘 같은 것은 시트에 있다.
    /// 등급을 글자가 아니라 아이콘으로 보여 주기로 해서 이 조회가 필요해졌다.
    /// </summary>
    public static class Enums
    {
        private static Dictionary<string, string> _icon;
        private static Dictionary<string, string> _slot;

        private static string Key(string enumType, string enumName) => enumType + "." + enumName;

        private static void EnsureIndex()
        {
            if (_icon != null) return;
            _icon = new Dictionary<string, string>();
            _slot = new Dictionary<string, string>();

            foreach (var e in GameData.EnumData)
            {
                if (!string.IsNullOrEmpty(e.IconResourceKey))
                    _icon[Key(e.EnumType, e.EnumName)] = e.IconResourceKey;
                if (!string.IsNullOrEmpty(e.SlotResourceKey))
                    _slot[Key(e.EnumType, e.EnumName)] = e.SlotResourceKey;
            }
        }

        /// <summary>
        /// 이 enum 값의 아이콘 리소스 키. 시트가 비어 있으면 null.
        /// 부르는 쪽은 null 을 「아직 아트가 없다」로 다루면 된다.
        /// </summary>
        public static string IconOf<T>(T value) where T : System.Enum
        {
            EnsureIndex();
            return _icon.TryGetValue(Key(typeof(T).Name, value.ToString()), out string key) ? key : null;
        }

        /// <summary>
        /// 이 enum 값의 칸 그림 리소스 키(등급별 칸 색). 시트가 비어 있으면 null 이고,
        /// 부르는 쪽은 기본 칸을 쓰면 된다.
        /// </summary>
        public static string SlotOf<T>(T value) where T : System.Enum
        {
            EnsureIndex();
            return _slot.TryGetValue(Key(typeof(T).Name, value.ToString()), out string key) ? key : null;
        }

        public static void ClearCache() { _icon = null; _slot = null; }
    }
}
