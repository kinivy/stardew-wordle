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
using StardewModdingAPI.Utilities;


namespace StardewWordle
{
    public class TestMenu : IClickableMenu
    {
        private ClickableTextureComponent okButton;
        private IMonitor Monitor;
        private String wordOfDay;
        private IModHelper helper;
        public static int menuWidth = 650 + borderWidth * 2;
        public static int menuHeight = 700 + borderWidth * 2 + Game1.tileSize;
        private Rectangle[] GridRectangles;
        private Dictionary<char, Rectangle> KeyboardMap;
        private ModData model;

        
        public TestMenu(IModHelper helper, IMonitor monitor) :  base((int)getAppropriateMenuPosition().X, (int)getAppropriateMenuPosition().Y, menuWidth , menuHeight)
        {
            this.helper = helper;
            this.wordOfDay = getWordOfDay();
            this.Monitor = monitor;

            this.model = this.helper.Data.ReadGlobalData<ModData>("wordle-data");

            this.GridRectangles = initGrid();
            this.KeyboardMap = initKeyboard();

            Monitor.Log(getWordOfDay(), LogLevel.Debug);

            Game1.keyboardDispatcher.Subscriber = new TextBox(null,null,Game1.smallFont,Color.Black);

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

        private void inputLetter(String key)
        {
            if (model.Guesses[model.Guesses.Count-1].Length == 5)
            {
                return;
            } 
            else
            {
                model.Guesses[model.Guesses.Count-1] += key;
            }
            this.helper.Data.WriteGlobalData("wordle-data", model);
        }

        private void removeLetter()
        {
            String guess = model.Guesses[model.Guesses.Count-1];
            if(guess.Length > 0)
            {
                model.Guesses[model.Guesses.Count-1]= model.Guesses[model.Guesses.Count-1][..^1];
                this.helper.Data.WriteGlobalData("wordle-data", model);
            }
        }

        private void submitGuess()
        {
            String lastGuess = model.Guesses[model.Guesses.Count() - 1];
            Monitor.Log("Guess: " + lastGuess, LogLevel.Debug);
            if(lastGuess.Length == 5)
            {
                if (model.PossibleGuesses.Contains(lastGuess.ToLower()))
                {
                    model.Guesses.Add("");
                    this.helper.Data.WriteGlobalData("wordle-data", model);
                } else
                {
                    Monitor.Log("Not in word bank.", LogLevel.Debug);
                    // not in word Bank
                }
            }             
        }

        public override void receiveKeyPress(Keys key)
        {
            Monitor.Log(key.ToString());
            if (key != Keys.None && key.ToString().Length == 1 && "ZXCVBNMASDFGHJKLQWERTYUIOP".Contains(key.ToString()))
            {
                inputLetter(key.ToString());
            }
            if(key == Keys.Escape)
            {
                exitThisMenu();
            }

            if(key == Keys.Back)
            {
                removeLetter();
            }

            if(key == Keys.Enter)
            {
                submitGuess();
            }
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            okButton.draw(b);

            for(int i = 0; i < GridRectangles.Length; i++)
            {
                Rectangle square = GridRectangles[i];
                if( model.Guesses.Count() > i / 5)
                {
                    String guess = model.Guesses[ i / 5];
                    if(guess.Length > i % 5)
                    {
                        String letter = guess[i % 5].ToString();
                        Color bgColor = i / 5 != model.Guesses.Count-1 ? DetermineGridBgColor(letter.ToLower(), i % 5) : Color.White;
                        Utility.DrawSquare(b, square, 2, bgColor, bgColor);
                        Utility.drawBoldText(b, letter, Game1.dialogueFont, new Vector2(square.X, square.Y), Game1.textColor);
                    } else
                    {
                        Utility.DrawSquare(b, square, 2, Color.White, Color.White);
                    }
                } else
                {
                    Utility.DrawSquare(b, square, 2, Color.White, Color.White);
                }
            }

            foreach( char key in this.KeyboardMap.Keys)
            {
                Color bgColor = DetermineKeyBgColor(key);
                Rectangle rect = KeyboardMap.GetValueOrDefault(key);
                Utility.DrawSquare(b, rect ,2, bgColor, bgColor);
                Utility.drawBoldText(b, key.ToString(), Game1.dialogueFont, new Vector2(rect.X, rect.Y), Game1.textColor);
            }
            drawMouse(b);
        }

        private Color DetermineGridBgColor(String letter, int index)
        {
            String correctWord = getWordOfDay();
            if (correctWord.IndexOf(letter) != -1)
            {
                for( int i = correctWord.IndexOf(letter); i < correctWord.Length; i++)
                {
                    if(correctWord[i].ToString() == letter && i == index)
                    {
                        return Color.Green;
                    }
                }
                
                return Color.Yellow;
            } else
            {
                return Color.White;
            }
        }

        private Color DetermineKeyBgColor(char key)
        {
            String correctWord = getWordOfDay();
            Color returnColor = Color.White;

            for(int i = 0; i < model.Guesses.Count; i++)
            {
                String guess = model.Guesses[i];
                for(int j = 0; j < guess.Length;  j++)
                {
                    if(guess[j] == key)
                    {                        
                        if(guess[j] == correctWord[j])
                        {
                            return Color.Green;
                        } else if(correctWord.Contains(guess[j].ToString()))
                        {
                            returnColor = Color.Yellow;
                        }
                    }
                }
            }
            return returnColor;
        }

        private int[] getAllIndices(String target, String letter)
        {
            List<int> indices = new List<int>();
            for(int i = 0; i < target.Length; i++)
            {
                if (target[i].ToString().Equals(letter))
                {
                    indices.Add(i);
                }
            }
            return indices.ToArray();
        }

        private string getWordOfDay()
        {
            if(model != null)
            {
                return model.WordOfDay;
            }
            return "";
        }
    }
}