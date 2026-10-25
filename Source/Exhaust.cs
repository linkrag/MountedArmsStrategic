using RimWorld;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// Rocket exhaust at the nozzle. Replaces CE's CompProperties_ProjectileFleck on our missiles: CE places the
    /// flame at DrawPos minus the last tick's movement x originOffset, so the offset scales with speed (in the slow
    /// boost phase the flame sat in the middle of the sprite, and on long sprites it never reached the tail).
    /// Here the flame sits nozzleDistance cells behind the drawn position, along the drawn heading.
    /// </summary>
    public class CompProperties_Exhaust : CompProperties
    {
        public FleckDef flameFleck;
        public FleckDef smokeFleck;
        public float nozzleDistance = 1f;     // ~half the sprite length (drawSize.y / 2)
        public float flameScale = 1f;
        public int flamePerTick = 2;
        public float smokeScale = 1.5f;
        public float smokeChance = 0.6f;
        public int flameTicks = -1;           // -1 = whole flight; >0 = booster burn only (e.g. Tomahawk)

        public CompProperties_Exhaust()
        {
            compClass = typeof(CompExhaust);
        }
    }

    public class CompExhaust : ThingComp
    {
        private Vector3 lastPos;
        private Vector3 heading;
        private int age;

        public CompProperties_Exhaust Props
        {
            get { return (CompProperties_Exhaust)props; }
        }

        public override void CompTick()
        {
            base.CompTick();
            Map map = parent.Map;
            if (!parent.Spawned || map == null)
            {
                return;
            }
            Vector3 pos = parent.DrawPos;
            age++;
            Vector3 delta = pos - lastPos;
            delta.y = 0f;
            if (age > 1 && delta.sqrMagnitude > 0.000001f)
            {
                heading = delta.normalized;
            }
            lastPos = pos;
            if (heading == Vector3.zero)
            {
                return;
            }
            Vector3 nozzle = pos - heading * Props.nozzleDistance;
            float angle = heading.AngleFlat();
            if (Props.flameFleck != null && (Props.flameTicks < 0 || age <= Props.flameTicks))
            {
                for (int i = 0; i < Props.flamePerTick; i++)
                {
                    FleckCreationData flame = FleckMaker.GetDataStatic(nozzle - heading * Rand.Range(0f, 0.25f), map, Props.flameFleck, Props.flameScale * Rand.Range(0.8f, 1.2f));
                    flame.rotation = angle;
                    map.flecks.CreateFleck(flame);
                }
            }
            if (Props.smokeFleck != null && Rand.Chance(Props.smokeChance))
            {
                FleckCreationData smoke = FleckMaker.GetDataStatic(nozzle - heading * 0.3f, map, Props.smokeFleck, Props.smokeScale * Rand.Range(0.8f, 1.2f));
                smoke.rotation = Rand.Range(0f, 360f);
                map.flecks.CreateFleck(smoke);
            }
        }
    }
}
