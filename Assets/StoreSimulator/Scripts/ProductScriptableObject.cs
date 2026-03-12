/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data asset for products sold in the game, bundled in packages and collected by customers.
    /// </summary>
    [CreateAssetMenu(fileName = "Product", menuName = "ScriptableObjects/Product")]
    public class ProductScriptableObject : PurchasableScriptableObject
    {
        /// <summary>
        /// The ingame representation of the object that should be instantiated.
        /// </summary>
        public GameObject prefab;

        /// <summary>
        /// Type of storage for defining compatible placements placed on.
        /// </summary>
        public StorageType storageType;

        /// <summary>
        /// Dimensions of a product taking up space in a PlacementObject.
        /// </summary>
        public Vector2Int size = Vector2Int.one;

        /// <summary>
        /// Amount of products pre-filled in a package when bought.
        /// </summary>
        public int packageCount = 12;

        /// <summary>
        /// Self-chosen price for one product sold to customers.
        /// </summary>
        public long storePrice = 0;

        /// <summary>
        /// The average price customers are expected to pay for one product.
        /// </summary>
        public long marketPrice = 0;

        /// <summary>
        /// Additional license id required to unlock, or empty if available on the required level by default.
        /// </summary>
        public string requiredLicense;
    }
}