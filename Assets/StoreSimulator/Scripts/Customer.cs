/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Logic for customer AI integrating movement, shopping bag and cashdesk queueing for payment.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        /// <summary>
        /// The location for holding the shopping bag or payment type object.
        /// </summary>
        public Transform handsLocation;

        /// <summary>
        /// The location of the UISpeechBubble showing why this customer's happyness state changed.
        /// </summary>
        public Transform speechLocation;

        /// <summary>
        /// If waiting for payment but queue is full, delay between checking whether a queue spot is available. 
        /// </summary>
        public int waitDelay = 10;

        /// <summary>
        /// Returns whether this customer pays with cash or card.
        /// </summary>
        public bool payCash { get; private set; }

        /// <summary>
        /// Current action to be executed.
        /// </summary>
        public CustomerStep currentStep { get; private set; } 
        
        //the state of the customer may change during the shopping experience
        private bool isHappy = true;
        ///reference to the cached speech bubble
        private UISpeechBubble speechBubble;
        //reference to the navigation component
        private CustomerAgent agent;
        //reference to the shopping cart/bag
        private CustomerCart cart;
        //reference to animation controller
        private Animator animator;
        //reference to randomly selected CashDesk when queueing up
        private CheckoutObject checkout;
        //position spawned for returning to it when finished
        private Vector3 spawnPosition;


        //initialize references
        void Awake()
        {
            agent = GetComponent<CustomerAgent>();
            cart = GetComponent<CustomerCart>();
            animator = GetComponent<Animator>();

            currentStep = CustomerStep.GoToStore;
            agent.onDestinationReached += OnDestinationReached;
        }


        //initialize variables
        void Start()
        {
            spawnPosition = transform.position;
            payCash = Random.Range(100, 0) <= CustomerSystem.Instance.payCashRate;

            agent.SetDestination(StoreDatabase.Instance.storeEntry.position);
        }


        /// <summary>
        /// Called when queueing up initially, or proceeding in the queue (by CashDesk).
        /// </summary>
        public void ProceedQueue(Transform queuePos, int queueIndex)
        {
            //first in line
            if (queueIndex == 1)
                currentStep = CustomerStep.Pay;

            //no callback in case we just made it into the queue
            //we leave the "proceed in queue" logic to the CashDesk
            agent.SetDestination(queuePos.position, currentStep == CustomerStep.Pay);
        }


        /// <summary>
        /// For CashDesk, called when all items have been scanned, or if the player re-enters.
        /// On Self Checkout this is called automatically at the end with the collider disabled.
        /// </summary>
        public void ProceedPayment(bool withCollider)
        {
            if (currentStep != CustomerStep.Pay)
                return;

            //instantiate payment type object in hands            
            GameObject payPrefab = payCash ? CustomerSystem.Instance.cashPrefab : CustomerSystem.Instance.cardPrefab;
            GameObject payInstance = Instantiate(payPrefab, handsLocation.position, Quaternion.identity, handsLocation);
            payInstance.GetComponent<PaymentItem>().Initialize(this);
            payInstance.GetComponent<Collider>().enabled = withCollider;

            animator.SetBool("IsPaying", true);
        }


        /// <summary>
        /// Destroy payment method object as the payment is temporarily paused or already finshed.
        /// </summary>
        public void PausePayment()
        {
            if (currentStep != CustomerStep.Pay)
                return;

            //Lower Hand to cancel Payment
            PaymentItem paymentItem = GetComponentInChildren<PaymentItem>();
            if (paymentItem != null)
            {
                Destroy(paymentItem.gameObject);
                animator.SetBool("IsPaying", false);
            }
        }


        /// <summary>
        /// Called by PaymentItem when it was interacted with.
        /// The player clicked, or cashier interacted with it. Continue checkout.
        /// </summary>
        public void HasPaid()
        {
            PausePayment();
            checkout.ActivateCheckout();
        }


        /// <summary>
        /// Shows the reason for an unhappy reaction in a speech bubble and decreases experience.
        /// </summary>
        public void ShowUnhappy(string failReason)
        {
            if (isHappy)
            {
                StoreDatabase.AddRemoveExperience(-1);
                isHappy = false;
            }

            if (speechBubble == null)
            {
                if (speechLocation == null || CustomerSystem.Instance.speechPrefab == null)
                    return;

                speechBubble = Instantiate(CustomerSystem.Instance.speechPrefab, speechLocation).GetComponent<UISpeechBubble>();   
                speechBubble.Initialize();
            }

            speechBubble.Show(failReason);
        }


        /// <summary>
        /// Return to initial spawn position and destroy.
        /// </summary>
        public void GoHome()
        {
            currentStep = CustomerStep.GoHome;
            agent.SetDestination(spawnPosition);

            CustomerSystem.CustomerLeft(isHappy);
        }


        //method override for root motion on the animator
        private void OnAnimatorMove()
        {
            NavMeshAgent navMeshAgent = agent.GetNative();
            //calculate variables based on movement
            Vector3 velocity = Quaternion.Inverse(transform.rotation) * navMeshAgent.desiredVelocity;
            float angle = Mathf.Atan2(velocity.x, velocity.z) * 180.0f / 3.14159f;

            //push variables to the animator with some optional damping
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            animator.SetFloat("Direction", angle, 0.15f, Time.deltaTime);
        }


        //callback whenever a navigation destination has been reached
        //this method decides what to do determined by the then-current CustomerStep value
        private void OnDestinationReached()
        {
            switch(currentStep)
            {
                //on spawn
                case CustomerStep.GoToStore:
                    //store closed
                    if (DayCycleSystem.GetStoreOpenState() == StoreOpenState.Closed)
                    {
                        agent.SetDestination(spawnPosition);
                        break;
                    }

                    AudioSystem.Play3D(StoreDatabase.Instance.entryClip, StoreDatabase.Instance.storeEntry.position);

                    //intialize shopping bag and start collecting
                    cart.CreateWishlist();
                    Collect();
                    break;

                //after each item collection
                case CustomerStep.Collect:
                    //there could be more items to collect
                    Collect();
                    break;

                //nothing more to collect, go to CashDesk
                case CustomerStep.Queue:
                    //checkout has been moved
                    //try to find another checkout for queuing up
                    if (checkout == null)
                    {
                        Collect();
                        break;
                    }

                    //try to add this customer to queue
                    (Transform queuePos, int queueIndex) = checkout.AddCustomerToQueue(this);
                    //if queue full - wait coroutine
                    if (queuePos == null) StartCoroutine(CheckQueueStatus());
                    else
                    {
                        //go to assigned queue position
                        ProceedQueue(queuePos, queueIndex);
                    }
                    break;

                //we are the first in queue
                //look at cashier position and unpack bag
                case CustomerStep.Pay:
                    //checkout has been moved
                    //try to find another checkout for queuing up
                    if (checkout == null)
                    {
                        Collect();
                        break;
                    }

                    StartCoroutine(LookAtY(checkout.transform.position));
                    checkout.PlaceBagContents(cart);
                    break;
                
                //returned to spawn, despawn
                case CustomerStep.GoHome:
                    Destroy(gameObject);
                    break;
            }
        }


        //method called for each item in the shopping bag, it tries to find the desired item and navigates to it
        //if all items have been processed it moves on to queuing up on a random CashDesk
        private void Collect()
        {
            //still items left to collect, and bag has space
            if (cart.ShouldCollect() && cart.CanCollect())
            {
                //we are near the current item
                if (agent.IsNear(cart.GetProductPlacement().position))
                {
                    //compare store price with market price
                    //do a random willingness to pay factor from +20% to +75%
                    ProductScriptableObject product = cart.GetProduct();
                    long maxPrice = Mathf.FloorToInt(product.marketPrice * (1 + Random.Range(0.2f, 0.75f)));

                    if (product.storePrice > maxPrice)
                    {
                        //item cannot be reached or agent stuck
                        ShowUnhappy("Product " + product.name + " is too expensive.");

                        //continue with next item
                        cart.SetNextItem();
                        //delay to fully show animation
                        animator.Play("Question");
                        Invoke("Collect", 5);
                        return;
                    }

                    //decide whether to collect multiples of this item
                    int itemCount = 1;
                    bool shouldDuplicate = Random.Range(100, 0) <= CustomerSystem.Instance.duplicateProductRate;
                    if (shouldDuplicate)
                        itemCount = Random.Range(3, 1);

                    //put item in bag
                    for(int i = 0; i < itemCount; i++)
                    {
                        cart.Add();

                        //stop if bag full
                        if (cart.GetMissingCount() == 0)
                            break;
                    }

                    //continue with next item
                    cart.SetNextItem();
                    //delay to fully show animation
                    animator.Play("Grab");
                    Invoke("Collect", 2);
                    return;
                }
                else if (agent.IsStuck())
                {
                    //item cannot be reached or agent stuck
                    ShowUnhappy("Could not get to " + cart.GetProduct().name + ".");

                    //continue with next item
                    cart.SetNextItem();
                    //delay to fully show animation
                    animator.Play("Question");
                    Invoke("Collect", 5);
                    return;
                }
            }

            //all items processed or bag full, go to random CashDesk
            if (!cart.ShouldCollect() && cart.GetItemsCount() > 0 || cart.GetMissingCount() == 0)
            {
                currentStep = CustomerStep.Queue;
                checkout = StoreDatabase.GetRandomCheckout();
                //check if there is a checkout in the scene
                if (checkout == null)
                {
                    ShowUnhappy("Could not find any Checkout.");
                    GoHome();
                }
                else
                    agent.SetDestination(checkout.GetLastQueuePosition());

                return;
            }

            //all items processed but bag is empty, go home
            if (!cart.ShouldCollect() && cart.GetItemsCount() == 0)
            {
                GoHome();
                return;
            }

            //find item position in store and move to it
            //upon reaching it this method will be called again via OnDestinationReached()
            currentStep = CustomerStep.Collect;
            Transform grabSpot = cart.GetProductPlacement();
            if (grabSpot != null)
            {
                agent.SetDestination(grabSpot.position);
            }
            else
            {
                //item not found in store
                ShowUnhappy("Product " + cart.GetProduct().name + " not found.");

                //continue with next item
                cart.SetNextItem();
                //delay to fully show animation
                animator.Play("Question");
                Invoke("Collect", 5);
                return;
            }
        }


        //turn to look at target position on Y-axis only
        private IEnumerator LookAtY(Vector3 target)
        {
            float lerpProgress = 0f;
            float lerpSpeed = 2f;
            Vector3 direction = target - transform.position;
            direction.y = 0;
            Quaternion startingRotation = transform.rotation; 
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            while (lerpProgress < 1f)
            {
                //update lerp progress
                lerpProgress += lerpSpeed * Time.deltaTime;

                //rotate linearly
                Quaternion linearRotation = Quaternion.Lerp(startingRotation, targetRotation, lerpProgress);
                transform.rotation = linearRotation;

                yield return null;
            }

            transform.rotation = targetRotation;
        }


        //waiting for CashDesk queue assignment, if current queue is full
        private IEnumerator CheckQueueStatus()
        {
            while(true)
            {
                yield return new WaitForSeconds(waitDelay);

                //in case the checkout object was moved while waiting in queue
                if (checkout == null)
                {
                    //try to find a new one
                    checkout = StoreDatabase.GetRandomCheckout();
                    //cancel if there is no checkout available
                    if (!checkout)
                    {
                        GoHome();
                        yield break;
                    }
                }

                (Transform queuePos, int queueIndex) = checkout.AddCustomerToQueue(this);
                if (queuePos == null)
                {
                    //wander around the last waiting position randomly
                    int attemptsLeft = 10;
                    Vector3 randomPoint = checkout.GetLastQueuePosition() + Random.insideUnitSphere * agent.GetNative().height;

                    while (attemptsLeft > 0)
                    {
                        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                        {
                            //if the target position is too close on the NavMesh edge we cannot use that
                            //since the agent would get stuck when moving close to out of the NavMesh
                            if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas))
                            {
                                if (Vector3.Distance(hit.position, edge.position) < agent.GetNative().stoppingDistance * 2)
                                {
                                    attemptsLeft--;
                                    continue;
                                }
                            }

                            agent.SetDestination(hit.position, false);
                            attemptsLeft = 0;
                        }

                        attemptsLeft--;
                    }
                }
                else
                {
                    ProceedQueue(queuePos, queueIndex);
                    yield break;
                }
            }
        }


        //unsubscribe from events
        void OnDestroy()
        {
            agent.onDestinationReached -= OnDestinationReached;
        }
    }
}
