using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    public class MidiDefs
    {
        #region Fields
        /// <summary>Midi constant.</summary>
        public const int MAX_MIDI = 127;

        /// <summary>Per device.</summary>
        public const int MAX_CHANNEL = 16;

        /// <summary>Per device.</summary>
        public const int MIN_CHANNEL = 1;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;
        #endregion

        //#region Properties
        ///// <summary>All the GM instruments.</summary>
        //public static BiLut Instruments { get; } = new();

        ///// <summary>All the common/known GM controllers.</summary>
        //public static BiLut Controllers { get; } = new();

        ///// <summary>All the GM drums.</summary>
        //public static BiLut Drums { get; } = new();

        ///// <summary>All the GM drum kits.</summary>
        //public static BiLut DrumKits { get; } = new();
        //#endregion


        public enum Instrument
        {
            AcousticGrandPiano, BrightAcousticPiano, ElectricGrandPiano, HonkyTonkPiano, ElectricPiano1, ElectricPiano2, Harpsichord,
            Clavinet, Celesta, Glockenspiel, MusicBox, Vibraphone, Marimba, Xylophone, TubularBells, Dulcimer, DrawbarOrgan,
            PercussiveOrgan, RockOrgan, ChurchOrgan, ReedOrgan, Accordion, Harmonica, TangoAccordion, AcousticGuitarNylon,
            AcousticGuitarSteel, ElectricGuitarJazz, ElectricGuitarClean, ElectricGuitarMuted, OverdrivenGuitar, DistortionGuitar,
            GuitarHarmonics, AcousticBass, ElectricBassFinger, ElectricBassPick, FretlessBass, SlapBass1, SlapBass2, SynthBass1,
            SynthBass2, Violin, Viola, Cello, Contrabass, TremoloStrings, PizzicatoStrings, OrchestralHarp, Timpani,
            StringEnsemble1, StringEnsemble2, SynthStrings1, SynthStrings2, ChoirAahs, VoiceOohs, SynthVoice, OrchestraHit,
            Trumpet, Trombone, Tuba, MutedTrumpet, FrenchHorn, BrassSection, SynthBrass1, SynthBrass2, SopranoSax, AltoSax,
            TenorSax, BaritoneSax, Oboe, EnglishHorn, Bassoon, Clarinet, Piccolo, Flute, Recorder, PanFlute, BlownBottle,
            Shakuhachi, Whistle, Ocarina, Lead1Square, Lead2Sawtooth, Lead3Calliope, Lead4Chiff, Lead5Charang, Lead6Voice,
            Lead7Fifths, Lead8BassAndLead, Pad1NewAge, Pad2Warm, Pad3Polysynth, Pad4Choir, Pad5Bowed, Pad6Metallic, Pad7Halo,
            Pad8Sweep, Fx1Rain, Fx2Soundtrack, Fx3Crystal, Fx4Atmosphere, Fx5Brightness, Fx6Goblins, Fx7Echoes, Fx8SciFi,
            Sitar, Banjo, Shamisen, Koto, Kalimba, BagPipe, Fiddle, Shanai, TinkleBell, Agogo, SteelDrums, Woodblock,
            TaikoDrum, MelodicTom, SynthDrum, ReverseCymbal, GuitarFretNoise, BreathNoise, Seashore, BirdTweet, TelephoneRing,
            Helicopter, Applause, Gunshot
        }

        public enum DrumKit
        {
            Standard = 0, Room = 8, Power = 16, Electronic = 24, TR808 = 25, Jazz = 32, Brush = 40, Orchestra = 48, SFX = 56
        }

        public enum Controller
        {
            BankSelect = 000, Modulation = 001, BreathController = 002, FootController = 004, PortamentoTime = 005, Volume = 007,
            Balance = 008, Pan = 010, Expression = 011, BankSelectLSB = 032, ModulationLSB = 033, BreathControllerLSB = 034,
            FootControllerLSB = 036, PortamentoTimeLSB = 037, VolumeLSB = 039, BalanceLSB = 040, PanLSB = 042, ExpressionLSB = 043,
            Sustain = 064, Portamento = 065, Sostenuto = 066, SoftPedal = 067, Legato = 068, Sustain2 = 069, PortamentoControl = 084,
            AllSoundOff = 120, ResetAllControllers = 121, LocalKeyboard = 122, AllNotesOff = 123,
        };


        public enum Drum
        {
            AcousticBassDrum = 035, BassDrum1 = 036, SideStick = 037, AcousticSnare = 038, HandClap = 039, ElectricSnare = 040,
            LowFloorTom = 041, ClosedHiHat = 042, HighFloorTom = 043, PedalHiHat = 044, LowTom = 045, OpenHiHat = 046, LowMidTom = 047,
            HiMidTom = 048, CrashCymbal1 = 049, HighTom = 050, RideCymbal1 = 051, ChineseCymbal = 052, RideBell = 053, Tambourine = 054,
            SplashCymbal = 055, Cowbell = 056, CrashCymbal2 = 057, Vibraslap = 058, RideCymbal2 = 059, HiBongo = 060, LowBongo = 061,
            MuteHiConga = 062, OpenHiConga = 063, LowConga = 064, HighTimbale = 065, LowTimbale = 066, HighAgogo = 067, LowAgogo = 068,
            Cabasa = 069, Maracas = 070, ShortWhistle = 071, LongWhistle = 072, ShortGuiro = 073, LongGuiro = 074, Claves = 075,
            HiWoodBlock = 076, LowWoodBlock = 077, MuteCuica = 078, OpenCuica = 079, MuteTriangle = 080, OpenTriangle = 081,
        };


        #region =====================================================================
        // /// <summary>The GM midi instrument definitions.</summary>
        // static readonly List<string> _instruments = new()
        // {
        //     "AcousticGrandPiano", "BrightAcousticPiano", "ElectricGrandPiano", "HonkyTonkPiano", "ElectricPiano1", "ElectricPiano2", "Harpsichord",
        //     "Clavinet", "Celesta", "Glockenspiel", "MusicBox", "Vibraphone", "Marimba", "Xylophone", "TubularBells", "Dulcimer", "DrawbarOrgan",
        //     "PercussiveOrgan", "RockOrgan", "ChurchOrgan", "ReedOrgan", "Accordion", "Harmonica", "TangoAccordion", "AcousticGuitarNylon",
        //     "AcousticGuitarSteel", "ElectricGuitarJazz", "ElectricGuitarClean", "ElectricGuitarMuted", "OverdrivenGuitar", "DistortionGuitar",
        //     "GuitarHarmonics", "AcousticBass", "ElectricBassFinger", "ElectricBassPick", "FretlessBass", "SlapBass1", "SlapBass2", "SynthBass1",
        //     "SynthBass2", "Violin", "Viola", "Cello", "Contrabass", "TremoloStrings", "PizzicatoStrings", "OrchestralHarp", "Timpani",
        //     "StringEnsemble1", "StringEnsemble2", "SynthStrings1", "SynthStrings2", "ChoirAahs", "VoiceOohs", "SynthVoice", "OrchestraHit",
        //     "Trumpet", "Trombone", "Tuba", "MutedTrumpet", "FrenchHorn", "BrassSection", "SynthBrass1", "SynthBrass2", "SopranoSax", "AltoSax",
        //     "TenorSax", "BaritoneSax", "Oboe", "EnglishHorn", "Bassoon", "Clarinet", "Piccolo", "Flute", "Recorder", "PanFlute", "BlownBottle",
        //     "Shakuhachi", "Whistle", "Ocarina", "Lead1Square", "Lead2Sawtooth", "Lead3Calliope", "Lead4Chiff", "Lead5Charang", "Lead6Voice",
        //     "Lead7Fifths", "Lead8BassAndLead", "Pad1NewAge", "Pad2Warm", "Pad3Polysynth", "Pad4Choir", "Pad5Bowed", "Pad6Metallic", "Pad7Halo",
        //     "Pad8Sweep", "Fx1Rain", "Fx2Soundtrack", "Fx3Crystal", "Fx4Atmosphere", "Fx5Brightness", "Fx6Goblins", "Fx7Echoes", "Fx8SciFi",
        //     "Sitar", "Banjo", "Shamisen", "Koto", "Kalimba", "BagPipe", "Fiddle", "Shanai", "TinkleBell", "Agogo", "SteelDrums", "Woodblock",
        //     "TaikoDrum", "MelodicTom", "SynthDrum", "ReverseCymbal", "GuitarFretNoise", "BreathNoise", "Seashore", "BirdTweet", "TelephoneRing",
        //     "Helicopter", "Applause", "Gunshot"
        // };

        // /// <summary>The GM midi drum kit definitions.</summary>
        // static readonly Dictionary<int, string> _drumKits = new()
        // {
        //     { 0, "Standard" }, { 8, "Room" }, { 16, "Power" }, { 24, "Electronic" }, { 25, "TR808" },
        //     { 32, "Jazz" }, { 40, "Brush" }, { 48, "Orchestra" }, { 56, "SFX" }
        // };

        // /// <summary>The GM midi drum definitions.</summary>
        // static readonly Dictionary<int, string> _drums = new()
        // {
        //     { 035, "AcousticBassDrum" }, { 036, "BassDrum1" }, { 037, "SideStick" }, { 038, "AcousticSnare" }, { 039, "HandClap" },
        //     { 040, "ElectricSnare" }, { 041, "LowFloorTom" }, { 042, "ClosedHiHat" }, { 043, "HighFloorTom" }, { 044, "PedalHiHat" },
        //     { 045, "LowTom" }, { 046, "OpenHiHat" }, { 047, "LowMidTom" }, { 048, "HiMidTom" }, { 049, "CrashCymbal1" },
        //     { 050, "HighTom" }, { 051, "RideCymbal1" }, { 052, "ChineseCymbal" }, { 053, "RideBell" }, { 054, "Tambourine" },
        //     { 055, "SplashCymbal" }, { 056, "Cowbell" }, { 057, "CrashCymbal2" }, { 058, "Vibraslap" }, { 059, "RideCymbal2" },
        //     { 060, "HiBongo" }, { 061, "LowBongo" }, { 062, "MuteHiConga" }, { 063, "OpenHiConga" }, { 064, "LowConga" },
        //     { 065, "HighTimbale" }, { 066, "LowTimbale" }, { 067, "HighAgogo" }, { 068, "LowAgogo" }, { 069, "Cabasa" },
        //     { 070, "Maracas" }, { 071, "ShortWhistle" }, { 072, "LongWhistle" }, { 073, "ShortGuiro" }, { 074, "LongGuiro" },
        //     { 075, "Claves" }, { 076, "HiWoodBlock" }, { 077, "LowWoodBlock" }, { 078, "MuteCuica" }, { 079, "OpenCuica" },
        //     { 080, "MuteTriangle" }, { 081, "OpenTriangle" }
        // };

        // /// <summary>The midi controller definitions.</summary>
        // static readonly Dictionary<int, string> _controllers = new()
        // {
        //     { 000, "BankSelect" }, { 001, "Modulation" }, { 002, "BreathController" }, { 004, "FootController" }, { 005, "PortamentoTime" },
        //     { 007, "Volume" }, { 008, "Balance" }, { 010, "Pan" }, { 011, "Expression" }, { 032, "BankSelectLSB" }, { 033, "ModulationLSB" },
        //     { 034, "BreathControllerLSB" }, { 036, "FootControllerLSB" }, { 037, "PortamentoTimeLSB" }, { 039, "VolumeLSB" },
        //     { 040, "BalanceLSB" }, { 042, "PanLSB" }, { 043, "ExpressionLSB" }, { 064, "Sustain" }, { 065, "Portamento" }, { 066, "Sostenuto" },
        //     { 067, "SoftPedal" }, { 068, "Legato" }, { 069, "Sustain2" }, { 084, "PortamentoControl" }, { 120, "AllSoundOff" },
        //     { 121, "ResetAllControllers" }, { 122, "LocalKeyboard" }, { 123, "AllNotesOff" }
        // };




        // /// <summary>Reverse lookup.</summary>
        // Dictionary<string, int> _instrumentNumbers = new();
        // //static readonly Dictionary<string, int> _instrumentNumbers = new();

        // /// <summary>Reverse lookup.</summary>
        // Dictionary<string, int> _drumKitNumbers = new();
        // //static readonly Dictionary<string, int> _drumKitNumbers = new();

        // /// <summary>Reverse lookup.</summary>
        // Dictionary<string, int> _drumNumbers = new();
        // //static readonly Dictionary<string, int> _drumNumbers = new();

        // /// <summary>Reverse lookup.</summary>
        // Dictionary<string, int> _controllerNumbers = new();
        // //static readonly Dictionary<string, int> _controllerNumbers = new();

        // void LoadThem()
        // {
        //     _instrumentNumbers = _instruments.ToDictionary(x => x, x => _instruments.IndexOf(x));
        //     _drumKitNumbers = _drumKits.ToDictionary(x => x.Value, x => x.Key);
        //     _drumNumbers = _drums.ToDictionary(x => x.Value, x => x.Key);
        //     _controllerNumbers = _controllers.ToDictionary(x => x.Value, x => x.Key);
        // }
        #endregion



        #region Lifecycle
        ///// <summary>Initialize some collections.</summary>
        //static MidiDefs()
        //{
        //    var ir = new IniReader();
        //    ir.ParseString(Properties.Resources.gm_defs);

        //    // Populate the defs.
        //    Instruments = ReadDefs(ir, "instruments") ?? throw new ArgumentException("Missing instruments section");
        //    Controllers = ReadDefs(ir, "controllers") ?? throw new ArgumentException("Missing controllers section");
        //    Drums = ReadDefs(ir, "drums") ?? throw new ArgumentException("Missing drums section");
        //    DrumKits = ReadDefs(ir, "drumkits") ?? throw new ArgumentException("Missing drumkits section");
        //}
        #endregion

        #region Utilities TODO1 other home?
        ///// <summary>
        ///// Read one section of midi definitions.
        ///// </summary>
        ///// <param name="ir">Ini reader</param>
        ///// <param name="section">Section name</param>
        ///// <returns>The lut or null if invalid section.</returns>
        ///// <exception cref="InvalidOperationException"></exception>
        //public static BiLut? ReadDefs(IniReader ir, string section)
        //{
        //    BiLut? target = null;

        //    if (ir.GetSectionNames().Contains(section))
        //    {
        //        target = new();
        //        ir.GetValues(section).ForEach(kv =>
        //        {
        //            int index = int.Parse(kv.Key); // can throw
        //            if (index < 0 || index > MAX_MIDI) { throw new InvalidOperationException($"Invalid key {kv.Key} in section {section}"); }
        //            target.Add(index, kv.Value);
        //        });
        //    }

        //    return target;
        //}

        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public static List<string> GenMarkdown()
        {
            //foreach (var inst in Enum.GetValues<Instrument>())
            //{
            //    //string name = inst.ToString(); // Or nameof(...)
            //    //int value = (int)inst;
            //    Console.WriteLine($"Name: {inst}, Value: {(int)inst}");
            //}

            List<string> ls = [];
            ls.Add("# Midi GM Instruments");
            ls.Add("|Instrument   | Number|");
            ls.Add("|----------   | ------|");
            //Instruments.Contents.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            Enum.GetValues<Instrument>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
            ls.Add("");

            ls.Add("# Midi GM Controllers");
            ls.Add("- Undefined: 3, 9, 14-15, 20-31, 85-90, 102-119");
            ls.Add("- For most controllers marked on/off, on=127 and off=0");
            ls.Add("|Controller   | Number|");
            ls.Add("|----------   | ------|");
            Enum.GetValues<Controller>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
            //Controllers.Contents.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drums");
            ls.Add("- These may vary depending on your Soundfont file.");
            ls.Add("|Drum         | Number|");
            ls.Add("|----         | ------|");
            Enum.GetValues<Drum>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
            //Drums.Contents.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drum Kits");
            ls.Add("- These may vary depending on your Soundfont file.");
            ls.Add("|Kit          | Number|");
            ls.Add("|---          | ------|");
            Enum.GetValues<DrumKit>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
            //DrumKits.Contents.ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            return ls;
        }

        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public static List<string> GenLua()
        {
            List<string> ls = [];
            ls.Add("-------------- GM midi definitions ----------------");
            ls.Add("-- Autogenerated from midi_defs.ini -- do not edit!");

            ls.Add("");
            ls.Add("local M = {}");
            ls.Add("");
            ls.Add("M.MAX_MIDI = 127");
            ls.Add("M.NO_PATCH = -1");

            ls.Add("");
            ls.Add("-- Instruments");
            ls.Add("M.instruments =");
            ls.Add("{");
            //Instruments.Contents.ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            Enum.GetValues<Instrument>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Controllers");
            ls.Add("M.controllers =");
            ls.Add("{");
            //Controllers.Contents.ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            Enum.GetValues<Controller>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Drums");
            ls.Add("M.drums =");
            ls.Add("{");
            //Drums.Contents.ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            Enum.GetValues<Drum>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Drum kits");
            ls.Add("M.drum_kits =");
            ls.Add("{");
            //DrumKits.Contents.ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            Enum.GetValues<DrumKit>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
            ls.Add("}");

            ls.Add("");
            ls.Add("return M");

            return ls;
        }

        /// <summary>
        /// Make content from user configuration.
        /// </summary>
        /// <returns>Content.</returns>
        public static List<string> GenUserDeviceInfo()
        {
            // Show them what they have.
            var outs = MidiOutputDevice.GetAvailableDevices();
            var ins = MidiInputDevice.GetAvailableDevices();

            List<string> ls = [];
            ls.Add($"# Your Midi Devices");

            ls.Add($"## Inputs");
            if (ins.Count == 0) { ls.Add($"None"); }
            else { ins.ForEach(d => ls.Add($"[{d}]")); }

            ls.Add($"## Outputs");
            if (outs.Count == 0) { ls.Add($"None"); }
            else { outs.ForEach(d => ls.Add($"[{d}]")); }

            return ls;
        }
        #endregion
    }
}
