using UnityEngine;
using System.Collections;
using SemiconductorDigitalTwin.Physics;
using SemiconductorDigitalTwin.Wafers.Core;
using SemiconductorDigitalTwin.TimeTravel;
using SemiconductorDigitalTwin.Pedagogy;
using SemiconductorDigitalTwin.Backend;

namespace SemiconductorDigitalTwin.Equipment
{
    /// <summary>
    /// Integration bridge linking existing VFab hardware controllers
    /// (SpinCoater, Hotplate, Aligner, Developer) to the authoritative WaferDigitalTwin,
    /// ConsequenceRuleEngine, Socratic AI Tutor, and Cloud Backend.
    /// </summary>
    public class VFabEquipmentBridge : MonoBehaviour
    {
        public static VFabEquipmentBridge Instance { get; private set; }

        [Header("Target Digital Twin Components")]
        public WaferDigitalTwin activeWaferTwin;
        public ConsequenceRuleEngine consequenceEngine;
        public TimeTravelDebugger timeTravelDebugger;
        public SocraticAITutor socraticTutor;
        public TelemetryBridge telemetryBridge;

        [Header("Monitored VFab Controllers")]
        public SpinCoaterController spinCoater;
        public HotplateController hotplate;
        public ExposureStationController exposureStation;
        public LithoDevelopmentEngine developerEngine;
        public PlasmaEtcherController plasmaEtcher;

        private SpinCoaterController.CoaterState _lastCoaterState = SpinCoaterController.CoaterState.Empty;
        private HotplateController.HotplateState _lastHotplateState = HotplateController.HotplateState.Idle;
        private bool _lastExposureState = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            FindCoreReferences();
        }

        private void Start()
        {
            FindCoreReferences();
            StartCoroutine(MonitorEquipmentRoutine());
        }

        private void FindCoreReferences()
        {
            if (activeWaferTwin == null) activeWaferTwin = FindAnyObjectByType<WaferDigitalTwin>();
            if (consequenceEngine == null) consequenceEngine = FindAnyObjectByType<ConsequenceRuleEngine>();
            if (timeTravelDebugger == null) timeTravelDebugger = FindAnyObjectByType<TimeTravelDebugger>();
            if (socraticTutor == null) socraticTutor = FindAnyObjectByType<SocraticAITutor>();
            if (telemetryBridge == null) telemetryBridge = FindAnyObjectByType<TelemetryBridge>();

            if (spinCoater == null) spinCoater = FindAnyObjectByType<SpinCoaterController>();
            if (hotplate == null) hotplate = HotplateController.GetProcessingHotplate();
            if (exposureStation == null) exposureStation = FindAnyObjectByType<ExposureStationController>();
            if (developerEngine == null) developerEngine = FindAnyObjectByType<LithoDevelopmentEngine>();
            if (plasmaEtcher == null) plasmaEtcher = FindAnyObjectByType<PlasmaEtcherController>();
        }

        private IEnumerator MonitorEquipmentRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.25f);

                // 1. Monitor Spin Coater
                if (spinCoater != null)
                {
                    if (_lastCoaterState == SpinCoaterController.CoaterState.Spinning && 
                        spinCoater.currentCoaterState == SpinCoaterController.CoaterState.Complete)
                    {
                        OnSpinCoaterCompleted();
                    }
                    _lastCoaterState = spinCoater.currentCoaterState;
                }

                // 2. Monitor Hotplate
                if (hotplate != null)
                {
                    if (_lastHotplateState == HotplateController.HotplateState.Baking && 
                        hotplate.currentState == HotplateController.HotplateState.Complete)
                    {
                        OnHotplateCompleted();
                    }
                    _lastHotplateState = hotplate.currentState;
                }

                // 3. Monitor UV Exposure
                if (exposureStation != null)
                {
                    if (_lastExposureState && !exposureStation.isExposingNow)
                    {
                        OnExposureCompleted();
                    }
                    _lastExposureState = exposureStation.isExposingNow;
                }
            }
        }

        private void OnSpinCoaterCompleted()
        {
            Debug.Log("[VFabBridge] Spin coating sequence completed. Evaluating physical consequences...");
            float spinSpeed = 3500f; // Nominal
            float duration = 30f;

            if (consequenceEngine != null)
            {
                consequenceEngine.ProcessSpinCoating(spinSpeed, duration);
            }

            PostProcessStageCompleted("Photoresist Spin Coating");
        }

        private void OnHotplateCompleted()
        {
            Debug.Log("[VFabBridge] Soft-bake thermal cycle completed. Evaluating physical consequences...");
            float bakeTemp = (hotplate != null) ? hotplate.targetTemperature : 95f;
            float bakeDuration = (hotplate != null) ? hotplate.targetBakeTime : 60f;

            if (consequenceEngine != null)
            {
                consequenceEngine.ProcessSoftBake(bakeTemp, bakeDuration);
            }

            PostProcessStageCompleted("Soft-Bake Thermal Processing");
        }

        private void OnExposureCompleted()
        {
            Debug.Log("[VFabBridge] Optical UV exposure finished. Evaluating physical consequences...");
            float dose = 180f; // Nominal mJ/cm^2
            float alignOffset = 0.05f; // Nominal um

            if (consequenceEngine != null)
            {
                consequenceEngine.ProcessUVExposure(dose, alignOffset);
            }

            PostProcessStageCompleted("UV Optical Mask Exposure");
        }

        public void OnPlasmaEtchCompleted(float rfPower, float chamberPressure, float sf6Flow, float duration)
        {
            Debug.Log("[VFabBridge] Plasma RIE Etching finished. Evaluating physical consequences...");
            if (consequenceEngine != null)
            {
                consequenceEngine.ProcessReactiveIonEtching(rfPower, chamberPressure, sf6Flow, duration);
            }

            PostProcessStageCompleted("Reactive Ion Etching (RIE)");
        }

        private void PostProcessStageCompleted(string stageName)
        {
            // 1. Capture snapshot in Time-Travel Debugger
            if (timeTravelDebugger != null)
            {
                timeTravelDebugger.CaptureSnapshot(stageName);
            }

            // 2. Transmit state to Backend
            if (telemetryBridge != null && activeWaferTwin != null)
            {
                telemetryBridge.SyncWaferState(activeWaferTwin.GetState());
            }

            // 3. Trigger Socratic AI Tutor if defects were detected
            if (socraticTutor != null && activeWaferTwin != null && activeWaferTwin.DefectCount > 0)
            {
                WaferDefect latestDefect = activeWaferTwin.Defects[activeWaferTwin.DefectCount - 1];
                socraticTutor.TriggerInquiryForDefect(latestDefect.Type);
            }
        }
    }
}
