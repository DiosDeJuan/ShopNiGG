/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Manages the instantiation (delivery) of packages, i.e. products or storage, that the player have bought.
    /// </summary>
    public class DeliverySystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static DeliverySystem Instance { get; private set; }

        /// <summary>
        /// Event fired when a product has been bought to inform the player.
        /// </summary>
        public static event Action<ProductScriptableObject> onProductPurchase;

        /// <summary>
        /// The starting point of where packages should start to be spawned at.
        /// </summary>
        public Transform deliveryStart;

        /// <summary>
        /// The direction to spawn new packages in.
        /// </summary>
        public Vector2 deliveryDirection;

        /// <summary>
        /// Count of maximum packages that can be spawned in the deliveryDirection.
        /// </summary>
        public int totalDeliveries = 1;

        /// <summary>
        /// Box prefab that should contain the bought item.
        /// </summary>
        public GameObject packagePrefab;

        /// <summary>
        /// Clip to play when a PackageObject has been picked up, or none if not set.
        /// </summary>
        public AudioClip pickupClip;

        /// <summary>
        /// Clip to play when a PackageObject has been dropped, or none if not set.
        /// </summary>
        public AudioClip dropClip;


        //initialize variables
        void Awake()
        {
            Instance = this;
        }


        /// <summary>
        /// Called from a UIShopItem instance when trying to purchase that specific item.
        /// Does an internal check for money and then spawns the package.
        /// </summary>
        public static void Purchase(PurchasableScriptableObject purchasable)
        {
            //get amount of products in the package
            int amount = 1;
            if (purchasable is ProductScriptableObject)
                amount = (purchasable as ProductScriptableObject).packageCount;

            if (!StoreDatabase.CanPurchase(purchasable.buyPrice * amount))
            {
                UIGame.Instance.ShowMessage("Not enough money to purchase this item");
                return;
            }

            //subtract money
            StoreDatabase.AddRemoveMoney(-purchasable.buyPrice * amount);

            //spawn package and amount of items within that package
            Vector3 deliveryPosition = Instance.GetDeliveryPosition();
            GameObject newPackage = Instantiate(Instance.packagePrefab, deliveryPosition + new Vector3(0, 2, 0), Quaternion.identity);
            PackageObject packageObject = newPackage.GetComponent<PackageObject>();
            packageObject.Add(purchasable, amount);

            onProductPurchase?.Invoke(purchasable as ProductScriptableObject);
        }


        //calculate the lowest possible position to deliver new packages
        private Vector3 GetDeliveryPosition()
        {
            //raycast properties
            Vector3 lowestPosition = deliveryStart.position;
            float highestDistance = 0f;
            float rayLength = 50;

            //starting from the deliveryStart position, do a raycast until the end of deliveryDirection to find the lowest
            //position in height by raycasting against all packages that have already been spawned at the delivery area
            for(int i = 0; i < totalDeliveries; i++)
            {
                Vector3 rayPosition = deliveryStart.position + new Vector3(i * deliveryDirection.x, 0, i * deliveryDirection.y);
                Ray ray = new Ray(rayPosition + Vector3.up * rayLength, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, rayLength, InteractionSystem.Instance.layerMask))
                {
                    float hitDistance = Vector3.Distance(ray.origin, hit.point);
                    if (hitDistance > highestDistance)
                    {
                        lowestPosition = hit.point;
                        highestDistance = hitDistance;
                    }
                }
                else
                {
                    lowestPosition = rayPosition;
                    break;
                }
            }

            return lowestPosition;
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            JSONNode objectArray = new JSONArray();

            PackageObject[] objects = FindObjectsByType<PackageObject>(FindObjectsSortMode.None);
            for(int i = 0; i < objects.Length; i++)
                objectArray[i] = objects[i].SaveToJSON();

            data["PackageObjects"] = objectArray;

            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            JSONArray objectsArray = data["PackageObjects"].AsArray;
            for(int i = 0; i < objectsArray.Count; i++)
            {
                GameObject go = Instantiate(packagePrefab);
                go.GetComponent<PackageObject>().LoadFromJSON(objectsArray[i]);
            }
        }
    }
}