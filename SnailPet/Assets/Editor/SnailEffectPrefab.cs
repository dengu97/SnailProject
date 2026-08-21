using System.IO;
using SnailPet.Snail;
using UnityEditor;
using UnityEngine;

namespace SnailPet.EditorTools
{
    /// <summary>
    /// 파티클 이펙트를 코드로 한 번 지어서 <b>편집 가능한 프리팹</b>으로 저장한다.
    ///
    /// UI 프리팹과 같은 생각이다 — 굽는 것은 코드가 하고, 그다음부터는 프리팹이 원본이다.
    /// 프리팹을 열어 Inspector 에서 값을 만지면 그대로 화면에 나온다(파티클은 씬 뷰에서
    /// 재생되므로 보면서 맞출 수 있다).
    ///
    /// 그림은 <b>머티리얼</b>에 물린다. 여기서는 빈 머티리얼만 만들어 두므로,
    /// <c>Assets/Resources/Effects/spark.mat</c> 을 골라 Inspector 의 텍스처 칸에
    /// 쓰실 이미지를 끌어다 넣으시면 됩니다.
    /// </summary>
    public static class SnailEffectPrefab
    {
        public const string Folder = "Assets/Resources/Effects";
        public const string PrefabPath = Folder + "/spark.prefab";
        public const string MaterialPath = Folder + "/spark.mat";

        /// <summary>구울 때 쓰는 기본값. 카메라가 1유닛 = 1픽셀이라 전부 픽셀 단위다.</summary>
        private const float Pixels = 24f, Spread = 120f, Seconds = 0.7f;
        private const int Count = 12, SortingOrder = 9600;

        [MenuItem("SnailPet/6. 이펙트 프리팹 생성", priority = 6)]
        public static void Generate()
        {
            // UI 프리팹과 같은 규칙 — 물어볼 수 없는 배치 모드에서는 덮어쓰지 않는다.
            bool exists = File.Exists(PrefabPath);
            if (exists && Application.isBatchMode)
            {
                Debug.LogWarning("[SnailPet] 이펙트 프리팹이 이미 있어 건너뜁니다: " + PrefabPath);
                return;
            }

            if (exists && !EditorUtility.DisplayDialog(
                    "SnailPet",
                    "이미 이펙트 프리팹이 있습니다.\n\n" +
                    "다시 만들면 Inspector 에서 맞춘 값이 코드의 기본값으로 되돌아갑니다.\n" +
                    "(머티리얼에 물린 그림은 그대로 남습니다)\n\n" + PrefabPath,
                    "덮어쓰기", "취소"))
                return;

            Directory.CreateDirectory(Folder);

            var host = new GameObject("Spark");
            try
            {
                var ps = host.AddComponent<ParticleSystem>();

                // 코드로 터뜨릴 때와 같은 값 한 벌을 쓴다. 두 벌이 되면 곧 어긋난다.
                SparkField.Shape(ps, Pixels, 1f, Spread, Seconds, SortingOrder);

                // 프리팹은 놓이자마자 스스로 한 번 뿜는다
                var main = ps.main;
                main.playOnAwake = true;
                main.maxParticles = Mathf.Max(Count, 64);

                var emission = ps.emission;
                emission.enabled = true;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Count) });

                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = EnsureMaterial();

                PrefabUtility.SaveAsPrefabAsset(host, PrefabPath, out bool ok);
                if (!ok)
                {
                    Debug.LogError("[SnailPet] 이펙트 프리팹 저장에 실패했습니다: " + PrefabPath);
                    return;
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SnailPet] 이펙트 프리팹 생성: {PrefabPath}\n" +
                      $"  · 그림은 {MaterialPath} 의 텍스처 칸에 끌어다 넣으세요.\n" +
                      $"  · 크기 {Pixels}px · {Count}개 · {Seconds}초 · 퍼짐 {Spread}px/s (Inspector 에서 조절)");
        }

        /// <summary>
        /// 그림을 물릴 머티리얼.
        ///
        /// <b>Sprites/Default 를 쓰면 안 된다</b> — 그 셰이더의 텍스처 칸은 [PerRendererData] 라
        /// Inspector 에 아예 안 보인다(스프라이트 렌더러가 코드로 물리는 용도다). 끌어다 놓을
        /// 자리가 없어서 금지 표시만 뜬다. 파티클용 셰이더는 칸이 보인다.
        ///
        /// 이미 있으면 셰이더만 고쳐 쓴다 — 애써 연결해 둔 그림이 날아가면 안 된다.
        /// </summary>
        private static Material EnsureMaterial()
        {
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null)
            {
                Debug.LogWarning("[SnailPet] 파티클 셰이더를 찾지 못해 Sprites/Default 로 둡니다. " +
                                 "그러면 Inspector 에 그림 칸이 안 보입니다.");
                shader = Shader.Find("Sprites/Default");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "spark" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }

            Fade(material);
            ReportTextureSlots(material);
            return material;
        }

        /// <summary>
        /// 알파로 비치게 맞춘다. Standard 계열은 Rendering Mode 를 바꾸면 Inspector 가 이 값들을
        /// 대신 써 주는데, 코드로 만들 때는 직접 해 줘야 한다. 안 하면 불투명으로 나온다.
        /// </summary>
        private static void Fade(Material m)
        {
            if (!m.HasProperty("_Mode")) return;

            m.SetFloat("_Mode", 2f);                                    // 2 = Fade
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            EditorUtility.SetDirty(m);
        }

        /// <summary>Inspector 에 실제로 보일 그림 칸을 알려 준다. 없으면 끌어넣을 자리가 없다는 뜻이다.</summary>
        private static void ReportTextureSlots(Material m)
        {
            var names = new System.Collections.Generic.List<string>();

            int count = UnityEditor.ShaderUtil.GetPropertyCount(m.shader);
            for (int i = 0; i < count; i++)
            {
                if (UnityEditor.ShaderUtil.GetPropertyType(m.shader, i) != UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;
                if (UnityEditor.ShaderUtil.IsShaderPropertyHidden(m.shader, i)) continue;

                names.Add(UnityEditor.ShaderUtil.GetPropertyDescription(m.shader, i));
            }

            Debug.Log($"[SnailPet] 머티리얼 셰이더: {m.shader.name} · Inspector 의 그림 칸: " +
                      (names.Count == 0 ? "없음 (끌어넣을 자리가 없습니다)" : string.Join(", ", names)));
        }
    }
}
