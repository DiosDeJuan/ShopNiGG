/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Groups multiple UIShopItem instances with the same purchasable type under one container.
    /// </summary>
    public class UIShopCategory : MonoBehaviour
    {
        /// <summary>
        /// Reference to the purchasable type this category should be associated with.
        /// The actual reference assigned does not matter, only the type it has.
        /// </summary>
        public PurchasableScriptableObject purchasable;

        /// <summary>
        /// The UIShopItem prefab to be instantiated for this category.
        /// </summary>
        public GameObject itemPrefab;

        /// <summary>
        /// The container where UIShopItemContainer categories should be parented to.
        /// </summary>
        public Transform container;

        /// <summary>
        /// The UIShopItemContainer prefab holding a header label and Transform the items should be parented to.
        /// </summary>
        public GameObject containerPrefab;

        /// <summary>
        /// Whether items from higher levels should be instantiated as well.
        /// </summary>
        public bool hideOtherLevels = true;

        /// <summary>
        /// If the category should be disabled on launch.
        /// Usually only one category can be active at the same time.
        /// </summary>
        public bool startHidden = false;

        //the initial maximum level value we aim for (current level or last)
        private int maxLevel = 0;
        //a list of all UIShopItems instances on this category for quick access
        private List<UIShopItem> shopItems = new List<UIShopItem>();
        //a dictionary of header labels with their Transform as a container for the items
        private Dictionary<string, Transform> containerList = new Dictionary<string, Transform>();


        //initialize references
        void Awake()
        {
            StoreDatabase.onLevelUpdate += OnLevelUpdate;

            if (purchasable is ProductScriptableObject)
                UpgradeSystem.onUpgradePurchase += OnUpgradePurchase;
        }


        //initialize variables
        void Start()
        {
            maxLevel = StoreDatabase.Instance.currentLevel;
            int lastLevel = StoreDatabase.Instance.levelXP.Length;

            if (hideOtherLevels) for(int i = 0; i <= maxLevel; i++) AddItems(i);
            else for(int i = 0; i <= lastLevel; i++) AddItems(i);

            if (startHidden)
                gameObject.SetActive(false);
        }


        //subscribed to level change event
        private void OnLevelUpdate(int level)
        {
            //skip in case we lost a level
            //to not lock already unlocked items again
            if (level < maxLevel)
                return;

            for(int i = 0; i < shopItems.Count; i++)
                shopItems[i].Refresh();

            //instantiate new items
            if (hideOtherLevels)
                AddItems(level);
                
            maxLevel = level;
        }


        //adds newly unlocked purchasables, with the same type, on the level, to this category
        private void AddItems(int level)
        {
            if (purchasable == null)
                return;
                
            List<PurchasableScriptableObject> items = ItemDatabase.GetByLevel(purchasable.GetType(), level);

            for(int i = 0; i < items.Count; i++)
            {             
                InstantiateItem(items[i]);
            }
        }


        //subscribed to the upgrade purchase event. But only if this category is a category for products,
        //and the other purchasable invoking this event is a LicenseScriptableObject that was unlocked
        private void OnUpgradePurchase(PurchasableScriptableObject otherPurchasable)
        {
            if (otherPurchasable is not LicenseScriptableObject)
                return;

            //get newly unlocked products after buying a new license
            LicenseScriptableObject license = otherPurchasable as LicenseScriptableObject;
            List<ProductScriptableObject> items = ItemDatabase.GetProductsByLicense(license.id);

            //instantiate new products after the last one for that level
            for (int i = 0; i < items.Count; i++)
            {
                //level not reached yet
                if (items[i].requiredLevel > maxLevel)
                    continue;

                UIShopItem shopItem = shopItems.Find(x => x.purchasable.id == items[i].id);
                shopItem?.lockedOverlay.SetActive(false);
            }
        }


        //instantiate UIShopItem instance in this category
        private GameObject InstantiateItem(PurchasableScriptableObject purchasable)
        {
            if (string.IsNullOrEmpty(purchasable.category))
                purchasable.category = "default";

            Transform categoryPosition = container;
            if (containerPrefab != null && !containerList.ContainsKey(purchasable.category))
            {
                GameObject newCategory = Instantiate(containerPrefab, container, false);
                containerList.Add(purchasable.category, newCategory.transform.GetChild(1));

                TMP_Text categoryHeader = newCategory.GetComponentInChildren<TMP_Text>();
                categoryHeader.text = purchasable.category;

                if (purchasable.category == "default")
                    categoryHeader.gameObject.SetActive(false);
            }

            categoryPosition = containerList.GetValueOrDefault(purchasable.category, categoryPosition);
            GameObject newItem = Instantiate(itemPrefab, categoryPosition, false);
            UIShopItem shopItem = newItem.GetComponent<UIShopItem>();
            shopItems.Add(shopItem);

            shopItem.Initialize(purchasable);
            return newItem;
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StoreDatabase.onLevelUpdate -= OnLevelUpdate;
            UpgradeSystem.onUpgradePurchase -= OnUpgradePurchase;
        }
    }
}
