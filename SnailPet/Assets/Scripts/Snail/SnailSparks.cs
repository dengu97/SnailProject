using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 이미지 한 장을 여러 개 뿌리는 이펙트.
    ///
    /// 부스러기·코인처럼 손으로 짠 것이 아니라 <b>유니티 파티클 시스템</b>을 쓴다. 수십 개가
    /// 한꺼번에 튀는 것은 이쪽이 싸고, 무엇보다 값을 에디터에서 보면서 맞출 수 있다.
    ///
    /// 카메라가 1 유닛 = 1 픽셀이라 <b>크기·속도가 전부 픽셀</b>이다. 24 를 주면 24px 로 나온다.
    /// 그림 비율은 텍스처에서 읽어 지킨다 — 파티클은 기본이 정사각이라 그냥 두면 눌린다.
    ///
    /// 나중에 에디터에서 구운 프리팹으로 갈아탈 수 있다. 그때는 이 자리에서
    /// <c>Resources.Load&lt;GameObject&gt;</c> 로 불러 <c>Instantiate</c> 하면 되고,
    /// 부르는 쪽(자리·개수)은 그대로 둔다.
    /// </summary>
    public sealed class SparkField
    {
        /// <summary>이펙트 이미지가 있는 곳.</summary>
        private const string ArtFolder = "Effects/";

        /// <summary>이펙트가 달팽이 위로 오게 하는 순서. 프리팹이 0 일 때만 쓴다.</summary>
        private const int DefaultOrder = 9600;

        private readonly Transform _parent;
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        public SparkField(Transform parent) { _parent = parent; }

        private Texture2D Art(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_textures.TryGetValue(key, out var had)) return had;

            var tex = Resources.Load<Texture2D>(ArtFolder + key);
            if (tex == null) Debug.LogWarning("[SnailPet] 이펙트 이미지를 찾지 못했습니다: " + ArtFolder + key);

            _textures[key] = tex;
            return tex;
        }

        /// <summary>같은 그림이면 머티리얼을 나눠 쓴다.</summary>
        private Material MaterialFor(string key, Texture2D tex)
        {
            if (_materials.TryGetValue(key, out var had) && had != null) return had;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[SnailPet] Sprites/Default 셰이더를 찾지 못했습니다.");
                shader = Shader.Find("Unlit/Transparent");
            }

            var m = new Material(shader) { mainTexture = tex };
            _materials[key] = m;
            return m;
        }

        /// <summary>
        /// 한 번 터뜨린다. 다 사라지면 스스로 없어진다.
        /// </summary>
        /// <param name="art">Resources/Effects 아래의 이미지 이름.</param>
        /// <param name="world">터질 자리(월드). 화면 좌표는 부르는 쪽이 옮겨서 넘긴다.</param>
        /// <param name="count">몇 개.</param>
        /// <param name="pixels">한 개의 가로 크기(px).</param>
        /// <param name="spread">퍼지는 속도(px/s).</param>
        /// <param name="seconds">한 개가 살아 있는 시간.</param>
        public ParticleSystem Burst(string art, Vector3 world, int count, float pixels,
                                    float spread = 90f, float seconds = 0.7f, int sortingOrder = 9600)
        {
            var tex = Art(art);
            if (tex == null || count <= 0) return null;

            var go = new GameObject("Spark_" + art);
            go.transform.SetParent(_parent, false);
            go.transform.position = world;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Shape(ps, pixels, tex.height / (float)Mathf.Max(1, tex.width), spread, seconds, sortingOrder);
            ps.GetComponent<ParticleSystemRenderer>().material = MaterialFor(art, tex);

            var main = ps.main;
            main.playOnAwake = false;
            main.maxParticles = Mathf.Max(count, 64);

            ps.Emit(count);      // 지금 이 자리에서 한 번에 뿌린다
            ps.Play();
            return ps;
        }

        /// <summary>
        /// 파티클 값 한 벌.
        ///
        /// 코드로 터뜨릴 때와 <b>에디터에서 프리팹을 구울 때</b>가 같은 모양이어야 해서 한 곳에 둔다.
        /// 뿌리는 방법만 다르다 — 코드 쪽은 그 자리에서 <c>Emit</c> 하고, 프리팹은 깨어나면서
        /// 스스로 한 번 뿜는다.
        /// </summary>
        /// <param name="aspect">그림의 세로÷가로. 파티클은 기본이 정사각이라 이걸로 비율을 지킨다.</param>
        public static void Shape(ParticleSystem ps, float pixels, float aspect,
                                 float spread, float seconds, int sortingOrder)
        {
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(seconds * 0.6f, seconds);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spread * 0.4f, spread);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            // 파티클은 기본이 정사각이라 그림 비율을 따로 지켜 준다
            main.startSize3D = true;
            main.startSizeX = pixels;
            main.startSizeY = pixels * (aspect <= 0f ? 1f : aspect);
            main.startSizeZ = 1f;

            // 다 끝나면 스스로 정리한다. 부르는 쪽이 들고 있을 것이 없다.
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.enabled = false;              // 뿌리는 것은 아래에서 한 번에 한다

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(1f, pixels * 0.4f);

            // 끝에서 스르륵 사라진다
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(g);

            // 커지지 않고 조금씩 작아진다
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.35f));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }

        /// <summary>
        /// 에디터에서 구운 이펙트 프리팹을 그 자리에 놓는다. 없으면 null —
        /// 부르는 쪽이 <see cref="Burst"/> 로 물러설 수 있다.
        ///
        /// 프리팹은 깨어나면서 스스로 한 번 뿜고, 끝나면 스스로 없어진다(StopAction=Destroy).
        /// 그래서 여기서 들고 있을 것이 없다.
        /// </summary>
        public GameObject Play(string prefabKey, Vector3 world)
        {
            if (string.IsNullOrEmpty(prefabKey)) return null;

            if (!_prefabs.TryGetValue(prefabKey, out var src))
            {
                src = Resources.Load<GameObject>(ArtFolder + prefabKey);
                _prefabs[prefabKey] = src;      // 없는 것도 기억해 매번 다시 찾지 않는다
            }
            if (src == null) return null;

            var go = Object.Instantiate(src, world, Quaternion.identity, _parent);
            go.transform.position = world;      // 부모가 안 움직이므로 월드 그대로 둔다

            // 그리는 순서를 안 정해 둔 프리팹은 달팽이 위로 올린다.
            // 스토어에서 받은 이펙트는 대개 0 이라 그대로 두면 몸에 가려 안 보인다.
            // 0 이 아닌 것은 만든 사람이 정한 값이므로 건드리지 않는다.
            foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
                if (r.sortingOrder == 0) r.sortingOrder = DefaultOrder;

            return go;
        }

        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        // ── 파츠에 딸린 이펙트 ──
        //
        // PartsData.EffectPath 에 이름이 적힌 파츠는 그 자리에서 이펙트가 돈다.
        // 내 달팽이와 손님 달팽이가 같은 길을 쓴다 — 두 벌이 되면 한쪽만 고치게 된다.

        /// <summary>붙어 있는 이펙트 하나. <see cref="Local"/> 은 합성 안에서의 자리다.</summary>
        public struct Attached
        {
            public Transform Root;
            public Vector3 Local;
        }

        /// <summary>
        /// 외형에서 <c>EffectPath</c> 가 적힌 파츠를 찾아 그 자리에 이펙트를 붙인다.
        /// 자리는 파츠 그림의 한가운데다 — 파츠가 전부 같은 캔버스에 그려져 있어 그 값이
        /// 곧 합성 안에서의 자리가 된다.
        /// </summary>
        public List<Attached> AttachTo(SnailAppearance look, Transform snailRoot)
        {
            var list = new List<Attached>();
            if (look == null || snailRoot == null) return list;

            foreach (var p in look.Parts)
            {
                if (p.Accessory.HasValue) continue;      // 악세서리에는 그 칸이 없다
                if (!Data.GameData.PartsDataById.TryGetValue(p.PartsId, out var row)) continue;
                if (string.IsNullOrEmpty(row.EffectPath)) continue;

                if (!AnchorOf(p, out var local)) continue;

                var go = Play(row.EffectPath, snailRoot.TransformPoint(local));
                if (go == null)
                {
                    Debug.LogWarning("[SnailPet] 이펙트 프리팹을 찾지 못했습니다: " +
                                     p.ResourceKey + " → " + row.EffectPath);
                    continue;
                }

                list.Add(new Attached { Root = go.transform, Local = local });
            }
            return list;
        }

        /// <summary>
        /// 그 파츠 그림의 <b>불투명한 부분 한가운데</b>. 파츠가 전부 같은 캔버스에 그려져 있어
        /// 이 값이 곧 합성 안에서의 자리가 된다. 못 재면 false.
        /// </summary>
        private static bool AnchorOf(SnailPartRef p, out Vector3 local)
        {
            local = Vector3.zero;

            var sprite = SnailComposer.LoadFrame(SnailComposer.LinePath(p.Type, p.ResourceKey));
            if (sprite == null || !SnailMetrics.TryMeasure(sprite, out var e)) return false;

            local = new Vector3((e.Left + e.Right) * 0.5f, (e.Bottom + e.Top) * 0.5f, 0f);
            return true;
        }

        /// <summary>
        /// 그 부위의 이펙트 자리. 미리보기(F5)가 <b>실제와 같은 자리</b>에 붙이려고 쓴다 —
        /// 두 벌로 재면 미리보기와 출시본이 어긋난다.
        /// </summary>
        public static bool AnchorOf(SnailAppearance look, Data.PartsType type, out Vector3 local)
        {
            local = Vector3.zero;
            if (look == null) return false;

            foreach (var p in look.Parts)
                if (!p.Accessory.HasValue && p.Type == type) return AnchorOf(p, out local);

            return false;
        }

        /// <summary>
        /// 붙어 있는 이펙트를 제자리에 둔다. <b>루트 자세가 정해진 뒤에</b> 불러야 한다.
        /// 회전은 주지 않는다 — 벽을 타고 돌아도 반짝임은 똑바로 서는 편이 낫다.
        /// </summary>
        public static void Place(List<Attached> attached, Transform snailRoot)
        {
            if (attached == null || attached.Count == 0 || snailRoot == null) return;

            for (int i = attached.Count - 1; i >= 0; i--)
            {
                if (attached[i].Root == null) { attached.RemoveAt(i); continue; }
                attached[i].Root.position = snailRoot.TransformPoint(attached[i].Local);
            }
        }

        /// <summary>
        /// 붙어 있던 것을 치운다. 프리팹이 looping 이면 스스로 끝나지 않으므로
        /// (StopAction=Destroy 가 안 걸린다) 세운 쪽이 반드시 치워야 한다.
        /// </summary>
        public static void Detach(List<Attached> attached)
        {
            if (attached == null) return;

            foreach (var a in attached)
                if (a.Root != null) Object.Destroy(a.Root.gameObject);

            attached.Clear();
        }
    }
}
