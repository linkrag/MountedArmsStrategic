using System.Collections.Generic;
using System.Reflection;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// Which thing to spawn at the impact cell and with what yield. Defaults match Dubs Rimatomics:
    /// its ICBM drops an "ICBMStrike" (Rimatomics.NuclearStrike) with Yield set, and that thing does
    /// the flash and blast by itself (read from Rimatomics.dll IL). Its NuclearFallout only comes with
    /// Yield > 150 (full long-event blast), so the fallout is ours (NuclearFallout).
    /// </summary>
    public class NuclearStrikeExtension : DefModExtension
    {
        public string strikeDef = "ICBMStrike";
        public string yieldField = "Yield";
        public float yield = 55f;   // what Rimatomics uses for a manually armed fission warhead
    }

    /// <summary>
    /// Explosive CE projectile that also starts a Rimatomics nuclear strike where it lands.
    /// No compile-time reference to Rimatomics: the strike def and its field are looked up by name,
    /// and the defs using this class only load when Rimatomics is active.
    /// </summary>
    public class ProjectileCE_NuclearStrike : ProjectileCE_Explosive
    {
        public override void Impact(Thing hitThing)
        {
            Map map = Map;
            IntVec3 cell = Position;
            NuclearStrikeExtension ext = def.GetModExtension<NuclearStrikeExtension>();
            if (map != null && ext != null && cell.InBounds(map))
            {
                ThingDef strikeDef = DefDatabase<ThingDef>.GetNamedSilentFail(ext.strikeDef);
                if (strikeDef == null)
                {
                    Log.ErrorOnce("[Mounted Arms: Strategic] Nuclear strike def '" + ext.strikeDef + "' not found (is Dubs Rimatomics active?).", 0x4D415331);
                }
                else
                {
                    Thing strike = ThingMaker.MakeThing(strikeDef);
                    FieldInfo yieldInfo = strike.GetType().GetField(ext.yieldField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (yieldInfo != null && yieldInfo.FieldType == typeof(float))
                    {
                        yieldInfo.SetValue(strike, ext.yield);
                    }
                    else
                    {
                        Log.WarningOnce("[Mounted Arms: Strategic] Field '" + ext.yieldField + "' not found on " + strike.GetType().FullName + "; using its default yield.", 0x4D415332);
                    }
                    GenSpawn.Spawn(strike, cell, map);
                    NuclearFallout.Start(map);
                }
            }
            base.Impact(hitThing);
        }
    }

    /// <summary>
    /// Fallout of our nuclear ICBM. Rimatomics only starts NuclearFallout for Yield > 150, which also runs its
    /// full blast as long events (the "Detonating" screen), so we keep Yield 55 and register the condition here.
    /// Its own fallout is constant for 10-30 days (rads only: its toxic part multiplies by the resistance, so it is
    /// 0 for most pawns). Ours follows the 7-10 rule of real fallout instead: dose rate ~ t^-1.2 (t in hours after
    /// the burst, full rate in the 1st hour), lethal outdoors in the first hours and about 1% of it after 2 days.
    /// </summary>
    public static class NuclearFallout
    {
        public const int DurationTicks = 2 * GenDate.TicksPerDay;
        private const int DoseInterval = 250;
        // radiation sickness per hour outdoors at full rate: ~2 h-equivalents of dose in the first 3 h reach 0.9,
        // where Rimatomics turns it into fatal radiation; the same pawn outdoors from day 1 to 2 only gets ~0.15
        private const float PeakSicknessPerHour = 0.45f;
        // what one strength unit of DubUtils.applyRads adds to radiation sickness (resistance 0)
        private const float SicknessPerRadStrength = 0.0028758333f;

        private static readonly System.Action<Pawn, float> applyRads =
            AccessTools.TypeByName("Rimatomics.DubUtils") is System.Type t
                ? AccessTools.MethodDelegate<System.Action<Pawn, float>>(AccessTools.Method(t, "applyRads"))
                : null;

        public static void Start(Map map)
        {
            GameConditionDef falloutDef = DefDatabase<GameConditionDef>.GetNamedSilentFail("NuclearFallout");
            if (falloutDef == null)
            {
                return;
            }
            GameConditionManager manager = map.GameConditionManager;
            MapComponent_NuclearFallout comp = map.GetComponent<MapComponent_NuclearFallout>();
            // like Rimatomics' strike: the nuclear fallout replaces its reactor fallout
            GameConditionDef radioactive = DefDatabase<GameConditionDef>.GetNamedSilentFail("RadioactiveFallout");
            if (radioactive != null)
            {
                manager.GetActiveCondition(radioactive)?.End();
            }
            GameCondition active = manager.GetActiveCondition(falloutDef);
            if (active != null)
            {
                if (active.uniqueID == comp.conditionID)
                {
                    // a new burst on top of ours: back to full rate for another 2 days
                    comp.detonationTick = Find.TickManager.TicksGame;
                    active.TicksLeft = DurationTicks;
                }
                // otherwise it is Rimatomics' own (stronger, 10-30 days): leave it
                return;
            }
            GameCondition cond = GameConditionMaker.MakeCondition(falloutDef, DurationTicks);
            manager.RegisterCondition(cond);
            comp.conditionID = cond.uniqueID;
            comp.detonationTick = Find.TickManager.TicksGame;
        }

        /// <summary>Dose rate relative to the 1st hour, or -1 when the condition is not from our missile.</summary>
        public static float Rate(GameCondition cond)
        {
            MapComponent_NuclearFallout comp = cond.gameConditionManager?.ownerMap?.GetComponent<MapComponent_NuclearFallout>();
            if (comp == null || comp.conditionID < 0 || cond.uniqueID != comp.conditionID)
            {
                return -1f;
            }
            float hours = (Find.TickManager.TicksGame - comp.detonationTick) / (float)GenDate.TicksPerHour;
            return hours <= 1f ? 1f : Mathf.Pow(hours, -1.2f);
        }

        /// <summary>Replaces Rimatomics' GameConditionTick for our fallout; false = not ours.</summary>
        public static bool Tick(GameCondition cond)
        {
            float rate = Rate(cond);
            if (rate < 0f)
            {
                return false;
            }
            Map map = cond.gameConditionManager.ownerMap;
            if (applyRads != null && Find.TickManager.TicksGame % DoseInterval == 0)
            {
                float sickness = PeakSicknessPerHour * rate * DoseInterval / GenDate.TicksPerHour;
                float strength = sickness / SicknessPerRadStrength;
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = pawns.Count - 1; i >= 0; i--)
                {
                    Pawn pawn = pawns[i];
                    if (pawn.def.race.IsFlesh && pawn.Position.UsesOutdoorTemperature(map))
                    {
                        applyRads(pawn, strength);
                    }
                }
            }
            List<SkyOverlay> overlays = cond.SkyOverlays(map);
            if (overlays != null)
            {
                for (int i = 0; i < overlays.Count; i++)
                {
                    overlays[i].TickOverlay(map, 1f);
                }
            }
            return true;
        }

        /// <summary>Rimatomics' plant and food effects scaled by the dose rate (the rads are in Tick); false = not ours.</summary>
        public static bool CellEffects(GameCondition cond, IntVec3 c, Map map)
        {
            float rate = Rate(cond);
            if (rate < 0f)
            {
                return false;
            }
            if (!c.Roofed(map) && Rand.Chance(rate))
            {
                List<Thing> things = c.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (thing is Plant)
                    {
                        if (Rand.Value < 0.0065f)
                        {
                            thing.Kill();
                        }
                    }
                    else if (thing.def.category == ThingCategory.Item)
                    {
                        CompRottable rottable = thing.TryGetComp<CompRottable>();
                        if (rottable != null && rottable.Stage != RotStage.Dessicated)
                        {
                            rottable.RotProgress += 3000f;
                        }
                    }
                }
            }
            return true;
        }
    }

    /// <summary>Which NuclearFallout on this map came from our missile (GameCondition.uniqueID) and its burst tick.</summary>
    public class MapComponent_NuclearFallout : MapComponent
    {
        public int conditionID = -1;
        public int detonationTick = -1;

        public MapComponent_NuclearFallout(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref conditionID, "conditionID", -1);
            Scribe_Values.Look(ref detonationTick, "detonationTick", -1);
        }
    }

    /// <summary>Our fallout decays (NuclearFallout.Tick); Rimatomics' own keeps its behaviour. Skipped without Rimatomics.</summary>
    [HarmonyPatch]
    public static class Patch_NuclearFalloutTick
    {
        public static bool Prepare()
        {
            return AccessTools.TypeByName("Rimatomics.GameCondition_NuclearFallout") != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("Rimatomics.GameCondition_NuclearFallout:GameConditionTick");
        }

        public static bool Prefix(GameCondition __instance)
        {
            return !NuclearFallout.Tick(__instance);
        }
    }

    [HarmonyPatch]
    public static class Patch_NuclearFalloutCells
    {
        public static bool Prepare()
        {
            return AccessTools.TypeByName("Rimatomics.GameCondition_NuclearFallout") != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("Rimatomics.GameCondition_NuclearFallout:DoCellSteadyEffects");
        }

        public static bool Prefix(GameCondition __instance, IntVec3 c, Map map)
        {
            return !NuclearFallout.CellEffects(__instance, c, map);
        }
    }
}
