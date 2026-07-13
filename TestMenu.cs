using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Enchantments;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Tools;
using xTile;


namespace StardewWordle
{
    public class TestMenu : IClickableMenu
    {
        private ClickableTextureComponent okButton;
        private ClickableComponent testLabel;
        private String wordOfDay;
        private IModHelper helper;
        public static int menuWidth = 650 + borderWidth * 2;
        public static int menuHeight = 700 + borderWidth * 2 + Game1.tileSize;
        private Rectangle[] GridRectangles;
        private Dictionary<char, Rectangle> KeyboardMap;

        
        public TestMenu(IModHelper helper) :  base((int)getAppropriateMenuPosition().X, (int)getAppropriateMenuPosition().Y, menuWidth , menuHeight)
        {
            this.helper = helper;
            this.wordOfDay = getWordOfDay();

            this.GridRectangles = initGrid();
            this.KeyboardMap = initKeyboard();

            testLabel = (new ClickableComponent(new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4 + spaceToClearSideBorder + borderWidth + Game1.tileSize * 3 + 8, this.yPositionOnScreen + borderWidth + spaceToClearTopBorder - Game1.tileSize / 8, 20, 5), wordOfDay));
            this.okButton = new ClickableTextureComponent("OK", new Rectangle(this.xPositionOnScreen + this.width - borderWidth - spaceToClearSideBorder - Game1.tileSize, this.yPositionOnScreen + this.height - borderWidth - spaceToClearTopBorder + Game1.tileSize / 4, Game1.tileSize, Game1.tileSize), "", null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46), 1f);
        }

        private Rectangle[] initGrid()
        {
            Rectangle[] grid = new Rectangle[25];
            int width = Game1.tileSize;
            int margin = 4;
            int rowStartX = this.xPositionOnScreen + (this.width - (width * 5)) / 2;
            for( int i = 0; i < 25; i++ )
            {
                int xPos = rowStartX + (i % 5) * width + (i % 5 * margin);
                int yPos = this.yPositionOnScreen + borderWidth + spaceToClearTopBorder + (width * 2) + (((i / 5)-1) * margin) + (((i / 5)-1) * width);
                grid[i] = new Rectangle(xPos, yPos, width, width);
            }
            return grid;
        }

        private Dictionary<char, Rectangle> initKeyboard()
        {
            Dictionary<char, Rectangle> map = new Dictionary<char, Rectangle>();
            string[] rows = [ "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" ];
            int width = Game1.tileSize * 5 / 8;
            int margin = 4;
            for(int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                int rowWidth = row.Length * width + row.Length * margin;
                int rowStartX = this.xPositionOnScreen + (this.width - rowWidth)/ 2;
                for(int j = 0; j < row.Length; j++)
                {
                    int xPos = rowStartX + (j * width) + (j * margin);
                    int yPos = this.yPositionOnScreen + (height - (width * 5)) + (i * width) + (i * margin);                    
                    map.Add(row[j], new Rectangle(xPos, yPos, width, width));
                }
            }
            return map;
        }

        public static Vector2 getAppropriateMenuPosition()
        {
            Vector2 defaultPosition = new Vector2(Game1.viewport.Width / 2 - menuWidth / 2, (Game1.viewport.Height / 2 - menuHeight / 2));

            //Force the viewport into a position that it should fit into on the screen???
            if (defaultPosition.X + menuWidth > Game1.viewport.Width)
            {
                defaultPosition.X = 0;
            }

            if (defaultPosition.Y + menuHeight > Game1.viewport.Height)
            {
                defaultPosition.Y = 0;
            }
            return defaultPosition;

        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            Utility.drawTextWithShadow(b, "Word of Day:" + testLabel.name, Game1.smallFont, new Vector2(testLabel.bounds.X, testLabel.bounds.Y), Game1.textColor);
            okButton.draw(b);

            foreach(Rectangle square in this.GridRectangles)
            {
                Utility.DrawSquare(b, square, 2, Color.White, Color.White);
            }

            foreach( char key in this.KeyboardMap.Keys)
            {
                Rectangle rect = KeyboardMap.GetValueOrDefault(key);
                Utility.DrawSquare(b, rect ,2, Color.White, Color.White);
                Utility.drawBoldText(b, key.ToString(), Game1.dialogueFont, new Vector2(rect.X, rect.Y), Game1.textColor);
            }
            drawMouse(b);
        }
        private string getWordOfDay()
        {
            var model = this.helper.Data.ReadGlobalData<ModData>("wordle-data");
            if(model != null)
            {
                return model.WordOfDay;
            }
            return "";
        }
    }
}