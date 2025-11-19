using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using SC2APIProtocol;
using SC2_Connector.ReplaySystem;
using Action = SC2APIProtocol.Action;

namespace SC2_Connector
{
	public static class Controller {
		
        #region Implementation
		//editable
		private static readonly int frameDelay = 0; //too fast? increase this to e.g. 20

		//don't edit
		private static readonly List<Action> actions = new List<Action>();
        private static readonly Random random = new Random();
        private const double FRAMES_PER_SECOND = 22.4;

        internal static ResponseGameInfo GameInfo;
        internal static ResponseData GameData;
        internal static ResponseObservation Observation;

        internal static void OpenFrame()
        {
            if (GameInfo == null || GameData == null || Observation == null)
            {
                if (GameInfo == null)
                    Logger.Info("GameInfo is null! The application will terminate.");
                else if (GameData == null)
                    Logger.Info("GameData is null! The application will terminate.");
                else
                    Logger.Info("ResponseObservation is null! The application will terminate.");
                Pause();
                Environment.Exit(0);
            }

            actions.Clear();
            PopulateUnits();

            foreach (var chat in Observation.Chat)
                ChatLog.Add(chat.Message);

            Frame = Observation.Observation.GameLoop;
            CurrentSupply = Observation.Observation.PlayerCommon.FoodUsed;
            MaxSupply = Observation.Observation.PlayerCommon.FoodCap;
            Minerals = Observation.Observation.PlayerCommon.Minerals;
            Vespene = Observation.Observation.PlayerCommon.Vespene;

            //initialization
            if (Frame == 0)
            {
                var resourceCenters = GetUnits(Units.ResourceCenters);
                if (resourceCenters.Count > 0)
                {
                    var rcPosition = resourceCenters[0].Position;

                    foreach (var startLocation in GameInfo.StartRaw.StartLocations)
                    {
                        var location = new Vector3(startLocation.X, startLocation.Y, 0);
                        var distance = Vector3.Distance(location, rcPosition);
                        if (distance > 30)
                            EnemyLocations.Add(location);
                        else StartLocation = location;
                    }
                }
            }

            // Process event player if enabled
            if (EventPlayer != null && EventPlayer.IsEnabled)
            {
                EventPlayer.ProcessFrame(Frame);
            }

            if (frameDelay > 0)
                Thread.Sleep(frameDelay);
        }

		public static double Distance(Vector3 position, Unit unit)
		{
            return Math.Sqrt(Math.Pow(position.X- unit.Position.X,2)+Math.Pow(position.Y - unit.Position.Y, 2) );
		}

		private static void PopulateUnits()
        {
            ObservableUnits = new Dictionary<ulong, Unit>();
            foreach (var unit in Observation.Observation.RawData.Units)
                ObservableUnits.Add(unit.Tag, new Unit(unit, Controller.GameData.Units[(int)unit.UnitType]));
        }

        internal static List<Action> CloseFrame()
        {
            return actions;
        }

        private static Action CreateRawUnitCommand(int ability)
        {
            var action = new Action();
            action.ActionRaw = new ActionRaw();
            action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
            action.ActionRaw.UnitCommand.AbilityId = ability;
            return action;
        }

        private static int GetAbilityID(uint unit)
        {
            return (int)Controller.GameData.Units[(int)unit].AbilityId;
        }

        internal static Point2D VectorToPoint(Vector3 vector)
        {
            var point= new Point2D();
            point.X = vector.X;
            point.Y = vector.Y;
            return point;
        }

        private static void AddUnitTagsToAction(List<Unit> units, Action action)
        {
            foreach (var unit in units)
                action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
        }


        private static void AddAction(Action action)
        {
            actions.Add(action);
        }

        #region Gameplay Specific Details
        private static int GetEggAbility(uint unitType)
        {
            switch (unitType)
            {
                //T1
                case Units.DRONE: return Abilities.LARVATRAIN_DRONE;
                case Units.OVERLORD: return Abilities.LARVATRAIN_OVERLORD;
                case Units.ZERGLING: return Abilities.LARVATRAIN_ZERGLING;
                case Units.ROACH: return Abilities.LARVATRAIN_ROACH;

                //T2
                case Units.MUTALISK: return Abilities.LARVATRAIN_MUTALISK;
                case Units.HYDRALISK: return Abilities.LARVATRAIN_HYDRALISK;
                case Units.CORRUPTOR: return Abilities.LARVATRAIN_ROACH;
                case Units.INFESTOR: return Abilities.LARVATRAIN_MUTALISK;
                case Units.SWARMHOST: return Abilities.LARVATRAIN_SWARMHOST;

                //T3
                case Units.VIPER: return Abilities.LARVATRAIN_VIPER;
                case Units.ULTRALISK: return Abilities.LARVATRAIN_ULTRALISK;
                default: throw new NotImplementedException();
            }
        }
        private static bool IsTechAvailable(uint unitType)
		{
            return (IsNoTechRequired(unitType) || IsTechPresent(unitType)) && IsProducerPresent(unitType);
        }
        private static bool IsProducerPresent(uint unitType)
        {
            return GetUnits(Units.ProducingStructure[unitType], onlyCompleted: true).Count > 0;
        }

