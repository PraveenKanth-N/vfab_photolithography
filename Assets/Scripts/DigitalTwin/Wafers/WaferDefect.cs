using UnityEngine;
using System;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Represents a defect on the wafer surface.
    /// Lightweight data structure; does not include full visualization.
    /// M7 owns detailed defect visualization.
    /// </summary>
    [Serializable]
    public class WaferDefect
    {
        /// <summary>
        /// Unique defect identifier
        /// </summary>
        public string DefectId;

        /// <summary>
        /// Type/description of the defect
        /// </summary>
        public string Type;

        /// <summary>
        /// Position on wafer surface in millimeters (x, y, z coordinates relative to wafer center)
        /// </summary>
        public (float x, float y, float z) Position;

        /// <summary>
        /// Severity level (0-1, where 1 is most severe)
        /// </summary>
        [Range(0f, 1f)] public float Severity;

        /// <summary>
        /// Timestamp when the defect was detected/registered
        /// </summary>
        public DateTime DetectedTimestamp;

        /// <summary>
        /// Source/process that caused the defect (optional)
        /// </summary>
        public string Source;

        /// <summary>
        /// Human-readable representation
        /// </summary>
        public override string ToString() => $"Defect: {Type} at ({Position.x}, {Position.y}, {Position.z}) severity {Severity:F1}";

        /// <summary>
        /// Check if the defect is considered critical
        /// </summary>
        public bool IsCritical => Severity >= 0.75f;
    }
}