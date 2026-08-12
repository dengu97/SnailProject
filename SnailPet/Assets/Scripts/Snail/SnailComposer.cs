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

        public static string LinePath(string folder, string key)  => $"{ResourceRoot}/{folder}/{key}";
        public static string ColorPath(string folder, string key) => $"{ResourceRoot}/{folder}/Color/{key}";

        public static string LinePath(PartsType type, string key)  => LinePath(type.ToString(), key);
        public static string ColorPath(PartsType type, string key) => ColorPath(type.ToString(), key);

        /// <summary>
        /// 합성 결과. 변형 그룹별 루트를 들고 있어 나중에 스켈레톤을 붙일 때
        /// 그 루트 아래 스프라이트들만 스키닝하면 된다.
        /// </summary>
        public sealed class Composed
        {
            public GameObject Root;

            /// <summary>DeformGroup id → 루트. 0 은 강체(껍질·악세서리).</summary>
            public readonly Dictionary<int, Transform> Groups = new Dictionary<int, Transform>();

            /// <summary>
            /// 말랑한 파츠들. 트랜스폼이 아니라 정점을 직접 밀어 변형하므로 따로 들고 있는다.
            /// 전부 같은 캔버스를 공유해 <see cref="SnailDeform"/> 하나로 한꺼번에 처리된다.
            /// </summary>
            public readonly List<DeformableSprite> Soft = new List<DeformableSprite>();

            public Transform GroupOrNull(int id) =>
                Groups.TryGetValue(id, out var t) ? t : null;
        }

        /// <summary>
        /// 합성된 달팽이를 만든다. 반환된 루트를 이동·회전·스케일하면 전체가 따라온다.
        /// SortingGroup 을 붙여 두면 여러 마리가 한 화면에 있어도 서로의 레이어에 끼어들지 않는다.
        ///
        /// 파츠는 변형 그룹별 루트 아래로 묶인다. SortingGroup 은 계층 깊이와 무관하게
        /// sortingOrder 로만 정렬하므로, 묶어도 그리는 순서는 달라지지 않는다.
        /// </summary>
        public static Composed Build(SnailAppearance appearance, string name = "Snail")
        {
            var composed = new Composed();
            var root = new GameObject(name);
            root.AddComponent<SortingGroup>();
            composed.Root = root;

            // 악세서리도 파츠와 같은 SortOrder 축을 쓰므로 한 줄에 세워 놓고 순서대로 깐다
            var parts = new List<SnailPartRef>(appearance.Parts);
            parts.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

            foreach (var p in parts)
            {
                int group = p.DeformGroup;
                var parent = GroupRoot(composed, group);
                bool soft = group != PartsLayer.RigidGroup;

                // 색상과 선화가 같은 순서 공간을 쓰되 항상 색상이 아래로 가도록 2칸씩 벌린다
                int baseOrder = p.SortOrder * 2;
                string label = p.Accessory?.ToString() ?? p.Type.ToString();

                if (!string.IsNullOrEmpty(p.ColorKey))
                    AddLayer(composed, parent, soft, ColorPath(p.Folder, p.ColorKey), baseOrder, label + "_color");

                AddLayer(composed, parent, soft, LinePath(p.Folder, p.ResourceKey), baseOrder + 1, label + "_line");
            }
            return composed;
        }

        private static Transform GroupRoot(Composed composed, int groupId)
        {
            if (composed.Groups.TryGetValue(groupId, out var existing)) return existing;

            var go = new GameObject(groupId == PartsLayer.RigidGroup ? "Rigid" : "Deform_" + groupId);
            go.transform.SetParent(composed.Root.transform, false);
            composed.Groups[groupId] = go.transform;
            return go.transform;
        }

        /// <summary>
        /// 말랑한 파츠는 격자 메시로, 단단한 파츠는 그냥 SpriteRenderer 로 만든다.
        /// 안 휘는 것까지 메시로 깔 이유가 없다.
        /// </summary>
        private static void AddLayer(Composed composed, Transform parent, bool soft,
                                     string path, int sortingOrder, string name)
        {
            var sprite = Load(path);
            if (sprite == null) return;

            if (soft)
            {
                var d = DeformableSprite.Create(parent, sprite, sortingOrder, name);
                if (d != null) composed.Soft.Add(d);
                return;
            }

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
