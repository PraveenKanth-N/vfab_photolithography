using UnityEngine;
using System;
using System.Collections.Generic;
using SemiconductorDigitalTwin.Wafers.Core;

namespace SemiconductorDigitalTwin.TimeTravel
{
    /// <summary>
    /// Module 8: Time-Travel State Debugger.
    /// Captures immutable snapshots of the wafer substrate after each unit fabrication process.
    /// Enables non-destructive timeline inspection and root-cause defect analysis.
    /// </summary>
    public class TimeTravelDebugger : MonoBehaviour
    {
        public static TimeTravelDebugger Instance { get; private set; }

        [Header("State History")]
        [SerializeField] private List<WaferStateSnapshot> _snapshotTimeline = new List<WaferStateSnapshot>();
        [SerializeField] private int _activeSnapshotIndex = -1;

        [Header("Target Wafer Twin")]
        [SerializeField] private WaferDigitalTwin _waferTwin;

        public event Action<int, WaferStateSnapshot> OnSnapshotSelected;
        public event Action<WaferStateSnapshot> OnSnapshotCaptured;

        public int TimelineCount => _snapshotTimeline.Count;
        public int ActiveIndex => _activeSnapshotIndex;
        public int SnapshotCount => _snapshotTimeline.Count;
        public int CurrentIndex => _activeSnapshotIndex;
        public WaferStateSnapshot CurrentSnapshot => (_activeSnapshotIndex >= 0 && _activeSnapshotIndex < _snapshotTimeline.Count) ? _snapshotTimeline[_activeSnapshotIndex] : null;

        public void ScrubToSnapshot(int index) => JumpToIndex(index);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (_waferTwin == null)
            {
                _waferTwin = FindAnyObjectByType<WaferDigitalTwin>();
            }
        }

        /// <summary>
        /// Captures the current authoritative state of the wafer into the immutable timeline.
        /// </summary>
        /// <param name="stageName">Name of the fabrication stage completed</param>
        public WaferStateSnapshot CaptureSnapshot(string stageName)
        {
            if (_waferTwin == null)
            {
                _waferTwin = FindAnyObjectByType<WaferDigitalTwin>();
                if (_waferTwin == null) return null;
            }

            WaferStateSnapshot snapshot = _waferTwin.CreateSnapshot();
            _snapshotTimeline.Add(snapshot);
            _activeSnapshotIndex = _snapshotTimeline.Count - 1;

            Debug.Log($"[TimeTravelDebugger] Snapshot recorded [{_activeSnapshotIndex}]: {stageName} (Defects: {snapshot.DefectCount})");
            OnSnapshotCaptured?.Invoke(snapshot);
            return snapshot;
        }

        /// <summary>
        /// Steps backward in the timeline to inspect the previous wafer condition.
        /// </summary>
        public bool StepBackward()
        {
            if (_activeSnapshotIndex > 0)
            {
                JumpToIndex(_activeSnapshotIndex - 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Steps forward in the timeline toward the latest processed state.
        /// </summary>
        public bool StepForward()
        {
            if (_activeSnapshotIndex < _snapshotTimeline.Count - 1)
            {
                JumpToIndex(_activeSnapshotIndex + 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Restores the wafer state to the specified historical index.
        /// </summary>
        public void JumpToIndex(int index)
        {
            if (index < 0 || index >= _snapshotTimeline.Count) return;

            _activeSnapshotIndex = index;
            WaferStateSnapshot target = _snapshotTimeline[index];

            if (_waferTwin != null)
            {
                _waferTwin.RestoreFromSnapshot(target);
            }

            Debug.Log($"[TimeTravelDebugger] Jumped to Timeline step [{index}] recorded at {target.Timestamp:HH:mm:ss}");
            OnSnapshotSelected?.Invoke(index, target);
        }

        /// <summary>
        /// Generates a diff summary comparing two snapshot stages.
        /// </summary>
        public string GetRunDiff(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _snapshotTimeline.Count || indexB < 0 || indexB >= _snapshotTimeline.Count)
                return "Invalid snapshot indices.";

            WaferStateSnapshot a = _snapshotTimeline[indexA];
            WaferStateSnapshot b = _snapshotTimeline[indexB];

            return $"=== RUN DIFF [{indexA} -> {indexB}] ===\n" +
                   $"Layers: {(a.LayerStack != null ? a.LayerStack.Count : 0)} -> {(b.LayerStack != null ? b.LayerStack.Count : 0)}\n" +
                   $"Thickness: {a.Thickness:F4} mm -> {b.Thickness:F4} mm\n" +
                   $"Defect Count: {a.DefectCount} -> {b.DefectCount}\n" +
                   $"New Flaws: {Mathf.Max(0, b.DefectCount - a.DefectCount)}";
        }
    }
}
