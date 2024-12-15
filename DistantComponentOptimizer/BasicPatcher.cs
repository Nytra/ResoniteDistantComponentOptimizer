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
        public readonly DefiningConfigKey<bool> EnableUpdateThrottling = new("EnableUpdateThrottling", "EnableUpdateThrottling", () => true);
        public readonly DefiningConfigKey<int> UpdateFrequency = new("UpdateFrequency", "Frequency of updates in frames", () => 30, valueValidator: value => value > 0);
        public readonly DefiningConfigKey<float> ThrottleDistance = new("ThrottleDistance", "Distance user can be from slot before throttling updates", () => 20f);

        public override string Description => "Contains the config for DistantComponentOptimizer.";
        public override string Id => "DistantComponentOptimizer Config";
        public override Version Version { get; } = new Version(1, 0, 0);
    }

    [HarmonyPatchCategory(nameof(DistantComponentOptimizer))]
    [HarmonyPatch(typeof(ComponentBase<Component>), "InternalRunUpdate")]
    internal class DistantComponentOptimizer : ConfiguredResoniteMonkey<DistantComponentOptimizer, DistantComponentOptimizerConfig>
    {
        private static bool Prefix(ComponentBase<Component> __instance)
        {
            if (!ConfigSection.EnableUpdateThrottling) return true;
            if (__instance.World.IsUserspace()) return true;
            if (__instance.Enabled && __instance.CanRunUpdates && !__instance.UserspaceOnly)
            {
                if (__instance.FindNearestParent<Slot>() is Slot slot)
                {
                    var globPos = slot.GlobalPosition;
                    var userPos = slot.LocalUser.Root.GetGlobalPosition(UserRoot.UserNode.View);
                    if (MathX.Distance(globPos, userPos) > ConfigSection.ThrottleDistance * __instance.LocalUserRoot.GlobalScale)
                    {
                        if (__instance.Time.LocalUpdateIndex % ConfigSection.UpdateFrequency != 0) return false;
                    }
                }
            }
            return true;
        }
    }
}