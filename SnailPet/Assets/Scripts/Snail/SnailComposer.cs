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

        /// <summary>애니메이션 시트의 기본 재생 속도(초당 칸). PartsData 에 칸이 생기면 그쪽이 이긴다.</summary>
        public const float DefaultFps = 14f;

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>시트 한 장 → 잘라 놓은 칸들. 같은 파츠를 여러 마리가 써도 한 번만 자른다.</summary>
        private static readonly Dictionary<Sprite, Sprite[]> _frames = new Dictionary<Sprite, Sprite[]>();

        /// <summary>
        /// 이 그림이 애니메이션 시트면 칸별 스프라이트를, 한 장짜리면 null 을 준다.
        ///
        /// 칸 수는 <b>가로÷세로</b>다 — 캔버스가 정사각이라 파일만 보면 알 수 있고, 시트를
        /// 3칸에서 5칸으로 바꿔도 데이터를 안 고쳐도 된다. 코인 모션(600x100=6칸)과 같은 규칙이다.
        /// </summary>
        public static Sprite[] FramesOf(Sprite sheet)
        {
            if (sheet == null) return null;
            if (_frames.TryGetValue(sheet, out var had)) return had;

            var r = sheet.rect;
            int count = Mathf.RoundToInt(r.width / Mathf.Max(1f, r.height));

            // 가로가 세로의 정수배가 아니면 시트로 볼 수 없다. 한 장짜리로 둔다.
            if (count < 2 || Mathf.Abs(r.width - count * r.height) > count)
            {
                _frames[sheet] = null;
                return null;
            }

            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                // 칸 경계를 정수로 맞춰 붙인다. 남는 픽셀이 있어도 틈이나 겹침이 안 생긴다.
                float x0 = Mathf.Round(r.x + r.width * i / count);
                float x1 = Mathf.Round(r.x + r.width * (i + 1) / count);
                var cut = new Rect(x0, r.y, x1 - x0, r.height);

                // PPU 는 시트의 것을 그대로 쓴다. 임포터가 텍스처를 줄일 때 PPU 를 같이 줄여
                // 원본 캔버스 크기(월드)를 지켜 주므로, 그러면 한 칸이 다른 파츠와 같은 크기가 된다.
                frames[i] = Sprite.Create(sheet.texture, cut, new Vector2(0.5f, 0.5f),
                                          sheet.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                frames[i].name = sheet.name + "_" + i;
            }

            _frames[sheet] = frames;

            var one = frames[0];
            Debug.Log($"[SnailPet] 애니메이션 시트: {sheet.name} {count}칸 · 한 칸 {one.rect.width:0}x{one.rect.height:0}px " +
                      $"→ 월드 {one.rect.width / one.pixelsPerUnit:0} (다른 파츠와 같아야 맞습니다)");
            return frames;
        }

        /// <summary>
        /// 크기를 재거나 한 장만 보여 줄 때 쓸 그림. 시트면 첫 칸, 아니면 그대로다.
        /// 시트를 통째로 재면 가로가 칸 수만큼 부풀어 달팽이가 그만큼 작아진다.
        /// </summary>
        public static Sprite FrameZero(Sprite sprite)
        {
            var frames = FramesOf(sprite);
            return frames != null ? frames[0] : sprite;
        }

        public static Sprite Load(string path)
        {
            if (_cache.TryGetValue(path, out var s)) return s;
            s = Resources.Load<Sprite>(path);
            _cache[path] = s;                       // 실패(null)도 캐시해 매 프레임 재시도하지 않게 한다
            if (s == null) Debug.LogWarning("[SnailPet] 스프라이트를 찾지 못했습니다: " + path);
            return s;
        }

        /// <summary>
        /// 크기를 재거나 한 장만 쓰려고 부르는 곳이 쓴다. 시트면 첫 칸이 나온다.
        /// 그리는 쪽은 <see cref="Load"/> 로 시트째 받아 칸을 돌린다.
        /// </summary>
        public static Sprite LoadFrame(string path) => FrameZero(Load(path));

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

            /// <summary>돌아가고 있는 애니메이션 파츠들. 속도를 바꾸거나 세울 때 쓴다.</summary>
            public readonly List<SnailFlipbook.Reel> Flips = new List<SnailFlipbook.Reel>();

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

            // 선화와 색은 같은 속도로 같이 돌아야 한다. 속도는 시트가 한 벌로 들고 있다.
            float fps = Fps;

            foreach (var p in parts)
            {
                int group = p.DeformGroup;
                var parent = GroupRoot(composed, group);
                bool soft = group != PartsLayer.RigidGroup;

                // 색상과 선화가 같은 순서 공간을 쓰되 항상 색상이 아래로 가도록 2칸씩 벌린다
                int baseOrder = p.SortOrder * 2;
                string label = p.Accessory?.ToString() ?? p.Type.ToString();

                if (!string.IsNullOrEmpty(p.ColorKey))
                    AddLayer(composed, parent, soft, ColorPath(p.Folder, p.ColorKey), baseOrder, label + "_color", fps);

                AddLayer(composed, parent, soft, LinePath(p.Folder, p.ResourceKey), baseOrder + 1, label + "_line", fps);
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
        /// 애니메이션 시트를 초당 몇 칸으로 돌릴지.
        ///
        /// 시트에는 <c>GameConfig.AnimationSec</c> — <b>다음 칸까지 몇 초</b> — 로 적혀 있어
        /// 여기서 뒤집는다. 0 이하면 나눌 수 없으므로 <see cref="DefaultFps"/> 로 버틴다.
        /// 파츠마다 다르게 주지 않는다. 전부 한 속도로 돈다.
        /// </summary>
        private static float Fps
        {
            get
            {
                double sec = Config.AnimationSec;
                return sec > 0.0 ? (float)(1.0 / sec) : DefaultFps;
            }
        }

        /// <summary>
        /// 말랑한 파츠는 격자 메시로, 단단한 파츠는 그냥 SpriteRenderer 로 만든다.
        /// 안 휘는 것까지 메시로 깔 이유가 없다.
        /// </summary>
        private static void AddLayer(Composed composed, Transform parent, bool soft,
                                     string path, int sortingOrder, string name, float fps)
        {
            var sprite = Load(path);
            if (sprite == null) return;

            // 시트면 첫 칸으로 세운다. 칸끼리 크기·피벗이 같으므로 나머지 칸은 그림만 갈아 끼우면 된다.
            var frames = FramesOf(sprite);
            var first = frames != null ? frames[0] : sprite;

            if (soft)
            {
                var d = DeformableSprite.Create(parent, first, sortingOrder, name);
                if (d == null) return;

                composed.Soft.Add(d);
                if (frames != null) composed.Flips.Add(SnailFlipbook.Play(composed.Root, frames, d, null, fps));
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = first;
            sr.sortingOrder = sortingOrder;

            if (frames != null) composed.Flips.Add(SnailFlipbook.Play(composed.Root, frames, null, sr, fps));
        }

        /// <summary>
        /// 씬을 갈아엎을 때 캐시가 남아 있으면 파괴된 스프라이트를 참조하게 된다.
        /// 잘라 둔 칸도 그 텍스처를 물고 있으므로 같이 버린다.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
            _frames.Clear();
        }
    }
}
