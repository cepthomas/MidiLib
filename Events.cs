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
        public int MidiChannel
        {
            get { return _midiChannel; }
            set { if (value is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) throw new ArgumentOutOfRangeException($"MidiChannel:{value}");
                  _midiChannel = value; }
        }
        int _midiChannel = 0;

        /// <summary>When to send. ZERO means unknown or don't care.</summary>
        public MusicTime When { get; set; } = MusicTime.ZERO;

        /// <summary>If true, delete after sending.</summary>
        public bool Transient { get; set; } = false;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"MidiChannel:{MidiChannel} When:{When}";
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

        /// <summary>Normal constructor.</summary>
        public NoteOn(int midiChannel, int note, int velocity, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{midiChannel}"); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"note:{note}"); }
            if (velocity is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"velocity:{velocity}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); }

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
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

        /// <summary>Normal constructor.</summary>
        public NoteOff(int midiChannel, int note, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{midiChannel}"); };
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"notec:{note}"); };
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); };

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
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
        public Enum Id { get; }

        /// <summary>Payload.</summary>
        public int Value { get; }

        /// <summary>Normal constructor.</summary>
        public Controller(int midiChannel, Enum ctrlr, int value, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"value:{value}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); }

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
            Id = ctrlr;
            Value = value;
        }

        /// <summary>Constructor from integer id.</summary>
        public Controller(int midiChannel, int ctrlr, int value, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"value:{value}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); }

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
            Id = (MidiDefs.ControllerId)Enum.ToObject(typeof(MidiDefs.ControllerId), ctrlr);

            Value = value;
        }

        /// <summary>Constructor from NAudio type.</summary>
        public Controller(NAudio.Midi.ControlChangeEvent evt)
        {
            Id = (MidiDefs.ControllerId)Enum.ToObject(typeof(MidiDefs.ControllerId), evt.Controller);
            MidiChannel = evt.Channel;
            Value = evt.ControllerValue;
            When = new(evt.AbsoluteTime);
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Controller:{Value} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    public class Patch : BaseEvent
    {
        /// <summary>Payload.</summary>
        public Enum Id { get; }

        /// <summary>Normal constructor.</summary>
        public Patch(int midiChannel, Enum patch, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); }
            //if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"value:{value}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); }

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
            Id = patch;
        }

        /// <summary>Constructor from integer id.</summary>
        public Patch(int midiChannel, int patch, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); }
            //if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"value:{value}"); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); }

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
            Id = (MidiDefs.InstrumentId)Enum.ToObject(typeof(MidiDefs.InstrumentId), patch);
        }

        /// <summary>Constructor from NAudio type.</summary>
        public Patch(NAudio.Midi.PatchChangeEvent evt)
        {
            Id = (MidiDefs.InstrumentId)Enum.ToObject(typeof(MidiDefs.InstrumentId), evt.Patch);
            MidiChannel = evt.Channel;
            When = new(evt.AbsoluteTime);
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Patch:{Id} ({Convert.ToInt32(Id)}) {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    /// <summary>Container for other real midi messages.</summary>
    public class Other : BaseEvent
    {
        /// <summary>Payload.</summary>
        public int RawMessage { get; init; }

        /// <summary>Normal constructor.</summary>
        public Other(int midiChannel, int rawMessage, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); };
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{when}"); };

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
            RawMessage = rawMessage;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Channel:{MidiChannel} {base.ToString()}";
        }
    }

    //----------------------------------------------------------------
    /// <summary>Custom type to support runtime functions.</summary>
    public class Function : BaseEvent 
    {
        /// <summary>The function to call.</summary>
        public Action ScriptFunction { get; init; }

        /// <summary>Normal constructor.</summary>
        public Function(int midiChannel, Action scriptFunc, MusicTime? when = null)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); };
            if (scriptFunc is null) { throw new ArgumentOutOfRangeException($"scriptFunc:{(scriptFunc)}"); };
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException($"when:{(when)}"); };

            When = when ?? MusicTime.ZERO;
            MidiChannel = midiChannel;
            ScriptFunction = scriptFunc;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"Function: {ScriptFunction} {base.ToString()}";
        }
    }
}
