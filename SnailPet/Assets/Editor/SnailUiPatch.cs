using System;
using System.IO;
using SnailPet.Ui;
using UnityEditor;
using UnityEngine;

namespace SnailPet.EditorTools
{
    /// <summary>
    /// 이미 있는 UI 프리팹에 <b>빠진 조각만</b> 심는다.
    ///
    /// 새 화면 조각은 실행할 때 「없으면 짓기」로 나오지만, 그러면 프리팹에 없어서
    /// 손으로 옮길 수가 없다. 그렇다고 프리팹을 다시 구우면(메뉴 5번) 손으로 맞춰 둔 배치가
    /// 통째로 사라진다 — 그래서 조각 하나만 넣고 나머지는 건드리지 않는 길을 따로 둔다.
    ///
    /// 심고 나면 그 조각이 프리팹의 것이 되므로, 실행할 때 다시 짓지 않고 프리팹에서 온 것을 쓴다.
    /// </summary>
    public static class SnailUiPatch
    {
        [MenuItem("SnailPet/7. 짝꿍 칸을 UI 프리팹에 넣기", priority = 7)]
        public static void AddMateSlot()
            => Patch("짝꿍 칸", ui => ui.BuildMateSlotForPrefab());

        [MenuItem("SnailPet/8. 짝꿍·도움말 팝업을 UI 프리팹에 넣기", priority = 8)]
        public static void AddPopupGroups()
            => Patch("짝꿍·도움말 팝업", ui => ui.BuildPopupGroupsForPrefab());

        /// <summary>
        /// 프리팹을 열어 <paramref name="add"/> 를 한 번 돌리고, 뭔가 심었을 때만 저장한다.
        /// 아무것도 안 심었으면 그대로 닫는다 — 괜히 저장해서 파일을 흔들 이유가 없다.
        /// </summary>
        private static void Patch(string what, Func<SnailUi, bool> add)
        {
            string path = SnailUiPrefab.Path;
            if (!File.Exists(path))
            {
                Debug.LogError("[SnailPet] UI 프리팹이 없습니다: " + path);
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var ui = contents.GetComponent<SnailUi>();
                if (ui == null)
                {
                    Debug.LogError("[SnailPet] 프리팹에서 SnailUi 를 찾지 못했습니다: " + path);
                    return;
                }

                if (!add(ui))
                {
                    Debug.Log($"[SnailPet] {what} 은(는) 이미 프리팹에 있습니다. 그대로 둡니다.");
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log($"[SnailPet] {what} 을(를) 프리팹에 넣었습니다. 이제 프리팹에서 옮길 수 있습니다: " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
