using System.Collections.Generic;
using System.Reflection;
using CombatExtended;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// A small nuclear warhead with our own blast (ProjectileCE_HeavyBlast + HeavyBlastExtension) instead of
    /// Rimatomics' strike: Rimatomics' blast radii are fixed below Yield 150 (25 destroyed, 55 plasma), so a
    /// "small" yield there is as big as the Minuteman. Starts our fallout with NuclearFalloutExtension.scale.
    /// </summary>
    public class ProjectileCE_NuclearBlast : ProjectileCE_HeavyBlast
    {
        public override void Impact(Thing hitThing)
        {
            Map map = Map;
            float scale = def.GetModExtension<NuclearFalloutExtension>()?.scale ?? 1f;
            base.Impact(hitThing);
            if (map != null && Find.Maps.Contains(map))
            {
                NuclearFallout.Start(map, scale);
            }
        }
    }

    /// <summary>How a MIRV bus splits over the target map (ProjectileCE_Mirv).</summary>
    public class MirvExtension : DefModExtension
    {
        public ThingDef warheadDef;
        public int warheads = 10;
        public float splitAt = 0.2f;       // share of the re-entry flight before the bus releases its warheads
        public float shotSpeed = 55f;      // what CE's TravelingShell uses for arrivals
        public int edgeMargin = 10;        // warheads never aim this close to the map edge
        public int candidates = 30;        // best-candidate sampling: tries per warhead
    }

    /// <summary>
    /// MIRV bus (RS-28 Sarmat). From the silo it flies like any ICBM; when it re-enters over a loaded map (CE spawns
    /// world arrivals at height 200, see BoostPhaseTrajectoryWorker.ReentryHeight) it releases MirvExtension.warheads
    /// separate warhead projectiles at splitAt of the flight and disappears. Each warhead is a normal CE projectile,
    /// so the missile defense can shoot each one down. Targets: the bus's aim cell, the rest spread over the whole
    /// map (best-candidate sampling: each new target is the farthest of `candidates` random cells from the chosen).
    /// If it lands without splitting it detonates as one warhead (its own HeavyBlastExtension).
    /// </summary>
    public class ProjectileCE_Mirv : ProjectileCE_NuclearBlast
    {
        private const float ReentryHeight = 50f;
        private static readonly float[] Speeds = { 1f, 1.5f, 2.2f, 3.2f };

        // BaseTrajectoryWorker.TryFindShotAngle is private: ShotAngle logs a warning and shoots at 45 degrees
        // when a cell is out of reach, so the speed is raised until the solver finds an angle
        private static readonly MethodInfo tryFindShotAngle =
            AccessTools.Method(typeof(BaseTrajectoryWorker), "TryFindShotAngle");

        public override void Tick()
        {
            MirvExtension ext = def.GetModExtension<MirvExtension>();
            if (Spawned && !landed && ext != null && ext.warheadDef != null && shotHeight >= ReentryHeight
                && startingTicksToImpact > 0f && FlightTicks >= startingTicksToImpact * ext.splitAt)
            {
                Release(ext);
                Destroy(DestroyMode.Vanish);
                return;
            }
            base.Tick();
        }

        private void Release(MirvExtension ext)
        {
            Map map = Map;
            Vector3 from = ExactPosition;
            IntVec3 spawnCell = Position.ClampInsideMap(map);
            IntVec3 aim = new IntVec3(Mathf.RoundToInt(Destination.x), 0, Mathf.RoundToInt(Destination.y)).ClampInsideMap(map);
            foreach (IntVec3 cell in PickTargets(map, aim, ext))
            {
                ProjectileCE warhead = (ProjectileCE)ThingMaker.MakeThing(ext.warheadDef);
                ProjectilePropertiesCE props = (ProjectilePropertiesCE)warhead.def.projectile;
                Vector3 to = cell.ToVector3Shifted();
                float speed = ext.shotSpeed;
                float? angle = null;
                foreach (float factor in Speeds)
                {
                    speed = ext.shotSpeed * factor;
                    angle = tryFindShotAngle?.Invoke(null, new object[] { props, from, to, (float?)speed }) as float?;
                    if (angle.HasValue)
                    {
                        break;
                    }
                }
                float shotAngle = angle ?? props.TrajectoryWorker.ShotAngle(props, from, to, speed);
                float shotRotation = props.TrajectoryWorker.ShotRotation(props, from, to);
                // same spawn as CE's TravelingShell.LaunchProjectile
                warhead.canTargetSelf = false;
                warhead.Position = spawnCell;
                warhead.SpawnSetup(map, false);
                warhead.Launch(launcher, new Vector2(from.x, from.z), shotAngle, shotRotation, from.y, speed, equipment);
            }
        }

        private static List<IntVec3> PickTargets(Map map, IntVec3 aim, MirvExtension ext)
        {
            List<IntVec3> chosen = new List<IntVec3> { aim };
            int margin = Mathf.Min(ext.edgeMargin, Mathf.Min(map.Size.x, map.Size.z) / 4);
            for (int i = 1; i < ext.warheads; i++)
            {
                IntVec3 best = aim;
                float bestDist = -1f;
                for (int c = 0; c < ext.candidates; c++)
                {
                    IntVec3 cell = new IntVec3(Rand.Range(margin, map.Size.x - margin), 0, Rand.Range(margin, map.Size.z - margin));
                    float nearest = float.MaxValue;
                    for (int j = 0; j < chosen.Count; j++)
                    {
                        nearest = Mathf.Min(nearest, (cell - chosen[j]).LengthHorizontalSquared);
                    }
                    if (nearest > bestDist)
                    {
                        bestDist = nearest;
                        best = cell;
                    }
                }
                chosen.Add(best);
            }
            return chosen;
        }
    }
}
