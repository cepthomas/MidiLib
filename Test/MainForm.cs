using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System.Drawing.Design;
using System.Text;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;


namespace Ephemera.MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>Where to put things.</summary>
        // readonly string _outPath = @"???";

        /// <summary>For debug.</summary>
        // int _count = 0;

        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";

        const string INDEV = "NebLoopback1";
        const string OUTDEV1 = "VirtualMIDISynth #1";
        const string OUTDEV2 = "Microsoft GS Wavetable Synth";
        #endregion


        //-------------------------------------------------------------------------------//
        [Serializable]
        public class TestSettings
        {
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public List<string> Devices { get; set; } = [];

            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public List<InputChannelSpec> InputChannels { get; set; } = [];

            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public List<OutputChannelSpec> OutputChannels { get; set; } = [];

            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            public int SomeOtherMidi { get; set; } = 0;
        }

        [Serializable]
        public class InputChannelSpec
        {
            /// <summary>Channel name. Optional.</summary>
            public string ChannelName { get; set; }

            /// <summary>Actual 1-based midi channel number. Required.</summary>
            public int ChannelNumber { get; init; }

        }

        [Serializable]
        public class OutputChannelSpec
        {
            /// <summary>Channel name. Optional.</summary>
            public string ChannelName { get; set; } = "";

            /// <summary>Actual 1-based midi channel number.</summary>
            public int ChannelNumber { get; init; } = 0;

            /// <summary>Instrument names. Optional.</summary>
            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string AliasFile { get; set; } = "";

            /// <summary>Instrument/patch.</summary>
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            // [TypeConverter(typeof(GenericConverter))]
            public int Patch { get; set; } = 0;
        }


        //-------------------------------------------------------------------------------//
        public MainForm()
        {
            InitializeComponent();
            Text = "MidiLib Test";

            // _outPath = MiscUtils.GetSourcePath();

            // The text output.
            tvInfo.Font = Font;
            tvInfo.WordWrap = true;
            tvInfo.Matchers =
            [
                new(ERROR, BgColor: Color.LightPink),
                new(WARN, BgColor: Color.Plum),
            ];

            // Master volume.
            sldMasterVolume.DrawColor = Color.SpringGreen;
            sldMasterVolume.Minimum = 0.0;
            sldMasterVolume.Maximum = VolumeDefs.MAX_VOLUME;
            sldMasterVolume.Resolution = VolumeDefs.MAX_VOLUME / 50;
            sldMasterVolume.Value = VolumeDefs.DEFAULT_VOLUME;
            sldMasterVolume.Label = "master volume";

            timeBar.DrawColor = Color.Green;
            timeBar.SelectedColor = Color.LightYellow;
            timeBar.Snap = SnapType.Beat;
            timeBar.StateChange += (sender, e) => { Tell(INFO, $">>> TimeBar event. {timeBar.Current}"); };

            // Simple UI handlers.
            btnKillMidi.Click += (_, __) => { _channels.ForEach(ch => ch.Value.Kill()); };
            chkLoop.CheckedChanged += (_, __) => { timeBar.DoLoop = chkLoop.Checked; };
            btnRewind.Click += (_, __) => { timeBar.Rewind(); };

            btnGo.Click += Go_Click;
            btnTimebar.Click += (_, _) => TestTimeBar();
            btnProperty.Click += (_, _) => TestPropertyEditor();

            btnSend.Click += BtnSend_Click;

            // Handle midi input.
            ChannelManager.Instance.MessageReceived += Mgr_MessageReceived;
            //MidiManager.Instance.MessageSent += Mgr_MessageSent;
        }

        /// <summary>
        /// Form is set up now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Tell(INFO, $">>> OnLoad start.");
            try
            {
                // Basic fixed channels.
                var chan_out1 = ChannelManager.Instance.OpenOutputChannel(OUTDEV1, 1, "channel 1!", "Harpsichord");
                var chan_out2 = ChannelManager.Instance.OpenOutputChannel(OUTDEV1, 2, "channel 2!", "Violin");

                // Bind.
                _channels.Add(ch_ctrl1, chan_out1);
                _channels.Add(ch_ctrl2, chan_out2);

                // Init controls. TODOP read from persistence.
                ch_ctrl1.BorderStyle = BorderStyle.FixedSingle;
                ch_ctrl1.DrawColor = Color.SpringGreen;
                ch_ctrl1.SelectedColor = Color.Yellow;
                ch_ctrl1.Volume = VolumeDefs.DEFAULT_VOLUME;
                ch_ctrl1.ChannelChange += ChannelControl_ChannelChange;

                ch_ctrl2.BorderStyle = BorderStyle.FixedSingle;
                ch_ctrl2.DrawColor = Color.SpringGreen;
                ch_ctrl2.SelectedColor = Color.Yellow;
                ch_ctrl2.Volume = VolumeDefs.DEFAULT_VOLUME;
                ch_ctrl2.ChannelChange += ChannelControl_ChannelChange;
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.Message);
            }

            Tell(INFO, $">>> OnLoad done.");

            Tell(INFO, string.Join(Environment.NewLine, MidiDefs.GenUserDeviceInfo()));

            base.OnLoad(e);
        }

        //-------------------------------------------------------------------------------//
        protected override void Dispose(bool disposing)
        {
            _channels.ForEach(ch => ch.Value.Kill());

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();

            ChannelManager.Instance.DestroyDevices();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        //-------------------------------------------------------------------------------//
        void Go_Click(object? sender, EventArgs e)
        {
            Tell(INFO, $">>> Go start.");

            try
            {
                // Create channels and controls dynamically. TODOP init solo/mute/volume/pan/??? from persistence
                var chan_out3 = ChannelManager.Instance.OpenOutputChannel(OUTDEV1, 1, "dyn keys", "HonkyTonkPiano");
                var chan_out4 = ChannelManager.Instance.OpenOutputChannel(OUTDEV1, 10, "dyn drums", "Electronic");
                //var chan_out3 = ChannelManager.Instance.OpenOutputChannel(OUTDEV1, 4, "dyn bass", "ElectricBassPick");
                var chan_in1 = ChannelManager.Instance.OpenInputChannel(INDEV, 1, "dyn my input");

                // Ceate a control for each channel and bind object.
                int x = ch_ctrl2.Right + 10;
                int y = ch_ctrl2.Top;

                List<OutputChannel> channels = [ chan_out3, chan_out4 ];
                channels.ForEach(chan =>
                {
                    var sui = $"Ch: {chan.ChannelNumber} {chan.ChannelName}";
                    var stt = $"{chan.PatchName} {chan.Patch:000}{Environment.NewLine}{chan.DeviceInfo}";

                    var ctrl = new ChannelControl()
                    {
                        Name = $"Control for {chan.ChannelName}",
                        Info = (sui, stt),
                        Location = new(x, y),
                        BorderStyle = BorderStyle.FixedSingle,
                        DrawColor = Color.SpringGreen,
                        SelectedColor = Color.Yellow,
                        Volume = VolumeDefs.DEFAULT_VOLUME,
                    };
                    ctrl.ChannelChange += ChannelControl_ChannelChange;
                    Controls.Add(ctrl);

                    _channels.Add(ctrl, chan);

                    x += ctrl.Width + 10; // Width is not valid until after previous statement.
                });
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.ToString());
            }

            Tell(INFO, $">>> Go done.");
        }

        //------------------------- Send some stuff ----------------------------------------//
        void BtnSend_Click(object? sender, EventArgs e)
        {
            // Run timer.
            timer1.Interval = 500;
            int count = 15;
            Random rand = new();
            void Timer1_Tick(object? sender, EventArgs e)
            {
                int noteNum = rand.Next(40, 60);
                SendNote(chan_out1, noteNum, 0.5);

                if (--count <= 0)
                {
                    timer1.Stop();
                    timer1.Tick -= Timer1_Tick;
                }
            }
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }

        //----------------- Test time bar -----------------------------------------------//
        void TestTimeBar()
        {
            // Sections. 256 ticks = 32 beats = 8 bars
            Dictionary<MusicTime, string> sectInfo = [];
            sectInfo.Add(new(0), "sect1");
            sectInfo.Add(new(128), "sect2"); // 4.0.0
            sectInfo.Add(new(256), "sect3"); // 8.0.0
            //sectInfo.Add(new(384), "END"); // 12.0.0
            sectInfo.Add(new(14, 2, 1), "END");

            timeBar.Snap = SnapType.FourBar;
            timeBar.GridLines = 4 * MusicTime.SubbeatsPerBar;
            timeBar.InitSectionInfo(sectInfo);
            timeBar.Invalidate();

            // Run timer.
            timer1.Interval = 3;
            int count = 1000;
            void Timer1_Tick(object? sender, EventArgs e)
            {
                timeBar.Increment();
                if (--count <= 0)
                {
                    timer1.Stop();
                    timer1.Tick -= Timer1_Tick;
                }
            }
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }

        //-------------------- Test property editing ------------------------------------//
        void TestPropertyEditor() // TODOP
        {
            // Init settings.
            TestSettings td = new()
            {
                Devices = ["NebLoopback1", "VirtualMIDISynth #1", "Microsoft GS Wavetable Synth"],
                InputChannels = [],
                OutputChannels = [],
                SomeOtherMidi = 88
            };

            // Set up options.
            //var insts = MidiDefs.GetDefaultInstrumentDefs();
            //IEnumerable<string> orderedValues = insts.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
            //var instsList = orderedValues.ToList();

            //Dictionary<int, string> vals = [];
            //Enumerable.Range(0, MidiDefs.MAX_MIDI + 1).ForEach(i => vals.Add(i, chan.GetInstrumentName(i)));
            //var instList = MidiUtils.CreateOrderedMidiList(vals, true, true);

            //GenericListTypeEditor.SetOptions("DeviceName", MidiOutputDevice.GetAvailableDevices());
            //GenericListTypeEditor.SetOptions("Patch", instList);

            var changes = SettingsEditor.Edit(td, "TESTOMATIC", 300);
            changes.ForEach(s => Tell(INFO, $"Editor changed {s}"));
        }

        //-------------------------------------------------------------------------------//
        void Tell(string cat, string s, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            var fn = Path.GetFileName(file);
            tvInfo.Append($"{cat} {fn}({line}) {s}");
        }

        ///////////////////////////////////////////////////////////////////////////////////
        ///////////// Stuff host needs to implement ///////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////

        /// <summary>All the channels.</summary>
        //OutputChannel? chan_out1;
        //OutputChannel? chan_out2;
        //OutputChannel? chan_out3;
        //OutputChannel? chan_out4;


        /// <summary>All the channel controls and associations. TODO1 a bit klunky..</summary>
        readonly List<ChannelControl> _channelControls = [];
        Dictionary<ChannelControl, OutputChannel> _channels = [];


        List<(OutputChannel, ChannelControl)> _channelsXX = [];


        //-------------------------------------------------------------------------------//
        /// <summary>User clicked something -> configure channels.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChange(object? sender, ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            //var channel = cc!.BoundChannel!;
            var channel = _channels[cc];

            if (e.State)
            {
                // Update all channels.
                bool anySolo = _channelControls.Where(c => c.State == ChannelState.Solo).Any();

                foreach (var cciter in _channelControls)
                {
                    bool enable = anySolo ?
                        cciter.State == ChannelState.Solo :
                        cciter.State != ChannelState.Mute;

                    channel.Enable = enable;
                    if (!enable)
                    {
                        // Kill just in case.
                        chan_out1.Kill();
                        //MidiManager.Instance.Kill(channel);
                    }
                }
            }

            if (e.Info)
            {
                // TODO do anything?
            }
        }

        //-------------------------------------------------------------------------------//
        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="note"></param>
        /// <param name="volume"></param>
        /// <exception cref="ArgumentException"></exception>
        void SendNote(OutputChannel channel, int note, double volume)
        {
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"Note:{note}"); }

            if (volume == 0.0)
            {
                channel.Send(new NoteOff(channel.ChannelNumber, note));
            }
            else if (channel.Enable)
            {
                var chctrl = _channels.Where(ch => ch.Value == channel);
                var vol = volume * sldMasterVolume.Value * chctrl.First().Key.Volume;
                var vel = (int)MathUtils.Constrain(vol * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI);
                channel.Send(new NoteOn(channel.ChannelNumber, note, vel));
                //if (_settings.MonitorSend) { _loggerMidi.Trace($">>> {e}"); }
            }
        }

        //-------------------------------------------------------------------------------//
        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="controller"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        void SendController(OutputChannel channel, int controller, int value)
        {
            if (controller is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"controller:{controller}"); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"value:{value}"); }

            // ?? if (channel.Enable)
            var se = new Controller(channel.ChannelNumber, controller, value);
            channel.Send(se);
            //if (_settings.MonitorSend) { _loggerMidi.Trace($">>> {e}"); }
        }

        //-------------------------------------------------------------------------------//
        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageReceived(object? sender, BaseEvent e)
        {
            // Get the channel and pass along msg.
            var chs = _channels.Where(ch => ch.Value.ChannelNumber == e.ChannelNumber);
            if (chs is not null && chs.Any())
            {
                var ch = chs.First();
                Tell(INFO, $"Mgr Received [{e}] on [{ch}]");
            }
        }
    }
}
