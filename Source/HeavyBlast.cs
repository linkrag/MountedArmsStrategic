using System.Collections.Generic;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>Settings for a large warhead blast (see ProjectileCE_HeavyBlast and Docs/BLAST.md).</summary>
    public class HeavyBlastExtension : DefModExtension
    {
        public float radius = 24.9f;              // R = the ~2 psi contour (outer edge of meaningful damage)
        public bool thermobaric = false;          // wider rings, confinement x1.5, fire table
        public DamageDef damageDef;               // damage type for pawns (default Bomb)
        public float fxRadius = 4.9f;             // small real explosion for the flash and sound only
        public ThingDef crater;
    }

    /// <summary>
    /// Large warhead blast with real overpressure zones (Kingery-Bulmash cube-root scaling; damage criteria from
    /// Glasstone &amp; Dolan / TM 5-1300 / OTA / FM 8-9; research in Docs/BLAST.md). The zone radii are fixed
    /// fractions of R because, under cube-root scaling, the ratios between overpressure contours do not depend on
    /// the charge mass; the per-zone damage depends only on overpressure, so it is the same for every warhead.
    ///   core      0 - 0.25R   >= ~22 psi   everything destroyed (except natural rock)
    ///   severe A  -  0.37R    22 - 10 psi  buildings -900 hp (reinforced concrete demolished)
    ///   severe B  -  0.50R    10 - 5.8 psi buildings -600 hp (most buildings collapse)
    ///   heavy     -  0.75R    5.8 - 3 psi  buildings -250 hp (houses collapse)
    ///   moderate  -  1.0R     3 - 2 psi    buildings -100 hp, doors -75% (doors/windows/roofs blown out)
    /// Thermobaric: same core, rings out to 0.55R / 0.82R, x1.5 damage in cells that were roofed (confinement,
    /// a game choice: the sources give no number), fire 100/60/60/25/5%.
    /// Performance (the first version hung the game): every non-thick roof in R is stripped first (game rule, so
    /// nothing caves in cascade-style), buildings/items/plants lose hit points directly (also avoids Bomb's
    /// building factor multiplying the values), natural rock is left alone, stored CE ammo vanishes (no cook-off
    /// chain), anything that explodes on death (chemfuel, turrets, IEDs...) vanishes too, each thing is processed
    /// once, fires are capped (picked at random over the whole area, so outer rings still burn).
    /// </summary>
    public class ProjectileCE_HeavyBlast : ProjectileCE_Explosive
    {
        private const float CoreEdge = 0.25f;
        private const float SevereAEdge = 0.37f;
        private const int MaxFires = 200;

        // per zone (core, severe A, severe B, heavy, moderate)
        private static readonly int[] BuildingDamage = { int.MaxValue, 900, 600, 250, 100 };
        private static readonly int[] PawnDamageMin = { 400, 150, 80, 30, 10 };
        private static readonly int[] PawnDamageMax = { 500, 250, 150, 60, 25 };
        private static readonly int[] PawnHits = { 5, 4, 3, 2, 1 };
        private static readonly float[] ItemDestroyChance = { 1f, 0.9f, 0.7f, 0.25f, 0f };
        private static readonly float[] ItemLossMin = { 1f, 0.5f, 0.5f, 0.2f, 0.1f };
        private static readonly float[] ItemLossMax = { 1f, 0.9f, 0.9f, 0.5f, 0.2f };
        private static readonly float[] PlantDestroyChance = { 1f, 1f, 1f, 0.5f, 0.1f };
        private static readonly float[] FireHE = { 0.3f, 0.1f, 0.1f, 0f, 0f };
        private static readonly float[] FireThermobaric = { 1f, 0.6f, 0.6f, 0.25f, 0.05f };

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

        private static int ZoneOf(float fraction, bool thermobaric)
        {
            if (fraction <= CoreEdge)
            {
                return 0;
            }
            if (fraction <= SevereAEdge)
            {
                return 1;
            }
            if (fraction <= (thermobaric ? 0.55f : 0.50f))
            {
                return 2;
            }
            if (fraction <= (thermobaric ? 0.82f : 0.75f))
            {
                return 3;
            }
            return 4;
        }

        private static void Detonate(Map map, IntVec3 center, HeavyBlastExtension ext, Thing instigator, ThingDef projectileDef)
        {
            if (map == null || !Find.Maps.Contains(map) || !center.InBounds(map))
            {
                return;
            }
            DamageDef pawnDamage = ext.damageDef ?? DamageDefOf.Bomb;
            float radius = Mathf.Max(ext.radius, 1f);

            List<IntVec3> cells = new List<IntVec3>();
            List<bool> confined = new List<bool>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map))
                {
                    cells.Add(cell);
                    confined.Add(map.roofGrid.Roofed(cell));   // snapshot before stripping (thermobaric confinement)
                }
            }

            // Pass 1: strip every non-thick roof in R (game rule; prevents cascade cave-ins).
            for (int c = 0; c < cells.Count; c++)
            {
                RoofDef roof = map.roofGrid.RoofAt(cells[c]);
                if (roof != null && !roof.isThickRoof)
                {
                    map.roofGrid.SetRoof(cells[c], null);
                }
            }

            // Pass 2: damage by zone.
            HashSet<Thing> processed = new HashSet<Thing>();
            List<IntVec3> fireCells = new List<IntVec3>();
            int coreCells = 0;
            for (int c = 0; c < cells.Count; c++)
            {
                IntVec3 cell = cells[c];
                int zone = ZoneOf(cell.DistanceTo(center) / radius, ext.thermobaric);
                if (zone == 0)
                {
                    coreCells = c + 1;   // radial order: the core is a prefix of the list
                }
                float mult = ext.thermobaric && zone > 0 && confined[c] ? 1.5f : 1f;
                DamageCell(map, cell, zone, mult, pawnDamage, instigator, projectileDef, processed);

                float fire = ext.thermobaric ? FireThermobaric[zone] : FireHE[zone];
                if (fire > 0f && Rand.Chance(fire))
                {
                    fireCells.Add(cell);
                }
            }
            // Pass 3: whatever appeared in the core during pass 2 (corpses, dropped gear, ejected contents).
            for (int c = 0; c < coreCells; c++)
            {
                DamageCell(map, cells[c], 0, 1f, pawnDamage, instigator, projectileDef, processed);
            }

            fireCells.Shuffle();
            int fires = 0;
            for (int i = 0; i < fireCells.Count && fires < MaxFires; i++)
            {
                if (FireUtility.TryStartFireIn(fireCells[i], map, Rand.Range(0.3f, 0.9f), instigator))
                {
                    fires++;
                }
            }

            if (ext.crater != null && CanPlaceCrater(ext.crater, center, map))
            {
                GenSpawn.Spawn(ext.crater, center, map, WipeMode.Vanish);
            }
            // flash + sound only (damage 1: the zones above already did the work)
            SoundDef sound = projectileDef.projectile != null && projectileDef.projectile.soundExplode != null
                ? projectileDef.projectile.soundExplode : SoundDef.Named("Explosion_GiantBomb");
            GenExplosion.DoExplosion(center, map, ext.fxRadius, DamageDefOf.Bomb, instigator, 1, -1f, sound);
            if (map == Find.CurrentMap)
            {
                Find.CameraDriver.shaker.DoShake(Mathf.Clamp(radius / 6f, 1f, 4f));
            }
        }

        private static void DamageCell(Map map, IntVec3 cell, int zone, float mult, DamageDef pawnDamage, Thing instigator,
            ThingDef projectileDef, HashSet<Thing> processed)
        {
            List<Thing> things = new List<Thing>(cell.GetThingList(map));
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
                    DamagePawn(pawn, zone, mult, pawnDamage, instigator, projectileDef);
                    continue;
                }
                if (!t.def.useHitPoints || IsRock(t.def))
                {
                    continue;
                }
                if (t.def is AmmoDef || ExplodesOnDeath(t))
                {
                    t.Destroy(DestroyMode.Vanish);   // no cook-off / secondary explosion chain
                    continue;
                }
                if (zone == 0)
                {
                    t.Destroy(DestroyMode.KillFinalize);
                    continue;
                }
                switch (t.def.category)
                {
                    case ThingCategory.Building:
                        int dmg = Mathf.RoundToInt(BuildingDamage[zone] * mult);
                        if (zone == 4 && t is Building_Door)
                        {
                            dmg = Mathf.Max(dmg, Mathf.RoundToInt(t.MaxHitPoints * 0.75f));   // doors blown in at 2-3 psi
                        }
                        LoseHitPoints(t, dmg);
                        break;
                    case ThingCategory.Plant:
                        if (Rand.Chance(PlantDestroyChance[zone]))
                        {
                            t.Destroy(DestroyMode.KillFinalize);
                        }
                        else
                        {
                            LoseHitPoints(t, Mathf.RoundToInt(t.MaxHitPoints * 0.7f));
                        }
                        break;
                    default:
                        if (Rand.Chance(ItemDestroyChance[zone]))
                        {
                            t.Destroy(DestroyMode.KillFinalize);
                        }
                        else
                        {
                            LoseHitPoints(t, Mathf.RoundToInt(t.MaxHitPoints * Rand.Range(ItemLossMin[zone], ItemLossMax[zone]) * mult));
                        }
                        break;
                }
            }
        }

        // natural, smoothed and collapsed rock and ore: they hold the mountain roof (removing them caves it in)
        private static bool IsRock(ThingDef def)
        {
            return def.mineable || (def.building != null && (def.building.isNaturalRock || def.building.isResourceRock));
        }

        private static bool ExplodesOnDeath(Thing t)
        {
            ThingWithComps twc = t as ThingWithComps;
            return twc != null && (twc.GetComp<CompExplosive>() != null || twc.GetComp<CompExplosiveCE>() != null);
        }

        private static void DamagePawn(Pawn pawn, int zone, float mult, DamageDef damageDef, Thing instigator, ThingDef projectileDef)
        {
            int total = Mathf.RoundToInt(Rand.RangeInclusive(PawnDamageMin[zone], PawnDamageMax[zone]) * mult);
            int hits = PawnHits[zone];
            int perHit = Mathf.Max(1, total / hits);
            for (int h = 0; h < hits && !pawn.Destroyed && !pawn.Dead; h++)
            {
                pawn.TakeDamage(new DamageInfo(damageDef, perHit, 0f, -1f, instigator, null, projectileDef));
            }
        }

        private static void LoseHitPoints(Thing t, int amount)
        {
            int hp = t.HitPoints - Mathf.Max(1, amount);
            if (hp <= 0)
            {
                t.Destroy(DestroyMode.KillFinalize);
            }
            else
            {
                t.HitPoints = hp;
                Building b = t as Building;
                if (b != null && b.Spawned)
                {
                    b.Map.listerBuildingsRepairable.Notify_BuildingTookDamage(b);   // normally done by PostApplyDamage
                }
            }
        }

        // the crater is a multi-cell building spawned with WipeMode.Vanish: inside the map, on buildable ground,
        // and never over rock or anything else that survived the blast (it would be deleted silently)
        private static bool CanPlaceCrater(ThingDef crater, IntVec3 center, Map map)
        {
            CellRect rect = GenAdj.OccupiedRect(center, Rot4.North, crater.size);
            if (!rect.InBounds(map))
            {
                return false;
            }
            foreach (IntVec3 c in rect)
            {
                if (c.GetEdifice(map) != null || !c.SupportsStructureType(map, crater.terrainAffordanceNeeded))
                {
                    return false;
                }
                List<Thing> list = c.GetThingList(map);
                for (int i = 0; i < list.Count; i++)
                {
                    if (!list[i].def.destroyable || IsRock(list[i].def))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// CE's ammo cook-off (AmmoThing.TryDetonate) runs GenExplosionCE with the projectile's explosionRadius and
    /// damage directly, never through ProjectileCE_HeavyBlast. For these warheads that would be the full-size CE
    /// explosion that hung the game, so a burning or shot missile only blows up locally (fxRadius). The XML
    /// explosionRadius stays because CE also uses it for the targeting ring.
    /// </summary>
    [HarmonyPatch(typeof(AmmoThing), "TryDetonate")]
    public static class Patch_HeavyBlastCookOff
    {
        public static bool Prefix(AmmoThing __instance, ref bool __result)
        {
            AmmoDef ammo = __instance.def as AmmoDef;
            ThingDef projectile = ammo != null ? ammo.detonateProjectile : null;
            HeavyBlastExtension ext = projectile != null ? projectile.GetModExtension<HeavyBlastExtension>() : null;
            if (ext == null || !__instance.Spawned)
            {
                return true;
            }
            // same idea as CE: the more damaged the stack, the likelier it detonates; CE destroys the stack itself
            // afterwards (we are inside PreApplyDamage), and the stack is ignored by its own blast (no recursion)
            __result = Rand.Chance(Mathf.Clamp01(1f - (float)__instance.HitPoints / __instance.MaxHitPoints));
            if (__result)
            {
                GenExplosion.DoExplosion(__instance.Position, __instance.Map, ext.fxRadius, DamageDefOf.Bomb, null, 60, -1f,
                    SoundDef.Named("Explosion_GiantBomb"), ignoredThings: new List<Thing> { __instance });
            }
            return false;
        }
    }
}
