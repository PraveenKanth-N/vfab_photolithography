using UnityEngine;

namespace SemiconductorDigitalTwin.Core
{
    /// <summary>
    /// Interface for onboarding services that future modules (like M4) can subscribe to.
    /// Defines the OnboardingCompleted event contract.
    /// </summary>
    public interface IOnboardingServices
    {
        /// <summary>
        /// Raised when onboarding completes.
        /// M4 will subscribe to this event to transition into the workflow.
        /// </summary>
        event System.Action OnboardingCompleted;

        /// <summary>
        /// Check if onboarding has been completed (via PlayerPrefs).
        /// </summary>
        bool IsOnboardingCompleted { get; }

        /// <summary>
        /// Reset onboarding state (for development/testing).
        /// </summary>
        void ResetOnboarding();
    }
}