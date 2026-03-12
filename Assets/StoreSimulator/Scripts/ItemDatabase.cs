/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Database of all types of ScriptableObject assets used in the game for quick lookup and reference.
    /// </summary>
    public class ItemDatabase : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static ItemDatabase Instance { get; private set; }

        /// <summary>
        /// Event fired when the price of a product has been updated.
        /// </summary>
        public static event Action<ProductScriptableObject, string> onStorePriceUpdate;

        /// <summary>
        /// List of all ScriptableObject types derived from PurchasableScriptableObject.
        /// If you create a new asset of any type, it needs to be added to this list in the inspector.
        /// </summary>
        public List<PurchasableScriptableObject> purchasables;

        //dictionary with ScriptableObject assets sorted by their class types
        private Dictionary<Type, Dictionary<string, PurchasableScriptableObject>> ids = new Dictionary<Type, Dictionary<string, PurchasableScriptableObject>>();

        //the products available up to and including this level, excluding products locked behind an unpurchased license
        private List<ProductScriptableObject> availableProducts = new List<ProductScriptableObject>();

        //we need another data container that is only used in the Unity editor when entering/leaving play mode
        //this is to work around ScriptableObjects being persisted across play sessions in the editor, so we
        //save all of them in this container on entering and then restore them all when leaving play mode
        #if UNITY_EDITOR
        private JSONNode defaultData = null;
        #endif


        //initialize references
        void Awake()
        {
            Instance = this;

            ids.Add(typeof(StorageScriptableObject), new Dictionary<string, PurchasableScriptableObject>());
            ids.Add(typeof(ProductScriptableObject), new Dictionary<string, PurchasableScriptableObject>());
            ids.Add(typeof(LicenseScriptableObject), new Dictionary<string, PurchasableScriptableObject>());
            ids.Add(typeof(ExpansionScriptableObject), new Dictionary<string, PurchasableScriptableObject>());
            ids.Add(typeof(DecorationScriptableObject), new Dictionary<string, PurchasableScriptableObject>());
            ids.Add(typeof(BoosterScriptableObject), new Dictionary<string, PurchasableScriptableObject>());

            //sort list of purchasables into respective dictionary groups
            for (int i = 0; i < purchasables.Count; i++)
            {
                ids[purchasables[i].GetType()].Add(purchasables[i].id, purchasables[i]);
            }

            StoreDatabase.onLevelUpdate += OnLevelUpdate;

            //(editor only) whenever we enter play mode, save all ScriptableObjects defaults
            //when leaving play mode, restore default data back to all ScriptableObjects
            #if UNITY_EDITOR
            defaultData = new JSONObject();
            for(int i = 0; i < purchasables.Count; i++)
            {
                defaultData.Add(i.ToString(), new JSONString(JsonUtility.ToJson(purchasables[i])));
            }
            #endif
        }


        //initialize variables
        void Start()
        {
            //after the current level value has been loaded
            int currentLevel = StoreDatabase.Instance.currentLevel;

            for(int i = 0; i <= currentLevel; i++)
                availableProducts.AddRange(GetByLevel(typeof(ProductScriptableObject), i).OfType<ProductScriptableObject>());
        }


        /// <summary>
        /// Returns all purchasables by type.
        /// </summary>
        public static List<PurchasableScriptableObject> GetByType(Type type)
        {
            return Instance.ids[type].Values.ToList();
        }


        /// <summary>
        /// Returns the purchasable by id.
        /// </summary>
        public static PurchasableScriptableObject GetById(Type type, string id)
        {
            return Instance.ids[type][id];
        }


        /// <summary>
        /// Returns all purchasables on a specific level, filtered by their asset type.
        /// </summary>
        public static List<PurchasableScriptableObject> GetByLevel(Type type, int level)
        {
            List<PurchasableScriptableObject> list = new List<PurchasableScriptableObject>();
            
            foreach(KeyValuePair<string, PurchasableScriptableObject> pair in Instance.ids[type])
            {
                if (pair.Value.requiredLevel == level)
                    list.Add(pair.Value);
            }

            return list;
        }


        /// <summary>
        /// Returns all products associated to a specific license, accessed by id.
        /// </summary>
        public static List<ProductScriptableObject> GetProductsByLicense(string id)
        {
            List<ProductScriptableObject> list = new List<ProductScriptableObject>();
            
            foreach(KeyValuePair<string, PurchasableScriptableObject> pair in Instance.ids[typeof(ProductScriptableObject)])
            {
                ProductScriptableObject product = pair.Value as ProductScriptableObject;
                if (product.requiredLicense == id)
                    list.Add(product);
            }

            return list;
        }


        /// <summary>
        /// Returns a random list of available products with a specific length.
        /// This is used e.g. by CustomerBag to create a shopping list.
        /// The filter excludes products locked behind an unpurchased license.
        /// </summary>
        public static List<ProductScriptableObject> GetProductsRandom(int count)
        {
            List<ProductScriptableObject> filtered = new List<ProductScriptableObject>(Instance.availableProducts);
            filtered.RemoveAll(product => !string.IsNullOrEmpty(product.requiredLicense) && !(GetById(typeof(LicenseScriptableObject), product.requiredLicense) as LicenseScriptableObject).isPurchased);

            if (count == 1)
                return new List<ProductScriptableObject>() { filtered[UnityEngine.Random.Range(0, filtered.Count)] };

            List<ProductScriptableObject> list = new List<ProductScriptableObject>(filtered);
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }

            return list.GetRange(0, Mathf.Min(list.Count, count));
        }


        /// <summary>
        /// Update the selling price of a product passed in accessed by id.
        /// </summary>
        public static void UpdateStorePrice(string id, long price)
        {
            ProductScriptableObject product = GetById(typeof(ProductScriptableObject), id) as ProductScriptableObject;
            product.storePrice = price;
            onStorePriceUpdate?.Invoke(product, StoreDatabase.FromLongToStringMoney(price));
        }


        //on level change add the newly available products to the existing list
        private void OnLevelUpdate(int level)
        {
            availableProducts.AddRange(GetByLevel(typeof(ProductScriptableObject), level).Cast<ProductScriptableObject>());
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            //save all products with their changed prices
            JSONNode productArray = new JSONArray();
            foreach (KeyValuePair<string, PurchasableScriptableObject> pair in ids[typeof(ProductScriptableObject)])
            {
                ProductScriptableObject product = pair.Value as ProductScriptableObject;

                JSONNode element = new JSONObject();
                element["id"] = product.id;
                element["buyPrice"] = product.buyPrice;
                element["storePrice"] = product.storePrice;
                element["marketPrice"] = product.marketPrice;
                productArray.Add(element);
            }
            data["ProductScriptableObjects"] = productArray;

            //save all purchased licenses
            JSONNode licenseArray = new JSONArray();
            foreach (KeyValuePair<string, PurchasableScriptableObject> pair in ids[typeof(LicenseScriptableObject)])
            {
                LicenseScriptableObject license = pair.Value as LicenseScriptableObject;
                if (!license.isPurchased) continue;

                JSONNode element = new JSONObject();
                element["id"] = license.id;
                element["isPurchased"] = license.isPurchased;
                licenseArray.Add(element);
            }
            data["LicenseScriptableObjects"] = licenseArray;

            //save all purchased expansions
            JSONNode expansionArray = new JSONArray();
            foreach (KeyValuePair<string, PurchasableScriptableObject> pair in ids[typeof(ExpansionScriptableObject)])
            {
                ExpansionScriptableObject expansion = pair.Value as ExpansionScriptableObject;
                if (!expansion.isPurchased) continue;

                JSONNode element = new JSONObject();
                element["id"] = expansion.id;
                element["isPurchased"] = expansion.isPurchased;
                expansionArray.Add(element);
            }
            data["ExpansionScriptableObjects"] = expansionArray;

            //save all purchased decorations. It is made sure that only one can be selected
            //at the same time per DecorationType by the UpgradeSystem on purchase
            JSONNode decorationArray = new JSONArray();
            foreach (KeyValuePair<string, PurchasableScriptableObject> pair in ids[typeof(DecorationScriptableObject)])
            {
                DecorationScriptableObject decoration = pair.Value as DecorationScriptableObject;
                if (!decoration.isPurchased) continue;

                JSONNode element = new JSONObject();
                element["id"] = decoration.id;
                element["isPurchased"] = decoration.isPurchased;
                decorationArray.Add(element);
            }
            data["DecorationScriptableObjects"] = decorationArray;

            //save all purchased boosters. They are reset at the end of the day above,
            //but if we save and load during one day we need to persist them anyway
            JSONNode boosterArray = new JSONArray();
            foreach (KeyValuePair<string, PurchasableScriptableObject> pair in ids[typeof(BoosterScriptableObject)])
            {
                BoosterScriptableObject booster = pair.Value as BoosterScriptableObject;
                if (!booster.isPurchased) continue;

                JSONNode element = new JSONObject();
                element["id"] = booster.id;
                element["isPurchased"] = booster.isPurchased;
                boosterArray.Add(element);
            }
            data["BoosterScriptableObjects"] = boosterArray;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            JSONArray productArray = data["ProductScriptableObjects"].AsArray;
            for (int i = 0; i < productArray.Count; i++)
            {
                ProductScriptableObject product = GetById(typeof(ProductScriptableObject), productArray[i]["id"]) as ProductScriptableObject;
                product.buyPrice = productArray[i]["buyPrice"].AsLong;
                product.storePrice = productArray[i]["storePrice"].AsLong;
                product.marketPrice = productArray[i]["marketPrice"].AsLong;
            }

            JSONArray licenseArray = data["LicenseScriptableObjects"].AsArray;
            for (int i = 0; i < licenseArray.Count; i++)
            {
                LicenseScriptableObject license = GetById(typeof(LicenseScriptableObject), licenseArray[i]["id"]) as LicenseScriptableObject;
                license.isPurchased = licenseArray[i]["isPurchased"].AsBool;
            }

            JSONArray expansionArray = data["ExpansionScriptableObjects"].AsArray;
            for (int i = 0; i < expansionArray.Count; i++)
            {
                ExpansionScriptableObject expansion = GetById(typeof(ExpansionScriptableObject), expansionArray[i]["id"]) as ExpansionScriptableObject;
                expansion.isPurchased = expansionArray[i]["isPurchased"].AsBool;
            }

            //unselect default selections per DecorationType
            JSONArray decorationArray = data["DecorationScriptableObjects"].AsArray;
            for (int i = 0; i < decorationArray.Count; i++)
            {
                DecorationScriptableObject decoration = GetById(typeof(DecorationScriptableObject), decorationArray[i]["id"]) as DecorationScriptableObject;
                DecorationScriptableObject isSelected = GetByType(typeof(DecorationScriptableObject)).OfType<DecorationScriptableObject>()
                                                        .FirstOrDefault(x => x.decorationType == decoration.decorationType && x.isPurchased);

                if (isSelected != null) isSelected.isPurchased = false;
                decoration.isPurchased = decorationArray[i]["isPurchased"].AsBool;
            }
            
            JSONArray boosterArray = data["BoosterScriptableObjects"].AsArray;
            for (int i = 0; i < boosterArray.Count; i++)
            {
                BoosterScriptableObject booster = GetById(typeof(BoosterScriptableObject), boosterArray[i]["id"]) as BoosterScriptableObject;
                booster.isPurchased = boosterArray[i]["isPurchased"].AsBool;
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StoreDatabase.onLevelUpdate -= OnLevelUpdate;

            #if UNITY_EDITOR
            for (int i = 0; i < purchasables.Count; i++)
            {
                JsonUtility.FromJsonOverwrite(defaultData[i].Value, purchasables[i]);
            }
            #endif
        }
    }
}