/*  This file is part of the "Store Simulator" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from an official reseller (Unity Asset Store, Epic FAB).
 *  You shall not license, sublicense, sell, resell, transfer, assign, distribute or otherwise make available to any third party the Service or the Content. */

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Allowed PlayerController movement state.
    /// </summary>
    public enum MovementState
    {
        /// <summary>
        /// Movement and Camera input disabled.
        /// </summary>
        None,
        /// <summary>
        /// Movement disabled, Camera rotation allowed.
        /// </summary>
        RotationOnly,
        /// <summary>
        /// Movement and Camera input allowed.
        /// </summary>
        All
    }

    /// <summary>
    /// Type of objects the InteractionSystem should cast the raycast against.
    /// </summary>
    public enum InteractionState
    {
        /// <summary>
        /// Disable raycasting altogether.
        /// </summary>
        None,
        /// <summary>
        /// Cast against all objects (on the correct layer).
        /// </summary>
        All
    }

    /// <summary>
    /// States of the store.
    /// </summary>
    public enum StoreOpenState
    {
        /// <summary>
        /// Not opened yet (preparation phase).
        /// </summary>
        Waiting,
        /// <summary>
        /// Store opened.
        /// </summary>
        Open,
        /// <summary>
        /// After opening hours.
        /// </summary>
        Closed
    }

    /// <summary>
    /// Used for raycast states in StorageSystem and PlacementSystem.
    /// </summary>
    public enum PlacementMode
    {
        /// <summary>
        /// Raycasting inactive.
        /// </summary>
        Inactive = 0,
        /// <summary>
        /// Raycast hit nothing.
        /// </summary>
        Outside = 1,
        /// <summary>
        /// Raycast hit something but unable to place there.
        /// </summary>
        Invalid = 2,
        /// <summary>
        /// Raycast detected valid placement position.
        /// </summary>
        Valid = 3
    }

    /// <summary>
    /// Multi-purpose axis selection, i.e. which axis to fill first on PlacementObject.
    /// </summary>
    public enum Axis
    {
        X,
        Y
    }


    /// <summary>
    /// Type of product or storage.
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// Not using a specific storage type.
        /// </summary>
        Default,
        /// <summary>
        /// For cooled products and storage objects.
        /// </summary>
        Cooled,
        /// <summary>
        /// For frozen products and storage objects.
        /// </summary>
        Frozen
    }

    /// <summary>
    /// Actions a customer can perform.
    /// </summary>
    public enum CustomerStep
    {
        /// <summary>
        /// Going to the store.
        /// </summary>
        GoToStore,
        /// <summary>
        /// In loop for collecting items.
        /// </summary>
        Collect,
        /// <summary>
        /// Going to or standing at a CashDesk.
        /// </summary>
        Queue,
        /// <summary>
        /// First in line for payment.
        /// </summary>
        Pay,
        /// <summary>
        /// Going home with or without items.
        /// </summary>
        GoHome
    }


    /// <summary>
    /// Type of decorations bought at the computer.
    /// </summary>
    public enum DecorationType
    {
        /// <summary>
        /// Customizing the wall texture.
        /// </summary>
        Wall,
        /// <summary>
        /// Customizing the floor texture.
        /// </summary>
        Floor
    }


    /// <summary>
    /// Supported document types for upload.
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// Binary save data file.
        /// </summary>
        SaveData,
        /// <summary>
        /// JSON formatted data file.
        /// </summary>
        Json,
        /// <summary>
        /// Plain text file.
        /// </summary>
        Text,
        /// <summary>
        /// Comma-separated values file.
        /// </summary>
        Csv
    }
}