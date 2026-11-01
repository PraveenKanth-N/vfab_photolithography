using UnityEngine;
using System;
using System.Collections.Generic;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Data container for current wafer state.
    /// JSON-friendly serialization structure.
    /// Use GetState() on WaferDigitalTwin to populate this.
    /// </summary>
    [Serializable]
    public class WaferStateData
    {
        /// <summary>Wafer identity</summary>
        public WaferId WaferId;

        /// <summary>Current wafer state enum</summary>
        public WaferState State;

        /// <summary>Current revision number</summary>
        public int Revision;

        /// <summary>Wafer diameter in millimeters</summary>
        public float Diameter;

        /// <summary>Wafer thickness in millimeters</summary>
        public float Thickness;

        /// <summary>Substrate material</summary>
        public string SubstrateMaterial;

        /// <summary>Crystal orientation</summary>
        public string CrystalOrientation;

        /// <summary>Surface state</summary>
        public string SurfaceState;

        /// <summary>Layer count</summary>
        public int LayerCount;

        /// <summary>Full layer stack data</summary>
        public List<WaferLayer> LayerStack;

        /// <summary>Defect count</summary>
        public int DefectCount;

        /// <summary>Full defects list</summary>
        public List<WaferDefect> Defects;

        /// <summary>Timestamp of this state data</summary>
        public DateTime Timestamp;
    }
}