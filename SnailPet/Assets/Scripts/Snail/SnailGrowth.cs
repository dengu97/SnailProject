using System;
using System.Collections.Generic;
using SnailPet.Data;

namespace SnailPet.Snail
{
    /// <summary>
    /// 개체 하나의 성장 상태.
    ///
    /// 레벨업은 <b>기다리면 된다</b>. LevelData.LevelUpTime(초) 만큼 시간이 쌓이면 오른다.
    /// 잘 돌봐주면 그 시간이 빨리 간다 — 포만도·행복도가 LevelUpAdvantage 의 기준을
    /// 넘으면 Acceleration 만큼 진행이 가속된다. 방치형이므로 돌봄은 「필수」가 아니라
    /// 「단축」으로 작동한다.
    ///
    /// 데이터의 Speed / Size 는 단위 없는 추상값이라 픽셀로 바꾸는 환산 상수만 코드가 갖는다.
    /// </summary>
    public sealed class SnailGrowth
    {
        /// <summary>Speed 1 당 초속 픽셀. 현재 데이터(Speed 1~10.5)에서 24~252 px/s 가 된다.</summary>
        public const float PixelsPerSpeedUnit = 24f;

        /// <summary>Size 1 당 화면 가로 픽셀. 현재 데이터(Size 4~11)에서 80~220 px 가 된다.</summary>
        public const float PixelsPerSizeUnit = 20f;

        /// <summary>
        /// UseFullPointTime / UseHappyPointTime 마다 각각 1 씩 줄어든다.
        /// 레벨 1 기준 120초 · Need 10 이므로 가득 찬 상태에서 바닥까지 20분이다.
        /// 감소량을 레벨마다 다르게 할 일이 생기면 LevelData 에 열을 추가하면 된다.
        /// </summary>
        public const double DecayPerTick = 1.0;

        private static Dictionary<int, LevelDataRow> _byLevel;
        private static int _maxLevel;

        public int Level { get; private set; } = 1;
        public double FullPoint { get; private set; }
        public double HappyPoint { get; private set; }

        /// <summary>다음 레벨까지 쌓인 시간(초). 가속이 붙으면 실제 시간보다 빨리 찬다.</summary>
        public double LevelUpProgress { get; private set; }

        private double _fullDecayTimer;
        private double _happyDecayTimer;

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
        public LevelDataRow Next    => _byLevel.TryGetValue(Level + 1, out var r) ? r : null;

        public float PixelsPerSecond => (float)(Current.Speed * PixelsPerSpeedUnit);

        /// <summary>화면에 보일 달팽이 가로 크기(px).</summary>
        public float SizePixels => (float)(Current.Size * PixelsPerSizeUnit);

        public double FullPercent  => Current.NeedFullPoint  > 0 ? FullPoint  / Current.NeedFullPoint  : 1.0;
        public double HappyPercent => Current.NeedHappyPoint > 0 ? HappyPoint / Current.NeedHappyPoint : 1.0;

        /// <summary>
        /// 지금 조건으로 받는 가속. 기준을 만족하는 행 중 가장 좋은 것을 쓴다.
        /// 예: 둘 다 100% 이면 +50%, 둘 다 50% 이상이면 +20%.
        /// </summary>
        public double Acceleration
        {
            get
            {
                double best = 0.0;
                foreach (var a in GameData.LevelUpAdvantage)
                {
                    if (HappyPercent < a.NeedHappyPointPercent) continue;
                    if (FullPercent  < a.NeedFullPointPercent)  continue;
                    if (a.Acceleration > best) best = a.Acceleration;
                }
                return best;
            }
        }

        /// <summary>다음 레벨까지 남은 실제 시간(초). 지금 가속이 유지된다고 가정한 값.</summary>
        public double SecondsToNextLevel
        {
            get
            {
                var next = Next;
                if (next == null) return 0;
                double remain = Math.Max(0, next.LevelUpTime - LevelUpProgress);
                return remain / (1.0 + Acceleration);
            }
        }

        public double LevelUpRatio
        {
            get
            {
                var next = Next;
                if (next == null || next.LevelUpTime <= 0) return 1.0;
                return Math.Min(1.0, LevelUpProgress / next.LevelUpTime);
            }
        }

        /// <summary>테스트·데모용. 조건을 무시하고 레벨만 바꾼다.</summary>
        public void ForceLevel(int level)
        {
            EnsureIndex();
            Level = UnityEngine.Mathf.Clamp(level, 1, _maxLevel);
            LevelUpProgress = 0;
            FullPoint = Current.NeedFullPoint;
            HappyPoint = Current.NeedHappyPoint;
        }

        /// <summary>
        /// 먹이 섭취. 요구치를 넘겨 쌓아둘 수는 없다고 본다 —
        /// 넘치게 먹여 시간을 저금하는 플레이를 막고, 가속 기준의 100% 가 상한이 된다.
        /// 다른 의도라면 상한을 데이터로 빼면 된다.
        /// </summary>
        public void Feed(double fullPoint, double happyPoint)
        {
            FullPoint  = Math.Min(Current.NeedFullPoint,  FullPoint  + fullPoint);
            HappyPoint = Math.Min(Current.NeedHappyPoint, HappyPoint + happyPoint);
        }

        /// <summary>시간 경과. timeScale 을 올리면 데모에서 몇 시간치를 몇 초로 볼 수 있다.</summary>
        public bool Tick(float deltaSeconds, float timeScale = 1f)
        {
            double dt = deltaSeconds * timeScale;

            // 포만도·행복도 감소. 각자의 주기마다 1 씩 줄어든다.
            FullPoint  = Decay(FullPoint,  Current.UseFullPointTime,  dt, ref _fullDecayTimer);
            HappyPoint = Decay(HappyPoint, Current.UseHappyPointTime, dt, ref _happyDecayTimer);

            // 레벨업 시간 누적 (돌봄 상태에 따라 가속)
            var next = Next;
            if (next == null) return false;

            LevelUpProgress += dt * (1.0 + Acceleration);
            if (LevelUpProgress < next.LevelUpTime) return false;

            Level++;
            LevelUpProgress = 0;
            // 요구치가 올라가므로 비율이 떨어진다. 값 자체는 유지해 계속 돌봐야 하게 둔다.
            FullPoint  = Math.Min(FullPoint,  Current.NeedFullPoint);
            HappyPoint = Math.Min(HappyPoint, Current.NeedHappyPoint);
            return true;
        }

        /// <summary>주기마다 1 씩 깎는다. 한 프레임에 여러 주기가 지나도 그만큼 처리한다.</summary>
        private static double Decay(double value, double interval, double dt, ref double timer)
        {
            if (interval <= 0 || value <= 0) return value;

            timer += dt;
            while (timer >= interval)
            {
                timer -= interval;
                value = Math.Max(0, value - DecayPerTick);
                if (value <= 0) break;
            }
            return value;
        }

        public override string ToString() =>
            $"Lv.{Level} 속도 {Current.Speed}({PixelsPerSecond:0}px/s) 크기 {Current.Size}({SizePixels:0}px) " +
            $"포만 {FullPoint:0}/{Current.NeedFullPoint:0} 행복 {HappyPoint:0}/{Current.NeedHappyPoint:0} " +
            $"성장 {LevelUpRatio * 100:0}% (가속 +{Acceleration * 100:0}%)";
    }
}
