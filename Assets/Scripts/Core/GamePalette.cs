using UnityEngine;

namespace ArrowBlast.Core
{
    public static class GamePalette
    {
        public static readonly Color Red = new Color(1f, 0.23f, 0.23f);     // #FF3B3B
        public static readonly Color Blue = new Color(0f, 0.57f, 1f);    // #0091FF
        public static readonly Color Green = new Color(0.3f, 0.85f, 0.39f);  // #4CD964
        public static readonly Color Yellow = new Color(1f, 0.84f, 0f);    // #FFD700
        public static readonly Color Purple = new Color(0.69f, 0.33f, 0.93f); // #AF52ED
        public static readonly Color Orange = new Color(1f, 0.58f, 0f);    // #FF9500

        public static readonly Color Background = new Color(0.67f, 0.73f, 0.81f); // #AAB9CF
        public static readonly Color SlotEmpty = new Color(0.56f, 0.62f, 0.7f);   // #8E9DB3

        public static Color GetColor(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Red: return Red;
                case BlockColor.Blue: return Blue;
                case BlockColor.Green: return Green;
                case BlockColor.Yellow: return Yellow;
                case BlockColor.Purple: return Purple;
                case BlockColor.Orange: return Orange;
                default: return Color.white;
            }
        }
    }
}
