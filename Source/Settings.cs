using System.Collections.Generic;
using CombatExtended;
using CombatExtended.WorldObjects;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    public class MASSettings : ModSettings
    {
        // multiplies the points CE gives a settlement to shell back with (0 = no retaliation shelling)
        public float retaliationBudgetFactor = 1f;
        public bool aiNuclearRetaliation = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref retaliationBudgetFactor, "retaliationBudgetFactor", 1f);
            Scribe_Values.Look(ref aiNuclearRetaliation, "aiNuclearRetaliation", false);
        }
    }

    public class MASMod : Mod
    {
        public static MASSettings Settings;

        public MASMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MASSettings>();
            new Harmony("linkrag.mountedarms.strategic").PatchAll();
        }

        public override string SettingsCategory()
        {
            return "Mounted Arms: Strategic";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(inRect);
            Text.Font = GameFont.Medium;
            l.Label("MAS_Settings_RetaliationHeader".Translate());
            Text.Font = GameFont.Small;
            l.Label("MAS_Settings_RetaliationExplain".Translate());
            l.GapLine();
            string pct = Settings.retaliationBudgetFactor <= 0f
                ? (string)"MAS_Settings_RetaliationOff".Translate()
                : Settings.retaliationBudgetFactor.ToStringPercent();
            l.Label("MAS_Settings_BudgetFactor".Translate(pct));
            Settings.retaliationBudgetFactor = GenMath.RoundTo(l.Slider(Settings.retaliationBudgetFactor, 0f, 5f), 0.05f);
            l.Gap();
            bool rimatomics = NuclearRetaliation.NukeDef != null;
            if (rimatomics)
            {
                l.CheckboxLabeled("MAS_Settings_AINuclear".Translate(), ref Settings.aiNuclearRetaliation, "MAS_Settings_AINuclearDesc".Translate());
            }
            else
            {
                l.Label("MAS_Settings_AINuclearNoRimatomics".Translate());
            }
            l.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            NuclearRetaliation.Apply();
        }
    }

    /// <summary>
    /// Adds/removes the nuclear Minuteman from the AI retaliation lists (CE ShellingResponseDefs) according to
    /// the setting. HostilityComp reads shellingResponseDef.projectiles live, so editing the list works in-game.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class NuclearRetaliation
    {
        private static readonly string[] Presets = { "CE_ShellingPreset_Spacer", "CE_ShellingPreset_Ultra", "CE_ShellingPreset_Archotech" };
        private const float NukePoints = 6000f;   // CE antigrain shell is 7000
        private const float NukeWeight = 0.05f;

        static NuclearRetaliation()
        {
            Apply();
        }

        public static ThingDef NukeDef
        {
            get { return DefDatabase<ThingDef>.GetNamedSilentFail("MAS_Bullet_ICBM_Nuclear"); }
        }

        public static void Apply()
        {
            ThingDef nuke = NukeDef;
            if (nuke == null || MASMod.Settings == null)
            {
                return;
            }
            foreach (string name in Presets)
            {
                ShellingResponseDef preset = DefDatabase<ShellingResponseDef>.GetNamedSilentFail(name);
                if (preset == null)
                {
                    continue;
                }
                if (preset.projectiles == null)
                {
                    preset.projectiles = new List<ShellingResponseDef.ShellingResponsePart_Projectile>();
                }
                preset.projectiles.RemoveAll(p => p.projectile == nuke);
                if (MASMod.Settings.aiNuclearRetaliation)
                {
                    ShellingResponseDef.ShellingResponsePart_Projectile part = new ShellingResponseDef.ShellingResponsePart_Projectile();
                    part.projectile = nuke;
                    part.points = NukePoints;
                    part.weight = NukeWeight;
                    preset.projectiles.Add(part);
                }
            }
        }
    }

    /// <summary>Scales the retaliation budget (applies to every shell type, CE's mortars included).</summary>
    [HarmonyPatch(typeof(HostilitySheller), "TryStartShelling")]
    public static class Patch_RetaliationBudget
    {
        public static void Prefix(ref float points)
        {
            if (MASMod.Settings != null)
            {
                points *= MASMod.Settings.retaliationBudgetFactor;
            }
        }
    }

    /// <summary>
    /// Proximity self-destruct for interceptors (every CE CIWS projectile). CE keeps a missed interceptor flying
    /// with its target "taken": VerbCIWS.TryFindNewTarget skips any projectile that has an interceptor in flight,
    /// so the next defense layer (e.g. Phalanx after a Patriot miss) ran out of time. Once the interceptor has
    /// passed its closest approach and keeps getting farther, it detonates and frees the target.
    /// </summary>
    [HarmonyPatch(typeof(ProjectileCE_CIWS), "Tick")]
    public static class Patch_InterceptorProximityFuse
    {
        private const int RecedingTicksToFuse = 3;

        // Keyed by thingIDNumber but tagged with the FlightTicks of the last update: an entry is only trusted on
        // the very next tick of the same flight. After a save reload (same ids, earlier positions) or an id reused
        // by a new interceptor, FlightTicks does not follow and the entry starts over (review finding).
        private class FuseState
        {
            public float closest;
            public int receding;
            public int lastTick;
        }
        private static readonly Dictionary<int, FuseState> states = new Dictionary<int, FuseState>();

        public static void Postfix(ProjectileCE_CIWS __instance)
        {
            if (__instance.FlightTicks == 1 && __instance.Spawned)
            {
                // interceptors keep CE's linear flight (its intercept maths assume it); only launch smoke/flash
                BoostPhaseTrajectoryWorker.LaunchEffects(__instance);
            }
            int id = __instance.thingIDNumber;
            if (__instance.Destroyed || !__instance.Spawned)
            {
                states.Remove(id);
                return;
            }
            ProjectileCE target = __instance.intendedTarget.Thing as ProjectileCE;
            if (target == null || target.Destroyed)
            {
                return;   // skyfallers and vanished targets keep CE's own handling
            }
            float dist = (target.ExactPosition - __instance.ExactPosition).sqrMagnitude;
            int tick = __instance.FlightTicks;
            FuseState st;
            if (!states.TryGetValue(id, out st) || st.lastTick != tick - 1)
            {
                st = new FuseState();
                st.closest = dist;
                st.lastTick = tick;
                states[id] = st;
                return;
            }
            st.lastTick = tick;
            if (dist < st.closest)
            {
                st.closest = dist;
                st.receding = 0;
                return;
            }
            st.receding++;
            if (st.receding < RecedingTicksToFuse)
            {
                return;
            }
            states.Remove(id);
            Map map = __instance.Map;
            FleckMaker.ThrowMicroSparks(__instance.DrawPos, map);
            FleckMaker.ThrowSmoke(__instance.DrawPos, map, 1f);
            __instance.Destroy(DestroyMode.Vanish);
        }
    }
}
