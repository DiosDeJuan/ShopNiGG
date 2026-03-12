/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// This component allows buying one-off items for extending product catalogs/space or customizing the store.
    /// All of the ScriptableObjects subscribe to the onUpgradePurchase event to further define the outcome.
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static UpgradeSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when an upgrade/extension/decoration has been bought.
        /// </summary>
        public static event Action<PurchasableScriptableObject> onUpgradePurchase;

        /// <summary>
        /// The speed used when moving wall/objects out of view after an upgrade from a ExpansionScriptableObject.
        /// </summary>
        public float lerpSpeed = 2f;

        /// <summary>
        /// The material a customized wall texture should be applied to from a DecorationScriptableObject.
        /// </summary>
        public Material wallMaterial;

        /// <summary>
        /// The material a customized floor texture should be applied to from a DecorationScriptableObject.
        /// </summary>
        public Material floorMaterial;

        //the negative height disappearing objects should move to
        private int belowGroundHeight = -10;


        //initialize references
        void Awake()
        {
            Instance = this;

            DayCycleSystem.onDayFinished += OnDayFinished;
        }


        //set the initial game states after loading
        void Start()
        {
            //find a purchased decoration per type and apply it to the store
            List<PurchasableScriptableObject> decorations = ItemDatabase.GetByType(typeof(DecorationScriptableObject));
            foreach (DecorationType type in Enum.GetValues(typeof(DecorationType)))
            {
                DecorationScriptableObject isSelected = decorations.OfType<DecorationScriptableObject>()
                                                                    .FirstOrDefault(x => x.decorationType == type && x.isPurchased);
                if (isSelected != null)
                    ApplyDecoration(isSelected);
            }
        }


        /// <summary>
        /// Called from a UIShopItem instance when trying to purchase that specific item.
        /// Does an internal check for money and then unlocks the asset.
        /// </summary>
        public static void Purchase(PurchasableScriptableObject purchasable)
        {
            if (!StoreDatabase.CanPurchase(purchasable.buyPrice))
            {
                UIGame.Instance.ShowMessage("Not enough money to purchase this object");
                return;
            }

            StoreDatabase.AddRemoveMoney(-purchasable.buyPrice);

            switch (purchasable)
            {
                case ExpansionScriptableObject:
                    (purchasable as ExpansionScriptableObject).isPurchased = true;
                    break;
                case LicenseScriptableObject:
                    (purchasable as LicenseScriptableObject).isPurchased = true;
                    break;
                case DecorationScriptableObject:
                    DecorationScriptableObject decoration = purchasable as DecorationScriptableObject;
                    ApplyDecoration(decoration);
                    break;
                case BoosterScriptableObject:
                    BoosterScriptableObject booster = purchasable as BoosterScriptableObject;
                    ApplyBooster(booster);
                    break;
            }

            onUpgradePurchase?.Invoke(purchasable);
        }


        /// <summary>
        /// Applies the decoration texture to the store based on the DecorationType.
        /// Unselects any other existing selection on the same DecorationType.
        /// </summary>
        public static void ApplyDecoration(DecorationScriptableObject decoration)
        {
            DecorationScriptableObject isSelected = ItemDatabase.GetByType(typeof(DecorationScriptableObject)).OfType<DecorationScriptableObject>()
                                                    .FirstOrDefault(x => x.decorationType == decoration.decorationType && x.isPurchased);

            if (isSelected != null && decoration != isSelected)
                isSelected.isPurchased = false;

            decoration.isPurchased = true;
            switch (decoration.decorationType)
            {
                case DecorationType.Wall:
                    if (Instance.wallMaterial == null) break;
                    Instance.wallMaterial.mainTexture = decoration.texture;
                    break;
                case DecorationType.Floor:
                    if (Instance.floorMaterial == null) break;
                    Instance.floorMaterial.mainTexture = decoration.texture;
                    break;
            }
        }


        /// <summary>
        /// Applies the defined effect to the game based on the Booster ID bought.
        /// When the day is finished some of the boosters need to be reverted.
        /// </summary>
        public static void ApplyBooster(BoosterScriptableObject booster, bool toApply = true)
        {
            booster.isPurchased = toApply;
            int multiplier = toApply ? 1 : -1;

            switch (booster.id)
            {
                case "0":
                    CustomerSystem.Instance.spawnRate += multiplier * 2;
                    break;
            }
        }


        /// <summary>
        /// Move the referenced transform on the negative y-axis, i.e. below ground.
        /// For example, this is for letting walls disappear when a store expansion has been bought. 
        /// </summary>
        public static IEnumerator MoveBelowGround(Transform item)
        {
            float lerpProgress = 0f;
            Vector3 startingPosition = item.position;
            Vector3 targetPosition = new Vector3(startingPosition.x, Instance.belowGroundHeight, startingPosition.z);

            while (lerpProgress < 1f)
            {
                lerpProgress += Instance.lerpSpeed * Time.deltaTime;
                item.position = Vector3.Lerp(startingPosition, targetPosition, lerpProgress);
                yield return null;
            }

            SetBelowGround(item);
        }


        /// <summary>
        /// Move the referenced transform on the negative y-axis instantly, without animation.
        /// </summary>
        public static void SetBelowGround(Transform item)
        {
            Vector3 startingPosition = item.position;
            Vector3 targetPosition = new Vector3(startingPosition.x, Instance.belowGroundHeight, startingPosition.z);

            item.position = targetPosition;
            item.gameObject.SetActive(false);
        }


        //clears all daily boosters and set them to unpurchased again
        private void OnDayFinished()
        {
            List<PurchasableScriptableObject> boosters = ItemDatabase.GetByType(typeof(BoosterScriptableObject));
            foreach (PurchasableScriptableObject purchasable in boosters)
            {
                BoosterScriptableObject booster = purchasable as BoosterScriptableObject;
                if (booster.isPurchased)
                    ApplyBooster(booster, false);
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            DayCycleSystem.onDayFinished -= OnDayFinished;
        }
    }
}