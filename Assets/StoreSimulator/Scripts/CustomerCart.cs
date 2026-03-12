/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Container for CustomerBagItem the customer wants to collect and purchase in the store.
    /// </summary>
    public class CustomerCart : MonoBehaviour
    {
        /// <summary>
        /// The current index the customer is processing, starts from 1 up to shoppingCount.
        /// </summary>
        [Header("Debugging Only")]
        public int itemIndex = 0;

        /// <summary>
        /// The wishlist of items, consisting of the respective PlacementObject that holds it.
        /// </summary>
        public List<(ProductScriptableObject product, PlacementObject placement)> wishlist = new List<(ProductScriptableObject, PlacementObject)>();

        /// <summary>
        /// List of items that have been selected and are currently in the customer's bag.
        /// </summary>
        public List<CustomerBagItem> items = new List<CustomerBagItem>();

        //minimum count of products to be collected
        private int shoppingCount = 1;


        /// <summary>
        /// Initializes a list of items this customer should collect, and also how many of them, on a random placement.
        /// For this, the method gets a list of random products which have been unlocked up to the player's level.
        /// The wishlist could also contain products from the product catalog that the player has not yet placed in the store.
        /// </summary>
        public void CreateWishlist()
        {
            bool multipleProducts = UnityEngine.Random.Range(100, 0) <= CustomerSystem.Instance.multipleProductRate;
            if (multipleProducts)
                shoppingCount = UnityEngine.Random.Range(CustomerSystem.Instance.maxBagProducts, shoppingCount);

            List<ProductScriptableObject> products = ItemDatabase.GetProductsRandom(shoppingCount);
            for(int i = 0; i < products.Count; i++)
                wishlist.Add((products[i], null));
        }


        /// <summary>
        /// Return the ScriptableObject reference of the current product the customer is trying to collect.
        /// </summary>
        public ProductScriptableObject GetProduct()
        {
            return wishlist[itemIndex].product;
        }


        /// <summary>
        /// Return the location the customer should walk to for collecting the item (grabSpot), or null if placement is empty.
        /// </summary>
        public Transform GetProductPlacement()
        {
            if (wishlist[itemIndex].placement == null)
                wishlist[itemIndex] = (wishlist[itemIndex].product, StoreDatabase.Instance.GetProductPlacementRandom(wishlist[itemIndex].product));

            return wishlist[itemIndex].placement == null || wishlist[itemIndex].placement.IsEmpty() ? null : wishlist[itemIndex].placement.grabSpot;
        }
        

        /// <summary>
        /// Returns the amount of space left in the bag.
        /// </summary>
        public int GetMissingCount()
        {
            int bagCount = 0;
            for(int i = 0; i < items.Count; i++)
                bagCount += items[i].count;

            return shoppingCount - bagCount;
        }


        /// <summary>
        /// Returns whether another iteration for collecting should be done.
        /// </summary>
        public bool ShouldCollect()
        {
            return itemIndex < wishlist.Count;
        }


        /// <summary>
        /// Checks whether the current placement is still valid for collection.
        /// </summary>
        public bool CanCollect()
        {
            return wishlist[itemIndex].placement != null && !wishlist[itemIndex].placement.IsEmpty();
        }


        /// <summary>
        /// Collects a product by adding it to the bag and destroys it from the placement. 
        /// </summary>
        public void Add()
        {
            PlacementObject placement = wishlist[itemIndex].placement;
            if (placement.IsEmpty())
                return;

            CustomerBagItem item = null;
            ProductScriptableObject product = placement.product;

            for(int i = 0; i < items.Count; i++)
            {
                if (items[i].product == product)
                {
                    item = items[i];
                    item.count++;
                    break;
                }
            }

            if (item == null)
            {
                item = new CustomerBagItem();
                item.product = product;
                item.fixedPrice = product.storePrice;
                items.Add(item);
            }

            DestroyImmediate(placement.Remove().gameObject);
        }


        /// <summary>
        /// Returns the count of individual items in the bag.
        /// </summary>
        public int GetItemsCount()
        {
            return items.Count;
        }


        /// <summary>
        /// Continues with the collection iteration and moves the item index further.
        /// </summary>
        public void SetNextItem()
        {
            itemIndex++;
        }
    }


    /// <summary>
    /// Class representing one collected product in the customer's bag.
    /// </summary>
    [Serializable]
    public class CustomerBagItem
    {
        /// <summary>
        /// The scriptable object reference to the product.
        /// </summary>
        public ProductScriptableObject product;
        
        /// <summary>
        /// Price at the time the product was collected, to have it fixed.
        /// </summary>
        public long fixedPrice;
        
        /// <summary>
        /// Count of this product.
        /// </summary>
        public int count = 1;
    }
}