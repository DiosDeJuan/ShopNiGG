/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI representation of a DecorationScriptableObject that can be purchased.
    /// </summary>
    public class UIShopItemDecoration : UIShopItem
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
        /// Customizations do not need to be unlocked based on other items.
        /// </summary>
        public override void Initialize(PurchasableScriptableObject purchasable)
        {
            base.Initialize(purchasable);

            DecorationScriptableObject decoration = purchasable as DecorationScriptableObject;
            purchasedOverlay.SetActive(decoration.isPurchased);
        }


        /// <summary>
        /// Forward the purchase call to the responsible system.
        /// </summary>
        public override void Purchase()
        {
            UpgradeSystem.Purchase(purchasable);
        }


        //subscribed to upgrade event, only one item in the same section can be purchased (selected)
        private void OnUpgradePurchase(PurchasableScriptableObject otherPurchasable)
        {
            if (otherPurchasable is not DecorationScriptableObject)
                return;

            DecorationScriptableObject decoration = purchasable as DecorationScriptableObject;
            DecorationScriptableObject otherDecoration = otherPurchasable as DecorationScriptableObject;

            if (decoration.decorationType == otherDecoration.decorationType)
                purchasedOverlay.SetActive(decoration == otherDecoration);
        }


        //unsubscribe from events
        void OnDestroy()
        {
            UpgradeSystem.onUpgradePurchase -= OnUpgradePurchase;
        }
    }
}
