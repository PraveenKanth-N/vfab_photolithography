using UnityEngine;
using System;
using System.Collections.Generic;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Snapshot of wafer state at a point in time.
    /// Used for history/traceability.
    /// M8 will build the time-travel system on top of this foundation.
    /// </summary>
    [Serializable]
    public class WaferStateSnapshot
    {
        /// <summary>
        /// Snapshot name/description
        /// </summary>
        public string Name;

        /// <summary>Alias for process stage description</summary>
        public string ProcessStep { get => Name; set => Name = value; }

        /// <summary>
        /// Timestamp when the snapshot was created
        /// </summary>
        public DateTime Timestamp;

        /// <summary>
        /// Wafer identity at the time of the snapshot
        /// </summary>
        public WaferId WaferId;

        /// <summary>
        /// Revision number at the time of the snapshot
        /// </summary>
        public int Revision;

        /// <summary>
        /// Wafer state at the time of the snapshot
        /// </summary>
        public WaferState State;

        /// <summary>
        /// Surface state at the time of the snapshot
        /// </summary>
        public string SurfaceState;

        /// <summary>
        /// Layer count at the time of the snapshot
        /// </summary>
        public int LayerCount;

        /// <summary>
        /// Defect count at the time of the snapshot
        /// </summary>
        public int DefectCount;

        /// <summary>
        /// Wafer diameter at the time of the snapshot
        /// </summary>
        public float Diameter;

        /// <summary>
        /// Wafer thickness at the time of the snapshot
        /// </summary>
        public float Thickness;

        /// <summary>
        /// Layer stack data at the time of the snapshot (deep copy)
        /// </summary>
        public List<WaferLayer> LayerStack;

        /// <summary>
        /// Defects at the time of the snapshot (deep copy)
        /// </summary>
        public List<WaferDefect> Defects;
    }
}