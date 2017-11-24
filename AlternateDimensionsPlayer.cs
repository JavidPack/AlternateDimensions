using Terraria.GameInput;
using Terraria.ModLoader;

namespace AlternateDimensions
{
	public class AlternateDimensionsPlayer : ModPlayer
	{
		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (AlternateDimensions.SwapWorldsHotkey.JustPressed)
			{
				AlternateDimensions.worldInstance.Switch();
			}
		}
	}
}
