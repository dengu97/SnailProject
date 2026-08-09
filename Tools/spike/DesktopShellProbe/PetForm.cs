using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DesktopShellProbe
{
    /// <summary>
    /// per-pixel alpha 레이어드 윈도우. 테두리도 배경도 없고, 클릭은 뒤 창으로 통과된다.
    /// WinForms 를 쓰지만 그리기는 전부 UpdateLayeredWindow 로 하므로,
    /// Unity 에서도 동일한 호출로 같은 결과를 낼 수 있다.
    /// </summary>
    internal sealed class PetForm : Form
    {
        private readonly Bitmap _sprite;

        public PetForm(Bitmap sprite)
        {
            _sprite = sprite;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar   = false;
            TopMost         = true;
            StartPosition   = FormStartPosition.Manual;
            Size            = sprite.Size;
            Text            = "SnailPet(Probe)";
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // 창이 만들어지는 시점에 확장 스타일을 심어야 깜빡임 없이 적용된다
                cp.ExStyle |= Win32.WS_EX_LAYERED
                            | Win32.WS_EX_TRANSPARENT
                            | Win32.WS_EX_TOOLWINDOW
                            | Win32.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Win32.SetWindowPos(Handle, Win32.HWND_TOPMOST, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
            Redraw();
        }

        /// <summary>비트맵을 창에 그대로 밀어 넣는다 (알파 채널 보존).</summary>
        public void Redraw()
        {
            IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
            IntPtr memDc    = Win32.CreateCompatibleDC(screenDc);
            IntPtr hBitmap  = IntPtr.Zero;
            IntPtr oldBmp   = IntPtr.Zero;

            try
            {
                // 배경을 완전 투명으로 주면 premultiplied ARGB DIB 가 나온다
                hBitmap = _sprite.GetHbitmap(Color.FromArgb(0));
                oldBmp  = Win32.SelectObject(memDc, hBitmap);

                var size    = new Win32.SIZE(_sprite.Width, _sprite.Height);
                var srcLoc  = new Win32.POINT(0, 0);
                var dstLoc  = new Win32.POINT(Left, Top);
                var blend   = new Win32.BLENDFUNCTION
                {
                    BlendOp             = Win32.AC_SRC_OVER,
                    BlendFlags          = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat         = Win32.AC_SRC_ALPHA
                };

                Win32.UpdateLayeredWindow(Handle, screenDc, ref dstLoc, ref size,
                    memDc, ref srcLoc, 0, ref blend, Win32.ULW_ALPHA);
            }
            finally
            {
                Win32.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    Win32.SelectObject(memDc, oldBmp);
                    Win32.DeleteObject(hBitmap);
                }
                Win32.DeleteDC(memDc);
            }
        }

        /// <summary>표면의 왼쪽 끝에서 오른쪽 끝까지 기어간다. 달팽이답게 느리게.</summary>
        public void WalkAlong(Surface surface, int seconds)
        {
            int y = surface.Top - Height + 12;          // 발이 테두리에 살짝 걸치도록
            int xStart = surface.Left;
            int xEnd   = Math.Max(surface.Left, surface.Right - Width);

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < seconds)
            {
                double t = sw.Elapsed.TotalSeconds / seconds;
                Left = (int)(xStart + (xEnd - xStart) * t);
                Top  = y;
                Redraw();
                Application.DoEvents();
                System.Threading.Thread.Sleep(16);      // ~60fps
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _sprite?.Dispose();
            base.Dispose(disposing);
        }
    }
}
