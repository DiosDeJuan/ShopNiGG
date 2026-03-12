/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace FLOBUK.StoreSimulator
{   
    /// <summary>
    /// Shop opened on a device allowing to purchase various available goods in order to replenish or extend the store.
    /// </summary>
    public class UIShopDesktop : Interactable
    {
        /// <summary>
        /// The initial position and rotation the player camera should move to when entering this object.
        /// </summary>
        public Transform lookTransform;

        /// <summary>
        /// The speed used when entering this object.
        /// </summary>
        public float lerpSpeed = 2f;

        /// <summary>
        /// Label for displaying the amount of player currency that can be spent.
        /// </summary>
        public TMP_Text moneyDisplay;

        /// <summary>
        /// Label for displaying the experience level.
        /// </summary>
        public TMP_Text levelDisplay;

        /// <summary>
        /// Label for displaying the number of the day played.
        /// </summary>
        public TMP_Text dayDisplay;

        /// <summary>
        /// Label for displaying the current formatted time string.
        /// </summary>
        public TMP_Text timeDisplay;

        /// <summary>
        /// Input field allowing players to change the name of the store.
        /// </summary>
        public TMP_InputField storeNameInput;

        /// <summary>
        /// Clip to play whenever a button has been clicked, or none if not set. 
        /// </summary>
        public AudioClip clickClip;

        //previous camera position that should be transitioned back to when leaving
        private Vector3 prevCamPosition;
        //previous camera rotation that should be transitioned back to when leaving
        private Quaternion prevCamRotation;
        //reference to collider used for detecting an interaction
        private Collider col;


        //initialize references
        void Awake()
        {
            col = GetComponent<Collider>();

            StoreDatabase.onMoneyUpdate += OnMoneyUpdate;
            StoreDatabase.onLevelUpdate += OnLevelUpdate;
            DayCycleSystem.onTimeUpdate += OnTimeUpdate;
        }


        //initialize variables
        IEnumerator Start()
        {
            OnMoneyUpdate(StoreDatabase.GetMoneyString(), string.Empty);
            OnTimeUpdate(DayCycleSystem.GetTimeString());
            levelDisplay.text = StoreDatabase.GetLevelString();
            dayDisplay.text = DayCycleSystem.GetDayString();
            storeNameInput.text = StoreDatabase.GetStoreName();
            storeNameInput.onEndEdit.AddListener((x) => StoreDatabase.SetStoreName(x));

            yield return new WaitForSeconds(1);
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for(int i = 0; i < buttons.Length; i++)
                buttons[i].onClick.AddListener(() => AudioSystem.Play2D(clickClip));
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
            UIGame.AddAction("Esc", "Exit");
            
            UIGame.Instance.SetVisible(false);
            PlayerController.SetMovementState(MovementState.None, false);
            col.enabled = false;

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
        /// Cancel controlling this object and return to player movement mode.
        /// </summary>
        public void Exit()
        {
            PlayerInput.GetPlayerByIndex(0).onActionTriggered -= OnAction;
            UIGame.RemoveAction("Esc");

            Transform camTransform = PlayerController.GetCameraTransform();
            InteractionSystem.MoveToTargetLinear(camTransform, null, prevCamPosition, prevCamRotation, lerpSpeed, true);

            Invoke("ReEnable", 0.5f);
        }


        //after exiting the controlled state, player movement is re-enabled with a short delay
        private void ReEnable()
        {
            PlayerController.SetMovementState(MovementState.All, true);
            col.enabled = true;

            UIGame.Instance.SetVisible(true);
        }


        //subscribed to money change
        private void OnMoneyUpdate(string money, string change)
        {
            moneyDisplay.text = money;
        }


        //subscribed to level change
        //use the pre-formatted string instead of value only
        private void OnLevelUpdate(int level)
        {
            levelDisplay.text = StoreDatabase.GetLevelString();
        }


        //subscribed to time change
        private void OnTimeUpdate(string time)
        {
            timeDisplay.text = time;
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                switch(context.action.name)
                {
                    case "Cancel":
                        Exit();
                        break;
                }
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StoreDatabase.onMoneyUpdate -= OnMoneyUpdate;
            DayCycleSystem.onTimeUpdate -= OnTimeUpdate;
        }
    }
}
