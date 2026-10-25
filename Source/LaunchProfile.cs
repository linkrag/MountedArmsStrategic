using System;
using System.Collections.Generic;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// How a missile leaves its launcher. Real references (see Docs/LAUNCH.md): Minuteman III is hot-launched
    /// from the silo and climbs slowly before accelerating; Tomahawk is cold-launched (a gas generator ejects
    /// it from the cell, then the booster lights); ATACMS/GMLRS are hot-launched from the pod and accelerate
    /// almost at once.
    /// </summary>
    public class LaunchProfileExtension : DefModExtension
    {
        public float boostFraction = 0f;      // share of the flight spent in the slow, accelerating boost phase
        public int launchSmoke = 0;           // smoke puffs at the launcher on the first tick
        public float launchSmokeSize = 2f;
        public bool hotLaunch = true;         // fire glow at launch; false = cold launch (puff, ignition later)
        public float ignitionAt = 0.02f;      // cold launch: flight fraction where the booster lights
    }

    [HarmonyPatch(typeof(ProjectileCE), "DrawRotation", MethodType.Getter)]
    public static class Patch_BoostDrawRotation
    {
        public static void Postfix(ProjectileCE __instance, ref Quaternion __result)
        {
            if (BoostPhaseTrajectoryWorker.IsBoosted(__instance))
            {
                __result = BoostPhaseTrajectoryWorker.EasedDrawRotation(__instance);
            }
        }
    }

    /// <summary>
    /// CE's lerped trajectory with a boost phase: the time along the same path is eased (slow start,
    /// then faster) with the same total flight time, so path, landing point and aim are unchanged.
    /// Easing g(x): x &lt; b: c(x/b)^2; else c + (1-c)(x-b)/(1-b), c = b/(2-b) (continuous speed at b).
    /// Re-entry from the world map (spawned high above the map edge) is not eased.
    /// </summary>
    public class BoostPhaseTrajectoryWorker : LerpedTrajectoryWorker
    {
        private const float ReentryHeight = 50f;   // CE TravelingShell spawns arrivals at height 200

        public override Vector3 MoveForward(ProjectileCE projectile)
        {
            LaunchEffects(projectile);
            return PositionAt(projectile, projectile.FlightTicks);
        }

        public override IEnumerable<Vector3> PredictPositions(ProjectileCE projectile, int ticks)
        {
            int end = Math.Min(projectile.ticksToImpact, ticks);
            for (int i = 0; i < end; i++)
            {
                yield return PositionAt(projectile, projectile.FlightTicks + i);
            }
        }

        /// <summary>True when this projectile flies the eased boost profile (launched from the map, profile set).</summary>
        public static bool IsBoosted(ProjectileCE p)
        {
            LaunchProfileExtension ext = p.def.GetModExtension<LaunchProfileExtension>();
            return ext != null && ext.boostFraction > 0f && p.startingTicksToImpact > 0f && p.shotHeight < ReentryHeight
                && (p.def.projectile as ProjectilePropertiesCE) != null
                && (p.def.projectile as ProjectilePropertiesCE).TrajectoryWorker is BoostPhaseTrajectoryWorker;
        }

        /// <summary>
        /// Sprite rotation from the eased drawn motion (CE draws at (x, z + 0.84*height)); replaces CE's
        /// DrawRotation, which assumes linear time and made the nose lead the slow boost phase.
        /// </summary>
        public static Quaternion EasedDrawRotation(ProjectileCE p)
        {
            Vector3 a = PositionAt(p, p.FlightTicks);
            Vector3 b = PositionAt(p, p.FlightTicks + 1);
            float dx = b.x - a.x;
            float dz = (b.z + 0.84f * Mathf.Max(0f, b.y)) - (a.z + 0.84f * Mathf.Max(0f, a.y));
            if (dx * dx + dz * dz < 0.0000001f)
            {
                dz = 0.001f;   // straight up out of the silo: point north (screen up)
            }
            return Quaternion.LookRotation(new Vector3(dx, 0f, dz));
        }

        private static Vector3 PositionAt(ProjectileCE p, int tick)
        {
            float total = p.startingTicksToImpact;
            float eased = tick;
            LaunchProfileExtension ext = p.def.GetModExtension<LaunchProfileExtension>();
            if (ext != null && ext.boostFraction > 0f && total > 0f && p.shotHeight < ReentryHeight)
            {
                eased = total * Ease(Mathf.Clamp01(tick / total), ext.boostFraction);
            }
            Vector2 flat = Vector2.LerpUnclamped(p.origin, p.Destination, eased / total);
            float seconds = eased / GenTicks.TicksPerRealSecond;
            float height = p.shotHeight + p.shotSpeed * Mathf.Sin(p.shotAngle) * seconds - p.GravityPerWidth * seconds * seconds / 2f;
            return new Vector3(flat.x, (float)Math.Round(height, 3), flat.y);
        }

        private static float Ease(float x, float b)
        {
            float c = b / (2f - b);
            if (x < b)
            {
                float k = x / b;
                return c * k * k;
            }
            return c + (1f - c) * (x - b) / (1f - b);
        }

        public static void LaunchEffects(ProjectileCE p)
        {
            LaunchProfileExtension ext = p.def.GetModExtension<LaunchProfileExtension>();
            Map map = p.Map;
            if (ext == null || map == null || p.shotHeight >= ReentryHeight || p.startingTicksToImpact <= 0f)
            {
                return;
            }
            Vector3 origin = new Vector3(p.origin.x, 0f, p.origin.y);
            if (p.FlightTicks <= 1)
            {
                for (int i = 0; i < ext.launchSmoke; i++)
                {
                    FleckMaker.ThrowSmoke(origin + new Vector3(Rand.Range(-1f, 1f), 0f, Rand.Range(-1f, 1f)), map, ext.launchSmokeSize * Rand.Range(0.7f, 1.3f));
                }
                if (ext.hotLaunch)
                {
                    FleckMaker.ThrowFireGlow(origin, map, ext.launchSmokeSize);
                    FleckMaker.ThrowMicroSparks(origin, map);
                }
                return;
            }
            if (!ext.hotLaunch)
            {
                // cold launch: the booster lights once the missile has cleared the cell
                int ignitionTick = Mathf.Max(2, Mathf.RoundToInt(p.startingTicksToImpact * ext.ignitionAt));
                if (p.FlightTicks == ignitionTick)
                {
                    Vector3 pos = p.ExactPosition.WithY(0f);
                    FleckMaker.ThrowFireGlow(pos, map, ext.launchSmokeSize);
                    FleckMaker.ThrowSmoke(pos, map, ext.launchSmokeSize);
                    FleckMaker.ThrowMicroSparks(pos, map);
                }
            }
        }
    }
}
