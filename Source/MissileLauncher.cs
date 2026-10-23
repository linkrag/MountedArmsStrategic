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
    /// "Unload" button for CE turrets. CE only offers unloading in the reload menu of a manned turret
    /// while it is manned; launchers and auto turrets need it too, to empty or swap warheads.
    /// </summary>
    public static class AmmoUnload
    {
        public static Command_Action UnloadCommand(Building_TurretGunCE turret)
        {
            CompAmmoUser ammo = turret.CompAmmo;
            if (ammo == null || !ammo.UseAmmo || !ammo.HasMagazine || turret.Faction != Faction.OfPlayer)
            {
                return null;
            }
            Command_Action cmd = new Command_Action();
            cmd.defaultLabel = "CE_UnloadLabel".Translate();
            cmd.defaultDesc = "MAS_UnloadDesc".Translate();
            cmd.icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true);
            cmd.action = delegate { ammo.TryUnload(true); };
            if (ammo.CurMagCount <= 0)
            {
                cmd.Disable("MAS_UnloadEmpty".Translate());
            }
            return cmd;
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
    /// Fixed launchers (VLS cells, silos): the turret top is pinned to this angle instead of turning
    /// toward the target. The shot direction is computed by the verb, not by the top.
    /// </summary>
    public class FixedTopExtension : DefModExtension
    {
        public float angle = 0f;
    }

    /// <summary>CE turret whose top shows how many missiles are loaded.</summary>
    public class Building_MissileLauncherCE : Building_TurretGunCE
    {
        public override void Tick()
        {
            base.Tick();
            FixedTopExtension fixedTop = def.GetModExtension<FixedTopExtension>();
            if (fixedTop != null && top != null)
            {
                top.CurRotation = fixedTop.angle;
            }
        }

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

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            Command_Action unload = AmmoUnload.UnloadCommand(this);
            if (unload != null)
            {
                yield return unload;
            }
        }
    }

    /// <summary>CE CIWS turret (projectile/skyfaller interception) whose top shows loaded interceptors.</summary>
    public class Building_MissileInterceptorCE : Building_CIWS_CE
    {
        public override Material TurretTopMaterial
        {
            get { return LoadedTopsExtension.TopFor(this, base.TurretTopMaterial); }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            Command_Action unload = AmmoUnload.UnloadCommand(this);
            if (unload != null)
            {
                yield return unload;
            }
        }
    }
}
