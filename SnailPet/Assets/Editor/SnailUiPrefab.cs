using System.IO;
using SnailPet.Ui;
using UnityEditor;
using UnityEngine;

namespace SnailPet.EditorTools
{
    /// <summary>
    /// UI 를 코드로 한 번 지어서 <b>편집 가능한 프리팹</b>으로 저장한다.
    ///
    /// 이후로는 프리팹이 원본이다. 프리팹을 열어 드래그로 옮기고 스프라이트를 갈아 끼우면
    /// 그대로 화면에 나온다. 코드 빌더는 처음 굽는 용도와, 프리팹이 없을 때의 대비책으로만 남는다.
    ///
    /// 대신 목업 좌표(UiTheme.At)가 자동으로 반영되는 것은 여기서 끝난다.
    /// 목업이 바뀌면 다시 구워야 하고, 그때 손으로 옮긴 것은 사라진다 — 그래서 덮어쓰기 전에 묻는다.
    /// </summary>
    public static class SnailUiPrefab
    {
        public const string Path = "Assets/Resources/" + SnailUi.PrefabResource + ".prefab";

        [MenuItem("SnailPet/5. UI 프리팹 생성", priority = 5)]
        public static void Generate()
        {
            // 배치 모드에서는 대화상자를 띄울 수 없다. 물어볼 수 없으면 덮어쓰지 않는다 —
            // 손으로 옮긴 배치를 말없이 날리는 것보다 아무것도 안 하는 편이 낫다.
            bool exists = File.Exists(Path);
            if (exists && Application.isBatchMode)
            {
                Debug.LogWarning("[SnailPet] UI 프리팹이 이미 있어 건너뜁니다. " +
                                 "덮어쓰려면 에디터에서 메뉴로 실행하세요: " + Path);
                return;
            }

            if (exists && !EditorUtility.DisplayDialog(
                    "SnailPet",
                    "이미 UI 프리팹이 있습니다.\n\n" +
                    "다시 만들면 프리팹에서 손으로 옮긴 배치와 갈아 끼운 이미지가 모두 사라지고,\n" +
                    "코드(UiTheme.At)의 좌표로 되돌아갑니다.\n\n" +
                    Path,
                    "덮어쓰기", "취소"))
                return;

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));

            var host = new GameObject("SnailUi");
            try
            {
                SnailUi.BuildForPrefab(host);
                PrefabUtility.SaveAsPrefabAsset(host, Path, out bool ok);

                if (!ok)
                {
                    Debug.LogError("[SnailPet] UI 프리팹 저장에 실패했습니다: " + Path);
                    return;
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }

            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log("[SnailPet] UI 프리팹을 만들었습니다: " + Path +
                      "\n이제 이 프리팹을 열어 배치를 바꾸시면 그대로 화면에 나옵니다.");
        }
    }
}
