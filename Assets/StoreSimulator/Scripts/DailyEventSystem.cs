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
    /// Manages the probability of starting a special event when the day starts.
    /// The event parameters and variables it applies to needs to be pre-defined in code.
    /// </summary>
    public class DailyEventSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static DailyEventSystem Instance { get; private set; }

        /// <summary>
        /// List of probabilities for each event to happen.
        /// The first entry is the chance to not start any event.
        /// </summary>
        public List<int> eventProbabilities = new List<int>() { 100 };

        /// <summary>
        /// Pool of events based on a MarketPriceChangeEvent.
        /// </summary>
        public List<MarketPriceChangeEvent> marketPriceChangePool = new List<MarketPriceChangeEvent>();

        /// <summary>
        /// Pool of events based on a CustomerTrafficChangeEvent.
        /// </summary>
        public List<CustomerTrafficChangeEvent> customerTrafficChangePool = new List<CustomerTrafficChangeEvent>();

        /// <summary>
        /// The event that is currently active on this day.
        /// </summary>
        public DailyEventChangeEvent currentEvent;

        //a random index value from the array of event types
        private int eventTypeIndex = 0;
        //the random index value from the array of the chosen event type
        private int changeEventIndex = 0;


        //initialize references
        void Awake()
        {
            Instance = this;

            DayCycleSystem.onDayLoaded += SelectEvent;
            DayCycleSystem.onDayFinished += OnDayFinished;
        }


        //randomly selects one from all events (or none)
        private void SelectEvent()
        {
            //finds a random event type
            int randomValue = UnityEngine.Random.Range(100, 0);
            int cumulativeProbability = 0;
            
            for (int i = 0; i < eventProbabilities.Count; i++)
            {
                cumulativeProbability += eventProbabilities[i];

                if (randomValue <= cumulativeProbability)
                {
                    eventTypeIndex = i;
                    break;
                }
            }

            //first entry = no event
            if (eventTypeIndex == 0)
                return;

            //collects the specific event pool based on the event type index
            randomValue = UnityEngine.Random.Range(100, 0);
            cumulativeProbability = 0;

            List<DailyEventChangeEvent> eventList = new List<DailyEventChangeEvent>();
            switch (eventTypeIndex)
            {
                case 1:
                    eventList.AddRange(marketPriceChangePool);
                    break;
                case 2:
                    eventList.AddRange(customerTrafficChangePool);
                    break;
            }

            //finds a random change event from the selected pool
            for (int i = 0; i < eventList.Count; i++)
            {
                cumulativeProbability += eventList[i].probability;

                if (randomValue <= cumulativeProbability)
                {
                    changeEventIndex = i;
                    break;
                }
            }

            //apply event
            currentEvent = eventList[changeEventIndex];
            currentEvent.Apply();
            UIGame.AddNotification(currentEvent.title + " event is active!", otherColor: Color.yellow);
        }


        //we have to delay the cleanup since the UpgradeSystem boosters may act on the same data set
        //so in order to give boosters time to revert their effects, we execute this slightly after
        private void OnDayFinished()
        {
            StartCoroutine(CleanUp());
        }


        //clears all references from a running event
        private IEnumerator CleanUp()
        {
            yield return new WaitForEndOfFrame();

            if (currentEvent != null)
            {
                currentEvent.Revert();
                currentEvent = null;
            }

            eventTypeIndex = 0;
            changeEventIndex = 0;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
            if (currentEvent == null)
                return data.ToString();

            if (currentEvent is MarketPriceChangeEvent)
            {
                MarketPriceChangeEvent marketPriceEvent = currentEvent as MarketPriceChangeEvent;
                JSONNode priceData = new JSONArray();
                foreach (KeyValuePair<string, long> pair in marketPriceEvent.previousPrices)
                {
                    JSONNode element = new JSONObject();
                    element[pair.Key] = new JSONNumber(pair.Value);
                    priceData.Add(element);
                }

                data["eventData"] = priceData;
            }

            if (currentEvent is CustomerTrafficChangeEvent)
            {
                CustomerTrafficChangeEvent customerTrafficEvent = currentEvent as CustomerTrafficChangeEvent;
                data["eventData"] = customerTrafficEvent.previousTraffic;
            }

            data["eventTypeIndex"] = eventTypeIndex;
            data["changeEventIndex"] = changeEventIndex;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            eventTypeIndex = data["eventTypeIndex"].AsInt;
            changeEventIndex = data["changeEventIndex"].AsInt;

            switch (eventTypeIndex)
            {
                case 1:
                    currentEvent = marketPriceChangePool[changeEventIndex];
                    break;
                case 2:
                    currentEvent = customerTrafficChangePool[changeEventIndex];
                    break;
            }

            if (currentEvent is MarketPriceChangeEvent)
            {
                MarketPriceChangeEvent marketPriceEvent = currentEvent as MarketPriceChangeEvent;
                marketPriceEvent.previousPrices.Clear();
                JSONArray pricesArray = data["eventData"].AsArray;
                foreach (JSONNode node in pricesArray)
                {
                    foreach (KeyValuePair<string, JSONNode> pair in node.AsObject)
                    {
                        marketPriceEvent.previousPrices.Add(pair.Key, pair.Value.AsLong);
                    }
                }
            }

            if (currentEvent is CustomerTrafficChangeEvent)
            {
                CustomerTrafficChangeEvent customerTrafficEvent = currentEvent as CustomerTrafficChangeEvent;
                customerTrafficEvent.previousTraffic = data["eventData"].AsInt;
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            DayCycleSystem.onDayLoaded -= SelectEvent;
            DayCycleSystem.onDayFinished -= OnDayFinished;
        }
    }


    /// <summary>
    /// An event to change product prices in the product catalog,
    /// making them cheaper or more expensive for the player to replenish.
    /// </summary>
    [Serializable]
    public class MarketPriceChangeEvent : DailyEventChangeEvent
    {
        /// <summary>
        /// Describes how many products should be affected.
        /// </summary>
        public int productCount;
        
        /// <summary>
        /// Whether the price change should be reverted on day finish, or stay persistent.
        /// </summary>
        public bool resetAfterDay;

        /// <summary>
        /// A library of product prices before the change event, in case we need to revert them.
        /// </summary>
        [HideInInspector]
        public Dictionary<string, long> previousPrices = new Dictionary<string, long>();


        /// <summary>
        /// Override to specify what should happen when applying the event.
        /// Iterate over products and apply price changes.
        /// </summary>
        public override void Apply()
        {
            List<PurchasableScriptableObject> products = new List<PurchasableScriptableObject>();
            List<PurchasableScriptableObject> selectedProducts = new List<PurchasableScriptableObject>();

            int currentLevel = StoreDatabase.Instance.currentLevel;
            for(int i = 0; i <= currentLevel; i++)
                products.AddRange(ItemDatabase.GetByLevel(typeof(ProductScriptableObject), i));

            for(int i = 0; i < productCount; i++)
            {
                int index = UnityEngine.Random.Range(0, products.Count);
                selectedProducts.Add(products[index]);
                products.RemoveAt(index);
            }

            for(int i = 0; i < selectedProducts.Count; i++)
            {
                if (resetAfterDay)
                {
                    previousPrices.Clear();
                    previousPrices.Add(selectedProducts[i].id, selectedProducts[i].buyPrice);
                }

                selectedProducts[i].buyPrice = Mathf.FloorToInt((1 + changePercentage) * selectedProducts[i].buyPrice);
            }
        }


        /// <summary>
        /// Override to specify how to revert the event.
        /// Iterate over previous prices and apply them.
        /// </summary>
        public override void Revert()
        {
            foreach(KeyValuePair<string, long> pair in previousPrices)
            {
                ItemDatabase.GetById(typeof(ProductScriptableObject), pair.Key).buyPrice = pair.Value;
            }
        }
    }


    /// <summary>
    /// An event to change the customer spawn rate,
    /// driving more or less customers per minute into the store.
    /// </summary>
    [Serializable]
    public class CustomerTrafficChangeEvent : DailyEventChangeEvent
    {
        /// <summary>
        /// The previous spawn rate in case we need to restore it.
        /// </summary>
        [HideInInspector]
        public int previousTraffic;


        /// <summary>
        /// Override to specify what should happen when applying the event.
        /// Change the spawnRate variable on the CustomerSystem.
        /// </summary>
        public override void Apply()
        {
            previousTraffic = CustomerSystem.Instance.spawnRate;
            CustomerSystem.Instance.spawnRate = Mathf.FloorToInt((1 + changePercentage) * previousTraffic);

            if (CustomerSystem.Instance.spawnRate < 1)
                CustomerSystem.Instance.spawnRate = 1;
        }


        /// <summary>
        /// Override to specify how to revert the event.
        /// Apply previous spawnRate value.
        /// </summary>
        public override void Revert()
        {
            CustomerSystem.Instance.spawnRate = previousTraffic;
        }
    }


    /// <summary>
    /// Base class defining general variables and method for a ChangeEvent.
    /// </summary>
    [Serializable]
    public abstract class DailyEventChangeEvent
    {
        /// <summary>
        /// Name of the event, if displayed in-game.
        /// </summary>
        public string title;

        /// <summary>
        /// Chance for the event to happen within the same event type pool.
        /// </summary>
        [Range(1, 100)]
        public int probability;
        
        /// <summary>
        /// Percentage of change for increasing or decreasing a specific value, therefore can be negative.
        /// </summary>
        public float changePercentage;


        /// <summary>
        /// Implement this method to define what should happen when the event occurs.
        /// </summary>
        public virtual void Apply() {}

        /// <summary>
        /// Implement this method to define necessary code to revert the event.
        /// </summary>
        public virtual void Revert() {}
    }
}