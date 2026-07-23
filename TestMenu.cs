using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;


namespace StardewWordle
{
    public class TestMenu : IClickableMenu
    {
        private ClickableTextureComponent okButton;
        private IMonitor Monitor;
        private IModHelper helper;
        public static int menuWidth = 650 + borderWidth * 2;
        public static int menuHeight = 700 + borderWidth * 2 + Game1.tileSize;
        private Rectangle[] GridRectangles;
        private Dictionary<char, Rectangle> KeyboardMap;
        private ModData model;
        private TimeSpan gridAnimStart = TimeSpan.Zero;
        private int gridAnimCount = -1;
        private static TimeSpan GRID_ANIM_INTERVAL = TimeSpan.FromMilliseconds(300);
        private static Color YELLOW = new Color(196, 173, 85);
        private static Color GREEN = new Color(103, 168, 92);
        private static Color GRAY = new Color(120, 124, 128);
        private static Color LIGHTGRAY = new Color(211, 214, 219);

        
        public TestMenu(IModHelper helper, IMonitor monitor) :  base((int)getAppropriateMenuPosition().X, (int)getAppropriateMenuPosition().Y, menuWidth , menuHeight)
        {
            this.helper = helper;
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
            int width = Game1.tileSize * 6 / 8;
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

        public override void update(GameTime gameTime)
        {
            if(gridAnimCount == 0 && gridAnimStart == TimeSpan.Zero)
            {
                gridAnimStart = gameTime.TotalGameTime;
            } else if ( gridAnimStart + 5 * GRID_ANIM_INTERVAL < gameTime.TotalGameTime)
            {
                gridAnimCount = -1;
                gridAnimStart = TimeSpan.Zero;
                return;
            }

            if(gridAnimCount != -1)
            {
                for(int i = 0; i < 5; i++)
                {
                    if (gameTime.TotalGameTime > gridAnimStart + ( i * GRID_ANIM_INTERVAL ) && i > gridAnimCount)
                    {
                        gridAnimCount = i;
                        if(i == 4 && inWinState())
                        {
                            Game1.playSound("powerup", null);
                        } else if (i == 4 && InLoseState()) {
                            Game1.playSound("death", null);   
                        }
                        else
                        {
                            Game1.playSound("machine_bell", null);
                        }
                        break;
                    }
                }
            }
        }

        public new void exitThisMenu(bool playSound = true)
        {
            base.exitThisMenu(playSound);
            this.gridAnimCount = -1;
            this.gridAnimStart = TimeSpan.Zero;
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
                model.Guesses[model.Guesses.Count-1] += key.ToUpper();
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
            if(gridAnimCount != -1)
            {
                return;
            }
            String lastGuess = model.Guesses.Last();
            if(lastGuess.Length == 5)
            {
                if (model.PossibleGuesses.Contains(lastGuess.ToLower()))
                {
                    Game1.playSound("crit", null);
                    updateColors();       
                    gridAnimCount = 0;
                    if (lastGuess.EqualsIgnoreCase(getWordOfDay()))
                    {
                        model.State = WordleState.WON;
                    } else if(model.Guesses.Count() == 5)
                    {
                        model.State = WordleState.LOST;
                    }
                    else
                    {
                        model.Guesses.Add(""); // Start new guess
                    }
                    this.helper.Data.WriteGlobalData("wordle-data", model);
                } else
                {
                    // not in word Bank
                    Monitor.Log("Not in word bank.", LogLevel.Debug);
                    Game1.playSound("fishEscape", null);
                }
            }   
        }

        private void updateColors()
        {
            String guess = model.Guesses.Last();
            Color[] guessColors = DetermineGridBgColor(guess);
            for(int i = 0; i < 5; i++)
            {
                model.Colors[model.Guesses.Count()-1, i] = guessColors[i];
            }
            this.helper.Data.WriteGlobalData("wordle-data", model);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (inPlayingState())
            {     
                if (key != Keys.None && key.ToString().Length == 1 && "ZXCVBNMASDFGHJKLQWERTYUIOP".Contains(key.ToString()))
                {
                    inputLetter(key.ToString());
                    Game1.playSound("smallSelect", null);
                }

                if(key == Keys.Back)
                {
                    removeLetter();
                    Game1.playSound("clubhit", null);
                }

                if(key == Keys.Enter)
                {
                    submitGuess();
                }
            } else {
                if(key == Keys.Enter)
                {
                    exitThisMenu();
                }
            }

            if(key == Keys.Escape)
            {
                exitThisMenu();
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
                if( model.Guesses.Count() > i / 5 && model.Guesses[ i / 5].Length > i % 5)
                {
                    String guess = model.Guesses[ i / 5];
                    String letter = guess[i % 5].ToString();
                    Color bgColor = Color.White;
                    if(!inPlayingState() || (inPlayingState() && i / 5 != model.Guesses.Count-1))
                    {
                        bgColor = model.Colors[i / 5, i % 5];
                    }
                    if (gridAnimCount != -1 && i / 5 == model.Guesses.Count - (inPlayingState() ? 2 : 1))
                    {
                        if(i % 5 > gridAnimCount)
                        {
                            bgColor = Color.White;
                        }
                    }
                    Utility.DrawSquare(b, square, 2, bgColor, bgColor);
                    Vector2 letterSize = Game1.dialogueFont.MeasureString(letter);
                    Vector2 letterPos = new Vector2(
                        square.X + (square.Width - letterSize.X) / 2f,
                        square.Y + (square.Height - letterSize.Y) / 2f
                    );
                    Utility.drawBoldText(b, letter, Game1.dialogueFont, letterPos, bgColor == Color.White ? Color.Black : Color.White);
                } else
                {
                    Utility.DrawSquare(b, square, 2, Color.White, Color.White);
                }
            }

            if ( inPlayingState() || (!inPlayingState() && gridAnimCount != -1))
            {    
                foreach(char key in this.KeyboardMap.Keys)
                {
                    Color bgColor = DetermineKeyBgColor(key);
                    Rectangle rect = KeyboardMap.GetValueOrDefault(key);
                    Utility.DrawSquare(b, rect, 2, bgColor, bgColor);
                    Vector2 letterSize = Game1.smallFont.MeasureString(key.ToString());
                    Vector2 letterPos = new Vector2(
                        rect.X + (rect.Width - letterSize.X) / 2f,
                        rect.Y + (rect.Height - letterSize.Y) / 2f
                    );
                    Utility.drawBoldText(b, key.ToString(), Game1.smallFont, letterPos, bgColor == LIGHTGRAY ? Color.Black : Color.White);
                }
            }

            drawMouse(b);
        }

        private Color[] DetermineGridBgColor(String guess)
        {
            String correctWord = getWordOfDay().ToUpper();
            Color[] colors = [GRAY,GRAY,GRAY,GRAY,GRAY];
            
            Dictionary<char,int> remainingCounts = new Dictionary<char, int>();
            for(int i = 0; i < correctWord.Length; i++ )
            {
                if(remainingCounts.ContainsKey(correctWord[i]))
                {
                    remainingCounts[correctWord[i]] = remainingCounts[correctWord[i]] + 1;
                } else
                {   
                    remainingCounts[correctWord[i]] = 1;
                }
            }

            for(int i = 0; i < guess.Length; i++)
            {
                if(guess[i] == correctWord[i])
                {
                    Monitor.Log("Matched " + guess[i], LogLevel.Debug);
                    colors[i] = GREEN;
                    remainingCounts[guess[i]]--;
                }
            }


            for(int i = 0; i < guess.Length; i++)
            {
                if(guess[i] != correctWord[i] && correctWord.Contains(guess[i]) && remainingCounts[guess[i]] > 0)
                {
                    colors[i] = YELLOW;
                    remainingCounts[guess[i]]--;
                }
            }
            return colors;
        }

        private Color DetermineKeyBgColor(char key)
        {
            String correctWord = getWordOfDay().ToUpper();
            Color returnColor = LIGHTGRAY;

            for(int i = 0; i < model.Guesses.Count - 1; i++)
            {
                String guess = model.Guesses[i];
                for(int j = 0; j < guess.Length;  j++)
                {
                    if(i == model.Guesses.Count - (inPlayingState() ? 2 : 1) && gridAnimCount != -1 && j > gridAnimCount)
                    {
                        continue;
                    }
                    if(guess[j] == key)
                    {                        
                        if(guess[j] == correctWord[j])
                        {
                            return GREEN;
                        } else if(correctWord.Contains(guess[j].ToString()))
                        {
                            returnColor = YELLOW;
                        } else
                        {
                            returnColor = GRAY;
                        }
                    }
                }
            }
            return returnColor;
        }

        private bool inWinState()
        {
            return model.State == WordleState.WON;
        }

        private bool inPlayingState()
        {
            return model.State == WordleState.PLAYING;
        }

        private bool InLoseState()
        {
            return model.State == WordleState.LOST;
        }

        private int[] getAllIndices(String target, char letter)
        {
            List<int> indices = new List<int>();
            for(int i = 0; i < target.Length; i++)
            {
                if (target[i].Equals(letter))
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