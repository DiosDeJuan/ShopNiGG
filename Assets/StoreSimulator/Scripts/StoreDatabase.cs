/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Linq;
using System.Globalization;
using UnityEngine;
using SimpleJSON;
using System.Collections.Generic;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Database storing all store related variables for quick access.
    /// Also keeps a list of available PlacementObjects per product and price formatting methods.
    /// </summary>
    public class StoreDatabase : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static StoreDatabase Instance { get; private set; }

        /// <summary>
        /// Culture that should be used for formatting currency.
        /// </summary>
        public static CultureInfo cultureInfo = new CultureInfo("en-US");

        /// <summary>
        /// Event fired when the amount of money changes. Current value, value change.
        /// </summary>
        public static event Action<string, string> onMoneyUpdate;

        /// <summary>
        /// Event fired when the amount of experience changes. Current value, value change.
        /// </summary>
        public static event Action<long, long> onExperienceUpdate;

        /// <summary>
        /// Event fired when the level value changes. Current value.
        /// </summary>
        public static event Action<int> onLevelUpdate;

        /// <summary>
        /// The name of the store displayed above the entry.
        /// </summary>
        public TMP_Text storeName;

        /// <summary>
        /// The location of the store entry, i.e. the store door customers should first walk to after spawn.
        /// </summary>
        public Transform storeEntry;

        /// <summary>
        /// The clip to play when a customer enters the store, or none if not set.
        /// </summary>
        public AudioClip entryClip;

        /// <summary>
        /// Money in cents the player should start with when creating a new game.
        /// </summary>
        public long startMoney;

        /// <summary>
        /// Different levels of experience. Each value describes the experience necessary to reach that level.
        /// </summary>
        public long[] levelXP;

        /// <summary>
        /// The current amount of player money in cents.
        /// </summary>
        public long currentMoney { get; private set; }

        /// <summary>
        /// The current amount of experience the player has.
        /// </summary>
        public long currentXP { get; private set; }

        /// <summary>
        /// The current level reached by the player.
        /// </summary>
        public int currentLevel { get; private set; }

        //cache references to CashDesk/SelfCheckout instances in the scene
        private List<CheckoutObject> checkouts = new List<CheckoutObject>();
        //provide a list of available PlacementObject for each product that was placed in the scene
        private Dictionary<ProductScriptableObject, List<PlacementObject>> productPlacements = new Dictionary<ProductScriptableObject, List<PlacementObject>>();


        //initialize references
        void Awake()
        {
            Instance = this;

            cultureInfo.NumberFormat.CurrencyNegativePattern = 1;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }


        //initialize variables
        void Start()
        {
            checkouts = FindObjectsByType<CheckoutObject>(FindObjectsSortMode.None).ToList();
        }


        /// <summary>
        /// Return the current player money in a formatted currency string.
        /// </summary>
        public static string GetMoneyString()
        {
            return FromLongToStringMoney(Instance.currentMoney);
        }


        /// <summary>
        /// Returns the level number as formatted string.
        /// </summary>
        public static string GetLevelString()
        {
            return "Level " + Instance.currentLevel;
        }


        /// <summary>
        /// Convert the passed in amount of money in cents to a currency string.
        /// </summary>
        public static string FromLongToStringMoney(long money)
        {
            return (money / 100m).ToString("C", cultureInfo);
        }


        /// <summary>
        /// Convert the passed in currency string to a money value in cents.
        /// </summary>
        public static long FromStringToLongMoney(string money)
        {
            decimal value = 0;
            decimal.TryParse(money, NumberStyles.Currency, cultureInfo, out value);
            return (long)(value * 100m);
        }


        /// <summary>
        /// Calculate current level based on total experience passed in.
        /// </summary>
        public static int GetLevel(long xp)
        {
            long totalLevelXP = 0;
            for(int i = 0; i < Instance.levelXP.Length; i++)
            {
                totalLevelXP += Instance.levelXP[i];

                if (xp < totalLevelXP)
                    return i;
            }

            return Instance.levelXP.Length;
        }


        /// <summary>
        /// Calculate total experience bounds for a level value passed in.
        /// </summary>
        public static Vector2 GetExperienceRange(int level)
        {
            if (level >= Instance.levelXP.Length)
                return Vector2.zero;

            long totalLevelXP = 0;
            for(int i = 0; i <= level; i++)
            {
                totalLevelXP += Instance.levelXP[i];
            }

            return new Vector2(totalLevelXP - Instance.levelXP[level], totalLevelXP);
        }


        /// <summary>
        /// Returns random CashDesk reference out of the available ones.
        /// </summary>
        public static CheckoutObject GetRandomCheckout()
        {
            if (Instance.checkouts.Count == 0)
                return null;
            
            return Instance.checkouts[UnityEngine.Random.Range(0, Instance.checkouts.Count)];
        }


        /// <summary>
        /// Returns the current store name either being default or a customized name.
        /// </summary>
        public static string GetStoreName()
        {
            return Instance.storeName.text;
        }

        
        /// <summary>
        /// Applies a new store name to the corresponding text display.
        /// This usually comes from the UIShopDesktop script, input field on Customization screen.
        /// </summary>
        public static void SetStoreName(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                inputText = "SUPERMARKET";

            Instance.storeName.text = inputText;
        }


        /// <summary>
        /// Checks whether the player has more money than the amount passed in.
        /// </summary>
        public static bool CanPurchase(long amount)
        {
            return Instance.currentMoney >= amount;
        }


        /// <summary>
        /// Method for increasing or decreasing the player's money, therefore the value can be negative.
        /// </summary>
        public static void AddRemoveMoney(long change)
        {
            //allow getting past negative 0 and do not clamp the change, disabled by default
            //since that could be a feature e.g. lose the game with 3 negative days in a row
            /*
            if (Instance.currentMoney > 0 && change < 0 && Mathf.Abs(change) > Instance.currentMoney)
                change = Instance.currentMoney * -1;
            else if (Instance.currentMoney == 0 && change < 0)
                return;
            */

            Instance.currentMoney = ClampLong(Instance.currentMoney, change);
            onMoneyUpdate?.Invoke(FromLongToStringMoney(Instance.currentMoney), FromLongToStringMoney(change));
        }


        /// <summary>
        /// Method for increasing or decreasing the player's experience, therefore the value can be negative.
        /// </summary>
        public static void AddRemoveExperience(long xp)
        {
            long newXP = ClampLong(Instance.currentXP, xp);
            int newLevel = GetLevel(newXP);

            //skip if we are already at max level
            if (Instance.currentLevel >= Instance.levelXP.Length)
                return;

            //avoid downgrading, clamp it to mininum XP for that level
            if (newLevel < Instance.currentLevel)
            {
                Instance.currentXP = (long)GetExperienceRange(Instance.currentLevel).x;
                onExperienceUpdate?.Invoke(Instance.currentXP, xp);
                return;
            }

            Instance.currentXP = newXP;
            onExperienceUpdate?.Invoke(Instance.currentXP, xp);

            if (Instance.currentLevel != newLevel)
            {
                Instance.currentLevel = newLevel;
                onLevelUpdate?.Invoke(newLevel);
                UIGame.AddNotification("Congratulations!\nYou have reached level " + newLevel, otherColor: Color.green);
            }
        }


        /// <summary>
        /// Add a new (portable) CheckoutObject to the list of available checkouts.
        /// </summary>
        public void AddCheckoutObject(CheckoutObject checkout)
        {
            if (checkouts.Contains(checkout))
                return;

            checkouts.Add(checkout);
        }


        /// <summary>
        /// Remove an existing CheckoutObject from the list of available checkouts.
        /// </summary>
        public void RemoveCheckoutObject(CheckoutObject checkout)
        {
            checkouts.Remove(checkout);
        }


        /// <summary>
        /// Add a new PlacementObject to the selection available for a specific product.
        /// </summary>
        public void AddProductPlacement(ProductScriptableObject product, PlacementObject placement)
        {
            if (!productPlacements.ContainsKey(product))
                productPlacements.Add(product, new List<PlacementObject>());

            productPlacements[product].Add(placement);
        }


        /// <summary>
        /// Returns a random PlacementObject in the scene for a specific product.
        /// </summary>
        public PlacementObject GetProductPlacementRandom(ProductScriptableObject product)
        {
            if (productPlacements.ContainsKey(product))
                return productPlacements[product][UnityEngine.Random.Range(0, productPlacements[product].Count)];

            return null;
        }


        /// <summary>
        /// Remove an existing PlacementObject from the selection available for a specific product.
        /// </summary>
        public void RemoveProductPlacement(ProductScriptableObject product, PlacementObject placement)
        {
            productPlacements[product].Remove(placement);

            if (productPlacements[product].Count == 0)
                productPlacements.Remove(product);
        }


        /// <summary>
        /// Return the store-wide inventory count for a specific product in stock.
        /// </summary>
        public int GetProductInventoryCount(ProductScriptableObject product)
        {
            if (!productPlacements.ContainsKey(product))
                return 0;

            int stockCount = 0;
            foreach(PlacementObject placement in productPlacements[product])
                stockCount += placement.count;
            
            return stockCount;
        }


        //add or subtract value while respecting min and max limits
        private static long ClampLong(long baseValue, long change)
        {
            if (baseValue > 0 && change > 0 && baseValue + change < 0) return long.MaxValue;
            if (baseValue < 0 && change < 0 && baseValue + change > 0) return long.MinValue;
            
            return baseValue + change;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            data["storeName"] = storeName.text;
            data["currentMoney"] = currentMoney;
            data["currentXP"] = currentXP;
            data["currentLevel"] = currentLevel;
            
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
            {
                currentMoney = startMoney;
                return;
            }

            storeName.text = data["storeName"].Value;
            currentMoney = data["currentMoney"].AsLong;
            currentXP = data["currentXP"].AsLong;
            currentLevel = data["currentLevel"].AsInt;
        }
    }
}