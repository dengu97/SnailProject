using System.Collections.Generic;
using SnailPet.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace SnailPet.Snail
{
    /// <summary>
    /// 파츠 스프라이트를 겹쳐 달팽이 한 마리를 만든다.
    ///
    /// 모든 파츠가 같은 캔버스에 미리 배치돼 있어 오프셋 계산이 필요 없다.
    /// 전부 로컬 (0,0) 에 두고 정렬 순서만 맞추면 그림이 완성된다.
    /// 파츠 하나는 색상 레이어(아래) + 선화 레이어(위) 두 장이다.
    /// </summary>
    public static class SnailComposer
    {
        public const string ResourceRoot = "Snail";

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Load(string path)
        {
            if (_cache.TryGetValue(path, out var s)) return s;
            s = Resources.Load<Sprite>(path);
            _cache[path] = s;                       // 실패(null)도 캐시해 매 프레임 재시도하지 않게 한다
            if (s == null) Debug.LogWarning("[SnailPet] 스프라이트를 찾지 못했습니다: " + path);
            return s;
        }

        public static string LinePath(PartsType type, string key)  => $"{ResourceRoot}/{type}/{key}";
        public static string ColorPath(PartsType type, string key) => $"{ResourceRoot}/{type}/Color/{key}";

        /// <summary>
        /// 합성된 달팽이를 만든다. 반환된 루트를 이동·회전·스케일하면 전체가 따라온다.
        /// SortingGroup 을 붙여 두면 여러 마리가 한 화면에 있어도 서로의 레이어에 끼어들지 않는다.
        /// </summary>
        public static GameObject Build(SnailAppearance appearance, string name = "Snail")
        {
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>();

            var parts = new List<SnailPartRef>(appearance.Parts);
            parts.Sort((a, b) => PartsLayer.SortOrderOf(a.Type).CompareTo(PartsLayer.SortOrderOf(b.Type)));

            foreach (var p in parts)
            {
                // 색상과 선화가 같은 순서 공간을 쓰되 항상 색상이 아래로 가도록 2칸씩 벌린다
                int baseOrder = PartsLayer.SortOrderOf(p.Type) * 2;

                if (!string.IsNullOrEmpty(p.ColorKey))
                    AddLayer(root.transform, ColorPath(p.Type, p.ColorKey), baseOrder, p.Type + "_color");

                AddLayer(root.transform, LinePath(p.Type, p.ResourceKey), baseOrder + 1, p.Type + "_line");
            }
            return root;
        }

        private static void AddLayer(Transform parent, string path, int sortingOrder, string name)
        {
            var sprite = Load(path);
            if (sprite == null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        /// <summary>씬을 갈아엎을 때 캐시가 남아 있으면 파괴된 스프라이트를 참조하게 된다.</summary>
        public static void ClearCache() => _cache.Clear();
    }
}
