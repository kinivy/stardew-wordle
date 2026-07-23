using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewWordle;

namespace GrassBomb
{
    internal static class CodePatches
    {
        static IMonitor Monitor { get; set; }
        public static void Initialize(IMonitor monitor, Harmony harmony)
        {
            Monitor = monitor;
			harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.Instance_Update)),
                postfix: new HarmonyMethod(typeof(CodePatches), nameof(Instance_Update_Postfix))
            );
        }
		
		static void Instance_Update_Postfix(GameTime gameTime) {
			if(Game1.activeClickableMenu != null && Game1.activeClickableMenu.GetType().Name == "TestMenu")
            {
                ((TestMenu) Game1.activeClickableMenu).update(gameTime);
            }
		}
    }
}
