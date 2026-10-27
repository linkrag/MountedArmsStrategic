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

    [DefOf]
    public static class MASDamageDefOf
    {
        public static DamageDef MAS_BlastWave;   // every factor 1: TakeDamage removes exactly the zone's hit points

        static MASDamageDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MASDamageDefOf));
        }
    }

    /// <summary>
    /// Large warhead blast with real overpressure zones (Kingery-Bulmash cube-root scaling; damage criteria from
    /// Glasstone &amp; Dolan / TM 5-1300 / OTA / FM 8-9; research in Docs/BLAST.md). The zone radii are fixed
    /// fractions of R because, under cube-root scaling, the ratios between overpressure contours do not depend on
    /// the charge mass; the per-zone damage depends only on overpressure, so it is the same for every warhead.
    ///   core      0 - 0.25R   >= ~22 psi   vaporized (everything but rock removed at once; crater)
    ///   severe A  -  0.37R    22 - 10 psi  buildings -900 hp (reinforced concrete demolished)
    ///   severe B  -  0.50R    10 - 5.8 psi buildings -600 hp (most buildings collapse)
    ///   heavy     -  0.75R    5.8 - 3 psi  buildings -250 hp (houses collapse)
    ///   moderate  -  1.0R     3 - 2 psi    buildings -100 hp, doors -75% (doors/windows/roofs blown out)
    /// Outside the core everything takes real damage (TakeDamage with MAS_BlastWave, building damage +-20%).
    /// Thermobaric: same core, rings out to 0.55R / 0.82R, x1.5 damage in cells that were roofed (confinement,
    /// a game choice: the sources give no number), fire 100/60/60/25/5%.
    /// Performance (the first version hung the game): every non-thick roof in R is stripped ahead of the front
    /// (game rule, so nothing caves in cascade-style), natural rock is left alone, ammo and explosives can only go
    /// off in the outer 3-8% of R (inside it ammo is consumed and explosives are wrecked without exploding: no
    /// cook-off chain), each thing is processed once, fires are capped (picked at random over the whole area).
    /// Walls/columns/doors holding up an overhead mountain fall at the end of the wave: a single vanilla cave-in.
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
            HeavyBlastWaves waves = map.GetComponent<HeavyBlastWaves>();
            if (waves == null)
            {
                return;
            }
            float radius = Mathf.Max(ext.radius, 1f);
            // flash + sound at once (damage 1); the damage follows the shock front over the next ticks
            SoundDef sound = projectileDef.projectile != null && projectileDef.projectile.soundExplode != null
                ? projectileDef.projectile.soundExplode : SoundDef.Named("Explosion_GiantBomb");
            GenExplosion.DoExplosion(center, map, ext.fxRadius, DamageDefOf.Bomb, instigator, 1, -1f, sound);
            FleckMaker.Static(center.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, radius * 4f);
            if (map == Find.CurrentMap)
            {
                Find.CameraDriver.shaker.DoShake(Mathf.Clamp(radius / 6f, 1f, 4f));
            }
            waves.Add(new BlastWave(map, center, radius, ext, instigator, projectileDef));
        }

        /// <summary>
        /// One blast spread over ticks: the front grows from the center to R in ~6-25 ticks, and each tick handles the
        /// cells it passed (at most MaxThingsPerTick things and MaxCellsPerTick cells, so a dense base or a nuclear
        /// R slows the front instead of the frame).
        /// Roofs are stripped RoofReach cells ahead of the front, so no wall is ever destroyed under a roof that
        /// is still there. Not saved: a save made mid-wave loses the rest of it (it lasts well under a second).
        /// </summary>
        public class BlastWave
        {
            private const int MaxThingsPerTick = 150;
            private const int MaxCellsPerTick = 600;   // empty cells too (a nuclear R 250 covers ~62k cells)
            private const float RoofReach = 7f;   // vanilla roof support distance is 6.9

            private readonly Map map;
            private readonly IntVec3 center;
            private readonly float radius;
            private readonly float speed;
            private readonly HeavyBlastExtension ext;
            private readonly Thing instigator;
            private readonly ThingDef projectileDef;
            private readonly DamageDef pawnDamage;
            private readonly FleckDef cellFleck;
            private readonly Color colorCenter;
            private readonly Color colorEdge;
            private readonly List<IntVec3> cells = new List<IntVec3>();
            private readonly List<float> dist = new List<float>();
            private readonly List<bool> confined = new List<bool>();
            private readonly HashSet<Thing> processed = new HashSet<Thing>();
            private readonly List<IntVec3> fireCells = new List<IntVec3>();
            private readonly List<Pawn> pawnsAtStart = new List<Pawn>();
            private readonly List<int> pawnZones = new List<int>();
            private readonly List<Thing> doomedRoofHolders = new List<Thing>();
            private float front;
            private int next;
            private int roofNext;
            private int coreCells;
            public bool Finished;

            public BlastWave(Map map, IntVec3 center, float radius, HeavyBlastExtension ext, Thing instigator, ThingDef projectileDef)
            {
                this.map = map;
                this.center = center;
                this.radius = radius;
                this.ext = ext;
                this.instigator = instigator;
                this.projectileDef = projectileDef;
                pawnDamage = ext.damageDef ?? DamageDefOf.Bomb;
                speed = radius / Mathf.Clamp(Mathf.Round(radius), 6f, 25f);
                DamageDef fx = pawnDamage.explosionCellFleck != null ? pawnDamage : DamageDefOf.Bomb;
                cellFleck = fx.explosionCellFleck;
                colorCenter = fx.explosionColorCenter;
                colorEdge = fx.explosionColorEdge;
                // map cells within R sorted by distance (GenRadial's pattern stops near r120; nuclear R goes to 250)
                int r = Mathf.CeilToInt(radius);
                List<IntVec3> inRadius = new List<IntVec3>();
                for (int x = Mathf.Max(0, center.x - r); x <= Mathf.Min(map.Size.x - 1, center.x + r); x++)
                {
                    for (int z = Mathf.Max(0, center.z - r); z <= Mathf.Min(map.Size.z - 1, center.z + r); z++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, z);
                        if ((cell - center).LengthHorizontalSquared <= radius * radius)
                        {
                            inRadius.Add(cell);
                        }
                    }
                }
                inRadius.SortBy(c => (c - center).LengthHorizontalSquared);
                foreach (IntVec3 cell in inRadius)
                {
                    cells.Add(cell);
                    dist.Add(cell.DistanceTo(center));
                    confined.Add(map.roofGrid.Roofed(cell));   // snapshot before stripping (thermobaric confinement)
                }
                // pawns inside R at impact: one that walks into a cell the front already passed is still hit in Finish
                List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
                for (int i = 0; i < pawns.Count; i++)
                {
                    float d = pawns[i].Position.DistanceTo(center);
                    if (d <= radius)
                    {
                        pawnsAtStart.Add(pawns[i]);
                        pawnZones.Add(ZoneOf(d / radius, ext.thermobaric));
                    }
                }
            }

            public void Tick()
            {
                if (Finished)
                {
                    return;
                }
                if (!Find.Maps.Contains(map))
                {
                    Finished = true;
                    return;
                }
                front += speed;
                // game rule: every non-thick roof in R goes, ahead of the front (prevents cascade cave-ins)
                while (roofNext < cells.Count && dist[roofNext] <= front + RoofReach)
                {
                    RoofDef roof = map.roofGrid.RoofAt(cells[roofNext]);
                    if (roof != null && !roof.isThickRoof)
                    {
                        map.roofGrid.SetRoof(cells[roofNext], null);
                    }
                    roofNext++;
                }
                int budget = MaxThingsPerTick;
                int cellBudget = MaxCellsPerTick;
                while (next < cells.Count && dist[next] <= front && budget > 0 && cellBudget > 0)
                {
                    budget -= HitCell(next);
                    cellBudget--;
                    next++;
                }
                if (next >= cells.Count)
                {
                    Finish();
                }
            }

            private int HitCell(int c)
            {
                IntVec3 cell = cells[c];
                float fraction = dist[c] / radius;
                int zone = ZoneOf(fraction, ext.thermobaric);
                if (zone == 0)
                {
                    coreCells = c + 1;   // radial order: the core is a prefix of the list
                }
                float mult = ext.thermobaric && zone > 0 && confined[c] ? 1.5f : 1f;
                int handled = DamageCell(cell, zone, fraction, mult);

                float fire = ext.thermobaric ? FireThermobaric[zone] : FireHE[zone];
                if (fire > 0f && Rand.Chance(fire))
                {
                    fireCells.Add(cell);
                }
                if (cell.ShouldSpawnMotesAt(map))
                {
                    Vector3 pos = cell.ToVector3Shifted();
                    if (cellFleck != null)
                    {
                        FleckCreationData data = FleckMaker.GetDataStatic(pos, map, cellFleck, 1f);
                        data.instanceColor = Color.Lerp(colorCenter, colorEdge, fraction);
                        map.flecks.CreateFleck(data);
                    }
                    if (Rand.Chance(zone <= 2 ? 0.06f : 0.02f))
                    {
                        FleckMaker.ThrowSmoke(pos, map, Rand.Range(1.5f, 3f));
                    }
                    if (Rand.Chance(0.08f))
                    {
                        FleckMaker.ThrowDustPuff(cell, map, Rand.Range(1f, 2f));
                    }
                    if (ext.thermobaric && zone <= 2 && Rand.Chance(0.2f))
                    {
                        FleckMaker.ThrowFireGlow(pos, map, Rand.Range(1f, 2f));
                    }
                }
                return handled;
            }

            private void Finish()
            {
                Finished = true;
                // the mountain caves in once, on the next tick (vanilla roof collapse), after everything under it
                // got its zone damage and its ammo/explosives were already dealt with
                for (int i = 0; i < doomedRoofHolders.Count; i++)
                {
                    Thing t = doomedRoofHolders[i];
                    if (!t.Destroyed && t.Spawned && t.Map == map)
                    {
                        t.Destroy(DestroyMode.KillFinalize);
                    }
                }
                // whatever appeared in the core while the front passed (corpses, dropped gear, ejected contents,
                // the leavings of the roof holders above)
                for (int c = 0; c < coreCells; c++)
                {
                    DamageCell(cells[c], 0, dist[c] / radius, 1f);
                }
                for (int i = 0; i < pawnsAtStart.Count; i++)
                {
                    Pawn p = pawnsAtStart[i];
                    if (p.Spawned && p.Map == map && !p.Dead && processed.Add(p))
                    {
                        DamagePawn(p, pawnZones[i], 1f, pawnDamage, instigator, projectileDef);
                    }
                }
                fireCells.Shuffle();   // the cap must not be used up by the center alone
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
            }

            // returns how many things were damaged (the per-tick budget; rock and indestructible things are free,
            // or a mountain map spent the budget on rock and the front crawled for seconds)
            private int DamageCell(IntVec3 cell, int zone, float fraction, float mult)
            {
                List<Thing> things = cell.GetThingList(map);
                if (things.Count == 0)
                {
                    return 0;
                }
                things = new List<Thing>(things);
                int handled = 0;
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
                        handled++;
                        DamagePawn(pawn, zone, mult, pawnDamage, instigator, projectileDef);
                        continue;
                    }
                    if (!t.def.useHitPoints || !t.def.destroyable || IsRock(t.def))
                    {
                        continue;
                    }
                    handled++;
                    int loss = zone == 0 ? t.HitPoints : ZoneLoss(t, zone, mult);
                    if (t.def.holdsRoof && t.def.category == ThingCategory.Building && NearThickRoof(t.Position, map))
                    {
                        // walls/columns/doors holding up an overhead mountain: a lethal hit waits for Finish (one cave-in)
                        if (loss >= t.HitPoints)
                        {
                            doomedRoofHolders.Add(t);
                        }
                        else
                        {
                            Hit(t, loss);
                        }
                        continue;
                    }
                    // ammo and explosives only get the chance to go off in the outer 3-8% of R
                    if ((t.def is AmmoDef || ExplodesOnDeath(t)) && fraction < 1f - Rand.Range(0.03f, 0.08f))
                    {
                        Defuse(t, loss);
                        continue;
                    }
                    if (zone == 0)
                    {
                        t.Destroy(DestroyMode.KillFinalize);   // vaporized
                        continue;
                    }
                    Hit(t, loss);
                }
                return handled;
            }

            private void Hit(Thing t, int amount)
            {
                float angle = (t.TrueCenter() - center.ToVector3Shifted()).AngleFlat();
                t.TakeDamage(new DamageInfo(MASDamageDefOf.MAS_BlastWave, Mathf.Max(1, amount), 0f, angle, null, null,
                    projectileDef, spawnFilth: false));
            }
        }

        // hit points a thing outside the core loses in this zone (its current hit points = destroyed)
        private static int ZoneLoss(Thing t, int zone, float mult)
        {
            switch (t.def.category)
            {
                case ThingCategory.Building:
                    int dmg = Mathf.RoundToInt(BuildingDamage[zone] * mult * Rand.Range(0.8f, 1.2f));
                    if (zone == 4 && t is Building_Door)
                    {
                        dmg = Mathf.Max(dmg, Mathf.RoundToInt(t.MaxHitPoints * 0.75f));   // doors blown in at 2-3 psi
                    }
                    return dmg;
                case ThingCategory.Plant:
                    return Rand.Chance(PlantDestroyChance[zone]) ? t.HitPoints : Mathf.RoundToInt(t.MaxHitPoints * 0.7f);
                default:
                    return Rand.Chance(ItemDestroyChance[zone]) ? t.HitPoints
                        : Mathf.RoundToInt(t.MaxHitPoints * Rand.Range(ItemLossMin[zone], ItemLossMax[zone]) * mult);
            }
        }

        // ammo/explosive away from the blast's edge: no cook-off or secondary explosion chain
        private static void Defuse(Thing t, int loss)
        {
            if (t.def.category == ThingCategory.Plant)
            {
                LoseHitPoints(t, loss);   // boomshroom: Destroy(KillFinalize) never calls Kill, which explodes
                return;
            }
            if (t.def.category != ThingCategory.Building)
            {
                t.Destroy(DestroyMode.Vanish);   // stored ammo, chemfuel, minified explosives: consumed by the blast
                return;
            }
            if (loss < t.HitPoints)
            {
                LoseHitPoints(t, loss);   // directly: TakeDamage would light the wick
                return;
            }
            CompExplosive comp = t.TryGetComp<CompExplosive>();
            if (comp != null)
            {
                comp.destroyedThroughDetonation = true;   // wrecked (leavings) without the explosion
            }
            t.Destroy(DestroyMode.KillFinalize);
        }

        // natural, smoothed and collapsed rock and ore: they hold the mountain roof (removing them caves it in)
        private static bool IsRock(ThingDef def)
        {
            return def.mineable || (def.building != null && (def.building.isNaturalRock || def.building.isResourceRock));
        }

        private static bool NearThickRoof(IntVec3 pos, Map map)
        {
            foreach (IntVec3 c in GenRadial.RadialCellsAround(pos, 6.9f, true))   // vanilla roof support distance
            {
                if (c.InBounds(map))
                {
                    RoofDef roof = map.roofGrid.RoofAt(c);
                    if (roof != null && roof.isThickRoof)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool ExplodesOnDeath(Thing t)
        {
            MinifiedThing minified = t as MinifiedThing;
            if (minified != null && minified.InnerThing != null)
            {
                t = minified.InnerThing;   // destroying the crate destroys what is inside it the same way
            }
            if (t is Plant_Boomshroom)
            {
                return true;   // Odyssey: its Kill sets off a flame explosion
            }
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
        // never over rock or anything else that survived the blast (it would be deleted silently), and never under
        // an overhead mountain (the cave-in after the wave would drop rocks on it)
        private static bool CanPlaceCrater(ThingDef crater, IntVec3 center, Map map)
        {
            CellRect rect = GenAdj.OccupiedRect(center, Rot4.North, crater.size);
            if (!rect.InBounds(map))
            {
                return false;
            }
            foreach (IntVec3 c in rect)
            {
                RoofDef roof = map.roofGrid.RoofAt(c);
                if (roof != null && roof.isThickRoof)
                {
                    return false;
                }
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

    /// <summary>Ticks the heavy blast waves in progress on this map (created for every map automatically).</summary>
    public class HeavyBlastWaves : MapComponent
    {
        private readonly List<ProjectileCE_HeavyBlast.BlastWave> waves = new List<ProjectileCE_HeavyBlast.BlastWave>();

        public HeavyBlastWaves(Map map) : base(map)
        {
        }

        public void Add(ProjectileCE_HeavyBlast.BlastWave wave)
        {
            waves.Add(wave);
        }

        public override void MapComponentTick()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                try
                {
                    waves[i].Tick();
                }
                catch (System.Exception e)
                {
                    waves[i].Finished = true;   // one error, not one per tick
                    Log.Error("[MountedArmsStrategic] heavy blast wave stopped: " + e);
                }
            }
            waves.RemoveAll(w => w.Finished);
        }
    }
}
