/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// The UI for displaying cash change that is being handed out to customers during the checkout process.
    /// Only activated when the customer pays with cash.
    /// </summary>
    public class UICashDeskRegister : MonoBehaviour
    {
        /// <summary>
        /// Event fired when the amount of change is being confirmed by the player.
        /// The CashDesk checks the amount and then bills the customer. Total cart value.
        /// </summary>
        public event Action<string> onInputConfirmed;

        /// <summary>
        /// Label for displaying the amount received from a customer.
        /// </summary>
        public TMP_Text received;

        /// <summary>
        /// Label for displaying the cart total to be billed.
        /// </summary>
        public TMP_Text total;

        /// <summary>
        /// Label for displaying the expected amount of change.
        /// </summary>
        public TMP_Text change;

        /// <summary>
        /// Label for displaying the current change given.
        /// </summary>
        public TMP_Text given;

        /// <summary>
        /// Parent holding all CashDeskCash components.
        /// </summary>
        public Transform cashParent;

        /// <summary>
        /// Clip to play when giving out notes, or none if not set.
        /// </summary>
        public AudioClip notesClip;

        /// <summary>
        /// Clip to play when giving out coins, or none if not set.
        /// </summary>
        public AudioClip coinsClip;

        //number value of cash received
        private long receivedValue;
        //number value of cart total
        private long totalValue;
        //number value of expected change
        private long changeValue;
        //number value of change given
        private long givenValue;
        //colliders of all CashDeskCash components gathered from cashParent
        private Collider[] cashColliders;


        //initialize references
        void Awake()
        {
            cashColliders = cashParent.GetComponentsInChildren<Collider>();
        }


        //initialize variables
        void Start()
        {
            Clear();
        }


        /// <summary>
        /// Initialize with cart total and calculate change for customer.
        /// </summary>
        public void Initialize(string totalText)
        {
            Clear();
            total.text = totalText;
            
            totalValue = StoreDatabase.FromStringToLongMoney(totalText);
            receivedValue = CustomerSystem.GetCashPaymentAmount(totalValue);
            changeValue = receivedValue - totalValue;

            received.text = StoreDatabase.FromLongToStringMoney(receivedValue);
            change.text = StoreDatabase.FromLongToStringMoney(changeValue);
            given.text = StoreDatabase.FromLongToStringMoney(givenValue);
        }


        /// <summary>
        /// Toggles the colliders for the InteractionSystem raycast.
        /// Also starts listening to user input.
        /// </summary>
        public void SetInteractable(bool state)
        {
            for(int i = 0; i < cashColliders.Length; i++)
                cashColliders[i].enabled = state;

            if (state == true)
            {
                UIGame.AddAction("Enter", "Confirm", true);
                PlayerInput.GetPlayerByIndex(0).onActionTriggered += OnAction;
            }
            else
            {
                Clear();
                UIGame.RemoveAction("Enter");
                PlayerInput.GetPlayerByIndex(0).onActionTriggered -= OnAction;
            }
        }


        /// <summary>
        /// Returns whether one of the Cash colliders is enabled to determine the register state.
        /// </summary>
        public bool IsInteractable()
        {
            return cashColliders[0].enabled;
        }


        /// <summary>
        /// Adds/removes a positive/negative amount of money to the amount given.
        /// </summary>
        public void AddRemoveMoney(long number)
        {
            if (number < 0 && (givenValue == 0 || -number > givenValue)) return;

            if (number < 100) AudioSystem.Play3D(coinsClip, cashParent.position, 0.2f);
            else AudioSystem.Play3D(notesClip, cashParent.position, 0.2f);

            givenValue += number;
            given.text = StoreDatabase.FromLongToStringMoney(givenValue);

            if (changeValue == givenValue) given.color = Color.green;
            else given.color = Color.white;
        }


        /// <summary>
        /// Try to confirm customer payment by sending the bill incl. change to CashDesk.
        /// </summary>
        public void Confirm()
        {
            string cartValue = StoreDatabase.FromLongToStringMoney(receivedValue - givenValue);
            onInputConfirmed?.Invoke(cartValue);
        }


        /// <summary>
        /// Resets all displayed currency labels and internal values to 0.
        /// </summary>
        public void Clear()
        {
            receivedValue = totalValue = changeValue = givenValue = 0;
            received.text = total.text = change.text = given.text = StoreDatabase.FromLongToStringMoney(totalValue);
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
    }
}
