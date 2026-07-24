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
            initModel();
            checkIfWordleStreakBroke();
        }


        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {

            initializeWordleData();

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
            var model = this.Helper.Data.ReadGlobalData<ModData>("wordle-data");
            if (String.IsNullOrEmpty(model.LastWinDate)) { return; }
            String[] split = model.LastWinDate.Split(" ");
            int day = Int32.Parse(split[0]);
            String season = split[1];
            int year = Int32.Parse(split[2]);
            
            if(season.Equals(Game1.season.ToString()) 
                && year == Game1.year
                && day != Game1.dayOfMonth -1)
            {
                Monitor.Log("Same season & year, lost streak.", LogLevel.Debug);
                model.Streak = 0;
                model.LastWinDate = "";
            }

            if(Game1.dayOfMonth == 1 
                && year == Game1.year
                && oneSeasonAhead(season)
                && day != 28)
            {
                Monitor.Log("Same year, one season ahead, lost streak.", LogLevel.Debug);
                model.LastWinDate = "";
                model.Streak = 0;
            }

            if(Game1.dayOfMonth == 1 
                && Game1.season == Season.Spring
                && year == Game1.year - 1
                && season == "Winter"
                && day != 28)
            {
                Monitor.Log("New year transfer, lost streak.", LogLevel.Debug);
                model.LastWinDate = "";
                model.Streak = 0;
            }
            this.Helper.Data.WriteGlobalData("wordle-data", model);
        }

        private bool oneSeasonAhead(String season)
        {
            return season.Equals("Spring") && Game1.season == Season.Spring
                || season.Equals("Summer") && Game1.season == Season.Fall
                || season.Equals("Fall") && Game1.season == Season.Winter
                || season.Equals("Winter") && Game1.season == Season.Spring;
        }

        private void initializeWordleData()
        {
            var model = this.Helper.Data.ReadGlobalData<ModData>("wordle-data");
            if(model == null){
                model = new ModData();
                var rand = new Random();
                string guessesPath = Path.Combine(this.Helper.DirectoryPath, "words", "possible_guesses.txt");
                model.PossibleGuesses = File.ReadAllLines(guessesPath);

                string wordsPath = Path.Combine(this.Helper.DirectoryPath, "words", "possible_words.txt");
                model.PossibleWords = File.ReadAllLines(wordsPath);
            }

            this.Helper.Data.WriteGlobalData("wordle-data", model);
        }

        private void initModel()
        {
            var model = this.Helper.Data.ReadGlobalData<ModData>("wordle-data");
            string[] words = model.PossibleWords;
            var rand = new Random();
            int index = (int) (rand.NextDouble() * (words.Length-1));

            model.WordOfDay = words[index];
            model.Guesses = new List<String>([""]);
            model.Colors = new Color[5,5];
            model.State = WordleState.PLAYING;
            
            this.Helper.Data.WriteGlobalData("wordle-data", model);
        }
    }

    public class ModData
    {
        public String WordOfDay {get; set;}
        public String[] PossibleGuesses {get; set;}
        public String[] PossibleWords {get; set;}
        public List<String> Guesses {get; set;} = new List<String>([""]);
        public Color[,] Colors {get; set;} = new Color[5,5];
        public WordleState State {get; set;} = WordleState.PLAYING;
        public int Streak {get; set;} = 0;
        public int TotalWins {get; set;} = 0;
        public String LastWinDate {get; set;} = "";
    }
    public enum WordleState
        {
        WON,
        LOST,
        PLAYING
    }
}
