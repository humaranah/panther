﻿using Panther.Core.Enums;
using Panther.Core.Events;
using Panther.Core.Models;

namespace Panther.Core.Interfaces
{
    public interface IPlayerQueue
    {
        bool IsShuffleEnabled { get; set; }
        RepeatMode RepeatMode { get; set; }
        TrackInfo? Current { get; set; }

        event EventHandler<QueueTrackChangedEventArgs> QueueTrackChanged;

        void Load(IEnumerable<TrackInfo> tracks);
        void Add(TrackInfo track);
        void Remove(TrackInfo track);
        TrackInfo? Previous();
        TrackInfo? Next();
    }
}