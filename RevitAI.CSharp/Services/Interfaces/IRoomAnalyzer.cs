using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitAI.Services.Interfaces
{
    /// <summary>
    /// Interface for analyzing rooms and their boundaries in a Revit document.
    /// This abstraction enables unit testing of business logic without requiring
    /// a running Revit instance (Layer 1 testing in SIL architecture).
    /// </summary>
    public interface IRoomAnalyzer
    {
        /// <summary>
        /// Retrieves all rooms from the given Revit document.
        /// </summary>
        /// <param name="doc">The Revit document to query.</param>
        /// <returns>Collection of all rooms in the document.</returns>
        IEnumerable<Room> GetAllRooms(Document doc);

        /// <summary>
        /// Gets all walls that bound the specified room.
        /// </summary>
        /// <param name="room">The room to analyze.</param>
        /// <returns>Collection of walls forming the room boundary.</returns>
        IEnumerable<Wall> GetBoundingWalls(Room room);

        /// <summary>
        /// Gets the geometric segments of a wall that can be dimensioned.
        /// Filters out curved segments and returns only linear segments suitable
        /// for standard dimension chains.
        /// </summary>
        /// <param name="wall">The wall to analyze.</param>
        /// <returns>Collection of curves representing dimensionable segments.</returns>
        IEnumerable<Curve> GetDimensionableSegments(Wall wall);

        /// <summary>
        /// Gets rooms filtered by level name.
        /// </summary>
        /// <param name="doc">The Revit document to query.</param>
        /// <param name="levelName">Name of the level to filter by.</param>
        /// <returns>Collection of rooms on the specified level.</returns>
        IEnumerable<Room> GetRoomsByLevel(Document doc, string levelName);

        /// <summary>
        /// Gets the area of a room in square feet.
        /// </summary>
        /// <param name="room">The room to measure.</param>
        /// <returns>Area in square feet, or 0 if room has no area.</returns>
        double GetRoomArea(Room room);
    }
}
