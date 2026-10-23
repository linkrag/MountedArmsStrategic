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

        /// <summary>
        /// Top material for a CE turret: by loaded count when the def has this extension, else the fallback.
        /// TurretTop.DrawTurret draws Building_Turret.TurretTopMaterial, so turrets override that property.
        /// </summary>
        public static Material TopFor(Building_TurretGunCE turret, Material fallback)
        {
            LoadedTopsExtension ext = turret.def.GetModExtension<LoadedTopsExtension>();
            CompAmmoUser ammo = turret.CompAmmo;
            if (ext == null || ext.topTexPaths.NullOrEmpty() || ammo == null)
            {
                return fallback;
            }
            return ext.MatFor(ammo.CurMagCount);
        }
    }

    /// <summary>
    /// Marker: the launcher never picks targets on its own; it only fires on player orders
    /// (e.g. CE's "attack world tile").
    /// </summary>
    public class ManualFireOnlyExtension : DefModExtension
    {
    }

    /// <summary>CE turret whose top shows how many missiles are loaded.</summary>
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
            get { return LoadedTopsExtension.TopFor(this, base.TurretTopMaterial); }
        }
    }

    /// <summary>CE CIWS turret (projectile/skyfaller interception) whose top shows loaded interceptors.</summary>
    public class Building_MissileInterceptorCE : Building_CIWS_CE
    {
        public override Material TurretTopMaterial
        {
            get { return LoadedTopsExtension.TopFor(this, base.TurretTopMaterial); }
        }
    }
}
