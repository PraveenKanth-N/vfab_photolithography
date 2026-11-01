using UnityEngine;
using System;
using System.Collections.Generic;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Wafer lifecycle state enumeration.
    /// Only includes states actually needed by M3.
    /// M4/M6/M8 may later extend this.
    /// </summary>
    [Serializable]
    public enum WaferState
    {
        /// <summary>
        /// Wafer has been created but not yet initialized
        /// </summary>
        Uninitialized,

        /// <summary>
        /// Wafer has been initialized and is ready for processing
        /// </summary>
        Initialized,

        /// <summary>
        /// Wafer is ready for fabrication processes
        /// </summary>
        Ready,

        /// <summary>
        /// Wafer is currently under a fabrication process
        /// </summary>
        Processing,

        /// <summary>
        /// Wafer is undergoing inspection
        /// </summary>
        Inspection,

        /// <summary>
        /// Wafer fabrication is complete
        /// </summary>
        Completed,

        /// <summary>
        /// Wafer is archived (no longer in active use)
        /// </summary>
        Archived
    }
}