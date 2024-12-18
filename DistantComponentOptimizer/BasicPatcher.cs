using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using MonkeyLoader.Configuration;
using MonkeyLoader.Resonite;
using System;

namespace DistantComponentOptimizer
{
    internal class DistantComponentOptimizerConfig : ConfigSection
    {
        public readonly DefiningConfigKey<int> UpdateInterval = new("UpdateInterval", "Interval between updates (roughly equiv. to frames)", () => 30, valueValidator: value => value > 0);
        public readonly DefiningConfigKey<float> ThrottleDistance = new("ThrottleDistance", "Distance user can be from slot before throttling updates (Meters)", () => 20f);

        public override string Description => "Contains the config for DistantComponentOptimizer.";
        public override string Id => "DistantComponentOptimizer Config";
        public override Version Version { get; } = new Version(1, 0, 0);
    }

    [HarmonyPatchCategory(nameof(DistantComponentOptimizer))]
    [HarmonyPatch(typeof(ComponentBase<Component>), "InternalRunUpdate")]
    internal class DistantComponentOptimizer : ConfiguredResoniteMonkey<DistantComponentOptimizer, DistantComponentOptimizerConfig>
    {
        public override bool CanBeDisabled => true;
        private static bool Prefix(ComponentBase<Component> __instance)
        {
            if (!Enabled || __instance.World.IsUserspace()) return true;
            if (__instance.Enabled && __instance.CanRunUpdates && !__instance.UserspaceOnly)
            {
                if (__instance.FindNearestParent<Slot>() is Slot slot && !slot.IsUnderLocalUser)
                {
                    var globPos = slot.GlobalPosition;
                    var userPos = slot.World.LocalUserViewPosition;
                    var num = ConfigSection.ThrottleDistance * __instance.LocalUserRoot?.GlobalScale ?? 1f;
                    if (MathX.DistanceSqr(globPos, userPos) > num * num)
                    {
                        if (__instance.Time.LocalUpdateIndex % ConfigSection.UpdateInterval != 0) return false;

                        // could use referenceID here to make the updates more spread out instead of running all in one frame
                        //if ((__instance.Time.LocalUpdateIndex + (int)__instance.ReferenceID.Position) % ConfigSection.UpdateInterval != 0) return false;
                    }
                }
            }
            return true;
        }
    }
}