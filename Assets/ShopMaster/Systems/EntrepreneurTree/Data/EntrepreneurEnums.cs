// ShopMaster - Entrepreneur Skill Tree System
// Enums used across the entire EntrepreneurTree module.

namespace ShopMaster
{
    /// <summary>
    /// The gameplay category of a tree node.
    /// Controls which subsystem receives the unlock signal via gameplayKey.
    /// </summary>
    public enum EntrepreneurNodeType
    {
        /// <summary>Products that can be stocked and sold.</summary>
        Product,

        /// <summary>Employee slots that can be assigned roles.</summary>
        Employee,

        /// <summary>Security levels (cameras, guards, anti-theft arches).</summary>
        Security,

        /// <summary>Passive gameplay improvements (speed, charisma, etc.).</summary>
        Upgrade
    }

    /// <summary>
    /// Visual and interaction state of a single tree node.
    /// </summary>
    public enum EntrepreneurNodeState
    {
        /// <summary>Prerequisites not met – node is greyed out and cannot be clicked.</summary>
        Locked,

        /// <summary>Prerequisites met but not yet purchased – shown in blue/yellow.</summary>
        Available,

        /// <summary>Node has been purchased – shown in green.</summary>
        Unlocked
    }
}
