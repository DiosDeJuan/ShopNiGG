/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI representation of a StorageScriptableObject that can be purchased.
    /// </summary>
    public class UIShopItemStorage : UIShopItem
    {
        /// <summary>
        /// Forward the purchase call to the responsible system.
        /// </summary>
        public override void Purchase()
        {
            DeliverySystem.Purchase(purchasable);
        }
    }
}
