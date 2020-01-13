using Harmony;

namespace BetterFarmAnimalVariety.Framework.Patches.AnimalHouse
{
    class ResetSharedState
    {
        //[HarmonyPatch(typeof(StardewValley.AnimalHouse), "resetSharedState")]
        public static void Postfix(ref StardewValley.AnimalHouse __instance)
        {
            Decorators.AnimalHouse moddedAnimalHouse = new Decorators.AnimalHouse(__instance);

            // MP: I dont know why this is triggering in barns. Adding some seemingly reasonable checks to prevent that
            if (!moddedAnimalHouse.IsFull() && moddedAnimalHouse.GetIncubatorWithEggReadyToHatch() != null)
            {
                moddedAnimalHouse.SetIncubatorHatchEvent();
            }
        }
    }
}
