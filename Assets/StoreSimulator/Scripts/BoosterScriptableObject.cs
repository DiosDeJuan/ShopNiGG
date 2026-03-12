/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data asset for temporary boosters sold in the game that are only active for one day.
    /// </summary>
    [CreateAssetMenu(fileName = "Booster", menuName = "ScriptableObjects/Booster")]
    public class BoosterScriptableObject : PurchasableScriptableObject
    {
        /// <summary>
        /// Additional text describing what the booster does when bought.
        /// </summary>
        public string description;

        /// <summary>
        /// Whether this asset has been bought.
        /// </summary>
        public bool isPurchased;
    }
}