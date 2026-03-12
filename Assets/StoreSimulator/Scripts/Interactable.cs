/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Base class describing methods that should be implemented for an object that can be interacted with.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        /// <summary>
        /// Can be overridden to define whether an interaction is allowed while a PlacementMode is not inactive.
        /// </summary>
        public virtual bool ShouldSkipSystemChecks() { return false; }

        /// <summary>
        /// Can be overridden to define what happens when entering the bounds of the Interactable object.
        /// </summary>
        public virtual void OnBecameFocus() { }

        /// <summary>
        /// Minimum implementation method to define what should be done when interacting with this object.
        /// The action can be further defined by checking the input action that was used.
        /// The boolean value returned describes whether the action was executed or not.
        /// </summary>
        public virtual bool Interact(string actionName) { return false; }

        /// <summary>
        /// Can be overridden to define what happens when leaving the bounds of the Interactable object.
        /// </summary>
        public virtual void OnLostFocus() { }
    }
}