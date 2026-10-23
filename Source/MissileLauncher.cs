using System.Collections.Generic;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// Turret top textures indexed by loaded rounds: topTexPaths[0] = empty, [1] = one loaded, ...
    /// Counts above the list use the last entry.
    /// </summary>
    public class LoadedTopsExtension : DefModExtension
    {
        public List<string> topTexPaths;

        private Material[] mats;

        public Material MatFor(int loaded)
        {
            // Built lazily: drawing runs on the main thread, where MaterialPool is safe to use.
            if (mats == null)
            {
                mats = new Material[topTexPaths.Count];
                for (int i = 0; i < topTexPaths.Count; i++)
                {
                    mats[i] = MaterialPool.MatFrom(topTexPaths[i]);
                }
            }
            return mats[Mathf.Clamp(loaded, 0, mats.Length - 1)];
        }
    }

    /// <summary>
    /// Marker: the launcher never picks targets on its own; it only fires on player orders
    /// (e.g. CE's "attack world tile").
    /// </summary>
    public class ManualFireOnlyExtension : DefModExtension
    {
    }

    /// <summary>
    /// CE turret whose top shows how many missiles are loaded. TurretTop.DrawTurret draws
    /// Building_Turret.TurretTopMaterial, so overriding it is enough.
    /// </summary>
    public class Building_MissileLauncherCE : Building_TurretGunCE
    {
        public override LocalTargetInfo TryFindNewTarget()
        {
            if (def.HasModExtension<ManualFireOnlyExtension>())
            {
                return LocalTargetInfo.Invalid;
            }
            return base.TryFindNewTarget();
        }

        public override Material TurretTopMaterial
        {
            get
            {
                LoadedTopsExtension ext = def.GetModExtension<LoadedTopsExtension>();
                CompAmmoUser ammo = CompAmmo;
                if (ext == null || ext.topTexPaths.NullOrEmpty() || ammo == null)
                {
                    return base.TurretTopMaterial;
                }
                return ext.MatFor(ammo.CurMagCount);
            }
        }
    }
}
