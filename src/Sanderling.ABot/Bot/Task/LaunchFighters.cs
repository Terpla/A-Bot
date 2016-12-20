using System.Collections.Generic;
using Sanderling.Motor;
using BotEngine.Motor;
using Sanderling.Parse;

namespace Sanderling.ABot.Bot.Task
{
    public class LaunchFighters: IBotTask
    {
        public IMemoryMeasurement MemoryMeasurement;

        public IEnumerable<IBotTask> Component => null;

        public MotionParam Motion
        {
            get
            {
                var launchButton = MemoryMeasurement?.ShipUi?.SquadronsUI?.LaunchAllButton;
                if (launchButton == null)
                    return null;

                return launchButton?.MouseClick(MouseButtonIdEnum.Left);
            }
        }
    }
}

