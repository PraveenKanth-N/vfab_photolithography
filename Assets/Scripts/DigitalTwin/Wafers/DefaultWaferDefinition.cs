using UnityEngine;
using SemiconductorDigitalTwin.Wafers.Core;

namespace SemiconductorDigitalTwin.Wafers.ScriptableObjects
{
    /// <summary>
    /// Default wafer definition ScriptableObject.
    /// Contains initial wafer properties used when creating new wafers.
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultWaferDefinition", menuName = "SemiconductorDigitalTwin/Wafers/Default Definition")]
    public class DefaultWaferDefinition : ScriptableObject
    {
        [Header("Wafer Geometry")]
        [Tooltip("Wafer diameter in millimeters")]
        [SerializeField] private float _diameter = 300f;

        [Tooltip("Wafer thickness in millimeters")]
        [SerializeField] private float _thickness = 0.775f;

        [Tooltip("Substrate material identifier")]
        [SerializeField] private string _substrateMaterial = "Silicon";

        [Tooltip("Crystal orientation")]
        [SerializeField] private string _crystalOrientation = "100";

        [Header("Initial State")]
        [Tooltip("Initial wafer state")]
        [SerializeField] private WaferState _initialState = WaferState.Initialized;

        [Tooltip("Initial surface state")]
        [SerializeField] private string _initialSurfaceState = "Clean";

        [Tooltip("Initial layer count")]
        [SerializeField] private int _initialLayerCount = 1;

        // ===== PUBLIC PROPERTIES =====

        /// <summary>Wafer diameter in millimeters</summary>
        public float Diameter => _diameter;

        /// <summary>Wafer thickness in millimeters</summary>
        public float Thickness => _thickness;

        /// <summary>Substrate material identifier</summary>
        public string SubstrateMaterial => _substrateMaterial;

        /// <summary>Crystal orientation</summary>
        public string CrystalOrientation => _crystalOrientation;

        /// <summary>Initial wafer state</summary>
        public WaferState InitialState => _initialState;

        /// <summary>Initial surface state</summary>
        public string InitialSurfaceState => _initialSurfaceState;

        /// <summary>Initial layer count</summary>
        public int InitialLayerCount => _initialLayerCount;
    }
}
