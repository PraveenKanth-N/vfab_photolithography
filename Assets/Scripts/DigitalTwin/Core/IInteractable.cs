using UnityEngine;

namespace SemiconductorDigitalTwin.Core
{
    /// <summary>
    /// Interface defining the contract for interactable objects in the VR environment.
    /// Future modules (M2-M10) will implement or extend this interface.
    /// M1 provides the basic implementation via DemonstrationConsole.
    /// </summary>
    /// <remarks>
    /// Does NOT include IsIndicatorOn() - that is specific to DemonstrationConsole,
    /// not a requirement for all future interactables.
    /// </remarks>
    public interface IInteractable
    {
        /// <summary>
        /// Called when the user starts aiming at this object.
        /// </summary>
        void OnAim();

        /// <summary>
        /// Called when the user stops aiming at this object.
        /// </summary>
        void OnStopAim();

        /// <summary>
        /// Called when the user interacts with this object.
        /// </summary>
        void OnInteract();

        /// <summary>
        /// Reset the object's state for re-testing.
        /// </summary>
        void ResetState();
    }
}