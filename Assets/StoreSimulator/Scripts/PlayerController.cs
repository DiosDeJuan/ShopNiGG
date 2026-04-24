/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.InputSystem;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Let's the player control this character to move, look and jump around the scene.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        /// <summary>
        /// Speed for walking around.
        /// </summary>
        [Range(1, 20)]
        public int speed = 5;

        [Header("Rotation")]
        /// <summary>
        /// A reference to the player's camera transform.
        /// </summary>
        public Transform cameraTransform;

        /// <summary>
        /// Multiplier for looking around.
        /// </summary>
        [Range(0.1f, 2)]
        public float viewSensitivity = 0.5f;

        /// <summary>
        /// Clamping the values of looking up and down to not exceed certain angles.
        /// </summary>
        public Vector2 viewClamp = new Vector2(-40, 40);

        [Header("Gravity")]
        /// <summary>
        /// Multiplier of gravity applied to the player.
        /// </summary>
        [Range(0.1f, 2f)]
        public float gravityMultiplier = 0.3f;

        /// <summary>
        /// Upwards force applied when jumping. 
        /// </summary>
        [Range(1, 5)]
        public int jumpForce = 2;

        [Header("Hands")]
        /// <summary>
        /// A reference to the character's hands as parents when picking up objects.
        /// </summary>
        public Transform hands;

        [Header("Mobile")]
        /// <summary>
        /// Joysticks that should be visible when running on mobile platform only.
        /// </summary>
        public GameObject[] joysticks;

        //reference to the underlying CharacterController component
        private CharacterController characterController;
        //cached PlayerInput reference for subscription cleanup
        private PlayerInput playerInput;
        //currently allowed movement states
        private MovementState movementState = MovementState.All;
        //previous movement state before changing it
        private MovementState previousMovementState = MovementState.All;
        //movement input directly read from the keyboard/mobile joystick
        private Vector2 moveInput;
        //cache of movement input for further processing
        private Vector2 moveCache;
        //movement direction based on input cache and gravity
        private Vector3 moveDir;
        //rotation input directly read from the mouse/mobile joystick
        private Vector2 viewInput;
        //player rotation with sensitivity applied
        private Vector3 playerRotation;
        //camera rotation only, with sensitivity applied
        private Vector3 cameraRotation;
        //skip one update frame after locking mouse cursor to discard delta
        private bool skipMouseDelta = false;
        //gravity value including multiplier when not grounded
        private float gravityVelocity;
        //the package that is currently carried around
        private PackageObject handsPackage;


        //initialize references
        void Awake()
        {
            Instance = this;
            Cursor.lockState = CursorLockMode.Locked;

            characterController = GetComponent<CharacterController>();

            // Validate required inspector fields
            if(cameraTransform == null)
            {
                Debug.LogError("PlayerController: cameraTransform is not assigned in the inspector!");
            }
            if(hands == null)
            {
                Debug.LogError("PlayerController: hands transform is not assigned in the inspector!");
            }

            #if UNITY_ANDROID || UNITY_IOS
                if(joysticks != null)
                {
                    for(int i = 0; i < joysticks.Length; i++)
                    {
                        if(joysticks[i] != null)
                            joysticks[i].SetActive(true);
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerController: joysticks array is not assigned in the inspector. Mobile controls will not be available.");
                }
            #endif

        }


        //initialize variables
        void Start()
        {
            if(StoreDatabase.Instance != null && StoreDatabase.Instance.storeEntry != null)
            {
                transform.LookAt(StoreDatabase.Instance.storeEntry.position + Vector3.up);
            }

            //subscribe to player input here (Start) so that PlayerInput.Awake() has already
            //registered itself before we try to retrieve it via GetPlayerByIndex
            playerInput = PlayerInput.GetPlayerByIndex(0);
            if(playerInput != null)
            {
                #if UNITY_6000_0_OR_NEWER
                    playerInput.actions.FindActionMap("UI").Disable();
                #endif
                playerInput.onActionTriggered += OnAction;
            }
            else
            {
                Debug.LogError("PlayerController: no PlayerInput found. Movement and interaction will not work.");
            }
        }


        //apply different inputs
        void Update()
        {
            switch(movementState)
            {
                case MovementState.All:
                    ApplyGravity();
                    ApplyRotation();
                    ApplyMovement();
                    break;
                
                case MovementState.RotationOnly:
                    ApplyRotation();
                    break;
            }
        }


        /// <summary>
        /// Returns the last remembered movement state before it changed to the current state.
        /// </summary>
        public static MovementState GetPreviousMovementState()
        {
            return Instance.previousMovementState;
        }


        /// <summary>
        /// Returns the Transform component of the player's camera.
        /// </summary>
        public static Transform GetCameraTransform()
        {
            if(Instance == null) return null;
            return Instance.cameraTransform;
        }


        /// <summary>
        /// Apply a different fixed rotation i.e. when entering an object, like the CashDesk.
        /// </summary>
        public static void SetCameraRotation(Quaternion newRotation)
        {
            if(Instance != null)
                Instance.cameraRotation = newRotation.eulerAngles;
        }


        /// <summary>
        /// Change movement state to allow or disallow certain inputs.
        /// </summary>
        public static void SetMovementState(MovementState state, bool lockCursor)
        {
            if(Instance == null) return;

            if (Cursor.lockState == CursorLockMode.None && lockCursor == true)
            {
                Mouse.current.WarpCursorPosition(new Vector2(Screen.width / 2, Screen.height / 2));
                Instance.skipMouseDelta = true;
            }

            #if UNITY_ANDROID || UNITY_IOS
                if(Instance.joysticks != null)
                {
                    for(int i = 0; i < Instance.joysticks.Length; i++)
                    {
                        if(Instance.joysticks[i] != null)
                            Instance.joysticks[i].SetActive(state == MovementState.All);
                    }
                    // Enable view joystick for RotationOnly mode (if available)
                    if (state == MovementState.RotationOnly && Instance.joysticks.Length > 1 && Instance.joysticks[1] != null)
                        Instance.joysticks[1].SetActive(true);
                }
            #endif

            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Instance.previousMovementState = Instance.movementState;
            Instance.movementState = state;
        }


        /// <summary>
        /// Move PackageObject to the player's hands.
        /// </summary>
        public void Carry(PackageObject package)
        {
            if(package == null || hands == null) return;

            handsPackage = package;
            handsPackage.GetComponent<Rigidbody>().isKinematic = true;
            handsPackage.GetComponent<Animation>().Play("OpenPackage");

            InteractionSystem.MoveToTargetArc(package.transform, hands, Vector3.zero, Quaternion.identity);
        }


        /// <summary>
        /// Destroy or throw away the active package and reenable its physics. 
        /// </summary>
        public void Drop(bool withDestroy = false)
        {
            if(handsPackage == null) return;

            if (withDestroy)
                Destroy(handsPackage.gameObject);
            else
            {
                StopAllCoroutines(); //in case carry is still animating
                handsPackage.transform.SetParent(null);
                handsPackage.GetComponent<Animation>().Play("ClosePackage");
                handsPackage.GetComponent<Collider>().enabled = true;

                Rigidbody rigidbody = handsPackage.GetComponent<Rigidbody>();
                rigidbody.isKinematic = false;
                if(hands != null)
                    rigidbody.AddForce(hands.forward * 10, ForceMode.Impulse);
            }

            handsPackage = null;
        }


        //react on user input
        private void OnAction(InputAction.CallbackContext context)
        {
            switch(context.action.name)
            {
                case "Move":
                    moveInput = context.ReadValue<Vector2>();
                    break;
                case "View":
                    viewInput = context.ReadValue<Vector2>();
                    if (context.control.device.name == "Gamepad")
                        viewInput *= viewSensitivity;

                    break;
                case "Jump":
                    if (context.started)
                    {
                        ApplyJump();
                    }
                    break;
            }
        }


        //movement calculations
        private void ApplyMovement()
        {
            //prevent direction changes while jumping
            if (characterController.isGrounded) moveCache = moveInput;

            moveDir = transform.TransformDirection(new Vector3(moveCache.x, gravityVelocity, moveCache.y));
            characterController.Move(moveDir * speed * Time.deltaTime);
        }


        //rotation calculations
        private void ApplyRotation()
        {
            if(cameraTransform == null) return;

            //after locking the mouse cursor back to the screen center on desktop
            //we need to skip the first frame with non-zero mouse delta values
            //since otherwise Unity generates a high delta resulting in a camera jump
            if (skipMouseDelta)
            {
                if (viewInput == Vector2.zero)
                    return;

                skipMouseDelta = false;
                return;
            }

            //Player
            if (movementState == MovementState.All)
            {
                playerRotation.y += viewInput.x * viewSensitivity;
                transform.localRotation = Quaternion.Euler(playerRotation);
            }
            else //Camera Only
            {
                cameraRotation.y += viewInput.x * viewSensitivity;
            }

            //Camera
            cameraRotation.x += -viewInput.y * viewSensitivity;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x, viewClamp.x, viewClamp.y);
            cameraTransform.localRotation = Quaternion.Euler(cameraRotation);
        }


        //gravity calculations
        private void ApplyGravity()
        {
            if (characterController.isGrounded && gravityVelocity < 0)
            {
                gravityVelocity = -1;
            }
            else
            {
                gravityVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }
        }


        //jumping calculations
        private void ApplyJump()
        {
            if (movementState != MovementState.All)
                return;

            if (characterController.isGrounded)
            {
                gravityVelocity += jumpForce;
            }
        }


        //unsubscribe from player input on destroy
        void OnDestroy()
        {
            if(playerInput != null)
                playerInput.onActionTriggered -= OnAction;
        }
    }
}
