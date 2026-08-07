using Microsoft.Xna.Framework;

namespace StardewWordle
{
     class WordleTheme
    {
        public bool DARK = false;
        public Color BACKGROUND = Color.White;
        public Color TILE_GRAY = new Color(120, 124, 128);
        public Color TILE_YELLOW = new Color(196, 173, 85);
        public Color TILE_GREEN = new Color(103, 168, 92);
        public Color KEYBOARD_BG = new Color(211, 214, 219);
        public Color KEYBOARD_TEXT = Color.Black;
        public Color KEYBOARD_ACTIVE_TEXT = Color.White;
        public Color ACTIVE_TILE_BORDER = new Color(120, 124, 128);
        public Color INACTIVE_TILE_BORDER = new Color(211, 214, 219);
        public Color ACTIVE_TILE_TEXT = Color.Black;
        public Color INACTIVE_TILE_TEXT = Color.White;
        public Color HEADER_COLOR = Color.Black;

        public WordleTheme(bool Dark = false)
        {
            if(Dark)
            { 
                BACKGROUND = new Color(18,18,18);
                TILE_GRAY = new Color(58,58,60);
                TILE_GREEN = new Color(82,141,77);
                TILE_YELLOW = new Color(181,159,59);
                KEYBOARD_BG = new Color(130,131,133);
                KEYBOARD_TEXT = Color.White;
                KEYBOARD_ACTIVE_TEXT = Color.White;
                ACTIVE_TILE_BORDER = new Color(130,131,133);
                INACTIVE_TILE_BORDER = new Color(58,58,60);
                ACTIVE_TILE_TEXT = Color.White;
                INACTIVE_TILE_TEXT = Color.White;
                HEADER_COLOR = Color.White;
            }
        }
    }
}