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

        const string INDEV = "NebLoopback1";
        const string OUTDEV1 = "VirtualMIDISynth #1";
        const string OUTDEV2 = "Microsoft GS Wavetable Synth";
        #endregion


        //-------------------------------------------------------------------------------//
        public MainForm()
        {
            InitializeComponent();
            Text = "MidiLib Test";

            _outPath = MiscUtils.GetSourcePath();

            // The text output.
            tvInfo.Font = Font;
            tvInfo.WordWrap = true;
            tvInfo.Matchers =
            [
                new(ERROR, BgColor: Color.LightPink),
                new(WARN, BgColor: Color.Plum),
            ];

            // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            // TODO1 who handles volume adjust?
            // ChannelControl knows about solo/mute/volume
            // Channel or ChCtrl???
            // MainForm knows master-volume


            // Master volume. TODO1
            sldMasterVolume.DrawColor = Color.SpringGreen;
            sldMasterVolume.Minimum = 0.0;
            sldMasterVolume.Maximum = VolumeDefs.MAX_VOLUME;
            sldMasterVolume.Resolution = VolumeDefs.MAX_VOLUME / 50;
            sldMasterVolume.Value = VolumeDefs.DEFAULT_VOLUME;
            sldMasterVolume.Label = "master volume";

            timeBar.DrawColor = Color.Green;
            timeBar.SelectedColor = Color.LightYellow;
            timeBar.Snap = SnapType.Beat;
            timeBar.StateChange += (object? sender, TimeBar.StateChangeEventArgs e) => { Tell(INFO, $">>> TimeBar event. {timeBar.Current}"); };

            // Simple UI handlers.
            btnKillMidi.Click += (_, __) => { MidiManager.Instance.Kill(); };
            chkLoop.CheckedChanged += (_, __) => { timeBar.DoLoop = chkLoop.Checked; };
            btnRewind.Click += (_, __) => { timeBar.Rewind(); };

            btnGo.Click += Go_Click;
            btnTimebar.Click += (_, _) => TestTimeBar();
            btnProperty.Click += (_, _) => TestPropertyEditor();

            btnSend.Click += BtnSend_Click;

            // Report on midi action.
            MidiManager.Instance.MessageReceived += Mgr_MessageReceived;
            //MidiManager.Instance.MessageSent += Mgr_MessageSent;
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
                // Basic channels.
                var chan_out1 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "channel 1!", "Harpsichord");
                var chan_out2 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 2, "channel 2!", "Violin");

                // Init controls.
                ch_ctrl1.BorderStyle = BorderStyle.FixedSingle;
                ch_ctrl1.DrawColor = Color.SpringGreen;
                ch_ctrl1.SelectedColor = Color.Yellow;
                ch_ctrl1.Volume = VolumeDefs.DEFAULT_VOLUME; //TODO1 need to persist this or channel volume
                ch_ctrl1.ChannelChange += ChannelControl_ChannelChange;
                ch_ctrl1.BoundChannel = chan_out1;

                ch_ctrl2.BorderStyle = BorderStyle.FixedSingle;
                ch_ctrl2.DrawColor = Color.SpringGreen;
                ch_ctrl2.SelectedColor = Color.Yellow;
                ch_ctrl2.Volume = VolumeDefs.DEFAULT_VOLUME;
                ch_ctrl2.ChannelChange += ChannelControl_ChannelChange;
                ch_ctrl2.BoundChannel = chan_out1;
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
            MidiManager.Instance.Kill();

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();

            MidiManager.Instance.DestroyDevices();

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
                // App driven by a script - a la Nebulua/Nebulator. Creates channels and controls dynamically.

                //ch_ctrl1.Hide();
                //ch_ctrl2.Hide();

                // 1 - create all channels
                var chan_out1 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 1, "dyn keys", "HonkyTonkPiano");
                var chan_out2 = MidiManager.Instance.OpenOutputChannel(OUTDEV1, 10, "dyn drums", "Electronic");
                //var chan_out3 = Manager.Instance.OpenOutputChannel(OUTDEV1, 4, "dyn bass", "ElectricBassPick");
                var chan_in1 = MidiManager.Instance.OpenInputChannel(INDEV, 1, "dyn my input");

                // 2 - create a control for each channel and bind object
                int x = ch_ctrl2.Right + 10;
                int y = ch_ctrl2.Top;

                List<OutputChannel> channels = [chan_out1, chan_out2];
                channels.ForEach(chan =>
                {
                    var ctrl = new ChannelControl()
                    {
                        Name = $"Control for {chan.ChannelName}",
                        BoundChannel = chan,
                        Location = new(x, y),
                        BorderStyle = BorderStyle.FixedSingle,
                        DrawColor = Color.SpringGreen,
                        SelectedColor = Color.Yellow,
                        Volume = VolumeDefs.DEFAULT_VOLUME,
                    };
                    ctrl.ChannelChange += ChannelControl_ChannelChange;
                    Controls.Add(ctrl);
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
            if (btnSend.Checked)
            {
                // Run timer.
                timer1.Interval = 500;
                _count = 3;
                Random rand = new();
                void Timer1_Tick(object? sender, EventArgs e)
                {
                    int noteNum = rand.Next(40, 60);
                    //chan_out1.Send(new NoteOn()); // TODO1
                    //ch_ctrl1.BoundChannel.Send(new NoteOn())

                    if (--_count <= 0)
                    {
                        timer1.Stop();
                        timer1.Tick -= Timer1_Tick;
                    }
                }
                timer1.Tick += Timer1_Tick;
                timer1.Start();
            }
            else
            {
            }
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
            _count = 1000;
            void Timer1_Tick(object? sender, EventArgs e)
            {
                timeBar.Increment();
                if (--_count <= 0)
                {
                    timer1.Stop();
                    timer1.Tick -= Timer1_Tick;
                }
            }
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }

        //-------------------- Test property editing ------------------------------------//
        void TestPropertyEditor()
        {
            Tell(INFO, $">>> Property editor.");

            SettingsTestClass td = new()
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
        [Serializable]
        public class SettingsTestClass
        {
            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            public string DeviceName { get; set; } = "";

            public string ChannelName { get; set; } = "";

            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            public int ChannelNumber { get; set; } = 1;

            [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
            public int SomeOtherMidi { get; set; } = 0;

            [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
            public string AliasFile { get; set; } = "";

            [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
            [TypeConverter(typeof(GenericConverter))]
            public int Patch { get; set; } = 0;
        }

        //----------------------- Events ------------------------------------------------//
        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageReceived(object? sender, BaseEvent e)
        {
            Tell(INFO, $"Mgr Received [{e}]");
        }

        ///// <summary>
        ///// Something sent to a midi device.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //void Mgr_MessageSent(object? sender, BaseEvent e)
        //{
        //    Tell(INFO, $"Mgr Sent [{e}]");
        //}



        ///////////////////////////////////////////////////////////////////////////////////
        ///////////// TODO2 Stuff host needs to implement /////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////

        /// <summary>User clicked something -> configure channels.</summary>
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

            if (e.Info)
            {
                //TODO2 anything?
            }
        }

        void SendNote(int channelNumber, int note, double volume) // , MusicTime? when = null)
        {
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"Note:{note}"); }
            var channel = MidiManager.Instance.GetOutputChannel(channelNumber) ?? throw new ArgumentException($"chan_hnd:{channelNumber}");

            if (volume == 0.0)
            {
                channel.Send(new NoteOff(channel.ChannelNumber, note));
            }
            else
            {
                var vel = (int)MathUtils.Constrain(volume * sldMasterVolume.Value * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI);
                channel.Send(new NoteOn(channel.ChannelNumber, note, vel));
            }

            //if (_settings.MonitorSend)
            //{
            //    // Intended sent.
            //    _loggerMidi.Trace($">>> {e}");
            //}
        }

        void SendController(int channelNumber, int controller, int value) // , MusicTime? when = null)
        {
            if (controller is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"controller:{controller}"); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"value:{value}"); }
            var channel = MidiManager.Instance.GetOutputChannel(channelNumber) ?? throw new ArgumentException($"chan_hnd:{channelNumber}");

            var se = new Controller(channel.ChannelNumber, controller, value);
            channel.Send(se);

            //if (_settings.MonitorSend)
            //{
            //    // Intended sent.
            //    _loggerMidi.Trace($">>> {e}");
            //}
        }
        ///////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////




        //----------------------- Misc --------------------------------------------------//
        void Tell(string cat, string s, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            var fn = Path.GetFileName(file);
            tvInfo.Append($"{cat} {fn}({line}) {s}");
        }
    }
}
