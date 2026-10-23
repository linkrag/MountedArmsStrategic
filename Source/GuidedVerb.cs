using CombatExtended;
using RimWorld.Planet;
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
    /// </summary>
    public class Verb_ShootGuidedCE : Verb_ShootMortarCE
    {
        // AimingAccuracy 1.5 makes ShiftVecReport.accuracyFactor 0: no range-estimate error (distShift)
        private const float GuidedAiming = 1.5f;

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
