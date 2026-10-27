using System.Collections.Generic;
using System.Reflection;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>Fallout strength of a nuclear warhead, relative to the 55-yield Minuteman (NuclearFallout).</summary>
    public class NuclearFalloutExtension : DefModExtension
    {
        public float scale = 1f;
        public float cloudScale = 1.2f;   // Rimatomics' mushroom cloud (Mote_NukeCloud.AtomicScale; it uses 1.2 and 2.2)
    }

    /// <summary>
    /// Nuclear warhead: our blast (ProjectileCE_HeavyBlast + HeavyBlastExtension, R = the 2 psi contour) instead of
    /// Rimatomics' NuclearStrike, plus Rimatomics' mushroom cloud and our fallout. Rimatomics' strike (lote 13 test)
    /// logged "already-destroyed" and "null map" errors and CE fragments out of bounds, deleted everything in r25
    /// below Yield 150 and used the "Detonating" long-event screen above it. R = the Rimatomics yield, which was its
    /// zero-damage radius (damage Yield * ln(Yield / d)), so the warheads keep their reach. The defs using this class
    /// only load with Rimatomics; its defs are looked up by name (no compile-time reference).
    /// </summary>
    public class ProjectileCE_NuclearBlast : ProjectileCE_HeavyBlast
    {
        private static FieldInfo atomicScale;

        public override void Impact(Thing hitThing)
        {
            Map map = Map;
            IntVec3 cell = Position;
            NuclearFalloutExtension ext = def.GetModExtension<NuclearFalloutExtension>();
            base.Impact(hitThing);
            if (map == null || !Find.Maps.Contains(map) || !cell.InBounds(map))
            {
                return;
            }
            SpawnCloud(cell, map, ext?.cloudScale ?? 1.2f);
            NuclearFallout.Start(map, ext?.scale ?? 1f);
        }

        private static void SpawnCloud(IntVec3 cell, Map map, float scale)
        {
            ThingDef cloudDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mote_NukeCloud");
            if (cloudDef == null || scale <= 0f)
            {
                return;
            }
            Thing cloud = ThingMaker.MakeThing(cloudDef);
            if (cloud is Mote mote)
            {
                mote.exactPosition = cell.ToVector3();
            }
            atomicScale = atomicScale ?? cloud.GetType().GetField("AtomicScale", BindingFlags.Public | BindingFlags.Instance);
            if (atomicScale != null && atomicScale.FieldType == typeof(float))
            {
                atomicScale.SetValue(cloud, scale);
            }
            GenSpawn.Spawn(cloud, cell, map);
        }
    }

    /// <summary>
    /// Fallout of our nuclear warheads (Rimatomics' NuclearFallout condition, registered and driven by us).
    /// Rimatomics' own fallout (from its strike above Yield 150) is constant for 10-30 days (rads only: its toxic part multiplies by the resistance, so it is
    /// 0 for most pawns). Ours follows real local fallout instead (Docs/STATUS.md, lote 12, has the sources):
    /// it starts falling ~30 min after the burst and builds up to the H+1 rate, then decays by the 7-10 rule
    /// (dose rate ~ t^-1.2, t in hours: 10% at H+7, 1% at H+48). The first 2-3 days are the dangerous ones; from
    /// day 3 the dose is below Rimatomics' natural recovery (-0.08/day), so the condition ends there.
    /// </summary>
    public static class NuclearFallout
    {
        public const int DurationTicks = 3 * GenDate.TicksPerDay;
        private const int DoseInterval = 250;
        private const float ArrivalHours = 0.5f;
        // radiation sickness per hour outdoors at the H+1 rate (~350 R/h; 0.9, where Rimatomics turns it into fatal
        // radiation, is about the LD50 of 4-5 Gy): outdoors for the first 3 h reaches ~0.9, outdoors on day 2 ~0.15
        private const float PeakSicknessPerHour = 0.7f;
        // fraction of the outdoor dose (1 / protection factor): main floor of a house PF 2-5, an open shed less,
        // under overhead mountain like a deep shelter (PF > 1000)
        private const float RoomFactor = 0.2f;
        private const float OpenRoofFactor = 0.5f;
        private const float ThickRoofFactor = 0.001f;
        // a salvo or a big warhead never goes past 3x the Minuteman (outdoors lethal in ~1 h)
        private const float MaxScale = 3f;
        // what one strength unit of DubUtils.applyRads adds to radiation sickness (resistance 0)
        private const float SicknessPerRadStrength = 0.0028758333f;

        private static readonly System.Action<Pawn, float> applyRads =
            AccessTools.TypeByName("Rimatomics.DubUtils") is System.Type t
                ? AccessTools.MethodDelegate<System.Action<Pawn, float>>(AccessTools.Method(t, "applyRads"))
                : null;
        private static readonly HediffDef fatalRad = DefDatabase<HediffDef>.GetNamedSilentFail("FatalRad");

        /// <summary>
        /// Starts (or adds to) our fallout on the map. scale multiplies the dose rate (1 = the 55-yield Minuteman).
        /// Bursts less than an hour apart (a MIRV salvo) add up, capped at MaxScale; a later burst restarts the curve
        /// with its own scale plus what is left of the old one.
        /// </summary>
        public static void Start(Map map, float scale)
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
                    int now = Find.TickManager.TicksGame;
                    if (now - comp.detonationTick < GenDate.TicksPerHour)
                    {
                        comp.scale = Mathf.Min(MaxScale, comp.scale + scale);
                    }
                    else
                    {
                        // a new burst on top of ours: its fallout starts over, for another 3 days
                        comp.scale = Mathf.Min(MaxScale, scale + Rate(active));
                        comp.detonationTick = now;
                        active.TicksLeft = DurationTicks;
                    }
                }
                // otherwise it is Rimatomics' own (stronger, 10-30 days): leave it
                return;
            }
            GameCondition cond = GameConditionMaker.MakeCondition(falloutDef, DurationTicks);
            manager.RegisterCondition(cond);
            comp.conditionID = cond.uniqueID;
            comp.detonationTick = Find.TickManager.TicksGame;
            comp.scale = Mathf.Min(MaxScale, scale);
        }

        /// <summary>Dose rate relative to the 55-yield Minuteman's 1st hour, or -1 when the condition is not ours.</summary>
        public static float Rate(GameCondition cond)
        {
            MapComponent_NuclearFallout comp = cond.gameConditionManager?.ownerMap?.GetComponent<MapComponent_NuclearFallout>();
            if (comp == null || comp.conditionID < 0 || cond.uniqueID != comp.conditionID)
            {
                return -1f;
            }
            float hours = (Find.TickManager.TicksGame - comp.detonationTick) / (float)GenDate.TicksPerHour;
            if (hours < ArrivalHours)
            {
                return 0f;
            }
            float curve = hours <= 1f ? (hours - ArrivalHours) / (1f - ArrivalHours) : Mathf.Pow(hours, -1.2f);
            return curve * comp.scale;
        }

        /// <summary>Share of the outdoor dose a pawn gets where it stands (shielding by roof and walls).</summary>
        private static float Shielding(IntVec3 c, Map map)
        {
            RoofDef roof = c.GetRoof(map);
            if (roof == null)
            {
                return 1f;
            }
            if (roof.isThickRoof)
            {
                return ThickRoofFactor;
            }
            return c.UsesOutdoorTemperature(map) ? OpenRoofFactor : RoomFactor;
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
            if (applyRads != null && rate > 0f && Find.TickManager.TicksGame % DoseInterval == 0)
            {
                float sickness = PeakSicknessPerHour * rate * DoseInterval / GenDate.TicksPerHour;
                float strength = sickness / SicknessPerRadStrength;
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = pawns.Count - 1; i >= 0; i--)
                {
                    Pawn pawn = pawns[i];
                    // applyRads feeds every later dose straight into FatalRad, which killed pawns ~7 h after the
                    // fatal threshold; ~7-14 Gy kills in 1-3 weeks, so after it only Rimatomics' own course runs
                    if (pawn.def.race.IsFlesh && (fatalRad == null || !pawn.health.hediffSet.HasHediff(fatalRad)))
                    {
                        applyRads(pawn, strength * Shielding(pawn.Position, map));
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
        public float scale = 1f;

        public MapComponent_NuclearFallout(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref conditionID, "conditionID", -1);
            Scribe_Values.Look(ref detonationTick, "detonationTick", -1);
            Scribe_Values.Look(ref scale, "scale", 1f);
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
