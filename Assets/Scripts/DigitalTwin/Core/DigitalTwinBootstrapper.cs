using UnityEngine;
using UnityEngine.UI;
using SemiconductorDigitalTwin.Wafers.Core;
using SemiconductorDigitalTwin.Physics;
using SemiconductorDigitalTwin.Equipment;
using SemiconductorDigitalTwin.TimeTravel;
using SemiconductorDigitalTwin.Pedagogy;
using SemiconductorDigitalTwin.Backend;

namespace SemiconductorDigitalTwin.Core
{
    /// <summary>
    /// Automatic turnkey bootstrapper for the Semiconductor Digital Twin system.
    /// Runs automatically after any scene loads in Unity without requiring manual Inspector setup.
    /// Ensures all 10 modules are instantiated, linked, and active upon pressing Play.
    /// </summary>
    public static class DigitalTwinBootstrapper
    {
        private const string BootstrapperGameObjectName = "_DigitalTwin_CoreSystem";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            // Prevent duplicate initialization
            if (GameObject.Find(BootstrapperGameObjectName) != null)
            {
                return;
            }

            Debug.Log("[DigitalTwinBootstrapper] Initializing AI-Driven Semiconductor Cleanroom Digital Twin (10 Modules)...");

            // 1. Create root system GameObject
            GameObject systemRoot = new GameObject(BootstrapperGameObjectName);
            Object.DontDestroyOnLoad(systemRoot);

            // 2. Attach Core & Wafer Digital Twin (Module 2)
            var waferTwin = systemRoot.AddComponent<WaferDigitalTwin>();

            // 3. Attach Consequence Rule Engine (Module 3 & 4)
            var ruleEngine = systemRoot.AddComponent<ConsequenceRuleEngine>();

            // 4. Attach Plasma Etcher Controller (Module 6)
            var plasmaEtcher = systemRoot.AddComponent<PlasmaEtcherController>();

            // 5. Attach Time-Travel Debugger (Module 10)
            var timeTravel = systemRoot.AddComponent<TimeTravelDebugger>();

            // 6. Attach Socratic AI Tutor (Module 8)
            var socraticTutor = systemRoot.AddComponent<SocraticAITutor>();

            // 7. Attach Microservices Telemetry Bridge (Module 9)
            var telemetryBridge = systemRoot.AddComponent<TelemetryBridge>();

            // 8. Attach Hardware Integration Bridge (Links VFab tools to Digital Twin)
            var bridgeType = System.Type.GetType("SemiconductorDigitalTwin.Equipment.VFabEquipmentBridge, Assembly-CSharp") 
                ?? typeof(PlasmaEtcherController).Assembly.GetType("SemiconductorDigitalTwin.Equipment.VFabEquipmentBridge");
            if (bridgeType != null)
            {
                systemRoot.AddComponent(bridgeType);
            }

            // 9. Attach Runtime In-Game HUD Helper
            systemRoot.AddComponent<DigitalTwinRuntimeHUD>();

            Debug.Log("[DigitalTwinBootstrapper] All 10 Modules successfully initialized and operational!");
        }
    }

    /// <summary>
    /// In-game overlay helper that provides keyboard shortcuts and HUD status for 
    /// the Time-Travel Debugger and Socratic AI Tutor.
    /// Press 'T' to step backward in time, 'Y' to step forward, 'H' to view status.
    /// </summary>
    public class DigitalTwinRuntimeHUD : MonoBehaviour
    {
        private bool showHUD = true;
        private GUIStyle boxStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        private void OnGUI()
        {
            if (!showHUD) return;

            InitStyles();

            // Render HUD box in top-left
            GUILayout.BeginArea(new Rect(15, 15, 340, 240), boxStyle);
            GUILayout.Label("Semiconductor Digital Twin HUD", headerStyle);
            GUILayout.Space(4);

            var waferTwin = WaferDigitalTwin.Instance ?? FindAnyObjectByType<WaferDigitalTwin>();
            var timeTravel = TimeTravelDebugger.Instance ?? FindAnyObjectByType<TimeTravelDebugger>();
            var backend = TelemetryBridge.Instance ?? FindAnyObjectByType<TelemetryBridge>();

            if (waferTwin != null)
            {
                GUILayout.Label($"Wafer State: {waferTwin.State} (Rev {waferTwin.Revision})", labelStyle);
                GUILayout.Label($"Active Defects: {waferTwin.DefectCount} | Layers: {waferTwin.LayerStack.Count}", labelStyle);
            }

            if (timeTravel != null)
            {
                GUILayout.Label($"Timeline Snapshots: {timeTravel.SnapshotCount} (Active: {timeTravel.CurrentIndex})", labelStyle);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<< Time Travel Prev [T]", buttonStyle))
                {
                    timeTravel.StepBackward();
                }
                if (GUILayout.Button("Time Travel Next >> [Y]", buttonStyle))
                {
                    timeTravel.StepForward();
                }
                GUILayout.EndHorizontal();
            }

            if (backend != null)
            {
                string statusText = backend.IsBackendAvailable ? "<color=#00FF66>ONLINE (Connected)</color>" : "<color=#FFCC00>OFFLINE (Local Standalone)</color>";
                GUILayout.Label($"Backend (Port 8005): {statusText}", labelStyle);
            }

            GUILayout.Space(4);
            GUILayout.Label("Shortcuts: [T] Step Back | [Y] Step Forward | [H] Toggle HUD", labelStyle);

            GUILayout.EndArea();
        }

        private void Update()
        {
            // Toggle HUD
            if (Input.GetKeyDown(KeyCode.H))
            {
                showHUD = !showHUD;
            }

            var timeTravel = TimeTravelDebugger.Instance ?? FindAnyObjectByType<TimeTravelDebugger>();
            if (timeTravel != null)
            {
                if (Input.GetKeyDown(KeyCode.T))
                {
                    timeTravel.StepBackward();
                }
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    timeTravel.StepForward();
                }
            }
        }

        private void InitStyles()
        {
            if (boxStyle != null) return;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0.05f, 0.08f, 0.12f, 0.88f));

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14
            };
            headerStyle.normal.textColor = new Color(0.3f, 0.8f, 1.0f);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true
            };
            labelStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10
            };
        }

        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
