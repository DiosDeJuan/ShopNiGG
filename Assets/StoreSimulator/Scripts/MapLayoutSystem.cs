/*  This file is part of the "ShopMaster" project.
 *  Manages the supermarket map layout, zone tracking, modular expansion, and optimization. */

using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Manages the supermarket map layout including zone definitions, modular expansion tracking,
    /// and runtime zone queries. Designed for a 900 m² total terrain with modular growth.
    /// </summary>
    public class MapLayoutSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static MapLayoutSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when a zone expansion module is activated.
        /// Provides the zone type and the new expansion count.
        /// </summary>
        public static event Action<MapZone, int> onZoneExpanded;

        /// <summary>
        /// Total terrain area in square meters.
        /// </summary>
        public const float TotalTerrainArea = 900f;

        /// <summary>
        /// Initial sales floor area in square meters.
        /// </summary>
        public const float InitialSalesArea = 192f;

        /// <summary>
        /// Initial storage area in square meters.
        /// </summary>
        public const float InitialStorageArea = 32f;

        /// <summary>
        /// Area added per sales floor expansion module in square meters.
        /// </summary>
        public const float SalesExpansionModule = 16f;

        /// <summary>
        /// Area added per storage expansion module in square meters.
        /// </summary>
        public const float StorageExpansionModule = 32f;

        /// <summary>
        /// Player office area in square meters.
        /// </summary>
        public const float OfficeArea = 32f;

        /// <summary>
        /// Minimum aisle width in Unity units for customer AI navigation.
        /// </summary>
        public const float MinAisleWidth = 2f;

        /// <summary>
        /// Array of zone configurations defining the map layout.
        /// Assign these in the inspector from MapZoneConfig ScriptableObject assets.
        /// </summary>
        public MapZoneConfig[] zoneConfigs;

        /// <summary>
        /// Number of active sales floor expansion modules.
        /// </summary>
        public int salesExpansionCount { get; private set; }

        /// <summary>
        /// Number of active storage expansion modules.
        /// </summary>
        public int storageExpansionCount { get; private set; }

        //lookup for zone configs by zone type
        private Dictionary<MapZone, MapZoneConfig> zoneLookup;


        //initialize references
        void Awake()
        {
            Instance = this;

            zoneLookup = new Dictionary<MapZone, MapZoneConfig>();
            if (zoneConfigs != null)
            {
                foreach (MapZoneConfig config in zoneConfigs)
                {
                    if (config != null && !zoneLookup.ContainsKey(config.zone))
                        zoneLookup[config.zone] = config;
                }
            }

            UpgradeSystem.onUpgradePurchase -= OnExpansionPurchase;
            UpgradeSystem.onUpgradePurchase += OnExpansionPurchase;
        }


        /// <summary>
        /// Returns the zone config for the given zone type, or null if not defined.
        /// </summary>
        public static MapZoneConfig GetZoneConfig(MapZone zone)
        {
            if (Instance == null || Instance.zoneLookup == null)
                return null;

            Instance.zoneLookup.TryGetValue(zone, out MapZoneConfig config);
            return config;
        }


        /// <summary>
        /// Returns the current total area of the sales floor including expansions.
        /// </summary>
        public static float GetCurrentSalesArea()
        {
            return InitialSalesArea + (Instance != null ? Instance.salesExpansionCount * SalesExpansionModule : 0f);
        }


        /// <summary>
        /// Returns the current total area of the storage zone including expansions.
        /// </summary>
        public static float GetCurrentStorageArea()
        {
            return InitialStorageArea + (Instance != null ? Instance.storageExpansionCount * StorageExpansionModule : 0f);
        }


        /// <summary>
        /// Returns the total built area of the supermarket (sales + storage + office).
        /// </summary>
        public static float GetTotalBuiltArea()
        {
            return GetCurrentSalesArea() + GetCurrentStorageArea() + OfficeArea;
        }


        /// <summary>
        /// Returns whether the supermarket can still expand in the given zone type.
        /// </summary>
        public static bool CanExpand(ExpansionType type)
        {
            if (Instance == null)
                return false;

            float projectedArea;
            switch (type)
            {
                case ExpansionType.SalesFloor:
                    MapZoneConfig salesConfig = GetZoneConfig(MapZone.SalesFloor);
                    if (salesConfig != null && Instance.salesExpansionCount >= salesConfig.maxExpansionModules)
                        return false;
                    projectedArea = GetTotalBuiltArea() + SalesExpansionModule;
                    break;
                case ExpansionType.Storage:
                    MapZoneConfig storageConfig = GetZoneConfig(MapZone.Storage);
                    if (storageConfig != null && Instance.storageExpansionCount >= storageConfig.maxExpansionModules)
                        return false;
                    projectedArea = GetTotalBuiltArea() + StorageExpansionModule;
                    break;
                default:
                    return false;
            }

            return projectedArea <= TotalTerrainArea;
        }


        //handles expansion purchase events from UpgradeSystem
        private void OnExpansionPurchase(PurchasableScriptableObject purchasable)
        {
            if (purchasable is not ExpansionScriptableObject expansion || !expansion.isPurchased)
                return;

            if (expansion.expansionType == ExpansionType.SalesFloor)
            {
                salesExpansionCount++;
                onZoneExpanded?.Invoke(MapZone.SalesFloor, salesExpansionCount);
            }
            else if (expansion.expansionType == ExpansionType.Storage)
            {
                storageExpansionCount++;
                onZoneExpanded?.Invoke(MapZone.Storage, storageExpansionCount);
            }
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            data["salesExpansionCount"] = salesExpansionCount;
            data["storageExpansionCount"] = storageExpansionCount;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            salesExpansionCount = data["salesExpansionCount"].AsInt;
            storageExpansionCount = data["storageExpansionCount"].AsInt;
        }


        //unsubscribe from events
        void OnDestroy()
        {
            UpgradeSystem.onUpgradePurchase -= OnExpansionPurchase;
        }
    }
}
