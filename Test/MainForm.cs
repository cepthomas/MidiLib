
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


namespace MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Fields - internal
        /// <summary>Midi player.</summary>
        readonly Player _player;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        MidiData _mdata = new();

        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;

        /// <summary>Our internal midi send timer resolution.</summary>
        readonly int _sendPPQ = 32;

        /// <summary>Only 4/4 time supported.</summary>
        readonly int _beatsPerBar = 4;

        /// <summary>Current file.</summary>
        string _fn = "";
        #endregion

        #region Fields - user custom
        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>My midi out.</summary>
        readonly string _midiDevice = "VirtualMIDISynth #1";

        /// <summary>Adjust to taste.</summary>
        readonly string _exportPath = @"C:\Dev\repos\MidiLib\out";

        /// <summary>Use this if not supplied.</summary>
        readonly int _defaultTempo = 100;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            _player = new(_midiDevice);
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _controlColor };

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN", Color.Plum);

            // Toolbar configs. Adjust to taste.
            btnAutoplay.Checked = false;
            btnLoop.Checked = true;

            // UI configs.
            sldVolume.DrawColor = _controlColor;
            sldVolume.Value = Channel.DEFAULT_VOLUME;
            sldTempo.DrawColor = _controlColor;
            sldTempo.Value = _defaultTempo;
            sldPosition.DrawColor = _controlColor;

            // Hook up some simple UI handlers.
            btnPlay.CheckedChanged += (_, __) => { UpdateState(); };
            btnRewind.Click += (_, __) => { Rewind(); };
            btnKillMidi.Click += (_, __) => { _player.KillAll(); };
            btnLogMidi.Click += (_, __) => { _player.LogMidi = btnLogMidi.Checked; };
            sldTempo.ValueChanged += (_, __) => { SetTimer(); };
            sldPosition.ValueChanged += (_, __) => { SetPositionFromSlider(); };

            // Set up timer.
            sldTempo.Value = _defaultTempo;
            SetTimer();

            // Make sure out path exists.
            DirectoryInfo di = new(_exportPath);
            di.Create();

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenFile(args[1]);
            }
            else
            {
                OpenFile(@"C:\Dev\repos\MidiStyleExplorer\test\_LoveSong.S474.sty");
                //OpenFile(@"C:\Users\cepth\OneDrive\Audio\Midi\styles\2kPopRock\60'sRock&Roll.S605.sty");
                //OpenFile(@"C:\Dev\repos\ClipExplorer\_files\_drums_ch1.mid");
                //OpenFile(@"C:\Dev\repos\ClipExplorer\_files\25jazz.mid"); //TODO1 not quite right...
            }
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Stop();
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

            _player?.Dispose();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion




        void SetPositionFromInternal()
        {
            int pos = _player.CurrentSubdiv * (int)sldPosition.Maximum / _player.TotalSubdivs;
            sldPosition.Value = pos;
        }

        void SetPositionFromSlider() // click from ui
        {
            int pos = _player.TotalSubdivs * (int)sldPosition.Value / (int)sldPosition.Maximum;
            _player.CurrentSubdiv = pos;
        }

        #region State management
        /// <summary>
        /// General state management. Triggered by play button or the player via mm timer function.
        /// </summary>
        void UpdateState()
        {
            // Suppress recursive updates caused by manually pressing the play button.
            if (_guard)
            {
                return;
            }

            _guard = true;

            //LogMessage($"DBG State:{_player.State}  btnLoop{btnLoop.Checked}  TotalSubdivs:{_player.TotalSubdivs}");

            switch (_player.State)
            {
                case RunState.Complete:
                    Rewind();

                    if (btnLoop.Checked)
                    {
                        btnPlay.Checked = true;
                        Play();
                    }
                    else
                    {
                        btnPlay.Checked = false;
                        Stop();
                    }
                    break;

                case RunState.Playing:
                    if (!btnPlay.Checked)
                    {
                        Stop();
                    }
                    break;

                case RunState.Stopped:
                    if (btnPlay.Checked)
                    {
                        Play();
                    }
                    break;
            }

            // Update UI.
            SetPositionFromInternal();

            _guard = false;
        }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
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
                _player.SetPatch(chc.ChannelNumber, chc.Patch);
            }
        }
        #endregion

        #region Transport control
        /// <summary>
        /// Internal handler.
        /// </summary>
        void Play()
        {
            // Start or restart?
            if (!_mmTimer.Running)
            {
                _mmTimer.Start();
            }
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
            //barBar.Current = BarSpan.Zero;
            _player.CurrentSubdiv = 0;
            sldPosition.Value = 0;
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
            _fn = "";

            LogMessage($"INF Reading file: {fn}");

            if(btnPlay.Checked)
            {
                btnPlay.Checked = false; // ==> Stop()
            }

            try
            {
                // Process the file. Set the default tempo from preferences.
                _mdata = new();
                _mdata.Read(fn, _defaultTempo, false);

                // Init new stuff with contents of file/pattern.
                lbPatterns.Items.Clear();

                if(_mdata.AllPatterns.Count == 0)
                {
                    LogMessage($"ERR Something wrong with this file: {fn}");
                    ok = false;
                }
                else if(_mdata.AllPatterns.Count == 1) // plain midi
                {
                    var pinfo = _mdata.AllPatterns[0];
                    LoadPattern(pinfo);
                }
                else // style - multiple patterns.
                {
                    foreach (var p in _mdata.AllPatterns)
                    {
                        switch (p.PatternName)
                        {
                            // These don't contain a pattern.
                            case "SFF1": // initial patches are in here
                            case "SFF2":
                            case "SInt":
                                break;

                            case "":
                                LogMessage("ERR Well, this should never happen!");
                                break;

                            default:
                                lbPatterns.Items.Add(p.PatternName);
                                break;
                        }
                    }

                    if (lbPatterns.Items.Count > 0)
                    {
                        lbPatterns.SelectedIndex = 0;
                    }
                }

                Rewind();

                if (ok && btnAutoplay.Checked)
                {
                    btnPlay.Checked = true; // ==> Start()
                }

                _fn = fn;
                Text = $"Midi Lib - {fn}";

            }
            catch (Exception ex)
            {
                LogMessage($"ERR Couldn't open the file: {fn} because: {ex.Message}");
                Text = "Midi Lib";
                ok = false;
            }

            return ok;
        }
        #endregion

        #region Process patterns
        /// <summary>
        /// Load the requested pattern and create controls.
        /// </summary>
        /// <param name="pinfo"></param>
        void LoadPattern(PatternInfo pinfo)
        {
            _player.Reset();

            // Clean out our collection.
            _channelControls.ForEach(c => Controls.Remove(c));
            _channelControls.Clear();

            // Create the new controls.
            int lastSubdiv = 0;
            int x = sldTempo.Right + 5;
            int y = sldTempo.Top;

            // For scaling subdivs to internal.
            MidiTime mt = new()
            {
                InternalPpq = _sendPPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = _defaultTempo
            };

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;

                var chEvents = _mdata.AllEvents.
                    Where(e => e.PatternName == pinfo.PatternName && e.ChannelNumber == chnum && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
                    OrderBy(e => e.AbsoluteTime);

                if (chEvents.Any())
                {
                    _player.SetEvents(chnum, chEvents, mt);
                    //PatchInfo patch = pinfo.Patches[i];

                    // Make new controls. Bind to internal channel object.
                    ChannelControl control = new()
                    {
                        Channel = _player.GetChannel(chnum),
                        Location = new(x, y),
                        Patch = pinfo.Patches[i],
                        //// default state
                        //State = ChannelState.Normal,
                        //Volume = Channel.DEFAULT_VOLUME,
                        //Selected = false
                    };

                    control.ChannelChange += Control_ChannelChange;
                    Controls.Add(control);
                    _channelControls.Add(control);

                    lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                    // Adjust positioning.
                    y += control.Height + 5;

                    // Send patch maybe. These can change per pattern.
                    _player.SetPatch(chnum, pinfo.Patches[i]);
                }
            }
        }

        /// <summary>
        /// Load pattern selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var pinfo = _mdata.AllPatterns.Where(p => p.PatternName == lbPatterns.SelectedItem.ToString()).First();

            LoadPattern(pinfo!);

            Rewind();

            if (btnAutoplay.Checked)
            {
                btnPlay.Checked = true; // ==> Start()
            }
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
        int _report = 0;

        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// This is running on the background thread.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            // TODO This sometimes blows up on shutdown with ObjectDisposedException. I am probably doing bad things with threads.
            try
            {
                if(--_report <= 0)
                {
                    this.InvokeIfRequired(_ => LogMessage($"DBG CurrentSubdiv:{_player.CurrentSubdiv}"));
                    _report = 100;
                }

                _player.DoNextStep();
                // Bump over to main thread.
                this.InvokeIfRequired(_ => UpdateState());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Convert tempo to period and set mm timer.
        /// </summary>
        void SetTimer()
        {
            MidiTime mt = new()
            {
                InternalPpq = _sendPPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = sldTempo.Value
            };

            double period = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        }

        /// <summary>
        /// Export current file to human readable or midi.
        /// </summary>
        void Export_Click(object? sender, EventArgs e)
        {
            _mdata.ExportPath = _exportPath;

            try
            {
                // Collect filters.
                List<string> patternNames = new();
                foreach(var p in lbPatterns.CheckedItems)
                {
                    patternNames.Add(p.ToString()!);
                }

                List<int> channels = new();
                foreach (var cc in _channelControls.Where(c => c.Selected))
                {
                    channels.Add(cc.ChannelNumber);
                }

                if (sender == btnExportAll)
                {
                    var s = _mdata.ExportAllEvents(channels);
                    LogMessage($"INF Exported to {s}");
                }
                else if (sender == btnExportPattern)
                {
                    foreach(var patternName in patternNames)
                    {
                        var s = _mdata.ExportGroupedEvents(patternName, channels, true);
                        LogMessage($"INF Exported to {s}");
                    }
                }
                else if (sender == btnExportMidi)
                {
                    foreach (var patternName in patternNames)
                    {
                        // Use original ppq.
                        var s = _mdata.ExportMidi(patternName, channels, _mdata.DeltaTicksPerQuarterNote, false);
                        LogMessage($"INF Export midi to {s}");
                    }
                }
                else
                {
                    LogMessage($"ERR Ooops: {sender}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERR {ex.Message}");
            }
        }

        /// <summary>
        /// Something you should know.
        /// </summary>
        /// <param name="msg"></param>
        void LogMessage(string msg)
        {
            string s = $"> {msg}";
            txtViewer.AppendLine(s);
        }
        #endregion
    }
}
