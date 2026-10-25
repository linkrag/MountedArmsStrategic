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
            HashSet<Thing> processed = new HashSet<Thing>();   // multi-cell things are hit once (review finding)

            List<IntVec3> cells = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map))
                {
                    cells.Add(cell);
                }
            }

            // Pass 1: the blast wave strips every non-thick roof in the radius first. Destroying the walls under a
            // roof made it cave in cascade-style (crushing things already being destroyed: "Tried to destroy
            // already-destroyed thing" spam and a hang on the first test).
            for (int c = 0; c < cells.Count; c++)
            {
                RoofDef roof = map.roofGrid.RoofAt(cells[c]);
                if (roof != null && !roof.isThickRoof)
                {
                    map.roofGrid.SetRoof(cells[c], null);
                }
            }

            // Pass 2: destruction. Pawns take real damage; buildings/items/plants lose hit points directly (no CE
            // damage pipeline per thing); natural rock is left alone (destroying it spawned chunks on despawned
            // cells and brought mountain roofs down); stored CE ammo vanishes (no cook-off chain).
            for (int c = 0; c < cells.Count; c++)
            {
                IntVec3 cell = cells[c];
                float dist = cell.DistanceTo(center);
                bool inCore = dist <= core;
                float falloff = inCore ? 1f : 1f - 0.85f * (dist - core) / Mathf.Max(radius - core, 0.1f);
                int amount = Mathf.Max(1, Mathf.RoundToInt(ext.damage * falloff));

                things.Clear();
                things.AddRange(cell.GetThingList(map));
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t.Destroyed || !t.Spawned || t.Map != map || !processed.Add(t))
                    {
                        continue;
                    }
                    Pawn pawn = t as Pawn;
                    if (pawn != null)
                    {
                        pawn.TakeDamage(new DamageInfo(damageDef, amount, ext.armorPenetration, -1f, instigator, null, projectileDef));
                        continue;
                    }
                    if (!t.def.useHitPoints || (t.def.building != null && t.def.building.isNaturalRock))
                    {
                        continue;
                    }
                    if (t.def is AmmoDef)
                    {
                        t.Destroy(DestroyMode.Vanish);
                        continue;
                    }
                    if (inCore)
                    {
                        t.Destroy(DestroyMode.KillFinalize);
                        continue;
                    }
                    float factor = t.def.category == ThingCategory.Building ? damageDef.buildingDamageFactor
                        : t.def.category == ThingCategory.Plant ? damageDef.plantDamageFactor : 1f;
                    int hp = t.HitPoints - Mathf.RoundToInt(amount * factor);
                    if (hp <= 0)
                    {
                        t.Destroy(DestroyMode.KillFinalize);
                    }
                    else
                    {
                        t.HitPoints = hp;
                    }
                }

                if (ext.fireChance > 0f && Rand.Chance(ext.fireChance * falloff))
                {
                    FireUtility.TryStartFireIn(cell, map, Rand.Range(0.3f, 0.9f), instigator);
                }
            }

            if (ext.crater != null && CanPlaceCrater(ext.crater, center, map))
            {
                GenSpawn.Spawn(ext.crater, center, map, WipeMode.Vanish);
            }
            GenExplosion.DoExplosion(center, map, ext.fxRadius, damageDef, instigator, Mathf.RoundToInt(ext.damage * 0.1f), -1f, SoundDef.Named("Explosion_GiantBomb"));
            if (map == Find.CurrentMap)
            {
                Find.CameraDriver.shaker.DoShake(4f);
            }
        }

        // the crater is a multi-cell building: inside the map and not over anything that must not be wiped
        private static bool CanPlaceCrater(ThingDef crater, IntVec3 center, Map map)
        {
            CellRect rect = GenAdj.OccupiedRect(center, Rot4.North, crater.size);
            if (!rect.InBounds(map))
            {
                return false;
            }
            foreach (IntVec3 c in rect)
            {
                List<Thing> list = c.GetThingList(map);
                for (int i = 0; i < list.Count; i++)
                {
                    if (!list[i].def.destroyable)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
