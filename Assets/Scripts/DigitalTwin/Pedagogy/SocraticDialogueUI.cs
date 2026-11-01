using UnityEngine;
using UnityEngine.UI;

namespace SemiconductorDigitalTwin.Pedagogy
{
    /// <summary>
    /// User interface manager for Socratic AI tutoring dialogues.
    /// Can render as ScreenSpaceOverlay HUD or world-space VR canvas.
    /// </summary>
    public class SocraticDialogueUI : MonoBehaviour
    {
        [Header("UI Canvas Components")]
        public GameObject dialogueRootPanel;
        public Text tierHeaderText;
        public Text promptBodyText;
        public Text feedbackText;
        public Slider masteryProgressBar;
        public Text masteryPercentageText;

        [Header("Option Buttons")]
        public Button[] optionButtons;
        public Text[] optionButtonTexts;
        public Button continueButton;

        private SocraticAITutor.SocraticInquiry _activeInquiry;
        private int _currentTier = 1;

        private void Start()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }

            if (SocraticAITutor.Instance != null)
            {
                SocraticAITutor.Instance.OnInquiryStarted += DisplayInquiryTier;
                SocraticAITutor.Instance.OnAnswerEvaluated += DisplayFeedback;
                SocraticAITutor.Instance.OnMasteryUpdated += UpdateMasteryBar;
            }

            if (dialogueRootPanel != null)
                dialogueRootPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (SocraticAITutor.Instance != null)
            {
                SocraticAITutor.Instance.OnInquiryStarted -= DisplayInquiryTier;
                SocraticAITutor.Instance.OnAnswerEvaluated -= DisplayFeedback;
                SocraticAITutor.Instance.OnMasteryUpdated -= UpdateMasteryBar;
            }
        }

        public void DisplayInquiryTier(SocraticAITutor.SocraticInquiry inquiry, int tier)
        {
            _activeInquiry = inquiry;
            _currentTier = tier;

            if (dialogueRootPanel != null)
                dialogueRootPanel.SetActive(true);

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);

            string title = "";
            string prompt = "";
            string[] options = null;

            switch (tier)
            {
                case 1:
                    title = $"[Tier 1: Physical Observation] - Defect: {inquiry.defectKey}";
                    prompt = inquiry.tier1ObservationPrompt;
                    options = null;
                    if (continueButton != null) continueButton.gameObject.SetActive(true);
                    break;
                case 2:
                    title = $"[Tier 2: Phenomenological Deduction] - {inquiry.conceptKey}";
                    prompt = inquiry.tier2MechanismPrompt;
                    options = inquiry.tier2Options;
                    break;
                case 3:
                    title = $"[Tier 3: Recipe Remediation] - Corrective Action";
                    prompt = inquiry.tier3RemediationPrompt;
                    options = inquiry.tier3Options;
                    break;
            }

            if (tierHeaderText != null) tierHeaderText.text = title;
            if (promptBodyText != null) promptBodyText.text = prompt;

            // Populate option buttons
            if (options != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    if (i < options.Length && optionButtons[i] != null)
                    {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        if (optionButtonTexts != null && i < optionButtonTexts.Length && optionButtonTexts[i] != null)
                        {
                            optionButtonTexts[i].text = $"{(char)('A' + i)}) {options[i]}";
                        }
                    }
                    else if (optionButtons[i] != null)
                    {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnOptionSelected(int optionIndex)
        {
            if (SocraticAITutor.Instance != null && _activeInquiry != null)
            {
                // Disable option buttons after selection
                foreach (var btn in optionButtons)
                {
                    if (btn != null) btn.interactable = false;
                }

                SocraticAITutor.Instance.SubmitAnswer(optionIndex);
            }
        }

        public void DisplayFeedback(bool correct, string explanation)
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(true);
                feedbackText.color = correct ? new Color(0.2f, 0.9f, 0.3f) : new Color(1.0f, 0.3f, 0.3f);
                feedbackText.text = (correct ? "[CORRECT] " : "[INCORRECT] ") + explanation;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
        }

        private void OnContinueClicked()
        {
            if (_currentTier < 3)
            {
                DisplayInquiryTier(_activeInquiry, _currentTier + 1);
            }
            else
            {
                // Inquiry finished
                if (dialogueRootPanel != null)
                    dialogueRootPanel.SetActive(false);
            }
        }

        public void UpdateMasteryBar(string concept, float mastery)
        {
            if (masteryProgressBar != null)
            {
                masteryProgressBar.value = mastery;
            }
            if (masteryPercentageText != null)
            {
                masteryPercentageText.text = $"{concept} Mastery: {(mastery * 100f):F0}%";
            }
        }
    }
}
