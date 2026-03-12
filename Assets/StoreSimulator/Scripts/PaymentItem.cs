/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Visualized object for a payment instrument that is interacted with by the player 
    /// to continue the checkout process. Payment instruments in this asset are cash and card.
    /// </summary>
    public class PaymentItem : Interactable
    {
        //reference of customer owning this
        private Customer customer;


        /// <summary>
        /// Set the reference to the customer instance holding this PaymentItem.
        /// </summary>
        public void Initialize(Customer customer)
        {
            this.customer = customer;
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            UIGame.AddAction("LeftClick", "Accept", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// Sends further instructions to the customer.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;
            
            UIGame.RemoveAction("LeftClick");
            customer.HasPaid();
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
