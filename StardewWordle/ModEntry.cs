using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using xTile.Layers;
using System.Reflection.Metadata;
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
            Utils.Helper = helper;
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
                WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey(e.Peer.PlayerID));
                if(Config.MultiplayerMode == MultiplayerMode.Individual)
                {
                    saveModel = performDailyActionsOnSave(saveModel, e.Peer.PlayerID);
                }
                Helper.Multiplayer.SendMessage(saveModel, MessageType.SEND_STATE, modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {e.Peer.PlayerID});
                //Syncing Multiplayer Mode in Config
                Helper.Multiplayer.SendMessage(Config.MultiplayerMode, MessageType.MODE_SYNC, modIDs: new[] { "kinivy.StardewWordle" });
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            handleDayStarted();
        }

        private void handleDayStarted()
        {
            if (Game1.IsMasterGame)
            {
                if(Config.MultiplayerMode == MultiplayerMode.Synchronous)
                {
                    WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey());
                    saveModel = performDailyActionsOnSave(saveModel);
                    Utils.WordleGameAvailable = saveModel.IsWordleGameAvailable();
                    
                    //Send data to other players.
                    Monitor.Log("Host Sending State.", LogLevel.Debug);
                    Helper.Multiplayer.SendMessage(saveModel, MessageType.SEND_STATE, modIDs: new[] { "kinivy.StardewWordle" });
                } else
                {
                    foreach(long id in Utils.getAllPlayerIDs())
                    {
                        WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey(id));
                        saveModel = performDailyActionsOnSave(saveModel, id);

                        if(id == Game1.player.UniqueMultiplayerID)
                        {
                            Utils.WordleGameAvailable = saveModel.IsWordleGameAvailable();
                            Utils.UpdateSaloonMachineAnimation();
                        } else {
                            Helper.Multiplayer.SendMessage(saveModel, MessageType.SEND_STATE, modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] { id});
                        }
                    }
                }
            }
        }

        private WordleSaveData performDailyActionsOnSave(WordleSaveData saveData, long saveId = -1)
        {
            if(saveData == null)
            {
                saveData = weeklyReset(null, saveId);
            }else if(Game1.dayOfMonth % 7 == 1)
            {
                checkIfWordleStreakBroke(saveData, saveId);
                saveData = weeklyReset(saveData, saveId);
            }
            return saveData;
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
            if(e.Type == MessageType.SEND_STATE)
            {
                Monitor.Log("OnMessageReceived: Received State.", LogLevel.Debug);
                WordleSaveData state = e.ReadAs<WordleSaveData>();
                if(!(Config.MultiplayerMode == MultiplayerMode.Individual && Game1.IsMasterGame))
                {
                    if(Game1.activeClickableMenu != null && Game1.activeClickableMenu is WordleMenu menu)
                    {
                        Monitor.Log("OnMessageReceived: Syncing menu. Correct Word is " + state.WordOfWeek, LogLevel.Debug);
                        menu.Sync(state);
                    }
                    Utils.WordleGameAvailable = state.IsWordleGameAvailable();
                    Utils.UpdateSaloonMachineAnimation();
                }

                if (Game1.IsMasterGame)
                {
                    Monitor.Log("OnMessageReceived: Host Writing State.", LogLevel.Debug);
                    this.Helper.Data.WriteSaveData(Utils.SaveKey(e.FromPlayerID), state);
                }
            } else if(e.Type == MessageType.REQUEST_STATE && Game1.IsMasterGame)
            {
                WordleSaveData saveModel = this.Helper.Data.ReadSaveData<WordleSaveData>(Utils.SaveKey(e.FromPlayerID));
                Monitor.Log("OnMessageReceived: Host received state request. Sending : " + saveModel.Guesses.Last(), LogLevel.Debug);
                Helper.Multiplayer.SendMessage(saveModel, MessageType.SEND_STATE, modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {e.FromPlayerID});
            } else if(e.Type == MessageType.STREAK_LOST && Config.EnableNotifications)
            {
                Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
            } else if(e.Type == MessageType.GAME_AVAILABLE && Config.EnableNotifications)
            {
                Game1.addHUDMessage(new HUDMessage("A new Wordle game is available.", HUDMessage.achievement_type));
            } else if(e.Type == MessageType.MODE_SYNC)
            {
                MultiplayerMode mode = e.ReadAs<MultiplayerMode>();
                Config.MultiplayerMode = mode;
                if(Game1.activeClickableMenu != null && Game1.activeClickableMenu is WordleMenu menu)
                {
                    menu.exitThisMenu();
                }
            } else if(e.Type == MessageType.PLAY_ANIM)
            {
                if(Game1.activeClickableMenu != null && Game1.activeClickableMenu is WordleMenu menu)
                {
                    menu.playAnim();
                }
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
                    if(Config.MultiplayerMode == mode) return;
                    Config.MultiplayerMode = mode;
                    handleDayStarted();
                    Helper.Multiplayer.SendMessage(mode, MessageType.MODE_SYNC, modIDs: new[] { "kinivy.StardewWordle" });

                },
                allowedValues: new string[] { "Synchronous", "Individual"}
            );
		}

        private void checkIfWordleStreakBroke(WordleSaveData saveData, long saveId)
        {
            //Always ran by Host.
            if(saveData.Streak > 0 && saveData.State != WordleState.WON)
            {
                saveData.Streak = 0;
                this.Helper.Data.WriteSaveData(Utils.SaveKey(saveId), saveData);
                if(Config.MultiplayerMode == MultiplayerMode.Synchronous)
                {
                    if (Config.EnableNotifications) Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
                    Helper.Multiplayer.SendMessage("", MessageType.STREAK_LOST, modIDs: new[] { "kinivy.StardewWordle" });
                } else
                {
                    if(Game1.player.UniqueMultiplayerID == saveId && Config.EnableNotifications) Game1.addHUDMessage(new HUDMessage("You lost your Wordle streak.", HUDMessage.error_type));
                    Helper.Multiplayer.SendMessage("", MessageType.STREAK_LOST, modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {saveId});
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

        private WordleSaveData weeklyReset(WordleSaveData? saveData, long playerId = -1)
        {
            //Always ran by host.
            var dictionaryModel = this.Helper.Data.ReadGlobalData<WordleDictionaryData>("wordle-dictionary-data");
            string[] words = dictionaryModel.PossibleWords;

            if(saveData == null)
            {
                saveData = new WordleSaveData();
            }
            var rand = new Random();
            int index = (int) (rand.NextDouble() * (words.Length-1));

            saveData.WordOfWeek = words[index];
            saveData.Guesses = new List<String>([""]);
            saveData.Colors = new Color[6,5];
            saveData.State = WordleState.PLAYING;
            
            this.Helper.Data.WriteSaveData(Utils.SaveKey(playerId), saveData);
            if(Config.MultiplayerMode == MultiplayerMode.Synchronous)
            {
                if (Config.EnableNotifications) Game1.addHUDMessage(new HUDMessage("A new Wordle game is available.", HUDMessage.achievement_type));
                Helper.Multiplayer.SendMessage("", MessageType.GAME_AVAILABLE, modIDs: new[] { "kinivy.StardewWordle" });
            } else
            {
                if(Game1.player.UniqueMultiplayerID == playerId && Config.EnableNotifications) Game1.addHUDMessage(new HUDMessage("A new Wordle game is available.", HUDMessage.achievement_type));
                Helper.Multiplayer.SendMessage("", MessageType.STREAK_LOST, modIDs: new[] { "kinivy.StardewWordle" }, playerIDs: new[] {playerId});
            }   
            return saveData;
        }
    }
}
