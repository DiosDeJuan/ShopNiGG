/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Handles playback of background music, 2D and 3D one-shot clips during the game.
    /// Makes use of the object pooling for activating 3D AudioSources at desired world positions.
    /// </summary>
	public class AudioSystem : MonoBehaviour
	{
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static AudioSystem Instance { get; private set; }

        /// <summary>
        /// AudioSource for playing back lengthy music clips.
        /// </summary>
		public AudioSource musicSource;

        /// <summary>
        /// AudioSource for playing back one-shot 2D clips.
        /// </summary>
		public AudioSource audioSource;

        /// <summary>
        /// Prefab instantiated for playing back one-shot 3D clips.
        /// </summary>
        public GameObject oneShotPrefab;

        /// <summary>
        /// Array for storing background music clips, so they can be
        /// referenced in PlayMusic() by passing in their index value.
        /// </summary>
        public AudioClip[] musicClips;

        //our pool of one-shot audio sources
        private IObjectPool<AudioSource> objectPool;
        //capacity to instantiate at launch
        private int defaultPoolCapacity = 10;


        //initialize references
        void Awake()
        {
            Instance = this;

            objectPool = new ObjectPool<AudioSource>(CreateObject, Spawn, Despawn, null, true, defaultPoolCapacity);

            //pre-warm
            AudioSource[] temp = new AudioSource[defaultPoolCapacity];
            for(int i = 0; i < defaultPoolCapacity; i++) temp[i] = objectPool.Get();
            for(int i = 0; i < temp.Length; i++) objectPool.Release(temp[i]);
        }


        /// <summary>
        /// Play sound clip in 2D on the background audio source.
        /// There can only be one music clip playing at the same time.
        /// Only plays music if the audio component is enabled.
        /// </summary>
        public static void PlayMusic(int index)
        {
            Instance.musicSource.clip = Instance.musicClips[index];

            if (Instance.musicSource.enabled)
                Instance.musicSource.Play();
        }


        /// <summary>
        /// Play sound clip passed in in 2D space.
        /// </summary>
        public static void Play2D(AudioClip clip)
        {
            //cancel execution if clip wasn't set
            if (clip == null) return;

            Instance.audioSource.PlayOneShot(clip);
        }


        /// <summary>
        /// Play sound clip passed in in 3D space, with optional random pitch (0-1 range).
        /// Automatically gets an audio source for playback using object pooling.
        /// </summary>
        public static void Play3D(AudioClip clip, Vector3 position, float pitch = 0f)
        {
            //cancel execution if clip wasn't set
            if (clip == null) return;
            //calculate random pitch in the range around 1, up or down
            pitch = Random.Range(1 - pitch, 1 + pitch);

            //activate new audio gameobject from pool
            AudioSource audioSource = Instance.objectPool.Get();
            audioSource.transform.position = position;
            audioSource.clip = clip;
            audioSource.pitch = pitch;
            audioSource.Play();
            
            //deactivate audio object when the clip stops playing
            Instance.StartCoroutine(Instance.Despawn(audioSource, clip.length));
        }


        //invoked when creating an item to populate the object pool
        private AudioSource CreateObject()
        {
            GameObject objectInstance = Instantiate(Instance.oneShotPrefab, transform);
            return objectInstance.GetComponent<AudioSource>();
        }


        //invoked when retrieving the next item from the object pool
        private void Spawn(AudioSource pooledObject)
        {
            pooledObject.gameObject.SetActive(true);
        }


        //invoked when returning an item to the object pool
        private void Despawn(AudioSource pooledObject)
        {
            pooledObject.gameObject.SetActive(false);
        }


        //our method to automatically despawn an audio source after a delay
        private IEnumerator Despawn(AudioSource pooledObject, float delay)
        {
            //cache instance to deactivate
            GameObject obj = pooledObject.gameObject;

            //wait for defined seconds
            float timer = Time.time + delay;
            while (obj.activeInHierarchy && Time.time < timer)
                yield return null;

            //the instance got deactivated in between already
            if (!obj.activeInHierarchy) yield break;

            objectPool.Release(pooledObject);
        }
    }
}