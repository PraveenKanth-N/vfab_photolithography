using UnityEngine;
using System;
using System.Collections.Generic;
using SemiconductorDigitalTwin.Wafers.Core;
using SemiconductorDigitalTwin.Wafers.ScriptableObjects;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Authoritative runtime wafer state.
    /// Single source of truth for wafer identity, geometry, state, and history.
    /// Other modules interact through the public API; do not directly manipulate internal fields.
    /// </summary>
    [DisallowMultipleComponent]
    public class WaferDigitalTwin : MonoBehaviour
    {
        // ===== WAVER IDENTITY =====

        [Header("Identity")]
        [SerializeField] private WaferId _waferId;

        // ===== GEOMETRY =====

        [Header("Geometry")]
        [SerializeField] private float _diameter = 300f;
        [SerializeField] private float _thickness = 0.775f;
        [SerializeField] private string _substrateMaterial = "Silicon";
        [SerializeField] private string _crystalOrientation = "100";

        // ===== STATE =====

        [Header("State")]
        [SerializeField] private WaferState _waferState = WaferState.Initialized;
        [SerializeField] private int _revision = 0;

        // ===== LAYER STACK =====

        [Header("Layer Stack")]
        [SerializeField] private List<WaferLayer> _layerStack = new List<WaferLayer>();

        // ===== SURFACE STATE =====

        [Header("Surface State")]
        [SerializeField] private string _surfaceState = "Clean";

        // ===== DEFECTS =====

        [Header("Defects")]
        [SerializeField] private List<WaferDefect> _defects = new List<WaferDefect>();

        // ===== HISTORY =====

        [Header("History")]
        [SerializeField] private List<WaferStateSnapshot> _history = new List<WaferStateSnapshot>();

        // ===== EVENTS =====

        /// <summary>
        /// Raised when wafer is initialized
        /// </summary>
        public event Action OnInitialized;

        /// <summary>
        /// Raised when wafer state changes
        /// </summary>
        public event Action<WaferState, WaferState> OnStateChanged;

        /// <summary>
        /// Raised when layer stack changes
        /// </summary>
        public event Action OnLayerStackChanged;

        /// <summary>
        /// Raised when surface state changes
        /// </summary>
        public event Action OnSurfaceStateChanged;

        /// <summary>
        /// Raised when a defect is registered
        /// </summary>
        public event Action<WaferDefect> OnDefectRegistered;

        /// <summary>
        /// Raised when a snapshot is created
        /// </summary>
        public event Action<WaferStateSnapshot> OnSnapshotCreated;

        // ===== PRIVATE STATE =====

        private bool _hasBeenInitialized = false;

        // ===== PUBLIC PROPERTIES =====

        /// <summary>
        /// Wafer identity
        /// </summary>
        public WaferId Id => _waferId ??= new WaferId();

        /// <summary>
        /// Wafer diameter in millimeters
        /// </summary>
        public float Diameter
        {
            get => _diameter;
            private set => _diameter = value;
        }

        /// <summary>
        /// Wafer thickness in millimeters
        /// </summary>
        public float Thickness => _thickness;

        /// <summary>
        /// Substrate material
        /// </summary>
        public string SubstrateMaterial => _substrateMaterial;

        /// <summary>
        /// Crystal orientation
        /// </summary>
        public string CrystalOrientation => _crystalOrientation;

        /// <summary>
        /// Current wafer state
        /// </summary>
        public WaferState State
        {
            get => _waferState;
            private set
            {
                if (_waferState != value)
                {
                    var oldState = _waferState;
                    _waferState = value;
                    _revision++;
                    OnStateChanged?.Invoke(oldState, _waferState);
                    Debug.Log($"[WaferDigitalTwin] State changed: {oldState} â†’ {_waferState} (revision {_revision})");
                }
            }
        }

        /// <summary>
        /// Current revision number
        /// </summary>
        public int Revision => _revision;

        /// <summary>
        /// Layer stack read-only
        /// </summary>
        public IReadOnlyList<WaferLayer> LayerStack => _layerStack.AsReadOnly();

        /// <summary>
        /// Surface state
        /// </summary>
        public string SurfaceState
        {
            get => _surfaceState;
            private set
            {
                if (_surfaceState != value)
                {
                    var oldState = _surfaceState;
                    _surfaceState = value;
                    _revision++;
                    OnSurfaceStateChanged?.Invoke();
                    Debug.Log($"[WaferDigitalTwin] Surface state changed: {oldState} â†’ {_surfaceState} (revision {_revision})");
                }
            }
        }

        /// <summary>
        /// History read-only
        /// </summary>
        public IReadOnlyList<WaferStateSnapshot> History => _history.AsReadOnly();

        /// <summary>
        /// Defects read-only
        /// </summary>
        public IReadOnlyList<WaferDefect> Defects => _defects.AsReadOnly();

        /// <summary>Defect count</summary>
        public int DefectCount => _defects.Count;

        public static WaferDigitalTwin Instance { get; private set; }

        // ===== CONSTRUCTION =====

        private void Awake()
        {
            if (Instance == null) Instance = this;

            // Ensure wafer ID is set
            _waferId ??= new WaferId();

            // If this is a new wafer (no history), initialize it
            if (_history.Count == 0 && _layerStack.Count == 0)
            {
                Initialize(new WaferDefinition());
            }
        }

        // ===== PUBLIC API =====

        /// <summary>
        /// Restores wafer state from a historical snapshot (for Time-Travel debugging).
        /// </summary>
        public void RestoreFromSnapshot(WaferStateSnapshot snapshot)
        {
            if (snapshot == null) return;
            WaferState oldState = _waferState;
            _waferState = snapshot.State;
            _revision = snapshot.Revision;
            _diameter = snapshot.Diameter;
            _thickness = snapshot.Thickness;
            _surfaceState = snapshot.SurfaceState;
            _layerStack = new List<WaferLayer>(snapshot.LayerStack ?? new List<WaferLayer>());
            _defects = new List<WaferDefect>(snapshot.Defects ?? new List<WaferDefect>());
            _revision++;
            OnStateChanged?.Invoke(oldState, _waferState);
            Debug.Log($"[WaferDigitalTwin] Restored state from snapshot: {snapshot.Name} (Rev {_revision})");
        }

        /// <summary>
        /// Initialize a new wafer with the given definition.
        /// Call once per wafer instance.
        /// </summary>
        /// <param name="definition">Wafer definition containing initial properties</param>
        public void Initialize(WaferDefinition definition)
        {
            // Set geometry from definition
            _diameter = definition.Diameter;
            _thickness = definition.Thickness;
            _substrateMaterial = definition.SubstrateMaterial;
            _crystalOrientation = definition.CrystalOrientation;

            // Set initial state
            _waferState = definition.InitialState;
            _surfaceState = definition.InitialSurfaceState;

            // Initialize layer stack from definition
            _layerStack.Clear();
            if (definition.InitialLayerCount > 0)
            {
                for (int i = 0; i < definition.InitialLayerCount; i++)
                {
                    _layerStack.Add(new WaferLayer
                    {
                        LayerId = i,
                        Material = _substrateMaterial,
                        Thickness = _thickness / definition.InitialLayerCount,
                        Position = i
                    });
                }
            }

            // Record initialization in history
            _history.Add(CreateSnapshot("Initialization"));

            // Fire initialization event
            OnInitialized?.Invoke();
            Debug.Log($"[WaferDigitalTwin] Wafer initialized: {Id} (revision {_revision})");
        }

        /// <summary>
        /// Initialize from a DefaultWaferDefinition ScriptableObject directly.
        /// Overload to accept ScriptableObject without requiring manual conversion.
        /// </summary>
        public void Initialize(SemiconductorDigitalTwin.Wafers.ScriptableObjects.DefaultWaferDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[WaferDigitalTwin] DefaultWaferDefinition is null. Using built-in defaults.");
                Initialize(new WaferDefinition());
                return;
            }
            Initialize(WaferDefinition.FromScriptableObject(definition));
        }

        /// <summary>
        /// Get current wafer state.
        /// </summary>
        /// <returns>Current wafer state data</returns>
        public WaferStateData GetState()
        {
            return new WaferStateData
            {
                WaferId = _waferId,
                State = _waferState,
                Revision = _revision,
                Diameter = _diameter,
                Thickness = _thickness,
                SubstrateMaterial = _substrateMaterial,
                CrystalOrientation = _crystalOrientation,
                SurfaceState = _surfaceState,
                LayerCount = _layerStack.Count,
                LayerStack = _layerStack.ConvertAll(l => l),
                DefectCount = _defects.Count,
                Defects = _defects.ConvertAll(d => d),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Update the wafer's surface state.
        /// </summary>
        /// <param name="newSurfaceState">New surface state string</param>
        public void UpdateSurfaceState(string newSurfaceState)
        {
            SurfaceState = newSurfaceState;
            _history.Add(CreateSnapshot($"Surface state: {newSurfaceState}"));
            Debug.Log($"[WaferDigitalTwin] Surface updated: {newSurfaceState} (revision {_revision})");
        }

        /// <summary>
        /// Add a layer to the wafer's layer stack.
        /// </summary>
        /// <param name="layerId">Layer identifier</param>
        /// <param name="material">Layer material</param>
        /// <param name="thickness">Layer thickness in millimeters</param>
        public void AddLayer(int layerId, string material, float thickness)
        {
            _layerStack.Add(new WaferLayer
            {
                LayerId = layerId,
                Material = material,
                Thickness = thickness,
                Position = _layerStack.Count
            });
            _revision++;
            OnLayerStackChanged?.Invoke();
            _history.Add(CreateSnapshot($"Layer added: {layerId}"));
            Debug.Log($"[WaferDigitalTwin] Layer added: {layerId} (revision {_revision})");
        }

        /// <summary>
        /// Update an existing layer in the stack.
        /// </summary>
        /// <param name="layerId">Layer identifier to update</param>
        /// <param name="newMaterial">New material</param>
        /// <param name="newThickness">New thickness</param>
        public void UpdateLayer(int layerId, string newMaterial, float newThickness)
        {
            var layer = _layerStack.Find(l => l.LayerId == layerId);
            if (layer != null)
            {
                var oldMaterial = layer.Material;
                var oldThickness = layer.Thickness;
                layer.Material = newMaterial;
                layer.Thickness = newThickness;
                _revision++;
                OnLayerStackChanged?.Invoke();
                _history.Add(CreateSnapshot($"Layer updated: {layerId} ({(oldMaterial != newMaterial ? "material" : "")}{(oldThickness != newThickness ? "thickness" : "")})"));
                Debug.Log($"[WaferDigitalTwin] Layer updated: {layerId} (revision {_revision})");
            }
            else
            {
                Debug.LogWarning($"[WaferDigitalTwin] Layer {layerId} not found in stack");
            }
        }

        /// <summary>
        /// Register a defect on the wafer.
        /// </summary>
        /// <param name="defectType">Type/description of the defect</param>
        /// <param name="position">Position on wafer (x, y, z in mm)</param>
        /// <param name="severity">Severity level (0-1, where 1 is most severe)</param>
        public void RegisterDefect(string defectType, (float x, float y, float z) position, float severity = 0.5f)
        {
            _defects.Add(new WaferDefect
            {
                DefectId = Guid.NewGuid().ToString(),
                Type = defectType,
                Position = position,
                Severity = severity,
                DetectedTimestamp = DateTime.UtcNow
            });
            _revision++;
            OnDefectRegistered?.Invoke(_defects[_defects.Count - 1]);
            _history.Add(CreateSnapshot($"Defect registered: {defectType} (severity {severity:F1})"));
            Debug.Log($"[WaferDigitalTwin] Defect registered: {defectType} at ({position.x}, {position.y}, {position.z}) severity {severity:F1} (revision {_revision})");
        }

        /// <summary>
        /// Create a snapshot of the current wafer state.
        /// </summary>
        /// <returns>Wafer state snapshot</returns>
        public WaferStateSnapshot CreateSnapshot()
        {
            return CreateSnapshot("Snapshot at " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>
        /// Create a snapshot with a descriptive name.
        /// </summary>
        /// <param name="name">Snapshot name/description</param>
        /// <returns>Wafer state snapshot</returns>
        public WaferStateSnapshot CreateSnapshot(string name)
        {
            var snapshot = new WaferStateSnapshot
            {
                Name = name,
                Timestamp = DateTime.UtcNow,
                WaferId = _waferId,
                Revision = _revision,
                State = _waferState,
                SurfaceState = _surfaceState,
                LayerCount = _layerStack.Count,
                DefectCount = _defects.Count,
                Diameter = _diameter,
                Thickness = _thickness
            };

            // Capture layer and defect data at this point in time
            snapshot.LayerStack = new List<WaferLayer>(_layerStack);
            snapshot.Defects = new List<WaferDefect>(_defects);

            OnSnapshotCreated?.Invoke(snapshot);
            _history.Add(snapshot);
            Debug.Log($"[WaferDigitalTwin] Snapshot created: {name} (revision {_revision})");

            return snapshot;
        }

        /// <summary>
        /// Validate the current wafer state.
        /// </summary>
        /// <returns>Validation results</returns>
        public WaferStateValidation ValidateState()
        {
            var results = WaferStateValidation.Valid();

            // Check wafer ID
            if (string.IsNullOrEmpty(_waferId.WaferIdString))
            {
                results.Errors.Add("Wafer ID is empty");
            }
            if (string.IsNullOrEmpty(_waferId.LotId))
            {
                results.Errors.Add("Lot ID is empty");
            }

            // Check geometry
            if (_diameter <= 0)
            {
                results.Errors.Add($"Invalid diameter: {_diameter}");
            }
            if (_thickness <= 0)
            {
                results.Errors.Add($"Invalid thickness: {_thickness}");
            }

            // Check state
            if (_waferState == WaferState.Uninitialized)
            {
                results.Errors.Add("Wafer is in Uninitialized state");
            }

            // Check layer stack
            for (int i = 0; i < _layerStack.Count; i++)
            {
                var layer = _layerStack[i];
                if (string.IsNullOrEmpty(layer.Material))
                {
                    results.Errors.Add($"Layer {i} has no material");
                }
                if (layer.Thickness <= 0)
                {
                    results.Errors.Add($"Layer {i} has invalid thickness: {layer.Thickness}");
                }
            }

            // Check for duplicate layer IDs
            var layerIds = new List<int>();
            foreach (var layer in _layerStack)
            {
                if (layerIds.Contains(layer.LayerId))
                {
                    results.Errors.Add($"Duplicate layer ID: {layer.LayerId}");
                }
                layerIds.Add(layer.LayerId);
            }

            if (results.Errors.Count > 0)
            {
                results.IsValid = false;
            }

            return results;
        }

        // ===== EDITOR MENU ITEMS (development only) =====

#if UNITY_EDITOR
        [UnityEditor.MenuItem("SemiconductorDigitalTwin/M3 \u2192 Initialize Demo Wafer")]
        public static void InitializeDemoWafer()
        {
            var wafer = FindFirstObjectByType<WaferDigitalTwin>();
            if (wafer != null)
            {
                // Create a default definition
                var def = Resources.Load<DefaultWaferDefinition>("Wafers/DefaultWaferDefinition");
                if (def != null)
                {
                    wafer.Initialize(def);
                    Debug.Log("[WaferDigitalTwin] Demo wafer initialized via editor menu");
                }
                else
                {
                    Debug.LogWarning("[WaferDigitalTwin] DefaultWaferDefinition not found in Resources");
                }
            }
            else
            {
                Debug.LogWarning("[WaferDigitalTwin] No WaferDigitalTwin found in scene");
            }
        }
#endif
    }
}
