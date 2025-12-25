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
using System.ComponentModel.DataAnnotations;
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

        /// <summary>The boss.</summary>
        readonly Manager _mgr = new();

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
            sldMasterVolume.Maximum = Defs.MAX_VOLUME;
            sldMasterVolume.Resolution = Defs.MAX_VOLUME / 50;
            sldMasterVolume.Value = Defs.DEFAULT_VOLUME;
            sldMasterVolume.Label = "master volume";

            timeBar.ControlColor = Color.Green;
            timeBar.SelectedColor = Color.LightYellow;
            timeBar.Snap = SnapType.Beat;
            timeBar.StateChange += TimeBar_StateChange;

            // Simple UI handlers.
            btnKillMidi.Click += (_, __) => { _mgr.Kill(); };
            chkLoop.CheckedChanged += (_, __) => { timeBar.DoLoop = chkLoop.Checked; };
            btnRewind.Click += (_, __) => { timeBar.Rewind(); };

            btnGo.Click += Go_Click;
            btnGen.Click += Gen_Click;

            //_mgr.MessageReceive += Mgr_MessageReceive;
            //_mgr.MessageSend += Mgr_MessageSend;
        }

        /// <summary>
        /// Window is set up now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            DestroyControls();
            _mgr.DestroyDevices();

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
            Tell(INFO, $">>>>> Go start.");

            TestScriptApp();

            TestDefFile();

            TestPropertyEditor();

            TestChannel();

            TestTimeBar();

            Tell(INFO, $">>>>> Go end.");
        }
        #endregion

        //-------------------------------------------------------------------------------//
        /// <summary>Test gen aux files.</summary>
        void Gen_Click(object? sender, EventArgs e)
        {
            Tell(INFO, $">>>>> Gen start.");
            var myPath = MiscUtils.GetSourcePath();
            string fnIni = Path.Combine(myPath, "..", "gm_defs.ini");

            Tell(INFO, $">>>>> Gen Markdown.");
            var smd = MidiDefs.Instance.GenMarkdown(fnIni);
            var fnOut = Path.Join(myPath, "out", "midi_defs.md");
            File.WriteAllText(fnOut, smd);

            Tell(INFO, $">>>>> Gen Lua.");
            var sld = MidiDefs.Instance.GenLua(fnIni);
            fnOut = Path.Join(myPath, "out", "midi_defs.lua");
            File.WriteAllText(fnOut, sld);

            Tell(INFO, $">>>>> Gen end.");
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
        void TestChannel()
        {
            Tell(INFO, $">>>>> Channel.");

            // Dummy channel.
            var dev = new NullOutputDevice("DUMMY_DEVICE");

            OutputChannel ch = new(dev, 3)
            {
                ChannelName = "booga-booga",
                ControllerId = 45,
                ControllerValue = 60
            };

            // Aliases.
            var myPath = MiscUtils.GetSourcePath();
            ch.AliasFile = Path.Combine(myPath, "test_defs.ini");

            if (ch.GetPatchName(40) != "SynthGuitar1") Tell(ERROR, "FAIL");
            if (ch.GetPatchName(101) != "INST_101") Tell(ERROR, "FAIL");

            // should send midi
            ch.Patch = 77;
            if (dev.CollectedEvents.Count != 1) Tell(ERROR, "FAIL");

            Tell(INFO, "DONE");
        }

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
            //var insts = MidiDefs.Instance.GetDefaultInstrumentDefs();
            //IEnumerable<string> orderedValues = insts.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
            //var instsList = orderedValues.ToList();

            Dictionary<int, string> vals = [];
            Enumerable.Range(0, MidiDefs.MAX_MIDI + 1).ForEach(i => vals.Add(i, MidiDefs.Instance.GetInstrumentName(i)));

            var instList = CreateOrderedMidiList(vals, true, true);

            GenericListTypeEditor.SetOptions("DeviceName", MidiOutputDevice.GetAvailableDevices());
            GenericListTypeEditor.SetOptions("Patch", instList);

            var changes = SettingsEditor.Edit(td, "TESTOMATIC", 300);
            changes.ForEach(s => Tell(INFO, $"Editor changed {s}"));
        }

        //-------------------------------------------------------------------------------//
        /// <summary>Test def file loading etc.</summary>
        void TestDefFile()
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
            var chan_out1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "keys", 0);
            var chan_out2 = _mgr.OpenMidiOutput(OUTDEV1, 4, "bass", 32);
            //var chan_out2 = _mgr.OpenMidiOutput(OUTDEV1, 10, "drums", 32);
            var chan_in1 = _mgr.OpenMidiInput(INDEV, 1, "my input");

            ///// 2 - create a control for each channel and bind object
            int x = sldMasterVolume.Left;
            int y = sldMasterVolume.Bottom + 10;

            List<OutputChannel> channels = [chan_out1, chan_out2];
            channels.ForEach(chan =>
            {
                var rend = new CustomRenderer() { ChannelHandle = chan.Handle };
                rend.SendMidi += ChannelControl_SendMidi;

                var ctrl = new ChannelControl()
                {
                    //                    Name = $"Control for {chan.Config.ChannelName}",
                    BoundChannel = chan,
                    UserRenderer = rend,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    ControlColor = Color.SpringGreen,
                    SelectedColor = Color.Yellow,
                    Volume = Defs.DEFAULT_VOLUME,
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
            // Create channels and initialize controls.
            var chan_out1 = _mgr.OpenMidiOutput(OUTDEV1, 1, "channel 1!", 0);
            var chan_out2 = _mgr.OpenMidiOutput(OUTDEV1, 2, "channel 2!", 12);

            List<(OutputChannel, ChannelControl)> channels = [(chan_out1, ch_ctrl1), (chan_out2, ch_ctrl2)];
            channels.ForEach(ch =>
            {
                ch.Item2.BorderStyle = BorderStyle.FixedSingle;
                ch.Item2.ControlColor = Color.SpringGreen;
                ch.Item2.SelectedColor = Color.Yellow;
                ch.Item2.Volume = Defs.DEFAULT_VOLUME;
                ch.Item2.ChannelChange += ChannelControl_ChannelChange;
                ch.Item2.SendMidi += ChannelControl_SendMidi;
                ch.Item2.BoundChannel = ch.Item1;

                var rend = new CustomRenderer() { ChannelHandle = ch.Item1.Handle };
                rend.SendMidi += ChannelControl_SendMidi;
                //TODO2 ideally hide this event chain in the ChannelControl itself. Prob need to add an interface for renderers.
                ch.Item2.UserRenderer = new CustomRenderer() { ChannelHandle = ch.Item1.Handle };
            });

            ///// 3 - do work
            // ...
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
            [Range(1, MidiDefs.NUM_CHANNELS)]
            public int ChannelNumber { get; set; } = 1;

            /// <summary>Actual 1-based midi channel number.</summary>
            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            [Range(0, MidiDefs.MAX_MIDI)]
            public int SomeOtherMidi { get; set; } = 0;

            /// <summary>Override default instrument presets.</summary>
            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string AliasFile { get; set; } = "";

            /// <summary>Current instrument/patch number.</summary>
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(GenericConverter))]
            [Range(0, MidiDefs.MAX_MIDI)]
            public int Patch { get; set; } = 0;
        }


        #region Script api functions TODO2???? Nebulua?

        // /// api.send_midi_note(hnd_strings, note_num, volume)
        // void SendMidiNote(int chnd, int note_num, double volume)
        // {
        //     if (note_num is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note_num)); }

        //     var ch = _mgr.GetOutputChannel(chnd);

        //     if (ch is not null)
        //     {
        //         BaseMidiEvent evt = volume == 0.0 ?
        //             new NoteOff(ChannelHandle.ChannelNumber(chnd), note_num) :
        //             new NoteOn(ChannelHandle.ChannelNumber(chnd), note_num, (int)MathUtils.Constrain(volume * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI));
        //         ch.Device.Send(evt);
        //     }
        //     else
        //     {
        //         // error?
        //     }
        // }

        // /// api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
        // void SendMidiController(int chnd, int controller_id, int value)
        // {
        //     if (controller_id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controller_id)); }
        //     if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }
        //     var ch = _mgr.GetOutputChannel(chnd);
        //     if (ch is not null)
        //     {
        //         BaseMidiEvent evt = new Controller(ChannelHandle.ChannelNumber(chnd), controller_id, value);
        //         ch.Device.Send(evt);
        //     }
        //     else
        //     {
        //         // error?
        //     }
        // }

        // /// Callback from script: function receive_midi_note(chan_hnd, note_num, volume)
        // void ReceiveMidiNote(int chnd, int note_num, double volume)
        // {
        // }

        // /// Callback from script: function receive_midi_controller(chan_hnd, controller, value)
        // void ReceiveMidiController(int chnd, int controller_id, int value)
        // {
        // }
        #endregion

        #region Events
        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageReceive(object? sender, BaseMidiEvent e)
        {
            Tell(INFO, $"Receive [{e}]");
        }

        /// <summary>
        /// Something sent to a midi device. This is what was actually sent, not what the
        /// channel thought it was sending.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageSend(object? sender, BaseMidiEvent e)
        {
            Tell(INFO, $"Send actual [{e}]");
        }

        /// <summary>
        /// UI clicked something -> send some midi. Works for different sources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SendMidi(object? sender, BaseMidiEvent e)
        {
            var channel = sender switch
            {
                ChannelControl => (sender as ChannelControl)!.BoundChannel,
                CustomRenderer => _mgr.GetOutputChannel((sender as CustomRenderer)!.ChannelHandle),
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
        void ChannelControl_ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

            if (e.StateChange)
            {
                Tell(INFO, $"StateChange");

                // Update all channels.
                bool anySolo = _channelControls.Where(c => c.State == ChannelControl.ChannelState.Solo).Any();

                foreach (var cciter in _channelControls)
                {
                    bool enable = anySolo ?
                        cciter.State == ChannelControl.ChannelState.Solo :
                        cciter.State != ChannelControl.ChannelState.Mute;

                    channel.Enable = enable;
                    if (!enable)
                    {
                        // Kill just in case.
                        _mgr.Kill(channel);
                    }
                }
            }

            //if (e.PatchChange)
            //{
            //    Tell(INFO, $"PatchChange [{channel.Patch}]");
            //    channel.Device.Send(new Patch(channel.ChannelNumber, channel.Patch));
            //}

            //if (e.AliasFileChange)
            //{
            //    Tell(INFO, $"AliasFileChange [{channel.AliasFile}]");
            //}
        }

        #endregion

        #region Misc internals
        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            _mgr.Kill();

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

        /// <summary>
        /// Convert a midi dictionary into ordered list of strings.
        /// </summary>
        /// <param name="source">The dictionary to process</param>
        /// <param name="addKey">Add the index number to the entry</param>
        /// <param name="fill">Add mising midi values</param>
        /// <returns></returns>
        public List<string> CreateOrderedMidiList(Dictionary<int, string> source, bool addKey, bool fill)
        {
            List<string> res = [];

            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                if (source.ContainsKey(i))
                {
                    res.Add(addKey ? $"{i:000} {source[i]}" : $"{source[i]}");
                }
                else if (fill)
                {
                    res.Add($"{i:000}");
                }
            }

            return res;
        }
        #endregion
    }
}
