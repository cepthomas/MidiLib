using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    /// <summary>Base class for internal representation of midi events.</summary>
    public class BaseEvent
    {
        /// <summary>Channel number.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; } = 0;

        /// <summary>When to send. ZERO means unknown or don't care.</summary>
        public MusicTime When { get; set; } = MusicTime.ZERO;

        /// <summary>If true, delete after sending.</summary>
        public bool Transient { get; set; } = false;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"ChannelNumber:{ChannelNumber} When:{When}";
        }
    }

    //----------------------------------------------------------------
    public class NoteOn : BaseEvent
    {
        /// <summary>The note number to play.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Note { get; init; }

        /// <summary>0 to 127.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Velocity { get; set; }

        public NoteOn(int channel, int note, int velocity, MusicTime when)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }
            if (velocity is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(velocity)); }
            if (when.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when;
            ChannelNumber = channel;
            Note  = note;
            Velocity = velocity;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOn:{Note} Vel:{Velocity} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    public class NoteOff : BaseEvent
    {
        /// <summary>The note number to stop.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Note { get; init; }

        public NoteOff(int channel, int note, MusicTime when)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }
            if (when.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when;
            ChannelNumber = channel;
            Note  = note;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOff:{Note} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    public class Controller : BaseEvent
    {
        /// <summary>Specific controller id.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerId { get; init; }

        /// <summary>Payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Value { get; init; }

        public Controller(int channel, int controllerId, int value, MusicTime when)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (controllerId is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controllerId)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }
            if (when.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when;
            ChannelNumber = channel;
            ControllerId = controllerId;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"ControllerId:{MidiDefs.Instance.GetControllerName(ControllerId)}({ControllerId}):{Value} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    public class Patch : BaseEvent
    {
        /// <summary>Payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Value { get; init; }

        public Patch(int channel, int value, MusicTime when)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }
            if (when.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when;
            ChannelNumber = channel;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Patch:{Value} {base.ToString()}"; // get patch name from channel?
        }
    }

    //----------------------------------------------------------------
    /// <summary>Container for other real midi messages.</summary>
    public class Other : BaseEvent
    {
        /// <summary>Payload.</summary>
        public int RawMessage { get; init; }

        public Other(int channel, int rawMessage, MusicTime when)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (when.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when;
            ChannelNumber = channel;
            RawMessage = rawMessage;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Channel:{ChannelNumber} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    /// <summary>Custom type to support runtime functions.</summary>
    public class Function : BaseEvent 
    {
        /// <summary>The function to call.</summary>
        public Action ScriptFunction { get; init; }

        public Function(int channel, Action scriptFunc, MusicTime when)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (scriptFunc is null) { throw new ArgumentOutOfRangeException(nameof(scriptFunc)); }
            if (when.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when;
            ChannelNumber = channel;
            ScriptFunction = scriptFunc;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"Function: {ScriptFunction} {base.ToString()}";
        }
    }


    /// <summary>
    /// Helper for managing groups of events - mainly sugar.
    /// </summary>
    public class EventCollection
    {
        readonly Dictionary<MusicTime, List<BaseEvent>> _allEvents = [];

        public void Add(BaseEvent evt)
        {
            if (!_allEvents.TryGetValue(evt.When, out List<BaseEvent>? events))
            {
                _allEvents.Add(evt.When, new([evt]));
            }
            else
            {
                events.Add(evt);
            }
        }

        public void AddRange(IEnumerable<BaseEvent> evts)
        {
            evts.ForEach(e => Add(e));
        }

        public IEnumerable<BaseEvent> Get(MusicTime when)
        {
            IEnumerable<BaseEvent> res = [];

            if (_allEvents.TryGetValue(when, out List<BaseEvent>? events))
            {
                res = events;
            }

            return res;
        }

        public void RemoveTransients(MusicTime when)
        {
            if (_allEvents.TryGetValue(when, out List<BaseEvent>? value))
            {
                value.RemoveAll(e => e.Transient);
                _allEvents.Remove(when);
            }
        }

        public int Count()
        {
            int total = 0;
            _allEvents.ForEach(e => total += e.Value.Count);
            return total;
        }
    }
}
