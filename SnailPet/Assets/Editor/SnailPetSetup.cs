using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SnailPet.EditorTools
{
    /// <summary>
    /// 데스크톱 펫이 되기 위해 필요한 프로젝트 설정을 자동으로 맞춘다.
    /// 프로젝트를 처음 열면 한 번 자동 실행되고, 이후에는 메뉴에서 다시 돌릴 수 있다.
    /// </summary>
    [InitializeOnLoad]
    public static class SnailPetSetup
    {
        private const string AppliedKey = "SnailPet.SetupApplied.v1";
        private const string ScenePath  = "Assets/Scenes/Main.unity";
        private const string BuildDir   = "Build";

        static SnailPetSetup()
        {
            // 도메인 리로드 중에 에셋을 건드리면 안 되므로 다음 에디터 틱으로 미룬다
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(AppliedKey, false)) return;
                if (EditorPrefs.GetBool(AppliedKey + "." + Application.dataPath, false)) return;
                Apply(silent: true);
            };
        }

        [MenuItem("SnailPet/1. 프로젝트 셋업", priority = 1)]
        public static void ApplyMenu() { Apply(silent: false); }

        public static void Apply(bool silent)
        {
            PlayerSettings.companyName = "SnailTown";
            PlayerSettings.productName = "SnailPet";

            // ── 데스크톱 펫에 필수인 설정들 ──
            PlayerSettings.runInBackground     = true;   // 포커스가 없어도 계속 움직여야 한다
            PlayerSettings.visibleInBackground = true;   // 다른 창이 앞에 와도 계속 보인다
            PlayerSettings.resizableWindow     = false;
            PlayerSettings.allowFullscreenSwitch = false;
            PlayerSettings.fullScreenMode      = FullScreenMode.Windowed;
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.defaultScreenWidth  = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.usePlayerLog        = true;

            // 투명 창의 핵심. flip model 스왑체인에서는 DWM 유리 영역과 알파 합성이
            // 제대로 동작하지 않는다. 반드시 꺼야 한다.
            try { PlayerSettings.useFlipModelSwapchain = false; }
            catch { Debug.LogWarning("[SnailPet] useFlipModelSwapchain 을 설정하지 못했습니다. " +
                                     "Player Settings 에서 직접 꺼주세요."); }

            // D3D11 로 고정. D3D12/Vulkan 은 레이어드 윈도우 알파 합성이 불안정하다.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });

            EnsureScene();

            EditorPrefs.SetBool(AppliedKey + "." + Application.dataPath, true);
            SessionState.SetBool(AppliedKey, true);
            AssetDatabase.SaveAssets();

            string msg = "SnailPet 프로젝트 셋업 완료.\n" +
                         "· Run In Background / Visible In Background 켬\n" +
                         "· Flip Model Swapchain 끔 (투명 창 필수)\n" +
                         "· 그래픽 API: Direct3D11 고정\n" +
                         "· 씬: " + ScenePath;
            Debug.Log("[SnailPet] " + msg);
            if (!silent) EditorUtility.DisplayDialog("SnailPet", msg, "확인");
        }

        /// <summary>
        /// 빈 씬 하나만 있으면 된다. 카메라도 달팽이도 SnailPetBootstrap 이 런타임에 만든다.
        /// </summary>
        private static void EnsureScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));

            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("SnailPet/2. 빌드 & 실행", priority = 2)]
        public static void BuildAndRun()
        {
            Apply(silent: true);

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", BuildDir));
            Directory.CreateDirectory(dir);
            string exe = Path.Combine(dir, "SnailPet.exe");

            var options = new BuildPlayerOptions
            {
                scenes           = new[] { ScenePath },
                locationPathName = exe,
                target           = BuildTarget.StandaloneWindows64,
                options          = BuildOptions.AutoRunPlayer
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log("[SnailPet] 빌드 성공 → " + exe);
            else
                Debug.LogError("[SnailPet] 빌드 실패: " + report.summary.result);
        }

        [MenuItem("SnailPet/3. 빌드만 (실행 안 함)", priority = 3)]
        public static void BuildOnly()
        {
            Apply(silent: true);

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", BuildDir));
            Directory.CreateDirectory(dir);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes           = new[] { ScenePath },
                locationPathName = Path.Combine(dir, "SnailPet.exe"),
                target           = BuildTarget.StandaloneWindows64,
                options          = BuildOptions.None
            });

            Debug.Log("[SnailPet] 빌드 결과: " + report.summary.result);
        }
    }
}
