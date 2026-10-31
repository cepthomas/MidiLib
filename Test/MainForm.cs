using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.IO;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using System.Text.Json.Serialization;


namespace Ephemera.MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Fields
        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";

        const string INDEV = "NebLoopback1";
        const string OUTDEV1 = "VirtualMIDISynth #1";
        const string OUTDEV2 = "Microsoft GS Wavetable Synth";

        // Shorthand
        readonly ChannelManager CM = ChannelManager.Instance;

        TestSettings _settings;
        #endregion

        #region Settings
        [Serializable]
        public class TestSettings
        {
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public List<string> DeviceNames { get; set; } = [];

            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public List<InputChannelStore> InputChannels { get; set; } = [];

            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public List<OutputChannelStore> OutputChannels { get; set; } = [];

            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            public int SomeOtherMidi { get; set; } = 0;

            // Master volume.
            public double Volume { get; init; } = 0.5;

            [JsonConverter(typeof(JsonColorConverter))]
            public Color DrawColor { get; set; } = Color.DodgerBlue;

            [JsonConverter(typeof(JsonColorConverter))]
            public Color SelectedColor { get; set; } = Color.Moccasin;

        }

        [Serializable]
        public class InputChannelStore
        {
            /// <summary>Channel name. Optional.</summary>
            public string ChannelName { get; set; } = "???";

            /// <summary>Actual 1-based midi channel number. Required.</summary>
            public int ChannelNumber { get; init; } = 0;

            /// <summary>True if channel is active.</summary>
            public bool Enable { get; set; } = true;
        }

        [Serializable]
        public class OutputChannelStore
        {
            public string ChannelName { get; set; } = "???";

            public int ChannelNumber { get; init; } = 0;

            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string AliasFile { get; set; } = "";

            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            // [TypeConverter(typeof(GenericConverter))]
            public string DeviceName { get; set; } = "";

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public ChannelState State { get; set; } = ChannelState.Normal;

            public double Volume { get; init; } = 0.5;

            public double Pan { get; init; } = 0.0;

        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Hello.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            Text = "MidiLib Test";

            // Init settings.
            _settings = new()
            {
                DeviceNames = ["NebLoopback1", "VirtualMIDISynth #1", "Microsoft GS Wavetable Synth"],
                InputChannels = [],
                OutputChannels = [],
                SomeOtherMidi = 88,
                Volume = 0.7,
                DrawColor = Color.SpringGreen,
                SelectedColor = Color.Magenta
            };

            // The text output.
            tvInfo.Font = Font;
            tvInfo.WordWrap = true;
            tvInfo.Matchers =
            [
                new(ERROR, BgColor: Color.LightPink),
                new(WARN, BgColor: Color.Plum),
            ];

            // Master volume.
            sldMasterVolume.DrawColor = _settings.DrawColor;
            sldMasterVolume.Minimum = 0.0;
            sldMasterVolume.Maximum = VolumeDefs.MAX_VOLUME;
            sldMasterVolume.Resolution = VolumeDefs.MAX_VOLUME / 50;
            sldMasterVolume.Value = _settings.Volume;
            sldMasterVolume.Label = "master volume";

            timeBar.DrawColor = _settings.DrawColor;
            timeBar.SelectedColor = _settings.SelectedColor;
            timeBar.Snap = SnapType.Beat;
            timeBar.StateChange += (sender, e) => { Tell(INFO, $">>> TimeBar event. {timeBar.Current}"); };

            // Simple UI handlers.
            btnKillMidi.Click += (_, __) => { CM.KillAll(); };
            chkLoop.CheckedChanged += (_, __) => { timeBar.DoLoop = chkLoop.Checked; };
            btnRewind.Click += (_, __) => { timeBar.Rewind(); };

            btnGo.Click += Go_Click;
            btnTimebar.Click += (_, _) => TestTimeBar();
            btnProperty.Click += (_, _) => TestPropertyEditor();

            btnSend.Click += BtnSend_Click;

            // Handle midi io.
            CM.MessageReceived += Mgr_MessageReceived;
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
                var chan = CM.OpenOutputChannel(OUTDEV1, 1, "channel 1!", "Harpsichord");
                // Init controls.
                ch_ctrl1.BorderStyle = BorderStyle.FixedSingle;
                ch_ctrl1.DrawColor = _settings.DrawColor;
                ch_ctrl1.SelectedColor = _settings.SelectedColor;

                ch_ctrl1.Volume = _settings.OutputChannels[0].Volume;
                ch_ctrl1.State = _settings.OutputChannels[0].State;
                // etc like these...
                //public string ChannelName { get; set; } = "???";
                //public int ChannelNumber { get; init; } = 0;
                //public string AliasFile { get; set; } = "";
                //public string DeviceName { get; set; } = "";
                //public double Pan { get; init; } = 0.0;

                ch_ctrl1.ChannelChange += ChannelControl_ChannelChange;
                CM.Bind(chan, ch_ctrl1);

                chan = CM.OpenOutputChannel(OUTDEV1, 2, "channel 2!", "Violin");
                ch_ctrl2.BorderStyle = BorderStyle.FixedSingle;
                ch_ctrl2.DrawColor = _settings.DrawColor;
                ch_ctrl2.SelectedColor = _settings.SelectedColor;

                ch_ctrl2.Volume = _settings.OutputChannels[0].Volume;
                ch_ctrl2.State = _settings.OutputChannels[0].State;
                // etc like above...

                ch_ctrl2.ChannelChange += ChannelControl_ChannelChange;
                CM.Bind(chan, ch_ctrl2);
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.Message);
            }

            CM.UpdateState();

            Tell(INFO, $">>> OnLoad done.");

            Tell(INFO, string.Join(Environment.NewLine, MidiDefs.GenUserDeviceInfo()));

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            // TODO refresh and save settings.

            if (disposing && (components is not null))
            {
                CM.Destroy(Controls);
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Test code
        /// <summary>
        /// Basic ops.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Go_Click(object? sender, EventArgs e)
        {
            Tell(INFO, $">>> Go start.");

            try
            {
                // Create channels and controls dynamically.
                List<OutputChannel> channels = [];

                var chan = CM.OpenOutputChannel(OUTDEV1, 1, "dyn keys", "HonkyTonkPiano");
                channels.Add(chan);
                chan = CM.OpenOutputChannel(OUTDEV1, 10, "dyn drums", "Electronic");
                channels.Add(chan);
                chan = CM.OpenOutputChannel(OUTDEV1, 4, "dyn bass", "KUSTOM", @"C:\Dev\Libs\MidiLib\Test\test_defs.ini");
                channels.Add(chan);

                var inchan = CM.OpenInputChannel(INDEV, 1, "dyn my input"); // TODO1 test this

                // Ceate a control for each channel and bind object.
                int x = ch_ctrl2.Right + 10;
                int y = ch_ctrl2.Top;

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
                        DrawColor = _settings.DrawColor,
                        SelectedColor = _settings.SelectedColor,
                        Volume = VolumeDefs.DEFAULT_VOLUME,
                    };
                    ctrl.ChannelChange += ChannelControl_ChannelChange;
                    Controls.Add(ctrl);

                    CM.Bind(chan, ctrl);

                    x += ctrl.Width + 10; // Width is not valid until after previous statement.
                });
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.ToString());
            }

            CM.UpdateState();

            Tell(INFO, $">>> Go done.");
        }

        /// <summary>
        /// Send some test stuff
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BtnSend_Click(object? sender, EventArgs e)
        {
            // Run timer.
            timer1.Interval = 500;
            int count = 15;
            Random rand = new();
            void Timer1_Tick(object? sender, EventArgs e)
            {
                int noteNum = rand.Next(40, 60);
                var chan = CM.GetChannel(ch_ctrl1);
                SendNote(chan!, noteNum, 0.5);

                if (--count <= 0)
                {
                    timer1.Stop();
                    timer1.Tick -= Timer1_Tick;
                }
            }
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }

        /// <summary>
        /// Test time bar
        /// </summary>
        void TestTimeBar()
        {
            // Sections. 256 ticks = 32 beats = 8 bars
            Dictionary<MusicTime, string> sectInfo = [];
            sectInfo.Add(new(0), "sect1");
            sectInfo.Add(new(128), "sect2");    // 4.0.0
            sectInfo.Add(new(256), "sect3");    // 8.0.0
            //sectInfo.Add(new(384), "END");    // 12.0.0
            sectInfo.Add(new(14, 2, 1), "END"); // 14.2.1

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

        /// <summary>
        /// Test property editing
        /// </summary>
        void TestPropertyEditor() // TODO
        {
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

        /// <summary>
        /// Logging.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="s"></param>
        void Tell(string cat, string s, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            var fn = Path.GetFileName(file);
            tvInfo.Append($"{cat} {fn}({line}) {s}");
        }
        #endregion

        #region Stuff host needs to implement
        /// <summary>
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChange(object? sender, ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var chan = CM.GetChannel(cc!);
            if (chan is null) return; // error?

            if (e.State) { CM.UpdateState(); }

            if (e.Info) {  } // TODO do anything?

            if (e.Volume) {  } // TODO do anything?
        }

        /// <summary>
        /// Send midi note.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="note"></param>
        /// <param name="volume"></param>
        /// <exception cref="ArgumentException"></exception>
        void SendNote(OutputChannel chan, int note, double volume)
        {
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"Note:{note}"); }

            if (volume == 0.0)
            {
                chan.Send(new NoteOff(chan.ChannelNumber, note));
            }
            else
            {
                var ctrl = CM.GetControl(chan);
                if (ctrl is null) return; // error?
                var vol = volume * sldMasterVolume.Value * ctrl.Volume;
                var vel = (int)MathUtils.Constrain(vol * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI);
                var se = new NoteOn(chan.ChannelNumber, note, vel);
                chan.Send(se);
                Tell($">>>", se.ToString());
            }
        }

        /// <summary>
        /// Send midi controller.
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
            Tell($">>>", se.ToString());
        }

        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageReceived(object? sender, BaseEvent e)
        {
            // Get the channel and pass along msg.
            //var chan = _chanBindings.Where(ch => ch.Key.ChannelNumber == e.ChannelNumber);
            var chan = CM.GetChannel(e.ChannelNumber);
            if (chan is not null)
            {
                Tell(INFO, $"Mgr Received [{e}] on [{chan}]");
            }
            // else error?
        }
        #endregion
    }
}
