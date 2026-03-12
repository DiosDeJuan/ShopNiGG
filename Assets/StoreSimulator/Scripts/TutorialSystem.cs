/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// System for showing sequential tutorial steps to the user and detecting their completion.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static TutorialSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when making progress on the current tutorial, providing the new step count.
        /// </summary>
        public static event Action<int> onProgressUpdate;

        /// <summary>
        /// List of tutorials that the player can complete in a fixed order.
        /// </summary>
        public List<TutorialScriptableObject> tutorials;

        //currently active tutorial, or null while inbetween steps
        private TutorialScriptableObject currentTutorial;
        //current tutorial index in the list of available tutorials
        private int currentIndex = 0;
        //current step progress, in case a step needs to be done multiple times
        private int currentCount = 0;


        //initialize references
        void Awake()
        {
            Instance = this;

            InteractionSystem.onInteractEvent += OnInteractEvent;
            DeliverySystem.onProductPurchase += OnProductPurchase;
            UpgradeSystem.onUpgradePurchase += OnUpgradePurchase;
            PlacementSystem.onPlacementEvent += OnPlacementEvent;
            StoreDatabase.onMoneyUpdate += OnMoneyUpdate;
            StoreDatabase.onLevelUpdate += OnLevelUpdate;
        }


        //subscribed to interaction event
        private void OnInteractEvent(Interactable interactable, string action)
        {
            if (currentTutorial == null || currentTutorial.type != TutorialType.Interact)
                return;

            string goName = interactable.gameObject.name.Replace("(Clone)", "");

            if ((!string.IsNullOrEmpty(currentTutorial.stringArg1) && goName != currentTutorial.stringArg1) ||
                (!string.IsNullOrEmpty(currentTutorial.stringArg2) && action != currentTutorial.stringArg2))
                return;

            CompleteStep();
        }


        //subscribed to product purchase
        private void OnProductPurchase(ProductScriptableObject product)
        {
            if (currentTutorial == null || currentTutorial.type != TutorialType.PurchaseItem)
                return;

            if ((!string.IsNullOrEmpty(currentTutorial.stringArg1) && product.id != currentTutorial.stringArg1) ||
                (!string.IsNullOrEmpty(currentTutorial.stringArg2) && product.category != currentTutorial.stringArg2))
                return;

            CompleteStep();
        }


        //subscribed to upgrade purchase
        private void OnUpgradePurchase(PurchasableScriptableObject purchasable)
        {
            if (currentTutorial == null || currentTutorial.type != TutorialType.UpgradeItem)
                return;

            if ((!string.IsNullOrEmpty(currentTutorial.stringArg1) && purchasable.id != currentTutorial.stringArg1) ||
                (!string.IsNullOrEmpty(currentTutorial.stringArg2) && purchasable.category != currentTutorial.stringArg2))
                return;

            CompleteStep();
        }


        //subscribed to placement place/collect
        private void OnPlacementEvent(ProductScriptableObject product, bool place)
        {
            if (currentTutorial == null || currentTutorial.type != TutorialType.Placement)
                return;

            int action = place ? 1 : -1;

            if ((!string.IsNullOrEmpty(currentTutorial.stringArg1) && product.id != currentTutorial.stringArg1) ||
                (!string.IsNullOrEmpty(currentTutorial.stringArg2) && product.category != currentTutorial.stringArg2) ||
                (currentTutorial.intArg1 != 0 && action != currentTutorial.intArg1))
                return;

            CompleteStep();
        }


        //subscribed to money change
        private void OnMoneyUpdate(string total, string change)
        {
            if (currentTutorial == null || currentTutorial.type != TutorialType.MoneyChange)
                return;

            long moneyChange = StoreDatabase.FromStringToLongMoney(change);

            if (moneyChange <= 0 || (currentTutorial.intArg1 > 0 && currentTutorial.intArg1 > moneyChange))
                return;

            CompleteStep();
        }


        //subscribed to level change
        private void OnLevelUpdate(int level)
        {
            if (currentTutorial == null || currentTutorial.type != TutorialType.LevelChange)
                return;

            if (currentTutorial.intArg1 > 0 && currentTutorial.intArg1 > level)
                return;

            CompleteStep();
        }


        //add progress to the current tutorial. If all steps were completed,
        //increases index and shows the next tutorial with delay
        private void CompleteStep()
        {
            currentCount++;
            onProgressUpdate?.Invoke(currentCount);

            if (currentCount < currentTutorial.requiredCount)
                return;

            currentIndex++;
            currentCount = 0;
            //this is set to null so that the player cannot complete the next
            //steps while the tutorial is not even visible on the screen
            currentTutorial = null;

            if (currentIndex < tutorials.Count)
                StartCoroutine(NextRoutine(6));
        }


        //add tutorial to UI
        private IEnumerator NextRoutine(int delay = 0)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            currentTutorial = tutorials[currentIndex];
            UIGame.AddTutorial(currentTutorial, currentCount);
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            data["currentIndex"] = currentIndex;
            data["currentCount"] = currentCount;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            //in case of a new game or empty data
            if (data == null || data.Count == 0)
            {
                //existing game where the TutorialSystem was added later on
                //skip tutorials as the player already achieved most of the tasks
                if (StoreDatabase.Instance.currentLevel > 1)
                {
                    currentIndex = tutorials.Count;
                    return;
                }

                //initialize with showing first tutorial step
                StartCoroutine(NextRoutine(2));
                return;
            }

            //resume tutorial steps from where player left off
            currentIndex = data["currentIndex"].AsInt;
            currentCount = data["currentCount"].AsInt;

            if (currentIndex < tutorials.Count)
                StartCoroutine(NextRoutine(2));
        }


        //unsubscribe from events
        void OnDestroy()
        {
            InteractionSystem.onInteractEvent -= OnInteractEvent;
            DeliverySystem.onProductPurchase -= OnProductPurchase;
            UpgradeSystem.onUpgradePurchase -= OnUpgradePurchase;
            PlacementSystem.onPlacementEvent -= OnPlacementEvent;
            StoreDatabase.onMoneyUpdate -= OnMoneyUpdate;
            StoreDatabase.onLevelUpdate -= OnLevelUpdate;
        }
    }


    /// <summary>
    /// Types of supported tutorial interactions.
    /// </summary>
    public enum TutorialType
    {
        /// <summary>
        /// Interact with any Interactable.
        /// </summary>
        Interact,

        /// <summary>
        /// Buy any ProductScriptableObject.
        /// </summary>
        PurchaseItem,

        /// <summary>
        /// Buy any PurchasableScriptableObject.
        /// </summary>
        UpgradeItem,

        /// <summary>
        /// Interact with a PlacementObject.
        /// </summary>
        Placement,

        /// <summary>
        /// React on money changes.
        /// </summary>
        MoneyChange,

        /// <summary>
        /// React on level changes.
        /// </summary>
        LevelChange
    }
}