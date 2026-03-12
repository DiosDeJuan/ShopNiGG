/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// System for saving and loading a file with game states in JSON format to/from local storage.
    /// </summary>
    public class SaveGameSystem : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static SaveGameSystem Instance { get; private set; }

        /// <summary>
        /// The name of the datafile key on the device.
        /// <summary>
        public const string fileKey = "save";

        /// <summary>
        /// The file extension of the persistentDataPath file.
        /// </summary>
        public const string fileExt = ".dat";

        /// <summary>
        /// Fired when a data save action finished.
        /// </summary>
        public static event Action dataSaveEvent;

        /// <summary>
        /// Fired when a data load action finished.
        /// </summary>
        public static event Action dataLoadEvent;

        //representation of device's data in memory during loading
        private JSONNode gameData = null;


        //initialize references
        void Awake()
        {
            //make sure we keep one instance of this script
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);

            //set static reference
            Instance = this;
        }


        /// <summary>
        /// Create a new save file.
        /// </summary>
        public static void New()
        {
            JSONNode data = new JSONObject();

            Instance.gameData = data;
            SceneManager.sceneLoaded += Instance.OnSceneLoaded;
        }


        /// <summary>
        /// Gather data from all game systems and save them to the device.
        /// Allow overwriting the file name with a different key, e.g. for another player profile.
        /// </summary>
        public static void Save(string otherKey = "")
        {
            string fileName = otherKey == string.Empty ? fileKey : otherKey;
            JSONNode data = new JSONObject();

            data["UISettings"] = UISettings.Instance.SaveToJSON();
            data["ItemDatabase"] = ItemDatabase.Instance.SaveToJSON();
            data["StoreDatabase"] = StoreDatabase.Instance.SaveToJSON();
            data["DayCycleSystem"] = DayCycleSystem.Instance.SaveToJSON();
            data["StorageSystem"] = StorageSystem.Instance.SaveToJSON();
            data["DeliverySystem"] = DeliverySystem.Instance.SaveToJSON();
            data["DailyEventSystem"] = DailyEventSystem.Instance.SaveToJSON();
            data["CustomerSystem"] = CustomerSystem.Instance.SaveToJSON();
            data["TutorialSystem"] = TutorialSystem.Instance.SaveToJSON();
            data["StatsDatabase"] = StatsDatabase.Instance.SaveToJSON();

            if (EntrepreneurTreeSystem.Instance != null)
                data["EntrepreneurTreeSystem"] = EntrepreneurTreeSystem.Instance.SaveToJSON();
            if (EmployeeSystem.Instance != null)
                data["EmployeeSystem"] = EmployeeSystem.Instance.SaveToJSON();
            if (SecuritySystem.Instance != null)
                data["SecuritySystem"] = SecuritySystem.Instance.SaveToJSON();
            if (ShoplifterSystem.Instance != null)
                data["ShoplifterSystem"] = ShoplifterSystem.Instance.SaveToJSON();
            if (MapLayoutSystem.Instance != null)
                data["MapLayoutSystem"] = MapLayoutSystem.Instance.SaveToJSON();

            byte[] dataAsBytes = Encoding.ASCII.GetBytes(data.ToString());
            try { File.WriteAllBytes(Application.persistentDataPath + "/" + fileName + fileExt, dataAsBytes); }
            catch (Exception) { }

            //notify subscribed scripts of data update
            dataSaveEvent?.Invoke();
        }


        /// <summary>
        /// Loads the local data and applies it to all game systems.
        /// Allow overwriting the file name with a different key, e.g. for another player profile.
        /// </summary>
        public static void Load(string otherKey = "")
        {
            string fileName = otherKey == string.Empty ? fileKey : otherKey;
            string dataString = string.Empty;

            if (File.Exists(Application.persistentDataPath + "/" + fileKey + fileExt))
            {
                byte[] dataAsBytes = File.ReadAllBytes(Application.persistentDataPath + "/" + fileKey + fileExt);
                dataString = Encoding.ASCII.GetString(dataAsBytes);
            }
            
            //savegame not found - create new game instead
            if (string.IsNullOrEmpty(dataString))
            {
                New();
                return;
            }

            Instance.gameData = JSON.Parse(dataString);
            SceneManager.sceneLoaded += Instance.OnSceneLoaded;
        }


        /// <summary>
        /// Returns data of a single system component for quick access.
        /// </summary>
        public static JSONNode ReadComponentData(string component)
        {
            JSONNode dataCopy = new JSONObject();

            if (Instance.gameData != null)
                dataCopy = Instance.gameData[component].Clone();

            return dataCopy;
        }


        //called on scene change for both a new or loaded save file
        //this makes sure we apply our local save file to the game systems in the scene
        //OnSceneLoaded is called after Awake(), but before Start(), making it possible to time actions
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UISettings.Instance.LoadFromJSON(gameData["UISettings"]);
            ItemDatabase.Instance.LoadFromJSON(gameData["ItemDatabase"]);
            StoreDatabase.Instance.LoadFromJSON(gameData["StoreDatabase"]);
            DayCycleSystem.Instance.LoadFromJSON(gameData["DayCycleSystem"]);
            StorageSystem.Instance.LoadFromJSON(gameData["StorageSystem"]);
            DeliverySystem.Instance.LoadFromJSON(gameData["DeliverySystem"]);
            DailyEventSystem.Instance.LoadFromJSON(gameData["DailyEventSystem"]);
            CustomerSystem.Instance.LoadFromJSON(gameData["CustomerSystem"]);
            TutorialSystem.Instance.LoadFromJSON(gameData["TutorialSystem"]);
            StatsDatabase.Instance.LoadFromJSON(gameData["StatsDatabase"]);

            if (EntrepreneurTreeSystem.Instance != null)
                EntrepreneurTreeSystem.Instance.LoadFromJSON(gameData["EntrepreneurTreeSystem"]);
            if (EmployeeSystem.Instance != null)
                EmployeeSystem.Instance.LoadFromJSON(gameData["EmployeeSystem"]);
            if (SecuritySystem.Instance != null)
                SecuritySystem.Instance.LoadFromJSON(gameData["SecuritySystem"]);
            if (ShoplifterSystem.Instance != null)
                ShoplifterSystem.Instance.LoadFromJSON(gameData["ShoplifterSystem"]);
            if (MapLayoutSystem.Instance != null)
                MapLayoutSystem.Instance.LoadFromJSON(gameData["MapLayoutSystem"]);
            
            //notify subscribed scripts of data update
            dataLoadEvent?.Invoke();
            SceneManager.sceneLoaded -= Instance.OnSceneLoaded;
        }
    }
}