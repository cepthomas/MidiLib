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
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;


namespace Ephemera.MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = [];

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"???";

        /// <summary>Debug.</summary>
        int _count = 0;

        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";

        const string INDEV = "loopMIDI Port 1";
        const string OUTDEV1 = "VirtualMIDISynth #1";
        const string OUTDEV2 = "Microsoft GS Wavetable Synth";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Make sure out path exists.
            _outPath = Path.Join(MiscUtils.GetSourcePath(), "out");
            DirectoryInfo di = new(_outPath);
            di.Create();

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.MatchText.Add(ERROR, Color.LightPink);
            txtViewer.MatchText.Add(WARN, Color.Plum);

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
            timeBar.StateChange += TimeBar_StateChange;

            // Simple UI handlers.
            btnKillMidi.Click += (_, __) => { MidiManager.Instance.Kill(); };
            chkLoop.CheckedChanged += (_, __) => { timeBar.DoLoop = chkLoop.Checked; };
            btnRewind.Click += (_, __) => { timeBar.Rewind(); };

            btnGo.Click += Go_Click;

            // Report on midi action.
            MidiManager.Instance.MessageReceived += Mgr_MessageReceived;
            MidiManager.Instance.MessageSent += Mgr_MessageSent;
        }

        /// <summary>
        /// Window is set up now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Tell(INFO, $">>> OnLoad start.");

            try
            {
                //TestScriptApp();
                //TestPropertyEditor();
                //TestTimeBar();
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.Message);
            }

            Tell(INFO, $">>> OnLoad done.");

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            DestroyControls();
            MidiManager.Instance.DestroyDevices();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Do work
        void Go_Click(object? sender, EventArgs e)
        {
            Tell(INFO, $">>> Go start.");

            try
            {
                TestScriptApp();

                //TestPropertyEditor();

                //TestTimeBar();
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.ToString());
            }

            Tell(INFO, $">>> Go done.");
        }
        #endregion


        //-------------------------------------------------------------------------------//
        /// <summary>Test time bar.</summary>
        void TestTimeBar()
        {
            // Sections.
            Dictionary<int, string> sectInfo = [];
            sectInfo.Add(0, "sect1");
            sectInfo.Add(128, "sect2");
            sectInfo.Add(256, "sect3");
            sectInfo.Add(384, "END");

            timeBar.Snap = SnapType.FourBar;
            timeBar.GridLines = 4 * MusicTime.TicksPerBar;
            timeBar.InitSectionInfo(sectInfo);
            timeBar.Invalidate();

            timer1.Tick += Timer1_Tick;
            timer1.Interval = 3;

            _count = 10000; // 350;

            timer1.Start();
        }

        void Timer1_Tick(object? sender, EventArgs e)
        {
            timeBar.Increment();

            if (--_count <= 0)
            {
                Tell(INFO, $">>> Timer done.");
                timer1.Stop();
                timer1.Tick -= Timer1_Tick;
            }
        }

        void TimeBar_StateChange(object? sender, TimeBar.StateChangeEventArgs e)
        {
            Tell(INFO, $">>> TimeBar event. {timeBar.Current}");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!timeBar.FreeRunning && e.KeyData == Keys.Escape)
            {
                // Reset.
                timeBar.ResetSelection();
                Invalidate();
            }

            base.OnKeyDown(e);
        }

        //-------------------------------------------------------------------------------//


        //-------------------------------------------------------------------------------//
        /// <summary>Test property editing using TypeEditors.</summary>
        void TestPropertyEditor()
        {
            Tell(INFO, $">>> Property editor.");

            TargetClass td = new()
            {
                Patch = 77,
                ChannelName = "booga-booga",
                ChannelNumber = 5,
                //AliasFile = @"..\???.ini",
                DeviceName = "pdev1",
                SomeOtherMidi = 88
            };

            // Set up options.
            //var insts = MidiDefs.GetDefaultInstrumentDefs();
            //IEnumerable<string> orderedValues = insts.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
            //var instsList = orderedValues.ToList();

            var chan = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "keys", "AcousticGrandPiano");
            chan.PatchName = "HonkyTonkPiano";
            Dictionary<int, string> vals = [];
            Enumerable.Range(0, MidiDefs.MAX_MIDI + 1).ForEach(i => vals.Add(i, chan.GetInstrumentName(i)));
            var instList = MidiUtils.CreateOrderedMidiList(vals, true, true);

            GenericListTypeEditor.SetOptions("DeviceName", MidiOutputDevice.GetAvailableDevices());
            GenericListTypeEditor.SetOptions("Patch", instList);

            var changes = SettingsEditor.Edit(td, "TESTOMATIC", 300);
            changes.ForEach(s => Tell(INFO, $"Editor changed {s}"));
        }


        //-------------------------------------------------------------------------------//
        /// <summary>App driven by a script - as Nebulua/Nebulator. Creates channels and controls dynamically.</summary>
        void TestScriptApp()
        {
            ///// 0 - pre-steps - only for this demo
            ch_ctrl1.Hide();
            ch_ctrl2.Hide();

            ///// 1 - create all channels
            var chan_out1 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "keys", "HonkyTonkPiano");
            var chan_out2 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 10, "drums", "Electronic");
            //var chan_out3 = Manager.Instance.OpenOutputChannel(OUTDEV1, 4, "bass", "ElectricBassPick");
            var chan_in1 = MidiManager.Instance.OpenInputChannel(INDEV, 1, "my input");

            ///// 2 - create a control for each channel and bind object
            int x = sldMasterVolume.Left;
            int y = sldMasterVolume.Bottom + 10;

            List<OutputChannel> channels = [chan_out1, chan_out2];
            channels.ForEach(chan =>
            {
                var ctrl = new ChannelControl()
                {
                    Name = $"Control for {chan.ChannelName}",
                    BoundChannel = chan,
                    Options = DisplayOptions.All,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    DrawColor = Color.SpringGreen,
                    SelectedColor = Color.Yellow,
                    Volume = VolumeDefs.DEFAULT_VOLUME,
                    ControllerId = 10, // pan
                    ControllerValue = 82
                };
                ctrl.SetRenderer(new CustomRenderer());
                ctrl.ChannelChange += ChannelControl_ChannelChange;
                Controls.Add(ctrl);
                x += ctrl.Width + 4; // Width is not valid until after previous statement.
            });
        }

        //-------------------------------------------------------------------------------//
        /// <summary>A standard app where controls are defined in VS designer.</summary>
        void TestStandardApp()
        {
            // Create channels.
            var chan_out1 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "channel 1!", "Harpsichord");
            var chan_out2 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 2, "channel 2!", "Violin");

            // Init controls.
            ch_ctrl1.BorderStyle = BorderStyle.FixedSingle;
            ch_ctrl1.DrawColor = Color.SpringGreen;
            ch_ctrl1.SelectedColor = Color.Yellow;
            ch_ctrl1.Volume = VolumeDefs.DEFAULT_VOLUME;
            ch_ctrl1.ChannelChange += ChannelControl_ChannelChange;
            ch_ctrl1.BoundChannel = chan_out1;
            ch_ctrl1.ControllerValue = 64; // Sustain
            ch_ctrl1.ControllerValue = 45;
            ch_ctrl1.SetRenderer(new CustomRenderer());

            ch_ctrl2.BorderStyle = BorderStyle.FixedSingle;
            ch_ctrl2.DrawColor = Color.SpringGreen;
            ch_ctrl2.SelectedColor = Color.Yellow;
            ch_ctrl2.Volume = VolumeDefs.DEFAULT_VOLUME;
            ch_ctrl2.ChannelChange += ChannelControl_ChannelChange;
            ch_ctrl2.BoundChannel = chan_out1;
            ch_ctrl2.ControllerValue = 68; // Legato
            ch_ctrl2.ControllerValue = 90;
            ch_ctrl2.SetRenderer(new CustomRenderer());
        }

        //-------------------------------------------------------------------------------//
        /// <summary>General purpose test class</summary>
        [Serializable]
        public class TargetClass
        {
            /// <summary>Device name.</summary>
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public string DeviceName { get; set; } = "";

            /// <summary>Channel name - optional.</summary>
            public string ChannelName { get; set; } = "";

            /// <summary>Actual 1-based midi channel number.</summary>
            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            public int ChannelNumber { get; set; } = 1;

            /// <summary>Actual 1-based midi channel number.</summary>
            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            public int SomeOtherMidi { get; set; } = 0;

            /// <summary>Override default instrument presets.</summary>
            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string AliasFile { get; set; } = "";

            /// <summary>Current instrument/patch number.</summary>
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(GenericConverter))]
            public int Patch { get; set; } = 0;
        }

        #region Events
        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageReceived(object? sender, BaseEvent e)
        {
            Tell(INFO, $"MM Received [{e}]");
        }

        /// <summary>
        /// Something sent to a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageSent(object? sender, BaseEvent e)
        {
            Tell(INFO, $"MM Sent [{e}]");
        }

        /// <summary>
        /// UI clicked something -> configure channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChange(object? sender, ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

            if (e.State)
            {
                Tell(INFO, $"State change");

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
                        MidiManager.Instance.Kill(channel);
                    }
                }
            }
        }

        #endregion

        #region Misc internals
        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            MidiManager.Instance.Kill();

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
        }

        /// <summary>Tell me something good.</summary>
        /// <param name="s">What</param>
        void Tell(string cat, string s, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            var fn = Path.GetFileName(file);
            txtViewer.AppendLine($"{cat} {fn}({line}) {s}");
        }
        #endregion
    }
}
