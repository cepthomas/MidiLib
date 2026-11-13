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
    public partial class MainForm : Form
    {
        #region Fields
        TestSettings _settings = new();

        readonly ChannelManager CM;

        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";

        ///// Device names - YMMV /////
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
            DoPersistence();

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
            btnProperty.Click += (_, _) => TestPropertyEditor();

            btnSend.Click += (_, _) => TestSend();
            btnDyn.Click += (_, _) => TestDynamic();

            // Hook up manager notifications.
            CM.MessageReceived += Mgr_MessageReceived;
            //CM.MessageSent += Mgr_MessageSent;
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
                // Configure channels defined in designer.
                InitOneChannel(_settings.OutputConfigs[0], cc1);
                InitOneChannel(_settings.OutputConfigs[1], cc2);

                void InitOneChannel(OutputConfig config, ChannelControl ctrl)
                {
                    var chan = CM.OpenMidiOutput(config.DeviceName, config.ChannelNumber, config.ChannelName, config.Patch);
                    CM.Bind(chan, ctrl);

                    // Init control.
                    ctrl.Volume = config.Volume;
                    ctrl.State = config.State;
                    ctrl.DrawColor = _settings.DrawColor;
                    ctrl.SelectedColor = _settings.SelectedColor;
                    ctrl.Info1 = $"M{config.ChannelNumber} {config.ChannelName}";
                    ctrl.Info2 = $"{chan}";
                    ctrl.InfoClick += ChannelControl_InfoClick;
                }
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
            CM.UpdateState();

            var ls = CM.DumpState();
            ls.ForEach(l => Tell(INFO, l));
        }

        #region Test code
        /// <summary>
        /// Create channels and controls dynamically.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TestDynamic()
        {
            Tell(INFO, $">>> Go start.");

            // Play with aliases.
            string aliasFile = @"C:\Dev\Libs\MidiLib\Test\test_defs.ini";
            var (insts, ctrls, drums, kits) = LoadAliases(aliasFile);

            try
            {
                Random rand = new();
                int x = cc2.Right + 10;
                int y = cc2.Top;

                var id = MidiDefs.Instruments.GetId("HonkyTonkPiano") ?? -1;
                var mout = CM.OpenMidiOutput(OUTDEV1, 3, "dyn keys", id);
                CreateControl(mout);

                id = insts!.GetId("GlurbleVox") ?? -1;
                mout = CM.OpenMidiOutput(OUTDEV1, 4, "dyn bass", id, false, aliasFile);
                CreateControl(mout);

                id = MidiDefs.DrumKits.GetId("Electronic") ?? -1;
                mout = CM.OpenMidiOutput(OUTDEV1, 10, "dyn drums", id, true);
                CreateControl(mout);

                var inchan = CM.OpenMidiInput(INDEV, 1, "dyn my input"); // TODO1 test midi input

                // Ceate a control for each channel and bind object.
                void CreateControl(MidiOutput mout)
                {
                    var ctrl = new ChannelControl()
                    {
                        // Name = chan.ChannelName,
                        Info1 = $"M{mout.ChannelNumber} {mout.ChannelName}",
                        Info2 = $"{mout.PatchName} ({mout.Patch}){Environment.NewLine}{mout}",
                        Location = new(x, y),
                        BorderStyle = BorderStyle.FixedSingle,
                        DrawColor = _settings.DrawColor,
                        SelectedColor = _settings.SelectedColor,
                        Volume = rand.NextDouble() + 0.5,
                        State = ChannelState.Normal
                    };
                    ctrl.InfoClick += ChannelControl_InfoClick;

                    CM.Bind(mout, ctrl);

                    x += ctrl.Width + 10; // Width is not valid until after previous statement.
                }
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
        void TestSend()
        {
            // Run timer.
            timer1.Interval = 500;
            int count = 15;
            Random rand = new();
            void Timer1_Tick(object? sender, EventArgs e)
            {
                int noteNum = rand.Next(40, 60);
                var mout = CM.GetMidiOutput(cc1);
                SendNote(mout!, cc1, noteNum, 0.5);

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
        void TestPropertyEditor()
        {
            // Set up options.
            List<string> inames = [];
            var mout = CM.GetMidiOutput(1);
            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                inames.Add(mout!.GetInstrumentName(i) ?? $"inst{i}");
            }

// TODO1 fee names to editor??

            var changes = SettingsEditor.Edit(_settings, "TESTOMATIC", 300);

            changes.ForEach(s => Tell(INFO, $"Editor changed {s}"));
        }
        #endregion

        #region Stuff the real host needs to implement
        /// <summary>
        /// A typical application config and persistance model.
        /// </summary>
        public void DoPersistence()
        {
            // Init fake settings.
            _settings = new()
            {
                InputConfigs = [],
                OutputConfigs = [],
                MasterVolume = 0.77,
                DrawColor = Color.SpringGreen,
                SelectedColor = Color.Magenta,
            };

            _settings.InputConfigs.Add(new()
            {
                ChannelNumber = 8,
                ChannelName = "Some input",
                DeviceName = INDEV
            });

            _settings.OutputConfigs.Add(new()
            {
                ChannelNumber = 1,
                ChannelName = "Output 1!",
                DeviceName = OUTDEV1,
                Patch = 6,
                //PatchName = "Harpsichord",
                Volume = 0.67,
                State = ChannelState.Normal
            });

            _settings.OutputConfigs.Add(new()
            {
                ChannelNumber = 2,
                ChannelName = "Output 2!",
                DeviceName = OUTDEV2,
                Patch = 40,
                //PatchName = "Violin",
                Volume = 1.19,
                State = ChannelState.Mute
            });
        }

        /// <summary>
        /// Send midi note.
        /// </summary>
        /// <param name="mout"></param>
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
        void SendController(MidiOutput mout, int controller, int value)
        {
            if (controller is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"controller:{controller}"); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"value:{value}"); }

            // ?? if (channel.Enable)
            var se = new Controller(mout.ChannelNumber, controller, value);
            mout.Send(se);
            Tell($">>>", se.ToString());
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
        /// Load up stuff.
        /// </summary>
        /// <param name="aliasFile"></param>
        (BiLut? insts, BiLut? ctrls, BiLut? drums, BiLut? kits) LoadAliases(string aliasFile)
        {
            var ir = new IniReader();
            ir.ParseFile(aliasFile);

            var defs = MidiDefs.ReadDefs(ir, "instruments");
            var _instruments = defs;

            defs = MidiDefs.ReadDefs(ir, "controllers");
            var _controllers = defs;

            defs = MidiDefs.ReadDefs(ir, "drums");
            var _drums = defs;

            defs = MidiDefs.ReadDefs(ir, "drumkits");
            var _drumKits = defs;

            return (_instruments, _controllers, _drums, _drumKits);
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

    }
}
