using System.Collections.Generic;
using System.Globalization;
using SnailPet.Data;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 개체 하나에 걸린 버프.
    ///
    /// 규칙:
    ///  · <b>중첩되지 않는다.</b> 같은 BuffType 은 하나만 유지된다.
    ///  · <b>조건을 벗어났다가 다시 달성하면 갱신된다.</b> 계속 조건을 만족하고 있는 동안에는
    ///    다시 걸리지 않는다. 그래서 조건 판정은 「지금 만족하는가」가 아니라
    ///    「방금 만족하기 시작했는가」(상승 에지) 로 해야 한다.
    /// </summary>
    public sealed class SnailBuffs
    {
        private struct Active
        {
            public int Id;
            public double Remaining;   // 초
            public double Duration;
            public int Generation;     // 갱신될 때마다 증가. 재적용을 로그로 구분하기 위한 것.
        }

        private readonly Dictionary<BuffType, Active> _active = new Dictionary<BuffType, Active>();

        public bool IsActive(BuffType type) => _active.ContainsKey(type);

        public double Remaining(BuffType type) =>
            _active.TryGetValue(type, out var a) ? a.Remaining : 0.0;

        public int Count => _active.Count;

        /// <summary>
        /// 지금 걸린 버프의 종류만. 남은 시간은 빼고 <b>갱신 횟수</b>를 붙인다.
        /// 이 문자열이 바뀌는 순간이 곧 「걸림 / 갱신 / 풀림」이라, 변화만 로그로 남기기 좋다.
        /// (남은 시간을 넣으면 매 프레임 달라져 로그가 도배된다)
        /// </summary>
        public string Signature
        {
            get
            {
                if (_active.Count == 0) return "없음";
                var sb = new System.Text.StringBuilder();
                foreach (var kv in _active)
                {
                    if (sb.Length > 0) sb.Append('+');
                    sb.Append(kv.Key).Append('#').Append(kv.Value.Generation);
                }
                return sb.ToString();
            }
        }

        /// <summary>버프를 건다. 이미 같은 타입이 걸려 있으면 남은 시간을 다시 채운다.</summary>
        public bool Apply(int buffId)
        {
            if (buffId <= 0) return false;
            if (!GameData.BuffDataById.TryGetValue(buffId, out var data))
            {
                Debug.LogWarning("[SnailPet] 버프 데이터를 찾을 수 없습니다: " + buffId);
                return false;
            }

            // Value1 은 string 으로 선언돼 있어 파싱해서 쓴다 (지속시간, 초)
            if (!double.TryParse(data.Value1, NumberStyles.Any, CultureInfo.InvariantCulture, out double duration))
            {
                Debug.LogWarning($"[SnailPet] 버프 {buffId} 의 Value1 <{data.Value1}> 을 지속시간으로 읽을 수 없습니다.");
                return false;
            }

            // 중첩되지 않는다. 같은 타입이 이미 있으면 남은 시간을 다시 채우고 세대만 올린다.
            int gen = _active.TryGetValue(data.BuffType, out var prev) ? prev.Generation + 1 : 1;
            _active[data.BuffType] = new Active
            {
                Id = buffId, Remaining = duration, Duration = duration, Generation = gen
            };
            return true;
        }

        /// <summary>토큰 문자열로 거는 경로. FoodData.BuffId 가 string 이라 필요하다.</summary>
        public bool ApplyToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (!GameData.IdByToken.TryGetValue(token, out int id))
            {
                Debug.LogWarning("[SnailPet] 알 수 없는 버프 토큰: " + token);
                return false;
            }
            return Apply(id);
        }

        public void Tick(double deltaSeconds)
        {
            if (_active.Count == 0) return;

            List<BuffType> expired = null;
            var keys = new List<BuffType>(_active.Keys);
            foreach (var k in keys)
            {
                var a = _active[k];
                a.Remaining -= deltaSeconds;
                if (a.Remaining <= 0)
                {
                    (expired ??= new List<BuffType>()).Add(k);
                    continue;
                }
                _active[k] = a;
            }
            if (expired != null)
                foreach (var k in expired) _active.Remove(k);
        }

        public override string ToString()
        {
            if (_active.Count == 0) return "버프 없음";
            var sb = new System.Text.StringBuilder();
            foreach (var kv in _active)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{kv.Key} {kv.Value.Remaining:0}초");
            }
            return sb.ToString();
        }
    }
}
