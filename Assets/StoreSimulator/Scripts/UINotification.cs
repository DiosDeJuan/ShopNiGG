/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Visual representation of important in-game events.
    /// </summary>
    public class UINotification : MonoBehaviour
    {
        /// <summary>
        /// Reference to image for changing the coloring or icon itself.
        /// </summary>
        public Image image;

        /// <summary>
        /// Label for displaying the notification contents.
        /// </summary>
        public TMP_Text content;

        /// <summary>
        /// Slider for showing the remaining time the notification is visible.
        /// </summary>
        public Slider slider;

        /// <summary>
        /// Time in seconds the element should be visible, excluding fading.
        /// </summary>
        public float visibleTime;

        //duration for the slide in animation
        private float slideDuration = 0.5f;
        //the rect of the UI element to slide into the screen
        private RectTransform animateRect;
        //reference to the canvas for controlling alpha
        private CanvasGroup canvasGroup;
        //the time visible remaining from the full duration
        private float remainingTime;
        //the time remaining for sliding in from full duration
        private float remainingSlide;


        /// <summary>
        /// Initialization for filling out general details about the element.
        /// Could override default variables, then fades it in and moves it to the screen.
        /// </summary>
        public void Initialize(string text, Sprite otherIcon = null, Color? otherColor = null, float otherDuration = 0)
        {
            content.text = text;

            if (otherIcon) image.sprite = otherIcon;
            if (otherColor != null) image.color = otherColor.Value;
            if (otherDuration > 0) visibleTime = otherDuration;

            remainingTime = visibleTime;
            remainingSlide = slideDuration;
            animateRect = transform.GetChild(0).GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            //cannot start coroutine on this script because the game object could be inactive
            UIGame.Instance.StartCoroutine(UIGame.FadeInOut(canvasGroup, 0, true)); //in
            UIGame.Instance.StartCoroutine(UIGame.FadeInOut(canvasGroup, visibleTime - 1, false)); //out
        }


        //slide in and update slider with remaining visible time
        void LateUpdate()
        {
            remainingSlide -= Time.deltaTime;
            if (remainingSlide > 0)
                animateRect.localPosition = new Vector3(animateRect.rect.width * (remainingSlide / slideDuration), animateRect.localPosition.y, animateRect.localPosition.z);

            remainingTime -= Time.deltaTime;
            slider.value = remainingTime / visibleTime;
        }


        //destroy to free space in layout group
        void OnDisable()
        {
            Destroy(gameObject);
        }
    }
}
