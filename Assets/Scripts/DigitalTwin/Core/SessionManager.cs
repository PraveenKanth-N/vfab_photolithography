using UnityEngine;

namespace SemiconductorDigitalTwin.Core
{
    /// <summary>
    /// Lightweight session manager providing extension points for future M4 workflow integration.
    /// Establishes the session contract without implementing fabrication logic.
    /// Persists onboarding completion via PlayerPrefs key SDT.M1.OnboardingCompleted.
    /// </summary>
    public class SessionManager : MonoBehaviour, ISessionService
    {
        private string _currentPhase = "Idle";
        private bool _onboardingCompleted = false;

        // The event - must be named OnboardingCompleted to match ISessionService contract
        public event System.Action OnboardingCompleted;

        public string CurrentPhase => _currentPhase;

        private void Awake()
        {
            // Persistent session object
            DontDestroyOnLoad(gameObject);

            // Restore onboarding state from PlayerPrefs
            string savedState = PlayerPrefs.GetString("SDT.M1.OnboardingCompleted", "0");
            if (savedState == "1")
            {
                _onboardingCompleted = true;
            }
        }

        /// <summary>
        /// Raised when onboarding completes. M4 will subscribe to this event.
        /// Only raised once per onboarding completion cycle.
        /// </summary>
        public void RaiseOnboardingCompleted()
        {
            if (!_onboardingCompleted)
            {
                _onboardingCompleted = true;
                PlayerPrefs.SetString("SDT.M1.OnboardingCompleted", "1");
                PlayerPrefs.Save();
                OnboardingCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Entry point for the fabrication workflow. M4 will call this.
        /// </summary>
        public void WorkflowEntryReady()
        {
            _currentPhase = "InWorkflow";
            Debug.Log($"[SessionManager] Workflow entry ready. Phase: {_currentPhase}");
        }

        /// <summary>
        /// Sets the current workflow phase. M4 will call SetPhase(InWorkflow).
        /// </summary>
        public void SetPhase(string phase)
        {
            _currentPhase = phase;
            Debug.Log($"[SessionManager] Phase set to: {_currentPhase}");
        }

        /// <summary>
        /// Resets onboarding state for testing purposes.
        /// </summary>
        public void ResetOnboarding()
        {
            _onboardingCompleted = false;
            PlayerPrefs.SetString("SDT.M1.OnboardingCompleted", "0");
            PlayerPrefs.Save();
            Debug.Log("[SessionManager] Onboarding state reset.");
        }
    }
}