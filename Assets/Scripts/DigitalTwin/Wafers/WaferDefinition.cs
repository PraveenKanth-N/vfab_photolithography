using UnityEngine;
using System;
using System.Collections.Generic;
using SemiconductorDigitalTwin.Wafers.ScriptableObjects;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Wafer definition asset data.
    /// Contains initial wafer properties used when creating new wafers.
    /// ScriptableObject-readable.
    /// </summary>
    [Serializable]
    public class WaferDefinition
    {
        /// <summary>
        /// Wafer diameter in millimeters
        /// </summary>
        public float Diameter = 300f;

        /// <summary>
        /// Wafer thickness in millimeters
        /// </summary>
        public float Thickness = 0.775f;

        /// <summary>
        /// Substrate material identifier
        /// </summary>
        public string SubstrateMaterial = "Silicon";

        /// <summary>
        /// Crystal orientation
        /// </summary>
        public string CrystalOrientation = "100";

        /// <summary>
        /// Initial wafer state
        /// </summary>
        public WaferState InitialState = WaferState.Initialized;

        /// <summary>
        /// Initial surface state
        /// </summary>
        public string InitialSurfaceState = "Clean";

        /// <summary>
        /// Initial layer count
        /// </summary>
        public int InitialLayerCount = 1;

        /// <summary>
        /// Create a wafer definition from the ScriptableObject
        /// </summary>
        /// <param>from Definition ScriptableObject</param>
        public static WaferDefinition FromScriptableObject(DefaultWaferDefinition def)
        {
            var result = new WaferDefinition
            {
                Diameter = def.Diameter,
                Thickness = def.Thickness,
                SubstrateMaterial = def.SubstrateMaterial,
                CrystalOrientation = def.CrystalOrientation,
                InitialState = def.InitialState,
                InitialSurfaceState = def.InitialSurfaceState,
                InitialLayerCount = def.InitialLayerCount
            };
            return result;
        }
    }
}