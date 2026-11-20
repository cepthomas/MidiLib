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
        readonly string myPath = MiscUtils.GetSourcePath();

        public override void RunSuite()
        {
            StopOnFail(true);

            var smd = MidiDefs.GenMarkdown();
            var fnOut = Path.Join(myPath, "midi_defs.md");
            File.WriteAllText(fnOut, string.Join(Environment.NewLine, smd));

            var sld = MidiDefs.GenLua();
            fnOut = Path.Join(myPath, "midi_defs.lua");
            File.WriteAllText(fnOut, string.Join(Environment.NewLine, sld));

            var sdi = MidiDefs.GenUserDeviceInfo();
            Assert(sdi.Count == 13); // YMMV
        }
    }

    //----------------------------------------------------------------
    /// <summary>Test midi IO logic.</summary>
    public class MIDILIB_INPUT_OUTPUT : TestSuite
    {
        readonly string myPath = MiscUtils.GetSourcePath();

        public override void RunSuite()
        {
            StopOnFail(true);

            MidiManager MM = new();

            // Dummy device.
            var outdev = "nullout:test1";
            var indev = "nullin:test1";
            BaseEvent? sent = null;
            MM.MessageSent += (sender, e) => sent = e;
            BaseEvent? revt = null;
            MM.MessageReceived += (sender, e) => revt = e;

            // Input
            var inp1 = MM.OpenMidiInput(indev, 1, "my input");

            ///// Named instrument mode
            // 1 is GM instruments
            var outp1 = MM.OpenMidiOutput(outdev, 1, "keys");
            MM.SetPatch(outp1, MidiDefs.InstrumentId.HonkyTonkPiano);
            // 2 is GM drums1
            var outp2= MM.OpenMidiOutput(outdev, 10, "syndrums");
            MM.SetPatch(outp2, MidiDefs.DrumKitId.Electronic);
            // 3 is Alt instruments
            var outp3 = MM.OpenMidiOutput(outdev, 4, "woofer");
            MM.SetPatch(outp3, KustomInstrumentId.WaterWhistle1);

            Assert(outp1.CurrentPatch!.ToString() == "HonkyTonkPiano");
            Assert(outp2.CurrentPatch!.ToString() == "Electronic");
            Assert(outp3.CurrentPatch!.ToString() == "WaterWhistle1");

            // Last sent midi patch.
            Patch pevt = (sent as Patch)!;
            Assert(pevt.MidiChannel == outp3.MidiChannel);
            Assert(pevt.Id.ToString() == "WaterWhistle1");
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
            Assert(mt.Tick == 23 * MusicTime.SubbeatsPerBar + 2 * MusicTime.SubbeatsPerBeat + 6);

            mt = new MusicTime("146.1");
            Assert(mt.Tick == 146 * MusicTime.SubbeatsPerBar + 1 * MusicTime.SubbeatsPerBeat);

            mt = new MusicTime("71");
            Assert(mt.Tick == 71 * MusicTime.SubbeatsPerBar);

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
}
