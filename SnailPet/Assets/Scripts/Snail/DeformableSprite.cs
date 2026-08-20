using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 스프라이트 한 장을 격자 메시로 깔아, 정점을 직접 밀어 변형할 수 있게 한 것.
    ///
    /// SpriteRenderer 는 사각형 네 점이라 <b>휠 수가 없다</b>. 발바닥이 모서리를 감싸거나
    /// 물결이 지나가려면 그 사이에 점이 있어야 해서 직접 메시를 만든다.
    ///
    /// 그리는 순서는 SpriteRenderer 와 똑같이 sortingOrder 로 정해지고,
    /// SortingGroup 도 Renderer 종류를 가리지 않으므로 합성 규칙은 그대로다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class DeformableSprite : MonoBehaviour
    {
        /// <summary>
        /// 격자 해상도. 가로는 발바닥이 모서리에서 꺾이는 곡선을 담아야 해서 넉넉히 준다.
        /// 세로는 발바닥 가중치가 부드럽게 떨어지기만 하면 되므로 적어도 된다.
        /// </summary>
        public const int Cols = 24;
        public const int Rows = 16;

        private static readonly Dictionary<Texture, Material> _materials = new Dictionary<Texture, Material>();

        private Mesh _mesh;
        private Vector3[] _rest;      // 변형 전 로컬 좌표
        private Vector3[] _work;      // 매 프레임 여기에 써서 넘긴다
        private Vector2[] _uv;        // 애니메이션 파츠는 칸이 바뀔 때 여기만 다시 쓴다

        /// <summary>이 파츠가 발바닥 변형을 받는가. 껍질처럼 단단한 것은 false.</summary>
        public bool Soft = true;

        public static DeformableSprite Create(Transform parent, Sprite sprite, int sortingOrder, string name)
        {
            if (sprite == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var self = go.AddComponent<DeformableSprite>();
            self.Build(sprite, sortingOrder);
            return self;
        }

        private void Build(Sprite sprite, int sortingOrder)
        {
            // sprite.bounds 는 메시 타입(Tight/FullRect)에 따라 달라지므로 쓰지 않는다.
            // rect·pivot·PPU 로 직접 계산하면 임포트 설정과 무관하게 항상 맞는다.
            var r = sprite.rect;
            float ppu = Mathf.Max(0.0001f, sprite.pixelsPerUnit);
            Vector2 pivot = sprite.pivot;                       // rect 좌하단 기준 px

            float x0 = -pivot.x / ppu, y0 = -pivot.y / ppu;
            float x1 = x0 + r.width / ppu, y1 = y0 + r.height / ppu;

            var tex = sprite.texture;
            int vx = Cols + 1, vy = Rows + 1;

            _rest = new Vector3[vx * vy];
            _work = new Vector3[vx * vy];
            var uv = _uv = new Vector2[vx * vy];

            for (int j = 0; j < vy; j++)
            {
                float fv = j / (float)Rows;
                for (int i = 0; i < vx; i++)
                {
                    float fu = i / (float)Cols;
                    int k = j * vx + i;
                    _rest[k] = new Vector3(Mathf.Lerp(x0, x1, fu), Mathf.Lerp(y0, y1, fv), 0f);
                    uv[k] = new Vector2((r.x + r.width * fu) / tex.width,
                                        (r.y + r.height * fv) / tex.height);
                }
            }

            var tris = new int[Cols * Rows * 6];
            int t = 0;
            for (int j = 0; j < Rows; j++)
            {
                for (int i = 0; i < Cols; i++)
                {
                    int a = j * vx + i, b = a + 1, c = a + vx, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            }

            _mesh = new Mesh { name = name + "_mesh" };
            _mesh.MarkDynamic();
            _mesh.vertices = _rest;
            _mesh.uv = uv;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = _mesh;

            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialFor(tex);
            mr.sortingOrder = sortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }

        /// <summary>
        /// 애니메이션 시트의 다른 칸으로 갈아 끼운다.
        ///
        /// 칸끼리 크기·피벗이 같으므로 정점은 그대로 두고 UV 만 옮긴다 — 변형 중이어도
        /// 그림만 바뀌고 모양은 안 흔들린다. 같은 텍스처라 머티리얼도 안 건드린다.
        /// </summary>
        public void SetFrame(Sprite frame)
        {
            if (_mesh == null || _uv == null || frame == null) return;

            var r = frame.rect;
            var tex = frame.texture;
            if (tex == null) return;

            int vx = Cols + 1, vy = Rows + 1;
            for (int j = 0; j < vy; j++)
            {
                float fv = j / (float)Rows;
                for (int i = 0; i < vx; i++)
                {
                    float fu = i / (float)Cols;
                    _uv[j * vx + i] = new Vector2((r.x + r.width * fu) / tex.width,
                                                  (r.y + r.height * fv) / tex.height);
                }
            }
            _mesh.uv = _uv;
        }

        /// <summary>텍스처가 같으면 머티리얼을 공유한다. 그리는 순서는 Renderer 쪽에 있으므로 안전하다.</summary>
        private static Material MaterialFor(Texture tex)
        {
            if (_materials.TryGetValue(tex, out var m) && m != null) return m;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[SnailPet] Sprites/Default 셰이더를 찾지 못했습니다. " +
                                 "Graphics 설정의 Always Included Shaders 를 확인하세요.");
                shader = Shader.Find("Unlit/Transparent");
            }

            m = new Material(shader) { mainTexture = tex };
            _materials[tex] = m;
            return m;
        }

        /// <summary>변형을 정점에 적용한다.</summary>
        public void Apply(SnailDeform d)
        {
            if (_mesh == null) return;

            for (int i = 0; i < _rest.Length; i++)
            {
                var p = _rest[i];
                Vector2 q = d.Apply(new Vector2(p.x, p.y));
                _work[i] = new Vector3(q.x, q.y, 0f);
            }

            _mesh.SetVertices(_work);
            _mesh.RecalculateBounds();
        }

        /// <summary>씬을 갈아엎을 때 공유 머티리얼이 파괴된 텍스처를 물고 있지 않게 한다.</summary>
        public static void ClearMaterialCache() => _materials.Clear();
    }
}
