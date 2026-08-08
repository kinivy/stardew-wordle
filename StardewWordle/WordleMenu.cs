using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Extensions;


namespace StardewWordle
{
    public class WordleMenu : IClickableMenu
    {
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
        private TimeSpan GRID_ANIM_INTERVAL = TimeSpan.FromMilliseconds(300);
        private TimeSpan NOT_IN_BANK_ANIM_INTERVAL = TimeSpan.FromMilliseconds(1000);
        private TimeSpan  notInBankMessageStart = TimeSpan.Zero;
        private bool displayNotInBankMessage = false;
        private int GUESS_LENGTH = 5;
        private int NUM_GUESSES = 6;
        private ModConfig Config;
        private int TILE_WIDTH = Game1.tileSize;
        private int TILE_MARGIN = 5;  
        private WordleTheme Theme;      
        public WordleMenu(IModHelper helper, IMonitor monitor) :  base((int)getAppropriateMenuPosition().X, (int)getAppropriateMenuPosition().Y, menuWidth , menuHeight)
        {
            this.helper = helper;
            this.Monitor = monitor;
            this.Config = helper.ReadConfig<ModConfig>();
            if (Game1.IsMasterGame)
            {
                this.saveModel = this.helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey(Game1.player.UniqueMultiplayerID));
            }
            else
            {
                long hostId = Utils.GetHostId();
                helper.Multiplayer.SendMessage("", MessageType.REQUEST_STATE, modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {hostId});

                this.saveModel = new WordleSaveData(); //placeholder until message comes back.
            }

            this.dictionaryModel = this.helper.Data.ReadGlobalData<WordleDictionaryData>("wordle-dictionary-data");
            this.Theme = new WordleTheme(Config.DarkTheme);

            this.GridRectangles = initGrid();
            this.KeyboardMap = initKeyboard();

            Monitor.Log(getWordOfWeek(), LogLevel.Debug);
            Game1.keyboardDispatcher.Subscriber = new TextBox(null,null,Game1.smallFont,Theme.KEYBOARD_ACTIVE_TEXT);
        }

        public void Sync(WordleSaveData saveData)
        {
            this.saveModel = saveData ?? new WordleSaveData();
        }

