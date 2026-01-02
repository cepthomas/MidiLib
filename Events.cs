using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    public class BaseMidi
    {
        /// <summary>Channel number.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; } = 0;

        /// <summary>When to send.</summary>
        public MusicTime When { get; set; } = MusicTime.ZERO;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"ChannelNumber:{ChannelNumber} When:{When}";
        }
    }

    //----------------------------------------------------------------
    public class NoteOn : BaseMidi
    {
        /// <summary>The note number to play.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Note { get; init; }

        /// <summary>0 to 127.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Velocity { get; set; }

        public NoteOn(int channel, int note, int velocity, MusicTime? when = null) // TODO1 double volume
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }
            if (velocity is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(velocity)); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when ?? new();
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
    public class NoteOff : BaseMidi
    {
        /// <summary>The note number to stop.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Note { get; init; }

        public NoteOff(int channel, int note, MusicTime? when = null)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when ?? new();
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
    public class Controller : BaseMidi
    {
        /// <summary>Specific controller id.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerId { get; init; }

        /// <summary>Payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Value { get; init; }

        public Controller(int channel, int controllerId, int value, MusicTime? when = null)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (controllerId is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controllerId)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when ?? new();
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
    public class Patch : BaseMidi
    {
        /// <summary>Payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Value { get; init; }

        public Patch(int channel, int value, MusicTime? when = null)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when ?? new();
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
    /// <summary>Container for other midi messages.</summary>
    public class Other : BaseMidi
    {
        /// <summary>Payload.</summary>
        //[Range(0, MidiDefs.MAX_MIDI)]
        public int RawMessage { get; init; }

        public Other(int channel, int rawMessage, MusicTime? when = null)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when ?? new();
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
    public class Function : BaseMidi 
    {
        /// <summary>The function to call.</summary>
        public Action ScriptFunction { get; init; }

        public Function(int channel, Action scriptFunc, MusicTime? when = null)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (scriptFunc is null) { throw new ArgumentOutOfRangeException(nameof(scriptFunc)); }
            if (when?.Tick is < 0) { throw new ArgumentOutOfRangeException(nameof(when)); }

            When = when ?? new();
            ChannelNumber = channel;
            ScriptFunction = scriptFunc;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"Function: {ScriptFunction} {base.ToString()}";
        }
    }
}
