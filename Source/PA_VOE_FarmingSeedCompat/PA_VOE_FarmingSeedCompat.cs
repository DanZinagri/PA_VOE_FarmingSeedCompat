using HarmonyLib;
using Outposts;
using ProgressionAgriculture;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VOE;

namespace BigMarabill.ProgressionAgricultureVOEFarmingPatch
{
    [StaticConstructorOnStartup]
    public static class ModStartup
    {
        static ModStartup()
        {
            new Harmony("danzinagri.progressionagriculture.voefarmingpatch").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Outpost_Farming), nameof(Outpost_Farming.GetExtraOptions))]
    public static class OutpostFarming_GetExtraOptions_Patch
    {
        public static void Postfix(ref IEnumerable<ResultOption> __result)
        {
            __result = __result.Where(IsProgressionAgricultureUnlocked).ToList();
        }

        private static bool IsProgressionAgricultureUnlocked(ResultOption option)
        {
            if (option?.Thing is not ThingDef harvestedThing)
                return true;

            ThingDef plantDef = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.category == ThingCategory.Plant
                    && d.plant?.harvestedThingDef == harvestedThing
                    && d.plant.sowTags.Contains("Ground"))
                .MaxBy(d => d.plant.harvestYield);

            if (plantDef == null)
                return true;

            var tracker = GameComponent_UnlockedCrops.Instance;
            return tracker == null || tracker.IsCropUnlocked(plantDef);
        }
    }
}
