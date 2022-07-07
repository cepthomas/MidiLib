﻿using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;


namespace MidiLib
{
    [Serializable]
    public class MidiSettings
    {
        /// <summary>Current midi settings. Client must set this before accessing!</summary>
        [Browsable(false)]
        public static MidiSettings LibSettings
        {
            get { if (_settings is null) throw new InvalidOperationException("Client must set this property before accessing"); return _settings; }
            set { _settings = value; }
        }
        static MidiSettings? _settings = null;

        #region Properties - persisted editable
        [DisplayName("Default Tempo")]
        [Description("Use this tempo if it's not in the file.")]
        [Browsable(true)]
        public int DefaultTempo { get; set; } = 100;

        [DisplayName("Internal Time Resolution")]
        [Description("aka DeltaTicksPerQuarterNote or subdivisions per beat.")]
        [Browsable(false)] // TODO Implement user selectable later maybe.
        [JsonIgnore()]
        public int InternalPPQ { get; set; } = 32;

        //[DisplayName("Zero Based Time")]
        //[Description("Use 0:0:0 time instead of 1:1:1.")]
        //[Browsable(true)]
        //public bool ZeroBased { get; set; } = false; // TODO Implement later maybe. For now it's always true.

        /// <summary>How to snap.</summary>
        [DisplayName("Snap Type")]
        [Description("How to snap to grid.")]
        [Browsable(true)]
        public SnapType Snap { get; set; } = SnapType.Beat;
        #endregion

        #region Properties - internal
        /// <summary>Only 4/4 time supported.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int BeatsPerBar { get { return 4; } }

        /// <summary>Convenience.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int SubdivsPerBeat { get { return InternalPPQ; } }

        /// <summary>Convenience.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int SubdivsPerBar { get { return InternalPPQ * BeatsPerBar; } }
        #endregion

        // /// <summary>
        // /// Utility function to make client's life easier.
        // /// </summary>
        // /// <returns></returns>
        // public List<(string id, string name)> GetInputDevices()
        // {
        //     var devs = new List<(string, string)>();
        //     if (InputDevice1 != "")
        //     {
        //         devs.Add(("InputDevice1", InputDevice1));
        //     }
        //     if (InputDevice2 != "")
        //     {
        //         devs.Add(("InputDevice2", InputDevice2));
        //     }
        //     if (InputDevice3 != "")
        //     {
        //         devs.Add(("InputDevice3", InputDevice3));
        //     }
        //     if (InputDevice4 != "")
        //     {
        //         devs.Add(("InputDevice4", InputDevice4));
        //     }

        //     return devs;
        // }

        // /// <summary>
        // /// Utility function to make client's life easier.
        // /// </summary>
        // /// <returns></returns>
        // public List<(string id, string name)> GetOutputDevices()
        // {
        //     var devs = new List<(string, string)>();
        //     if (OutputDevice1 != "")
        //     {
        //         devs.Add(("OutputDevice1", OutputDevice1));
        //     }
        //     if (OutputDevice2 != "")
        //     {
        //         devs.Add(("OutputDevice2", OutputDevice2));
        //     }

        //     return devs;
        // }

    }

}
