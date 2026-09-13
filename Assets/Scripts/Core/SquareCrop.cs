namespace CityPuzzle.Core
{
    // Centered square crop rect (in source pixels) for a non-square photo, so slicing/backgrounds
    // never stretch a rectangular photo to fit the game's square board/cards.
    public readonly struct SquareCrop
    {
        public readonly int X, Y, Size;

        public SquareCrop(int x, int y, int size)
        {
            X = x;
            Y = y;
            Size = size;
        }

        public static SquareCrop Centered(int width, int height)
        {
            int size = width < height ? width : height;
            return new SquareCrop((width - size) / 2, (height - size) / 2, size);
        }
    }
}
