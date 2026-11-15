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
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib.Test
{

    #region Types
    // Aliased instrument names.
    public enum KustomInstrumentId { AmbientWind = 0, AmbientWind2 = 1, WaterWhistle1 = 18, OctaveStringPad = 27 }
    #endregion

    public partial class MainForm : Form
    {
        #region Fields
        TestSettings _settings = new();

        readonly ChannelManager CM;

        readonly List<MidiOutput> _outputs = [];

        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";

        // Device names - to taste
        public const string INDEV = "NebLoopback1";
        public const string OUTDEV1 = "VirtualMIDISynth #1";
        public const string OUTDEV2 = "Microsoft GS Wavetable Synth";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Hello.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            CM = new(Controls);

            Text = "MidiLib ¯\\_(^^)_/¯";

            // Init fake settings.
            MakeSomeSettings();

            // The text output.
            tvInfo.Font = new("Cascadia Mono", 9);
            tvInfo.WordWrap = true;
            tvInfo.Matchers =
            [
                new(ERROR, BgColor: Color.LightPink),
                new(WARN, BgColor: Color.Plum),
            ];

            // Master volume.
            sldMasterVolume.DrawColor = _settings.DrawColor;
            sldMasterVolume.Minimum = 0.0;
            sldMasterVolume.Maximum = MidiLibDefs.MAX_VOLUME;
            sldMasterVolume.Resolution = MidiLibDefs.RES_VOLUME;
            sldMasterVolume.Value = _settings.MasterVolume;
            sldMasterVolume.Label = "master volume";

            timeBar.DrawColor = _settings.DrawColor;
            timeBar.SelectedColor = _settings.SelectedColor;
            timeBar.Snap = SnapType.Beat;
            timeBar.StateChange += (sender, e) => { Tell(INFO, $">>> TimeBar event. {timeBar.Current}"); };

            // UI handlers.
            btnKillMidi.Click += (_, __) => { CM.KillAll(); };
            chkLoop.CheckedChanged += (_, __) => { timeBar.DoLoop = chkLoop.Checked; };
            btnRewind.Click += (_, __) => { timeBar.Rewind(); };

            btnGo.Click += Go_Click; ;
            btnTimebar.Click += (_, _) => TestTimeBar();
            btnProperty.Click += (_, _) => ChangePatch();

            btnSend.Click += (_, _) => TestSend();

            // Hook up manager notifications.
            CM.MessageReceived += Mgr_MessageReceived;
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
                BuildStatic();

                BuildDynamic();
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
            if (disposing && (components is not null))
            {
                CM.Dispose();
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// Debug stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Go_Click(object? sender, EventArgs e)
        {
            // CM.UpdateState();
            // var ls = CM.DumpState();
            // ls.ForEach(l => Tell(INFO, l));
        }

        /// <summary>
        /// For the UI.
        /// </summary>
        /// <param name="mout"></param>
        /// <param name="ctrl"></param>
        /// <param name="patch"></param>
        void UpdateInfo(MidiOutput mout, ChannelControl ctrl, Enum? patch = null)
        {
            ctrl.Info1 = $"M{mout.ChannelNumber} {mout.ChannelName}";
            ctrl.Info2 = patch is null ?
                $"No patch{Environment.NewLine}{mout}" :
                $"{patch} ({Convert.ToInt32(patch)}){Environment.NewLine}{mout}";
        }

        /// <summary>
        /// Configure channels defined in designer.
        /// </summary>
        void BuildStatic()
        {
            // Init system - Keys
            var mout = CM.OpenMidiOutput(_settings.KeysDeviceName, _settings.KeysChannelNumber, _settings.KeysChannelName);
            // Update system.
            CM.Bind(mout, cc1);
            _outputs.Add(mout);

            // Init controls. Typically voloume and state would be persisted per output.
            cc1.Volume = 0.8;
            cc1.State = ChannelState.Normal;
            cc1.DrawColor = _settings.DrawColor;
            cc1.SelectedColor = _settings.SelectedColor;
            cc1.InfoClick += ChannelControl_InfoClick;
            // Update UI.
            UpdateInfo(mout, cc1, _settings.KeysPatch);

            // Init system - Drums
            mout = CM.OpenMidiOutput(_settings.DrumDeviceName, _settings.DrumChannelNumber, _settings.DrumChannelName);
            CM.Bind(mout, cc2);
            _outputs.Add(mout);

            // Init controls.
            cc2.Volume = 0.72;
            cc2.State = ChannelState.Normal; // ChannelState.Solo;
            cc2.DrawColor = _settings.DrawColor;
            cc2.SelectedColor = _settings.SelectedColor;
            cc2.InfoClick += ChannelControl_InfoClick;
            UpdateInfo(mout, cc2, _settings.DrumKitPatch);
        }

        /// <summary>
        /// Create channels and controls dynamically.
        /// </summary>
        void BuildDynamic()
        {
            Random rand = new();
            int x = cc2.Right + 10;
            int y = cc2.Top;

            var mout = CM.OpenMidiOutput(OUTDEV1, 3, "dyn keys");
            CreateControl(mout, MidiDefs.InstrumentId.HonkyTonkPiano);

            // Custom.
            mout = CM.OpenMidiOutput(OUTDEV1, 4, "dyn bass");
            CreateControl(mout, KustomInstrumentId.OctaveStringPad);

            // Drum kit.
            mout = CM.OpenMidiOutput(OUTDEV1, 10, "dyn drums");
            CreateControl(mout, MidiDefs.DrumKitId.Orchestra);

            // Input.
            var inchan = CM.OpenMidiInput(INDEV, 1, "dyn input"); // TODO1 test midi input

            // Ceate a control for each channel and bind object.
            void CreateControl(MidiOutput mout, Enum patch)
            {
                var ctrl = new ChannelControl()
                {
                    // Name = chan.ChannelName,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    DrawColor = _settings.DrawColor,
                    SelectedColor = _settings.SelectedColor,
                    Volume = rand.NextDouble() + 0.5,
                    State = ChannelState.Normal
                };
                ctrl.InfoClick += ChannelControl_InfoClick;

                CM.Bind(mout, ctrl);
                CM.SetPatch(mout, patch);
                UpdateInfo(mout, ctrl, patch);

                _outputs.Add(mout);

                x += ctrl.Width + 10; // Width is not valid until after previous statement.
            }
        }

        #region Test code
        /// <summary>
        /// Send some test stuff.
        /// </summary>
        void TestSend()
        {
            // Run timer.
            timer1.Interval = 50;
            int tickCount = 100;
            Random rand = new();

            void Timer1_Tick(object? sender, EventArgs e)
            {
                int pace = rand.Next(0, 5);

                if (pace == 0)
                {
                    int devind = rand.Next(0, _outputs.Count);
                    int noteNum = rand.Next(40, 70);
                    var dev = _outputs[devind];
                    SendNote(dev, cc1, noteNum, 0.5);
                }

                if (--tickCount <= 0)
                {
                    timer1.Stop();
                    timer1.Tick -= Timer1_Tick;
                    Tell(INFO, "TestSend() done");
                }
            }
            timer1.Tick += Timer1_Tick;
            Tell(INFO, "TestSend() start");
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
        #endregion

        #region Stuff the real host needs to implement
        /// <summary>
        /// A typical application config.
        /// </summary>
        public void MakeSomeSettings()
        {
            // Init fake settings.
            _settings = new()
            {
                MasterVolume = 0.77,
                DrawColor = Color.SpringGreen,
                SelectedColor = Color.Magenta,

                InputChannelName = "stat input",
                InputChannelNumber = 1,
                InputDeviceName = INDEV,

                KeysChannelName = "stat keys",
                KeysChannelNumber = 2,
                KeysPatch = MidiDefs.InstrumentId.Harpsichord,
                KeysDeviceName = OUTDEV1,

                DrumChannelName = "stat drums",
                DrumChannelNumber = 10,
                DrumKitPatch = MidiDefs.DrumKitId.Orchestra,
                DrumDeviceName = OUTDEV1,
            };
        }

        /// <summary>
        /// Send midi note.
        /// </summary>
        /// <param name="mout"></param>
        /// <param name="ctrl">Only needed for channel volume.</param>
        /// <param name="note"></param>
        /// <param name="volume"></param>
        /// <exception cref="ArgumentException"></exception>
        void SendNote(MidiOutput mout, ChannelControl ctrl, int note, double volume)
        {
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"Note:{note}"); }

            if (volume == 0.0)
            {
                mout.Send(new NoteOff(mout.ChannelNumber, note));
            }
            else
            {
                var vol = volume * sldMasterVolume.Value * ctrl.Volume;
                var vel = (int)MathUtils.Constrain(vol * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI);
                var se = new NoteOn(mout.ChannelNumber, note, vel);
                mout.Send(se);
                //Tell($">>>", se.ToString());
            }
        }

        /// <summary>
        /// Send midi controller.
        /// </summary>
        /// <param name="mout"></param>
        /// <param name="controller"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        void SendController(MidiOutput mout, MidiDefs.ControllerId controller, int value)
        {
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"value:{value}"); }

            // ?? if (channel.Enable)
            var se = new Controller(mout.ChannelNumber, controller, value);
            mout.Send(se);
            //Tell($">>>", se.ToString());
        }

        /// <summary>
        /// Something arrived from a midi device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageReceived(object? sender, BaseEvent e)
        {
            var mout = CM.GetMidiOutput(e.ChannelNumber);
            if (mout is not null)
            {
                Tell(INFO, $"Mgr Received [{e}] on [{mout}]");
            }
            else
            {
                throw new MidiLibException("Unknown/null channel");
            }
        }

        /// <summary>
        /// Change patch using property editing
        /// </summary>
        void ChangePatch()
        {
            var changes = SettingsEditor.Edit(_settings, "TESTOMATIC", 300);

            changes.ForEach(ch =>
            {
                if (ch.name == "KeysPatch")
                {
                    // Update system.
                    CM.SetPatch(_outputs[0], _settings.KeysPatch);
                    // Update UI.
                    UpdateInfo(_outputs[0], cc1, _settings.KeysPatch);
                }

                if (ch.name == "DrumKitPatch")
                {
                    // Update system.
                    CM.SetPatch(_outputs[1], _settings.DrumKitPatch);
                    // Update UI.
                    UpdateInfo(_outputs[1], cc2, _settings.DrumKitPatch);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_InfoClick(object? sender, ChannelControl.InfoClickEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Infrastrucutre
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
    }
}
