/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Base class for a data asset that can be bought by the player and extended for different purposes.
    /// </summary>
    [CreateAssetMenu(fileName = "Purchasable", menuName = "ScriptableObjects/Purchasable")]
    public class PurchasableScriptableObject : ScriptableObject
    {
        /// <summary>
        /// Unique identifier of the purchasable used in the databases.
        /// </summary>
        public string id;

        /// <summary>
        /// A sub-category the item should be organized into in the layout.
        /// </summary>
        public string category;

        /// <summary>
        /// Name of the item displayed when buying or selling it.
        /// </summary>
        public string title;

        /// <summary>
        /// Preview image displayed in the shop, packages and on a PriceTag.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Level where this purchasable should be first available for purchase.
        /// </summary>
        public int requiredLevel;

        /// <summary>
        /// The initial price this item can be purchased for by the player.
        /// </summary>
        public long buyPrice;
    }
}