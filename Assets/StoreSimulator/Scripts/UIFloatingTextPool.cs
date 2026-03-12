/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using TMPro;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Handles pooled instantiation of floating text to visually show changes in experience and money.
    /// </summary>
	public class UIFloatingTextPool : MonoBehaviour
	{
        /// <summary>
        /// Prefab instantiated for displaying floating text.
        /// </summary>
        public GameObject floatingPrefab;

        /// <summary>
        /// Container transform that acts as the parent for existing floating text prefab instances.
        /// </summary>
        public Transform container;

        //our pool of one-shot texts
        private IObjectPool<TMP_Text> objectPool;
        //capacity to instantiate at launch
        private int defaultPoolCapacity = 10;
        //cache of level slider transform
        private Transform experienceLocation;
        //cache of money display transform
        private Transform moneyLocation;
        //offset to animate the text to or from base position
        private Vector3 offset = new Vector3(0, 75, 0);


        //initialize references
        void Awake()
        {
            StoreDatabase.onExperienceUpdate += OnExperienceUpdate;
            StoreDatabase.onMoneyUpdate += OnMoneyUpdate;

            objectPool = new ObjectPool<TMP_Text>(CreateObject, Spawn, Despawn, null, true, defaultPoolCapacity);

            //pre-warm
            TMP_Text[] temp = new TMP_Text[defaultPoolCapacity];
            for(int i = 0; i < defaultPoolCapacity; i++) temp[i] = objectPool.Get();
            for(int i = 0; i < temp.Length; i++) objectPool.Release(temp[i]);
        }


        //initialize variables
        void Start()
        {
            experienceLocation = UIGame.Instance.levelSlider.transform;
            moneyLocation = UIGame.Instance.moneyDisplay.transform;
        }


        //subscribed to experience change
        private void OnExperienceUpdate(long currentXP, long changeXP)
        {
            if (!UIGame.Instance.hudParent.activeInHierarchy)
                return;

            TMP_Text floating = objectPool.Get();
            floating.text = changeXP.ToString();

            bool isNegative = changeXP < 0;
            floating.color = isNegative ? Color.red : Color.green;
            floating.transform.position = experienceLocation.position;
            if (!isNegative) floating.text = "+" + floating.text;

            //deactivate object after some delay
            StartCoroutine(Move(floating, isNegative));
        }


        //subscribed to money change
        private void OnMoneyUpdate(string money, string change)
        {
            if (!UIGame.Instance.hudParent.activeInHierarchy)
                return;

            TMP_Text floating = objectPool.Get();
            floating.text = change;

            bool isNegative = change[0] == '-';
            floating.color = isNegative ? Color.red : Color.green;
            floating.transform.position = moneyLocation.position;
            if (!isNegative) floating.text = "+" + floating.text;

            //deactivate object after some delay
            StartCoroutine(Move(floating, isNegative));
        }


        //invoked when creating an item to populate the object pool
        private TMP_Text CreateObject()
        {
            GameObject objectInstance = Instantiate(floatingPrefab, container, false);
            return objectInstance.GetComponent<TMP_Text>();
        }


        //invoked when retrieving the next item from the object pool
        private void Spawn(TMP_Text pooledObject)
        {
            pooledObject.gameObject.SetActive(true);
        }


        //invoked when returning an item to the object pool
        private void Despawn(TMP_Text pooledObject)
        {
            pooledObject.gameObject.SetActive(false);
        }


        //our method to automatically move and despawn a text
        private IEnumerator Move(TMP_Text pooledObject, bool isNegative)
        {
            //cache instance to deactivate
            GameObject obj = pooledObject.gameObject;
            Transform trans = pooledObject.transform;

            float lerpDuration = 2f;
            float lerpProgress = 0f;

            //position negative text slightly below original to not interfere with it that much
            Vector3 startPosition = isNegative ? trans.position - new Vector3(0 , pooledObject.preferredHeight / 2, 0) : trans.position - offset;
            Vector3 endPosition = isNegative ? trans.position - offset : trans.position;

            while (obj.activeInHierarchy && lerpProgress < lerpDuration)
            {
                lerpProgress += Time.deltaTime;

                float a = Mathf.Lerp(1, 0, lerpProgress / lerpDuration);
                pooledObject.alpha = a;
                
                trans.position = Vector3.Lerp(startPosition, endPosition, lerpProgress / lerpDuration);
                yield return null;
            }

            pooledObject.alpha = 0;
            //the instance got deactivated in between already
            if (!obj.activeInHierarchy) yield break;

            objectPool.Release(pooledObject);
        }


        //unsubscribe from events
        void OnDestroy()
        {
            StoreDatabase.onExperienceUpdate -= OnExperienceUpdate;
            StoreDatabase.onMoneyUpdate -= OnMoneyUpdate;
        }
    }
}