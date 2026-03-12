/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Component added to a product item when it is placed on a Checkout, allowing it to be scanned.
    /// </summary>
    public class CheckoutItem : Interactable
    {
        /// <summary>
        /// Product asset this item should represent.
        /// </summary>
        [HideInInspector]
        public ProductScriptableObject product;

        /// <summary>
        /// Price that was set when the customer took the item, passed over when scanned.
        /// </summary>
        [HideInInspector]
        public long fixedPrice;

        //reference to the "owning" CheckoutObject for adding this item to it
        private CheckoutObject checkout;
        //reference to collider used for detecting an interaction
        private Collider col;


        /// <summary>
        /// Initialize after instantiation and allow interaction.
        /// Take over variables from CustomerBagItem and assign CashDesk.
        /// </summary>
        public void Initialize(CustomerBagItem bagItem, CheckoutObject checkoutObj)
        {
            col = GetComponent<Collider>();

            if (checkoutObj is CashDesk) col.enabled = ((CashDesk)checkoutObj).isPlayerControlled;
            else col.enabled = false;

            product = bagItem.product;
            fixedPrice = bagItem.fixedPrice;
            checkout = checkoutObj;

            gameObject.layer = (int)Mathf.Log(InteractionSystem.Instance.layerMask.value, 2);
        }


        /// <summary>
        /// Toggles the collider for the InteractionSystem raycast.
        /// </summary>
        public void SetInteractable(bool state)
        {
            col.enabled = state;
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            UIGame.AddAction("LeftClick", "Scan", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// This item is scanned and added to UICashCart.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;

            col.enabled = false;
            checkout.Scan(this);
            return true;
        }


        /// <summary>
        /// Interactable override, removing UI action.
        /// </summary>
        public override void OnLostFocus()
        {
            UIGame.RemoveAction("LeftClick");
        }
    }
}