        private static bool IsTechPresent(uint unitType)
		{
			return GetUnits(Units.TechTree[unitType], onlyCompleted: true).Count > 0;
		}

		private static bool IsNoTechRequired(uint unitType)
		{
			return !Units.TechTree.ContainsKey(unitType);
		}

		private static List<Vector3> GetPointsInRadius(Vector3 value, int maxRadius)
        {
            List<Vector3> result = new List<Vector3>();
            for (int i = 0; i < maxRadius; i++)
            {
                for (int j = 0; j < maxRadius; j++)
                {
                    result.Add(new Vector3(value.X - (maxRadius / 2) + i, value.Y - (maxRadius / 2) + j, value.Z));
                }
            }
            return result;
        }
        #endregion
        #endregion

        #region Public 
        #region State
        public static GameConnection Connection;
        public static EventPlayer EventPlayer;

        public static ulong Frame;
        public static uint CurrentSupply;
        public static uint MaxSupply;
        public static uint Minerals;
        public static uint Vespene;

        public static readonly List<Vector3> EnemyLocations = new List<Vector3>();
        public static Vector3 StartLocation;
        public static readonly List<string> ChatLog = new List<string>();

        public static Dictionary<ulong, Unit> ObservableUnits;
        #endregion

        #region Not Gameplay Actions
        public static void Pause()
        {
            Console.WriteLine("Press any key to continue...");
            while (Console.ReadKey().Key != ConsoleKey.Enter)
            {
                //do nothing
            }
        }

        public static ulong SecsToFrames(int seconds)
        {
            return (ulong)(FRAMES_PER_SECOND * seconds);
        }

        public static void Chat(string message, bool team = false)
        {
            var actionChat = new ActionChat();
            actionChat.Channel = team ? ActionChat.Types.Channel.Team : ActionChat.Types.Channel.Broadcast;
            actionChat.Message = message;

            var action = new Action();
            action.ActionChat = actionChat;
            AddAction(action);
        }

        public static string GetUnitName(uint unitType)
        {
            return GameData.Units[(int)unitType].Name;
        }

        public static void FocusCamera(Vector3 Position)
        {
            var action = new Action();
            action.ActionRaw = new ActionRaw();
            action.ActionRaw.CameraMove = new ActionRawCameraMove();
            action.ActionRaw.CameraMove.CenterWorldSpace = new Point();
            action.ActionRaw.CameraMove.CenterWorldSpace.X = Position.X;
            action.ActionRaw.CameraMove.CenterWorldSpace.Y = Position.Y;
            action.ActionRaw.CameraMove.CenterWorldSpace.Z = Position.Z;
            Controller.AddAction(action);
        }

        #endregion

        #region Gameplay Actions
        #region Unit Commands
        public static bool Cancel(Unit unitToCancel)
        {
            var action = Controller.CreateRawUnitCommand(Abilities.CANCEL);
            action.ActionRaw.UnitCommand.UnitTags.Add(unitToCancel.Tag);
            Controller.AddAction(action);
            return true;
        }

        public static void Move(List<Unit> units, Vector3 target)
        {
            var action = Controller.CreateRawUnitCommand(Abilities.MOVE);
            action.ActionRaw.UnitCommand.TargetWorldSpacePos = VectorToPoint(target);
            AddUnitTagsToAction(units, action);
            Controller.AddAction(action);
        }

        public static void Smart(Unit unit, Unit target)
        {
            var action = Controller.CreateRawUnitCommand(Abilities.SMART);
            action.ActionRaw.UnitCommand.TargetUnitTag = target.Tag;
            action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
            Controller.AddAction(action);
        }

        public static void Smart(List<Unit> units, Unit target)
        {
            var action = Controller.CreateRawUnitCommand(Abilities.SMART);
            action.ActionRaw.UnitCommand.TargetUnitTag = target.Tag;
            AddUnitTagsToAction(units, action);
            Controller.AddAction(action);
        }

