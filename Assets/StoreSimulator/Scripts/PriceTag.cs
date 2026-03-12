/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Label component associated to a placement to allow changing the related product price on interaction.
    /// </summary>
    public class PriceTag : Interactable
    {
        /// <summary>
        /// Reference to the placement this PriceTag should be assigned to.
        /// </summary>
        public PlacementObject placement;

        /// <summary>
        /// Displaying the icon of the product.
        /// </summary>
        public Image image;

        /// <summary>
        /// Displaying the current amount of products in the placement.
        /// </summary>
        public TMP_Text fillCount;

        /// <summary>
        /// Displaying the store-wide price of the product.
        /// </summary>
        public TMP_Text price;


        //initialize references
        void Awake()
        {          
            placement.onCountChanged += OnCountChanged;
            placement.onProductChanged += OnProductChanged;
            ItemDatabase.onStorePriceUpdate += OnPriceUpdate;

            gameObject.SetActive(false);
        }


        /// <summary>
        /// Interactable override, adding UI action.
        /// </summary>
        public override void OnBecameFocus()
        {
            UIGame.AddAction("LeftClick", "Set Price", true);
        }


        /// <summary>
        /// Interactable override, react on player interaction.
        /// This object open a separate UI window for further inputs.
        /// </summary>
        public override bool Interact(string actionName)
        {
            if (actionName != "LeftClick") return false;

            UIGame.Instance.priceLabelWindow.Initialize(placement.product);
            return true;
        }


        /// <summary>
        /// Interactable override, removing UI action.
        /// </summary>
        public override void OnLostFocus()
        {
            UIGame.RemoveAction("LeftClick");
        }


        //subscribed to the placement to always display the current item count
        private void OnCountChanged(int count)
        {
            fillCount.text = count.ToString();
        }


        //subscribed to the placement to display the current product icon and price
        private void OnProductChanged(ProductScriptableObject newProduct)
        {
            if (newProduct == null)
            {
                gameObject.SetActive(false);
                return;
            }

            image.sprite = newProduct.icon;
            price.text = StoreDatabase.FromLongToStringMoney(newProduct.storePrice);
            gameObject.SetActive(true);
        }


        //update the price displayed whenever it changes
        private void OnPriceUpdate(ProductScriptableObject product, string newPrice)
        {
            if (placement.product != product)
                return;

            price.text = newPrice;
        }


        //unsubscribe from events
        void OnDestroy()
        {
            placement.onCountChanged -= OnCountChanged;
            placement.onProductChanged -= OnProductChanged;
            ItemDatabase.onStorePriceUpdate -= OnPriceUpdate;
        }
    }
}
