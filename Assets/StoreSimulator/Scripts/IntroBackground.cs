/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Adding some customers walking in the background to the Intro scene.
    /// </summary>
    public class IntroBackground : MonoBehaviour
    {
        /// <summary>
        /// Minimum and maximum delay in seconds between customer spawns.
        /// </summary>
        public Vector2Int minMaxDelay;

        /// <summary>
        /// A reference to the CustomerSystem to access its public variables.
        /// </summary>
        public CustomerSystem customerSystem;

        
        //the coroutine randomly spawning customers
        IEnumerator Start()
        {
            //one to begin
            Spawn();

            while(true)
            {
                yield return new WaitForSeconds(Random.Range(minMaxDelay.x, minMaxDelay.y));
                Spawn();
            }
        }


        //the actual spawn method
        private void Spawn()
        {
            Transform[] spawnLocations = customerSystem.spawnLocations;
            int pathIndex = Random.Range(0, 1 + 1);
            Transform startLocation = spawnLocations[pathIndex];
            Transform endLocation = spawnLocations[pathIndex + 2];

            int prefabCount = customerSystem.customerPrefabs.Length;
            GameObject obj = Instantiate(customerSystem.customerPrefabs[Random.Range(0, prefabCount)], startLocation.position, Quaternion.identity);

            obj.GetComponent<Customer>().enabled = false;
            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();

            agent.SetDestination(endLocation.position);
            obj.GetComponent<Animator>().SetFloat("Speed", agent.speed);

            Destroy(obj, 20);
        }
    }
}