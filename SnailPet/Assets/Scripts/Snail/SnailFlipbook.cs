using System.Collections.Generic;
using UnityEngine;

namespace SnailPet.Snail
{
    /// <summary>
    /// 애니메이션 파츠를 돌린다.
    ///
    /// 합성된 달팽이 <b>한 마리에 하나</b> 붙어서 그 마리의 시트 파츠를 전부 넘긴다.
    /// 여기에 두면 내 달팽이든 손님이든 초상이든 세운 쪽이 아무것도 안 해도 알아서 돈다.
    ///
    /// 칸을 넘기는 비용은 거의 없다. 말랑한 파츠는 메시의 UV 만 다시 쓰고(정점은 그대로),
    /// 단단한 파츠는 스프라이트만 갈아 끼운다. 칸끼리 같은 텍스처라 머티리얼도 그대로다.
    /// </summary>
    public sealed class SnailFlipbook : MonoBehaviour
    {
        /// <summary>돌아가는 파츠 하나.</summary>
        public sealed class Reel
        {
            public Sprite[] Frames;

            /// <summary>둘 중 하나만 채워진다 — 말랑한 파츠인가 단단한 파츠인가.</summary>
            public DeformableSprite Soft;
            public SpriteRenderer Rigid;

            public float Fps = SnailComposer.DefaultFps;

            /// <summary>지금까지 흐른 시간. 파츠마다 따로 세서 속도를 다르게 줄 수 있다.</summary>
            public float Time;

            /// <summary>지금 보이고 있는 칸. 안 바뀌었으면 아무것도 안 한다.</summary>
            public int Shown = 0;
        }

        private readonly List<Reel> _reels = new List<Reel>();

        /// <summary>
        /// 이 마리에 시트 파츠를 하나 건다. 컴포넌트가 없으면 붙이면서 시작한다.
        /// 시작 칸을 조금씩 어긋나게 두어, 같은 파츠를 여러 마리가 써도 한 몸처럼 깜빡이지 않는다.
        /// </summary>
        public static Reel Play(GameObject root, Sprite[] frames, DeformableSprite soft, SpriteRenderer rigid)
        {
            var book = root.GetComponent<SnailFlipbook>() ?? root.AddComponent<SnailFlipbook>();

            var reel = new Reel
            {
                Frames = frames,
                Soft = soft,
                Rigid = rigid,
                Time = Random.value * frames.Length / SnailComposer.DefaultFps,
            };

            book._reels.Add(reel);
            return reel;
        }

        private void Update()
        {
            float dt = UnityEngine.Time.deltaTime;

            foreach (var r in _reels)
            {
                if (r.Frames == null || r.Frames.Length < 2 || r.Fps <= 0f) continue;

                r.Time += dt;

                int frame = (int)(r.Time * r.Fps) % r.Frames.Length;
                if (frame == r.Shown) continue;

                r.Shown = frame;
                if (r.Soft != null) r.Soft.SetFrame(r.Frames[frame]);
                else if (r.Rigid != null) r.Rigid.sprite = r.Frames[frame];
            }
        }
    }
}
