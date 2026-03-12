/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using UnityEngine;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Responsible for each day cycle, managing time and sending important events on that day.
    /// </summary>
    public class DayCycleSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static DayCycleSystem Instance { get; private set; }

        /// <summary>
        /// Event fired when the scene has been first loaded and the store was not opened yet.
        /// Subscribed to by other systems for initialization purposes.
        /// </summary>
        public static event Action onDayLoaded;

        /// <summary>
        /// Event fired when the store is opened by the player.
        /// </summary>
        public static event Action onDayStarted;

        /// <summary>
        /// Event fired when the openingHours have passed and the store closed.
        /// </summary>
        public static event Action onDayOver;

        /// <summary>
        /// Event fired when the player decides to end the day.
        /// </summary>
        public static event Action onDayFinished;

        /// <summary>
        /// Event fired each frame, providing the time as formatted string.
        /// </summary>
        public static event Action<string> onTimeUpdate;

        /// <summary>
        /// Total time in seconds for a full day from open to closing.
        /// </summary>
        public int lengthDaySeconds = 600;

        /// <summary>
        /// Opening hours represented in x = am and y = pm (value cannot exceed 12/0).
        /// </summary>
        public Vector2Int openingHours;

        /// <summary>
        /// Reference to the sun's Transform for rotation.
        /// </summary>
        public Transform sun;

        /// <summary>
        /// The current number of the day played.
        /// </summary>
        public int currentDay { get; private set; }

        //for checking whether the day has been started already
        private bool isLoaded = false;
        //current day time in seconds
        private int currentTime;
        //fraction of time passed in the last second
        private float fractionalSecond;
        //the speed time passes per second, in seconds
        private float timeSpeed;
        //initial sun rotation as vector
        private Vector3 sunRotation = Vector3.zero;


        //initialize references
        void Awake()
        {
            Instance = this;
        }


        //initialize variables
        void Start()
        {
            //calculate how many seconds should be added per realtime second, so the day
            //from open to closed ends in lengthDaySeconds. Given that openingHours.x is before 12 pm
            timeSpeed = (12 - openingHours.x + openingHours.y) * 3600 / lengthDaySeconds;

            //set initial sun rotation
            if (sun != null)
                sunRotation = sun.eulerAngles;
            
            SetSunRotation();

            //invoke onDayLoaded only once
            if (GetStoreOpenState() == StoreOpenState.Waiting && !isLoaded)
            {
                onDayLoaded?.Invoke();
                isLoaded = true;
            }

            //start time passing
            if (GetStoreOpenState() == StoreOpenState.Open)
                StartCoroutine(TimeCoroutine());
        }


        /// <summary>
        /// Returns the current time as formatted string.
        /// </summary>
        public static string GetTimeString()
        {
            int hours = Instance.currentTime / 3600; // Integer division for hours
            int minutes = Instance.currentTime % 3600 / 60; // Remainder divided by 60 for minutes

            // Determine AM or PM
            string period = hours >= 12 ? "pm" : "am";

            // Convert hours to 12-hour format
            if (hours == 0) hours = 12; // Midnight is 12am
            else if (hours > 12) hours -= 12; // Convert 13-23 to 1-11 for pm

            // Return formatted string
            return $"{hours:D2}:{minutes:D2} {period}";
        }


        /// <summary>
        /// Returns the number of the day as formatted string.
        /// </summary>
        public static string GetDayString()
        {
            return "Day " + Instance.currentDay;
        }


        /// <summary>
        /// Returns the current opening state of the store.
        /// </summary>
        public static StoreOpenState GetStoreOpenState()
        {
            if (Instance.currentTime <= Instance.openingHours.x * 3600)
                return StoreOpenState.Waiting;

            if (Instance.currentTime < 12 * 3600 + Instance.openingHours.y * 3600)
                return StoreOpenState.Open;

            return StoreOpenState.Closed;
        }


        /// <summary>
        /// Invoked by turning the OpenSign in order to open the store.
        /// </summary>
        public void StartDay()
        {
            if (GetStoreOpenState() != StoreOpenState.Waiting)
                return;

            onDayStarted?.Invoke();
            StartCoroutine(TimeCoroutine());
        }


        /// <summary>
        /// Invoked by turning the OpenSign in order to close the store.
        /// Resets the time and adds one to the day number.
        /// </summary>
        public void FinishDay()
        {
            if (GetStoreOpenState() != StoreOpenState.Closed)
                return;

            currentTime = openingHours.x * 3600;
            currentDay++;
            isLoaded = false;

            onDayFinished?.Invoke();
        }
        

        //the actual time routine called every frame until the store is closed
        private IEnumerator TimeCoroutine()
        {
            while (currentTime <= 12 * 3600 + openingHours.y * 3600)
            {
                fractionalSecond += Time.deltaTime * timeSpeed;

                if (fractionalSecond > 1)
                {
                    int secondsPassed = Mathf.FloorToInt(fractionalSecond);

                    currentTime += secondsPassed;
                    fractionalSecond -= secondsPassed;

                    onTimeUpdate?.Invoke(GetTimeString());
                }

                SetSunRotation();
                yield return null;
            }

            onDayOver?.Invoke();
        }


        //calculate sun rotation going from 180 to -5 on x over the course of a day
        private void SetSunRotation()
        {
            float openingSeconds = openingHours.x * 3600;
            float closingSeconds = 12 * 3600 + openingHours.y * 3600;
            float sunAngle = 185 - 185 * (1 - ((currentTime - openingSeconds) / (closingSeconds - openingSeconds)));
            sunRotation.x = sunAngle;

            if (sun != null)
            {
                sun.rotation = Quaternion.Euler(sunRotation);
            }
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
                
            data["isLoaded"] = isLoaded;
            data["currentDay"] = currentDay;
            data["currentTime"] = currentTime;
            
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
            {
                isLoaded = false;
                currentDay = 1;
                currentTime = openingHours.x * 3600;
                return;
            }

            isLoaded = data["isLoaded"].AsBool;
            currentDay = data["currentDay"].AsInt;
            currentTime = data["currentTime"].AsInt;
        }
    }
}