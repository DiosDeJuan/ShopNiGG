/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Visual representation of a single tutorial step.
    /// </summary>
    public class UITutorial : MonoBehaviour
    {
        /// <summary>
        /// Label for displaying the tutorial title.
        /// </summary>
        public TMP_Text title;

        /// <summary>
        /// Label for displaying the tutorial contents.
        /// </summary>
        public TMP_Text content;

        /// <summary>
        /// Label for displaying the steps and total progress.
        /// </summary>
        public TMP_Text progress;
        
        /// <summary>
        /// Reference to border image for changing the coloring when completed.
        /// </summary>
        public Image border;

        //duration for the slide in animation
        private float slideDuration = 0.5f;
        //the rect of the UI element to slide into the screen
        private RectTransform animateRect;
        //reference to the canvas for controlling alpha
        private CanvasGroup canvasGroup;
        //the time remaining for sliding in from full duration
        private float remainingSlide;
        //reference to the tutorial data used for later access
        private TutorialScriptableObject data;


        /// <summary>
        /// Initialization for filling out general details about the tutorial.
        /// Could override starting step, then fades it in and moves it to the screen.
        /// </summary>
        public void Initialize(TutorialScriptableObject tutorial, int startingCount = 0)
        {
            data = tutorial;
            title.text = data.title;
            content.text = data.description;

            if (data.requiredCount > 1) progress.text = startingCount + " / " + data.requiredCount;
            else progress.gameObject.SetActive(false);

            TutorialSystem.onProgressUpdate += OnProgressUpdate;

            remainingSlide = slideDuration;
            animateRect = transform.GetChild(0).GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            UIGame.Instance.StartCoroutine(UIGame.FadeInOut(canvasGroup, 0, true)); //in
        }


        //slide in and update slider with remaining visible time
        void LateUpdate()
        {
            remainingSlide -= Time.deltaTime;
            if (remainingSlide > 0)
                animateRect.localPosition = new Vector3(-animateRect.rect.width * (remainingSlide / slideDuration), animateRect.localPosition.y, animateRect.localPosition.z);
        }


        //subscribed to new progress values
        private void OnProgressUpdate(int value)
        {
            bool isCompleted = value == data.requiredCount;

            if (!isCompleted)
            {
                progress.text = value + " / " + data.requiredCount;
                return;
            }

            progress.gameObject.SetActive(false);
            border.gameObject.SetActive(true);
            TutorialSystem.onProgressUpdate -= OnProgressUpdate;

            AudioSystem.Play2D(UIGame.Instance.tutorialClip);
            UIGame.Instance.StartCoroutine(UIGame.FadeInOut(canvasGroup, 5, false)); //out
        }


        //gets called after fading out
        void OnDisable()
        {
            Destroy(gameObject);
        }


        //unsubscribe from events
        void OnDestroy()
        {
            TutorialSystem.onProgressUpdate -= OnProgressUpdate;
        }
    }
}
