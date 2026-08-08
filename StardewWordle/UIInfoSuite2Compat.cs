
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace StardewWordle
{
    internal class UiInfoSuite2Compat
    {
        static IMonitor Monitor { get; set; }
        static IModHelper Helper { get; set; }
        static Texture2D icon {get; set;}
        ClickableTextureComponent IconComponent { get; set; }
        static Rectangle SpriteLocation= new Rectangle(0,0,12,12);
        String HoverText = "A new Wordle game is available!";

        public void Initialize(IMonitor monitor, IModHelper helper, Harmony harmony, ModConfig config)
        {
            helper.Events.Display.RenderingHud += OnRenderingHud;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            icon =  helper.ModContent.Load<Texture2D>("assets/UIIcon.png");
            Monitor = monitor;
            Helper = helper;
        }

        private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
        {
            if (!Utils.WordleGameAvailable || !UInfoSuite2_IsRenderingNormally()) return;

            Point? pos = UIInfoSuite2_GetNewIconPosition();
            if (pos.HasValue)
            {
                    IconComponent = new ClickableTextureComponent(
                    new Rectangle(pos.Value.X, pos.Value.Y,(int) (12*3.5), (int) (12*3.5)),
                    icon,
                    SpriteLocation,
                    3.25f
                );
                IconComponent.draw(e.SpriteBatch);
            } else
            {
                Monitor.Log("no value", LogLevel.Debug);
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Utils.WordleGameAvailable || !UInfoSuite2_IsRenderingNormally()) return;

            bool hasMouse = IconComponent?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false;
            if (hasMouse)
            {
                IClickableMenu.drawHoverText(Game1.spriteBatch, HoverText, Game1.dialogueFont);
            }
        }

        private bool UInfoSuite2_IsRenderingNormally()
        {
            try
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "UIInfoSuite2");
                Type uiUtils = assembly.GetType("UIInfoSuite2.UIElements.UIElementUtils");
                MethodInfo method = uiUtils.GetMethod("IsRenderingNormally");

                return (bool) method.Invoke(null,null);
            }
            catch (Exception ex)
            {
                // Failed to get UIInfoSuite2 isRenderingNormally
                return false;
            }
        }

        private Point? UIInfoSuite2_GetNewIconPosition()
        {
            try
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "UIInfoSuite2");
                Type iconHandlerType = assembly.GetType("UIInfoSuite2.Infrastructure.IconHandler");
                object handlerInstance = iconHandlerType.GetProperty("Handler")?.GetValue(null);
                MethodInfo method = iconHandlerType.GetMethod("GetNewIconPosition");

                return (Point)method.Invoke(handlerInstance, null);
            }
            catch (Exception ex)
            {
                // Failed to get UIInfoSuite2 icon position via reflection.
                return null;
            }
        }
    }
}