/*  This file is part of the "ShopMaster" project.
 *  Defines configuration data for individual map zones in the supermarket layout. */

using UnityEngine;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Data asset defining the dimensions and properties of a single map zone.
    /// Used by MapLayoutSystem to validate bounds, manage expansions, and organize the supermarket layout.
    /// </summary>
    [CreateAssetMenu(fileName = "MapZoneConfig", menuName = "ScriptableObjects/MapZoneConfig")]
    public class MapZoneConfig : ScriptableObject
    {
        /// <summary>
        /// The functional zone this config represents.
        /// </summary>
        public MapZone zone;

        /// <summary>
        /// Display name shown in the UI and debug views.
        /// </summary>
        public string displayName;

        /// <summary>
        /// Target area in square meters for this zone.
        /// </summary>
        public float areaSqMeters;

        /// <summary>
        /// Zone dimensions in Unity units (width x depth).
        /// 1 Unity unit = 1 meter.
        /// </summary>
        public Vector2 dimensions;

        /// <summary>
        /// Local position offset of this zone relative to the store origin.
        /// </summary>
        public Vector3 positionOffset;

        /// <summary>
        /// Whether this zone can be expanded with modular additions.
        /// </summary>
        public bool isExpandable;

        /// <summary>
        /// Area added per expansion module in square meters.
        /// Only used when isExpandable is true.
        /// </summary>
        public float expansionModuleSqMeters;

        /// <summary>
        /// Maximum number of expansion modules allowed for this zone.
        /// </summary>
        public int maxExpansionModules;

        /// <summary>
        /// Direction the zone expands in (positive X or positive Z).
        /// </summary>
        public Vector3 expansionDirection = Vector3.right;
    }
}
