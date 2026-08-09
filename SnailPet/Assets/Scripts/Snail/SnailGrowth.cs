using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 개체 하나의 성장 상태. LevelData 가 레벨마다 요구치와 능력치를 들고 있다.
    ///
    /// 데이터의 Speed / Size 는 단위 없는 추상값(현재 레벨과 같은 1~20)이라,
    /// 실제 픽셀로 바꾸는 환산 상수를 여기 한 곳에 둔다. 곡선은 데이터가 갖고,
    /// 코드는 배율만 갖는 구조다.
    /// </summary>
    public sealed class SnailGrowth
    {
        /// <summary>Speed 1 당 초속 픽셀. 현재 데이터(Speed 1~10.5)에서 24~252 px/s 가 된다.</summary>
        public const float PixelsPerSpeedUnit = 24f;

        /// <summary>Size 1 당 화면 가로 픽셀. 현재 데이터(Size 4~11)에서 80~220 px 가 된다.</summary>
        public const float PixelsPerSizeUnit = 20f;

        /// <summary>
        /// UseFullPointTime 마다 포만도가 얼마나 줄어드는지는 데이터에 없다. 1 로 가정한다.
        /// 레벨 1 기준 UseFullPointTime 120초 · NeedFullPoint 10 이므로
        /// 가득 찬 상태에서 바닥까지 10회 = 20분이 걸린다.
        /// 데이터로 옮길 값이면 LevelData 에 열을 추가하면 된다.
        /// </summary>
        public const double FullPointDecayPerTick = 1.0;

        private static Dictionary<int, LevelDataRow> _byLevel;
        private static int _maxLevel;

        public int Level { get; private set; } = 1;
        public double Exp { get; private set; }
        public double FullPoint { get; private set; }
        public double HappyPoint { get; private set; }

        private double _decayTimer;

        public SnailGrowth()
        {
            EnsureIndex();
            FullPoint = Current.NeedFullPoint;
            HappyPoint = Current.NeedHappyPoint;
        }

        private static void EnsureIndex()
        {
            if (_byLevel != null) return;
            _byLevel = new Dictionary<int, LevelDataRow>(GameData.LevelData.Length);
            foreach (var row in GameData.LevelData)
            {
                _byLevel[row.Level] = row;
                if (row.Level > _maxLevel) _maxLevel = row.Level;
            }
        }

        public static int MaxLevel { get { EnsureIndex(); return _maxLevel; } }

        public LevelDataRow Current => _byLevel.TryGetValue(Level, out var r) ? r : GameData.LevelData[0];

        public LevelDataRow Next => _byLevel.TryGetValue(Level + 1, out var r) ? r : null;

        public float PixelsPerSecond => (float)(Current.Speed * PixelsPerSpeedUnit);

        /// <summary>화면에 보일 달팽이 가로 크기(px).</summary>
        public float SizePixels => (float)(Current.Size * PixelsPerSizeUnit);

        /// <summary>
        /// 다음 레벨의 요구치를 모두 채웠는가.
        /// RequiredExp 는 「그 레벨이 되기 위해」 필요한 경험치로 읽는다 (레벨 1 이 0).
        /// </summary>
        public bool CanLevelUp
        {
            get
            {
                var next = Next;
                return next != null
                    && Exp >= next.RequiredExp
                    && FullPoint >= Current.NeedFullPoint
                    && HappyPoint >= Current.NeedHappyPoint;
            }
        }

        public bool TryLevelUp()
        {
            if (!CanLevelUp) return false;
            Level++;
            return true;
        }

        /// <summary>테스트·데모용. 요구치를 무시하고 레벨만 바꾼다.</summary>
        public void ForceLevel(int level)
        {
            EnsureIndex();
            Level = UnityEngine.Mathf.Clamp(level, 1, _maxLevel);
            FullPoint = Current.NeedFullPoint;
            HappyPoint = Current.NeedHappyPoint;
        }

        public void AddExp(double amount)
        {
            if (amount <= 0) return;
            Exp += amount;
            while (TryLevelUp()) { }
        }

        public void Feed(double fullPoint, double happyPoint)
        {
            FullPoint += fullPoint;
            HappyPoint += happyPoint;
        }

        /// <summary>시간 경과. timeScale 을 올리면 데모에서 몇 시간치를 몇 초로 볼 수 있다.</summary>
        public void Tick(float deltaSeconds, float timeScale = 1f)
        {
            double interval = Current.UseFullPointTime;
            if (interval <= 0) return;

            _decayTimer += deltaSeconds * timeScale;
            while (_decayTimer >= interval)
            {
                _decayTimer -= interval;
                FullPoint = System.Math.Max(0, FullPoint - FullPointDecayPerTick);
            }
        }

        public override string ToString() =>
            $"Lv.{Level} 속도 {Current.Speed}({PixelsPerSecond:0}px/s) 크기 {Current.Size}({SizePixels:0}px) " +
            $"포만 {FullPoint:0}/{Current.NeedFullPoint:0} 행복 {HappyPoint:0}/{Current.NeedHappyPoint:0}";
    }
}
