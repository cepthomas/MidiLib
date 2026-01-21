using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    /// <summary>Base class for internal representation of midi events.</summary>
    public class BaseEvent
    {
        /// <summary>Channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { if (value is < MidiDefs.TEMP_CHANNEL or > MidiDefs.NUM_CHANNELS) throw new ArgumentOutOfRangeException($"ChannelNumber:{value}");
                  _channelNumber = value; }
        }
        int _channelNumber = 0;

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
        public int Note
        {
            get { return _note; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Note:{value}");
                  _note = value; }
        }
        int _note = 0;

        /// <summary>Note velocity, 0 to 127.</summary>
        public int Velocity
        {
            get { return _velocity; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Velocity:{value}");
                  _velocity = value; }
        }
        int _velocity = 0;

        public NoteOn(int channel, int note, int velocity, MusicTime? when = null)
        {
            if (channel is < MidiDefs.TEMP_CHANNEL or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channel:{channel}"); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"note:{note}"); }
            if (velocity is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"velocity:{velocity}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); }

            When = when ?? MusicTime.ZERO;
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
        public int Note
        {
            get { return _note; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Note:{value}");
                  _note = value; }
        }
        int _note = 0;

        public NoteOff(int channel, int note, MusicTime? when = null)
        {
            if (channel is < MidiDefs.TEMP_CHANNEL or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channel:{channel}"); };
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"notec:{note}"); };
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); };

            When = when ?? MusicTime.ZERO;
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
        public int Id
        {
            get { return _id; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Id:{value}");
                  _id = value; }
        }
        int _id = 0;

        /// <summary>Payload.</summary>
        public int Value
        {
            get { return _value; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Value:{value}");
                  _value = value; }
        }
        int _value = 0;

        public Controller(int channel, int id, int value, MusicTime? when = null)
        {
            if (channel is < MidiDefs.TEMP_CHANNEL or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channel:{(channel)}"); };
            if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"controllerId:{(id)}"); };
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"value:{value}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); };

            When = when ?? MusicTime.ZERO;
            ChannelNumber = channel;
            Id = id;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Controller:{MidiDefs.Controllers.GetName(Id)}({Id}):{Value} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    public class Patch : BaseEvent
    {
        /// <summary>Payload.</summary>
        public int Value
        {
            get { return _value; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"invalid:{value}");
                  _value = value; }
        }
        int _value = 0;

        public Patch(int channel, int value, MusicTime? when = null)
        {
            if (channel is < MidiDefs.TEMP_CHANNEL or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channel:{(channel)}"); };
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"value:{value}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); };

            When = when ?? MusicTime.ZERO;
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

        public Other(int channel, int rawMessage, MusicTime? when = null)
        {
            if (channel is < MidiDefs.TEMP_CHANNEL or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channel:{(channel)}"); };
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); };

            When = when ?? MusicTime.ZERO;
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

        public Function(int channel, Action scriptFunc, MusicTime? when = null)
        {
            if (channel is < MidiDefs.TEMP_CHANNEL or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channel:{(channel)}"); };
            if (scriptFunc is null) { throw new ArgumentOutOfRangeException($"scriptFunc:{(scriptFunc)}"); };
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{(when)}"); };

            When = when ?? MusicTime.ZERO;
            ChannelNumber = channel;
            ScriptFunction = scriptFunc;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"Function: {ScriptFunction} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    /// <summary>Helper for managing groups of events - mainly sugar.</summary>
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
            _allEvents.TryGetValue(when, out List<BaseEvent>? res);
            return res ?? [];
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
