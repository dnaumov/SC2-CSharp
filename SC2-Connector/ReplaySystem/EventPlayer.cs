using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SC2_Connector.ReplaySystem
{
    /// <summary>
    /// Plays back logged events during a bot game
    /// </summary>
    public class EventPlayer
    {
        private List<ReplayEvent> events;
        private int currentEventIndex = 0;
        private bool isEnabled = false;

        /// <summary>
        /// Gets whether the event player is enabled
        /// </summary>
        public bool IsEnabled => isEnabled;

        /// <summary>
        /// Gets the total number of events
        /// </summary>
        public int TotalEvents => events?.Count ?? 0;

        /// <summary>
        /// Gets the number of events already executed
        /// </summary>
        public int ExecutedEvents => currentEventIndex;

        /// <summary>
        /// Gets the number of remaining events
        /// </summary>
        public int RemainingEvents => TotalEvents - ExecutedEvents;

        /// <summary>
        /// Initializes the event player with a list of events
        /// </summary>
        public EventPlayer(List<ReplayEvent> events)
        {
            this.events = events ?? new List<ReplayEvent>();
            this.events.Sort((a, b) => a.Frame.CompareTo(b.Frame));
            this.currentEventIndex = 0;
        }

        /// <summary>
        /// Loads events from a file
        /// </summary>
        public static EventPlayer LoadFromFile(string filePath)
        {
            var events = EventLogger.LoadFromFile(filePath);
            return new EventPlayer(events);
        }

        /// <summary>
        /// Enables the event player
        /// </summary>
        public void Enable()
        {
            isEnabled = true;
            Logger.Info($"EventPlayer enabled with {TotalEvents} events");
        }

        /// <summary>
        /// Disables the event player
        /// </summary>
        public void Disable()
        {
            isEnabled = false;
            Logger.Info("EventPlayer disabled");
        }

        /// <summary>
        /// Resets the event player to the beginning
        /// </summary>
        public void Reset()
        {
            currentEventIndex = 0;
            Logger.Info("EventPlayer reset to beginning");
        }

        /// <summary>
        /// Processes the current frame and executes any events that should occur
        /// </summary>
        public void ProcessFrame(ulong currentFrame)
        {
            if (!isEnabled || events == null || events.Count == 0)
                return;

            // Execute all events that should have occurred by now
            while (currentEventIndex < events.Count && events[currentEventIndex].Frame <= currentFrame)
            {
                var replayEvent = events[currentEventIndex];
                ExecuteEvent(replayEvent, currentFrame);
                currentEventIndex++;
            }
        }

        /// <summary>
        /// Executes a single event
        /// </summary>
        private void ExecuteEvent(ReplayEvent replayEvent, ulong currentFrame)
        {
            try
            {
                Logger.Info($"Executing event [{currentEventIndex + 1}/{TotalEvents}]: {replayEvent}");

                var position = replayEvent.GetPosition();

                switch (replayEvent.EventType)
                {
                    case "BuildingStarted":
                        ExecuteBuildingStarted(replayEvent.UnitType, position);
                        break;

                    case "UnitCreated":
                        ExecuteUnitCreated(replayEvent.UnitType);
                        break;

                    case "BuildingCompleted":
                        // Buildings complete automatically, no action needed
                        Logger.Info($"Building {replayEvent.UnitType} should be completed at {position}");
                        break;

                    default:
                        Logger.Info($"Unknown event type: {replayEvent.EventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing event: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a building construction event
        /// </summary>
        private void ExecuteBuildingStarted(uint unitType, Vector3 position)
        {
            try
            {
                // Check if we have the necessary resources and tech
                if (!CanBuildUnit(unitType))
                {
                    Logger.Info($"Cannot build {unitType} yet - missing resources or tech");
                    return;
                }

                // Try to find a worker to build
                var workers = Controller.GetUnits(Units.Workers, onlyCompleted: true);
                if (workers.Count == 0)
                {
                    Logger.Info($"No workers available to build {unitType}");
                    return;
                }

                // Use the closest worker
                var worker = workers.OrderBy(w => Vector3.Distance(w.Position, position)).First();

                // Issue build command
                Controller.Construct(worker, unitType, position);
                Logger.Info($"Commanded worker to build {unitType} at {position}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error building {unitType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a unit creation event
        /// </summary>
        private void ExecuteUnitCreated(uint unitType)
        {
            try
            {
                // Check if we have the necessary resources and tech
                if (!CanBuildUnit(unitType))
                {
                    Logger.Info($"Cannot train {unitType} yet - missing resources or tech");
                    return;
                }

                // Find an appropriate production structure
                var producers = GetProducersForUnit(unitType);
                if (producers.Count == 0)
                {
                    Logger.Info($"No production structure available for {unitType}");
                    return;
                }

                // Use the first available producer
                var producer = producers.First();
                Controller.Train(producer, unitType);
                Logger.Info($"Commanded to train {unitType}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error training {unitType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if we can build/train a unit (has resources and tech)
        /// </summary>
        private bool CanBuildUnit(uint unitType)
        {
            // This is a simplified check - could be enhanced
            // In a real implementation, you'd check minerals, vespene, supply, and tech requirements
            return true;
        }

        /// <summary>
        /// Gets production structures that can create the given unit type
        /// </summary>
        private List<Unit> GetProducersForUnit(uint unitType)
        {
            if (!Units.ProducingStructure.ContainsKey(unitType))
                return new List<Unit>();

            var producerType = Units.ProducingStructure[unitType];
            return Controller.GetUnits(producerType, onlyCompleted: true);
        }

        /// <summary>
        /// Gets a progress summary
        /// </summary>
        public string GetProgressSummary()
        {
            if (events == null || events.Count == 0)
                return "No events loaded";

            return $"Events: {ExecutedEvents}/{TotalEvents} ({RemainingEvents} remaining)";
        }

        /// <summary>
        /// Gets the next few upcoming events
        /// </summary>
        public List<ReplayEvent> GetUpcomingEvents(int count = 5)
        {
            if (events == null || currentEventIndex >= events.Count)
                return new List<ReplayEvent>();

            return events.Skip(currentEventIndex).Take(count).ToList();
        }
    }
}
