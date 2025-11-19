# Replay Event Logging and Playback System

This system allows you to analyze StarCraft 2 replays, log important events (like building constructions and unit creations), and then have your bot execute those same actions during gameplay.

## Features

1. **Replay Analysis**: Load and analyze SC2 replay files
2. **Event Logging**: Automatically detect and log important events:
   - Building construction starts
   - Building completions
   - Unit creations
3. **Event Playback**: Execute logged events during bot gameplay
4. **JSON Storage**: Events are stored in human-readable JSON format

## Usage

### Step 1: Analyze a Replay and Log Events

Run the bot in replay mode to analyze a replay file and generate an event log:

```bash
BotRunner.exe --replay <replay_path> [observed_player_id] [output_log_path]
```

**Parameters:**
- `<replay_path>`: Path to the .SC2Replay file (required)
- `[observed_player_id]`: ID of the player to observe (default: 1)
- `[output_log_path]`: Path where the event log will be saved (default: "replay_events.json")

**Example:**
```bash
BotRunner.exe --replay "C:\Replays\mygame.SC2Replay" 1 "my_build_order.json"
```

This will:
1. Start StarCraft 2
2. Load the replay
3. Watch player 1's actions
4. Log all building constructions and unit creations to `my_build_order.json`

### Step 2: Play with Event Playback

Run the bot with event playback enabled:

```bash
BotRunner.exe --playback <event_log_path>
```

**Parameters:**
- `<event_log_path>`: Path to the event log JSON file created in Step 1

**Example:**
```bash
BotRunner.exe --playback "my_build_order.json"
```

This will:
1. Start a normal game against the computer
2. Load the event log
3. Execute the logged events at the appropriate times
4. Your bot will attempt to replicate the build order from the replay

## Event Log Format

The event log is stored as JSON and contains entries like:

```json
[
  {
    "Frame": 168,
    "EventType": "BuildingStarted",
    "UnitType": 86,
    "PositionX": 125.5,
    "PositionY": 132.0,
    "PositionZ": 11.9,
    "PlayerId": 1,
    "Notes": ""
  },
  {
    "Frame": 336,
    "EventType": "UnitCreated",
    "UnitType": 104,
    "PositionX": 0.0,
    "PositionY": 0.0,
    "PositionZ": 0.0,
    "PlayerId": 1,
    "Notes": ""
  }
]
```

## Event Types

- **BuildingStarted**: A building construction began
- **BuildingCompleted**: A building finished construction
- **UnitCreated**: A unit was created/trained

## Integration with Your Bot

The event playback system integrates automatically with the bot through the `Controller.EventPlayer`. During each game frame, if an `EventPlayer` is enabled, it will:

1. Check if any events should execute at the current frame
2. Attempt to execute those events (build buildings, train units, etc.)
3. Log the execution attempt

## Customization

You can customize the event detection and playback by modifying:

- `EventLogger.cs`: Add detection for new event types
- `EventPlayer.cs`: Modify how events are executed
- `ReplayEvent.cs`: Add new fields to track additional information

## Notes

- The bot will attempt to execute events even if resources are not available
- Events are executed in chronological order based on frame number
- The system logs all attempts, successful or not, to help with debugging
- Only events from the observed player are logged during replay analysis

## Troubleshooting

**Problem**: Replay won't load
- Ensure the replay path is correct and the file exists
- Make sure StarCraft 2 is installed and configured correctly

**Problem**: Events aren't being logged
- Check that the observed player ID is correct (usually 1 or 2)
- Verify that the replay contains actions from that player

**Problem**: Bot can't execute events
- The bot needs appropriate buildings and resources to execute events
- Events are attempted but may fail if requirements aren't met
- Check the game logs for detailed execution information
