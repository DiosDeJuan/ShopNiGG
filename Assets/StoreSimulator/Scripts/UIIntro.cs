/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Main UI script in the intro scene.
    /// </summary>
    public class UIIntro : MonoBehaviour
    {
        /// <summary>
        /// Scene to load for the actual game.
        /// </summary>
        public string gameScene;

        /// <summary>
        /// Container of a background element, allowing easy access to its alpha value.
        /// This element prevents additional user input in scene transitions.
        /// </summary>
        public CanvasGroup blockerGroup;


        /// <summary>
        /// Lerp alpha value of the UI canvas group to let it fade in or out over time.
        /// </summary>
        public static IEnumerator FadeInOut(CanvasGroup group, float delay, bool fadeIn)
        {
            float startingAlpha = fadeIn ? 0 : 1;
            float targetAlpha = fadeIn ? 1 : 0;
            float lerpDuration = 0.5f;
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
        /// Create or load an existing save file and transition to the game scene.
        /// </summary>
        public void LoadGame(bool isNew)
        {
            if (isNew)
                SaveGameSystem.New();
            else
                SaveGameSystem.Load();

            StartCoroutine(FadeInOut(blockerGroup, 0, true));
            Invoke("LoadScene", 1);
        }


        /// <summary>
        /// Shut down the game.
        /// </summary>
        public void Exit()
        {
            Application.Quit();
        }


        //load scene
        private void LoadScene()
        {
            SceneManager.LoadScene(gameScene);
        }
    }
}
