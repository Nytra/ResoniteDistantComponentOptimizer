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
        public readonly DefiningConfigKey<int> UpdateInterval = new("UpdateInterval", "Interval between updates (roughly equiv. to frames)", () => 30, valueValidator: value => value > 0 && value <= 60);
        public readonly DefiningConfigKey<float> ThrottleDistance = new("ThrottleDistance", "Distance user can be from slot before throttling updates (Meters)", () => 20f);
        public readonly DefiningConfigKey<bool> SpreadUpdates = new("SpreadUpdates", "Spread out the updates more instead of running them all in one frame.", () => false);
        public readonly DefiningConfigKey<bool> IgnoreUserScale = new("IgnoreUserScale", "Ignore user scale in distance calculation.", () => false);
        //public readonly DefiningConfigKey<Type> ExcludeType1 = new("ExcludeType1", "Excludes this type from throttling.");
        //public readonly DefiningConfigKey<Type> ExcludeType2 = new("ExcludeType2", "Excludes this type from throttling.");
        //public readonly DefiningConfigKey<Type> ExcludeType3 = new("ExcludeType3", "Excludes this type from throttling.");

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
            try
            {
                if (!Enabled || __instance.World.IsUserspace()) return true;
                //if (ConfigSection.ExcludeType1 != null || ConfigSection.ExcludeType2 != null || ConfigSection.ExcludeType3 != null)
                //{
                //    var type = __instance.GetType();
                //    if (ConfigSection.ExcludeType1 != null && type == ConfigSection.ExcludeType1) return true;
                //    if (ConfigSection.ExcludeType2 != null && type == ConfigSection.ExcludeType2) return true;
                //    if (ConfigSection.ExcludeType3 != null && type == ConfigSection.ExcludeType3) return true;
                //}
                if (__instance.Enabled && __instance.CanRunUpdates && !__instance.UserspaceOnly)
                {
                    if (__instance.FindNearestParent<Slot>() is Slot slot && !slot.IsUnderLocalUser)
                    {
                        var globPos = slot.GlobalPosition;
                        var userPos = slot.World.LocalUserViewPosition;
                        float mult = 1f;
                        if (!ConfigSection.IgnoreUserScale)
                        {
                            mult = __instance.LocalUserRoot?.GlobalScale ?? 1f;
                        }
                        var num = ConfigSection.ThrottleDistance * mult;
                        if (MathX.DistanceSqr(globPos, userPos) > num * num)
                        {
                            if (ConfigSection.SpreadUpdates)
                            {
                                if (((__instance.Time?.LocalUpdateIndex ?? 0) + (int)__instance.ReferenceID.Position) % ConfigSection.UpdateInterval != 0) return false;
                            }
                            else
                            {
                                if ((__instance.Time?.LocalUpdateIndex ?? 0) % ConfigSection.UpdateInterval != 0) return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    Logger.Error(() => e.ToString());
                }
                catch
                {

                }
                return true;
            }
        }
    }
}