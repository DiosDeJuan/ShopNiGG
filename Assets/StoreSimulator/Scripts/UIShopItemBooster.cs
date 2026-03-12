/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI representation of a BoosterScriptableObject that can be purchased.
    /// </summary>
    public class UIShopItemBooster : UIShopItem
    {
        /// <summary>
        /// Label for displaying the description on the booster effect.
        /// </summary>
        public TMP_Text description;

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

            BoosterScriptableObject booster = purchasable as BoosterScriptableObject;
            description.text = booster.description;
            purchasedOverlay.SetActive(booster.isPurchased);
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
                purchasedOverlay.SetActive(true);
        }


        //unsubscribe from events
        void OnDestroy()
        {
            UpgradeSystem.onUpgradePurchase -= OnUpgradePurchase;
        }
    }
}