        public static void Train(Unit producingStructure, uint unitType, bool queue = false)
        {
            if (!queue && producingStructure.Orders.Count > 0)
                return;

            var abilityID = GetAbilityID(unitType);
            var action = Controller.CreateRawUnitCommand(abilityID);
            action.ActionRaw.UnitCommand.UnitTags.Add(producingStructure.Tag);
            Controller.AddAction(action);

            var targetName = Controller.GetUnitName(unitType);
            Logger.Info("Started training: {0}", targetName);
        }

        public static void Attack(List<Unit> units, Vector3 target)
		{
			var action = CreateRawUnitCommand(Abilities.ATTACK);

			action.ActionRaw.UnitCommand.TargetWorldSpacePos = VectorToPoint(target);
			AddUnitTagsToAction(units, action);
			AddAction(action);
		}

		public static void Inject(List<Unit> units, Unit target)
        {
            var action = CreateRawUnitCommand(Abilities.EFFECT_INJECTLARVA);
            action.ActionRaw.UnitCommand.TargetUnitTag = target.Tag;
            AddUnitTagsToAction(units, action);
            AddAction(action);
        }

        private static Vector3? FindAvailableConstructionSpot(uint buildingToConstruct)
        {
            Vector3 constructionSpot;
            Vector3 startingSpot;
            var resourceCenters = GetUnits(Units.ResourceCenters);
            if (resourceCenters.Count > 0)
                startingSpot = resourceCenters[0].Position;
            else
            {
                Logger.Error("Unable to construct: {0}. No resource center was found.", GetUnitName(buildingToConstruct));
                return null;
            }


            const int radius = 12;

            //trying to find a valid construction spot
            var mineralFields = GetUnits(Units.MineralFields, onlyVisible: true, alliance: Alliance.Neutral);
            int retryCount = 100;
            while (true)
            {
                retryCount--;
                if (retryCount < 1)
                {
                    Logger.Error("Unable to find a place for {0} building here", GetUnitName(buildingToConstruct));
                    return null;
                }
                constructionSpot = new Vector3(startingSpot.X + random.Next(-radius, radius + 1), startingSpot.Y + random.Next(-radius, radius + 1), 0);

                //avoid building in the mineral line
                if (IsInRange(constructionSpot, mineralFields, 5)) continue;

                //check if the building fits
                if (!CanPlace(buildingToConstruct, constructionSpot)) continue;

                //ok, we found a spot
                break;
            }
            return constructionSpot;
        }

        private static Vector3? AdjustConstructionSpot(Vector3 constructionSpot, uint buildingToConstruct)
        {
            if (!CanPlace(buildingToConstruct, constructionSpot))
            {
                int maxRadius = 11;

                List<Vector3> points = GetPointsInRadius(constructionSpot, maxRadius);
                bool foundSpot = false;
                foreach (var point in points)
                {
                    if (CanPlace(buildingToConstruct, point))
                    {
                        constructionSpot = point;
                        foundSpot = true;
                        break;
                    }
                }
                if (!foundSpot)
                {
                    Logger.Error("Unable to construct a {0} building here", GetUnitName(buildingToConstruct));
                    return null;
                }
            }
            return constructionSpot;
        }
        public static bool ConstructGas(Unit worker, uint buildingToConstruct, Unit geyser = null)
        {
            if (worker == null)
            {
                Logger.Error("Unable to find worker to construct: {0}", GetUnitName(buildingToConstruct));
                return false;
            }

            if (geyser == null)
            {
                geyser = GetGeysers().FirstOrDefault();
            }

            var abilityID = GetAbilityID(buildingToConstruct);
            var constructAction = CreateRawUnitCommand(abilityID);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
            constructAction.ActionRaw.UnitCommand.TargetUnitTag = geyser.Tag;
            AddAction(constructAction);

            Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(buildingToConstruct), geyser.Position.X, geyser.Position.Y);
            return true;
        }
        public static bool Construct(Unit worker, uint buildingToConstruct, Vector3? constructionSpot = null, bool adjustConstructionSpot = false)
        {
            if (worker == null)
            {
                Logger.Error("Unable to find worker to construct: {0}", GetUnitName(buildingToConstruct));
                return false;
            }

            if (constructionSpot == null)
            {
                constructionSpot = FindAvailableConstructionSpot(buildingToConstruct);
            }
            else if (adjustConstructionSpot)
            {
                constructionSpot = AdjustConstructionSpot(constructionSpot.Value, buildingToConstruct);
            }
            if (constructionSpot == null)
            {
                return false;
            }

            var abilityID = GetAbilityID(buildingToConstruct);
            var constructAction = CreateRawUnitCommand(abilityID);
            constructAction.ActionRaw.UnitCommand.UnitTags.Add(worker.Tag);
            constructAction.ActionRaw.UnitCommand.TargetWorldSpacePos = VectorToPoint(constructionSpot.Value);
            AddAction(constructAction);

            Logger.Info("Constructing: {0} @ {1} / {2}", GetUnitName(buildingToConstruct), constructionSpot.Value.X, constructionSpot.Value.Y);
            return true;
        }
        #endregion

