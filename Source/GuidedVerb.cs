using CombatExtended;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MountedArmsStrategic
{
    /// <summary>
    /// Verb for guided missiles and rockets (inertial / satellite guidance).
    /// CE's Verb_ShootMortarCE treats a world-map shot like unguided artillery: the global report gets a
    /// fixed lighting shift (1) and smoke density (10), the distance is floored to 44% of max range, and the
    /// range-estimate error scales with that distance, so an ICBM landed ~30+ cells off (or on a random
    /// cell when the shift left the map). Guidance does not care about light, smoke, weather or range
    /// estimation: here only the verb's circularError (the CEP) remains.
    /// On the local map CE also locks the whole burst to the first shot's aim (range estimate, circular error) and
    /// only ShotSpread/recoil vary per shot, so a salvo landed as a tight cluster. Here every missile flies to its
    /// own point, uniform within circularError of the aim point, with no spread or recoil.
    /// </summary>
    public class Verb_ShootGuidedCE : Verb_ShootMortarCE
    {
        // AimingAccuracy 1.5 makes ShiftVecReport.accuracyFactor 0: no range-estimate error (distShift)
        private const float GuidedAiming = 1.5f;
        // CE logs a warning and fires at 45 degrees when the point is beyond the charge's reach
        private const float ReachMargin = 0.98f;

        public override void ShiftTarget(ShiftVecReport report, Vector3 v, out float targetHeight,
            bool calculateMechanicalOnly = false, bool isInstant = false)
        {
            base.ShiftTarget(report, v, out targetHeight, calculateMechanicalOnly, isInstant);
            if (calculateMechanicalOnly || globalTargetInfo.IsValid)
            {
                return;   // world shots: ShiftGlobalTarget already scatters each missile within circularError
            }
            Vector3 source = caster.TrueCenter();
            Vector2 from = new Vector2(source.x, source.z);
            Vector2 aim = new Vector2(v.x, v.z);
            if (!isInstant)
            {
                aim += report.GetRandLeadVec();   // CE's lead on moving pawns (zero for cells and buildings)
            }
            aim += Rand.InsideUnitCircle * VerbPropsCE.circularError;
            Vector2 bracket;
            if (CompCharges != null && CompCharges.GetChargeBracket((currentTarget.Cell - caster.Position).LengthHorizontal,
                ShotHeight, projectilePropsCE.GravityPerWidth, out bracket))
            {
                float reach = bracket.y * ReachMargin;
                if ((aim - from).magnitude > reach)
                {
                    aim = from + (aim - from).normalized * reach;
                }
            }
            Vector3 aim3 = new Vector3(aim.x, targetHeight, aim.y);
            shotAngle = ShotAngle(source.WithY(ShotHeight), aim3);
            shotRotation = ShotRotation(source, aim3);
            distance = (aim - from).magnitude;
        }

        public override ShiftVecReport ShiftVecReportFor(GlobalTargetInfo target)
        {
            ShiftVecReport report = base.ShiftVecReportFor(target);
            if (report != null)
            {
                Guide(report);
                report.aimingAccuracy = GuidedAiming;
            }
            return report;
        }

        public override ShiftVecReport ShiftVecReportFor(LocalTargetInfo target, IntVec3 targetCell)
        {
            ShiftVecReport report = base.ShiftVecReportFor(target, targetCell);
            if (report != null)
            {
                Guide(report);
            }
            return report;
        }

        private static void Guide(ShiftVecReport report)
        {
            report.lightingShift = 0f;
            report.weatherShift = 0f;
            report.smokeDensity = 0f;
        }
    }
}
