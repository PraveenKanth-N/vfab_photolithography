using UnityEngine;
using System.Collections;
using SemiconductorDigitalTwin.Physics;
using SemiconductorDigitalTwin.Wafers.Core;

namespace SemiconductorDigitalTwin.Equipment
{
    /// <summary>
    /// Module 2 & 5: Reactive Ion Etcher (RIE) Plasma Reactor Digital Twin.
    /// Simulates industrial vacuum chamber evacuation, mass flow controllers (CF4/SF6/O2),
    /// RF power generation, plasma glow discharge, and ion-assisted chemical etch kinetics.
    /// References: IEEE Access Manuscript Equations (5), (6), and (7).
    /// </summary>
    public class PlasmaEtcherController : MonoBehaviour
    {
        public static PlasmaEtcherController Instance { get; private set; }

        public enum EtcherState
        {
            Idle,
            WaferLoaded,
            VacuumPumping,
            GasStabilization,
            PlasmaIgnition,
            ActiveEtching,
            ChamberVenting,
            Complete,
            Fault
        }

        [Header("Machine State")]
        public EtcherState currentState = EtcherState.Idle;

        [Header("Recipe Parameters (Table II)")]
        [Tooltip("RF Generator Bias Power in Watts (Nominal: 150W, Range: 50-400W)")]
        [Range(50f, 400f)] public float rfPowerWatts = 150f;

        [Tooltip("Vacuum Chamber Pressure in mTorr (Nominal: 40 mTorr, Range: 10-200 mTorr)")]
        [Range(10f, 200f)] public float chamberPressureMtorr = 40f;

        [Tooltip("SF6 Reactive Gas Flow in sccm (Nominal: 30 sccm)")]
        [Range(0f, 100f)] public float sf6FlowSccm = 30f;

        [Tooltip("CF4 Radical Gas Flow in sccm (Nominal: 20 sccm)")]
        [Range(0f, 100f)] public float cf4FlowSccm = 20f;

        [Tooltip("Oxygen Gas Flow in sccm (Nominal: 5 sccm)")]
        [Range(0f, 30f)] public float o2FlowSccm = 5f;

        [Tooltip("Etch Process Duration in seconds (Nominal: 45s)")]
        [Range(5f, 180f)] public float etchDurationSec = 45f;

        [Header("Physical Outputs (Calculated)")]
        [SerializeField] private float calculatedVerticalRateNmPerSec;
        [SerializeField] private float calculatedAnisotropyFactor;
        [SerializeField] private float totalEtchedDepthNm;
        [SerializeField] private float currentEtchProgress; // 0 to 1

        [Header("Visual & Audio Feedback")]
        public Light plasmaGlowLight;
        public ParticleSystem plasmaVFX;
        public AudioSource plasmaAudioSource;
        public Color glowColorActive = new Color(0.6f, 0.2f, 1f, 1f); // Fluorine/Argon purple glow

        [Header("Wafer Placement")]
        public Transform waferChuckAnchor;
        public GameObject loadedWaferObject;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (plasmaGlowLight != null) plasmaGlowLight.enabled = false;
            if (plasmaVFX != null) plasmaVFX.Stop();
        }

        /// <summary>
        /// Places a wafer into the RIE vacuum chamber chuck.
        /// </summary>
        public void LoadWafer(GameObject wafer)
        {
            if (currentState != EtcherState.Idle) return;

            loadedWaferObject = wafer;
            if (waferChuckAnchor != null && wafer != null)
            {
                wafer.transform.SetParent(waferChuckAnchor);
                wafer.transform.localPosition = Vector3.zero;
                wafer.transform.localRotation = Quaternion.identity;
            }
            currentState = EtcherState.WaferLoaded;
            Debug.Log("[PlasmaEtcher] Wafer successfully loaded onto cathode chuck.");
        }

        /// <summary>
        /// Initiates the automated RIE plasma etching cycle.
        /// </summary>
        public void StartEtchSequence()
        {
            if (currentState != EtcherState.WaferLoaded && currentState != EtcherState.Idle)
            {
                Debug.LogWarning("[PlasmaEtcher] Cannot start sequence; tool is not in ready state.");
                return;
            }

            StartCoroutine(EtchSequenceRoutine());
        }

        private IEnumerator EtchSequenceRoutine()
        {
            // 1. Pump down chamber pressure to base vacuum
            currentState = EtcherState.VacuumPumping;
            Debug.Log($"[PlasmaEtcher] Roughing and turbomolecular pump active: target base pressure < 5 mTorr");
            yield return new WaitForSeconds(1.5f);

            // 2. Gas Flow Stabilization
            currentState = EtcherState.GasStabilization;
            Debug.Log($"[PlasmaEtcher] MFCs introducing SF6: {sf6FlowSccm} sccm, CF4: {cf4FlowSccm} sccm, O2: {o2FlowSccm} sccm at {chamberPressureMtorr} mTorr");
            yield return new WaitForSeconds(1.0f);

            // 3. RF Plasma Ignition
            currentState = EtcherState.PlasmaIgnition;
            if (plasmaGlowLight != null)
            {
                plasmaGlowLight.enabled = true;
                plasmaGlowLight.color = glowColorActive;
            }
            if (plasmaVFX != null) plasmaVFX.Play();
            if (plasmaAudioSource != null) plasmaAudioSource.Play();
            Debug.Log($"[PlasmaEtcher] RF Generator struck: {rfPowerWatts}W at 13.56 MHz. Plasma sheath established.");
            yield return new WaitForSeconds(0.8f);

            // 4. Active Etching Execution
            currentState = EtcherState.ActiveEtching;
            
            // Calculate real-time kinetics from PhysicsSimulationEngine
            calculatedVerticalRateNmPerSec = PhysicsSimulationEngine.CalculateRIEVerticalEtchRate(rfPowerWatts, chamberPressureMtorr, sf6FlowSccm);
            calculatedAnisotropyFactor = PhysicsSimulationEngine.CalculateEtchAnisotropy(rfPowerWatts, chamberPressureMtorr);
            totalEtchedDepthNm = calculatedVerticalRateNmPerSec * etchDurationSec;

            float elapsedTime = 0f;
            while (elapsedTime < etchDurationSec)
            {
                elapsedTime += Time.deltaTime * 5f; // Accelerated for interactive fluidity
                currentEtchProgress = Mathf.Clamp01(elapsedTime / etchDurationSec);
                yield return null;
            }

            // 5. Plasma Extinction & Chamber Venting
            currentState = EtcherState.ChamberVenting;
            if (plasmaGlowLight != null) plasmaGlowLight.enabled = false;
            if (plasmaVFX != null) plasmaVFX.Stop();
            if (plasmaAudioSource != null) plasmaAudioSource.Stop();
            Debug.Log("[PlasmaEtcher] RF bias powered down. N2 purge venting to atmospheric pressure.");
            yield return new WaitForSeconds(1.2f);

            // 6. Consequence Evaluation and Wafer Mutation
            currentState = EtcherState.Complete;
            if (ConsequenceRuleEngine.Instance != null)
            {
                ConsequenceRuleEngine.Instance.ProcessReactiveIonEtching(
                    rfPowerWatts,
                    chamberPressureMtorr,
                    sf6FlowSccm,
                    etchDurationSec
                );
            }

            Debug.Log($"[PlasmaEtcher] Etch complete: Etched {totalEtchedDepthNm:F1} nm (Anisotropy A={calculatedAnisotropyFactor:F2})");
        }
    }
}
