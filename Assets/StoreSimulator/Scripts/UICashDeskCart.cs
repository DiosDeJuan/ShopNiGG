/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI for displaying the list of scanned items on a CashDesk.
    /// </summary>
    public class UICashDeskCart : MonoBehaviour
    {
        /// <summary>
        /// The UICashCartItem prefab instantiated for each new scanned product.
        /// </summary>
        public GameObject itemPrefab;

        /// <summary>
        /// The container new UICashCartItem instances should be parented to.
        /// </summary>
        public Transform container;

        /// <summary>
        /// Label for displaying the total price as formatted currency string.
        /// </summary>
        public TMP_Text total;

        //total of all items in cents
        private long sum;
        //list of items that have been scanned
        private List<UICashDeskCartItem> items = new List<UICashDeskCartItem>();


        //initialize variables
        void Start()
        {  
            total.text = StoreDatabase.FromLongToStringMoney(0);
        }


        /// <summary>
        /// Instantiates a new UICashCartItem for the CashDeskItem passed in.
        /// If the product was already scanned, its count is increased by one.
        /// </summary>
        public void Add(CheckoutItem deskItem)
        {
            UICashDeskCartItem item = null;
            for(int i = 0; i < items.Count; i++)
            {
                if (items[i].product == deskItem.product)
                {
                    item = items[i];
                    item.AddCount();
                }
            }

            if (item == null)
            {
                item = Instantiate(itemPrefab, container, false).GetComponent<UICashDeskCartItem>();
                item.Initialize(deskItem);
                items.Add(item);
            }

            sum += deskItem.fixedPrice;
            total.text = StoreDatabase.FromLongToStringMoney(sum);
        }


        /// <summary>
        /// Destroy all UICashCartItem instances on the container and reset values.
        /// This is to clear up after checkout for the next customer.
        /// </summary>
        public void Clear()
        {
            int itemCount = items.Count;
            for(int i = itemCount - 1; i >= 0; i--)
                Destroy(items[i].gameObject);

            items.Clear();
            sum = 0;
            total.text = StoreDatabase.FromLongToStringMoney(sum);
        }
    }
}
