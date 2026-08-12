using UnityEngine;

namespace SnailPet.Data
{
    /// <summary>
    /// GameConfig 시트의 값 하나하나에 이름으로 닿는다.
    ///
    /// 시트가 한 줄짜리라 생성 코드에서는 <c>GameData.GameConfig[0]</c> 이 되는데,
    /// 그대로 쓰면 부르는 곳마다 [0] 이 붙고 시트가 비었을 때 터진다.
    /// 여기 한 번만 감싸 두고 값이 없으면 코드의 기본값으로 버틴다 —
    /// 상수가 시트로 옮겨 갔다고 게임이 안 뜨는 것은 곤란하다.
    /// </summary>
    public static class Config
    {
        private static GameConfigRow Row =>
            GameData.GameConfig != null && GameData.GameConfig.Length > 0 ? GameData.GameConfig[0] : null;

        private static bool _warned;

        private static double Of(double fromSheet, double fallback, string name)
        {
            if (Row != null) return fromSheet;
            if (!_warned)
            {
                _warned = true;
                Debug.LogWarning("[SnailPet] GameConfig 시트가 비어 있어 코드 기본값을 씁니다. (" + name + " 등)");
            }
            return fallback;
        }

        /// <summary>돌연변이가 부모 가중치와 겨루는 힘. 부모 파츠의 AppearWeight 와 같은 저울에 올린다.</summary>
        public static int MutationWeight => Row?.MutationWeight ?? 5;

        /// <summary>포만도가 한 주기마다 깎이는 양. 주기는 LevelData.UseFullPointTime.</summary>
        public static double FullDecayPerTick => Of(Row?.FullDecayPerTick ?? 0, 1.0, nameof(FullDecayPerTick));

        /// <summary>행복도가 한 주기마다 깎이는 양. 주기는 LevelData.UseHappyPointTime.</summary>
        public static double HappyDecayPerTick => Of(Row?.HappyDecayPerTick ?? 0, 1.0, nameof(HappyDecayPerTick));

        /// <summary>LevelData.Speed 1 당 초속 픽셀.</summary>
        public static float PixelsPerSpeed => (float)Of(Row?.PixelsPerSpeed ?? 0, 24.0, nameof(PixelsPerSpeed));

        /// <summary>LevelData.Size 1 당 화면 가로 픽셀.</summary>
        public static float PixelsPerSize => (float)Of(Row?.PixelsPerSize ?? 0, 20.0, nameof(PixelsPerSize));

        /// <summary>먹이 낙하 가속도(px/s^2).</summary>
        public static float FoodGravity => (float)Of(Row?.FoodGravity ?? 0, 1600.0, nameof(FoodGravity));
    }
}
