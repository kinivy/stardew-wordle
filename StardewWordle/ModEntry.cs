using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using xTile.Layers;
namespace StardewWordle
{
    public class ModEntry : Mod
    {
        internal static ModConfig Config;
        static string machineName = "kinivy_Wordle_WordleMachine";
        static string machineTexture = "Tilesheets/kinivy_Wordle_WordleMachine";
        internal static bool UIInfoSuite2Loaded = false;
        internal static bool WordleGameAvailable = false; //used to show alert texture & UIInfoSuite icon
        private static Tile originalMachineTile;
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Player.Warped += onWarped;
            GameLocation.RegisterTileAction("WordleMenu", this.OpenWordleMenu);

            var harmony = new Harmony(this.ModManifest.UniqueID);

            if (Helper.ModRegistry.IsLoaded("Annosz.UiInfoSuite2"))
            {
                UIInfoSuite2Loaded = true;
                if(Config.EnableUIInfoSuite2Integration)
                {
                    new UiInfoSuite2Compat().Initialize(Monitor,helper,harmony,Config);
                }
            } else if (Config.EnableUIInfoSuite2Integration)
            {
                Monitor.Log("UIInfoSuite2 Integration Enabled but UIInfoSuite2 is not loaded.", LogLevel.Warn);
            }

            CodePatches.Initialize(this.Monitor, helper, harmony, Config);
        }

        private bool OpenWordleMenu(GameLocation location, string[] args, Farmer player, Microsoft.Xna.Framework.Point point)
        {
            Monitor.Log("Test", LogLevel.Debug);
            Game1.activeClickableMenu = new WordleMenu(Helper, Monitor);
            return true;
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>("wordle-save-data");
            if(Game1.dayOfMonth % 7 == 1)
            {
                checkIfWordleStreakBroke();
                weeklyReset();
            } else
            {
                WordleGameAvailable = saveModel.IsWordleGameAvailable();
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            var saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>("wordle-save-data");
            if(saveModel == null)
            {
                weeklyReset();
            }
        }

        private void onWarped(object? sender, WarpedEventArgs e)
        {
            if(e.NewLocation.Name == "Saloon")
            {
                UpdateSaloonMachineAnimation();
            }
        }

        internal static void UpdateSaloonMachineAnimation()
        {
            GameLocation location = Game1.currentLocation;

            if(location == null || location.Name != "Saloon") return;
            Layer layer = location.Map.GetLayer("Front");
            Tile tile = layer.Tiles[34,16];

            if(!WordleGameAvailable)
            {
                if(tile is AnimatedTile animatedTile)
                {
                    originalMachineTile = animatedTile;
                    layer.Tiles[34,16] = animatedTile.TileFrames[0];
                }
            } else if (originalMachineTile != null)
            {
                layer.Tiles[34,16] = originalMachineTile;
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            initializeWordleDictionaryData();

            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Dark Theme",
                getValue: () => Config.DarkTheme,
                setValue: value => Config.DarkTheme = value
            );
		}

        private void checkIfWordleStreakBroke()
        {
            var saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>("wordle-save-data");
            if(saveModel.Streak > 0 && saveModel.State != WordleState.WON)
            {
                saveModel.Streak = 0;
                this.Helper.Data.WriteSaveData("wordle-save-data", saveModel);
                if (Config.EnableNotifications)
                {
                    Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
                }
            }
        }

        private void initializeWordleDictionaryData()
        {
            var dictionaryData = this.Helper.Data.ReadGlobalData<WordleDictionaryData>("wordle-dictionary-data");
            if(dictionaryData == null){
                dictionaryData = new WordleDictionaryData();
                string guessesPath = Path.Combine(this.Helper.DirectoryPath, "words", "possible_guesses.txt");
                dictionaryData.PossibleGuesses = File.ReadAllLines(guessesPath);

                string wordsPath = Path.Combine(this.Helper.DirectoryPath, "words", "possible_words.txt");
                dictionaryData.PossibleWords = File.ReadAllLines(wordsPath);
            }
            this.Helper.Data.WriteGlobalData("wordle-dictionary-data", dictionaryData);
        }

        private void weeklyReset()
        {
            var dictionaryModel = this.Helper.Data.ReadGlobalData<WordleDictionaryData>("wordle-dictionary-data");
            string[] words = dictionaryModel.PossibleWords;

            var saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>("wordle-save-data");
            if(saveModel == null)
            {
                saveModel = new WordleSaveData();
            }
            var rand = new Random();
            int index = (int) (rand.NextDouble() * (words.Length-1));

            saveModel.WordOfWeek = words[index];
            saveModel.Guesses = new List<String>([""]);
            saveModel.Colors = new Color[6,5];
            saveModel.State = WordleState.PLAYING;
            
            this.Helper.Data.WriteSaveData("wordle-save-data", saveModel);


            WordleGameAvailable = saveModel.IsWordleGameAvailable();

            if(Config.EnableNotifications)
            {
                Game1.addHUDMessage(new HUDMessage("A new Wordle game is available.", HUDMessage.achievement_type));
            }
        }
    }
    public class WordleDictionaryData
    {
        public String[] PossibleGuesses {get; set;}
        public String[] PossibleWords {get; set;}
    }

    public class WordleSaveData
    {
        public String WordOfWeek {get; set;} = "";
        public List<String> Guesses {get; set;} = new List<String>([""]);
        public Color[,] Colors {get; set;} = new Color[6,5];
        public WordleState State {get; set;} = WordleState.PLAYING;
        public int Streak {get; set;} = 0;
        public int MaxStreak {get; set;} = 0;
        public int TotalWins {get; set;} = 0;
        public void handleWin()
        {
            Streak++;
            if(MaxStreak < Streak)
            {
                MaxStreak = Streak;
            }
            TotalWins++;
            State = WordleState.WON;
        }
        public bool IsWordleGameAvailable()
        {
            return State == WordleState.PLAYING;
        }
    }
    public enum WordleState
    {
        WON,
        LOST,
        PLAYING,
        MENU
    }
}
