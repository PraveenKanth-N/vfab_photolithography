using UnityEngine;
using System;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Represents a single layer in the wafer's layer stack.
    /// </summary>
    [Serializable]
    public class WaferLayer
    {
        /// <summary>
        /// Layer identifier (0 = substrate, 1, 2, ... = overlying layers)
        /// </summary>
        public int LayerId;

        /// <summary>
        /// Material identifier
        /// </summary>
        public string Material;

        /// <summary>
        /// Layer thickness in millimeters
        /// </summary>
        public float Thickness;

        /// <summary>
        /// Layer position/order in the stack
        /// </summary>
        public int Position;

        /// <summary>
        /// Human-readable representation
        /// </summary>
        public override string ToString() => $"Layer {LayerId}: {Material} ({Thickness}mm)";

        /// <summary>
        /// Check if this is the substrate layer
        /// </summary>
        public bool IsSubstrate => LayerId == 0;
    }
}