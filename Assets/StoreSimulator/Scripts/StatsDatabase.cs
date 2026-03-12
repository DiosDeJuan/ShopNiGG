/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Caches certain values from events that happened during the day.
    /// Used in the day over scene to display statistical information to the player.
    /// </summary>
    public class StatsDatabase : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static StatsDatabase Instance { get; private set; }

        /// <summary>
        /// Cache for money earned during the day.
        /// </summary>
        public long moneyEarned { get; private set; }

        /// <summary>
        /// Cache for money spent during the day.
        /// </summary>
        public long moneySpent { get; private set; }

        /// <summary>
        /// Cache for experience earned during the day. If only customers are taken into account,
        /// and customers give 1 XP, then this is the same as customerHappy - customersUnhappy.
        /// </summary>
        public long xpEarned { get; private set; }

        /// <summary>
        /// Cache for count of customers who left happy.
        /// </summary>
        public int customersHappy { get; private set; }

        /// <summary>
        /// Cache for count of customers who left unhappy.
        /// </summary>
        public int customersUnhappy { get; private set; }

        /// <summary>
        /// Cache for count of shoplifters caught during the day. (RQF20)
        /// </summary>
        public int shopliftersCaught { get; private set; }

        /// <summary>
        /// Cache for count of shoplifters who escaped during the day. (RQF20)
        /// </summary>
        public int shopliftersEscaped { get; private set; }

        /// <summary>
        /// Cache for total money lost to shoplifters during the day. (RQF20)
        /// </summary>
        public long moneyLostToTheft { get; private set; }


        //initialize references
        void Awake()
        {
            Instance = this;

            StoreDatabase.onMoneyUpdate += OnMoneyUpdate;
            StoreDatabase.onExperienceUpdate += OnExperienceUpdate;
            DayCycleSystem.onDayLoaded += OnDayLoaded;
            CustomerSystem.onCustomerLeft += OnCustomerLeft;
            ShoplifterSystem.onShoplifterCaught += OnShoplifterCaught;
            ShoplifterSystem.onShoplifterEscaped += OnShoplifterEscaped;
        }


        //subscribed to day event
        private void OnDayLoaded()
        {
            moneyEarned = moneySpent = 0;
            xpEarned = 0;
            customersHappy = customersUnhappy = 0;
            shopliftersCaught = shopliftersEscaped = 0;
            moneyLostToTheft = 0;
        }


        //subscribed to money change
        private void OnMoneyUpdate(string current, string changeString)
        {
            long change = StoreDatabase.FromStringToLongMoney(changeString);

            if (change > 0) moneyEarned += change;
            else moneySpent += change;
        }


        //subscribed to experience change
        private void OnExperienceUpdate(long current, long change)
        {
            //if (change > 0) if only positive values should count
            xpEarned += change;
        }


        //subscribed to customer event
        private void OnCustomerLeft(bool wasHappy)
        {
            if (wasHappy) customersHappy++;
            else customersUnhappy++;
        }


        //subscribed to shoplifter caught event (RQF20)
        private void OnShoplifterCaught(ShoplifterType type)
        {
            shopliftersCaught++;
        }


        //subscribed to shoplifter escaped event (RQF20)
        private void OnShoplifterEscaped(long stolenValue)
        {
            shopliftersEscaped++;
            moneyLostToTheft += stolenValue;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
                
            data["moneyEarned"] = moneyEarned;
            data["moneySpent"] = moneySpent;
            data["xpEarned"] = xpEarned;
            data["customersHappy"] = customersHappy;
            data["customersUnhappy"] = customersUnhappy;
            data["shopliftersCaught"] = shopliftersCaught;
            data["shopliftersEscaped"] = shopliftersEscaped;
            data["moneyLostToTheft"] = moneyLostToTheft;
            
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            moneyEarned = data["moneyEarned"].AsLong;
            moneySpent = data["moneySpent"].AsLong;
            xpEarned = data["xpEarned"].AsLong;
            customersHappy = data["customersHappy"].AsInt;
            customersUnhappy = data["customersUnhappy"].AsInt;
            shopliftersCaught = data["shopliftersCaught"].AsInt;
            shopliftersEscaped = data["shopliftersEscaped"].AsInt;
            moneyLostToTheft = data["moneyLostToTheft"].AsLong;
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StoreDatabase.onMoneyUpdate -= OnMoneyUpdate;
            StoreDatabase.onExperienceUpdate -= OnExperienceUpdate;
            DayCycleSystem.onDayLoaded -= OnDayLoaded;
            CustomerSystem.onCustomerLeft -= OnCustomerLeft;
            ShoplifterSystem.onShoplifterCaught -= OnShoplifterCaught;
            ShoplifterSystem.onShoplifterEscaped -= OnShoplifterEscaped;
        }
    }
}