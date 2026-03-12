/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data container for a StorageObject.
    /// </summary>
    [CreateAssetMenu(fileName = "Storage", menuName = "ScriptableObjects/Storage")]
    public class StorageScriptableObject : PurchasableScriptableObject
    {
        /// <summary>
        /// The ingame representation of the object that should be instantiated.
        /// </summary>
        public GameObject prefab;
    }
}