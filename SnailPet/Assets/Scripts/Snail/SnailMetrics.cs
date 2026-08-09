using System.Collections.Generic;
using SnailPet.Data;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>합성된 달팽이의 접지·경계 정보. 전부 루트 기준 로컬 좌표(월드 단위).</summary>
    public struct SnailBounds
    {
        public float Left, Right;   // 합성 전체의 가로 끝
        public float Foot;          // 몸통이 지면에 닿는 높이 (보통 음수)
        public float Top;           // 합성 전체의 위쪽 끝
        public bool  Measured;
    }

    /// <summary>
    /// 달팽이를 창 테두리에 올려놓으려면 「발이 어디인가」를 알아야 한다.
    ///
    /// 발선은 <b>몸통 레이어만</b> 재고, 가로 경계는 <b>합성 전체</b>를 잰다.
    ///  · 세로를 전체로 재면 점액이나 아래로 늘어지는 가방이 최하단이 되어 달팽이가 뜬다.
    ///  · 가로를 몸통만 재면 더듬이가 화면 끝에서 잘린다 (더듬이가 몸통보다 바깥으로 나온다).
    ///
    /// 측정 결과는 스프라이트 단위로 캐시한다. 같은 파츠를 쓰는 개체가 많아도 비용이 고정된다.
    /// </summary>
    public static class SnailMetrics
    {
        private struct Extents { public float Left, Right, Bottom, Top; }

        private static readonly Dictionary<Sprite, Extents> _cache = new Dictionary<Sprite, Extents>();

        public static SnailBounds Measure(SnailAppearance appearance)
        {
            var result = new SnailBounds { Left = float.MaxValue, Right = float.MinValue, Top = float.MinValue };
            bool anyPart = false, footFound = false;

            foreach (var p in appearance.Parts)
            {
                // 가로·위쪽은 색상 레이어까지 포함해도 선화와 실루엣이 같으므로 선화만 보면 된다
                var sprite = SnailComposer.Load(SnailComposer.LinePath(p.Type, p.ResourceKey));
                if (sprite == null) continue;
                if (!TryGetExtents(sprite, out var e)) continue;

                anyPart = true;
                if (e.Left  < result.Left)  result.Left  = e.Left;
                if (e.Right > result.Right) result.Right = e.Right;
                if (e.Top   > result.Top)   result.Top   = e.Top;

                if (p.Type == PartsType.Body) { result.Foot = e.Bottom; footFound = true; }
            }

            if (!anyPart) return result;

            if (!footFound)
            {
                // 몸통이 없는 개체는 정상 데이터가 아니지만, 그렇다고 화면 밖으로 보낼 이유는 없다
                Debug.LogWarning("[SnailPet] 몸통 파츠가 없어 발선을 잴 수 없습니다. 합성 전체의 최하단을 씁니다.");
                float lowest = float.MaxValue;
                foreach (var p in appearance.Parts)
                {
                    var sprite = SnailComposer.Load(SnailComposer.LinePath(p.Type, p.ResourceKey));
                    if (sprite != null && TryGetExtents(sprite, out var e) && e.Bottom < lowest) lowest = e.Bottom;
                }
                result.Foot = lowest == float.MaxValue ? 0f : lowest;
            }

            result.Measured = true;
            return result;
        }

        /// <summary>스프라이트의 불투명 영역을 피벗 기준 월드 단위로 돌려준다.</summary>
        private static bool TryGetExtents(Sprite sprite, out Extents e)
        {
            if (_cache.TryGetValue(sprite, out e)) return true;
            e = default;

            var tex = sprite.texture;
            if (tex == null) return false;

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException)
            {
                // Read/Write 가 꺼져 있으면 여기서 막힌다. SnailArtImporter 가 켜 주지만
                // 임포트 설정이 어긋나면 조용히 틀린 값을 쓰는 것보다 알리는 게 낫다.
                Debug.LogWarning($"[SnailPet] {tex.name} 의 픽셀을 읽을 수 없습니다. " +
                                 "Read/Write Enabled 를 켜거나 메뉴 SnailPet > 4. 아트 리임포트 를 실행하세요.");
                return false;
            }

            int w = tex.width, h = tex.height;
            int minX = w, maxX = -1, minY = h, maxY = -1;

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (px[row + x].a <= 8) continue;       // 거의 투명한 픽셀은 몸이 아니다
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return false;                     // 전부 투명

            // 피벗 기준 오프셋. 피벗 설정이 무엇이든 맞도록 sprite.pivot 을 직접 쓴다.
            float ppu = sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot;                   // 스프라이트 rect 안에서의 픽셀 좌표
            e = new Extents
            {
                Left   = (minX     - pivot.x) / ppu,
                Right  = (maxX + 1 - pivot.x) / ppu,
                Bottom = (minY     - pivot.y) / ppu,
                Top    = (maxY + 1 - pivot.y) / ppu,
            };
            _cache[sprite] = e;
            return true;
        }

        public static void ClearCache() => _cache.Clear();
    }
}
