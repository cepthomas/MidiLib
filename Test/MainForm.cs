
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;
using NBagOfTricks.Slog;
using System.Text.Json.Serialization;


namespace MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Types
        public enum PlayState { Stop, Play, Rewind, Complete }
        #endregion

        #region Fields - internal
        /// <summary>All the channels - key is user assigned name.</summary>
        readonly Dictionary<string, Channel> _channels = new();

        /// <summary>Midi player.</summary>
        readonly MidiPlayer _player;

        /// <summary>Midi output.</summary>
        readonly MidiSender _sender;

        /// <summary>Midi input.</summary>
        readonly MidiListener _listener;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        MidiDataFile _mdata = new();

        /// <summary>All the channel play controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;

        /// <summary>My logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MainForm");

        /// <summary>Test stuff.</summary>
        readonly TestSettings _settings = new();
        #endregion

        #region Fields - adjust to taste
        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>My midi out.</summary>
        readonly string _midiOutDeviceName = "VirtualMIDISynth #1";

        /// <summary>My midi in.</summary>
        readonly string _midiInDeviceName = "MPK mini";

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"..\..\out";

        /// <summary>Use this if not supplied.</summary>
        readonly int _defaultTempo = 100;

        /// <summary>Actual 1-based midi channel number.</summary>
        readonly int _kbdChannelNumber = 1;

        /// <summary>Current patch.</summary>
        readonly int _kbdPatch = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. No logging yet!
        /// </summary>
        public MainForm()
        {
            // Must do this first before initializing.
            MidiSettings.LibSettings = _settings.MidiSettings;

            InitializeComponent();

            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _controlColor };

            // Make sure out path exists.
            DirectoryInfo di = new(_outPath);
            di.Create();

            _settings = (TestSettings)Settings.Load(".", typeof(TestSettings));

            // Logger. Note: you can create this here but don't call any _logger functions until loaded.
            LogManager.MinLevelFile = LogLevel.Trace;
            LogManager.MinLevelNotif = LogLevel.Trace;
            LogManager.LogEvent += LogManager_LogEvent;
            LogManager.Run(Path.Join(_outPath, "log.txt"), 5000);

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN", Color.Plum);

            // UI configs.
            sldVolume.DrawColor = _controlColor;
            sldVolume.Minimum = VolumeDefs.MIN;
            sldVolume.Maximum = VolumeDefs.MAX;
            sldVolume.Resolution = VolumeDefs.MAX / 50;
            sldVolume.Value = VolumeDefs.DEFAULT;
            sldVolume.Label = "volume";

            // Time controller.
            MidiSettings.LibSettings.Snap = SnapType.Beat;
            //MidiSettings.LibSettings.ZeroBased = true;
            barBar.ProgressColor = _controlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;

            // Init channel selectors.
            cmbDrumChannel1.Items.Add("NA");
            cmbDrumChannel2.Items.Add("NA");
            for (int i = 1; i <= MidiDefs.NUM_CHANNELS; i ++)
            {
                cmbDrumChannel1.Items.Add(i);
                cmbDrumChannel2.Items.Add(i);
            }

            // Set up midi devices. TODOX2 should get from settings, and create vk and bb.
            _sender = new(_midiOutDeviceName);
            _player = new(_sender, _channelManager);
            _listener = new(_midiInDeviceName);
            _listener.CaptureEnable = _listener.Valid;
            _player.SendPatch(_kbdChannelNumber, _kbdPatch);
            bb.InputEvent += Virtual_InputEvent;
            vkey.InputEvent += Virtual_InputEvent;

            // Hook up some simple UI handlers.
            btnPlay.CheckedChanged += (_, __) => { UpdateState(btnPlay.Checked ? PlayState.Play : PlayState.Stop); };
            btnRewind.Click += (_, __) => { UpdateState(PlayState.Rewind); };
            btnKillMidi.Click += (_, __) => { _player.KillAll(); };
            btnLogMidi.Click += (_, __) => { _player.LogMidi = btnLogMidi.Checked; };
            nudTempo.ValueChanged += (_, __) => { SetTimer(); };
            sldVolume.ValueChanged += (_, __) => { _player.Volume = sldVolume.Value; };

            // Set up timer.
            nudTempo.Value = _defaultTempo;
        }

        /// <summary>
        /// Window is set up now. OK to log!!
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            if (!_player.Valid)
            {
                _logger.Error($"Something wrong with your midi output device:{_midiOutDeviceName}");
            }

            if (!_listener.Valid)
            {
                _logger.Error($"Something wrong with your midi input device:{_midiInDeviceName}");
            }

            // MidiTimeTest();

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenFile(args[1]);
            }
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();
            Stop();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // Resources.
            _mmTimer.Stop();
            _mmTimer.Dispose();

            // Wait a bit in case there are some lingering events.
            System.Threading.Thread.Sleep(100);

            _player.Dispose();
            _listener.Dispose();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region State management
        /// <summary>
        /// General state management. Triggered by play button or the player via mm timer function.
        /// </summary>
        void UpdateState(PlayState state)
        {
            // Suppress recursive updates caused by manually pressing the play button.
            if (_guard)
            {
                return;
            }
            _guard = true;

            switch(state)
            {
                case PlayState.Complete:
                    btnPlay.Checked = false;
                    Rewind();
                    Stop();
                    break;

                case PlayState.Play:
                    btnPlay.Checked = true;
                    Play();
                    break;

                case PlayState.Stop:
                    btnPlay.Checked = false;
                    Stop();
                    break;

                case PlayState.Rewind:
                    Rewind();
                    break;
            }

            _guard = false;
        }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_ChannelChangeEvent(object? sender, ChannelChangeEventArgs e)
        {
            ChannelControl chc = (ChannelControl)sender!;

            if (e.StateChange)
            {
                switch (chc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                        {
                            int chnum = i + 1;
                            if (chnum != chc.ChannelNumber && chc.State != ChannelState.Solo)
                            {
                                _player.Kill(chnum);
                            }
                        }
                        break;

                    case ChannelState.Mute:
                        _player.Kill(chc.ChannelNumber);
                        break;
                }
            }

            if (e.PatchChange && chc.Patch >= 0)
            {
                _player.SendPatch(chc.ChannelNumber, chc.Patch);
            }
        }
        #endregion

        #region Transport control
        /// <summary>
        /// Internal handler.
        /// </summary>
        void Play()
        {
            _mmTimer.Start();
            _player.Run(true);
        }

        /// <summary>
        /// Internal handler.
        /// </summary>
        void Stop()
        {
            _mmTimer.Stop();
            _player.Run(false);
        }

        /// <summary>
        /// Go back Jack. Doesn't affect the run state.
        /// </summary>
        void Rewind()
        {
            _player.CurrentSubdiv = 0;
            barBar.Current = new(0);
        }

        /// <summary>
        /// User has changed the time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BarBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
            _player.CurrentSubdiv = barBar.Current.TotalSubdivs;
        }
        #endregion

        #region File handling
        /// <summary>
        /// Common file opener. Initializes pattern list from contents.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        public bool OpenFile(string fn)
        {
            bool ok = true;

            _logger.Info($"Reading file: {fn}");

            if(btnPlay.Checked)
            {
                Stop();
            }

            try
            {
                // Reset stuff.
                cmbDrumChannel1.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL;
                cmbDrumChannel2.SelectedIndex = 0;
                _mdata = new MidiDataFile();

                // Process the file. Set the default tempo from preferences.
                _mdata.Read(fn, _defaultTempo, false);

                // Init new stuff with contents of file/pattern.
                lbPatterns.Items.Clear();
                var pnames = _mdata.GetPatternNames();

                switch(pnames.Count)
                {
                    case 0:
                        ok = false;
                        throw new InvalidOperationException($"Something wrong with this file: {fn}");

                    case 1:
                        var pinfo = _mdata.GetPattern(pnames[0]);
                        LoadPattern(pinfo);
                        break;

                    default: // style has multiple patterns.
                        pnames.ForEach(pn =>
                        {
                            var p = _mdata.GetPattern(pn);
                            switch (p!.PatternName)
                            {
                                // These don't contain a pattern.
                                case "SFF1":
                                case "SFF2":
                                case "SInt": // Initial patches are in here.
                                    break;

                                case "":
                                    _logger.Error("Well, this should never happen!");
                                    break;

                                default:
                                    lbPatterns.Items.Add(p.PatternName);
                                    break;
                            }
                        });
                        break;
                }

                Rewind();

                Text = $"Midi Lib Test - {fn}";

                lbPatterns.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.Error($"Couldn't open the file: {fn} because: {ex.Message}");
                Text = "Midi Lib";
                ok = false;
            }

            return ok;
        }
        #endregion

        #region Process pattern info into events
        /// <summary>
        /// Load the requested pattern and create controls.
        /// </summary>
        /// <param name="pinfo"></param>
        void LoadPattern(PatternInfo? pinfo)
        {
            _player.Reset();

            // Clean out our collections.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
            _channels.Clear();

            if (pinfo is null)
            {
                _logger.Error($"Invalid pattern!");
            }
            else
            {
                // Create the new controls.
                int lastSubdiv = 0;
                int x = lbPatterns.Right + 5;
                int y = lbPatterns.Top;


                foreach(int chnum in pinfo.ChannelNumbers)
                {
                    var chEvents = pinfo.GetFilteredEvents(new List<int>() { chnum }).Where(e => e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent);

                    // Is this channel pertinent?
                    if (chEvents.Any())
                    {
                        // Make new channel.
                        Channel channel = new()
                        {
                            ChannelName = $"chan{chnum}",
                            ChannelNumber = chnum,
                            DeviceId = "OutputDevice1",
                            Volume = VolumeDefs.DEFAULT,
                            State = ChannelState.Normal,
                            Patch = pinfo.Patches[chnum - 1],
                            IsDrums = chnum == MidiDefs.DEFAULT_DRUM_CHANNEL,
                            Selected = false,
               //TODOX1             Device = _outputDevices[chspec.DeviceId]
                        };
                        channel.SetEvents(chEvents);
                        _channels.Add(channel.ChannelName, channel);

                        // Make new control and bind to channel.
                        ChannelControl control = new()
                        {
                            Location = new(x, y),
                            BorderStyle = BorderStyle.FixedSingle,
                            BoundChannel = channel
                        };
                        control.ChannelChangeEvent += Control_ChannelChangeEvent;
                        Controls.Add(control);
                        _channelControls.Add(control);

                        // Good time to send initial patch.
                        channel.SendPatch();

                        //lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                        // Adjust positioning.
                        y += control.Height + 5;
                    }
                }


                //was:
                //for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                //{
                //    int chnum = i + 1;

                //    var chEvents = pinfo.GetFilteredEvents(new List<int>() { chnum }).Where(e => e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent);

                //    // Is this channel pertinent?
                //    if (chEvents.Any())
                //    {
                //        _channelManager.SetEvents(chnum, chEvents);

                //        // Make new control.
                //        ChannelControl control = new() { Location = new(x, y), Name = $"channel{chnum}" };

                //        // Bind to internal channel object.
                //        _channelManager.Bind(chnum, control);

                //        // Now init the control - after binding!
                //        control.Patch = pinfo.Patches[i];
                //        //control.IsDrums = GetDrumChannels().Contains(chnum);

                //        control.ChannelChangeEvent += Control_ChannelChange;
                //        Controls.Add(control);
                //        _channelControls.Add(control);

                //        lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                //        // Adjust positioning.
                //        y += control.Height + 5;

                //        // Send patch maybe. These can change per pattern.
                //        _player.SendPatch(chnum, pinfo.Patches[i]);
                //    }
                //}
            }

            // Update bar.
            var tot = _channels.TotalSubdivs();
            barBar.Start = new(0);
            barBar.End = new(tot - 1);
            barBar.Length = new(tot);
            barBar.Current = new(0);

            UpdateDrumChannels();
        }

        /// <summary>
        /// Load pattern selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var pinfo = _mdata.GetPattern(lbPatterns.SelectedItem.ToString()!);

            LoadPattern(pinfo);

            Rewind();
        }

        /// <summary>
        /// Pattern selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AllOrNone_Click(object? sender, EventArgs e)
        {
            bool check = sender == btnAll;
            for(int i = 0; i < lbPatterns.Items.Count; i++)
            {
                lbPatterns.SetItemChecked(i, check);                
            }
        }
        #endregion

        #region Process tick
        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// This is running on the background thread.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            try
            {
                // Bump time. Check for end of play. Client will take care of transport control.
                barBar.IncrementCurrent(1);

                if (_player.DoNextStep())
                {
                    // Done playing. Bump over to main thread.
                    this.InvokeIfRequired(_ => UpdateState(PlayState.Complete));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Drum channel
        /// <summary>
        /// User changed the drum channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumChannel_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateDrumChannels();
        }

        /// <summary>
        /// Update all channels based on current UI.
        /// </summary>
        void UpdateDrumChannels()
        {
            _channelControls.ForEach(ctl => ctl.IsDrums =
                (ctl.ChannelNumber == cmbDrumChannel1.SelectedIndex) ||
                (ctl.ChannelNumber == cmbDrumChannel2.SelectedIndex));
        }
        #endregion

        #region Export
        /// <summary>
        /// Export current file to human readable or midi.
        /// </summary>
        void Export_Click(object? sender, EventArgs e)
        {
            try
            {
                // Get selected patterns.
                List<string> patternNames = new();
                if (lbPatterns.Items.Count == 1)
                {
                    patternNames.Add(lbPatterns.Items[0].ToString()!);
                }
                else if (lbPatterns.CheckedItems.Count > 0)
                {
                    foreach (var p in lbPatterns.CheckedItems)
                    {
                        patternNames.Add(p.ToString()!);
                    }
                }
                else
                {
                    _logger.Warn("Please select at least one pattern");
                    return;
                }
                List<PatternInfo> patterns = new();
                patternNames.ForEach(p => patterns.Add(_mdata.GetPattern(p)!));

                // Get selected channels.
                List<Channel> channels = new();
                _channelControls.Where(cc => cc.Selected).ForEach(cc => channels.Add(cc.BoundChannel));
                if (!channels.Any()) // grab them all.
                {
                    _channelControls.ForEach(cc => channels.Add(cc.BoundChannel));
                }

                // Execute the requested export function.
                if (sender == btnExportAll)
                {
                    var newfn = MakeExportFileName(_outPath, _mdata.FileName, "all", "csv");
                    MidiExport.ExportAllEvents(newfn, patterns, channels, MakeMeta());
                    _logger.Info($"Exported to {newfn}");
                }
                else if (sender == btnExportPattern)
                {
                    foreach (var pattern in patterns)
                    {
                        var newfn = MakeExportFileName(_outPath, _mdata.FileName, pattern.PatternName, "csv");
                        MidiExport.ExportGroupedEvents(newfn, pattern, channels, MakeMeta(), true); //includeAll
                        _logger.Info($"Exported pattern {pattern.PatternName} to {newfn}");
                    }
                }
                else if (sender == btnExportMidi)
                {
                    foreach (var pattern in patterns)
                    {
                        var newfn = MakeExportFileName(_outPath, _mdata.FileName, pattern.PatternName, "mid");
                        MidiExport.ExportMidi(newfn, pattern, channels, MakeMeta());
                        _logger.Info($"Export midi to {newfn}");
                    }
                }
                else
                {
                    _logger.Error($"Ooops: {sender}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.Message}");
            }
        }

        /// <summary>
        /// Create a new clean filename for export. Creates path if it doesn't exist.
        /// </summary>
        /// <param name="path">Export path</param>
        /// <param name="baseFn">Root of the new file name</param>
        /// <param name="mod">Modifier</param>
        /// <param name="ext">File extension</param>
        /// <returns></returns>
        public string MakeExportFileName(string path, string baseFn, string mod, string ext)
        {
            string name = Path.GetFileNameWithoutExtension(baseFn);

            // Clean the file name.
            name = name.Replace('.', '-').Replace(' ', '_');
            mod = mod == "" ? "default" : mod.Replace(' ', '_');
            var newfn = Path.Join(path, $"{name}_{mod}.{ext}");
            return newfn;
        }

        /// <summary>
        /// Utility to contain midi file meta info.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, int> MakeMeta()
        {
            Dictionary<string, int> meta = new()
            {
                { "MidiFileType", _mdata.MidiFileType },
                { "DeltaTicksPerQuarterNote", _mdata.DeltaTicksPerQuarterNote },
                { "NumTracks", _mdata.NumTracks }
            };

            return meta;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Convert tempo to period and set mm timer.
        /// </summary>
        void SetTimer()
        {
            MidiTimeConverter mt = new(_mdata.DeltaTicksPerQuarterNote, (double)nudTempo.Value);
            double period = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        }

        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogEvent(object? sender, LogEventArgs e)
        {
            // Usually come from a different thread.
            if(IsHandleCreated)
            {
                this.InvokeIfRequired(_ =>
                {
                    txtViewer.AppendLine($"{e.Message}");
                });
            }
        }
        #endregion

        #region Debug stuff
        /// <summary>
        /// Mainly for debug.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Stuff_Click(object sender, EventArgs e)
        {
            MidiTimeTest();

            // var def = _settings.MidiSettings.InternalPPQ;

            // // ppq = 4/8
            // // 1.0 1.1 ... 1.7 2.0
            // _settings.MidiSettings.InternalPPQ = PPQ.PPQ_8;

            // var b1 = new BarTime(1.0);
            // var b2 = new BarTime(1.7);
            // var b3 = new BarTime(1.8);
            // var b4 = new BarTime(2.0);
            // var b5 = new BarTime(2.1);

            // // ppq = 16/32
            // // 1.0 1.1 ... 1.7 1.8 ... 1.15 2.0
            // // 1.00 1.01 ... 1.07 1.08 ... 1.15 2.00
            // _settings.MidiSettings.InternalPPQ = PPQ.PPQ_32;
            // var b11 = new BarTime(1.0);
            // var b12 = new BarTime(1.7);
            // var b13 = new BarTime(1.8);
            // var b14 = new BarTime(1.31);
            // var b15 = new BarTime(1.32);
            // var b16 = new BarTime(2.0);
            // var b17 = new BarTime(2.1);

            // // Restore.
            // _settings.MidiSettings.InternalPPQ = def;
        }

        /// <summary>
        /// Unit test.
        /// </summary>
        void MidiTimeTest()
        {
            // If we use ppq of 8 (32nd notes):
            // 100 bpm = 800 ticks/min = 13.33 ticks/sec = 0.01333 ticks/msec = 75.0 msec/tick
            //  99 bpm = 792 ticks/min = 13.20 ticks/sec = 0.0132 ticks/msec  = 75.757 msec/tick

            MidiTimeConverter mt = new(0, 100);
            TestClose(mt.InternalPeriod(), 75.0, 0.001);

            mt = new(0, 90);
            TestClose(mt.InternalPeriod(), 75.757, 0.001);

            mt = new(384, 100);
            TestClose(mt.MidiToSec(144000) / 60.0, 3.75, 0.001);

            mt = new(96, 100);
            TestClose(mt.MidiPeriod(), 6.25, 0.001);

            _logger.Error($"MidiTimeTest done.");

            void TestClose(double value1, double value2, double tolerance)
            {
                if (Math.Abs(value1 - value2) > tolerance)
                {
                    _logger.Error($"[{value1}] not close enough to [{value2}]");
                }
            }
        }

        /// <summary>
        /// Tell me what you have.
        /// </summary>
        void DumpMidiDevices()
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                _logger.Info($"Midi In {i} \"{MidiIn.DeviceInfo(i).ProductName}\"");
            }

            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                _logger.Info($"Midi Out {i} \"{MidiOut.DeviceInfo(i).ProductName}\"");
            }
        }

        /// <summary>
        /// Generate human info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Docs_Click(object sender, EventArgs e)
        {
            DumpMidiDevices();

            var docs = MusicDefinitions.FormatDoc();
            Tools.MarkdownToHtml(docs, Color.LightYellow, new Font("tahoma", 16), true);

            docs = MidiDefs.FormatDoc();
            Tools.MarkdownToHtml(docs, Color.LightGreen, new Font("Lucida Sans Unicode", 16), true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Settings_Click(object sender, EventArgs e)
        {
            _settings.Edit("howdy!", 400);
            _settings.Save();
            _logger.Warn("You better restart!");
        }
        #endregion

        #region Device input
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Virtual_InputEvent(object? sender, InputEventArgs e)
        {
            _logger.Debug($"VirtDev N:{e.Note} V:{e.Value}");

            NoteEvent nevt = e.Value > 0 ?
                new NoteOnEvent(0, _kbdChannelNumber, e.Note % MidiDefs.MAX_MIDI, e.Value % MidiDefs.MAX_MIDI, 0) :
                new NoteEvent(0, _kbdChannelNumber, MidiCommandCode.NoteOff, e.Note, 0);

            _player.SendMidi(nevt);
        }
        #endregion
    }

    public class TestSettings : Settings
    {
        [DisplayName("Background Color")]
        [Description("The color used for overall background.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.AliceBlue;

        [DisplayName("Ignore Warnings")]
        [Description("Ignore compiler warnings otherwise treat them as errors.")]
        [Browsable(true)]
        public bool IgnoreWarnings { get; set; } = true;

        [DisplayName("Midi Settings")]
        [Description("Edit midi settings.")]
        [Browsable(true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public MidiSettings MidiSettings { get; set; } = new();
    }
}
