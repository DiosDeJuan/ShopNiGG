/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data asset for decoratives sold in the game for customizing the store visually.
    /// </summary>
    [CreateAssetMenu(fileName = "Decoration", menuName = "ScriptableObjects/Decoration")]
    public class DecorationScriptableObject : PurchasableScriptableObject
    {
        /// <summary>
        /// Type of where the texture should be applied.
        /// </summary>
        public DecorationType decorationType;

        /// <summary>
        /// The texture of this decoration object.
        /// </summary>
        public Texture texture;

        /// <summary>
        /// Whether this asset has been bought.
        /// </summary>
        public bool isPurchased;
    }
}