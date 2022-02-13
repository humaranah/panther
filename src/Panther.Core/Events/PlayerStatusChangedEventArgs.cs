using Panther.Core.Enums;

namespace Panther.Core.Events
{
    public class PlayerStatusChangedEventArgs
    {
        public PlayerStatusChangedEventArgs(PlayerStatus last, PlayerStatus current)
        {
            Last = last;
            Current = current;
        }

        public PlayerStatus Last { get; }
        public PlayerStatus Current { get; }
    }
}
