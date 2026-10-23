using CombatExtended;
using RimWorld;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// Submunitions released by a cluster warhead. CE has no cluster projectile (ProjectileCE_Bursting is a
    /// time fuse for direct fire), so the warhead scatters explosions itself on impact.
    /// </summary>
    public class ClusterExtension : DefModExtension
    {
        public int count = 12;
        public float spreadRadius = 6f;
        public float subExplosionRadius = 1.9f;
        public DamageDef damageDef;
        public int damageAmount = 80;
        public SoundDef explosionSound;
    }

    /// <summary>Explosive CE projectile that scatters ClusterExtension.count bomblets around the impact cell.</summary>
    public class ProjectileCE_Cluster : ProjectileCE_Explosive
    {
        public override void Impact(Thing hitThing)
        {
            Map map = Map;
            IntVec3 center = Position;
            ClusterExtension ext = def.GetModExtension<ClusterExtension>();
            if (map != null && ext != null && center.InBounds(map))
            {
                DamageDef damage = ext.damageDef ?? DamageDefOf.Bomb;
                int cells = GenRadial.NumCellsInRadius(ext.spreadRadius);
                for (int i = 0; i < ext.count; i++)
                {
                    IntVec3 cell = center + GenRadial.RadialPattern[Rand.Range(0, cells)];
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }
                    GenExplosion.DoExplosion(cell, map, ext.subExplosionRadius, damage, launcher, ext.damageAmount, -1f, ext.explosionSound);
                }
            }
            base.Impact(hitThing);
        }
    }
}
