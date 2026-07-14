using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using Microsoft.Xna.Framework.Graphics;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Resources;

namespace StardewWordle
{
    public class ModEntry : Mod
    {
        internal static ModConfig Config;
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonPressed += OnButtonClick;

            var harmony = new Harmony(this.ModManifest.UniqueID);
        }

        private void OnButtonClick(object? sender, ButtonPressedEventArgs e)
        {
            if( e.Button.Equals(SButton.Y))
            {
                Game1.activeClickableMenu = new TestMenu(this.Helper, this.Monitor);
            }
        }


        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            setWordOfDay();
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

        private void initializeWordleData()
        {
            var model = this.Helper.Data.ReadGlobalData<ModData>("wordle-data");
            model = null;
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

        private void setWordOfDay()
        {
            var model = this.Helper.Data.ReadGlobalData<ModData>("wordle-data");
            string[] words = model.PossibleWords;
            var rand = new Random();
            int index = (int) (rand.NextDouble() * (words.Length-1));

            model.WordOfDay = words[index];
            model.Guesses = new List<String>([""]);
            
            this.Helper.Data.WriteGlobalData("wordle-data", model);
        }
    }

    public class ModData
    {
        public String WordOfDay {get; set;}
        public String[] PossibleGuesses {get; set;}
        public String[] PossibleWords {get; set;}
        public List<String> Guesses {get; set;}
    }
}
