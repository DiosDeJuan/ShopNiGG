/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI button for displaying and invoking UI actions on mobile devices.
    /// These buttons effectively replace the UIActionHint component on mobile platforms.
    /// </summary>
    public class UIActionButton : MonoBehaviour
    {
        /// <summary>
        /// Reference to the control(s) that should be invoked.
        /// </summary>
        public InputActionReference[] actions;


        /// <summary>
        /// Label for displaying the action description.
        /// </summary>
        public TMP_Text actionText;


        /// <summary>
        /// Initialize with action description.
        /// This also activates the button game object.
        /// </summary>
        public void Initialize(string actionText)
        {
            this.actionText.text = actionText;
            gameObject.SetActive(true);
        }


        /// <summary>
        /// Custom implementation of On-Screen Button, since the Unity Input System
        /// cannot handle both On-Screen Button and PlayerInput components at the same time.
        /// </summary>
        public void OnActionPressed()
        {
            for(int i = 0; i < actions.Length; i++)
            {
                InputControl control = actions[i].action.controls[0];
                InputEventPtr eventPtr = null;

                //button pressed
                var rawEvent = StateEvent.From(control.device, out eventPtr);
                control.WriteValueIntoEvent(1f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
                //button released
                control.WriteValueIntoEvent(0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }

            InputSystem.Update();
        }
    }
}
