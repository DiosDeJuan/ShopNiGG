/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// UI element representing customer reactions, showing additional text to the player.
    /// Fades in/out and rotates to always face the player.
    /// </summary>
    public class UISpeechBubble : MonoBehaviour
    {
        /// <summary>
        /// Label for displaying text inside the speech bubble.
        /// </summary>
        public TMP_Text content;

        /// <summary>
        /// Time in seconds the element should be visible, excluding fading.
        /// </summary>
        public float visibleTime;

        //reference to the canvas for controlling alpha
        private CanvasGroup canvasGroup;
        //cached reference of the player transform
        private Transform playerTrans;
        //cached transform
        private Transform thisTrans;


        /// <summary>
        /// Initialize after instantiation with reference to the player.
        /// </summary>
        public void Initialize()
        {
            playerTrans = PlayerController.Instance.transform;
            thisTrans = transform;
            
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }


        /// <summary>
        /// Fade in speech bubble with the text passed in. Then fade out.
        /// Additional calls while the speech bubble is already active will be skipped.
        /// </summary>
        public void Show(string text)
        {
            if (gameObject.activeInHierarchy)
                return;

            content.text = text;

            //cannot start coroutine on this script because the game object could be inactive
            UIGame.Instance.StartCoroutine(UIGame.FadeInOut(canvasGroup, 0, true)); //in
            UIGame.Instance.StartCoroutine(UIGame.FadeInOut(canvasGroup, 1 + visibleTime, false)); //out
        }


        //rotate to player, don't face up/down and
        //mirror on y-axis because it is facing the opposite direction 
        void LateUpdate()
        {
            Vector3 direction = (playerTrans.position - thisTrans.position).normalized;
            direction.y = 0;
            thisTrans.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180, 0);
        }
    }
}
