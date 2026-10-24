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
        private static readonly Dictionary<int, float> closest = new Dictionary<int, float>();
        private static readonly Dictionary<int, int> receding = new Dictionary<int, int>();

        public static void Postfix(ProjectileCE_CIWS __instance)
        {
            int id = __instance.thingIDNumber;
            if (__instance.Destroyed || !__instance.Spawned)
            {
                closest.Remove(id);
                receding.Remove(id);
                return;
            }
            ProjectileCE target = __instance.intendedTarget.Thing as ProjectileCE;
            if (target == null || target.Destroyed)
            {
                return;   // skyfallers and vanished targets keep CE's own handling
            }
            float dist = (target.ExactPosition - __instance.ExactPosition).sqrMagnitude;
            float best;
            if (!closest.TryGetValue(id, out best) || dist < best)
            {
                closest[id] = dist;
                receding[id] = 0;
                return;
            }
            int count;
            receding.TryGetValue(id, out count);
            count++;
            if (count < RecedingTicksToFuse)
            {
                receding[id] = count;
                return;
            }
            closest.Remove(id);
            receding.Remove(id);
            Map map = __instance.Map;
            FleckMaker.ThrowMicroSparks(__instance.DrawPos, map);
            FleckMaker.ThrowSmoke(__instance.DrawPos, map, 1f);
            __instance.Destroy(DestroyMode.Vanish);
        }
    }
}
