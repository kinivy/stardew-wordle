using System.Xml;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;
using StardewWordle;

namespace StardewWordle
{
    internal static class CodePatches
    {
        static IMonitor Monitor { get; set; }
        static IModHelper Helper { get; set; }
        static string machineName = "kinivy_Wordle_WordleMachine";
        static TimeSpan machineAnimInterval = TimeSpan.FromMilliseconds(1000);
        public static bool HasWonThisWeek = false;

        public static void Initialize(IMonitor monitor, IModHelper helper, Harmony harmony)
        {
            Monitor = monitor;
            Helper = helper;
			harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.Instance_Update)),
                postfix: new HarmonyMethod(typeof(CodePatches), nameof(Instance_Update_Postfix))
            );
			harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.checkForAction)),
                postfix: new HarmonyMethod(typeof(CodePatches), nameof(CheckForAction_Postfix))
            );
			harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.updateWhenCurrentLocation)),
                postfix: new HarmonyMethod(typeof(CodePatches), nameof(UpdateWhenLocation_Postfix))
            );
        }
		
		static void Instance_Update_Postfix(GameTime gameTime) {
			if(Game1.activeClickableMenu != null && Game1.activeClickableMenu.GetType().Name == "TestMenu")
            {
                ((TestMenu) Game1.activeClickableMenu).update(gameTime);
            }
		}

        static void UpdateWhenLocation_Postfix(StardewValley.Object __instance, GameTime time)
		{
			if(__instance.itemId.Contains(machineName))
            {     
                if (!HasWonThisWeek) 
                {
                    long second = (long)(time.TotalGameTime.TotalMilliseconds / 1000);
                    int idx = (int) (second % 2);
                    if(__instance.ParentSheetIndex != idx)
                    {
                        __instance.ParentSheetIndex = idx;
                    }
                } else
                {
                    __instance.ParentSheetIndex = 0;
                }
            }
		}

        static void CheckForAction_Postfix(StardewValley.Object __instance, Farmer who, bool justCheckingForActivity = false)
        {
            if(__instance.itemId.Contains(machineName))
            {
                if (justCheckingForActivity)
                {
                    return;
                }
                Game1.activeClickableMenu = new TestMenu(Helper, Monitor);
            }
        }
    }
}
