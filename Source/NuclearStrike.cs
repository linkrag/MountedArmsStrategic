using System.Reflection;
using CombatExtended;
using RimWorld;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// Which thing to spawn at the impact cell and with what yield. Defaults match Dubs Rimatomics:
    /// its ICBM drops an "ICBMStrike" (Rimatomics.NuclearStrike) with Yield set, and that thing does
    /// the flash, blast wave and NuclearFallout condition by itself (read from Rimatomics.dll IL).
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
                }
            }
            base.Impact(hitThing);
        }
    }
}
