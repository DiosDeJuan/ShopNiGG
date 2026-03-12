/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Customer process their items on this checkout on their own, using card or cash.
    /// Only one item can be processed at the same time. There is no need to give change.
    /// This uses a StorageObject under the hood for being able to move and place it.
    /// </summary>
    public class SelfCheckout : CheckoutObject
    {
        [Header("Self Checkout")]

        /// <summary>
        /// Delay between products scanned by the customer.
        /// </summary>
        public float scanDelay = 3;

        /// <summary>
        /// Delay for the customer to hold the PaymentItem before final payment.
        /// </summary>
        public float billDelay = 5;

        /// <summary>
        /// Lamp for showing if checkout is available or occupied.
        /// We re-use the placement preview materials from StorageSystem for this.
        /// </summary>
        public MeshRenderer lampRenderer;

        //reference to the StorageObject for moving this checkout
        private StorageObject storage;


        //initialize references
        void Awake()
        {
            storage = GetComponent<StorageObject>();
        }


        //initialize variables
        void Start()
        {
            //do not add to checkouts yet if we're still in preview
            //only do it for an object when it is placed on a grid
            if (StorageSystem.Instance.previewObject != storage)
            {
                StoreDatabase.Instance.AddCheckoutObject(this);
            }
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
        /// CheckoutObject override, starts a coroutine for automatic scanning of products.
        /// </summary>
        public override void PlaceBagContents(CustomerCart bag)
        {
            if (customerBag != null)
                return;

            customerBag = bag;
            StartCoroutine(ScanRoutine());

            if (lampRenderer != null)
                lampRenderer.material = StorageSystem.Instance.invalidMaterial;
        }


        /// <summary>
        /// CheckoutObject override, do scanning only.
        /// Whether a customer has scanned all products and needs to pay is handled in the ScanRoutine.
        /// </summary>
        public override void Scan(CheckoutItem item)
        {
            cart.Add(item);
            deskItems.Remove(item);
            AudioSystem.Play3D(scanClip, conveyorPositions.position);

            StartCoroutine(DestroyItem(item.transform));
        }


        /// <summary>
        /// CheckoutObject override, nothing to do here.
        /// We only add it here so you can see it is actually not being used.
        /// </summary>
        public override void ActivateCheckout() { }


        /// <summary>
        /// CheckoutObject override, having a simplified version where customer always pay with the correct amount.
        /// There is no need for change calculation. Once billed the customer then leaves the queue for the next one
        /// </summary>
        protected override void OnBillCustomer(string amount)
        {
            long cartAmount = StoreDatabase.FromStringToLongMoney(cart.total.text);

            cart.Clear();
            StoreDatabase.AddRemoveMoney(cartAmount);
            AudioSystem.Play2D(successClip);

            customerBag = null;
            customerQueue[0].GoHome();
            customerQueue.RemoveAt(0);
            StoreDatabase.AddRemoveExperience(3);

            if(lampRenderer != null)
                lampRenderer.material = StorageSystem.Instance.validMaterial;

            for (int i = 0; i < customerQueue.Count; i++)
            {
                customerQueue[i].ProceedQueue(queuePositions.GetChild(i), i + 1);
            }
        }


        /// <summary>
        /// Interactable override, forwarding to StorageObject Interactable.
        /// </summary>
        public override void OnBecameFocus()
        {
            storage.OnBecameFocus();
        }
        

        /// <summary>
        /// Interactable override, forwarding to StorageObject Interactable.
        /// Prevent moving when used.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;

            if (customerBag != null || customerQueue.Count > 0)
            {
                UIGame.Instance.ShowMessage("Checkout is currently in use");
                return false;
            }

            StoreDatabase.Instance.RemoveCheckoutObject(this);
            storage.Interact(actionName);
            return true;
        }


        /// <summary>
        /// Interactable override, forwarding to StorageObject Interactable.
        /// </summary>
        public override void OnLostFocus()
        {
            storage.OnLostFocus();
        }


        //go over customer items one by one and scan them
        //if no items are left proceed with the payment
        private IEnumerator ScanRoutine()
        {
            List<CustomerBagItem> items = customerBag.items;

            for (int i = 0; i < items.Count; i++)
            {
                for (int j = 0; j < items[i].count; j++)
                {
                    GameObject bagItem = Instantiate(items[i].product.prefab, conveyorEndpoint.position, Quaternion.identity);

                    CheckoutItem deskItem = bagItem.AddComponent<CheckoutItem>();
                    deskItem.Initialize(items[i], this);
                    deskItems.Add(deskItem);

                    //move item from customer to desk position
                    //this is simulated because the Queue and Conveyor position is the same
                    yield return InteractionSystem.MoveToTargetArc(bagItem.transform, null, conveyorPositions.GetChild(0).position, Quaternion.identity, lerpSpeed, false);
                    
                    yield return new WaitForSeconds(scanDelay);
                    Scan(deskItem);
                }
            }

            yield return new WaitForSeconds(1);

            //this was the last item available, do payment
            customerQueue[0].ProceedPayment(false);
            StartCoroutine(BillRoutine());
        }


        //adds some delay to the payment workflow
        private IEnumerator BillRoutine()
        {
            yield return new WaitForSeconds(billDelay);

            customerQueue[0].HasPaid();

            yield return new WaitForSeconds(1);

            OnBillCustomer(string.Empty);
        }


        //move item back to customer and destroy it at the end
        private IEnumerator DestroyItem(Transform item)
        {
            yield return InteractionSystem.MoveToTargetArc(item, null, conveyorEndpoint.position, Quaternion.identity, lerpSpeed, false);
            Destroy(item.gameObject);
        }
    }
}