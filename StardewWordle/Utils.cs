
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
namespace StardewWordle
{
    
    public class Utils
{
        internal static bool WordleGameAvailable = false; //used to show alert texture & UIInfoSuite icon
        private static Tile originalMachineTile;

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

        internal static string SaveKey()
        {
            if(ModEntry.Config.MultiplayerMode == MultiplayerMode.Individual)
            {
                return "wordle-save-data-" + Game1.player.UniqueMultiplayerID;
            } else
            {
                return "wordle-save-data";
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
}