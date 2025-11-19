using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SC2_Connector.ReplaySystem
{
    /// <summary>
    /// Logs events that occur during a replay for later playback
    /// </summary>
    public class EventLogger
    {
        private List<ReplayEvent> events = new List<ReplayEvent>();
        private Dictionary<ulong, uint> unitTypeCache = new Dictionary<ulong, uint>();
        private Dictionary<ulong, bool> trackedUnits = new Dictionary<ulong, bool>();
        
        /// <summary>
        /// Gets all logged events
        /// </summary>
        public IReadOnlyList<ReplayEvent> Events => events.AsReadOnly();

        /// <summary>
        /// Logs a unit/building creation event
        /// </summary>
        public void LogUnitCreated(ulong frame, uint unitType, Vector3 position, int playerId, bool isBuilding)
        {
            var eventType = isBuilding ? "BuildingStarted" : "UnitCreated";
            var replayEvent = new ReplayEvent(frame, eventType, unitType, position, playerId);
            events.Add(replayEvent);
            Logger.Info($"Logged: {replayEvent}");
        }

        /// <summary>
        /// Logs a building completion event
        /// </summary>
        public void LogBuildingCompleted(ulong frame, uint unitType, Vector3 position, int playerId)
        {
            var replayEvent = new ReplayEvent(frame, "BuildingCompleted", unitType, position, playerId);
            events.Add(replayEvent);
            Logger.Info($"Logged: {replayEvent}");
        }

        /// <summary>
        /// Processes the current frame to detect new events
        /// </summary>
        public void ProcessFrame(ulong frame, Dictionary<ulong, Unit> units, int observedPlayerId)
        {
            foreach (var kvp in units)
            {
                var tag = kvp.Key;
                var unit = kvp.Value;

                // Only track units for the observed player
                if (unit.Alliance != SC2APIProtocol.Alliance.Self)
                    continue;

                // Check if this is a new unit we haven't seen before
                if (!trackedUnits.ContainsKey(tag))
                {
                    trackedUnits[tag] = true;
                    unitTypeCache[tag] = unit.UnitType;

                    // Determine if this is a building
                    bool isBuilding = IsBuilding(unit.UnitType);

                    // Log the creation
                    LogUnitCreated(frame, unit.UnitType, unit.Position, observedPlayerId, isBuilding);
                }
                else if (IsBuilding(unit.UnitType))
                {
                    // Check if building just completed
                    if (unit.BuildProgress >= 1.0f && unitTypeCache.ContainsKey(tag))
                    {
                        // Check if we haven't logged completion yet
                        bool alreadyLogged = events.Any(e => 
                            e.EventType == "BuildingCompleted" && 
                            e.UnitType == unit.UnitType &&
                            Math.Abs(e.PositionX - unit.Position.X) < 0.1f &&
                            Math.Abs(e.PositionY - unit.Position.Y) < 0.1f);

                        if (!alreadyLogged)
                        {
                            LogBuildingCompleted(frame, unit.UnitType, unit.Position, observedPlayerId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a unit type is a building/structure
        /// </summary>
        private bool IsBuilding(uint unitType)
        {
            // Check if the unit type is in the building categories
            // Using common building type ranges for all races
            return Units.Buildings.Contains(unitType);
        }

        /// <summary>
        /// Saves the logged events to a JSON file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(List<ReplayEvent>));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.WriteObject(stream, events);
                }
                Logger.Info($"Saved {events.Count} events to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save events to {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads events from a JSON file
        /// </summary>
        public static List<ReplayEvent> LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Error($"Event log file not found: {filePath}");
                    return new List<ReplayEvent>();
                }

                var serializer = new DataContractJsonSerializer(typeof(List<ReplayEvent>));
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var loadedEvents = (List<ReplayEvent>)serializer.ReadObject(stream);
                    Logger.Info($"Loaded {loadedEvents.Count} events from {filePath}");
                    return loadedEvents ?? new List<ReplayEvent>();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load events from {filePath}: {ex.Message}");
                return new List<ReplayEvent>();
            }
        }

        /// <summary>
        /// Clears all logged events
        /// </summary>
        public void Clear()
        {
            events.Clear();
            trackedUnits.Clear();
            unitTypeCache.Clear();
        }

        /// <summary>
        /// Gets a summary of logged events
        /// </summary>
        public string GetSummary()
        {
            var summary = $"Total Events: {events.Count}\n";
            var eventsByType = events.GroupBy(e => e.EventType);
            foreach (var group in eventsByType)
            {
                summary += $"  {group.Key}: {group.Count()}\n";
            }
            return summary;
        }
    }
}
