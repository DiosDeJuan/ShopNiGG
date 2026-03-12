/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Provides access to NavMeshAgent component and methods for navigation on a customer.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAgent : MonoBehaviour
    {
        /// <summary>
        /// Event fired each time the agent reached it's target destination.
        /// </summary>
        public event Action onDestinationReached;

        //reference to Unity's navigation agent component
        private NavMeshAgent agent;

        //the routine for WaitForDestination. Assigning this allows us to check
        //any running routine to make sure we only have one of it at all times
        private Coroutine waitRoutine = null;


        //initialize references
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }


        /// <summary>
        /// Provides access to the native Unity NavMeshAgent component.
        /// </summary>
        public NavMeshAgent GetNative()
        {
            return agent;
        }   


        /// <summary>
        /// Target destination to navigate to. Optionally callback upon reaching it can be disabled.
        /// </summary>
        public void SetDestination(Vector3 target, bool withCallback = true)
        {
            if (waitRoutine != null)
            {
                agent.isStopped = true;
                StopCoroutine(waitRoutine);
            }

            agent.isStopped = false;
            agent.SetDestination(target);
            waitRoutine = StartCoroutine(WaitForDestination(withCallback));
        }


        /// <summary>
        /// Check whether target position is within agent's stopping distance + buffer zone.
        /// </summary>
        public bool IsNear(Vector3 target)
        {
            return Vector3.Distance(transform.position, target) <= agent.stoppingDistance * 3;
        }


        /// <summary>
        /// Is the agent still moving?
        /// </summary>
        public bool IsStuck()
        {
            return agent.velocity.magnitude == 0;
        }


        //wait until the agent reached its destination
        private IEnumerator WaitForDestination(bool withCallback)
        {
            yield return new WaitForSeconds(0.5f);

            while (agent.pathPending)
                yield return null;

            while (agent.remainingDistance == Mathf.Infinity || (agent.remainingDistance - agent.stoppingDistance) > float.Epsilon || agent.pathStatus != NavMeshPathStatus.PathComplete)
                yield return null;

            waitRoutine = null;

            //invoke callback at destination
            if (withCallback)
                onDestinationReached?.Invoke();
        }
    }
}
