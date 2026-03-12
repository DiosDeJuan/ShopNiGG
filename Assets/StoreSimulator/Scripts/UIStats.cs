/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using SimpleJSON;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI script for showing statistical data for the day that passed as a summary.
    /// </summary>
    public class UIStats : MonoBehaviour
    {
        /// <summary>
        /// Scene to load next.
        /// </summary>
        public string nextScene;

        /// <summary>
        /// Button for invoking the scene change, shown at the end.
        /// </summary>
        public GameObject continueButton;

        /// <summary>
        /// Element for fading to black to prepare the scene transition.
        /// </summary>
        public CanvasGroup blockerGroup;

        /// <summary>
        /// References to all value labels that should be animated to show up one after another.
        /// </summary>
        public GameObject[] showArray;

        /// <summary>
        /// Label displaying the number of day played.
        /// </summary>
        public TMP_Text dayNumber;

        /// <summary>
        /// Label displaying the total money earned this day.
        /// </summary>
        public TMP_Text moneyEarned;

        /// <summary>
        /// Label displaying the total money spent this day.
        /// </summary>
        public TMP_Text moneySpent;

        /// <summary>
        /// Label displaying the profit, or loss, of money this day.
        /// </summary>
        public TMP_Text moneyProfit;

        /// <summary>
        /// Label displaying the current amount of money left.
        /// </summary>
        public TMP_Text moneyCurrent;

        /// <summary>
        /// Label displaying the total number of customers served.
        /// </summary>
        public TMP_Text customersTotal;

        /// <summary>
        /// Label displaying the number of customers that were happy.
        /// </summary>
        public TMP_Text customersHappy;

        /// <summary>
        /// Label displaying the number of customers that were unhappy.
        /// </summary>
        public TMP_Text customersUnhappy;

        /// <summary>
        /// Label displaying the total experience earned this day.
        /// </summary>
        public TMP_Text xpEarned;

        /// <summary>
        /// Label displaying the current player level.
        /// </summary>
        public TMP_Text xpLevel;


        //initialize references
        void Awake()
        {
            continueButton.SetActive(false);

            for(int i = 0; i < showArray.Length; i++)
            {
                TMP_Text[] texts = showArray[i].GetComponentsInChildren<TMP_Text>();
                for(int j = 0; j < texts.Length; j++)
                    texts[j].enabled = false;
            }

            blockerGroup.gameObject.SetActive(true);
            StartCoroutine(FadeInOut(blockerGroup, 0, false));
        }


        //initialize variables
        void Start()
        {
            //a SaveLoadSystem instance is required to continue
            //the game should be started as usual from the intro scene for this
            if (SaveGameSystem.Instance == null)
            {
                Debug.LogWarning("No SaveLoadSystem Instance found, data cannot be loaded.\n" + 
                                "To test this scene, transition from a Game scene or temporarily add a SaveLoadSystem component.");
                return;
            }

            //reload data and apply to game systems in next scene load
            //this will be the game scene again after we have skipped this scene
            SaveGameSystem.Load();

            //selectively read DayCycleSystem data
            JSONNode cycleData = SaveGameSystem.ReadComponentData("DayCycleSystem");
            dayNumber.text = "DAY " + (cycleData["currentDay"].AsInt - 1);

            //selectively read StatisticsDatabase data
            JSONNode dailyData = SaveGameSystem.ReadComponentData("StatsDatabase");
            moneyEarned.text = StoreDatabase.FromLongToStringMoney(dailyData["moneyEarned"].AsLong);
            moneySpent.text = StoreDatabase.FromLongToStringMoney(dailyData["moneySpent"].AsLong);
            moneyProfit.text = StoreDatabase.FromLongToStringMoney(dailyData["moneyEarned"].AsLong + dailyData["moneySpent"].AsLong);
            customersTotal.text = (dailyData["customersHappy"].AsInt + dailyData["customersUnhappy"].AsInt).ToString();           
            customersHappy.text = dailyData["customersHappy"].Value;
            customersUnhappy.text = dailyData["customersUnhappy"].Value;
            xpEarned.text = Mathf.Clamp(dailyData["xpEarned"].AsInt, 0, int.MaxValue).ToString();

            //selectively read StoreDatabase data
            JSONNode storeData = SaveGameSystem.ReadComponentData("StoreDatabase");
            moneyCurrent.text = StoreDatabase.FromLongToStringMoney(storeData["currentMoney"].AsLong);
            xpLevel.text = storeData["currentLevel"].Value;

            StartCoroutine(AnimateActive());
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
        /// Leave the scene and continue with the next scene,
        /// which should be the game scene again.
        /// </summary>
        public void Continue()
        {
            StartCoroutine(FadeInOut(blockerGroup, 0, true));
            Invoke("LeaveScene", 1);
        }


        //load the next scene
        private void LeaveScene()
        {
            SceneManager.LoadScene(nextScene);
        }


        //show the text components one after the other until the end
        private IEnumerator AnimateActive()
        {
            for(int i = 0; i < showArray.Length; i++)
            {
                yield return new WaitForSeconds(0.2f);

                TMP_Text[] texts = showArray[i].GetComponentsInChildren<TMP_Text>();
                for(int j = 0; j < texts.Length; j++)
                    texts[j].enabled = true;
            }

            yield return new WaitForSeconds(2);
            continueButton.SetActive(true);
        }
    }
}
