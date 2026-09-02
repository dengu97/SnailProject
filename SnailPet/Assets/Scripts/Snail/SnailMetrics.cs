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
        /// <summary>스프라이트의 불투명 영역. 피벗 기준 월드 단위 오프셋.</summary>
        public struct Extents
        {
            public float Left, Right, Bottom, Top;
            public float Width  { get { return Right - Left; } }
            public float Height { get { return Top - Bottom; } }
        }

        private static readonly Dictionary<Sprite, Extents> _cache = new Dictionary<Sprite, Extents>();

        /// <summary>먹이 등 달팽이가 아닌 스프라이트도 같은 방식으로 잰다.</summary>
        public static bool TryMeasure(Sprite sprite, out Extents extents) => TryGetExtents(sprite, out extents);

        public static SnailBounds Measure(SnailAppearance appearance)
        {
            var result = new SnailBounds { Left = float.MaxValue, Right = float.MinValue, Top = float.MinValue };
            bool anyPart = false, footFound = false;

            foreach (var p in appearance.Parts)
            {
                // 가로·위쪽은 색상 레이어까지 포함해도 선화와 실루엣이 같으므로 선화만 보면 된다.
                //
                // 폴더는 <b>Folder 로 물어야 한다</b>. 악세서리는 PartsType 이 아니라
                // AccessoriesType 이라 Type 으로 물으면 엉뚱한 폴더를 찾아 null 이 되고,
                // 그대로 건너뛰어 모자·가방이 경계에서 빠졌다 — 초상이 몸통 높이로 잘려
                // 머리 위 악세서리가 통째로 화면 밖에 있었다.
                var sprite = SnailComposer.LoadFrame(SnailComposer.LinePath(p.Folder, p.ResourceKey));
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
                    var sprite = SnailComposer.LoadFrame(SnailComposer.LinePath(p.Folder, p.ResourceKey));
                    if (sprite != null && TryGetExtents(sprite, out var e) && e.Bottom < lowest) lowest = e.Bottom;
                }
                result.Foot = lowest == float.MaxValue ? 0f : lowest;
            }

            result.Measured = true;
            return result;
        }

        /// <summary>
        /// 몸통의 <b>발바닥 선</b>을 x 를 따라 재서 돌려준다.
        ///
        /// 발선을 최하단 한 점으로만 잡으면, 머리 쪽이 들려 있는 몸통에서는
        /// 「발바닥에서의 높이」가 실제보다 크게 나와 그 부분만 변형이 약해진다.
        /// (commonbody03 은 머리 쪽이 87px 들려 있다.)
        /// 열마다 실제 아래끝을 재 두면 어떤 몸통 모양이든 발바닥 전체가 고르게 변형된다.
        ///
        /// 값은 스프라이트 로컬(피벗 기준). 불투명 픽셀이 없는 열은 양옆 값으로 메운다.
        /// </summary>
        public static bool TryMeasureSole(Sprite sprite, int samples, out float[] sole,
                                          out float minX, out float maxX)
        {
            sole = null; minX = maxX = 0f;
            if (sprite == null || samples < 2) return false;
            if (!TryGetExtents(sprite, out var e)) return false;

            var tex = sprite.texture;
            Color32[] px;
            try { px = tex.GetPixels32(); } catch (UnityException) { return false; }

            // 경계와 마찬가지로 이 스프라이트가 쓰는 칸 안에서만 훑는다
            var rect = sprite.rect;
            int x0 = Mathf.Clamp(Mathf.RoundToInt(rect.x), 0, tex.width);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(rect.y), 0, tex.height);
            int w = Mathf.Clamp(Mathf.RoundToInt(rect.width), 0, tex.width - x0);
            int h = Mathf.Clamp(Mathf.RoundToInt(rect.height), 0, tex.height - y0);

            float ppu = sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot;

            minX = e.Left; maxX = e.Right;
            sole = new float[samples];
            var found = new bool[samples];

            for (int i = 0; i < samples; i++)
            {
                // 샘플 하나가 담당하는 픽셀 구간 전체에서 가장 아래를 취한다.
                // 한 열만 찍으면 선화의 빈틈에 걸려 값이 튄다.
                float u0 = i / (float)samples, u1 = (i + 1) / (float)samples;
                int px0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(minX, maxX, u0) * ppu + pivot.x), 0, w - 1);
                int px1 = Mathf.Clamp(Mathf.CeilToInt (Mathf.Lerp(minX, maxX, u1) * ppu + pivot.x), 0, w - 1);

                int lowest = -1;
                for (int y = 0; y < h && lowest < 0; y++)
                {
                    int row = (y + y0) * tex.width + x0;
                    for (int x = px0; x <= px1; x++)
                        if (px[row + x].a > 8) { lowest = y; break; }
                }

                found[i] = lowest >= 0;
                if (found[i]) sole[i] = (lowest - pivot.y) / ppu;
            }

            // 빈 칸 메우기. 앞뒤로 훑어 가장 가까운 실측값을 끌어온다.
            float last = e.Bottom;
            for (int i = 0; i < samples; i++) { if (found[i]) last = sole[i]; else sole[i] = last; }
            last = e.Bottom;
            for (int i = samples - 1; i >= 0; i--) { if (found[i]) last = sole[i]; else sole[i] = last; }

            return true;
        }

        /// <summary>스프라이트의 불투명 영역을 피벗 기준 월드 단위로 돌려준다.</summary>
        /// <summary>
        /// 알파가 있는 부분만 감싸는 텍스처 사각형.
        ///
        /// 달팽이 파츠와 악세서리는 1200x1200 공용 캔버스에 「얹힐 자리 그대로」 그려져 있어,
        /// 아이콘처럼 작은 칸에 그대로 넣으면 그림이 점만 하게 나온다. 잘라 쓸 때 이 값을 쓴다.
        /// </summary>
        public static bool TryGetTightRect(Sprite sprite, out Rect rect)
        {
            rect = default;
            if (sprite == null) return false;

            // 시트면 첫 칸만 잘라 낸다. 통째로 재면 아이콘에 옆 칸까지 들어간다.
            var frame = SnailComposer.FrameZero(sprite);
            if (!TryScan(frame, out int minX, out int maxX, out int minY, out int maxY)) return false;

            // 훑은 좌표는 그 칸 안에서의 것이다. 텍스처 좌표로 되돌려 준다 —
            // 부르는 쪽이 이 값으로 텍스처를 직접 자른다.
            var r = frame.rect;
            rect = new Rect(r.x + minX, r.y + minY, maxX - minX + 1, maxY - minY + 1);
            return true;
        }

        /// <summary>
        /// 스프라이트가 텍스처의 어느 칸을 쓰는가. 결과는 <b>그 칸 안에서의</b> 픽셀 좌표다.
        ///
        /// 애니메이션 시트는 한 텍스처에 여러 칸이 이어 붙어 있어, 텍스처 전체를 훑으면
        /// 옆 칸까지 재게 된다 — 눈 하나가 가로로 세 배가 되어 달팽이가 그만큼 작아진다.
        /// </summary>
        private static bool TryScan(Sprite sprite, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = maxX = minY = maxY = 0;

            var tex = sprite.texture;
            if (tex == null) return false;

            Color32[] px;
            try { px = tex.GetPixels32(); }
            catch (UnityException)
            {
                Debug.LogWarning($"[SnailPet] {tex.name} 의 픽셀을 읽을 수 없습니다. " +
                                 "Read/Write Enabled 를 켜거나 메뉴 SnailPet > 4. 아트 리임포트 를 실행하세요.");
                return false;
            }

            var r = sprite.rect;
            int x0 = Mathf.Clamp(Mathf.RoundToInt(r.x), 0, tex.width);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(r.y), 0, tex.height);
            int w = Mathf.Clamp(Mathf.RoundToInt(r.width), 0, tex.width - x0);
            int h = Mathf.Clamp(Mathf.RoundToInt(r.height), 0, tex.height - y0);

            minX = w; maxX = -1; minY = h; maxY = -1;

            for (int y = 0; y < h; y++)
            {
                int row = (y + y0) * tex.width + x0;
                for (int x = 0; x < w; x++)
                {
                    if (px[row + x].a <= 8) continue;       // 거의 투명한 픽셀은 몸이 아니다
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            return maxX >= 0;                               // 전부 투명이면 false
        }

        private static bool TryGetExtents(Sprite sprite, out Extents e)
        {
            if (_cache.TryGetValue(sprite, out e)) return true;
            e = default;

            var tex = sprite.texture;
            if (tex == null) return false;

            // Read/Write 가 꺼져 있으면 여기서 막힌다. SnailArtImporter 가 켜 주지만
            // 임포트 설정이 어긋나면 조용히 틀린 값을 쓰는 것보다 알리는 게 낫다.
            if (!TryScan(sprite, out int minX, out int maxX, out int minY, out int maxY)) return false;

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
