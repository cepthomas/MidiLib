using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;
using System.Runtime.CompilerServices;


namespace Ephemera.MidiLib
{
    // TODO1 smart? [if (channel is < 0] is allowed so controls can gen events without knowing their own chan num
    // Gets filled in by the client eventually.

    //----------------------------------------------------------------
    public class BaseMidi
    {
        /// <summary>Channel number.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; }

        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"BaseMidi:{ChannelNumber} {ErrorInfo}";
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
        public int Velocity { get; init; }

        public NoteOn(int channel, int note, int velocity)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }
            if (velocity is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(velocity)); }

            ChannelNumber = channel;
            Note  = note;
            Velocity = velocity;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOn:{Note} V:{Velocity}";
        }
    }

    //----------------------------------------------------------------
    public class NoteOff : BaseMidi
    {
        /// <summary>The note number to play.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Note { get; init; }

        public NoteOff(int channel, int note)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note)); }

            ChannelNumber = channel;
            Note  = note;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"NoteOff:{Note}";
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

        public Controller(int channel, int controllerId, int value)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (controllerId is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controllerId)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }

            ChannelNumber = channel;
            ControllerId = controllerId;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"ControllerId:{MidiDefs.Instance.GetControllerName(ControllerId)}({ControllerId}):{Value}";
        }
    }

    //----------------------------------------------------------------
    public class Patch : BaseMidi
    {
        /// <summary>Payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Value { get; init; }

        public Patch(int channel, int value)
        {
            if (channel is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channel)); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }

            ChannelNumber = channel;
            Value = value;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Channel:{ChannelNumber} Patch:{Value}"; // get patch name from channel?
        }
    }
}
