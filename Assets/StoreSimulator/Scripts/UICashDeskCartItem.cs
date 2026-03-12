/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI representation of one line in the UICashCart, for one unique product that was scanned.
    /// </summary>
    public class UICashDeskCartItem : MonoBehaviour
    {
        /// <summary>
        /// Reference to the data asset of the product that was scanned.
        /// </summary>
        [HideInInspector]
        public ProductScriptableObject product;

        /// <summary>
        /// Label for displaying the name of the product.
        /// </summary>
        public TMP_Text title;

        /// <summary>
        /// Label for displaying the product count that was scanned so far.
        /// </summary>
        public TMP_Text count;

        /// <summary>
        /// Label for displaying the price of one product unit.
        /// </summary>
        public TMP_Text singlePrice;

        /// <summary>
        /// Label for displaying the total price of all units for this product.
        /// </summary>
        public TMP_Text totalPrice;

        //take over fixed price from CheckoutItem
        private long fixedPrice;
        //the unit count as number
        private int amount = 1;


        /// <summary>
        /// Initialize UI labels with values from CheckoutItem.
        /// </summary>
        public void Initialize(CheckoutItem deskItem)
        {
            product = deskItem.product;
            fixedPrice = deskItem.fixedPrice;

            title.text = deskItem.product.title;
            count.text = amount.ToString();
            singlePrice.text = StoreDatabase.FromLongToStringMoney(fixedPrice);
            totalPrice.text = StoreDatabase.FromLongToStringMoney(amount * fixedPrice);
        }


        /// <summary>
        /// Increase displayed product unit count by one.
        /// </summary>
        public void AddCount()
        {
            amount++;

            count.text = amount.ToString();
            totalPrice.text = StoreDatabase.FromLongToStringMoney(amount * fixedPrice);
        }
    }
}
