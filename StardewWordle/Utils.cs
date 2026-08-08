
using System.Reflection;
using System.Security.Principal;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile.Tiles;
using xTile.Layers;
using System.Reflection.Metadata.Ecma335;
namespace StardewWordle
{
    
    public class Utils
{
        internal static bool WordleGameAvailable = false; //used to show alert texture & UIInfoSuite icon
        private static Tile originalMachineTile;
        internal static IModHelper Helper;
        //CachedMultiplayerMode is updated whenever the peer connects to the host or the host changes the config.
        private static MultiplayerMode _CachedMultiplayerMode;
        internal static MultiplayerMode MultiplayerMode
        {
            get
            {
                if(Game1.IsMasterGame) return ModEntry.Config.MultiplayerMode;
                return _CachedMultiplayerMode;
            }
            set
            {
                _CachedMultiplayerMode = value;
            }
        }

        internal static void UpdateSaloonMachineAnimation()
        {
            GameLocation location = Game1.currentLocation;

            if(location == null || location.Name != "Saloon") return;
            Layer layer = location.Map.GetLayer("Front");
            Tile tile = layer.Tiles[34,16];

            if(!Utils.WordleGameAvailable)
            {
                if(tile is AnimatedTile animatedTile)
                {
                    originalMachineTile = animatedTile;
                    layer.Tiles[34,16] = animatedTile.TileFrames[0]; //Sets to first frame to stop animation
                }
            } else if (originalMachineTile != null)
            {
                layer.Tiles[34,16] = originalMachineTile;
            }
        }

        internal static string SaveKey(long playerId=-1)
        {
            if(ModEntry.Config.MultiplayerMode == MultiplayerMode.Individual)
            {
                return "wordle-save-data-" + playerId;
            } else
            {
                return "wordle-save-data-shared";
            }
        }

        internal static long GetHostId()
        {
            if (Game1.IsMasterGame)
            {
                return Game1.player.UniqueMultiplayerID;
            }
            foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsHost)
                {
                  return peer.PlayerID;
                }
            }
            return -1;
        }

        internal static List<long> getAllPlayerIDs()
        {
            List<long> ids = new List<long>();
            foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
            {
                ids.Add(peer.PlayerID);
            }         
            ids.Add(Game1.player.UniqueMultiplayerID);
            return ids;
        }
    }

    public class WordleDictionaryData
    {
        public String[] PossibleGuesses {get; set;}
        public String[] PossibleWords {get; set;}
    }

    public class WordleSaveData
    {
        public string WordOfWeek {get; set;} = "";
        public List<string> Guesses {get; set;} = new List<string>([""]);
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

    public class MessageType
    {
        public static string SEND_STATE = "StardewWordle_SendState";
        public static string REQUEST_STATE = "StardewWordle_RequestState";
        public static string STREAK_LOST = "StardewWordle_StreakLost";
        public static string GAME_AVAILABLE = "StardewWordle_GameAvailable";
        public static string MP_MODE = "StardewWordle_MP_Mode";
        public static string PLAY_ANIM = "StardewWordle_PlayAnim";
        public static string COMPLETE_STREAK_QUEST = "StardewWordle_CompleteStreakQuest";
    }
}