using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Ephemera.MidiLib;


namespace Ephemera.MidiLib.Test
{
    //----------------------------------------------------------------
    /// <summary>Test gen aux files.</summary>
    public class MIDILIB_GEN : TestSuite
    {
        string myPath = MiscUtils.GetSourcePath();

        public override void RunSuite()
        {
            StopOnFail(true);

            string fnIni = Path.Combine(myPath, "..", "gm_defs.ini");

            var smd = MidiDefs.GenMarkdown();
            var fnOut = Path.Join(myPath, "midi_defs.md");
            File.WriteAllText(fnOut, string.Join(Environment.NewLine, smd));

            var sld = MidiDefs.GenLua();
            fnOut = Path.Join(myPath, "midi_defs.lua");
            File.WriteAllText(fnOut, string.Join(Environment.NewLine, sld));

            var sdi = MidiDefs.GenUserDeviceInfo();
        }
    }

    //----------------------------------------------------------------
    /// <summary>Test channel logic.</summary>
    public class MIDILIB_CHANNEL : TestSuite
    {
        string myPath = MiscUtils.GetSourcePath();

        public override void RunSuite()
        {
            StopOnFail(true);

            // Dummy device.
            var outdev = "nullout:test1";
            var indev = "nullin:test1";
            BaseEvent? sent = null;
            MidiManager.Instance.MessageSent += (object? sender, BaseEvent e) => sent = e;
            BaseEvent? rcvd = null;
            MidiManager.Instance.MessageReceived += (object? sender, BaseEvent e) => rcvd = e;

            // Input
            var chan_in1 = MidiManager.Instance.OpenInputChannel(indev, 1, "my input");


            ///// Named instrument mode
            // 1 is GM instruments
            var chan_out1 = MidiManager.Instance.OpenOutputChannel(outdev, 1, "keys", "HonkyTonkPiano");
            // 2 is GM drums1
            var chan_out2 = MidiManager.Instance.OpenOutputChannel(outdev, 10, "drums", "Electronic");
            // 3 is Alt instruments
            var instFn = Path.Join(MiscUtils.GetSourcePath(), "test_defs.ini");
            var chan_out3 = MidiManager.Instance.OpenOutputChannel(outdev, 4, "alt", "WaterWhistle1", instFn);

            Assert(chan_out1.PatchName == "HonkyTonkPiano");
            Assert(chan_out2.PatchName == "Electronic");
            Assert(chan_out3.PatchName == "WaterWhistle1");

            // Should send midi patch.
            chan_out1.PatchName = "Trumpet";
            Assert(sent is Patch);
            Patch pevt = (sent as Patch)!;
            Assert(pevt.ChannelNumber == chan_out1.ChannelNumber);
            Assert(pevt.Value == 56);

            ///// Anonymous mode
            sent = null;
            var chan_out4 = MidiManager.Instance.OpenOutputChannel(outdev, 1, "keys", 38);
            Assert(chan_out4.PatchName == "INST_38");
            Assert(sent is Patch);
            pevt = (sent as Patch)!;
            Assert(pevt.ChannelNumber == chan_out4.ChannelNumber);
            Assert(pevt.Value == 38);
        }
    }

    //----------------------------------------------------------------
    /// <summary>Test def file loading etc.</summary>
    public class MIDILIB_DEF : TestSuite
    {
        string myPath = MiscUtils.GetSourcePath();

        public override void RunSuite()
        {
            StopOnFail(true);

            string fn = Path.Join(myPath, "..", "gm_defs.ini");

            var ir = new IniReader();
            ir.ParseFile(fn);

            var sn = ir.GetSectionNames();
            Assert(sn.Count == 4);
        }
    }

    //----------------------------------------------------------------
    /// <summary>Test MusicTime.</summary>
    public class MIDILIB_MUSTIME : TestSuite
    {
        public override void RunSuite()
        {
            StopOnFail(true);

            ///// Basic parse and format.
            var mt = new MusicTime("23.2.6");
            Assert(mt.Tick == 23 * MusicTime.TicksPerBar + 2 * MusicTime.TicksPerBeat + 6);

            mt = new MusicTime("146.1");
            Assert(mt.Tick == 146 * MusicTime.TicksPerBar + 1 * MusicTime.TicksPerBeat);

            mt = new MusicTime("71");
            Assert(mt.Tick == 71 * MusicTime.TicksPerBar);

            mt = new MusicTime(12345);
            Assert(mt.ToString() == "385.3.1");

            Throws(typeof(ArgumentException), () =>
            {
                mt = new MusicTime("49.55.8");
            });

            Throws(typeof(ArgumentException), () =>
            {
                mt = new MusicTime("111.3.88");
            });

            Throws(typeof(ArgumentException), () =>
            {
                mt = new MusicTime("invalid");
            });

            ///// Interfaces and overloads.
            Dictionary<MusicTime, string> tmvals = [];
            List<int> vals = [24, 511, 9, 370, 33, 2, 0, 659, 72];
            vals.ForEach(v => { var mt = new MusicTime(v); tmvals.Add(mt, mt.ToString()); });

            {
                mt = new MusicTime(369);
                bool ok = tmvals.TryGetValue(mt, out string? res);
                Assert(!ok);
                Assert(res is null);
            }

            {
                mt = new MusicTime(370);
                bool ok = tmvals.TryGetValue(mt, out string? res);
                Assert(ok);
                Assert(res! == "11.2.2");
            }

            {
                mt = new MusicTime(371);
                bool ok = tmvals.TryGetValue(mt, out string? res);
                Assert(!ok);
                Assert(res is null);
            }
        }
    }

    //----------------------------------------------------------------
    /// <summary>Test MidiTimeConverter.</summary>
    public class MIDILIB_TIMECONV : TestSuite
    {
        public override void RunSuite()
        {
            StopOnFail(true);

            // A unit test. If we use ppq of 8 (32nd notes):
            // 100 bpm = 800 ticks/min = 13.33 ticks/sec = 0.01333 ticks/msec = 75.0 msec/tick
            //  99 bpm = 792 ticks/min = 13.20 ticks/sec = 0.0132 ticks/msec  = 75.757 msec/tick

            MidiTimeConverter mt = new(0, 100);

            Assert(mt.InternalPeriod().IsClose(75.0, 0.001));

            mt = new(0, 99);
            Assert(mt.InternalPeriod().IsClose(75.757, 0.001));

            mt = new(384, 100);
            Assert((mt.MidiToSec(144000) / 60.0).IsClose(3.75, 0.001));

            mt = new(96, 100);
            Assert(mt.MidiPeriod().IsClose(6.25, 0.001));
        }
    }
}
