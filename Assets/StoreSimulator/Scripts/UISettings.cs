/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI script attached to the in-game settings menu, connected to its handle elements.
    /// Also serves for persisting game-wide settings such as volume or graphic settings.
    /// </summary>
    public class UISettings : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static UISettings Instance { get; private set; }

        /// <summary>
        /// Slider for controlling the global volume of the game.
        /// </summary>
        public Slider volumeSlider;

        /// <summary>
        /// Text displaying the volume levels in percentage.
        /// </summary>
        public TMP_Text volumeDisplay;

        /// <summary>
        /// Slider for controlling the sensitivity when looking around.
        /// </summary>
        public Slider sensitivitySlider;

        /// <summary>
        /// Text displaying the sensitivity multiplier in percentage.
        /// </summary>
        public TMP_Text sensitivityDisplay;


        //initialize references
        void Awake()
        {
            Instance = this;
        }


        /// <summary>
        /// Subscribed to the UI Menu volume slider's OnValueChanged inspector event.
        /// </summary>
        public void OnVolumeChange(float value)
        {
            volumeSlider.value = value; //when called from LoadFromJSON
            volumeDisplay.text = (int)(value * 100) + "%";
            
            AudioListener.volume = value;
        }


        /// <summary>
        /// Subscribed to the UI Menu view sensitivity slider's OnValueChanged inspector event.
        /// </summary>
        public void OnViewSensitivityChange(float value)
        {
            sensitivitySlider.value = value; //when called from LoadFromJSON
            sensitivityDisplay.text = (int)(value * 100) + "%";

            InputAction viewAction = PlayerInput.GetPlayerByIndex(0).actions.FindAction("View");
            viewAction.ApplyParameterOverride("scaleVector2:x", value);
            viewAction.ApplyParameterOverride("scaleVector2:y", value);
        }


        //quick access to audio volume
        private float GetVolume()
        {
            return AudioListener.volume;
        }


        //quick access to view sensitivity
        private float GetViewSensitivity()
        {
            InputAction viewAction = PlayerInput.GetPlayerByIndex(0).actions.FindAction("View");
            return viewAction.GetParameterValue("scaleVector2:x").Value.ToSingle();
        }


        /// <summary>
        /// Reads component data that should be persisted and returns it as a JSONNode. 
        /// </summary>
        public JSONNode SaveToJSON()
        {
            JSONNode data = new JSONObject();
                
            data["volume"] = GetVolume();
            data["viewSens"] = GetViewSensitivity();
            
            return data;
        }


        /// <summary>
        /// Applies existing data coming from a JSONNode and overwrites it on this component.
        /// </summary>
        public void LoadFromJSON(JSONNode data)
        {
            if (data == null || data.Count == 0)
                return;

            OnVolumeChange(data["volume"].AsFloat);
            OnViewSensitivityChange(data["viewSens"].AsFloat);
        }
    }
}
