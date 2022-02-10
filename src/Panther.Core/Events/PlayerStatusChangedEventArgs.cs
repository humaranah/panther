using Panther.Core.Enums;

namespace Panther.Core.Events
{
    public class PlayerStatusChangedEventArgs
    {
        public PlayerStatusChangedEventArgs(PlayerStatus previousStatus, PlayerStatus currentStatus)
        {
            PreviousStatus = previousStatus;
            CurrentStatus = currentStatus;
        }

        public PlayerStatus PreviousStatus { get; }
        public PlayerStatus CurrentStatus { get; }
    }
}