        private Rectangle[] initGrid()
        {
            Rectangle[] grid = new Rectangle[NUM_GUESSES*GUESS_LENGTH];
            int rowStartX = this.xPositionOnScreen + (this.width - GUESS_LENGTH * (TILE_MARGIN + TILE_WIDTH)) / 2;
            for( int i = 0; i < NUM_GUESSES * GUESS_LENGTH; i++ )
            {
                int xPos = rowStartX + (i % GUESS_LENGTH) * TILE_WIDTH + (i % GUESS_LENGTH * TILE_MARGIN);
                int yPos = this.yPositionOnScreen + borderWidth + spaceToClearTopBorder + TILE_WIDTH + (((i / GUESS_LENGTH)-1) * TILE_MARGIN) + (((i / GUESS_LENGTH)-1) * TILE_WIDTH);
                grid[i] = new Rectangle(xPos, yPos, TILE_WIDTH, TILE_WIDTH);
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
                    int yPos = this.yPositionOnScreen + (this.height - ( height * 375/100)) + (i * height) + (i * margin);
                    
                    map.Add(row[j], new Rectangle(xPos, yPos, width, height));
                }
            }
            return map;
        }

        public override void update(GameTime gameTime)
        {
            animateGrid(gameTime);

            if (displayNotInBankMessage && notInBankMessageStart == TimeSpan.Zero)
            {
                notInBankMessageStart = gameTime.TotalGameTime;
            }

            if(displayNotInBankMessage && notInBankMessageStart + NOT_IN_BANK_ANIM_INTERVAL < gameTime.TotalGameTime)
            {
                notInBankMessageStart = TimeSpan.Zero;
                displayNotInBankMessage = false;
                return;
            }
        }

        private void animateGrid(GameTime gameTime)
        {
            if(gridAnimCount == 0 && gridAnimStart == TimeSpan.Zero)
            {
                gridAnimStart = gameTime.TotalGameTime;
            } else if ( gridAnimStart + GUESS_LENGTH * GRID_ANIM_INTERVAL < gameTime.TotalGameTime)
            {
                gridAnimCount = -1;
                gridAnimStart = TimeSpan.Zero;
                return;
            }

            if(gridAnimCount != -1)
            {
                for(int i = 0; i < GUESS_LENGTH; i++)
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
            if (saveModel.Guesses[saveModel.Guesses.Count-1].Length >= GUESS_LENGTH)
            {
                return;
            } 
            else
            {
                saveModel.Guesses[saveModel.Guesses.Count-1] += key.ToUpper();
            }

            writeOrSyncData();
        }

        private int determineReward()
        {
            return (int) (500 * Math.Pow(1.5, saveModel.Streak));
        }

        private void removeLetter()
        {
            String guess = saveModel.Guesses[saveModel.Guesses.Count-1];
            if(guess.Length > 0)
            {
                Game1.playSound("clubhit", null);
                saveModel.Guesses[saveModel.Guesses.Count-1]= saveModel.Guesses[saveModel.Guesses.Count-1][..^1];
                
                writeOrSyncData();
            }
        }

        private void submitGuess()
        {
            if(gridAnimCount != -1)
            {
                return;
            }
            String lastGuess = saveModel.Guesses.Last();
            if(lastGuess.Length == GUESS_LENGTH)
            {
                if (dictionaryModel.PossibleGuesses.Contains(lastGuess.ToLower()))
                {
                    Game1.playSound("crit", null);
                    updateColors();       
                    gridAnimCount = 0;
                    if (lastGuess.EqualsIgnoreCase(getWordOfWeek()))
                    {
                        saveModel.handleWin();
                        int reward = determineReward();
                        Game1.player.addUnearnedMoney(reward);
                        if(Config.MultiplayerMode == MultiplayerMode.Individual)
                        {    
                            if(Game1.player.hasQuest("kinivy_Wordle_WordleQuest") && saveModel.MaxStreak >= 4)
                            {
                                Game1.player.completeQuest("kinivy_Wordle_WordleQuest");
                                Game1.addMailForTomorrow("kinivy_Wordle_GusWordleMail");
                            }
                        } else
                        {
                            Game1.addMailForTomorrow("kinivy_Wordle_GusWordleMail",false,true);
                            foreach(Farmer farmer in Game1.getAllFarmers()){
                                if(farmer.hasQuest("kinivy_Wordle_WordleQuest") && saveModel.MaxStreak >= 4)
                                {
                                    farmer.completeQuest("kinivy_Wordle_WordleQuest");
                                }
                            }
                        }
                    } else if(saveModel.Guesses.Count() == NUM_GUESSES)
                    {
                        saveModel.State = WordleState.LOST;
                        saveModel.Streak = 0;
                        if (Config.EnableNotifications)
                        {
                            Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
                        }
                        if(Config.MultiplayerMode == MultiplayerMode.Synchronous)
                        {
                            helper.Multiplayer.SendMessage("", MessageType.STREAK_LOST, modIDs: new[] { "kinivy.StardewWordle" });
                        }
                    }
                    else
                    {
                        saveModel.Guesses.Add(""); // Start new guess
                    }
                    writeOrSyncData();
                    if(Config.MultiplayerMode == MultiplayerMode.Synchronous)
                    {
                        helper.Multiplayer.SendMessage("", MessageType.PLAY_ANIM, modIDs: new[] { "kinivy.StardewWordle" });
                    }

                    if(saveModel.State != WordleState.PLAYING)
                    {
                        Utils.WordleGameAvailable = false;
                        Utils.UpdateSaloonMachineAnimation();
                    }
                } else
                {
                    // not in word Bank
                    displayNotInBankMessage = true;
                    Monitor.Log("Not in word bank.", LogLevel.Debug);
                    Game1.playSound("fishEscape", null);
                }

                
            }   
        }

        public void playAnim()
        {
            gridAnimCount = 0;
            gridAnimStart = TimeSpan.Zero;
        }

        private void updateColors()
        {
            if (saveModel == null || saveModel.Guesses == null || saveModel.Guesses.Count == 0)
                return;

            String guess = saveModel.Guesses.Last();
            Color[] guessColors = DetermineGridBgColor(guess);
            for(int i = 0; i < GUESS_LENGTH; i++)
            {
                saveModel.Colors[saveModel.Guesses.Count()-1, i] = guessColors[i];
            }
        }

        private void writeOrSyncData()
        {
            if(Game1.IsMasterGame)
            {
                this.helper.Data.WriteSaveData(Utils.SaveKey(Game1.player.UniqueMultiplayerID), saveModel);
            } else if(Config.MultiplayerMode == MultiplayerMode.Individual)
            {
                //Send individual data to host to write.
                helper.Multiplayer.SendMessage(saveModel, MessageType.SEND_STATE, modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {Utils.GetHostId()});
            }
            
            if(Config.MultiplayerMode == MultiplayerMode.Synchronous)
            {
                Monitor.Log("writeOrSync: Sending state.", LogLevel.Debug);
                helper.Multiplayer.SendMessage(saveModel, MessageType.SEND_STATE, modIDs: new[] { "kinivy.StardewWordle" });
            }
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
                if( saveModel.Guesses.Count() > i / GUESS_LENGTH && saveModel.Guesses[ i /  GUESS_LENGTH].Length > i % GUESS_LENGTH)
                {
                    String guess = saveModel.Guesses[ i / GUESS_LENGTH ];
                    String letter = guess[i % GUESS_LENGTH].ToString();
                    Color bgColor = Theme.BACKGROUND;
                    if(!inPlayingState() || (inPlayingState() && i / GUESS_LENGTH != saveModel.Guesses.Count-1))
                    {
                        bgColor = saveModel.Colors[i / GUESS_LENGTH, i % GUESS_LENGTH];
                    }
                    if (gridAnimCount != -1 && i / GUESS_LENGTH == saveModel.Guesses.Count - (inPlayingState() ? 2 : 1))
                    {
                        if(i % GUESS_LENGTH > gridAnimCount)
                        {
                            bgColor = Theme.BACKGROUND;
                        }
                    }
                    Color borderColor = bgColor == Theme.BACKGROUND ? Theme.ACTIVE_TILE_BORDER : bgColor;
                    Utility.DrawSquare(b, square, 3, borderColor, bgColor);
                    Vector2 letterSize = Game1.dialogueFont.MeasureString(letter);
                    Vector2 letterPos = new Vector2(
                        square.X + (square.Width - letterSize.X) / 2f,
                        square.Y + (square.Height - letterSize.Y) / 2f
                    );
                    Utility.drawBoldText(b, letter, Game1.dialogueFont, letterPos, bgColor == Theme.BACKGROUND ? Theme.ACTIVE_TILE_TEXT : Theme.INACTIVE_TILE_TEXT);
                } else
                {
                    Utility.DrawSquare(b, square, 3, Theme.INACTIVE_TILE_BORDER, Theme.BACKGROUND);
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
                Color textColor = bgColor == Theme.KEYBOARD_BG ? Theme.KEYBOARD_TEXT : Theme.KEYBOARD_ACTIVE_TEXT;
                Utility.drawBoldText(b, letter, Game1.smallFont, letterPos, textColor);
            }
        }

        public void drawBoxAndHeader(SpriteBatch b)
        {
            Rectangle box = new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
            Utility.DrawSquare(b, box, 12, Theme.INACTIVE_TILE_BORDER, Theme.BACKGROUND);

            Vector2 headerSize = Game1.dialogueFont.MeasureString("WORDLE");
            Vector2 headerPos = new Vector2(
                this.xPositionOnScreen + (this.width - headerSize.X * 1.5f) / 2f,
                this.yPositionOnScreen + borderWidth + 10
            );
            Utility.drawBoldText(b,"WORDLE",Game1.dialogueFont,headerPos,Theme.HEADER_COLOR,1.5f,-1,2);
        }

        private void drawStats(SpriteBatch b )
        {
            int maxWidth = (int) Game1.dialogueFont.MeasureString("Total Wins").X;
            int maxHeight = (int) Game1.dialogueFont.MeasureString("Total Wins").Y * 2 + 5;
            int xPadding = 30;
            int yPadding = 10;
            int firstX = xPositionOnScreen + this.width/2 -xPadding - maxWidth;
            int secondX = xPositionOnScreen + width/2 + xPadding;
            int firstY = GridRectangles.Last().Y + TILE_WIDTH + yPadding * 2;
            int secondY = firstY + yPadding + maxHeight;
            int reward = determineReward();
            drawStat(b,"Reward", saveModel.State == WordleState.WON ? reward : 0, firstX, firstY, maxWidth);
            drawStat(b,"Total Wins", saveModel.TotalWins, firstX, secondY, maxWidth);
            drawStat(b,"Streak", saveModel.Streak, secondX, firstY, maxWidth);
            drawStat(b,"Max Streak", saveModel.MaxStreak, secondX, secondY, maxWidth);
        }

        private void drawStat(SpriteBatch b, String label, int stat, int xPos, int yPos, int width)
        {
            int margin = 5;
            Vector2 labelSize = Game1.dialogueFont.MeasureString(label);
            Vector2 labelPos = new Vector2(xPos + width/2 - labelSize.X/2, yPos);

            Vector2 statSize = Game1.dialogueFont.MeasureString(stat.ToString());
            if(statSize.X > width) { width = (int) statSize.X; }
            Vector2 statPos = new Vector2(xPos + width/2 - statSize.X/2, yPos + labelSize.Y + margin);

            if(label == "Reward")
            {
                Vector2 goldPos = new Vector2(xPos + width/2 - (statSize.X + 9*4+margin)/2, statPos.Y);
                statPos.X = goldPos.X + (9*4+margin);
                b.Draw(Game1.mouseCursors, goldPos, new Rectangle(408,476,9,11),Color.White,0,Vector2.Zero,new Vector2(4,4),SpriteEffects.None,0);
            }
            Utility.drawBoldText(b,label, Game1.dialogueFont,labelPos,Theme.HEADER_COLOR);
            Utility.drawBoldText(b,stat.ToString(), Game1.dialogueFont,statPos,Theme.HEADER_COLOR);
        }

        private void drawNotInBankMessage(SpriteBatch b)
        {
            Vector2 wordsSize = Game1.smallFont.MeasureString("Not in word list");

            int padding = 8;
            int BoxWidth = (int) wordsSize.X + padding;
            int BoxHeight = (int) wordsSize.Y + padding;

            int boxXPos = this.xPositionOnScreen + (this.width - BoxWidth) / 2;
            int boxYPos = GridRectangles.Last().Y + Game1.tileSize + 10;
            Rectangle box = new Rectangle(boxXPos, boxYPos, BoxWidth, BoxHeight);

            Vector2 textPos = new Vector2(boxXPos + padding/2, boxYPos + padding/2);
            Utility.DrawSquare(b,box,0,null,Theme.HEADER_COLOR);
            Utility.drawBoldText(b, "Not in word list", Game1.smallFont, textPos, Theme.BACKGROUND);
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            drawBoxAndHeader(b);
            drawGrid(b);
            if ( inPlayingState() || (!inPlayingState() && gridAnimCount != -1))
            {    
                drawKeyboard(b);
            } else if(!inPlayingState() && gridAnimCount == -1)
            {
                drawStats(b);
            }

            if (displayNotInBankMessage)
            {
                drawNotInBankMessage(b);
            }
            drawMouse(b);
        }



        private Color[] DetermineGridBgColor(String guess)
        {
            String correctWord = getWordOfWeek().ToUpper();
            Color[] colors = [Theme.TILE_GRAY,Theme.TILE_GRAY,Theme.TILE_GRAY,Theme.TILE_GRAY,Theme.TILE_GRAY];
            
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
                    colors[i] = Theme.TILE_GREEN;
                    remainingCounts[guess[i]]--;
                }
            }

            for(int i = 0; i < guess.Length; i++)
            {
                if(guess[i] != correctWord[i] && correctWord.Contains(guess[i]) && remainingCounts[guess[i]] > 0)
                {
                    colors[i] = Theme.TILE_YELLOW;
                    remainingCounts[guess[i]]--;
                }
            }
            return colors;
        }

        private Color DetermineKeyBgColor(char key)
        {
            String correctWord = getWordOfWeek().ToUpper();
            Color returnColor = Theme.KEYBOARD_BG;

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
                            return Theme.TILE_GREEN;
                        } else if(correctWord.Contains(guess[j].ToString()))
                        {
                            returnColor = Theme.TILE_YELLOW;
                        } else
                        {
                            returnColor = Theme.TILE_GRAY;
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