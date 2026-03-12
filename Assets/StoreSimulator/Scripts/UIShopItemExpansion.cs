/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI representation of a ExpansionScriptableObject that can be purchased.
    /// </summary>
    public class UIShopItemExpansion : UIShopItem
    {
        /// <summary>
        /// Gameobject to activate in the UI when this item has been bought.
        /// </summary>
        public GameObject purchasedOverlay;


        //initialize references
        void Awake()
        {
            UpgradeSystem.onUpgradePurchase += OnUpgradePurchase;
        }


        /// <summary>
        /// Extend or override the base UIShopItem initialization.
        /// Here we've implemented a check for another item that is required before unlocking this one.
        /// </summary>
        public override void Initialize(PurchasableScriptableObject purchasable)
        {
            base.Initialize(purchasable);

            ExpansionScriptableObject expansion = purchasable as ExpansionScriptableObject;
            if (lockedOverlay != null && !lockedOverlay.activeInHierarchy && !string.IsNullOrEmpty(expansion.otherRequired))
            {
                ExpansionScriptableObject otherExpansion = ItemDatabase.GetById(typeof(ExpansionScriptableObject), expansion.otherRequired) as ExpansionScriptableObject;
                lockedOverlay.SetActive(!otherExpansion.isPurchased);
                lockedMessage.text = "Requires Expansion " + otherExpansion.title;
            }

            purchasedOverlay.SetActive(expansion.isPurchased);
        }


        /// <summary>
        /// Forward the purchase call to the responsible system.
        /// </summary>
        public override void Purchase()
        {
            UpgradeSystem.Purchase(purchasable);
        }


        //subscribed to upgrade event, setting this item to purchased
        private void OnUpgradePurchase(PurchasableScriptableObject otherPurchasable)
        {
            if (otherPurchasable == purchasable)
            {
                purchasedOverlay.SetActive(true);
                return;
            }

            if (otherPurchasable is not ExpansionScriptableObject)
                return;

            ExpansionScriptableObject expansion = purchasable as ExpansionScriptableObject;
            if (otherPurchasable.id == expansion.otherRequired)
                Refresh();
        }


        //unsubscribe from events
        void OnDestroy()
        {
            UpgradeSystem.onUpgradePurchase -= OnUpgradePurchase;
        }
    }
}
