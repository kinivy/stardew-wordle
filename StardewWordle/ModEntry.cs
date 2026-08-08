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
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Player.Warped += onWarped;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
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
            Game1.activeClickableMenu = new WordleMenu(Helper, Monitor);
            return true;
        }

        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (Game1.IsMasterGame)
            {
                Monitor.Log("Host Sending State to " + e.Peer.PlayerID, LogLevel.Debug);
                WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey());
                Helper.Multiplayer.SendMessage(saveModel, "StardewWordle_State", modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {e.Peer.PlayerID});
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (Game1.IsMasterGame)
            {
                WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey());
                if(saveModel == null)
                {
                    weeklyReset();
                }else if(Game1.dayOfMonth % 7 == 1)
                {
                    checkIfWordleStreakBroke();
                    weeklyReset();
                } else
                {
                    Utils.WordleGameAvailable = saveModel.IsWordleGameAvailable();
                }

                if(Config.MultiplayerMode == MultiplayerMode.Synchronous)
                {
                    //Send data to other players.
                    Monitor.Log("Host Sending State.", LogLevel.Debug);
                    Helper.Multiplayer.SendMessage(saveModel, "StardewWordle_State", modIDs: new[] { "kinivy.StardewWordle" });
                }
            }
        }

        private void onWarped(object? sender, WarpedEventArgs e)
        {
            if(e.NewLocation.Name == "Saloon")
            {
                Utils.UpdateSaloonMachineAnimation();
            }
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if(e.Type == "StardewWordle_State")
            {
                Monitor.Log("OnMessageReceived: Received State.", LogLevel.Debug);
                WordleSaveData state = e.ReadAs<WordleSaveData>();
                if(Game1.activeClickableMenu != null && Game1.activeClickableMenu is WordleMenu menu)
                {
                    Monitor.Log("OnMessageReceived: Syncing menu.", LogLevel.Debug);
                    menu.Sync(state);
                }
                Utils.WordleGameAvailable = state.IsWordleGameAvailable();
                Utils.UpdateSaloonMachineAnimation();

                if (Game1.IsMasterGame)
                {
                    Monitor.Log("OnMessageReceived: Host Writing State.", LogLevel.Debug);
                    this.Helper.Data.WriteSaveData(Utils.SaveKey(), state);
                }
            } else if(e.Type == "StardewWordle_RequestState" && Game1.IsMasterGame)
            {
                Monitor.Log("OnMessageReceived: Host received state request. Sending.", LogLevel.Debug);
                WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey());
                Helper.Multiplayer.SendMessage(saveModel, "StardewWordle_State", modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {e.FromPlayerID});
            } else if(e.Type == "StardewWordle_StreakLost" && Config.EnableNotifications)
            {
                Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
            } else if(e.Type == "StardewWordle_NewGame" && Config.EnableNotifications)
            {
                Game1.addHUDMessage(new HUDMessage("A new Wordle game is available.", HUDMessage.achievement_type));
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

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable HUD Notifications",
                getValue: () => Config.EnableNotifications,
                setValue: value => Config.EnableNotifications = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable UIInfoSuite2 Integration",
                tooltip: () => "A Wordle Icon will appear in the top right icon list whenever a game is available.",
                getValue: () => Config.EnableUIInfoSuite2Integration,
                setValue: value => Config.EnableUIInfoSuite2Integration = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Multiplayer Mode",
                getValue: () => Config.MultiplayerMode.ToString(),
                setValue: value =>
                {
                    Enum.TryParse(value, out MultiplayerMode mode);
                    Config.MultiplayerMode = mode;
                },
                allowedValues: new string[] { "Synchronous", "Individual"}
            );
		}

        private void checkIfWordleStreakBroke()
        {
            //Always ran by Host.
            var saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey());
            if(saveModel.Streak > 0 && saveModel.State != WordleState.WON)
            {
                saveModel.Streak = 0;
                this.Helper.Data.WriteSaveData(Utils.SaveKey(), saveModel);
                if (Config.EnableNotifications)
                {
                    Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
                    Helper.Multiplayer.SendMessage(saveModel, "StardewWordle_StreakLost", modIDs: new[] { "kinivy.StardewWordle" });
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
            //Always ran by host.
            var dictionaryModel = this.Helper.Data.ReadGlobalData<WordleDictionaryData>("wordle-dictionary-data");
            string[] words = dictionaryModel.PossibleWords;

            var saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey());
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
            
            this.Helper.Data.WriteSaveData(Utils.SaveKey(), saveModel);

            Utils.WordleGameAvailable = saveModel.IsWordleGameAvailable();

            if(Config.EnableNotifications)
            {
                Game1.addHUDMessage(new HUDMessage("A new Wordle game is available.", HUDMessage.achievement_type));
            }
        }
    }
}
