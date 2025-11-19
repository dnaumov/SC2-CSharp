using System;
using System.Numerics;
using System.Runtime.Serialization;

namespace SC2_Connector.ReplaySystem
{
    /// <summary>
    /// Represents a game event that occurred during a replay
    /// </summary>
    [DataContract]
    public class ReplayEvent
    {
        /// <summary>
        /// The game frame (timestamp) when this event occurred
        /// </summary>
        [DataMember]
        public ulong Frame { get; set; }

        /// <summary>
        /// Type of event (e.g., "UnitCreated", "BuildingStarted", "BuildingCompleted")
        /// </summary>
        [DataMember]
        public string EventType { get; set; }

        /// <summary>
        /// The unit type ID involved in this event
        /// </summary>
        [DataMember]
        public uint UnitType { get; set; }

        /// <summary>
        /// The position where the event occurred (X coordinate)
        /// </summary>
        [DataMember]
        public float PositionX { get; set; }

        /// <summary>
        /// The position where the event occurred (Y coordinate)
        /// </summary>
        [DataMember]
        public float PositionY { get; set; }

        /// <summary>
        /// The position where the event occurred (Z coordinate)
        /// </summary>
        [DataMember]
        public float PositionZ { get; set; }

        /// <summary>
        /// The player ID who performed this action
        /// </summary>
        [DataMember]
        public int PlayerId { get; set; }

        /// <summary>
        /// Additional notes or context about the event
        /// </summary>
        [DataMember]
        public string Notes { get; set; }

        public ReplayEvent()
        {
        }

        public ReplayEvent(ulong frame, string eventType, uint unitType, Vector3 position, int playerId, string notes = "")
        {
            Frame = frame;
            EventType = eventType;
            UnitType = unitType;
            PositionX = position.X;
            PositionY = position.Y;
            PositionZ = position.Z;
            PlayerId = playerId;
            Notes = notes ?? "";
        }

        /// <summary>
        /// Gets the position as a Vector3
        /// </summary>
        public Vector3 GetPosition()
        {
            return new Vector3(PositionX, PositionY, PositionZ);
        }

        public override string ToString()
        {
            return $"[Frame {Frame}] {EventType}: {UnitType} at ({PositionX:F1}, {PositionY:F1})";
        }
    }
}
