using System.Collections.Generic;
using SnailPet.Data;
using SnailPet.Desktop;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 화면에 놓인 먹이 하나. 좌표는 가상 화면 px 이고, ScreenY 는 <b>먹이의 발밑</b>이다.
    /// 어디에 놓아도 중력으로 바닥까지 떨어진 뒤 그 자리에 머문다.
    /// </summary>
    public sealed class FoodItem
    {
        public FoodDataRow Data;
        public Transform Root;

        public float ScreenX;
        public float ScreenY;      // 먹이 바닥면의 y
        public float VelocityY;
        public bool  Landed;
        public bool  Eaten;

        /// <summary>유저가 집고 있는 동안은 중력을 받지 않고 달팽이도 먹으러 오지 않는다.</summary>
        public bool  Held;

        /// <summary>보이는 부분의 세로 크기(px). 집기 판정에 쓴다.</summary>
        public float Height;

        /// <summary>보이는 부분의 크기(px). 달팽이가 얼마나 가까이 와야 먹는지 판정에 쓴다.</summary>
        public float HalfWidth;

        /// <summary>떨어뜨릴 때 정한 배율. 착지 뽀잉은 이 값을 기준으로 눌렀다 편다.</summary>
        public float BaseScale = 1f;

        /// <summary>지금 세로로 눌린 정도(1 = 평소). 바닥면을 붙여 두려면 자리 잡을 때 곱해야 한다.</summary>
        public float SquashY = 1f;

        /// <summary>착지 후 지난 시간. 음수면 안 튀는 중이다.</summary>
        public float BounceAge = -1f;

        public override string ToString() => (Data != null ? SnailPet.Data.Loc.ById(Data.NameId) : "?") + (Landed ? " (착지)" : " (낙하 중)");
    }

    /// <summary>
    /// 먹이를 떨어뜨리고 관리한다.
    ///
    /// 기획서상 PC 는 화면 어디든 놓을 수 있지만, 중력이 있으므로 최종 위치는 항상 바닥이다.
    /// 그래서 달팽이 입장에서 먹이는 언제나 「아래 벽 위의 한 점」이고,
    /// 둘레 좌표 하나로 목표를 표현할 수 있다.
    /// </summary>
    public sealed class FoodField
    {
        /// <summary>낙하 가속도(px/s^2). GameConfig 시트가 소유한다.</summary>
        public static float Gravity => SnailPet.Data.Config.FoodGravity;

        /// <summary>화면에 보일 먹이 가로 크기(px).</summary>
        public const float FoodPixels = 64f;

        private readonly List<FoodItem> _items = new List<FoodItem>();
        private readonly Transform _parent;

        public FoodField(Transform parent) { _parent = parent; }

        public IReadOnlyList<FoodItem> Items => _items;
        public int Count => _items.Count;

        /// <summary>화면 어느 지점에 놓아도 아래로 떨어진다.</summary>
        public FoodItem Drop(FoodDataRow data, float screenX, float screenY)
        {
            if (data == null || string.IsNullOrEmpty(data.ResourceKey))
            {
                Debug.LogWarning("[SnailPet] 먹이에 ResourceKey 가 없어 표시할 수 없습니다: " +
                                 (data != null ? SnailPet.Data.Loc.ById(data.NameId) : "null"));
                return null;
            }

            var sprite = SnailComposer.Load(SnailComposer.ResourceRoot + "/Food/" + data.ResourceKey);
            if (sprite == null) return null;

            var go = new GameObject("Food_" + data.ResourceKey);
            go.transform.SetParent(_parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;                   // 달팽이 뒤에 깔린다

            // 보이는 부분 기준으로 크기를 맞추고, 바닥면이 ScreenY 에 오도록 배치할 값을 구한다
            float scale = 1f, bottomOffset = 0f, halfWidth = FoodPixels * 0.5f;
            if (SnailMetrics.TryMeasure(sprite, out var e) && e.Width > 0.01f)
            {
                scale = FoodPixels / e.Width;
                bottomOffset = e.Bottom * scale;      // 음수: 루트에서 바닥까지
                halfWidth = e.Width * scale * 0.5f;
            }
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var item = new FoodItem
            {
                Data = data,
                Root = go.transform,
                ScreenX = screenX,
                ScreenY = screenY,
                HalfWidth = halfWidth,
                Height = (SnailMetrics.TryMeasure(sprite, out var e2) ? e2.Height * scale : FoodPixels),
                BaseScale = scale,
            };
            item.Root.SetSiblingIndex(0);
            _items.Add(item);
            _bottomOffset[item] = bottomOffset;
            return item;
        }

        private readonly Dictionary<FoodItem, float> _bottomOffset = new Dictionary<FoodItem, float>();

        /// <summary>루트에서 먹이 바닥면까지의 오프셋(월드 단위, 보통 음수).</summary>
        public float BottomOffsetOf(FoodItem item) =>
            _bottomOffset.TryGetValue(item, out float v) ? v : 0f;

        /// <summary>중력 적용. floorY 는 박스 아래 벽의 화면 y.</summary>
        /// <summary>착지할 때 눌렸다 펴지는 시간과 세기.</summary>
        private const float BounceSeconds = 0.32f, BounceAmount = 0.28f;

        public void Tick(float deltaSeconds, float floorY)
        {
            foreach (var f in _items)
            {
                if (f.Eaten) continue;
                if (f.Held) { f.VelocityY = 0f; f.Landed = false; continue; }   // 들고 있으면 안 떨어진다

                if (f.ScreenY < floorY)
                {
                    f.VelocityY += Gravity * deltaSeconds;
                    f.ScreenY += f.VelocityY * deltaSeconds;
                    f.Landed = false;
                }
                if (f.ScreenY >= floorY)
                {
                    // 떨어지던 것이 방금 닿았으면 그 순간부터 뽀잉이 시작된다
                    if (!f.Landed) f.BounceAge = 0f;

                    f.ScreenY = floorY;      // 바닥에 닿으면 멈춘다. 튕기지 않는다.
                    f.VelocityY = 0f;
                    f.Landed = true;
                }

                StepBounce(f, deltaSeconds);
            }
        }

        /// <summary>
        /// 착지 뽀잉. 세로로 눌리면 가로로 퍼지고, 감쇠 진동으로 제자리에 붙는다.
        ///
        /// 바닥면이 뜨지 않게 세로 배율을 <see cref="FoodItem.SquashY"/> 로 남긴다 —
        /// 자리를 잡는 쪽이 바닥까지의 거리를 그만큼 줄여야 한다.
        /// </summary>
        private static void StepBounce(FoodItem f, float deltaSeconds)
        {
            if (f.BounceAge < 0f || f.Root == null) return;

            f.BounceAge += deltaSeconds;

            float t = f.BounceAge / BounceSeconds;
            if (t >= 1f)
            {
                f.BounceAge = -1f;
                f.SquashY = 1f;
                f.Root.localScale = new Vector3(f.BaseScale, f.BaseScale, 1f);
                return;
            }

            float k = Mathf.Sin(t * Mathf.PI * 4f) * BounceAmount * Mathf.Exp(-t * 5f);
            f.SquashY = 1f - k;
            f.Root.localScale = new Vector3(f.BaseScale * (1f + k), f.BaseScale * f.SquashY, 1f);
        }

        /// <summary>착지해서 먹을 수 있는 것 중 둘레 거리가 가장 가까운 먹이.</summary>
        public FoodItem FindNearestLanded(ScreenRect box, float fromPerimeter, out float delta)
        {
            FoodItem best = null;
            delta = 0f;
            float bestDist = float.MaxValue;

            foreach (var f in _items)
            {
                if (f.Eaten || !f.Landed || f.Held) continue;
                float p = BoxWalk.BottomXToPerimeter(box, f.ScreenX);
                float d = BoxWalk.ShortestDelta(box, fromPerimeter, p);
                if (Mathf.Abs(d) < bestDist) { bestDist = Mathf.Abs(d); best = f; delta = d; }
            }
            return best;
        }

        /// <summary>커서 아래에 있는 먹이. 나중에 놓인 것부터 찾아 위에 있는 것이 먼저 잡힌다.</summary>
        public FoodItem FindAt(float screenX, float screenY)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var f = _items[i];
                if (f.Eaten) continue;
                if (Mathf.Abs(screenX - f.ScreenX) > f.HalfWidth) continue;
                if (screenY > f.ScreenY || screenY < f.ScreenY - f.Height) continue;
                return f;
            }
            return null;
        }

        public void Consume(FoodItem item)
        {
            if (item == null || item.Eaten) return;
            item.Eaten = true;
            if (item.Root != null) Object.Destroy(item.Root.gameObject);
            _items.Remove(item);
            _bottomOffset.Remove(item);
        }
    }
}
