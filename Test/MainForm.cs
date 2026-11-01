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



// namespace Ephemera.NBagOfUis
//     public class Slider : UserControl // TODO1 log/db for volume use.
//     public class Meter : UserControl
//         public enum MeterType { Linear, Log, ContinuousLine, ContinuousDots }  // TODO1 Add damped/vu, peak/hold

// >>> C:\Dev\Third\NAudio3\src\NAudio.WinForms\Gui\VolumeMeter.cs
// >>> C:\Dev\Third\NAudio3\src\NAudio.WinForms\Gui\VolumeSlider.cs


// C:\Dev\Apps\Midifrier\MainForm.cs:
// C:\Dev\Apps\MidiGenerator\MainForm.Designer.cs:
// C:\Dev\Apps\Nebulator\App\MainForm.cs:
// C:\Dev\Apps\Nebulua\MainForm.cs:


// TODO1 options:
// def: volume/level, info, Solo/Mute
// all: meters, fx, pan




namespace Ephemera.MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Fields
        const string ERROR = "ERR";
        const string WARN = "WRN";
        const string INFO = "INF";

        //const string INDEV = "NebLoopback1";
        //const string OUTDEV1 = "VirtualMIDISynth #1";
        //const string OUTDEV2 = "Microsoft GS Wavetable Synth";

        // Some shorthand
        readonly ChannelManager CM = ChannelManager.Instance;

        TestSettings _settings;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Hello.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            Text = "MidiLib ¯\\_(^^)_/¯";

            // Init fake settings.
            _settings = TestSettings.GenFake();

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
            sldMasterVolume.Value = _settings.MasterVolume;
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
                // Configure channels defined in designer.
                {
                    var opts = _settings.OutputChannels[0];
                    var ctrl = ch_ctrl1;

                    var chan = CM.OpenOutputChannel(opts.DeviceName, opts.ChannelNumber, opts.PatchName);
                    CM.Bind(chan, ctrl);
                    // Init control.
                    ctrl.Name = opts.ChannelName;
                    ctrl.Volume = opts.Volume;
                    ctrl.State = opts.State;
                    ctrl.DrawColor = _settings.DrawColor;
                    ctrl.SelectedColor = _settings.SelectedColor;
                    //ctrl.Pan = outs.Pan; TODO1
                    //ctrl.??? = outs.AliasFile; TODO1
                    //ctrl.Options = _settings.Options;
                    ctrl.ChannelChange += ChannelControl_ChannelChange;
                }

                {
                    var opts = _settings.OutputChannels[1];
                    var ctrl = ch_ctrl2;

                    var chan = CM.OpenOutputChannel(opts.DeviceName, opts.ChannelNumber, opts.PatchName);
                    CM.Bind(chan, ctrl);
                    // Init control.
                    ctrl.Name = opts.ChannelName;
                    ctrl.Volume = opts.Volume;
                    ctrl.State = opts.State;
                    ctrl.DrawColor = _settings.DrawColor;
                    ctrl.SelectedColor = _settings.SelectedColor;
                    //ctrl.Pan = outs.Pan; TODO1
                    //ctrl.??? = outs.AliasFile; TODO1
                    //ctrl.Options = _settings.Options;
                    ctrl.ChannelChange += ChannelControl_ChannelChange;
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
                //List<OutputChannel> channels = [];
                Random rand = new();
                int x = ch_ctrl2.Right + 10;
                int y = ch_ctrl2.Top;

                var chan = CM.OpenOutputChannel(TestSettings.OUTDEV1, 3, "HonkyTonkPiano");
                //channels.Add(chan);
                AddControl(chan, "dyn keys");

                chan = CM.OpenOutputChannel(TestSettings.OUTDEV1, 4, "KUSTOM", @"C:\Dev\Libs\MidiLib\Test\test_defs.ini");
                //channels.Add(chan);
                AddControl(chan, "dyn bass");

                chan = CM.OpenOutputChannel(TestSettings.OUTDEV1, 10, "Electronic");
                //channels.Add(chan);
                AddControl(chan, "dyn drums");

                var inchan = CM.OpenInputChannel(TestSettings.INDEV, 1); // TODO1 test this. name="dyn my input"

                // Ceate a control for each channel and bind object.
                void AddControl(OutputChannel chan, string channelName)
                {
                    var sui = $"Ch: {chan.ChannelNumber} {channelName}";
                    var stt = $"{chan.PatchName} {chan.Patch:000}"; //{Environment.NewLine}"Dev {chan.DeviceInfo}";

                    var ctrl = new ChannelControl()
                    {
                        Name = channelName,
                        Info = (sui, stt),
                        Location = new(x, y),
                        BorderStyle = BorderStyle.FixedSingle,
                        DrawColor = _settings.DrawColor,
                        SelectedColor = _settings.SelectedColor,
                        Volume = rand.NextDouble() + 0.5,
                    };
                    ctrl.ChannelChange += ChannelControl_ChannelChange;
                    Controls.Add(ctrl);

                    CM.Bind(chan, ctrl);

                    x += ctrl.Width + 10; // Width is not valid until after previous statement.
                }

                //channels.ForEach(chan => AddControl(chan, "????"))
                //{
                //    var sui = $"Ch: {chan.ChannelNumber} {chan.ChannelName}";
                //    var stt = $"{chan.PatchName} {chan.Patch:000}{Environment.NewLine}{chan.DeviceInfo}";

                //    var ctrl = new ChannelControl()
                //    {
                //        Name = $"Control for {chan.ChannelName}",
                //        Info = (sui, stt),
                //        Location = new(x, y),
                //        BorderStyle = BorderStyle.FixedSingle,
                //        DrawColor = _settings.DrawColor,
                //        SelectedColor = _settings.SelectedColor,
                //        Volume = rand.NextDouble() + 0.5,
                //    };
                //    ctrl.ChannelChange += ChannelControl_ChannelChange;
                //    Controls.Add(ctrl);

                //    CM.Bind(chan, ctrl);

                //    x += ctrl.Width + 10; // Width is not valid until after previous statement.
                //});
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

            var changes = SettingsEditor.Edit(_settings, "TESTOMATIC", 300);
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

            if (e.State) { CM.UpdateState(); } // TODO1 should CM handle these?

            if (e.Select) {  } // TODO1

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
