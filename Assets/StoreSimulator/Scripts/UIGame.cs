/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI script mainly for all UI-logic in the game scene,
    /// but also serves as controller for entering or leaving it.
    /// </summary>
    public class UIGame : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static UIGame Instance { get; private set; }

        /// <summary>
        /// Scene to load when the day has been closed by the player.
        /// </summary>
        public string nextScene;

        /// <summary>
        /// Gameobject that holds all UI elements, except menus and overlays.
        /// Those parts should continue to be displayed even with UI disabled.
        /// </summary>
        public GameObject hudParent;

        /// <summary>
        /// Gameobject that holds the UI elements associated with the in-game menu.
        /// By default, on desktop build it is triggered when pressing the ESC key.
        /// </summary>
        public GameObject menuParent;

        /// <summary>
        /// Gameobject opens the in-game menu on mobile platforms.
        /// This button is only shown on mobile devices and otherwise deactivated.
        /// </summary>
        public GameObject mobileMenuButton;

        /// <summary>
        /// Container of a background element, allowing easy access to its alpha value.
        /// This element prevents additional user input in scene transitions.
        /// </summary>
        public CanvasGroup blockerGroup;

        /// <summary>
        /// Reference to the window for setting the price of a product, enabled via PriceTag.
        /// </summary>
        public UIPriceLabelWindow priceLabelWindow;

        /// <summary>
        /// Label for displaying the experience level.
        /// </summary>
        public TMP_Text levelDisplay;

        /// <summary>
        /// Slider for visualization of the current level experience progress.
        /// </summary>
        public Slider levelSlider;

        /// <summary>
        /// Label for displaying the player currency balance.
        /// </summary>
        public TMP_Text moneyDisplay;

        /// <summary>
        /// Label for displaying the current formatted time string.
        /// </summary>
        public TMP_Text timeDisplay;

        /// <summary>
        /// Container of the message element, allowing easy access to its alpha value.
        /// </summary>
        public CanvasGroup messageGroup;

        /// <summary>
        /// Label for displaying important messages to the player, such as hints or errors.
        /// </summary>
        public TMP_Text message;


        [Header("Actions")]
        /// <summary>
        /// Reference to the prefab that should be instantiated when adding a new input action hint to the UI.
        /// </summary>
        public GameObject actionHintPrefab;

        /// <summary>
        /// Container transform that acts as the parent for existing action prefab instances.
        /// </summary>
        public Transform actionContainer;

        /// <summary>
        /// Icons that could be shown on actions instead of keys, in case they have the same name as the input key.
        /// </summary>
        public Sprite[] actionIcons;

        /// <summary>
        /// Array of game objects with UIActionButton components shown on mobile platforms instead of the actionHintPrefab.
        /// </summary>
        public GameObject[] actionButtons;

        [Header("Notifications")]
        /// <summary>
        /// Reference to the prefab that should be instantiated when adding a new notification popup to the UI.
        /// </summary>
        public GameObject notificationPrefab;

        /// <summary>
        /// Container transform that acts as the parent for existing notification prefab instances.
        /// </summary>
        public Transform notificationContainer;

        /// <summary>
        /// Clip to play when a new notification occurs, or none if not set.
        /// </summary>
        public AudioClip notificationClip;

        [Header("Tutorials")]
        /// <summary>
        /// Reference to the prefab that should be instantiated when adding a new notification popup to the UI.
        /// </summary>
        public GameObject tutorialPrefab;

        /// <summary>
        /// Container transform that acts as the parent for existing notification prefab instances.
        /// </summary>
        public Transform tutorialContainer;

        /// <summary>
        /// Clip to play when a tutorial step was completed, or none if not set.
        /// </summary>
        public AudioClip tutorialClip;


        //dictionary of UIActionControl instances mapped to a key identifier
        private Dictionary<string, GameObject> actionsDic = new Dictionary<string, GameObject>();
        //coroutine for the error message to enable access to it
        private Coroutine messageCoroutine;


        //initialize references
        void Awake()
        {
            Instance = this;

            StoreDatabase.onLevelUpdate += OnLevelUpdate;
            StoreDatabase.onExperienceUpdate += OnExperienceUpdate;
            StoreDatabase.onMoneyUpdate += OnMoneyUpdate;
            DayCycleSystem.onTimeUpdate += OnTimeUpdate;
            DayCycleSystem.onDayOver += OnDayOver;
            DayCycleSystem.onDayFinished += OnDayFinished;

            PlayerInput.GetPlayerByIndex(0).onActionTriggered += OnAction;

            #if UNITY_ANDROID || UNITY_IOS
                mobileMenuButton.SetActive(true);
            #endif

            blockerGroup.gameObject.SetActive(true);
            StartCoroutine(FadeInOut(blockerGroup, 0, false));
        }


        //initialize variables
        void Start()
        {
            OnLevelUpdate(StoreDatabase.Instance.currentLevel);
            OnExperienceUpdate(StoreDatabase.Instance.currentXP, 0);
            moneyDisplay.text = StoreDatabase.GetMoneyString();
            timeDisplay.text = DayCycleSystem.GetTimeString();
        }


        /// <summary>
        /// Lerp alpha value of the UI canvas group to let it fade in or out over time.
        /// </summary>
        public static IEnumerator FadeInOut(CanvasGroup group, float delay, bool fadeIn)
        {
            float startingAlpha = fadeIn ? 0 : 1;
            float targetAlpha = fadeIn ? 1 : 0;
            float lerpDuration = 1f;
            float lerpProgress = 0f;
            
            if (delay > 0)
                yield return new WaitForSecondsRealtime(delay);

            if (fadeIn) group.gameObject.SetActive(true);
            while (lerpProgress < lerpDuration)
            {
                lerpProgress += Time.deltaTime;
                float a = Mathf.Lerp(startingAlpha, targetAlpha, lerpProgress / lerpDuration);

                group.alpha = a;
                yield return null;
            }

            group.alpha = targetAlpha;
            if (!fadeIn) group.gameObject.SetActive(false);
        } 


        /// <summary>
        /// Adds an input action with the details specified to the action container.
        /// On mobile platforms, this enabled the corresponding UIActionButton instead.
        /// </summary>
        public static void AddAction(string key, string action, bool showIcon = false)
        {
            if (Instance.actionsDic.ContainsKey(key))
                return;

            GameObject obj = null;
            #if UNITY_ANDROID || UNITY_IOS
                switch(key)
                {
                    case "LeftClick":
                        obj = Instance.actionButtons[0];
                        break;
                    case "RightClick":
                        obj = Instance.actionButtons[1];
                        break;
                    case "Esc":
                        obj = Instance.actionButtons[2];
                        break;
                    default:
                        obj = Instance.actionButtons[3];
                        break;
                }

                obj.GetComponent<UIActionButton>().Initialize(action);
            #else
                obj = Instantiate(Instance.actionHintPrefab, Instance.actionContainer, false);
                Sprite icon = null;

                if (showIcon)
                {
                    //search icon with the same name in the list of icons
                    for(int i = 0; i < Instance.actionIcons.Length; i++)
                    {
                        if (Instance.actionIcons[i].name == key)
                        {
                            icon = Instance.actionIcons[i];
                            break;
                        }
                    }
                }

                obj.GetComponent<UIActionHint>().Initialize(key, action, icon);
            #endif

            Instance.actionsDic.Add(key, obj);
        }


        /// <summary>
        /// Returns whether an action with the associated key is currently shown.
        /// </summary>
        public static bool HasAction(string key)
        {
            return Instance.actionsDic.ContainsKey(key);
        }


        /// <summary>
        /// Destroys an input action for the specified key from the action container.
        /// </summary>
        public static void RemoveAction(string key)
        {
            if (!Instance.actionsDic.ContainsKey(key))
                return;
           
            #if UNITY_ANDROID || UNITY_IOS
                Instance.actionsDic[key].SetActive(false);
            #else
                Destroy(Instance.actionsDic[key]);
            #endif

            Instance.actionsDic.Remove(key);
        }


        /// <summary>
        /// Adds a notification popup with the details specified to the notification container.
        /// </summary>
        public static void AddNotification(string text, Sprite otherIcon = null, Color? otherColor = null, float otherDuration = 0)
        {
            GameObject obj = Instantiate(Instance.notificationPrefab, Instance.notificationContainer, false);
            UINotification notification = obj.GetComponent<UINotification>();

            notification.Initialize(text, otherIcon, otherColor, otherDuration);
            AudioSystem.Play2D(Instance.notificationClip);
        }


        /// <summary>
        /// Adds a notification popup with the details specified to the notification container.
        /// </summary>
        public static void AddTutorial(TutorialScriptableObject data, int startingCount = 0)
        {
            GameObject obj = Instantiate(Instance.tutorialPrefab, Instance.tutorialContainer, false);
            UITutorial tutorial = obj.GetComponent<UITutorial>();

            tutorial.Initialize(data, startingCount);
        }


        /// <summary>
        /// Show a message to the player and fade it out slowly.
        /// </summary>
        public void ShowMessage(string text, bool fadeOut = true)
        {
            message.text = text;
            messageGroup.alpha = 1;
            messageGroup.gameObject.SetActive(true);

            if (messageCoroutine != null) StopCoroutine(messageCoroutine);
            if (fadeOut) messageCoroutine = StartCoroutine(FadeInOut(messageGroup, 2, false));
        }


        /// <summary>
        /// Toggles visibility of the UI, except menus and overlay.
        /// </summary>
        public void SetVisible(bool state)
        {
            hudParent.SetActive(state);
        }


        /// <summary>
        /// Toggles visibility of the in-game menu UI.
        /// </summary>
        public void SetMenuVisible(bool state)
        {
            if (state == true)
            {
                PlayerController.SetMovementState(MovementState.None, false);   
                InteractionSystem.SetInteractionState(InteractionState.None); 
                Time.timeScale = 0;
            }
            else
            {
                PlayerController.SetMovementState(PlayerController.GetPreviousMovementState(), true);    
                InteractionSystem.SetInteractionState(InteractionState.All);
                Time.timeScale = 1;
            }
            
            menuParent.SetActive(state);
        }


        /// <summary>
        /// Returns to the Intro scene with optional save, initiated from the in-game menu.
        /// </summary>
        public void Quit(bool withSave)
        {
            if (withSave)
                SaveGameSystem.Save();

            SetMenuVisible(false);
            PlayerController.SetMovementState(MovementState.None, false);

            StartCoroutine(FadeInOut(blockerGroup, 0, true));
            Invoke("LeaveToIntro", 1);
        }


        //subscribed to level change
        private void OnLevelUpdate(int level)
        {
            levelDisplay.text = StoreDatabase.GetLevelString();

            if (level >= StoreDatabase.Instance.levelXP.Length)
            {
                levelSlider.gameObject.SetActive(false);
                return;
            }

            levelSlider.value = 0;
            levelSlider.maxValue = StoreDatabase.Instance.levelXP[level];
        }


        //subscribed to experience change
        private void OnExperienceUpdate(long currentXP, long changeXP)
        {
            levelSlider.value = currentXP - StoreDatabase.GetExperienceRange(StoreDatabase.Instance.currentLevel).x;
        }


        //subscribed to money change
        private void OnMoneyUpdate(string money, string change)
        {
            moneyDisplay.text = money;
        }


        //subscribed to time change
        private void OnTimeUpdate(string time)
        {
            timeDisplay.text = time;
        }


        //subscribed to day event
        private void OnDayOver()
        {
            ShowMessage("Turn Open Sign to finish the day", false);
        }


        //subscribed to day event
        private void OnDayFinished()
        {
            PlayerController.SetMovementState(MovementState.None, false);          
            message.text = "";

            StartCoroutine(FadeInOut(blockerGroup, 0, true));
            Invoke("LeaveToNext", 1f);
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                switch(context.action.name)
                {
                    case "Cancel":
                        //if not currently controlling any object
                        if (!HasAction("Esc"))
                            SetMenuVisible(!menuParent.activeInHierarchy);
                        break;
                }
            }
        }


        //go back to the intro scene
        private void LeaveToIntro()
        {
            SceneManager.LoadScene(0);
        }


        //close the day and progress to the next scene
        //in this asset the daily statistics
        private void LeaveToNext()
        {
            SaveGameSystem.Save();
            SceneManager.LoadScene(nextScene);
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StoreDatabase.onLevelUpdate -= OnLevelUpdate;
            StoreDatabase.onExperienceUpdate -= OnExperienceUpdate;
            StoreDatabase.onMoneyUpdate -= OnMoneyUpdate;
            DayCycleSystem.onTimeUpdate -= OnTimeUpdate;
            DayCycleSystem.onDayOver -= OnDayOver;
            DayCycleSystem.onDayFinished -= OnDayFinished;
        }
    }
}
