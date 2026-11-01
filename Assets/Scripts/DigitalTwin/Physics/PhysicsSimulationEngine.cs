using UnityEngine;
using System;

namespace SemiconductorDigitalTwin.Physics
{
    /// <summary>
    /// Module 5: Deterministic Semiconductor Physics Simulation Engine.
    /// Implements governing numerical physical formulations for photolithography
    /// and reactive ion etching (RIE) to compute real-time wafer state transformations.
    /// References: IEEE Access Manuscript Equations (1) through (6).
    /// </summary>
    public static class PhysicsSimulationEngine
    {
        // =========================================================================
        // EQUATION (1): Emslie-Bonner-Peck Hydrodynamic Spin-Coating Model
        // tpr = [ (3 * eta * rho0^2) / (4 * rho * omega^2 * t_spin) ]^(1/2) ≈ K0 * (mu^alpha / omega^(1/2))
        // =========================================================================
        
        /// <summary>
        /// Calculates final photoresist film thickness (t_pr) in micrometers (um).
        /// </summary>
        /// <param name="omegaRPM">Rotational spin speed in RPM (e.g., 3000 RPM)</param>
        /// <param name="viscosityEta">Dynamic viscosity in cP (centipoise, default 28.5 cP for SPR-220)</param>
        /// <param name="spinDurationSec">Total spin cycle duration in seconds (default 30s)</param>
        /// <returns>Photoresist thickness t_pr in micrometers (um)</returns>
        public static float CalculateSpinCoatingThickness(float omegaRPM, float viscosityEta = 28.5f, float spinDurationSec = 30f)
        {
            if (omegaRPM <= 0f) return 10.0f; // Static puddle fallback

            // Empirical calibration constant K0 for standard novolac resist
            const float K0 = 65.8f;
            const float alpha = 0.42f; // Viscosity scaling exponent

            // Hydrodynamic power-law formulation: tpr = K0 * (mu^alpha) / (omega^0.5)
            float thicknessUm = K0 * Mathf.Pow(viscosityEta, alpha) / Mathf.Sqrt(omegaRPM);
            
            // Spin duration attenuation correction
            float timeFactor = Mathf.Sqrt(30f / Mathf.Max(spinDurationSec, 5f));
            return thicknessUm * timeFactor;
        }

        /// <summary>
        /// Calculates radial thickness non-uniformity across a 300mm wafer.
        /// When spin speed is too low (< 1800 RPM), centrifugal force is insufficient to overcome viscous drag,
        /// causing radial striations and high thickness variance.
        /// </summary>
        /// <param name="omegaRPM">Spin speed in RPM</param>
        /// <returns>Percentage thickness variation (+/- %)</returns>
        public static float CalculateSpinUniformityVariance(float omegaRPM)
        {
            if (omegaRPM >= 1800f && omegaRPM <= 5500f)
            {
                // Optimal process window: standard +/- 1.5% to 3% across wafer
                return Mathf.Lerp(3.0f, 1.5f, (omegaRPM - 1800f) / (5500f - 1800f));
            }
            else if (omegaRPM < 1800f)
            {
                // Sub-critical speed: severe radial striation instability
                return 18.0f + (1800f - omegaRPM) * 0.02f;
            }
            else
            {
                // Excessive speed: edge beading and turbulent solvent evaporation
                return 8.0f + (omegaRPM - 5500f) * 0.01f;
            }
        }

        // =========================================================================
        // EQUATION (2): Rayleigh Diffraction Resolution and Depth of Focus (DOF)
        // CD = k1 * (lambda / NA),   DOF = +/- k2 * (lambda / NA^2)
        // =========================================================================

        /// <summary>
        /// Calculates minimum printable Critical Dimension (CD) in nanometers (nm).
        /// </summary>
        /// <param name="wavelengthNm">Exposure wavelength (e.g., 365 nm for i-line, 248 nm for KrF)</param>
        /// <param name="numericalAperture">Lens Numerical Aperture NA (e.g., 0.65)</param>
        /// <param name="k1Factor">Process capability factor k1 (typically 0.4 to 0.7)</param>
        /// <returns>Minimum critical dimension CD in nanometers</returns>
        public static float CalculateRayleighCriticalDimension(float wavelengthNm = 365f, float numericalAperture = 0.65f, float k1Factor = 0.55f)
        {
            if (numericalAperture <= 0f) return 1000f;
            return k1Factor * (wavelengthNm / numericalAperture);
        }

        /// <summary>
        /// Calculates usable Depth of Focus (DOF) in micrometers (um).
        /// </summary>
        /// <param name="wavelengthNm">Exposure wavelength in nm</param>
        /// <param name="numericalAperture">Numerical Aperture NA</param>
        /// <param name="k2Factor">Empirical process factor k2 (typically 0.5 to 0.8)</param>
        /// <returns>Depth of focus in micrometers (+/- um)</returns>
        public static float CalculateDepthOfFocus(float wavelengthNm = 365f, float numericalAperture = 0.65f, float k2Factor = 0.60f)
        {
            if (numericalAperture <= 0f) return 0.1f;
            // Convert nm to um: (wavelengthNm / 1000f)
            return k2Factor * ((wavelengthNm / 1000f) / (numericalAperture * numericalAperture));
        }

        // =========================================================================
        // EQUATIONS (3) & (4): Dill Photochemical Bleaching Kinetic Decomposition
        // dI(z,t)/dz = -[A * M(z,t) + B] * I(z,t)
        // dM(z,t)/dt = -I(z,t) * M(z,t) * C
        // =========================================================================

