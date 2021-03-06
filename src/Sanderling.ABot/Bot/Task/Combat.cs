﻿using BotEngine.Common;
using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.Parse;
using System;
using Sanderling.Interface.MemoryStruct;
using Sanderling.ABot.Parse;
using Bib3;
using WindowsInput.Native;

namespace Sanderling.ABot.Bot.Task
{
	public class ReloadAnomalieso : IBotTask
	{


		public IEnumerable<IBotTask> Component => null;

		public IEnumerable<MotionParam> Effects
		{
			get
			{
				var APPS = VirtualKeyCode.NUMPAD0;
				yield return APPS.KeyboardPress();
				yield return APPS.KeyboardPress();
				var nomscanned = true;



			}

		}
	}
	public class CombatTask : IBotTask
	{
		const int TargetCountMax = 4;

		public Bot bot;
		static public bool ActuallyAnomaly(Interface.MemoryStruct.IListEntry scanResult) =>
			scanResult?.CellValueFromColumnHeader("Distance")?.RegexMatchSuccessIgnoreCase("km") ?? false;


		public bool Completed { private set; get; }


		public IEnumerable<IBotTask> Component
		{
			get
			{

				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;

				var memoryMeasurementAccu = bot?.MemoryMeasurementAccu;


				var memoryMeasurement = memoryMeasurementAtTime?.Value;
				var probeScannerWindow = memoryMeasurement?.WindowProbeScanner?.FirstOrDefault();
				var listOverviewDreadCheck = memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry?.Where(entry => entry?.Name?.RegexMatchSuccess("Dreadnought") ?? true)
					.ToList();




				var scanActuallyAnomaly =
							probeScannerWindow?.ScanResultView?.Entry?.FirstOrDefault(ActuallyAnomaly);

				if (!memoryMeasurement.ManeuverStartPossible())
					yield break;

				bool IsFriendBackgroundColor(BotEngine.Interface.ColorORGB color) =>
				color.OMilli == 500 && color.RMilli == 0 && color.GMilli == 150 && color.BMilli == 600;
				bool IsFleetBackgroundColor(BotEngine.Interface.ColorORGB color) =>
				color.OMilli == 500 && color.RMilli == 600 && color.GMilli == 150 && color.BMilli == 900;
				// our speed
				var speedMilli = bot?.MemoryMeasurementAtTime?.Value?.ShipUi?.SpeedMilli;

				// Object. They should be shown in overview. If you want orbit "collidable entity" replace the word "asteroid" with a word from the column type
				var Pirate = memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry
					?.Where(entry => entry?.Name?.RegexMatchSuccessIgnoreCase("Pirate") ?? false)
					?.OrderBy(entry => entry?.DistanceMax ?? int.MaxValue)
					?.ToArray();

				// Object. They should be shown in overview. If you want orbit "collidable entity" replace the word "asteroid" with a word from the column type
				var Chemical = memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry
					?.Where(entry => entry?.Name?.RegexMatchSuccessIgnoreCase("Chemical") ?? false)
					?.OrderBy(entry => entry?.DistanceMax ?? int.MaxValue)
					?.ToArray();

				var listOverviewEntryFriends =
						memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry
						?.Where(entry => entry?.ListBackgroundColor?.Any(IsFriendBackgroundColor) ?? false)
						?.ToArray();
				var listOverviewEntryFleet =
						memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry
						?.Where(entry => entry?.ListBackgroundColor?.Any(IsFleetBackgroundColor) ?? false)
						?.ToArray();

				var listOverviewEntryToAttack =
					  memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry?.Where(entry => entry?.MainIcon?.Color?.IsRed() ?? false)
					  ?.OrderBy(entry => bot.AttackPriorityIndex(entry))
					  ?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"coreli|centi|alvi|pithi|corpii|gistii")) //Frigate
					  ?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"corelior|centior|alvior|pithior|corpior|gistior")) //Destroyer
					  ?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"corelum|centum|alvum|pithum|corpum|gistum")) //Cruiser
					  ?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"corelatis|centatis|alvatis|pithatis|copatis|gistatis")) //Battlecruiser
					  ?.OrderBy(entry => entry?.Name?.RegexMatchSuccessIgnoreCase(@"core\s|centus|alvus|pith\s|corpus|gist\s")) //Battleship
					  ?.ThenBy(entry => entry?.DistanceMax ?? int.MaxValue)
					  ?.ToArray();
				var scanResult =
				probeScannerWindow?.ScanResultView?.Entry?.FirstOrDefault();
				var nomreloaded = true;

				if (listOverviewDreadCheck.Count() > 0)
				{
					yield return new RetreatTask { Bot = bot };
				}
				else
				{
					// try to detect blue

					if (null != scanActuallyAnomaly)

						if (listOverviewEntryFriends.Length == 0)
						{
							bot?.SetOwnAnomaly(true);
						}
					if (listOverviewEntryFriends.Length > 0)
					{
						bot?.SetOwnAnomaly(false);

						yield return scanActuallyAnomaly.ClickMenuEntryByRegexPattern(bot, "Ignore Result");


					}
				}

				//Looking for Afterburner module
				var moduleAB = memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.LabelText?.Any(
					label => label?.Text?.RegexMatchSuccess("Afterburner", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ?? false) ?? false);

				// if we not warp and our speed < 20 m/s then try orbit 30 km
				if (speedMilli < 20000)
					yield return Pirate.FirstOrDefault().ClickMenuEntryByRegexPattern(bot, "Orbit", "20 km");

				//turn it on
				if (moduleAB != null)
					yield return bot.EnsureIsActive(moduleAB);

				if (speedMilli < 20000)
					yield return Chemical.FirstOrDefault().ClickMenuEntryByRegexPattern(bot, "Orbit", "20 km");





				var targetSelected =
					memoryMeasurement?.Target?.FirstOrDefault(target => target?.IsSelected ?? false);

				var shouldAttackTarget =
					listOverviewEntryToAttack?.Any(entry => entry?.MeActiveTarget ?? false) ?? false;

				var setModuleWeapon =
					memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsWeapon ?? false);

				if (null != targetSelected)

					if (shouldAttackTarget)
						yield return bot.EnsureIsActive(setModuleWeapon);


					else
						yield return targetSelected.ClickMenuEntryByRegexPattern(bot, "unlock");


				var droneListView = memoryMeasurement?.WindowDroneView?.FirstOrDefault()?.ListView;

				var droneGroupWithNameMatchingPattern = new Func<string, DroneViewEntryGroup>(namePattern =>
						droneListView?.Entry?.OfType<DroneViewEntryGroup>()?.FirstOrDefault(group => group?.LabelTextLargest()?.Text?.RegexMatchSuccessIgnoreCase(namePattern) ?? false));

				var droneGroupInBay = droneGroupWithNameMatchingPattern("bay");
				var droneGroupInLocalSpace = droneGroupWithNameMatchingPattern("local space");

				var droneInBayCount = droneGroupInBay?.Caption?.Text?.CountFromDroneGroupCaption();
				var droneInLocalSpaceCount = droneGroupInLocalSpace?.Caption?.Text?.CountFromDroneGroupCaption();

				//	assuming that local space is bottommost group.
				var setDroneInLocalSpace =
					droneListView?.Entry?.OfType<DroneViewEntryItem>()
					?.Where(drone => droneGroupInLocalSpace?.RegionCenter()?.B < drone?.RegionCenter()?.B)
					?.ToArray();

				var droneInLocalSpaceSetStatus =
					setDroneInLocalSpace?.Select(drone => drone?.LabelText?.Select(label => label?.Text?.StatusStringFromDroneEntryText()))?.ConcatNullable()?.WhereNotDefault()?.Distinct()?.ToArray();

				var droneInLocalSpaceIdle =
					droneInLocalSpaceSetStatus?.Any(droneStatus => droneStatus.RegexMatchSuccessIgnoreCase("idle")) ?? false;



				if (shouldAttackTarget)


				{
					if (0 < droneInBayCount && droneInLocalSpaceCount < 5)
						yield return droneGroupInBay.ClickMenuEntryByRegexPattern(bot, @"launch");

					if (droneInLocalSpaceIdle)
						yield return droneGroupInLocalSpace.ClickMenuEntryByRegexPattern(bot, @"engage");
				}

				var overviewEntryLockTarget =
					listOverviewEntryToAttack?.FirstOrDefault(entry => !((entry?.MeTargeted ?? false) || (entry?.MeTargeting ?? false)));

				if (null != overviewEntryLockTarget && !(TargetCountMax <= memoryMeasurement?.Target?.Length))
					yield return overviewEntryLockTarget.ClickMenuEntryByRegexPattern(bot, @"^lock\s*target");

				if (!(0 < listOverviewEntryToAttack?.Length))
					if (0 < droneInLocalSpaceCount)
						yield return droneGroupInLocalSpace.ClickMenuEntryByRegexPattern(bot, @"return.*bay");
					else
						Completed = true;

			}
		}

		public IEnumerable<MotionParam> Effects => null;


	}
}
