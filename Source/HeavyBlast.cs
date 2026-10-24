using System.Collections.Generic;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>Settings for a very large conventional blast (see ProjectileCE_HeavyBlast).</summary>
    public class HeavyBlastExtension : DefModExtension
    {
        public float radius = 24.9f;
        public float coreFraction = 0.35f;       // inside radius*coreFraction things are destroyed outright
        public int damage = 8000;                 // at the core edge; falls to 15% at the rim
        public float armorPenetration = 50f;
        public DamageDef damageDef;               // default Bomb
        public float fireChance = 0f;             // per cell, scaled by falloff (thermobaric)
        public float fxRadius = 4.9f;             // small real explosion for the flash and sound
        public ThingDef crater;
    }

    /// <summary>
    /// Large conventional warhead done the way Dubs Rimatomics does its nuke: it skips CE's cell-by-cell
    /// ExplosionCE: things in the core are destroyed directly, the rest takes one damage pass that falls off
    /// with distance. Stored CE ammo in the blast vanishes instead of cooking off (a base full of ammo turned
    /// one blast into thousands of fragments). Runs at once (the long-event "Detonating" screen was dropped
    /// at the user's request). CE's ExplosionCE at r24.9 (~1950 cells, hundreds of walls) dropped the frame rate.
    /// </summary>
    public class ProjectileCE_HeavyBlast : ProjectileCE_Explosive
    {
        public override void Impact(Thing hitThing)
        {
            HeavyBlastExtension ext = def.GetModExtension<HeavyBlastExtension>();
            Map map = Map;
            if (ext == null || map == null)
            {
                base.Impact(hitThing);
                return;
            }
            IntVec3 center = Position;
            Thing instigator = launcher;
            ThingDef projectileDef = def;
            Destroy(DestroyMode.Vanish);
            Detonate(map, center, ext, instigator, projectileDef);
        }

        private static void Detonate(Map map, IntVec3 center, HeavyBlastExtension ext, Thing instigator, ThingDef projectileDef)
        {
            if (map == null || !Find.Maps.Contains(map) || !center.InBounds(map))
            {
                return;
            }
            DamageDef damageDef = ext.damageDef ?? DamageDefOf.Bomb;
            float radius = ext.radius;
            float core = radius * ext.coreFraction;
            List<Thing> things = new List<Thing>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                float dist = cell.DistanceTo(center);
                bool inCore = dist <= core;
                float falloff = inCore ? 1f : 1f - 0.85f * (dist - core) / Mathf.Max(radius - core, 0.1f);
                int amount = Mathf.Max(1, Mathf.RoundToInt(ext.damage * falloff));

                if (inCore)
                {
                    RoofDef roof = map.roofGrid.RoofAt(cell);
                    if (roof != null && !roof.isThickRoof)
                    {
                        map.roofGrid.SetRoof(cell, null);
                    }
                }

                things.Clear();
                things.AddRange(cell.GetThingList(map));
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t.Destroyed || !t.def.useHitPoints && !(t is Pawn))
                    {
                        continue;
                    }
                    if (t.def is AmmoDef)
                    {
                        t.Destroy(DestroyMode.Vanish);   // no cook-off chain (each stack would throw fragments)
                        continue;
                    }
                    bool naturalRock = t.def.building != null && t.def.building.isNaturalRock;
                    if (inCore && !(t is Pawn) && !naturalRock)
                    {
                        t.Destroy(DestroyMode.KillFinalize);
                        continue;
                    }
                    t.TakeDamage(new DamageInfo(damageDef, amount, ext.armorPenetration, -1f, instigator, null, projectileDef));
                }

                if (ext.fireChance > 0f && Rand.Chance(ext.fireChance * falloff))
                {
                    FireUtility.TryStartFireIn(cell, map, Rand.Range(0.3f, 0.9f), instigator);
                }
            }

            if (ext.crater != null)
            {
                GenSpawn.Spawn(ext.crater, center, map, WipeMode.Vanish);
            }
            GenExplosion.DoExplosion(center, map, ext.fxRadius, damageDef, instigator, Mathf.RoundToInt(ext.damage * 0.1f), -1f, SoundDef.Named("Explosion_GiantBomb"));
            Find.CameraDriver.shaker.DoShake(4f);
        }
    }
}
