using UnityEngine;
using System;

namespace SemiconductorDigitalTwin.Wafers.Core
{
    /// <summary>
    /// Wafer identity model.
    /// Supports multiple wafer instances in the future.
    /// </summary>
    [Serializable]
    public class WaferId
    {
        [Tooltip("Unique wafer identifier")]
        [SerializeField] private string _waferId = Guid.NewGuid().ToString();

        [Tooltip("Lot or batch identifier")]
        [SerializeField] private string _lotId = Guid.NewGuid().ToString();

        [Tooltip("Wafer number within the lot")]
        [SerializeField] private int _waferNumber = 1;

        [Tooltip("Creation/session identifier")]
        [SerializeField] private string _sessionIdentifier = Guid.NewGuid().ToString();

        [Tooltip("Creation timestamp")]
        [SerializeField] private DateTime _creationTimestamp = DateTime.UtcNow;

        // ===== PUBLIC PROPERTIES =====

        /// <summary>
        /// Unique wafer identifier
        /// </summary>
        public string WaferIdString => _waferId;

        /// <summary>
        /// Lot or batch identifier
        /// </summary>
        public string LotId => _lotId;

        /// <summary>
        /// Wafer number within the lot
        /// </summary>
        public int WaferNumber => _waferNumber;

        /// <summary>
        /// Creation/session identifier
        /// </summary>
        public string SessionIdentifier => _sessionIdentifier;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreationTimestamp => _creationTimestamp;

        /// <summary>
        /// Human-readable identifier string
        /// </summary>
        public override string ToString() => $"Wafer {_waferNumber} (Lot {_lotId})";

        // ===== EQUALS / GET HASH CODE =====

        public override bool Equals(object obj)
        {
            if (obj is WaferId other)
            {
                return _waferId == other._waferId && _lotId == other._lotId;
            }
            return false;
        }

        public override int GetHashCode() => (_waferId + _lotId).GetHashCode();

        // ===== OPERATORS =====

        public static bool operator ==(WaferId a, WaferId b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a._waferId == b._waferId && a._lotId == b._lotId;
        }

        public static bool operator !=(WaferId a, WaferId b) => !(a == b);
    }
}