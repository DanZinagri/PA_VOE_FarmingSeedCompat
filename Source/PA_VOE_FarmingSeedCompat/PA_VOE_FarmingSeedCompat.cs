using HarmonyLib;
using Outposts;
using ProgressionAgriculture;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VOE;

namespace danzinagri.ProgressionAgricultureVOEFarmingPatch
{
    [StaticConstructorOnStartup]
    public static class ModStartup
    {
        static ModStartup()
        {
            var harmony = new Harmony("danzinagri.progressionagriculture.voefarmingpatch");

            harmony.PatchAll();

            PatchVFECFarmingOutpost(harmony);
        }

        private static void PatchVFECFarmingOutpost(Harmony harmony)
        {
            var targetType = AccessTools.TypeByName("VFEC.Outposts.Outpost_Farming");

            if (targetType == null)
                return;

            var targetMethod = AccessTools.Method(targetType, "GetExtraOptions");

            if (targetMethod == null)
                return;

            var postfix = new HarmonyMethod(
                typeof(VFEC_OutpostFarming_GetExtraOptions_Patch),
                nameof(VFEC_OutpostFarming_GetExtraOptions_Patch.Postfix)
            );

            harmony.Patch(targetMethod, postfix: postfix);
        }
    }

    [HarmonyPatch(typeof(Outpost_Farming), nameof(Outpost_Farming.GetExtraOptions))]
    public static class OutpostFarming_GetExtraOptions_Patch
    {
        public static void Postfix(ref IEnumerable<ResultOption> __result)
        {
            __result = __result
                .Where(OutpostFarmingCropFilter.IsProgressionAgricultureUnlocked)
                .ToList();
        }
    }

    public static class VFEC_OutpostFarming_GetExtraOptions_Patch
    {
        public static void Postfix(ref IEnumerable<ResultOption> __result)
        {
            __result = __result
                .Where(OutpostFarmingCropFilter.IsProgressionAgricultureUnlocked)
                .ToList();
        }
    }

    public static class OutpostFarmingCropFilter
    {
        public static bool IsProgressionAgricultureUnlocked(ResultOption option)
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