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
        private WordleSaveData saveModel;
        private WordleDictionaryData dictionaryModel;
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
            this.saveModel = this.helper.Data.ReadSaveData<WordleSaveData>("wordle-save-data");
            this.dictionaryModel = this.helper.Data.ReadGlobalData<WordleDictionaryData>("wordle-dictionary-data");

            this.GridRectangles = initGrid();
            this.KeyboardMap = initKeyboard();

            Monitor.Log(getWordOfWeek(), LogLevel.Debug);

            Game1.keyboardDispatcher.Subscriber = new TextBox(null,null,Game1.smallFont,Color.Black);
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
                int yPos = this.yPositionOnScreen + borderWidth + spaceToClearTopBorder + (width * 1) + (((i / 5)-1) * margin) + (((i / 5)-1) * width);
                grid[i] = new Rectangle(xPos, yPos, width, width);
            }
            return grid;
        }

        private Dictionary<char, Rectangle> initKeyboard()
        {
            Dictionary<char, Rectangle> map = new Dictionary<char, Rectangle>();
            string[] rows = [ "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" ];
            int width = Game1.tileSize * 6 / 8;
            int height = width * 3 / 2;
            int margin = 4;
            for(int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                int rowWidth = row.Length * width + row.Length * margin;
                int rowStartX = this.xPositionOnScreen + (this.width - rowWidth)/ 2;
                for(int j = 0; j < row.Length; j++)
                {
                    int xPos = rowStartX + (j * width) + (j * margin);
                    int yPos = this.yPositionOnScreen + (this.height - ( height * 4)) + (i * height) + (i * margin);
                    
                    map.Add(row[j], new Rectangle(xPos, yPos, width, height));
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
            this.gridAnimCount = -1;
            this.gridAnimStart = TimeSpan.Zero;
            base.exitThisMenu(playSound);
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
            if (saveModel.Guesses[saveModel.Guesses.Count-1].Length == 5)
            {
                return;
            } 
            else
            {
                saveModel.Guesses[saveModel.Guesses.Count-1] += key.ToUpper();
            }
            this.helper.Data.WriteSaveData("wordle-save-data", saveModel);
        }

        private void removeLetter()
        {
            String guess = saveModel.Guesses[saveModel.Guesses.Count-1];
            if(guess.Length > 0)
            {
                saveModel.Guesses[saveModel.Guesses.Count-1]= saveModel.Guesses[saveModel.Guesses.Count-1][..^1];
                this.helper.Data.WriteSaveData("wordle-save-data", saveModel);
            }
        }

        private void submitGuess()
        {
            if(gridAnimCount != -1)
            {
                return;
            }
            String lastGuess = saveModel.Guesses.Last();
            if(lastGuess.Length == 5)
            {
                if (dictionaryModel.PossibleGuesses.Contains(lastGuess.ToLower()))
                {
                    Game1.playSound("crit", null);
                    updateColors();       
                    gridAnimCount = 0;
                    if (lastGuess.EqualsIgnoreCase(getWordOfWeek()))
                    {
                        saveModel.State = WordleState.WON;
                        saveModel.TotalWins++;
                        int reward = (int) (500 * Math.Pow(1.25, saveModel.Streak));
                        Game1.player.addUnearnedMoney(reward);
                        saveModel.Streak++;
                        saveModel.HasWonThisWeek = true;
                        Monitor.Log("Total Wins: " + saveModel.TotalWins, LogLevel.Debug);
                        Monitor.Log("Streak: " + saveModel.Streak, LogLevel.Debug);
                    } else if(saveModel.Guesses.Count() == 5)
                    {
                        saveModel.State = WordleState.LOST;
                        saveModel.Streak = 0;
                        Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
                    }
                    else
                    {
                        saveModel.Guesses.Add(""); // Start new guess
                    }
                    this.helper.Data.WriteSaveData("wordle-save-data", saveModel);
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
            String guess = saveModel.Guesses.Last();
            Color[] guessColors = DetermineGridBgColor(guess);
            for(int i = 0; i < 5; i++)
            {
                saveModel.Colors[saveModel.Guesses.Count()-1, i] = guessColors[i];
            }
            this.helper.Data.WriteSaveData("wordle-save-data", saveModel);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            if(inPlayingState())
            {
                foreach(char key in KeyboardMap.Keys)
                {
                    if (KeyboardMap[key].Contains(x, y))
                    {
                        inputLetter(key.ToString());
                        Game1.playSound("smallSelect", null);
                    }
                }
            }
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

                if(key == Keys.Escape)
                {
                    exitThisMenu();
                }
            } else {
                exitThisMenu();
            }

        }

        public void drawGrid(SpriteBatch b)
        {
            for(int i = 0; i < GridRectangles.Length; i++)
            {
                Rectangle square = GridRectangles[i];
                if( saveModel.Guesses.Count() > i / 5 && saveModel.Guesses[ i / 5].Length > i % 5)
                {
                    String guess = saveModel.Guesses[ i / 5];
                    String letter = guess[i % 5].ToString();
                    Color bgColor = Color.White;
                    if(!inPlayingState() || (inPlayingState() && i / 5 != saveModel.Guesses.Count-1))
                    {
                        bgColor = saveModel.Colors[i / 5, i % 5];
                    }
                    if (gridAnimCount != -1 && i / 5 == saveModel.Guesses.Count - (inPlayingState() ? 2 : 1))
                    {
                        if(i % 5 > gridAnimCount)
                        {
                            bgColor = Color.White;
                        }
                    }
                    Utility.DrawSquare(b, square, 4, bgColor == Color.White ? GRAY : bgColor, bgColor);
                    Vector2 letterSize = Game1.dialogueFont.MeasureString(letter);
                    Vector2 letterPos = new Vector2(
                        square.X + (square.Width - letterSize.X) / 2f,
                        square.Y + (square.Height - letterSize.Y) / 2f
                    );
                    Utility.drawBoldText(b, letter, Game1.dialogueFont, letterPos, bgColor == Color.White ? Color.Black : Color.White);
                } else
                {
                    Utility.DrawSquare(b, square, 4, LIGHTGRAY, Color.White);
                }
            }
        }

        private void drawKeyboard(SpriteBatch b)
        {
            foreach(char key in this.KeyboardMap.Keys)
            {
                Color bgColor = DetermineKeyBgColor(key);
                Rectangle rect = KeyboardMap.GetValueOrDefault(key);
                Utility.DrawSquare(b, rect, 2, bgColor, bgColor);
                String letter = key.ToString();
                Vector2 letterSize = Game1.smallFont.MeasureString(letter);
                Vector2 letterPos = new Vector2(
                    rect.X + (rect.Width - letterSize.X) / 2f,
                    rect.Y + (rect.Height - letterSize.Y) / 2f
                );
                Utility.drawBoldText(b, letter, Game1.smallFont, letterPos, bgColor == LIGHTGRAY ? Color.Black : Color.White);
            }
        }

        public void drawBoxAndHeader(SpriteBatch b)
        {
            Rectangle box = new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
            Utility.DrawSquare(b, box, 12, GRAY, Color.White);

            Vector2 headerSize = Game1.dialogueFont.MeasureString("WORDLE");
            Vector2 headerPos = new Vector2(
                this.xPositionOnScreen + (this.width - headerSize.X * 1.5f) / 2f,
                this.yPositionOnScreen + borderWidth + 10
            );
            Utility.drawBoldText(b,"WORDLE",Game1.dialogueFont,headerPos,Color.Black,1.5f);
        }

        public void drawStats(SpriteBatch b )
        {
            String totalWinsText = "Total Wins: " + saveModel.TotalWins;
            String streakText = "Streak:    " + saveModel.Streak;
            String rewardText = "Reward:    " + (int) (500 * Math.Pow(1.25, saveModel.Streak));
            int margin = 12;
            Vector2 totalWinsSize = Game1.dialogueFont.MeasureString(totalWinsText);
            Vector2 totalWinsPos = new Vector2(
                this.xPositionOnScreen + (this.width - totalWinsSize.X) / 2f,
                this.yPositionOnScreen + borderWidth + (this.height - 4*totalWinsSize.Y) - margin
            );

            Vector2 streakSize = Game1.dialogueFont.MeasureString(streakText);
            Vector2 streakPos = new Vector2(
                this.xPositionOnScreen + (this.width - streakSize.X) / 2f,
                this.yPositionOnScreen + borderWidth + (this.height - 3*streakSize.Y) - margin
            );

            Vector2 rewardSize = Game1.dialogueFont.MeasureString(totalWinsText);
            Vector2 rewardPos = new Vector2(
                this.xPositionOnScreen + (this.width - rewardSize.X) / 2f,
                this.yPositionOnScreen + borderWidth + (this.height - 2*rewardSize.Y) - margin
            );

            Utility.drawBoldText(b,totalWinsText,Game1.dialogueFont,totalWinsPos,Color.Black);
            Utility.drawBoldText(b,streakText,Game1.dialogueFont,streakPos,Color.Black);
            Utility.drawBoldText(b,rewardText,Game1.dialogueFont,rewardPos,Color.Black);
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            //Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            drawBoxAndHeader(b);
            drawGrid(b);
            if ( inPlayingState() || (!inPlayingState() && gridAnimCount != -1))
            {    
                drawKeyboard(b);
            } else if(!inPlayingState() && gridAnimCount == -1)
            {
                drawStats(b);
            }
            drawMouse(b);
        }

        private Color[] DetermineGridBgColor(String guess)
        {
            String correctWord = getWordOfWeek().ToUpper();
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
            String correctWord = getWordOfWeek().ToUpper();
            Color returnColor = LIGHTGRAY;

            for(int i = 0; i < saveModel.Guesses.Count - 1; i++)
            {
                String guess = saveModel.Guesses[i];
                for(int j = 0; j < guess.Length;  j++)
                {
                    if(i == saveModel.Guesses.Count - (inPlayingState() ? 2 : 1) && gridAnimCount != -1 && j > gridAnimCount)
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
            return saveModel.State == WordleState.WON;
        }

        private bool inPlayingState()
        {
            return saveModel.State == WordleState.PLAYING;
        }

        private bool InLoseState()
        {
            return saveModel.State == WordleState.LOST;
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

        private string getWordOfWeek()
        {
            if(saveModel != null)
            {
                return saveModel.WordOfWeek;
            }
            return "";
        }
    }
}