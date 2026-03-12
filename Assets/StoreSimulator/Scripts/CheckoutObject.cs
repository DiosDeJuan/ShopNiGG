/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Abstract class for checkout objects allowing customers or the player to process checkouts.
    /// The implementation defines what type and whether manual or automatic checkouts are available.
    /// </summary>
    public abstract class CheckoutObject : Interactable
    {
        [Header("General")]
                
        /// <summary>
        /// The speed used when entering this object or moving items to their target destination when processing them.
        /// </summary>
        public float lerpSpeed = 2f;

        /// <summary>
        /// Positions for customers to stand in line.
        /// </summary>
        public Transform queuePositions;

        /// <summary>
        /// The positions items should be instantiated at when placed from customers.
        /// </summary>
        public Transform conveyorPositions;

        /// <summary>
        /// The endpoint where items should move to from their start position when processed.
        /// </summary>
        public Transform conveyorEndpoint;

        /// <summary>
        /// Reference to the UI display of already processed items for the current customer.
        /// </summary>
        public UICashDeskCart cart;

        /// <summary>
        /// The clip to play when an item has been scanned, or none if not set.
        /// </summary>
        public AudioClip scanClip;

        /// <summary>
        /// The clip to play when a customer checkout succeeded, or none if not set.
        /// </summary>
        public AudioClip successClip;

        //the current list of customers queued and waiting for checkout
        protected List<Customer> customerQueue = new List<Customer>();
        //reference to the customers shopping bag to pull out items
        protected CustomerCart customerBag;
        //a list of items that have been placed on this desk
        protected List<CheckoutItem> deskItems = new List<CheckoutItem>();


        /// <summary>
        /// Can be overridden to define where new customers should be added to in the queue.
        /// Returns the queue Transform position and index they get in line.
        /// </summary>
        public virtual (Transform, int) AddCustomerToQueue(Customer customer) { return (null, 0); }

        /// <summary>
        /// Can be overridden to define the position where customers should walk to when queueing up.
        /// </summary>
        public virtual Vector3 GetLastQueuePosition() { return Vector3.zero; }

        /// <summary>
        /// Can be overridden to define what should happen when customers are first in line.
        /// </summary>
        public virtual void PlaceBagContents(CustomerCart bag) { }

        /// <summary>
        /// Can be overridden to define an action when an item is processed.
        /// </summary>
        public virtual void Scan(CheckoutItem item) { }

        /// <summary>
        /// Can be overridden to define the action when customer payment item was collected.
        /// </summary>
        public virtual void ActivateCheckout() { }
        
        /// <summary>
        /// Can be overridden to define calculations on how customers are getting billed, like change and checkout errors.
        /// </summary>
        protected virtual void OnBillCustomer(string amount) { }
    }
}
