using UnityEngine;
using UnityEngine.UI;

namespace SemiconductorDigitalTwin.TimeTravel
{
    /// <summary>
    /// User interface overlay for scrubbing through historical wafer states.
    /// Supports both desktop mouse controls and VR ray/touch interaction.
    /// </summary>
    public class TimeTravelUI : MonoBehaviour
    {
        [Header("UI References")]
        public Slider timelineSlider;
        public Button prevButton;
        public Button nextButton;
        public Text stageLabelText;
        public Text defectCountText;
        public Text timestampText;
        public GameObject panelRoot;

        private void Start()
        {
            if (prevButton != null)
                prevButton.onClick.AddListener(OnPrevClicked);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);

            if (timelineSlider != null)
                timelineSlider.onValueChanged.AddListener(OnSliderChanged);

            if (TimeTravelDebugger.Instance != null)
            {
                TimeTravelDebugger.Instance.OnSnapshotCaptured += (snapshot) => UpdateUI();
                TimeTravelDebugger.Instance.OnSnapshotSelected += (index, snapshot) => UpdateUI();
            }

            UpdateUI();
        }

        private void OnPrevClicked()
        {
            if (TimeTravelDebugger.Instance != null)
            {
                TimeTravelDebugger.Instance.StepBackward();
                UpdateUI();
            }
        }

        private void OnNextClicked()
        {
            if (TimeTravelDebugger.Instance != null)
            {
                TimeTravelDebugger.Instance.StepForward();
                UpdateUI();
            }
        }

        private void OnSliderChanged(float val)
        {
            if (TimeTravelDebugger.Instance != null)
            {
                int index = Mathf.RoundToInt(val);
                TimeTravelDebugger.Instance.ScrubToSnapshot(index);
            }
        }

        public void UpdateUI()
        {
            if (TimeTravelDebugger.Instance == null) return;

            int count = TimeTravelDebugger.Instance.SnapshotCount;
            int current = TimeTravelDebugger.Instance.CurrentIndex;

            if (timelineSlider != null)
            {
                timelineSlider.minValue = 0;
                timelineSlider.maxValue = Mathf.Max(0, count - 1);
                timelineSlider.SetValueWithoutNotify(current);
            }

            if (prevButton != null) prevButton.interactable = current > 0;
            if (nextButton != null) nextButton.interactable = current < count - 1;

            var snapshot = TimeTravelDebugger.Instance.CurrentSnapshot;
            if (snapshot != null)
            {
                if (stageLabelText != null) stageLabelText.text = $"Stage: {snapshot.ProcessStep} (Rev {snapshot.Revision})";
                if (defectCountText != null) defectCountText.text = $"Defects: {snapshot.DefectCount}";
                if (timestampText != null) timestampText.text = $"Time: {snapshot.Timestamp:HH:mm:ss}";
            }
            else
            {
                if (stageLabelText != null) stageLabelText.text = "No snapshots captured";
                if (defectCountText != null) defectCountText.text = "Defects: 0";
            }
        }
    }
}
