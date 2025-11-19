using System;
using BeholderBot;
using SC2_Connector;
using SC2_Connector.ReplaySystem;
using SC2APIProtocol;

namespace BotRunner
{
    internal class Program
    {
        // Settings for your bot.
        private static readonly Bot bot = new Beholder();
        //private static readonly Bot bot = new RaxBot.RaxBot();

        // Settings for single player mode.
        //        private static string mapName = "AbyssalReefLE.SC2Map";
        //        private static string mapName = "AbiogenesisLE.SC2Map";
        //        private static string mapName = "FrostLE.SC2Map";
        private static readonly string mapName = "DiscoBloodbathLE.SC2Map";

        private static readonly Race opponentRace = Race.Protoss;
        private static readonly Difficulty opponentDifficulty = Difficulty.Medium;


        private static void Main(string[] args)
        {
            try
            {
                Controller.Connection = new GameConnection();
                Controller.Connection.readSettings();
                
                // Check for replay mode
                if (args.Length >= 2 && args[0] == "--replay")
                {
                    // Replay mode: analyze a replay and log events
                    // Usage: --replay <replay_path> [observed_player_id] [output_log_path]
                    var replayPath = args[1];
                    var observedPlayerId = args.Length >= 3 ? int.Parse(args[2]) : 1;
                    var outputLogPath = args.Length >= 4 ? args[3] : "replay_events.json";
                    
                    Logger.Info("Running in REPLAY mode");
                    Logger.Info($"Replay: {replayPath}");
                    Logger.Info($"Observed Player: {observedPlayerId}");
                    Logger.Info($"Output Log: {outputLogPath}");
                    
                    Controller.Connection.RunReplayAndLogEvents(replayPath, observedPlayerId, outputLogPath).Wait();
                }
                else if (args.Length >= 2 && args[0] == "--playback")
                {
                    // Playback mode: run bot with event playback from a log file
                    // Usage: --playback <event_log_path>
                    var eventLogPath = args[1];
                    
                    Logger.Info("Running in PLAYBACK mode");
                    Logger.Info($"Event Log: {eventLogPath}");
                    
                    // Load the event player
                    Controller.EventPlayer = EventPlayer.LoadFromFile(eventLogPath);
                    Controller.EventPlayer.Enable();
                    
                    Logger.Info($"Loaded {Controller.EventPlayer.TotalEvents} events for playback");
                    
                    // Run the game normally with event playback enabled
                    Controller.Connection.RunSinglePlayer(bot, mapName, bot.GetRace(), opponentRace, opponentDifficulty).Wait();
                }
                else if (args.Length == 0)
                {
                    // Normal single player mode
                    Controller.Connection.RunSinglePlayer(bot, mapName, bot.GetRace(), opponentRace, opponentDifficulty).Wait();
                }
                else
                {
                    // Ladder mode
                    Controller.Connection.RunLadder(bot, bot.GetRace(), args).Wait();
                }
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }

            Logger.Info("Terminated.");
        }
    }
}