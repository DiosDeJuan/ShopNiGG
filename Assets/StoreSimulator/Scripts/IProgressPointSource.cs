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
        /// Raised when the source grants progress points to the Entrepreneur Tree.
        /// </summary>
        /// <param name="amount">Amount of progress points granted.</param>
        /// <param name="reason">Reason/source tag for telemetry or UI messaging.</param>
        event Action<int, string> onProgressPointsGranted;
    }
}
