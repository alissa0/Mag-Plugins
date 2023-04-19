using System;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Mag.Shared;

namespace MagTools.Macros
{
	class OneTouchHeal
	{
		private bool _currentLightInfusedSetting = false;

		private WorldObject SetTargetKit()
		{
			WorldObjectCollection inventory = CoreManager.Current.WorldFilter.GetInventory();
			WorldObject targetKit = null;

			// No kits found with id data, lets just use the first kit we find
			if (targetKit == null
				|| (!this._currentLightInfusedSetting
					&& targetKit != null
					&& targetKit.Name.Contains("Light Infused")) // handle prefer light kits not being selected
				|| (this._currentLightInfusedSetting
					&& targetKit != null
					&& !targetKit.Name.Contains("Light Infused"))) // handle prefer light selected but not current target
			{
				WorldObject fallbackStandardKit = null;
				foreach (WorldObject obj in inventory)
				{
					if (obj.ObjectClass == ObjectClass.HealingKit)
					{
						if (!obj.HasIdData)
						{
							CoreManager.Current.Actions.RequestId(obj.Id);
						}

						// No mana or stamina kits please
						if (!obj.Name.Contains("Stamina")
							&& !obj.Name.Contains("Mana"))
						{
							if (this._currentLightInfusedSetting) // prefer light infused, fall back to standard if they don't exist
							{ 
								if (obj.Name.Contains("Light Infused"))
								{
									return obj;
								}
								else
								{
									fallbackStandardKit = obj;
								}
							}
							else // don't use light infused kits
							{
								if (!obj.Name.Contains("Light Infused"))
								{
									return obj;
								}
							}
						}

						if (targetKit == null
							&& fallbackStandardKit != null)
						{
							targetKit = fallbackStandardKit;
						}
					}
				}
			}
			return targetKit; 
		}

		public void Start()
		{
			int healthPointsFromMax = CoreManager.Current.Actions.Vital[VitalType.MaximumHealth] - CoreManager.Current.Actions.Vital[VitalType.CurrentHealth];
			if (healthPointsFromMax <= 0)
				return;

			// Try to use a healing kit
			if (CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Healing].Training >= TrainingType.Trained)
			{
				var targetKit = this.SetTargetKit();

				// Find the healing kit with the least uses left.
				if (targetKit == null
					|| this._currentLightInfusedSetting != Settings.SettingsManager.Misc.UseLumKitsIfAvailable.Value)
				{
					this._currentLightInfusedSetting = Settings.SettingsManager.Misc.UseLumKitsIfAvailable.Value;
					this.SetTargetKit();
				}


				if (targetKit != null)
				{
					int healingSkillRequired;

					if (CoreManager.Current.Actions.CombatMode == CombatState.Peace)
						healingSkillRequired = 2 * healthPointsFromMax;
					else
						healingSkillRequired = (int)Math.Ceiling(2.6 * healthPointsFromMax);

					int healingSkillWithKitBonus = CoreManager.Current.Actions.Skill[SkillType.CurrentHealing] + targetKit.Values(LongValueKey.AffectsVitalAmt);

					// healingSkillRequired == healingSkillWithKitBonus is ~50%
					if (healingSkillRequired < healingSkillWithKitBonus || !DoWeHaveFood())
					{
						CoreManager.Current.Actions.ApplyItem(targetKit.Id, CoreManager.Current.CharacterFilter.Id);

						return;
					}
				}
			}

			// Try to use a pot
			if (DoWeHaveFood())
			{
				if (UseFood())
					return;
			}

			// Are we in magic mode? Maybe we can cast a heal spell
		}

		private bool DoWeHaveFood()
		{
			foreach (WorldObject obj in CoreManager.Current.WorldFilter.GetInventory())
			{
				if (obj.ObjectClass == ObjectClass.Food)
				{
					if (obj.Name.Contains("Heal") || obj.Name.Contains("Meat"))
						return true;
				}
			}

			return false;
		}

		private bool UseFood()
		{
			foreach (WorldObject obj in CoreManager.Current.WorldFilter.GetInventory())
			{
				if (obj.ObjectClass == ObjectClass.Food)
				{
					if (obj.Name.Contains("Heal") || obj.Name.Contains("Meat"))
					{
						CoreManager.Current.Actions.UseItem(obj.Id, 0);
						return true;
					}
				}
			}

			return false;
		}
	}
}
