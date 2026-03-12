/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// The UI script for the terminal that requires the player to enter the cart total during the checkout process.
    /// Only activated when the customer pays by card.
    /// </summary>
    public class UICashDeskTerminal : Interactable
    {
        /// <summary>
        /// Whether this object is currently controlled by the player.
        /// </summary>
        public bool isPlayerControlled { get; private set; }

        /// Event fired when the amount of change is being confirmed by the player.
        /// The CashDesk checks the amount and then bills the customer. Total cart value.
        public event Action<string> onInputConfirmed;

        /// <summary>
        /// The initial position and rotation the player camera should move to when entering this object.
        /// </summary>
        public Transform lookTransform;

        /// <summary>
        /// The speed used when entering this object.
        /// </summary>
        public float lerpSpeed = 2f;

        /// <summary>
        /// Label for displaying the currency symbol before the cart total.
        /// </summary>
        public TMP_Text currency;

        /// <summary>
        /// Input field for entering (keyboard) or clicking numbers to match the cart total.
        /// </summary>
        public TMP_InputField input;

        //previous camera position that should be transitioned back to when leaving    
        private Vector3 prevCamPosition;
        //previous camera rotation that should be transitioned back to when leaving
        private Quaternion prevCamRotation;
        //reference to collider used for detecting an interaction
        private Collider col;
        //reference to image for colorization in case of incorrect input
        private Image inputBackground;


        //intitalize references
        void Awake()
        {
            col = GetComponent<Collider>();
            inputBackground = input.GetComponent<Image>();

            input.onValidateInput = OnValidateInput;
        }


        //initialize variables
        void Start()
        {
            currency.text = StoreDatabase.cultureInfo.NumberFormat.CurrencySymbol;
        }


        /// <summary>
        /// Toggles the collider for the InteractionSystem raycast.
        /// </summary>
        public void SetInteractable(bool state)
        {
            col.enabled = state;
        }


        /// <summary>
        /// Returns whether the collider is enabled to determine the terminal state.
        /// </summary>
        public bool IsInteractable()
        {
            return col.enabled;
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            UIGame.AddAction("LeftClick", "Use", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// This object can be "controlled" by the player.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;

            PlayerInput.GetPlayerByIndex(0).onActionTriggered += OnAction;
            UIGame.AddAction("Enter", "Confirm", true);

            input.interactable = true;
            input.text = string.Empty;
            FocusInputField();

            isPlayerControlled = true;
            PlayerController.SetMovementState(MovementState.None, false);
            SetInteractable(false);

            Transform camTransform = PlayerController.GetCameraTransform();
            prevCamPosition = camTransform.localPosition;
            prevCamRotation = camTransform.localRotation;

            InteractionSystem.MoveToTargetLinear(camTransform, null, lookTransform.position, Quaternion.LookRotation(lookTransform.forward), lerpSpeed, false);
            return true;
        }


        /// <summary>
        /// Interactable override, removing UI action.
        /// </summary>
        public override void OnLostFocus()
        {
            UIGame.RemoveAction("LeftClick");
        }


        /// <summary>
        /// Pass in a single character that is validated before adding it to the input field.
        /// </summary>
        public void AddCharacter(string character)
        {
            if (!isPlayerControlled) return;

            input.text += OnValidateInput(input.text, input.text.Length, char.Parse(character));
            input.caretPosition = input.text.Length;
        }


        /// <summary>
        /// Remove the last character from the input field.
        /// </summary>
        public void RemoveCharacter()
        {
            if (!isPlayerControlled || input.text.Length == 0)
                return;

            inputBackground.color = Color.white;
            input.text = input.text.Substring(0, input.text.Length - 1);
        }

        /// <summary>
        /// Try to confirm customer payment by sending the input field value to CashDesk.
        /// CashDesk might refuse the transaction if over/undercharging is disallowed.
        /// </summary>
        public void Confirm()
        {
            if (!isPlayerControlled) return;
            
            FocusInputField();
            onInputConfirmed?.Invoke(input.text);
        }


        /// <summary>
        /// Cancel controlling this object and return to CashDesk.
        /// </summary>
        public void Exit(bool exitOnly = false)
        {
            PlayerInput.GetPlayerByIndex(0).onActionTriggered -= OnAction;
            UIGame.RemoveAction("Enter");

            isPlayerControlled = false;
            input.interactable = false;
            input.text = string.Empty;
            Invoke("ReEnable", 0.5f);

            if (exitOnly)
            {
                Transform camTransform = PlayerController.GetCameraTransform();
                InteractionSystem.MoveToTargetLinear(camTransform, null, prevCamPosition, prevCamRotation, lerpSpeed, true);
            }
        }


        //after exiting the controlled state, player movement is re-enabled with a short delay
        private void ReEnable()
        {
            PlayerController.SetMovementState(MovementState.RotationOnly, true);
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                switch(context.action.name)
                {
                    case "Enter":
                        Confirm();
                        break;
                }
            }
        }


        private void FocusInputField()
        {
            #if !UNITY_ANDROID && !UNITY_IOS
                input.ActivateInputField();
            #endif
        }


        //similar to the validation in UIPriceLabelWindow
        private char OnValidateInput(string text, int charIndex, char addedChar)
        {
            FocusInputField();

            if (char.IsDigit(addedChar) || addedChar == ',' || addedChar == '.')
            {
                if (addedChar == ',') addedChar = '.';
                if (addedChar == '.' && text.Contains('.'))
                    return '\0';

                int firstDot = text.IndexOf('.');
                if (firstDot > 0 && text.Length > firstDot + 2)
                    return '\0';

                inputBackground.color = Color.white;
                return addedChar;
            }
            
            return '\0';
        }
    }
}
