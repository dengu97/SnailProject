using System.IO;
using UnityEditor;
using UnityEngine;

namespace SnailPet.EditorTools
{
    /// <summary>
    /// 달팽이 아트의 임포트 설정을 강제한다.
    ///
    /// 이 프로젝트는 3D 템플릿으로 만들어져서 PNG 가 기본적으로 Texture 로 들어온다.
    /// 그대로 두면 SpriteRenderer 에 넣을 수 없으므로 여기서 Sprite 로 바꾼다.
    /// 아티스트가 새 파츠를 추가해도 자동 적용된다.
    /// </summary>
    public sealed class SnailArtImporter : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/Resources/Snail";

        /// <summary>
        /// 1 텍스처 픽셀 = 1 월드 유닛.
        /// 파츠가 전부 같은 캔버스(1200x1200)에 그려져 있어 오프셋 없이 겹치기만 하면 되는데,
        /// 이 값이 1 이면 "월드 좌표 = 캔버스 픽셀" 이 되어 합성·발선 계산이 그대로 읽힌다.
        /// </summary>
        public const float PixelsPerUnit = 1f;

        /// <summary>
        /// 원본은 1200px 이지만 화면에는 200px 안팎으로 나온다.
        /// 1200px 을 그대로 두면 압축해도 파츠당 1.4MB 라 42장이면 60MB 가 된다.
        /// 512 면 표시 크기의 2.5배라 확대 연출에도 충분하다.
        /// </summary>
        public const int MaxTextureSize = 512;

        /// <summary>
        /// UI 아이콘. 달팽이 파츠와 규칙이 다르다.
        ///  · PPU 1 을 쓰면 안 된다. UI 는 RectTransform 이 크기를 정하는데 PPU 1 이면
        ///    9-슬라이스 테두리 두께 계산이 어긋난다.
        ///  · 알파 스캔 대상이 아니라 isReadable 이 필요 없다.
        /// </summary>
        public const string UiRoot = "Assets/Resources/Ui";

        /// <summary>9-슬라이스 도형 아트. 아이콘과 규칙이 또 다르다 (아래 ShapeScale 참고).</summary>
        public const string UiShapeRoot = "Assets/Resources/Ui/Shape";

        /// <summary>
        /// 도형 아트를 화면 크기의 몇 배로 그리는가.
        ///
        /// 9-슬라이스의 모서리는 <b>원본 픽셀 크기 그대로</b> 찍힌다. 그래서 1:1 로 그리면
        /// 모서리가 6px 밖에 안 돼 디자인을 넣을 자리가 없다. 4배로 그리고 PPU 를 4로 두면
        /// 유니티가 테두리 두께를 4로 나눠 쓰므로, 넉넉한 캔버스에 그리면서 화면 크기는 유지된다.
        /// (캔버스 referencePixelsPerUnit 이 1 이라 PPU 가 곧 축소 배율이 된다.)
        /// </summary>
        public const float ShapeScale = 4f;

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');

            if (path.StartsWith(UiRoot))
            {
                var ui = (TextureImporter)assetImporter;
                ui.textureType         = TextureImporterType.Sprite;
                ui.spriteImportMode    = SpriteImportMode.Single;
                ui.alphaIsTransparency = true;
                ui.mipmapEnabled       = false;
                ui.filterMode          = FilterMode.Bilinear;
                ui.wrapMode            = TextureWrapMode.Clamp;
                ui.maxTextureSize      = 256;      // 화면에는 16~32px 로 나온다
                ui.textureCompression  = TextureImporterCompression.CompressedHQ;

                if (path.StartsWith(UiShapeRoot))
                {
                    ui.spritePixelsPerUnit = ShapeScale;
                    // 9-슬라이스 경계는 .meta 의 spriteBorder 로 들어간다.
                    // 아트가 들어오면 여기서 잡지 말고 인스펙터의 Sprite Editor 로 정한다.
                }
                return;
            }

            if (!path.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;      // 2D 스프라이트에 밉맵은 흐려지기만 한다
            importer.filterMode          = FilterMode.Bilinear;
            importer.wrapMode            = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.maxTextureSize      = MaxTextureSize;
            importer.textureCompression  = TextureImporterCompression.CompressedHQ;

            // 알파 스캔으로 발선을 재려면 CPU 에서 픽셀을 읽을 수 있어야 한다.
            // 몸통만 읽으면 되지만, 파츠가 늘어날 때 빠뜨리기 쉬워 전부 켜 둔다.
            importer.isReadable = true;
        }

        [MenuItem("SnailPet/4. 아트 리임포트", priority = 4)]
        public static void ReimportArt()
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ArtRoot));
            if (!Directory.Exists(full))
            {
                EditorUtility.DisplayDialog("SnailPet", ArtRoot + " 폴더가 없습니다.", "확인");
                return;
            }

            AssetDatabase.ImportAsset(ArtRoot, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot });
            Debug.Log($"[SnailPet] 아트 리임포트 완료. Sprite {guids.Length}장 " +
                      $"(PPU={PixelsPerUnit}, 최대 {MaxTextureSize}px)");
        }
    }
}
