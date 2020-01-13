using System;
using System.Collections.Generic;
using Harmony;
using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;

namespace BetterFarmAnimalVariety.Framework.Events
{
    class LoadMod
    {
        public static void OnEntry(ModEntry mod)
        {
            // Always kill the mod if we could not set up the config
            LoadMod.SetUpConfig(mod);

            // Harmony
            LoadMod.SetUpHarmonyPatches();

            // Asset Loaders
            LoadMod.SetUpAssetLoaders(mod);

            // Asset Editors
            LoadMod.SetUpAssetEditors(mod);

            // Commands
            LoadMod.SetUpConsoleCommands(mod);
        }

        private static void SetUpConfig(ModEntry mod)
        {
            ModConfig config;

            string targetFormat = mod.ModManifest.Version.MajorVersion.ToString();

            try
            {
                // Load the config
                config = Helpers.Mod.ReadConfig<ModConfig>();

                // Do this outside of the constructor so that we can use the ModManifest helper
                if (config.Format == null)
                {
                    config.Format = targetFormat;
                }
                else
                {
                    config.AssertValidFormat(targetFormat);
                }
            }
            catch
            {
                MigrateDeprecatedConfig.OnEntry(mod, targetFormat, out config);
            }

            // Write back the config
            config.Write(mod.Helper);

            if (!config.IsEnabled)
            {
                throw new ApplicationException($"Mod is disabled. To enable, set IsEnabled to true in config.json.");
            }
        }

        private static void SetUpHarmonyPatches()
        {
            // Patch the game code directly
            HarmonyInstance harmony = HarmonyInstance.Create(Constants.Mod.Key);

            // Unpatched FarmAnimal constructor calls:
            // - Forest.Forest: Marnie's cows
            // - Farm.placeAnimal: only affects the "debug bluebook" command (CataloguePage is not used)
            // - Game1.parseDebugInput: only affects "debug animal" and "debug spawnCoopsAndBarns"
            // - FarmInfoPage: not used, but lists out animals by vanilla type
            ModEntry.Instance.Monitor.Log("Applying Harmony patches...");
            harmony.Patch(
                original: AccessTools.Method(typeof(AnimalHouse), "addNewHatchedAnimal"),
                prefix: new HarmonyMethod(typeof(Patches.AnimalHouse.AddNewHatchedAnimal), nameof(Patches.AnimalHouse.AddNewHatchedAnimal.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(AnimalHouse), "resetSharedState"),
                postfix: new HarmonyMethod(typeof(Patches.AnimalHouse.ResetSharedState), nameof(Patches.AnimalHouse.ResetSharedState.Postfix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Buildings.Coop), "dayUpdate"),
                prefix: new HarmonyMethod(typeof(Patches.Coop.DayUpdate), nameof(Patches.Coop.DayUpdate.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "behaviors"),
                prefix: new HarmonyMethod(typeof(Patches.FarmAnimal.Behaviors), nameof(Patches.FarmAnimal.Behaviors.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "dayUpdate"),
                prefix: new HarmonyMethod(typeof(Patches.FarmAnimal.DayUpdate), nameof(Patches.FarmAnimal.DayUpdate.Prefix)),
                postfix: new HarmonyMethod(typeof(Patches.FarmAnimal.DayUpdate), nameof(Patches.FarmAnimal.DayUpdate.Postfix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "findTruffle"),
                prefix: new HarmonyMethod(typeof(Patches.FarmAnimal.FindTruffle), nameof(Patches.FarmAnimal.FindTruffle.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(FarmAnimal), "reload"),
                prefix: new HarmonyMethod(typeof(Patches.FarmAnimal.Reload), nameof(Patches.FarmAnimal.Reload.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Object), "DayUpdate"),
                prefix: new HarmonyMethod(typeof(Patches.Object.DayUpdate), nameof(Patches.Object.DayUpdate.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Constructor(typeof(PurchaseAnimalsMenu), new Type[] { typeof(List<StardewValley.Object>) }),
                prefix: new HarmonyMethod(typeof(Patches.PurchaseAnimalsMenu.Constructor), nameof(Patches.PurchaseAnimalsMenu.Constructor.Prefix)),
                postfix: new HarmonyMethod(typeof(Patches.PurchaseAnimalsMenu.Constructor), nameof(Patches.PurchaseAnimalsMenu.Constructor.Postfix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "draw", new Type[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(Patches.PurchaseAnimalsMenu.Draw), nameof(Patches.PurchaseAnimalsMenu.Draw.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "getAnimalDescription"),
                prefix: new HarmonyMethod(typeof(Patches.PurchaseAnimalsMenu.GetAnimalDescription), nameof(Patches.PurchaseAnimalsMenu.GetAnimalDescription.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "getAnimalTitle"),
                prefix: new HarmonyMethod(typeof(Patches.PurchaseAnimalsMenu.GetAnimalTitle), nameof(Patches.PurchaseAnimalsMenu.GetAnimalTitle.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(PurchaseAnimalsMenu), "receiveLeftClick"),
                prefix: new HarmonyMethod(typeof(Patches.PurchaseAnimalsMenu.ReceiveLeftClick), nameof(Patches.PurchaseAnimalsMenu.ReceiveLeftClick.Prefix))
                );
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), "getPurchaseAnimalStock"),
                prefix: new HarmonyMethod(typeof(Patches.Utility.GetPurchaseAnimalStock), nameof(Patches.Utility.GetPurchaseAnimalStock.Prefix))
                );
            ModEntry.Instance.Monitor.Log("... done patching.");
        }

        private static void SetUpConsoleCommands(ModEntry mod)
        {
            List<Commands.Command> commands = new List<Commands.Command>()
            {
                new Commands.List(mod.Helper, mod.Monitor),
            };

            foreach (Commands.Command command in commands)
            {
                mod.Helper.ConsoleCommands.Add(command.Name, command.Description, command.Callback);
            }
        }

        private static void SetUpAssetEditors(ModEntry mod)
        {
            mod.Helper.Content.AssetEditors.Add(new Editors.AnimalBirth(mod.Helper));
            mod.Helper.Content.AssetEditors.Add(new Editors.FarmAnimalData(mod.Helper, mod.Monitor));
        }

        private static void SetUpAssetLoaders(ModEntry mod)
        {
            mod.Helper.Content.AssetLoaders.Add(new Loaders.FarmAnimalSprites());
        }
    }
}
