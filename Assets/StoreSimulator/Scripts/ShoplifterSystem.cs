using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Manages shoplifter spawning, behavior, and detection.
    /// Spawns 1 shoplifter per 25 customers (RQF13). Includes three types: Common, Expert, Fast (RQF15).
    /// Difficulty scales with store expansion (RQF16). Provides visual/audio notifications (RQF19)
    /// and maintains a daily robbery report (RQF20).
    /// Addresses RQF13 through RQF20.
    /// </summary>
    public class ShoplifterSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static ShoplifterSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when a shoplifter is detected in the store. (RQF19)
        /// </summary>
        public static event Action<ShoplifterType> onShoplifterDetected;

        /// <summary>
        /// Event fired when a shoplifter escapes. (RQF17)
        /// </summary>
        public static event Action<long> onShoplifterEscaped;

        /// <summary>
        /// Event fired when a shoplifter is caught. (RQF17)
        /// </summary>
        public static event Action<ShoplifterType> onShoplifterCaught;

        /// <summary>
        /// Number of customers before a shoplifter appears. (RQF13: 1 per 25)
        /// </summary>
        public int customersPerShoplifter = 25;

        /// <summary>
        /// Audio clip to play when a shoplifter is detected. (RQF19)
        /// </summary>
        public AudioClip detectionClip;

        /// <summary>
        /// Audio clip to play when a shoplifter escapes.
        /// </summary>
        public AudioClip escapeClip;

        /// <summary>
        /// Audio clip to play when a shoplifter is caught.
        /// </summary>
        public AudioClip caughtClip;

        /// <summary>
        /// Probability weights for each shoplifter type [Common, Expert, Fast]. (RQF15)
        /// </summary>
        public Vector3 typeWeights = new Vector3(60f, 25f, 15f);

        /// <summary>
        /// Difficulty multiplier that increases with store expansions. (RQF16)
        /// </summary>
        public float difficultyMultiplier { get; private set; } = 1f;

        //counter of customers since last shoplifter
        private int customerCounter = 0;

        //daily robbery history for the report (RQF20)
        private List<RobberyRecord> dailyRobberyHistory = new List<RobberyRecord>();

        //total money lost today to shoplifters
        private long dailyMoneyLost = 0;

        //total shoplifters caught today
        private int dailyCaught = 0;

        //total shoplifters escaped today
        private int dailyEscaped = 0;


        //initialize references
        void Awake()
        {
            Instance = this;

            CustomerSystem.onCustomerLeft += OnCustomerLeft;
            DayCycleSystem.onDayLoaded += OnDayLoaded;
            UpgradeSystem.onUpgradePurchase += OnUpgradePurchased;
        }


        //reset daily statistics on new day
        private void OnDayLoaded()
        {
            dailyRobberyHistory.Clear();
            dailyMoneyLost = 0;
            dailyCaught = 0;
            dailyEscaped = 0;
            customerCounter = 0;
        }


        //track customer count for shoplifter spawning (RQF13)
        private void OnCustomerLeft(bool wasHappy)
        {
            customerCounter++;

            if (customerCounter >= customersPerShoplifter)
            {
                customerCounter = 0;
                SpawnShoplifter();
            }
        }


        //increase difficulty when the store expands (RQF16)
        private void OnUpgradePurchased(PurchasableScriptableObject purchasable)
        {
            if (purchasable is ExpansionScriptableObject)
            {
                difficultyMultiplier += 0.15f;
            }
        }


        /// <summary>
        /// Determines which type of shoplifter should spawn based on weights and difficulty. (RQF15, RQF16)
        /// </summary>
        private void SpawnShoplifter()
        {
            ShoplifterType type = GetRandomShoplifterType();

            //notify the UI with a visual and audio alert (RQF19)
            onShoplifterDetected?.Invoke(type);
            AudioSystem.Play2D(detectionClip);

            string typeName = type.ToString();
            UIGame.AddNotification("Shoplifter detected!\nType: " + typeName, otherColor: Color.red);

            //try auto-arrest first via security system (RQF18)
            if (SecuritySystem.TryAutomaticArrest())
            {
                HandleShoplifterCaught(type, 0);
                UIGame.AddNotification("Security caught the " + typeName + " shoplifter!", otherColor: Color.green);
                return;
            }

            //if no security or auto-arrest failed, simulate theft (RQF10, RQF14, RQF17)
            SimulateTheft(type);
        }


        /// <summary>
        /// Simulates the theft attempt. Products are stolen prioritizing expensive ones. (RQF14)
        /// Player can intercept manually; if they escape the player loses money. (RQF17)
        /// </summary>
        private void SimulateTheft(ShoplifterType type)
        {
            //calculate stolen value based on type and difficulty
            long stolenValue = CalculateStolenValue(type);

            //determine escape chance based on shoplifter type
            int escapeChance = GetEscapeChance(type);
            bool escaped = UnityEngine.Random.Range(0, 100) < escapeChance;

            if (escaped)
            {
                HandleShoplifterEscaped(type, stolenValue);
            }
            else
            {
                HandleShoplifterCaught(type, stolenValue);
            }
        }


        /// <summary>
        /// Handle a shoplifter that was caught. Products are recovered. (RQF17)
        /// </summary>
        public static void HandleShoplifterCaught(ShoplifterType type, long recoveredValue)
        {
            Instance.dailyCaught++;

            Instance.dailyRobberyHistory.Add(new RobberyRecord
            {
                type = type,
                wasCaught = true,
                stolenValue = 0,
                time = DayCycleSystem.GetTimeString()
            });

            onShoplifterCaught?.Invoke(type);
            AudioSystem.Play2D(Instance.caughtClip);
        }


        /// <summary>
        /// Handle a shoplifter that escaped. Player loses money. (RQF17)
        /// </summary>
        private void HandleShoplifterEscaped(ShoplifterType type, long stolenValue)
        {
            dailyEscaped++;
            dailyMoneyLost += stolenValue;

            StoreDatabase.AddRemoveMoney(-stolenValue);

            dailyRobberyHistory.Add(new RobberyRecord
            {
                type = type,
                wasCaught = false,
                stolenValue = stolenValue,
                time = DayCycleSystem.GetTimeString()
            });

            onShoplifterEscaped?.Invoke(stolenValue);
            AudioSystem.Play2D(escapeClip);

            UIGame.AddNotification("Shoplifter escaped!\nLost: " + StoreDatabase.FromLongToStringMoney(stolenValue), otherColor: Color.red);
        }


        /// <summary>
        /// Returns a random shoplifter type based on the probability weights. (RQF15)
        /// </summary>
        private ShoplifterType GetRandomShoplifterType()
        {
            float total = typeWeights.x + typeWeights.y + typeWeights.z;
            float roll = UnityEngine.Random.Range(0f, total);

            if (roll < typeWeights.x) return ShoplifterType.Common;
            if (roll < typeWeights.x + typeWeights.y) return ShoplifterType.Expert;
            return ShoplifterType.Fast;
        }


        /// <summary>
        /// Calculates the value of stolen products based on shoplifter type and difficulty. (RQF14, RQF16)
        /// Shoplifters prioritize expensive products.
        /// </summary>
        private long CalculateStolenValue(ShoplifterType type)
        {
            //base stolen value scales with shoplifter type
            long baseValue;
            switch (type)
            {
                case ShoplifterType.Expert:
                    baseValue = UnityEngine.Random.Range(300, 1500);
                    break;
                case ShoplifterType.Fast:
                    baseValue = UnityEngine.Random.Range(100, 800);
                    break;
                default: //Common
                    baseValue = UnityEngine.Random.Range(200, 1000);
                    break;
            }

            //scale by difficulty multiplier (RQF16)
            return (long)(baseValue * difficultyMultiplier);
        }


        /// <summary>
        /// Returns the escape chance for a given shoplifter type.
        /// Higher values for Expert and Fast types.
        /// </summary>
        private int GetEscapeChance(ShoplifterType type)
        {
            switch (type)
            {
                case ShoplifterType.Expert:
                    return 70;
                case ShoplifterType.Fast:
                    return 80;
                default: //Common
                    return 50;
            }
        }


        /// <summary>
        /// Returns the daily robbery history for the report. (RQF20)
        /// </summary>
        public static List<RobberyRecord> GetDailyRobberyHistory()
        {
            return new List<RobberyRecord>(Instance.dailyRobberyHistory);
        }


        /// <summary>
        /// Returns the total money lost to shoplifters today.
        /// </summary>
        public static long GetDailyMoneyLost()
        {
            return Instance.dailyMoneyLost;
        }


        /// <summary>
        /// Returns the count of shoplifters caught today.
        /// </summary>
        public static int GetDailyCaughtCount()
        {
            return Instance.dailyCaught;
        }


        /// <summary>
        /// Returns the count of shoplifters who escaped today.
        /// </summary>
        public static int GetDailyEscapedCount()
        {
            return Instance.dailyEscaped;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode.
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            data["difficultyMultiplier"] = difficultyMultiplier;
            data["dailyMoneyLost"] = dailyMoneyLost;
            data["dailyCaught"] = dailyCaught;
            data["dailyEscaped"] = dailyEscaped;

            JSONNode historyArray = new JSONArray();
            foreach (RobberyRecord record in dailyRobberyHistory)
            {
                JSONNode element = new JSONObject();
                element["type"] = (int)record.type;
                element["wasCaught"] = record.wasCaught;
                element["stolenValue"] = record.stolenValue;
                element["time"] = record.time;
                historyArray.Add(element);
            }
            data["robberyHistory"] = historyArray;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            difficultyMultiplier = data["difficultyMultiplier"].AsFloat;
            dailyMoneyLost = data["dailyMoneyLost"].AsLong;
            dailyCaught = data["dailyCaught"].AsInt;
            dailyEscaped = data["dailyEscaped"].AsInt;

            dailyRobberyHistory.Clear();
            JSONArray historyArray = data["robberyHistory"].AsArray;
            if (historyArray != null)
            {
                for (int i = 0; i < historyArray.Count; i++)
                {
                    dailyRobberyHistory.Add(new RobberyRecord
                    {
                        type = (ShoplifterType)historyArray[i]["type"].AsInt,
                        wasCaught = historyArray[i]["wasCaught"].AsBool,
                        stolenValue = historyArray[i]["stolenValue"].AsLong,
                        time = historyArray[i]["time"].Value
                    });
                }
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            CustomerSystem.onCustomerLeft -= OnCustomerLeft;
            DayCycleSystem.onDayLoaded -= OnDayLoaded;
            UpgradeSystem.onUpgradePurchase -= OnUpgradePurchased;
        }
    }


    /// <summary>
    /// Data structure for recording a robbery attempt in the daily report. (RQF20)
    /// </summary>
    [Serializable]
    public struct RobberyRecord
    {
        public ShoplifterType type;
        public bool wasCaught;
        public long stolenValue;
        public string time;
    }
}
