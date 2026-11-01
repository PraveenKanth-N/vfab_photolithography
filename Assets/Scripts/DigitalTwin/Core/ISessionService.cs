using UnityEngine;

namespace SemiconductorDigitalTwin.Core
{
    /// <summary>
    /// Session service providing extension points for future workflow integration.
    /// M1 establishes the session contract but does not implement fabrication workflow.
    /// </summary>
    public interface ISessionService
    {
        /// <summary>
        /// Raised when onboarding completes - M4 will subscribe to this event.
        /// </summary>
        event System.Action OnboardingCompleted;

        /// <summary>
        /// Entry point for the fabrication workflow. M4 will call this.
        /// </summary>
        void WorkflowEntryReady();

        /// <summary>
        /// Sets the current workflow phase. M4 will call SetPhase(InWorkflow).
        /// </summary>
        void SetPhase(string phase);

        /// <summary>
        /// Current workflow phase status.
        /// </summary>
        string CurrentPhase { get; }
    }
}