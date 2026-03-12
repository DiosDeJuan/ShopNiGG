/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Allows processing customer checkouts by card or cash manually.
    /// Customers place their items on it and wait in line.
    /// </summary>
    public class CashDesk : CheckoutObject
    {
        [Header("CashDesk")]
        /// <summary>
        /// The initial position and rotation the player camera should move to when entering this object.
        /// </summary>
        public Transform lookTransform;

        /// <summary>
        /// The clip to play when the cash register opens, or none if not set.
        /// </summary>
        public AudioClip registerClip;

        /// <summary>
        /// The clip to play when charging a customer failed, or none if not set.
        /// </summary>
        public AudioClip failureClip;

        /// <summary>
        /// Reference to the UI terminal component for customers paying by card.
        /// </summary>
        public UICashDeskTerminal terminal;

        /// <summary>
        /// Reference to the UI cash register component for customers paying with cash.
        /// </summary>
        public UICashDeskRegister register;

        /// <summary>
        /// Whether this object is currently controlled by the player.
        /// </summary>
        public bool isPlayerControlled { get; private set; }

        //previous camera position that should be transitioned back to when leaving
        private Vector3 prevCamPosition;
        //previous camera rotation that should be transitioned back to when leaving
        private Quaternion prevCamRotation;
        //reference to colliders used for detecting an interaction
        private Collider[] cols;
        //reference to Animation component
        private Animation anim;


        //initialize references
        void Awake()
        {
            cols = GetComponents<Collider>();
            anim = GetComponent<Animation>();

            terminal.onInputConfirmed += OnBillCustomer;
            register.onInputConfirmed += OnBillCustomer;
        }


        /// <summary>
        /// CheckoutObject override, adding new customers to the end of the queue.
        /// </summary>
        public override (Transform, int) AddCustomerToQueue(Customer customer)
        {
            if (customerQueue.Count == queuePositions.childCount)
                return (null, -1);

            customerQueue.Add(customer);
            return (queuePositions.GetChild(customerQueue.Count - 1), customerQueue.Count);
        }


        /// <summary>
        /// CheckoutObject override, getting last world position of queue positions.
        /// </summary>
        public override Vector3 GetLastQueuePosition()
        {
            return queuePositions.GetChild(queuePositions.childCount - 1).position;
        }


        /// <summary>
        /// CheckoutObject override, places all items from the "shopping" bag on this desk.
        /// </summary>
        public override void PlaceBagContents(CustomerCart bag)
        {
            //a reference is stored to not call this multiple times
            if (customerBag != null)
                return;

            customerBag = bag;
            List<CustomerBagItem> items = bag.items;

            if (!isPlayerControlled)
                UIGame.AddNotification("Customer waiting at Cash Desk");

            int itemIndex = 0;
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = 0; j < items[i].count; j++)
                {
                    Transform child = conveyorPositions.GetChild(itemIndex);
                    GameObject bagItem = Instantiate(items[i].product.prefab, child.position, Quaternion.identity, child);

                    CheckoutItem deskItem = bagItem.AddComponent<CheckoutItem>();
                    deskItem.Initialize(items[i], this);
                    deskItems.Add(deskItem);

                    itemIndex++;
                }
            }
        }
        

        /// <summary>
        /// CheckoutObject override, do scanning and check for last item for initiating payment.
        /// </summary>
        public override void Scan(CheckoutItem item)
        {
            cart.Add(item);
            deskItems.Remove(item);
            AudioSystem.Play3D(scanClip, conveyorPositions.position);

            //this was the last item available, do payment
            if (deskItems.Count == 0)
                customerQueue[0].ProceedPayment(true);

            StartCoroutine(DestroyItem(item.transform));
        }


        /// <summary>
        /// CheckoutObject override, activates terminal or cash register based on customer preference.
        /// </summary>
        public override void ActivateCheckout()
        {
            if (customerQueue[0].payCash)
            {
                register.Initialize(cart.total.text);
                register.SetInteractable(true);

                AudioSystem.Play3D(registerClip, register.cashParent.position);
                anim.Play("CashRegister_Open");
            } 
            else terminal.SetInteractable(true);
        }


        /// <summary>
        /// CheckoutObject override, finish the customer by comparing billed amount with total due amount.
        /// Incorrect change plays a failed sound. Once billed the customer then leaves the queue for the next one.
        /// </summary>
        protected override void OnBillCustomer(string amount)
        {
            long billAmount = StoreDatabase.FromStringToLongMoney(amount);
            long cartAmount = StoreDatabase.FromStringToLongMoney(cart.total.text);

            //checkout mismatch
            if (billAmount != cartAmount)
            {
                bool checkoutError = false;

                switch (customerQueue[0].payCash)
                {
                    //we gave less change than required
                    case true:
                        if (billAmount > cartAmount)
                        {
                            register.given.color = Color.red;
                            checkoutError = true;
                        }
                        break;

                    //card checkouts need to match
                    case false:
                        terminal.input.GetComponent<Image>().color = new Color32(255, 99, 71, 255);
                        checkoutError = true;
                        break;
                }

                if (checkoutError)
                {
                    AudioSystem.Play2D(failureClip);
                    return;
                }
            }

            cart.Clear();
            StoreDatabase.AddRemoveMoney(billAmount);
            AudioSystem.Play2D(successClip);

            if (customerQueue[0].payCash)
            {
                if (billAmount > cartAmount)
                    customerQueue[0].ShowUnhappy("Overcharged!");

                register.SetInteractable(false);
                anim.Play("CashRegister_Close");
            }
            else
            {
                terminal.input.GetComponent<Image>().color = Color.white;
                terminal.Exit(true);
            }

            customerBag = null;
            customerQueue[0].GoHome();
            customerQueue.RemoveAt(0);
            StoreDatabase.AddRemoveExperience(3);

            for (int i = 0; i < customerQueue.Count; i++)
            {
                customerQueue[i].ProceedQueue(queuePositions.GetChild(i), i + 1);
            }
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

            isPlayerControlled = true;
            PlayerController.SetMovementState(MovementState.None, false);

            for(int i = 0; i < cols.Length; i++)
                cols[i].enabled = false;

            StartCoroutine(Enter());
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

            //in case we left during checkout
            if (terminal.isPlayerControlled)
            {
                terminal.Exit();
            }
            if (register.IsInteractable())
            {
                register.SetInteractable(false);
                anim.Play("CashRegister_Close");
            }

            //if there are any items placed already, disable them
            for (int i = 0; i < deskItems.Count; i++)
                deskItems[i].SetInteractable(false);

            //pause checkout for the current customer
            if (customerQueue.Count > 0)
                customerQueue[0].PausePayment();

            isPlayerControlled = false;
            Transform camTransform = PlayerController.GetCameraTransform();
            InteractionSystem.MoveToTargetLinear(camTransform, null, prevCamPosition, prevCamRotation, lerpSpeed, true);

            Invoke("ReEnable", 0.5f);
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                switch (context.action.name)
                {
                    case "Cancel":
                        Exit();
                        break;
                }
            }
        }


        //move item across the desk and destroy it at the end
        private IEnumerator DestroyItem(Transform item)
        {
            yield return InteractionSystem.MoveToTargetLinear(item, null, conveyorEndpoint.position, item.rotation, lerpSpeed, false);
            Destroy(item.gameObject);
        }


        //transition the player to the initial camera position and rotation (lookTransform)
        //if a customer is already waiting for checkout, that payment workflow is being continued
        private IEnumerator Enter()
        {
            Transform camTransform = PlayerController.GetCameraTransform();
            prevCamPosition = camTransform.localPosition;
            prevCamRotation = camTransform.localRotation;

            yield return InteractionSystem.MoveToTargetLinear(camTransform, null, lookTransform.position, Quaternion.LookRotation(lookTransform.forward), lerpSpeed, false);

            PlayerController.SetCameraRotation(camTransform.localRotation);
            PlayerController.SetMovementState(MovementState.RotationOnly, true);

            for(int i = 0; i < deskItems.Count; i++)
                deskItems[i].SetInteractable(true);

            if (customerBag != null && deskItems.Count == 0)
                customerQueue[0].ProceedPayment(true);
        }


        //after exiting the controlled state, player movement is re-enabled with a short delay
        private void ReEnable()
        {
            PlayerController.SetCameraRotation(prevCamRotation);
            PlayerController.SetMovementState(MovementState.All, true);

            for(int i = 0; i < cols.Length; i++)
                cols[i].enabled = true;
        }


        //unsubscribe from events
        void OnDestroy()
        {
            terminal.onInputConfirmed -= OnBillCustomer;
            register.onInputConfirmed -= OnBillCustomer;
        }
    }
}
