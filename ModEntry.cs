using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.GameData.BigCraftables;
using Microsoft.Xna.Framework.Graphics;

namespace StardewWordle
{
    public class ModEntry : Mod
    {
        internal static ModConfig Config;
        static string machineName = "kinivy_Wordle_WordleMachine";
        static string machineTexture = "Tilesheets/kinivy_Wordle_WordleMachine";
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Content.AssetRequested += OnAssetRequested;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            CodePatches.Initialize(this.Monitor, helper, harmony);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if(e.Name.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit((IAssetData data) =>
                {
                    var dict = data.GetData<Dictionary<string, BigCraftableData>>();
                    dict[machineName] = new()
                    {
                        Name = machineName,
                        DisplayName = "Wordle Arcade Machine",
                        Description = "asdf",
                        Fragility = 0,
                        CanBePlacedOutdoors = true,
                        CanBePlacedIndoors = true,
                        IsLamp = false,
                        Price = 0,
						SpriteIndex = 0,
                        Texture = machineTexture,
                    };
                });
			} 
            else if (e.Name.IsEquivalentTo(machineTexture))
            {
                e.LoadFromModFile<Texture2D>("Assets/Machine.png", AssetLoadPriority.Medium);
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if(Game1.dayOfMonth % 7 == 1)
            {
                checkIfWordleStreakBroke();
                weeklyReset();
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            var saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>("wordle-save-data");
            if(saveModel == null)
            {
                weeklyReset();
            }
            CodePatches.HasWonThisWeek = saveModel.HasWonThisWeek;
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
		}

        private void checkIfWordleStreakBroke()
        {
            var saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>("wordle-save-data");
            if(saveModel.Streak > 0 && saveModel.State != WordleState.WON)
            {
                saveModel.Streak = 0;
                this.Helper.Data.WriteSaveData("wordle-save-data", saveModel);
                Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
            }
        }

        private void initializeWordleDictionaryData()
        {
            var saveModel = this.Helper.Data.ReadGlobalData<WordleDictionaryData>("wordle-dictionary-data");
            if(saveModel == null){
                saveModel = new WordleDictionaryData();
                string guessesPath = Path.Combine(this.Helper.DirectoryPath, "words", "possible_guesses.txt");
                saveModel.PossibleGuesses = File.ReadAllLines(guessesPath);

                string wordsPath = Path.Combine(this.Helper.DirectoryPath, "words", "possible_words.txt");
                saveModel.PossibleWords = File.ReadAllLines(wordsPath);
            }
            this.Helper.Data.WriteGlobalData("wordle-dictionary-data", saveModel);
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
            saveModel.Colors = new Color[5,5];
            saveModel.State = WordleState.PLAYING;
            saveModel.HasWonThisWeek = false;
            
            this.Helper.Data.WriteSaveData("wordle-save-data", saveModel);

            CodePatches.HasWonThisWeek = false;

            Game1.addHUDMessage(new HUDMessage("A new Wordle game is available.", HUDMessage.achievement_type));
        }
    }
    public class WordleDictionaryData
    {
        public String[] PossibleGuesses {get; set;}
        public String[] PossibleWords {get; set;}
    }

    public class WordleSaveData
    {
        public String WordOfWeek {get; set;}

        public List<String> Guesses {get; set;} = new List<String>([""]);
        public Color[,] Colors {get; set;} = new Color[5,5];
        public WordleState State {get; set;} = WordleState.PLAYING;
        public int Streak {get; set;} = 0;
        public int TotalWins {get; set;} = 0;
        public bool HasWonThisWeek {get; set;} = false;
    }
    public enum WordleState
        {
        WON,
        LOST,
        PLAYING,
        MENU
    }
}
