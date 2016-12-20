using BotEngine.Common;
using System.Collections.Generic;
using System.Linq;
using Sanderling.Motor;
using Sanderling.Parse;
using Sanderling.Accumulation;
using System;
using Sanderling.Interface.MemoryStruct;
using Sanderling.ABot.Parse;
using Bib3;
using WindowsInput.Native;

namespace Sanderling.ABot.Bot.Task
{
	public class CombatTask : IBotTask
	{
		const int TargetCountMax = 4;

		public Bot bot;

		public bool Completed { private set; get; }

		public IEnumerable<IBotTask> Component
		{
			get
			{
				var memoryMeasurementAtTime = bot?.MemoryMeasurementAtTime;
				var memoryMeasurementAccu = bot?.MemoryMeasurementAccu;

				var memoryMeasurement = memoryMeasurementAtTime?.Value;

				if (!memoryMeasurement.ManeuverStartPossible())
					yield break;

                var listOverviewEntryToAttack =
					memoryMeasurement?.WindowOverview?.FirstOrDefault()?.ListView?.Entry?.Where(entry => entry?.MainIcon?.Color?.IsRed() ?? false)
					?.OrderBy(entry => bot.AttackPriorityIndex(entry))
					?.ThenBy(entry => entry?.DistanceMax ?? int.MaxValue)
					?.ToArray();

				var targetSelected =
					memoryMeasurement?.Target?.FirstOrDefault(target => target?.IsSelected ?? false);

				var shouldAttackTarget =
					listOverviewEntryToAttack?.Any(entry => entry?.MeActiveTarget ?? false) ?? false;

				var setModuleWeapon =
					memoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsWeapon ?? false);
                                
                if (listOverviewEntryToAttack?.Length > 0)
                {
                    var retreatBookmark = bot?.ConfigSerialAndStruct.Value?.RetreatBookmark;
                    var isAligned = memoryMeasurement?.ShipUi?.Indication?.LabelText?.Select(label => label?.Text?.RegexMatchSuccessIgnoreCase("align"));
                    if (isAligned == null)
                        yield return new MenuPathTask
                        {
                            RootUIElement = memoryMeasurement?.InfoPanelCurrentSystem?.ListSurroundingsButton,
                            Bot = bot,
                            ListMenuListPriorityEntryRegexPattern = new[] { new[] { retreatBookmark }, new[] { @"align" } },
                        };
                }

                if (null != targetSelected)
					if (shouldAttackTarget)
						yield return bot.EnsureIsActive(setModuleWeapon);
					else
						yield return targetSelected.ClickMenuEntryByRegexPattern(bot, "unlock");
                
				if (shouldAttackTarget)
				{
                    var isSquadsLaunched = memoryMeasurement?.ShipUi?.SquadronsUI?.SetSquadron?.Any(squad => squad?.SetAbilityIcon != null) ?? false;
                    var isAnySquadNotSelected = memoryMeasurement?.ShipUi?.SquadronsUI?.SetSquadron?.Any(squad => !squad?.Squadron?.IsSelected ?? false) ?? false;
                    if (!isSquadsLaunched || isAnySquadNotSelected)                    
                        yield return new LaunchFighters { MemoryMeasurement = memoryMeasurementAtTime?.Value };
                    /*
                    var setSquadron = memoryMeasurement?.ShipUi?.SquadronsUI?.SetSquadron;
                    var setAbility = setSquadron?.Where(ss => ss?.SetAbilityIcon != null);
                    var rampActive = setAbility?.Any(squad => squad?.SetAbilityIcon?.Any(sa => !sa?.RampActive ?? false) ?? false);
                    //var primaryAtack = ;
                    var isAnyIdleAttackAbility = memoryMeasurement?.ShipUi?.SquadronsUI?.SetSquadron?.Any(squad => squad?.SetAbilityIcon?.Any(sa => !sa?.RampActive ?? false) ?? false) ?? false;
                    if (isAnyIdleAttackAbility)
                        yield return new BotTask { Motion = VirtualKeyCode.F1.KeyboardPress() };*/


                    var isAnyAbilityActive= memoryMeasurement?.ShipUi?.SquadronsUI?.SetSquadron?.Any(squad => squad?.SetAbilityIcon?.Any(sa => sa?.RampActive ?? false) ?? false) ?? false;
                    if (!isAnyAbilityActive)
                        yield return new BotTask { Motion = VirtualKeyCode.F1.KeyboardPress() };

                }

                var overviewEntryLockTarget =
					listOverviewEntryToAttack?.FirstOrDefault(entry => !((entry?.MeTargeted ?? false) || (entry?.MeTargeting ?? false)));

				if (null != overviewEntryLockTarget && !(TargetCountMax <= memoryMeasurement?.Target?.Length))
					yield return overviewEntryLockTarget.ClickMenuEntryByRegexPattern(bot, @"^lock\s*target");

				if (!(0 < listOverviewEntryToAttack?.Length))
					/*if (0 < droneInLocalSpaceCount)
						yield return droneGroupInLocalSpace.ClickMenuEntryByRegexPattern(bot, @"return.*bay");
					else*/
						Completed = true;
                
			}
		}

		public MotionParam Motion => null;
	}
}