        /// <summary>
        /// Computes remaining unexposed photoactive inhibitor fraction M(z, t) in [0.0, 1.0].
        /// When dose is insufficient (M > 0.35 at interface), unreacted resist fails to develop.
        /// </summary>
        /// <param name="exposureDoseMilliJoules">Delivered UV dose (I * t_exp) in mJ/cm^2</param>
        /// <param name="depthUm">Depth into photoresist layer from surface in um</param>
        /// <param name="paramA">Dill A bleachable absorption parameter (default 0.85 /um)</param>
        /// <param name="paramB">Dill B unbleachable absorption parameter (default 0.05 /um)</param>
        /// <param name="paramC">Dill C optical sensitivity parameter (default 0.016 cm^2/mJ)</param>
        /// <returns>Normalized remaining inhibitor fraction M in [0.0, 1.0]</returns>
        public static float CalculateDillRemainingInhibitor(float exposureDoseMilliJoules, float depthUm = 1.2f, float paramA = 0.85f, float paramB = 0.05f, float paramC = 0.016f)
        {
            // Effective intensity attenuation with depth: I(z) ≈ I0 * exp(-(A + B) * z)
            float attenuation = Mathf.Exp(-(paramA + paramB) * depthUm);
            float effectiveLocalDose = exposureDoseMilliJoules * attenuation;

            // Kinetic integration: M(z, t) = exp(-C * Dose_effective)
            float remainingInhibitorM = Mathf.Exp(-paramC * effectiveLocalDose);
            return Mathf.Clamp01(remainingInhibitorM);
        }

        // =========================================================================
        // EQUATION (5): Reactive Ion Etch (RIE) Vertical Etch Rate
        // R_vert = k_chem * J_rad + Y_ion * J_ion * sqrt(V_bias)
        // where V_bias ∝ sqrt(P_RF / p_ch)
        // =========================================================================

        /// <summary>
        /// Calculates vertical substrate etch rate in nm/second.
        /// Combines spontaneous chemical radical reaction with energetic ion sputtering.
        /// </summary>
        /// <param name="rfPowerWatts">RF generator power (W), typical: 150-250 W</param>
        /// <param name="chamberPressureMtorr">Chamber pressure (mTorr), typical: 20-50 mTorr</param>
        /// <param name="sf6FlowSccm">Fluorine reactive gas flow rate (sccm), typical: 30-60 sccm</param>
        /// <returns>Vertical etch rate in nm/second</returns>
        public static float CalculateRIEVerticalEtchRate(float rfPowerWatts, float chamberPressureMtorr, float sf6FlowSccm = 45f)
        {
            if (rfPowerWatts <= 0f || chamberPressureMtorr <= 0f) return 0f;

            // DC Self-Bias Voltage: V_bias ∝ sqrt(P_RF / p_ch)
            float dcSelfBias = 32.5f * Mathf.Sqrt(rfPowerWatts / chamberPressureMtorr);

            // Chemical Radical Flux J_rad scales with flow and chamber pressure
            float radicalFluxJ = 0.045f * sf6FlowSccm * (chamberPressureMtorr / 30f);
            float chemicalEtchRate = 0.85f * radicalFluxJ;

            // Ion Bombardment Sputter Yield: Y_ion * J_ion * sqrt(V_bias)
            float ionFluxJ = 0.012f * rfPowerWatts;
            float physicalSputterRate = ionFluxJ * Mathf.Sqrt(dcSelfBias);

            // R_vert = chemical component + physical ion component
            return chemicalEtchRate + physicalSputterRate;
        }

        // =========================================================================
        // EQUATION (6): Etch Anisotropy Factor
        // A_etch = 1 - (R_lat / R_vert)
        // =========================================================================

        /// <summary>
        /// Calculates directional etch anisotropy factor A_etch in [0.0, 1.0].
        /// A_etch = 1.0 represents perfectly vertical directional anisotropic walls.
        /// A_etch = 0.0 represents purely isotropic circular lateral undercut.
        /// </summary>
        /// <param name="rfPowerWatts">RF generator power in Watts</param>
        /// <param name="chamberPressureMtorr">Chamber pressure in mTorr</param>
        /// <returns>Anisotropy factor A_etch between 0.0 and 1.0</returns>
        public static float CalculateEtchAnisotropy(float rfPowerWatts, float chamberPressureMtorr)
        {
            if (chamberPressureMtorr <= 0f) return 1.0f;

            // Ion mean free path is inversely proportional to chamber pressure: lambda_mfp ∝ 1 / p_ch
            // At high pressure (> 120 mTorr), collisional scattering dominates, increasing isotropic lateral etch
            float lateralScatteringFactor = Mathf.Clamp01(chamberPressureMtorr / 120f);
            
            // RF power increases directional sheath acceleration, suppressing lateral deflection
            float directionalSuppression = Mathf.Clamp(rfPowerWatts / 200f, 0.5f, 2.0f);

            // R_lat / R_vert ratio
            float lateralToVerticalRatio = (lateralScatteringFactor * 0.95f) / directionalSuppression;
            lateralToVerticalRatio = Mathf.Clamp01(lateralToVerticalRatio);

            // Equation (6): A_etch = 1 - (R_lat / R_vert)
            return 1.0f - lateralToVerticalRatio;
        }
    }
}
