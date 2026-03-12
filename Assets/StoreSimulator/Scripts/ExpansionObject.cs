/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */
 
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Object reacting to when an expansion has been bought, doing a visual reflection in the scene.
    /// </summary>
    public class ExpansionObject : MonoBehaviour
    {
        /// <summary>
        /// ScriptableObject asset this object is associated with.
        /// </summary>
        public ExpansionScriptableObject expansion;

        /// <summary>
        /// The StorageGrid game object to activate when this expansion has been bought.
        /// </summary>
        public StorageGrid grid;
      

        //initialize references
        void Awake()
        {
            UpgradeSystem.onUpgradePurchase += OnExpansionPurchase;
        }


        //initialize variables
        void Start()
        {
            grid.gameObject.SetActive(expansion.isPurchased);

            if (expansion.isPurchased)
                UpgradeSystem.SetBelowGround(transform);
        }


        //action on purchase callback
        private void OnExpansionPurchase(PurchasableScriptableObject otherPurchasable)
        {
            if (otherPurchasable != expansion)
                return;

            StartCoroutine(UpgradeSystem.MoveBelowGround(transform));
            grid.gameObject.SetActive(true);
        }


        //unsubscribe from events
        void OnDestroy()
        {
            UpgradeSystem.onUpgradePurchase -= OnExpansionPurchase;
        }
    }
}
