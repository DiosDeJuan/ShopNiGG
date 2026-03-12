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
    /// Manages the spawning of customers and contains variables and methods that should be applied to all of them.
    /// </summary>
    public class CustomerSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static CustomerSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when a customer left the store, for statistical purposes.
        /// The boolean describes whether they were happy (true) or unhappy (false).
        /// </summary>
        public static event Action<bool> onCustomerLeft;
       
        /// <summary>
        /// Rate for spawning per real time minute. E.g. 5 = 5 new customers per minute.
        /// </summary>
        public int spawnRate = 5;

        /// <summary>
        /// Maximum amount of products that can be stored in a CustomerBag.
        /// This is a total amount, including products that could be collected multiple times.
        /// You should limit this by considering the place you have on a CashDesk conveyor.
        /// </summary>
        [Range(1, 10)]
        public int maxBagProducts = 5;

        /// <summary>
        /// Chance for the customer to not only collect a single product, but multiple.
        /// </summary>
        [Range(0, 100)]
        public int multipleProductRate = 70;

        /// <summary>
        /// Chance for a product to be collected multiple times, from 0 to 100.
        /// </summary>
        [Range(0, 100)]
        public int duplicateProductRate = 30;

        /// <summary>
        /// Chance for the customer to pay in cash, instead of by card, from 0 to 100.
        /// </summary>
        [Range(0, 100)]
        public int payCashRate = 40; 

        /// <summary>
        /// The PaymentItemCash prefab in the customer's hands that should be interacted with upon checkout.
        /// </summary>
        public GameObject cashPrefab;

        /// <summary>
        /// The PaymentItemCard prefab in the customer's hands that should be interacted with upon checkout.
        /// </summary>
        public GameObject cardPrefab;

        /// <summary>
        /// Array of customer prefabs that should be spawned randomly according to the spawnRate.
        /// </summary>
        public GameObject[] customerPrefabs;

        /// <summary>
        /// Prefab that contains a UISpeechBubble component for displaying customer's reactions.
        /// </summary>
        public GameObject speechPrefab;

        /// <summary>
        /// Whether spawn locations should be retrieved from child transforms automatically.
        /// </summary>
        public bool spawnFromChildren = true;

        /// <summary>
        /// Array for manual assignment of locations where the customers should spawn at.
        /// </summary>
        public Transform[] spawnLocations;


        //initialize references
        void Awake()
        {
            Instance = this;

            if (spawnFromChildren)
            {
                List<Transform> list = new List<Transform>(spawnLocations);
                list.AddRange(GetComponentsInChildren<Transform>());
                list.RemoveAt(spawnLocations.Length); //remove this parent
                spawnLocations = list.ToArray();
            }

            DayCycleSystem.onDayStarted += StartSpawning;
            DayCycleSystem.onDayOver += StopSpawning;
        }


        //initialize variables
        void Start()
        {
            if (DayCycleSystem.GetStoreOpenState() == StoreOpenState.Open)
                StartSpawning();
        }


        /// <summary>
        /// Method called by a Customer instance when leaving the store, firing the onCustomerLeft event.
        /// </summary>
        public static void CustomerLeft(bool wasHappy = false)
        {
            onCustomerLeft?.Invoke(wasHappy);
        }


        /// <summary>
        /// Calculates an amount of money a customers should pay with, when paying in cash.
        /// The amount can match the cart total, or more, requiring the player to give out change.
        /// </summary>
        public static long GetCashPaymentAmount(long total)
        {
            //type used for the calculation, there are 3 different types
            int cashType = UnityEngine.Random.Range(100, 0);
            //available notes (US Dollar standard), from $5 to $100
            long[] availableNotes = new long[] {500, 1000, 2000, 5000, 10000};
            List<long> drawNotes = new List<long>();

            //20%: exact payment, no change
            if (cashType <= 20)
            {
                return total;
            }

            //if the amount due exceeds $100 we force to use the next type
            //since a payment amount over $100 definitely needs multiple notes
            if (total > 10000) cashType = 60;

            //60%: multiple notes
            if (cashType <= 60)
            {
                //determine whether payment should be as close to the cart total as possible
                bool nearest = UnityEngine.Random.Range(100, 0) <= 20;

                //if total is not rounded, one dollar above total
                if (nearest && total % 100 != 0)
                    return ((total / 100) + 1) * 100;
                
                //find all notes that are smaller than the cart total
                for(int i = 0; i < availableNotes.Length; i++)
                {
                    if (availableNotes[i] < total)
                        drawNotes.Add(availableNotes[i]);
                }

                //random (greater) note above total, $5 minimum
                if (drawNotes.Count == 0) drawNotes.Add(500);
                long selectedNote = drawNotes[UnityEngine.Random.Range(0, drawNotes.Count)];
                long noteAmount = 0;

                //add multiples of that note until we have exceeded the cart total
                while(noteAmount < total)
                    noteAmount += selectedNote;

                return noteAmount;
            }

            //20%: overpayment with a single note
            //find all notes that are higher than the cart total
            for(int i = 0; i < availableNotes.Length; i++)
            {
                if (availableNotes[i] > total)
                    drawNotes.Add(availableNotes[i]);
            }

            //return a random single note from that list
            return drawNotes[UnityEngine.Random.Range(0, drawNotes.Count)];
        }


        //since this method is subscribed to the onDayStarted event,
        //it starts spawning customers after the store has been opened
        private void StartSpawning()
        {
            StartCoroutine(SpawnCustomers());
        }


        //the actual spawn routine, distributing customer spawn across one minute
        private IEnumerator SpawnCustomers()
        {
            while(true)
            {
                for(int i = 0; i < spawnRate; i++)
                {
                    Invoke("SpawnCustomer", UnityEngine.Random.Range(1, 60));
                }

                yield return new WaitForSeconds(60); 
            }
        }


        //instantiation of the random customer prefab at their initial random location
        private void SpawnCustomer()
        {
            GameObject prefab = customerPrefabs[UnityEngine.Random.Range(0, customerPrefabs.Length)];
            Vector3 spawnPosition = spawnLocations[UnityEngine.Random.Range(0, spawnLocations.Length)].position;
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }


        //subscribed to the onDayOver event, stopping spawns when the day cycle passed
        private void StopSpawning()
        {
            StopAllCoroutines();
            CancelInvoke();
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();

            data["spawnRate"] = spawnRate;
            data["maxBagProducts"] = maxBagProducts;
            data["multipleProductRate"] = multipleProductRate;
            data["duplicateProductRate"] = duplicateProductRate;
            data["payCashRate"] = payCashRate;
            
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            spawnRate = data["spawnRate"].AsInt;
            maxBagProducts = data["maxBagProducts"].AsInt;
            multipleProductRate = data["multipleProductRate"].AsInt;
            duplicateProductRate = data["duplicateProductRate"].AsInt;
            payCashRate = data["payCashRate"].AsInt;
        }


        //unsubscribe from events
        void OnDestroy()
        {
            DayCycleSystem.onDayStarted -= StartSpawning;
            DayCycleSystem.onDayOver -= StopSpawning;
        }
    }
}