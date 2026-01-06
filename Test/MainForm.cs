using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
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
            btnGen.Click += Gen_Click;

            //Manager.Instance.MessageReceive += Mgr_MessageReceive;
            //Manager.Instance.MessageSend += Mgr_MessageSend;
        }

        /// <summary>
        /// Window is set up now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Tell(INFO, $">>>>> OnLoad start.");

            try
            {
                TestX();

                //TestScriptApp();
                //TestPropertyEditor();
                //TestTimeBar();

                //TestMusicTime();

                //TestDefFile();

                //TestChannel();
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.Message);
            }

            Tell(INFO, $">>>>> OnLoad done.");

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

        void TestX()
        {
            var _outputDevice = MidiManager.Instance.GetOutputDevice("");



        }


        #region Do work
        void Go_Click(object? sender, EventArgs e)
        {
            Tell(INFO, $">>>>> Go start.");

            try
            {
                TestScriptApp();

                //TestMusicTime();

                //TestDefFile();

                //TestPropertyEditor();

                //TestChannel();

                //TestTimeBar();
            }
            catch (Exception ex)
            {
                Tell(ERROR, ex.ToString());//.Message);
            }

            Tell(INFO, $">>>>> Go done.");
        }
        #endregion

        //-------------------------------------------------------------------------------//
        /// <summary>Test gen aux files.</summary>
        void Gen_Click(object? sender, EventArgs e) // TODO1 -> UT
        {
            Tell(INFO, $">>>>> Gen start.");
            var myPath = MiscUtils.GetSourcePath();
            string fnIni = Path.Combine(myPath, "..", "gm_defs.ini");

            Tell(INFO, $">>>>> Gen Markdown.");
            var smd = MidiDefs.GenMarkdown();
            var fnOut = Path.Join(myPath, "out", "midi_defs.md");
            File.WriteAllText(fnOut, string.Join(Environment.NewLine, smd));

            Tell(INFO, $">>>>> Gen Lua.");
            var sld = MidiDefs.GenLua();
            fnOut = Path.Join(myPath, "out", "midi_defs.lua");
            File.WriteAllText(fnOut, string.Join(Environment.NewLine, sld));

            Tell(INFO, $">>>>> Gen Device Info.");
            var sdi = MidiDefs.GenUserDeviceInfo();
            Tell(INFO, string.Join(Environment.NewLine, sdi));

            Tell(INFO, $">>>>> Gen done.");
        }

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
                Tell(INFO, $">>>>> Timer done.");
                timer1.Stop();
                timer1.Tick -= Timer1_Tick;
            }
        }

        void TimeBar_StateChange(object? sender, TimeBar.StateChangeEventArgs e)
        {
            Tell(INFO, $">>>>> TimeBar event. {timeBar.Current}");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (timeBar.Valid && e.KeyData == Keys.Escape)
            {
                // Reset.
                timeBar.ResetSelection();
                Invalidate();
            }

            base.OnKeyDown(e);
        }

        //-------------------------------------------------------------------------------//
        /// <summary>Test channel logic.</summary>
        void TestChannel() // TODO1 -> UT
        {
            Tell(INFO, $">>>>> Channel.");
            var myPath = MiscUtils.GetSourcePath();

            // Dummy device.
            var outdev = "nullout:test1";
            //var dev = new NullOutputDevice("DUMMY_DEVICE");

            var chan_out1 = MidiManager.Instance.OpenOutputChannel(outdev, 1, "keys", false);
            // GM instruments
            chan_out1.PatchName = "HonkyTonkPiano";

            // GM drums
            var chan_out2 = MidiManager.Instance.OpenOutputChannel(outdev, 10, "drums", true);
            // TODO1 needs built in drumss or file
            chan_out2.PatchName = "Electronic";

            // Alt instruments
            var chan_out3 = MidiManager.Instance.OpenOutputChannel(outdev, 4, "bass", false);
            chan_out3.InstrumentFile = Path.Combine(myPath, "test_defs.ini");
            chan_out3.PatchName = "WaterWhistle2";

            // Input
            var chan_in1 = MidiManager.Instance.OpenInputChannel(outdev, 1, "my input");

            // Test aliases.
            if (chan_out1.GetInstrumentName(40) != "SynthGuitar1") Tell(ERROR, "FAIL");
            if (chan_out2.GetInstrumentName(101) != "INST_101") Tell(ERROR, "FAIL");

            // Should send midi.
            chan_out3.Patch = 77;
            //if (dev.CollectedEvents.Count != 1) Tell(ERROR, "FAIL");

            Tell(INFO, "DONE");
        }



        //var chan_out1 = Manager.Instance.OpenOutputChannel(OUTDEV1, 1, "keys", "HonkyTonkPiano", false);
        ////var chan_out2 = Manager.Instance.OpenOutputChannel(OUTDEV1, 4, "bass", "ElectricBassPick");
        //var chan_out2 = Manager.Instance.OpenOutputChannel(OUTDEV1, 10, "drums", "Electronic", true);
        //var chan_out1 = Manager.Instance.OpenOutputChannel(OUTDEV1, 1, "channel 1!", "Harpsichord", false);
        //var chan_out2 = Manager.Instance.OpenOutputChannel(OUTDEV1, 2, "channel 2!", "Violin", false);

        //-------------------------------------------------------------------------------//
        /// <summary>Test property editing using TypeEditors.</summary>
        void TestPropertyEditor()
        {
            Tell(INFO, $">>>>> Property editor.");

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

            var chan = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "keys", false);
            chan.PatchName = "HonkyTonkPiano";
            Dictionary<int, string> vals = [];
            Enumerable.Range(0, MidiDefs.MAX_MIDI + 1).ForEach(i => vals.Add(i, chan.GetInstrumentName(i)));
            var instList = MidiDefs.CreateOrderedMidiList(vals, true, true);

            GenericListTypeEditor.SetOptions("DeviceName", MidiOutputDevice.GetAvailableDevices());
            GenericListTypeEditor.SetOptions("Patch", instList);

            var changes = SettingsEditor.Edit(td, "TESTOMATIC", 300);
            changes.ForEach(s => Tell(INFO, $"Editor changed {s}"));
        }

        //-------------------------------------------------------------------------------//
        /// <summary>Test def file loading etc.</summary>
        void TestDefFile() // TODO1 -> UT
        {
            Tell(INFO, $">>>>> Low level loading.");
            var myPath = MiscUtils.GetSourcePath();
            string fn = Path.Join(myPath, "..", "gm_defs.ini");

            var ir = new IniReader();
            ir.ParseFile(fn);

            ir.GetSectionNames().ForEach(n => Tell(INFO, $"section:{n} => {ir.GetValues(n).Count}"));
        }

        //-------------------------------------------------------------------------------//
        /// <summary>App driven by a script - as Nebulua/Nebulator. Creates channels and controls dynamically.</summary>
        void TestScriptApp()
        {
            ///// 0 - pre-steps - only for this demo
            ch_ctrl1.Hide();
            ch_ctrl2.Hide();

            ///// 1 - create all channels
            var chan_out1 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "keys", false);
            chan_out1.PatchName = "HonkyTonkPiano";
            var chan_out2 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 10, "drums", true);
            chan_out2.PatchName = "Electronic";
            //var chan_out3 = Manager.Instance.OpenOutputChannel(OUTDEV1, 4, "bass", "ElectricBassPick");
            var chan_in1 = MidiManager.Instance.OpenInputChannel(INDEV, 1, "my input");

            ///// 2 - create a control for each channel and bind object
            int x = sldMasterVolume.Left;
            int y = sldMasterVolume.Bottom + 10;

            List<OutputChannel> channels = [chan_out1, chan_out2];
            channels.ForEach(chan =>
            {
                var rend = new CustomRenderer() { ChannelNumber = chan.ChannelNumber };
                rend.SendMidi += ChannelControl_SendMidi;

                var ctrl = new ChannelControl()
                {
                    Name = $"Control for {chan.ChannelName}",
                    BoundChannel = chan,
                    UserRenderer = rend,
                    Options = DisplayOptions.All,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    DrawColor = Color.SpringGreen,
                    SelectedColor = Color.Yellow,
                    Volume = VolumeDefs.DEFAULT_VOLUME,
                    ControllerId = 10, // pan
                    ControllerValue = 82
                };
                ctrl.ChannelChange += ChannelControl_ChannelChange;
                ctrl.SendMidi += Mgr_MessageSend;
                Controls.Add(ctrl);
                x += ctrl.Width + 4; // Width is not valid until after previous statement.
            });

            ///// 3 - do work

            // create all channels - script api calls like:
            // local hnd_keys = api.open_midi_output("loopMIDI Port 2", 1, "keys", inst.AcousticGrandPiano)
            // local hnd_synth = api.open_midi_output("loopMIDI Port 2", 3, "synth", inst.Lead1Square)
            // local hnd_ccin = api.open_midi_input("loopMIDI Port 1", 1, "my input")

            // call script api functions
            // api.send_midi_note(hnd_strings, note_num, volume)
            // api.send_midi_controller(hnd_synth, ctrl.Pan, 90)

            // callbacks from script
            // function receive_midi_note(chan_hnd, note_num, volume)
            // function receive_midi_controller(chan_hnd, controller, value)
        }

        //-------------------------------------------------------------------------------//
        /// <summary>A standard app where controls are defined in VS designer.</summary>
        void TestStandardApp()
        {
            // Create channels.
            var chan_out1 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "channel 1!", false);
            chan_out1.PatchName = "Harpsichord";

            var chan_out2 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 2, "channel 2!", false);
            chan_out2.PatchName = "Violin";

            // Init controls.
            ch_ctrl1.BorderStyle = BorderStyle.FixedSingle;
            ch_ctrl1.DrawColor = Color.SpringGreen;
            ch_ctrl1.SelectedColor = Color.Yellow;
            ch_ctrl1.Volume = VolumeDefs.DEFAULT_VOLUME;
            ch_ctrl1.ChannelChange += ChannelControl_ChannelChange;
            ch_ctrl1.SendMidi += ChannelControl_SendMidi;
            ch_ctrl1.BoundChannel = chan_out1;
            ch_ctrl1.ControllerValue = 64; // Sustain
            ch_ctrl1.ControllerValue = 45;
            var rend1 = new CustomRenderer() { ChannelNumber = chan_out1.ChannelNumber };
            rend1.SendMidi += ChannelControl_SendMidi;
            ch_ctrl1.UserRenderer = rend1;

            ch_ctrl2.BorderStyle = BorderStyle.FixedSingle;
            ch_ctrl2.DrawColor = Color.SpringGreen;
            ch_ctrl2.SelectedColor = Color.Yellow;
            ch_ctrl2.Volume = VolumeDefs.DEFAULT_VOLUME;
            ch_ctrl2.ChannelChange += ChannelControl_ChannelChange;
            ch_ctrl2.SendMidi += ChannelControl_SendMidi;
            ch_ctrl2.BoundChannel = chan_out1;
            ch_ctrl2.ControllerValue = 68; // Legato
            ch_ctrl2.ControllerValue = 90;
            var rend2 = new CustomRenderer() { ChannelNumber = chan_out2.ChannelNumber };
            rend2.SendMidi += ChannelControl_SendMidi;
            ch_ctrl2.UserRenderer = rend2;

            ///// 3 - do work
            // ...
        }

        //-------------------------------------------------------------------------------//
        void TestMusicTime() // TODO1 -> UT
        {
            var bt = new MusicTime("23.2.6");
            //UT_EQUAL(bt, 23 * MusicTime.SUBS_PER_BAR + 2 * MusicTime.SUBS_PER_BEAT + 6);
            Tell(INFO, $"bt [{bt}]");

            bt = new MusicTime("146.1");
            //UT_EQUAL(bt, 146 * MusicTime.SUBS_PER_BAR + 1 * MusicTime.SUBS_PER_BEAT);
            Tell(INFO, $"bt [{bt}]");

            bt = new MusicTime("71");
            //UT_EQUAL(bt, 71 * MusicTime.SUBS_PER_BAR);
            Tell(INFO, $"bt [{bt}]");

            bt = new MusicTime("49.55.8");
            //UT_EQUAL(bt, -1);
            Tell(INFO, $"bt [{bt}]");

            bt = new MusicTime("111.3.88");
            //UT_EQUAL(bt, -1);
            Tell(INFO, $"bt [{bt}]");

            bt = new MusicTime(12345);
            //UT_EQUAL(sbt, "385.3.1");
            Tell(INFO, $"bt [{bt}]");

            bt = new MusicTime("invalid");
            //UT_EQUAL(bt, -1);
            Tell(INFO, $"bt [{bt}]");
        }

        //----------------------------------------------------------------
        void TestConverter() // TODO1 -> UT
        {
            //UT_STOP_ON_FAIL(false);

            //////////////////////////////////////////////////////////
            // A unit test. If we use ppq of 8 (32nd notes):
            // 100 bpm = 800 ticks/min = 13.33 ticks/sec = 0.01333 ticks/msec = 75.0 msec/tick
            //  99 bpm = 792 ticks/min = 13.20 ticks/sec = 0.0132 ticks/msec  = 75.757 msec/tick

            MidiTimeConverter mt = new(0, 100);
            //UT_CLOSE(mt.InternalPeriod(), 75.0, 0.001);

            mt = new(0, 99);
            //UT_CLOSE(mt.InternalPeriod(), 75.757, 0.001);

            mt = new(384, 100);
            //UT_CLOSE(mt.MidiToSec(144000) / 60.0, 3.75, 0.001);

            mt = new(96, 100);
            //UT_CLOSE(mt.MidiPeriod(), 6.25, 0.001);
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
        void Mgr_MessageReceive(object? sender, BaseEvent e)
        {
            Tell(INFO, $"Receive [{e}]");
        }

        /// <summary>
        /// Something sent to a midi device. This is what was actually sent, not what the
        /// channel thought it was sending.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageSend(object? sender, BaseEvent e)
        {
            Tell(INFO, $"Send actual [{e}]");
        }

        /// <summary>
        /// UI clicked something -> send some midi. Works for different sources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SendMidi(object? sender, BaseEvent e)
        {
            var channel = sender switch
            {
                ChannelControl => (sender as ChannelControl)!.BoundChannel,
                CustomRenderer => MidiManager.Instance.GetOutputChannel((sender as CustomRenderer)!.ChannelNumber),
                _ => null // should never happen
            };

            if (channel is not null && channel.Enable)
            {
                Tell(INFO, $"Channel send [{e}]");
                channel.Device.Send(e);
            }
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
