using System;

namespace FLOBUK.StoreSimulator
{
    /// <summary>
    /// Adapter interface for systems that can grant Entrepreneur Tree progress points.
    /// Example: future AchievementSystem can implement this interface.
    /// </summary>
    public interface IProgressPointSource
    {
        /// <summary>
        /// amount, reason
        /// </summary>
        event Action<int, string> onProgressPointsGranted;
    }
}
