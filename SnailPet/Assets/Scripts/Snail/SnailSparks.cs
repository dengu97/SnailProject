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

            // 남의 프로젝트에서 온 이펙트를 여기서 돌게 만드는 두 손질.
            foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                // UI 파티클로 만든 것(fx_ui_*)은 <b>렌더러가 꺼져 있다</b> — 캔버스에 직접
                // 그리는 부품(UIParticle 계열)이 대신 그렸기 때문이다. 그 부품은 이 프로젝트에
                // 없고 필요도 없다(여기는 월드에 그린다). 꺼진 채로 두면 아무것도 안 보인다.
                if (!r.enabled) r.enabled = true;

                // 통째로 달팽이 위로 올린다. 프리팹에 적힌 값은 <b>제 안에서의 앞뒤</b>로만 쓴다.
                //
                // 달팽이 파츠는 SortOrder*2 로 깔려 한 자릿수~스물 몇에 들어 있어서, 남의
                // 프로젝트에서 온 값(0 이나 2 같은)을 그대로 두면 몸 사이에 끼어 안 보인다.
                // 예전에는 0 일 때만 올렸는데, 2 로 적힌 프리팹이 그대로 몸 뒤에 깔렸다(2026-09-05).
                //
                // 이미 위에 있으면 그대로 둔다 — 안 그러면 프리팹에서 값을 올려 놓을 때마다
                // 여기서 또 더해 배로 불어난다(9600 으로 고쳤더니 19200 이 됐다).
                if (r.sortingOrder < DefaultOrder) r.sortingOrder += DefaultOrder;

                // 재질이 없으면 그 알갱이는 <b>아무것도 안 그려진다</b>. 프리팹을 다른 곳에서
                // 가져올 때 재질만 빠지는 일이 흔한데, 조용히 안 보이므로 여기서 짚어 준다.
                if (r.sharedMaterial == null)
                    Debug.LogWarning("[SnailPet] 이펙트에 재질이 없습니다: " + prefabKey + " / " + r.name +
                                     " — 이 알갱이는 안 그려집니다");
            }

            return go;
        }

        private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        // ── 파츠에 딸린 이펙트 ──
        //
        // PartsData.EffectPath 에 이름이 적힌 파츠는 그 자리에서 이펙트가 돈다.
        // 내 달팽이와 손님 달팽이가 같은 길을 쓴다 — 두 벌이 되면 한쪽만 고치게 된다.

        /// <summary>
        /// 파츠에 딸린 이펙트를 끌지. 설정의 「달팽이 이펙트 끄기」가 여기로 온다.
        ///
        /// 세이브에 있는 값을 이펙트 층까지 들고 오지 않으려고 <b>정적 스위치</b> 하나만 둔다 —
        /// 손님 달팽이를 만드는 곳은 플레이어 상태를 모르는 자리라 값을 넘길 길이 마땅치 않다.
        /// 켜고 끄는 것은 설정이 바뀔 때 게임 쪽이 한 번 해 준다.
        /// </summary>
        public static bool NoEffect;

        /// <summary>붙어 있는 이펙트 하나. <see cref="Local"/> 은 합성 안에서의 자리다.</summary>
        public struct Attached
        {
            public Transform Root;
            public Vector3 Local;

            /// <summary>
            /// 달팽이 배율 1 당 이 이펙트에 줄 배율. <see cref="Place"/> 가 매 프레임 곱한다.
            /// 0 이면 크기를 안 건드린다(못 쟀다는 뜻).
            /// </summary>
            public float Fit;

            /// <summary>이 이펙트가 차지했으면 하는 세로(캔버스 단위). 달팽이 몸 높이에서 나온다.</summary>
            public float WantCanvas;

            /// <summary>붙은 뒤 흐른 시간. 알갱이가 퍼질 때까지 기다렸다 재려고 센다.</summary>
            public float Age;

            /// <summary>실제로 그려진 크기를 재서 한 번 맞췄는가.</summary>
            public bool Tuned;
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

            // 설정에서 껐으면 아예 안 붙인다. 내 달팽이와 손님이 이 길을 함께 쓰므로
            // 여기 하나만 막으면 둘 다 꺼진다.
            if (NoEffect) return list;

            // 몸 높이는 <b>캔버스 단위</b>로 잰다. 달팽이 배율은 여기서 곱하지 않는다 —
            // 이펙트를 붙이는 시점에는 자세가 아직 안 정해져 배율이 1 로 읽힌다.
            // 실제 크기는 매 프레임 Place 가 그때의 배율을 곱해 맞춘다.
            var b = SnailMetrics.Measure(look, withAccessories: false);
            float bodyCanvas = b.Measured ? b.Top - b.Foot : 0f;

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

                float fit = FitOf(go, bodyCanvas);
                Describe(p.ResourceKey, row.EffectPath, go, fit);
                list.Add(new Attached
                {
                    Root = go.transform,
                    Local = local,
                    Fit = fit,
                    WantCanvas = bodyCanvas * EffectFraction,
                });
            }
            return list;
        }

        /// <summary>
        /// 이펙트 <b>전체</b>가 차지할 세로 — 달팽이 몸 높이에 대한 비율.
        /// 0.9 면 30px 짜리 달팽이에 27px 짜리 이펙트가 뜬다. 크기가 어색하면 여기만 만진다.
        ///
        /// 알갱이 하나가 아니라 전체를 재는 것은, 알갱이 하나의 크기는 프리팹마다 손잡이가
        /// 달라 믿을 수 없기 때문이다(<see cref="Tune"/> 참고).
        /// </summary>
        private const float EffectFraction = 0.9f;

        /// <summary>알갱이가 퍼질 때까지 기다리는 시간(초). 갓 태어난 것만 재면 뭉쳐 있어 작게 나온다.</summary>
        private const float TuneDelay = 0.35f;

        /// <summary>
        /// <b>실제로 그려진 크기를 재서</b> 한 번 맞춘다.
        ///
        /// 프리팹의 <c>startSize</c> 로 미루어 짐작하는 것은 못 쓴다 — 크기를 정하는 손잡이가
        /// 한둘이 아니다(startSize · size3D · 수명 곡선 · 트랜스폼 배율 · 렌더 모드).
        /// heart 는 그 계산으로 「9.5px」이었는데 화면에서는 훨씬 컸다(2026-09-05).
        ///
        /// 렌더러의 <c>bounds</c> 는 지금 살아 있는 알갱이가 <b>차지한 실제 넓이</b>라
        /// 그 손잡이들이 어떻게 얽혔든 결과 하나만 본다. 한 번 재서 고치면 그다음부터는
        /// 배율만 곱하면 되므로 매 프레임 잴 일도 없다.
        /// </summary>
        private static void Tune(ref Attached a, float wantPx)
        {
            a.Age += Time.deltaTime;
            if (a.Age < TuneDelay || wantPx <= 0f) return;

            var ps = a.Root.GetComponentInChildren<ParticleSystem>(true);
            var r  = a.Root.GetComponentInChildren<ParticleSystemRenderer>(true);

            // 알갱이가 아직 하나도 안 나왔으면 잴 것이 없다. 다음 프레임에 다시 본다.
            if (ps == null || r == null || ps.particleCount == 0) return;

            float now = r.bounds.size.y;
            if (now > 0.0001f) a.Fit *= wantPx / now;

            a.Tuned = true;
        }

        /// <summary>
        /// 첫 배율의 어림값. 여기서 크게 빗나가도 <see cref="Tune"/> 이 곧 바로잡으므로,
        /// 첫 몇 프레임이 터무니없지만 않으면 된다.
        ///
        /// <b>이게 없으면 대부분의 이펙트가 안 보인다.</b> 남에게서 받은 프리팹은 1 유닛을
        /// 1 미터로 잡고 만드는데, 여기는 카메라가 <c>orthographicSize = 화면 높이 / 2</c> 라
        /// <b>1 유닛이 1 픽셀</b>이다. heart 는 그래서 3px 짜리로 떠 있었다(2026-09-05).
        ///
        /// 프리팹에 적힌 크기는 버린다 — 어떤 크기로 만들어 오든 화면에서는 같게 보인다.
        /// </summary>
        private static float FitOf(GameObject go, float bodyCanvas)
        {
            if (bodyCanvas <= 0f) return 0f;

            var ps = go.GetComponentInChildren<ParticleSystem>(true);
            if (ps == null) return 0f;

            // startSizeMultiplier 는 상수로 잡았든 곡선으로 잡았든 그 크기의 배수다.
            float unit = ps.main.startSizeMultiplier;
            return unit > 0.0001f ? bodyCanvas * EffectFraction / unit : 0f;
        }

        /// <summary>
        /// 붙인 이펙트가 어떤 상태인지 로그에 남긴다.
        ///
        /// 「연결했는데 안 뜬다」가 되풀이되는 자리라서 남긴다. 남의 프로젝트에서 온 프리팹은
        /// 대개 <b>1 유닛 = 1 미터</b>로 만들어져 있는데 여기는 <b>1 유닛 = 1 픽셀</b>이라
        /// (카메라 orthographicSize = 화면 높이의 절반), 알갱이가 몇 픽셀짜리로 줄어
        /// 붙긴 붙었는데 안 보이는 일이 생긴다. 그 크기를 눈으로 확인할 수 있게 적는다.
        /// </summary>
        private static void Describe(string partKey, string effectKey, GameObject go, float fit)
        {
            var all = go.GetComponentsInChildren<ParticleSystemRenderer>(true);

            // 재질 없는 렌더러가 섞여 있으면 그것만 안 그려진다. 몇 개 중 몇 개인지 같이 적는다.
            int drawn = 0;
            foreach (var r in all) if (r.sharedMaterial != null) drawn++;

            // 픽셀 크기는 여기서 못 적는다 — 달팽이 배율이 아직 안 걸려 있다(Place 가 맞춘다).
            // 대신 못 쟀는지(0)만 보이면 된다. 실제 크기는 Place 의 Tune 이 재서 맞춘다.
            Debug.Log($"[SnailPet] 이펙트 {partKey} → {effectKey}: " +
                      $"자리({go.transform.position.x:0},{go.transform.position.y:0}) " +
                      $"크기맞춤 {(fit > 0f ? $"몸의 {EffectFraction * 100:0}%" : "못 쟀음")} " +
                      $"렌더러 {all.Length}개 중 그릴 수 있는 것 {drawn}개 " +
                      $"정렬 {(all.Length > 0 ? all[0].sortingOrder : 0)}");
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

            float scale = Mathf.Abs(snailRoot.lossyScale.y);

            for (int i = attached.Count - 1; i >= 0; i--)
            {
                var a = attached[i];
                if (a.Root == null) { attached.RemoveAt(i); continue; }

                a.Root.position = snailRoot.TransformPoint(a.Local);

                // 크기는 여기서 맞춘다. 붙일 때 한 번만 하면 안 된다 — 그때는 달팽이 자세가
                // 아직 안 정해져 배율이 1 로 읽혀 이펙트가 서른 배로 커졌다(2026-09-05).
                // 여기서 하면 레벨이 올라 몸이 커질 때 이펙트도 저절로 따라 커진다.
                if (a.Fit > 0f)
                {
                    a.Root.localScale = Vector3.one * (a.Fit * scale);
                    if (!a.Tuned) Tune(ref a, a.WantCanvas * scale);
                    attached[i] = a;
                }
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
