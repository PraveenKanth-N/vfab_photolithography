using UnityEngine;
using System;
using SemiconductorDigitalTwin.Wafers.Core;

namespace SemiconductorDigitalTwin.Physics
{
    /// <summary>
    /// Module 6: Consequence Rule Engine.
    /// Evaluates user process inputs against the physical equations in PhysicsSimulationEngine
    /// and Table II operational parameter boundaries. If boundaries are breached, mutates
    /// the authoritative WaferDigitalTwin state by registering physical defect entries.
    /// </summary>
    public class ConsequenceRuleEngine : MonoBehaviour
    {
        public static ConsequenceRuleEngine Instance { get; private set; }

        [Header("Target Digital Twin")]
        [SerializeField] private WaferDigitalTwin _waferTwin;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (_waferTwin == null)
            {
                _waferTwin = FindAnyObjectByType<WaferDigitalTwin>();
            }
        }

        // =========================================================================
        // PROCESS STAGE 1: Photoresist Coating (Eq. 1 Validation)
        // =========================================================================

        /// <summary>
        /// Evaluates spin coating recipe and updates wafer layer thickness & defects.
        /// </summary>
        /// <param name="spinSpeedRPM">User-selected spin speed in RPM</param>
        /// <param name="spinDurationSec">Cycle duration in seconds</param>
        public void ProcessSpinCoating(float spinSpeedRPM, float spinDurationSec = 30f)
        {
            if (_waferTwin == null) return;

            // 1. Calculate physical thickness from Emslie-Bonner-Peck Equation (1)
            float calculatedThicknessUm = PhysicsSimulationEngine.CalculateSpinCoatingThickness(spinSpeedRPM, 28.5f, spinDurationSec);
            float thicknessVariancePct = PhysicsSimulationEngine.CalculateSpinUniformityVariance(spinSpeedRPM);

            // Convert um to mm for WaferDigitalTwin layer representation (e.g., 1.2 um = 0.0012 mm)
            float layerThicknessMm = calculatedThicknessUm / 1000f;

            // 2. Add or update photoresist layer on the authoritative Wafer Digital Twin
            _waferTwin.AddLayer(1, "SPR-220 Photoresist", layerThicknessMm);
            _waferTwin.UpdateSurfaceState("Photoresist Coated");

            // 3. Evaluate Table II Operational Boundary Deviations
            if (spinSpeedRPM < 1800f)
            {
                // Under-speed failure: severe radial thickness non-uniformity and striations
                float severity = Mathf.Clamp01(0.5f + (1800f - spinSpeedRPM) / 1000f);
                _waferTwin.RegisterDefect(
                    defectType: "RadialThicknessNonUniformity_Striations",
                    position: (35.0f, 15.0f, _waferTwin.Thickness),
                    severity: severity
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Radial Striations (Spin Speed {spinSpeedRPM} RPM < 1800 RPM, Variance {thicknessVariancePct:F1}%)");
            }
            else if (spinSpeedRPM > 5500f)
            {
                // Over-speed failure: overly thin film prone to pinhole breakthrough
                float severity = Mathf.Clamp01(0.4f + (spinSpeedRPM - 5500f) / 1500f);
                _waferTwin.RegisterDefect(
                    defectType: "PinholeBreakthroughRisk_ThinFilm",
                    position: (0.0f, 0.0f, _waferTwin.Thickness),
                    severity: severity
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Pinhole Risk (Spin Speed {spinSpeedRPM} RPM > 5500 RPM)");
            }
            else
            {
                Debug.Log($"[ConsequenceEngine] Spin coating successful: Uniform film thickness {calculatedThicknessUm:F2} um (RPM: {spinSpeedRPM})");
            }
        }

        // =========================================================================
        // PROCESS STAGE 2: Soft Bake Thermal Treatment
        // =========================================================================

        /// <summary>
        /// Evaluates soft bake temperature and solvent evaporation dynamics.
        /// </summary>
        /// <param name="temperatureCelsius">Hotplate temperature in °C</param>
        /// <param name="durationSeconds">Bake duration in seconds</param>
        public void ProcessSoftBake(float temperatureCelsius, float durationSeconds = 60f)
        {
            if (_waferTwin == null) return;

            if (temperatureCelsius < 85f)
            {
                // Under-baking: excess residual solvent leads to resist bubbling upon UV exposure
                _waferTwin.RegisterDefect(
                    defectType: "ExcessResidualSolvent_Bubbling",
                    position: (-20.0f, 40.0f, _waferTwin.Thickness),
                    severity: 0.72f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Solvent Bubbling (Temp {temperatureCelsius}°C < 85°C)");
            }
            else if (temperatureCelsius > 115f)
            {
                // Over-baking: premature thermal cross-linking degrades optical photosensitivity
                _waferTwin.RegisterDefect(
                    defectType: "ThermalCrosslinking_PhotosensitivityLoss",
                    position: (0.0f, 0.0f, _waferTwin.Thickness),
                    severity: 0.65f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Thermal Crosslinking (Temp {temperatureCelsius}°C > 115°C)");
            }
            else
            {
                _waferTwin.UpdateSurfaceState("Soft Baked (Solvent Stabilized)");
                Debug.Log($"[ConsequenceEngine] Soft bake optimal: {temperatureCelsius}°C for {durationSeconds}s");
            }
        }

        // =========================================================================
        // PROCESS STAGE 3: UV Exposure and Alignment (Eqs. 2, 3, 4 Validation)
        // =========================================================================

        /// <summary>
        /// Evaluates UV exposure dose and spatial mask overlay registration.
        /// </summary>
        /// <param name="doseMilliJoules">Delivered UV exposure dose in mJ/cm^2</param>
        /// <param name="alignmentOffsetXUm">Mask alignment error in micrometers</param>
        public void ProcessUVExposure(float doseMilliJoules, float alignmentOffsetXUm = 0.0f)
        {
            if (_waferTwin == null) return;

            // Compute photoactive inhibitor decomposition fraction via Dill Kinetics (Eqs. 3 & 4)
            float remainingInhibitorM = PhysicsSimulationEngine.CalculateDillRemainingInhibitor(doseMilliJoules);

            // 1. Exposure Dose Envelope Checks
            if (doseMilliJoules < 120f)
            {
                // Under-exposure: incomplete photoacid decomposition, unreacted resist residue
                _waferTwin.RegisterDefect(
                    defectType: "IncompletePhotoacidDecomposition_Residue",
                    position: (10.0f, -25.0f, _waferTwin.Thickness),
                    severity: 0.85f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Resist Residue (Dose {doseMilliJoules} mJ/cm^2 < 120 mJ/cm^2, Remaining Inhibitor M={remainingInhibitorM:F2})");
            }
            else if (doseMilliJoules > 250f)
            {
                // Over-exposure: lateral diffraction broadening shrinks critical dimension (Rayleigh Eq. 2)
                _waferTwin.RegisterDefect(
                    defectType: "LateralDiffraction_CDShrinkage",
                    position: (15.0f, 15.0f, _waferTwin.Thickness),
                    severity: 0.75f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: CD Shrinkage (Dose {doseMilliJoules} mJ/cm^2 > 250 mJ/cm^2)");
            }

            // 2. Spatial Mask Alignment Offset Check
            if (Mathf.Abs(alignmentOffsetXUm) > 0.15f)
            {
                // Overlay misregistration: inter-layer contact vias will not align
                _waferTwin.RegisterDefect(
                    defectType: "InterlayerOverlayMisregistration",
                    position: (0.0f, 0.0f, _waferTwin.Thickness),
                    severity: 0.95f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Overlay Misregistration (|Offset| = {alignmentOffsetXUm:F2} um > 0.15 um)");
            }

            _waferTwin.UpdateSurfaceState("UV Patterned & Developed");
        }

        // =========================================================================
        // PROCESS STAGE 4: Reactive Ion Etching (RIE) (Eqs. 5 & 6 Validation)
        // =========================================================================

        /// <summary>
        /// Evaluates plasma etch recipe using vertical rate (Eq. 5) and anisotropy (Eq. 6).
        /// </summary>
        /// <param name="rfPowerWatts">RF generator power (W)</param>
        /// <param name="chamberPressureMtorr">Chamber pressure (mTorr)</param>
        /// <param name="sf6FlowSccm">SF6 reactive gas flow rate (sccm)</param>
        /// <param name="etchDurationSec">Etch process time in seconds</param>
        public void ProcessReactiveIonEtching(float rfPowerWatts, float chamberPressureMtorr, float sf6FlowSccm, float etchDurationSec)
        {
            if (_waferTwin == null) return;

            // 1. Calculate physical vertical etch rate from Equation (5)
            float verticalRateNmPerSec = PhysicsSimulationEngine.CalculateRIEVerticalEtchRate(rfPowerWatts, chamberPressureMtorr, sf6FlowSccm);
            float totalEtchedDepthNm = verticalRateNmPerSec * etchDurationSec;

            // 2. Calculate directional anisotropy factor from Equation (6)
            float anisotropyA = PhysicsSimulationEngine.CalculateEtchAnisotropy(rfPowerWatts, chamberPressureMtorr);

            // 3. Evaluate Table II Operational Deviations
            if (chamberPressureMtorr > 120f)
            {
                // High pressure failure: mean free path collapse leads to isotropic undercut profile
                _waferTwin.RegisterDefect(
                    defectType: "IsotropicLateralUndercut_PitchCollapse",
                    position: (-15.0f, -15.0f, _waferTwin.Thickness),
                    severity: 0.88f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Isotropic Undercut (Chamber Pressure {chamberPressureMtorr} mTorr > 120 mTorr, Anisotropy A={anisotropyA:F2})");
            }

            if (rfPowerWatts > 300f)
            {
                // High RF power failure: high energetic ion bombardment causes gate oxide punch-through & microtrenching
                _waferTwin.RegisterDefect(
                    defectType: "GateOxidePunchthrough_Microtrenching",
                    position: (20.0f, 30.0f, _waferTwin.Thickness),
                    severity: 0.92f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Oxide Punchthrough (RF Power {rfPowerWatts} W > 300 W)");
            }

            // Target silicon oxide layer removal is 350 nm
            const float targetEndpointDepthNm = 350f;
            if (totalEtchedDepthNm < targetEndpointDepthNm * 0.85f)
            {
                // Under-etching failure: residual conductive film bridges adjacent interconnects
                _waferTwin.RegisterDefect(
                    defectType: "UnderEtch_ResidualConductiveFilmShorts",
                    position: (0.0f, 0.0f, _waferTwin.Thickness),
                    severity: 0.80f
                );
                Debug.LogWarning($"[ConsequenceEngine] Defect Injected: Residual Film Shorts (Etched {totalEtchedDepthNm:F1} nm < Target {targetEndpointDepthNm} nm)");
            }
            else
            {
                _waferTwin.UpdateSurfaceState("Etched Anisotropic Trenches Complete");
                Debug.Log($"[ConsequenceEngine] RIE Etch completed successfully: Etched {totalEtchedDepthNm:F1} nm (Anisotropy: {anisotropyA:F2})");
            }
        }
    }
}
