using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SemiconductorDigitalTwin.Wafers.Core;

namespace SemiconductorDigitalTwin.Backend
{
    /// <summary>
    /// Module 9: High-throughput telemetry and microservices bridge connecting 
    /// the Unity VR frontend to the FastAPI digital twin persistence backend.
    /// Handles sessions, wafer state synchronization, live sensor streams, and defect logging.
    /// </summary>
    public class TelemetryBridge : MonoBehaviour
    {
        public static TelemetryBridge Instance { get; private set; }

        [Header("Backend Connection")]
        [SerializeField] private string backendBaseUrl = "http://localhost:8005/api/v1";
        [SerializeField] private bool autoConnectOnStart = true;
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private float telemetryHeartbeatInterval = 1.0f;

        [Header("Active Session Info")]
        [SerializeField] private string activeSessionId = "";
        [SerializeField] private string studentId = "STUDENT_VR_01";
        [SerializeField] private string activeModule = "Photolithography_Etching";

        private bool isBackendAvailable = false;
        private readonly Queue<IEnumerator> pendingRequests = new Queue<IEnumerator>();
        private bool isProcessingQueue = false;

        public string ActiveSessionId => activeSessionId;
        public bool IsBackendAvailable => isBackendAvailable;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoConnectOnStart)
            {
                StartCoroutine(CheckHealthAndInitialize());
            }
        }

        private IEnumerator CheckHealthAndInitialize()
        {
            string url = $"{backendBaseUrl.Replace("/api/v1", "")}/health";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 3;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    isBackendAvailable = true;
                    if (verboseLogging) Debug.Log($"[TelemetryBridge] Connected to FastAPI backend: {req.downloadHandler.text}");
                    CreateSession(studentId, activeModule);
                }
                else
                {
                    isBackendAvailable = false;
                    Debug.LogWarning($"[TelemetryBridge] FastAPI backend not detected at {url}. Operating in local offline mode.");
                }
            }
        }

        #region Session Management

        /// <summary>
        /// Registers a new student training session in the backend.
        /// </summary>
        public void CreateSession(string student, string module, Action<string> onSuccess = null, Action<string> onError = null)
        {
            studentId = student;
            activeModule = module;
            string url = $"{backendBaseUrl}/sessions/";
            string jsonBody = $"{{\"student_id\":\"{Escape(student)}\",\"module_name\":\"{Escape(module)}\"}}";

            StartCoroutine(PostJsonCoroutine(url, jsonBody, (response) =>
            {
                // Parse session ID from response
                SessionResponse resp = JsonUtility.FromJson<SessionResponse>(response);
                if (resp != null && !string.IsNullOrEmpty(resp.session_id))
                {
                    activeSessionId = resp.session_id;
                    if (verboseLogging) Debug.Log($"[TelemetryBridge] Active session registered: {activeSessionId}");
                    onSuccess?.Invoke(activeSessionId);
                }
                else
                {
                    onSuccess?.Invoke(response);
                }
            }, onError));
        }

        /// <summary>
        /// Closes the active training session upon completion.
        /// </summary>
        public void CloseActiveSession(float finalYield = 100f, Action onSuccess = null)
        {
            if (string.IsNullOrEmpty(activeSessionId)) return;

            string url = $"{backendBaseUrl}/sessions/{activeSessionId}/complete";
            string jsonBody = $"{{\"yield_percentage\":{finalYield.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}}}";

            StartCoroutine(PostJsonCoroutine(url, jsonBody, (resp) =>
            {
                if (verboseLogging) Debug.Log($"[TelemetryBridge] Session {activeSessionId} closed successfully.");
                onSuccess?.Invoke();
            }, (err) => Debug.LogWarning($"[TelemetryBridge] Error closing session: {err}")));
        }

        #endregion

        #region Wafer State Serialization

        /// <summary>
        /// Synchronizes the current wafer physical state, critical dimension, and layer stack to the backend.
        /// </summary>
        public void SyncWaferState(WaferStateData waferData, Action onSuccess = null, Action<string> onError = null)
        {
            if (waferData == null) return;

            string url = $"{backendBaseUrl}/wafers/";

            // Convert to backend DTO
            WaferPayload payload = new WaferPayload
            {
                session_id = activeSessionId,
                wafer_id = waferData.WaferId != null ? waferData.WaferId.ToString() : "WAF-001",
                state = waferData.State.ToString(),
                revision = waferData.Revision,
                diameter = waferData.Diameter,
                thickness = waferData.Thickness,
                substrate_material = waferData.SubstrateMaterial ?? "Silicon",
                crystal_orientation = waferData.CrystalOrientation ?? "<100>",
                surface_state = waferData.SurfaceState ?? "Clean",
                defect_count = waferData.DefectCount
            };

            string jsonBody = JsonUtility.ToJson(payload);

            StartCoroutine(PostJsonCoroutine(url, jsonBody, (resp) =>
            {
                if (verboseLogging) Debug.Log($"[TelemetryBridge] Wafer {payload.wafer_id} state synced (Rev {payload.revision}).");
                onSuccess?.Invoke();
            }, onError));
        }

        #endregion

        #region Equipment Telemetry Stream

        /// <summary>
        /// Records high-frequency operational telemetry from fabrication tools.
        /// </summary>
        public void RecordTelemetry(string equipmentName, string parameterName, float value, string unit = "")
        {
            string url = $"{backendBaseUrl}/telemetry/";
            string jsonBody = $"{{\"session_id\":\"{Escape(activeSessionId)}\",\"equipment_name\":\"{Escape(equipmentName)}\",\"parameter_name\":\"{Escape(parameterName)}\",\"parameter_value\":{value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},\"unit\":\"{Escape(unit)}\"}}";

            StartCoroutine(PostJsonCoroutine(url, jsonBody, null, null));
        }

        #endregion

        #region Defect Reporting

        /// <summary>
        /// Logs an analytical defect to the central database for yield analysis and student feedback.
        /// </summary>
        public void LogDefect(WaferDefect defect, string waferId, string originTool, Action onSuccess = null)
        {
            if (defect == null) return;

            string url = $"{backendBaseUrl}/defects/";
            DefectPayload payload = new DefectPayload
            {
                session_id = activeSessionId,
                wafer_id = waferId,
                defect_id = defect.DefectId ?? Guid.NewGuid().ToString().Substring(0, 8),
                defect_type = defect.Type ?? "UnknownDefect",
                severity = defect.Severity,
                is_critical = defect.IsCritical,
                origin_equipment = originTool,
                pos_x = defect.Position.x,
                pos_y = defect.Position.y,
                pos_z = defect.Position.z
            };

            string jsonBody = JsonUtility.ToJson(payload);

            StartCoroutine(PostJsonCoroutine(url, jsonBody, (resp) =>
            {
                if (verboseLogging) Debug.Log($"[TelemetryBridge] Defect {payload.defect_type} logged to backend.");
                onSuccess?.Invoke();
            }, (err) => Debug.LogWarning($"[TelemetryBridge] Defect log warning: {err}")));
        }

        #endregion

        #region Recipe Management

        /// <summary>
        /// Retrieves process recipe parameters from the backend catalog.
        /// </summary>
        public void FetchRecipe(string processType, Action<string> onRecipeReceived, Action<string> onError = null)
        {
            string url = $"{backendBaseUrl}/recipes/{UnityWebRequest.EscapeURL(processType)}";
            StartCoroutine(GetJsonCoroutine(url, onRecipeReceived, onError));
        }

        #endregion

        #region Web Request Helpers

        private IEnumerator PostJsonCoroutine(string url, string jsonBody, Action<string> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 5;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(req.downloadHandler.text);
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        private IEnumerator GetJsonCoroutine(string url, Action<string> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(req.downloadHandler.text);
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        private static string Escape(string s) => s == null ? "" : s.Replace("\"", "\\\"");

        #endregion

        #region Data Transfer Objects

        [Serializable]
        private class SessionResponse
        {
            public string session_id;
            public string student_id;
            public string module_name;
            public string status;
        }

        [Serializable]
        private class WaferPayload
        {
            public string session_id;
            public string wafer_id;
            public string state;
            public int revision;
            public float diameter;
            public float thickness;
            public string substrate_material;
            public string crystal_orientation;
            public string surface_state;
            public int defect_count;
        }

        [Serializable]
        private class DefectPayload
        {
            public string session_id;
            public string wafer_id;
            public string defect_id;
            public string defect_type;
            public float severity;
            public bool is_critical;
            public string origin_equipment;
            public float pos_x;
            public float pos_y;
            public float pos_z;
        }

        #endregion
    }
}
