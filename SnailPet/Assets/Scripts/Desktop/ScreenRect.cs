namespace SnailPet.Desktop
{
    /// <summary>
    /// 화면 영역 (가상 화면 px, y 는 아래로 증가).
    ///
    /// Win32 구조체를 밖으로 노출하지 않기 위한 공개 타입이면서,
    /// 플랫폼 의존이 전혀 없는 순수 사각형이라 어디서든 쓸 수 있다.
    /// </summary>
    public struct ScreenRect
    {
        public int Left, Top, Right, Bottom;
        public int Width  { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }

        public override string ToString()
        {
            return string.Format("x={0}..{1}, y={2}..{3} ({4}x{5})", Left, Right, Top, Bottom, Width, Height);
        }
    }
}
