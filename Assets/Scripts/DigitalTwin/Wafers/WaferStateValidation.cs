using System;
using System.Collections.Generic;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Results of a wafer state validation check.
    /// Returned by WaferDigitalTwin.ValidateState().
    /// </summary>
    [Serializable]
    public class WaferStateValidation
    {
        /// <summary>True if the wafer state passes all validation checks.</summary>
        public bool IsValid = true;

        /// <summary>List of error messages. Empty when IsValid is true.</summary>
        public List<string> Errors = new List<string>();

        /// <summary>List of warning messages (non-fatal issues).</summary>
        public List<string> Warnings = new List<string>();

        /// <summary>
        /// Create a valid (passing) validation result.
        /// </summary>
        public static WaferStateValidation Valid()
        {
            return new WaferStateValidation { IsValid = true };
        }

        /// <summary>
        /// Create a failed validation result with a single error.
        /// </summary>
        public static WaferStateValidation Failed(string error)
        {
            return new WaferStateValidation
            {
                IsValid = false,
                Errors = new List<string> { error }
            };
        }

        public override string ToString()
        {
            if (IsValid) return "VALID";
            return $"INVALID: {string.Join("; ", Errors)}";
        }
    }
}