		#region Get Unit Methods
		public static int GetTotalCount(uint unitType)
        {
            var pendingCount = GetPendingCount(unitType, inConstruction: false);
            var constructionCount = GetUnits(unitType).Count;
            return pendingCount + constructionCount;
        }

        public static int GetPendingCount(uint unitType, bool inConstruction = true)
        {
            var workers = GetUnits(Units.Workers);
            var abilityID = GetAbilityID(unitType);

            var counter = 0;

            //count workers that have been sent to build this structure
            foreach (var worker in workers)
            {
                if (worker.Order.AbilityId == abilityID)
                    counter += 1;
            }

            //count buildings that are already in construction
            if (inConstruction)
            {
                foreach (var unit in GetUnits(Units.EGG))
                    if (unit.Order.AbilityId == GetEggAbility(unitType))
                        counter += 1;
            }

            //count eggs that are already in construction
            if (inConstruction)
            {
                foreach (var unit in GetUnits(unitType))
                    if (unit.BuildProgress < 1)
                        counter += 1;
            }

            return counter;
        }

        public static Unit GetUnitByTag(ulong unitTag)
        {
            if (ObservableUnits.ContainsKey(unitTag))
                return ObservableUnits[unitTag];
            return null;
        }

        public static List<Unit> GetUnits(HashSet<uint> unitTypes, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            var units = new List<Unit>();
            foreach (var unit in ObservableUnits.Values)
                if (unitTypes.Contains(unit.UnitType) && unit.Alliance == alliance)
                {
                    if (onlyCompleted && unit.BuildProgress < 1)
                        continue;

                    if (onlyVisible && (!unit.IsVisible))
                        continue;

                    units.Add(unit);
                }
            return units;
        }
        public static Unit GetUnit(uint unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            foreach (var unit in ObservableUnits.Values)
                if (unitType == unit.UnitType && unit.Alliance == alliance)
                {
                    if (onlyCompleted && unit.BuildProgress < 1)
                        continue;

                    if (onlyVisible && (!unit.IsVisible))
                        continue;
                    return unit;
                }
            return null;
        }

        public static List<Unit> GetUnits(uint unitType, Alliance alliance = Alliance.Self, bool onlyCompleted = false, bool onlyVisible = false)
        {
            return GetUnits(new HashSet<uint>() { unitType }, alliance, onlyCompleted, onlyVisible);
        }
        // </TODO>
        public static Unit GetClosestLarva(Vector3? targetPosition = null)
        {
            return Controller.GetUnits(Units.LARVA).FirstOrDefault();
        }

        public static List<Unit> GetLarvae()
        {
            return Controller.GetUnits(Units.LARVA, onlyCompleted: true);
        }

        public static List<Unit> GetHatcheries(bool onlyCompleted = false)
        {
            return Controller.GetUnits(Units.ZergResourceCenters, onlyCompleted: onlyCompleted);
        }
        public static List<Unit> GetGeysers()
        {
            return Controller.GetUnits(Units.GasGeysers, Alliance.Neutral);
        }

