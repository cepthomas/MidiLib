
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


namespace Ephemera.MidiLib
{
    public partial class MainForm : Form
    {
        ///// Services. /////
        readonly ChannelManager CM;
        readonly InputManager IM;
        readonly Random rand = new();

        ///// UI. /////
        readonly Slider sldMasterVolume = new();
        readonly TimeBar timeBar = new();

        ///// Hello. /////
        public MainForm()
        {
            CM = new(Controls);
            IM = new();

            // Master volume.
            sldMasterVolume.Minimum = 0.0;
            sldMasterVolume.Maximum = VolumeDefs.MAX_VOLUME;
            sldMasterVolume.Resolution = VolumeDefs.MAX_VOLUME / 50;
            sldMasterVolume.Value = 0.8;
            sldMasterVolume.Label = "master volume";

            timeBar.Snap = SnapType.Beat;
            timeBar.StateChange += (sender, e) => { };

            //// Simple UI handlers.
            //btnKillMidi.Click += (_, __) => { CM.KillAll(); };
            //chkLoop.CheckedChanged += (_, __) => { timeBar.DoLoop = chkLoop.Checked; };
            //btnRewind.Click += (_, __) => { timeBar.Rewind(); };

            //btnGo.Click += Go_Click;
            //btnTimebar.Click += (_, _) => TestTimeBar();
            //btnProperty.Click += (_, _) => TestPropertyEditor();

            //btnSend.Click += BtnSend_Click;

            // Handle midi io.
            IM.MessageReceived += Mgr_MessageReceived;
            //MidiManager.Instance.MessageSent += Mgr_MessageSent;
        }

        ///// Goodbye. /////
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CM.Dispose();
                IM.Dispose();
                //components.Dispose();
            }

            base.Dispose(disposing);
        }

        ///// Create channels and controls dynamically. /////
        void DoDynamic()
        {
            try
            {
                int x = 10;
                int y = 10;

                var chan = CM.OpenChannel("My midi output 1", 3, "HonkyTonkPiano");
                AddNewControl(chan, "dyn keys");

                chan = CM.OpenChannel("My midi output 1", 4, "KUSTOM", @"C:\Dev\Libs\MidiLib\Test\test_defs.ini");
                AddNewControl(chan, "dyn bass");

                chan = CM.OpenChannel("My midi output 2", 10, "Electronic");
                AddNewControl(chan, "dyn drums");

                var in1 = IM.OpenMidiInput("My midi input", 1);

                // Ceate a control for each channel and bind object.
                void AddNewControl(Channel chan, string channelName)
                {
                    var sui = $"Ch: {chan.ChannelNumber} {channelName}";
                    var stt = $"{chan.PatchName} {chan.Patch:000}"; //{Environment.NewLine}"Dev {chan.DeviceInfo}";

                    var ctrl = new ChannelControl()
                    {
                        Name = channelName,
                        Info = (sui, stt),
                        Location = new(x, y),
                        BorderStyle = BorderStyle.FixedSingle,
                        DrawColor = Color.Blue,
                        SelectedColor = Color.Green,
                        Volume = rand.NextDouble() + 0.5,
                    };
                    ctrl.InfoClick += ChannelControl_InfoClick;

                    CM.Bind(chan, ctrl);

                    x += ctrl.Width + 10; // Width is not valid until after previous statement.
                }
            }
            catch
            {
            }

            CM.UpdateState();
        }

        ///// Send some midi. /////
        void BtnSend_Click(object? sender, EventArgs e)
        {
            int noteNum = rand.Next(40, 60);
            var cc = sender as ChannelControl;
            var chan = CM.GetChannel(cc!);
            SendNote(chan!, noteNum, 0.5 * cc!.Volume);
        }

        ///// Send midi note. /////
        void SendNote(Channel chan, int note, double volume)
        {
            if (note is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"Note:{note}"); }

            if (volume == 0.0)
            {
                chan.Send(new NoteOff(chan.ChannelNumber, note));
            }
            else
            {
                var vol = volume * sldMasterVolume.Value;
                var vel = (int)MathUtils.Constrain(vol * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI);
                var se = new NoteOn(chan.ChannelNumber, note, vel);
                chan.Send(se);
            }
        }

        ///// Common midi sender. Adjusts volume as needed. /////
        void SendMidi(object? sender, BaseEvent e)
        {
            if (sender is not ChannelControl ctrl) throw new MidiLibException("Unknown/null control");
            var chan = CM.GetChannel(e.ChannelNumber) ?? throw new MidiLibException("Unknown/null channel");

            if (e is NoteOn)
            {
                var non = e as NoteOn;
                // Adjust volume.
                var masterVol = 1.0; // or from sldMasterVolume.Value

                var vol = masterVol * ctrl!.Volume * non!.Velocity;
                non.Velocity = MathUtils.Constrain((int)vol, 0, MidiDefs.MAX_MIDI);
            }
            chan.Send(e);
        }

        ///// Send midi controller. /////
        void SendController(Channel channel, int controller, int value)
        {
            if (controller is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"controller:{controller}"); }
            if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"value:{value}"); }

            // ?? if (channel.Enable)
            var se = new Controller(channel.ChannelNumber, controller, value);
            channel.Send(se);
        }

        ///// Something arrived from a midi device. /////
        void Mgr_MessageReceived(object? sender, BaseEvent e)
        {
            var chan = CM.GetChannel(e.ChannelNumber);
            if (chan is not null)
            {
                //Tell(INFO, $"Mgr Received [{e}] on [{chan}]");
            }
            else
            {
                throw new MidiLibException("Unknown/null channel");
            }
        }

        ///// Host can do custom stuff with this. /////
        void ChannelControl_InfoClick(object? sender, ChannelControl.InfoClickEventArgs e)
        {
        }

        ///// Tick-tock. /////
        void DoTimeBar()
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
            Timer tmr = new() { Interval = 3 };
            int count = 1000;
            void Timer_Tick(object? sender, EventArgs e)
            {
                timeBar.Increment();
                if (--count <= 0)
                {
                    tmr.Stop();
                    tmr.Tick -= Timer_Tick;
                    tmr.Dispose();
                }
            }
            tmr.Tick += Timer_Tick;
            tmr.Start();
        }

        ///// Typical implementation. /////
        public bool DoNextStep()
        {
            int drumChannel = 10;

            // Process each channel.
            foreach (var (chan, ctrl) in CM.GetChannels())
            {
                // Process any sequence steps.
                var playEvents = chan.Events.Get(timeBar.Current);

                foreach (var mevt in playEvents)
                {
                    switch (mevt)
                    {
                        case NoteOn evt:
                            // Adjust volume.
                            evt.Velocity = MathUtils.Constrain((int)(evt.Velocity * ctrl!.Volume * sldMasterVolume.Value), 0, MidiDefs.MAX_MIDI);
                            // Adjust channel.
                            evt.ChannelNumber = evt.ChannelNumber == drumChannel ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.ChannelNumber;
                            chan.Send(evt);
                            break;

                        case NoteOff evt:
                            // Adjust channel.
                            evt.ChannelNumber = evt.ChannelNumber == drumChannel ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.ChannelNumber;
                            chan.Send(evt);
                            break;

                        default:
                            // Everything else as is.
                            chan.Send(mevt);
                            break;
                    }
                }
            }

            // Bump time. Check for end of play.
            bool done = !timeBar.Increment();

            return done;
        }
    }
}
