/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data asset for licenses sold in the game for expanding the product catalog available.
    /// </summary>
    [CreateAssetMenu(fileName = "License", menuName = "ScriptableObjects/License")]
    public class LicenseScriptableObject : PurchasableScriptableObject
    {
        /// <summary>
        /// ID of asset to be owned as pre-requisite, if any.
        /// </summary>
        public string otherRequired;

        /// <summary>
        /// Whether this asset has been bought.
        /// </summary>
        public bool isPurchased;
    }
}