        public static Unit GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            //squared distance is faster to calculate
            var maxDistanceSqr = maxDistance * maxDistance;
            foreach (var unit in units)
            {
                if (Vector3.DistanceSquared(targetPosition, unit.Position) <= maxDistanceSqr)
                    return unit;
            }
            return null;
        }
        public static Unit GetFirstInRange(Vector3 targetPosition, Unit unit, float maxDistance)
        {
            //squared distance is faster to calculate
            var maxDistanceSqr = maxDistance * maxDistance;

                if (Vector3.DistanceSquared(targetPosition, unit.Position) <= maxDistanceSqr)
                    return unit;
            return null;
        }
        #endregion

        #region Building Units/Structures
        /// <summary>
        /// Checks if there are available larvae and orders a larva to train the unit.
        /// Also checks <see cref="CanConstruct(uint)"/>
        /// </summary>
        /// <param name="unitType"></param>
        /// <param name="withLarva"></param>
        /// <returns></returns>
        public static bool BuildUnit(uint unitType, Unit withLarva = null)
        {
            if (!Controller.CanConstruct(unitType))
                return false;
            if (withLarva == null)
            {
                var larvae = Controller.GetUnits(Units.LARVA);
                foreach (var larva in larvae)
                {
                    if (larva.Orders.Count == 0)
                    {
                        withLarva = larva;
                    }
                }
            }
            if (withLarva != null)
            {
                Train(withLarva, unitType);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool BuildUnit(uint unitType)
        {
            if (Controller.CanConstruct(unitType))
            {
                if (unitType == Units.QUEEN)
                {
                    var rcs = Controller.GetUnits(
                        new HashSet<uint>() { Units.HATCHERY, Units.LAIR, Units.HIVE },
                        onlyCompleted: true);
                    foreach (var rc in rcs)
                    {
                        if (Controller.CanConstruct(unitType) && rc.Orders.Count == 0)
                        {
                            Train(rc, unitType);
                            return true;
                        }
                    }
                }
                else
                {
                    var larvae = Controller.GetUnits(Units.LARVA);
                    foreach (var larva in larvae)
                    {
                        if (Controller.CanConstruct(unitType) && larva.Orders.Count == 0)
                        {
                            Train(larva, unitType);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool CanAfford(uint unitType)
        {
            var unitData = GameData.Units[(int)unitType];
            return (Minerals >= (unitType == Units.ZERGLING ? 2 : 1) * unitData.MineralCost) && (Vespene >= unitData.VespeneCost);
        }

        public static bool CanConstruct(uint unitType)
        {
            //is it a structure?
            if (Units.Structures.Contains(unitType))
            {
                //we need worker for every structure
                if (GetUnits(Units.Workers).Count == 0) return false;

                //we need an RC for any structure
                var resourceCenters = GetUnits(Units.ResourceCenters, onlyCompleted: true);
                if (resourceCenters.Count == 0) return false;


                return CanAfford(unitType);
            }

            //it's an actual unit
            else
            {
                //do we have enough supply?
                var requiredSupply = Controller.GameData.Units[(int)unitType].FoodRequired;
                if (requiredSupply > (MaxSupply - CurrentSupply))
                    return false;


                if (!IsTechAvailable(unitType))
                    return false;
            }
            return CanAfford(unitType);
        }

        public static bool CanPlace(uint unitType, Vector3 targetPos)
            // QueryBuildingPlacement(unitType, targetPos); Verification on our side is sufficient
        {
            bool[,] placement;
            placement = MapHelper.GetPlacementGrid();
            //Check for creep
            if (Units.RequireCreep.Contains(unitType))
            {
                placement = MapHelper.And(MapHelper.GetCreepGrid(), placement);
            }
            if (!CheckPoints(targetPos, BuildingType.GetBuildingSize(unitType), placement))
                return false;

            return true;
        }

		private static bool QueryBuildingPlacement(uint unitType, Vector3 targetPos)
		{

			//Note: this is a blocking call! Use it sparingly, or you will slow down your execution significantly!
			bool canPlace = false;
			var abilityID = GetAbilityID(unitType);
			RequestQueryBuildingPlacement queryBuildingPlacement = new RequestQueryBuildingPlacement();
			queryBuildingPlacement.AbilityId = abilityID;
			queryBuildingPlacement.TargetPos = new Point2D();
			queryBuildingPlacement.TargetPos.X = targetPos.X;
			queryBuildingPlacement.TargetPos.Y = targetPos.Y;

			Request requestQuery = new Request();
			requestQuery.Query = new RequestQuery();
			requestQuery.Query.Placements.Add(queryBuildingPlacement);

			var result = Connection.SendQuery(requestQuery.Query);
			if (result.Result.Placements.Count > 0)
				canPlace = (result.Result.Placements[0].Result == ActionResult.Success);
			return canPlace;
		}

		private static bool CheckPoints(Vector3 targetPos, Point2D point, bool[,] map)
		{
            for (int i = 0; i < point.X; i++)
            {
                for (int j = 0; j < point.Y; j++)
                {
                    if (!map[(int)(targetPos.X + i - (point.X / 2)), (int)(targetPos.Y + j - (point.Y / 2))])
                        return false;
                }
            }
            return true;

        }
		#endregion

		public static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance)
        {
            return (GetFirstInRange(targetPosition, units, maxDistance) != null);
        }
        public static bool IsInRange(Vector3 targetPosition, Unit unit, float maxDistance)
        {
            return (GetFirstInRange(targetPosition, unit, maxDistance) != null);
        }

        #endregion
        #endregion
    }
}