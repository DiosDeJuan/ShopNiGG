/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Base class of a UI representation for ScriptableObjects to be purchased by the player.
    /// Extended in implementations to add type-specific code.
    /// </summary>
    public class UIShopItem : MonoBehaviour
    {
        /// <summary>
        /// A reference to the purchasable that is linked to this shop item.
        /// </summary>
        public PurchasableScriptableObject purchasable;

        /// <summary>
        /// Label for displaying the item name.
        /// </summary>
        public TMP_Text title;

        /// <summary>
        /// Image for displaying the item icon.
        /// </summary>
        public Image icon;

        /// <summary>
        /// Label for displaying the price of one item unit.
        /// </summary>
        public TMP_Text buyPrice;

        /// <summary>
        /// Gameobject to activate in the UI when this item is locked.
        /// </summary>
        public GameObject lockedOverlay;

        /// <summary>
        /// Label for displaying the requirements in order to unlock this item.
        /// </summary>
        public TMP_Text lockedMessage;


        /// <summary>
        /// Base initialization for filling out general details about the item.
        /// Can be overridden in an implementation to cover additional variables.
        /// </summary>
        public virtual void Initialize(PurchasableScriptableObject purchasable)
        {
            this.purchasable = purchasable;

            if (title) title.text = purchasable.title;
            if (icon) icon.sprite = purchasable.icon;
            if (buyPrice) buyPrice.text = StoreDatabase.FromLongToStringMoney(purchasable.buyPrice);

            if (lockedOverlay != null && purchasable.requiredLevel > 0)
            {
                bool isLocked = StoreDatabase.Instance.currentLevel < purchasable.requiredLevel;
                lockedOverlay.SetActive(isLocked);
                lockedMessage.text = "Unlocked at Level " + purchasable.requiredLevel;
            }
        }


        /// <summary>
        /// Defines what should happen on purchase attempt.
        /// Each ScriptableObject type has its own implementation.
        /// </summary>
        public virtual void Purchase() {}


        /// <summary>
        /// Refresh in case this item could have been unlocked.
        /// </summary>
        public void Refresh()
        {
            if (StoreDatabase.Instance.currentLevel < purchasable.requiredLevel)
                return;

            Initialize(purchasable);
        }
    }
}
