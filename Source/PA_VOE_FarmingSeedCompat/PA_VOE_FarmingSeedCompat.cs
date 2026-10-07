using HarmonyLib;
using Outposts;
using ProgressionAgriculture;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VOE;

namespace danzinagri.ProgressionAgricultureVOEFarmingPatch
{
    [StaticConstructorOnStartup]
    public static class ModStartup
    {
        public const string HarmonyId = "danzinagri.progressionagriculture.voefarmingpatch";

        static ModStartup()
        {
            // This DLL ships both as a standalone mod and bundled inside
            // Progression: Agriculture ("Mods and Shit/VE Outposts"). If both
            // copies are loaded, only the first one should patch, otherwise
            // every outpost option list is filtered twice.
            if (Harmony.HasAnyPatches(HarmonyId))
                return;

            var harmony = new Harmony(HarmonyId);

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
            __result = OutpostFarmingCropFilter.FilterUnlocked(__result);
        }
    }

    public static class VFEC_OutpostFarming_GetExtraOptions_Patch
    {
        public static void Postfix(ref IEnumerable<ResultOption> __result)
        {
            __result = OutpostFarmingCropFilter.FilterUnlocked(__result);
        }
    }

    public static class OutpostFarmingCropFilter
    {
        // harvestedThingDef -> the highest-yield ground-sowable plant that
        // produces it. This mirrors the grouping VOE/VFEC Outpost_Farming does
        // in GetExtraOptions, so the plant we check for an unlock is the same
        // plant the outpost would actually grow.
        //
        // Built once on first use. GetExtraOptions is re-evaluated every frame
        // while a farming outpost is selected (Outpost_ChooseResult.ResultOptions
        // calls it from the inspect string and gizmos), so scanning
        // DefDatabase<ThingDef> per option per frame was the perf spike.
        private static Dictionary<ThingDef, ThingDef> plantByHarvestedThing;

        private static Dictionary<ThingDef, ThingDef> PlantByHarvestedThing
        {
            get
            {
                if (plantByHarvestedThing == null)
                    BuildCache();

                return plantByHarvestedThing;
            }
        }

        private static void BuildCache()
        {
            var cache = new Dictionary<ThingDef, ThingDef>();

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.category != ThingCategory.Plant)
                    continue;

                PlantProperties plant = def.plant;

                if (plant?.harvestedThingDef == null)
                    continue;

                if (plant.sowTags == null || !plant.sowTags.Contains("Ground"))
                    continue;

                if (!cache.TryGetValue(plant.harvestedThingDef, out ThingDef best)
                    || plant.harvestYield > best.plant.harvestYield)
                {
                    cache[plant.harvestedThingDef] = def;
                }
            }

            plantByHarvestedThing = cache;
        }

        public static IEnumerable<ResultOption> FilterUnlocked(IEnumerable<ResultOption> options)
        {
            if (options == null)
                return options;

            return options.Where(IsProgressionAgricultureUnlocked).ToList();
        }

        public static bool IsProgressionAgricultureUnlocked(ResultOption option)
        {
            ThingDef harvestedThing = option?.Thing;

            if (harvestedThing == null)
                return true;

            if (!PlantByHarvestedThing.TryGetValue(harvestedThing, out ThingDef plantDef))
                return true;

            var tracker = GameComponent_UnlockedCrops.Instance;
            return tracker == null || tracker.IsCropUnlocked(plantDef);
        }
    }
}
