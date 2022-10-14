using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Readable versions of midi numbers.</summary>
    public class MidiDefs
    {
        #region Constants - midi spec
        /// <summary>Midi caps.</summary>
        public const int MIN_MIDI = 0;

        /// <summary>Midi caps.</summary>
        public const int MAX_MIDI = 127;

        /// <summary>Midi caps.</summary>
        public const int NUM_CHANNELS = 16;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;
        #endregion

        #region Fields
        /// <summary>Reverse lookup.</summary>
        static readonly Dictionary<string, int> _instrumentNumbers = new();

        /// <summary>Reverse lookup.</summary>
        static readonly Dictionary<string, int> _drumKitNumbers = new();

        /// <summary>Reverse lookup.</summary>
        static readonly Dictionary<string, int> _drumNumbers = new();

        /// <summary>Reverse lookup.</summary>
        static readonly Dictionary<string, int> _controllerNumbers = new();
        #endregion

        /// <summary>
        /// Initialize some collections.
        /// </summary>
        static MidiDefs()
        {
            _instrumentNumbers = _instruments.ToDictionary(x => x, x => _instruments.IndexOf(x));
            _drumKitNumbers = _drumKits.ToDictionary(x => x.Value, x => x.Key);
            _drumNumbers = _drums.ToDictionary(x => x.Value, x => x.Key);
            _controllerNumbers = _controllers.ToDictionary(x => x.Value, x => x.Key);
        }

        /// <summary>
        /// Make markdown content from the definitions.
        /// </summary>
        /// <returns>Markdown content.</returns>
        public static List<string> FormatDoc()
        {
            List<string> docs = new();
            docs.Add("# Midi GM Instruments");
            docs.Add("Instrument          | Number");
            docs.Add("----------          | ------");
            Enumerable.Range(0, _instruments.Count).ForEach(i => docs.Add($"{_instruments[i]}|{i}"));
            docs.Add("# Midi GM Drums");
            docs.Add("Drum                | Number");
            docs.Add("----                | ------");
            _drums.ForEach(kv => docs.Add($"{kv.Value}|{kv.Key}"));
            docs.Add("# Midi GM Controllers");
            docs.Add("- Undefined: 3, 9, 14-15, 20-31, 85-90, 102-119");
            docs.Add("- For most controllers marked on/off, on=127 and off=0");
            docs.Add("Controller          | Number");
            docs.Add("----------          | ------");
            _controllers.ForEach(kv => docs.Add($"{kv.Value}|{kv.Key}"));
            docs.Add("# Midi GM Drum Kits");
            docs.Add("Note that these will vary depending on your Soundfont file.");
            docs.Add("Kit        | Number");
            docs.Add("-----------| ------");
            _drumKits.ForEach(kv => docs.Add($"{kv.Value}|{kv.Key}"));

            return docs;
        }

        #region API
        /// <summary>
        /// Get patch name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The name.</returns>
        public static string GetInstrumentName(int which)
        {
            string ret = which switch
            {
                -1 => "NoPatch",
                >= 0 and < MAX_MIDI => _instruments[which],
                _ => throw new ArgumentOutOfRangeException(nameof(which)),
            };
            return ret;
        }

        /// <summary>
        /// Get the instrument/patch number.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The midi number or -1 if invalid.</returns>
        public static int GetInstrumentNumber(string which)
        {
            if (_instrumentNumbers.ContainsKey(which))
            {
                return _instrumentNumbers[which];
            }
            //throw new ArgumentException($"Invalid instrument: {which}");
            return -1;
        }

        /// <summary>
        /// Get drum name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The drum name or a fabricated one if unknown.</returns>
        public static string GetDrumName(int which)
        {
            return _drums.ContainsKey(which) ? _drums[which] : $"DRUM_{which}";
        }

        /// <summary>
        /// Get drum number.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The midi number or -1 if invalid.</returns>
        public static int GetDrumNumber(string which)
        {
            if (_drumNumbers.ContainsKey(which))
            {
                return _drumNumbers[which];
            }
            //throw new ArgumentException($"Invalid drum: {which}");
            return -1;
        }

        /// <summary>
        /// Get controller name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The controller name or a fabricated one if unknown.</returns>
        public static string GetControllerName(int which)
        {
            return _controllers.ContainsKey(which) ? _controllers[which] : $"CTLR_{which}";
        }

        /// <summary>
        /// Get the controller number.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The midi number or -1 if invalid.</returns>
        public static int GetControllerNumber(string which)
        {
            if (_controllerNumbers.ContainsKey(which))
            {
                return _controllerNumbers[which];
            }
            //throw new ArgumentException($"Invalid controller: {which}");
            return -1;
        }

        /// <summary>
        /// Get GM drum kit name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The drumkit name or a fabricated one if unknown.</returns>
        public static string GetDrumKitName(int which)
        {
            return _drumKits.ContainsKey(which) ? _drumKits[which] : $"KIT_{which}";
        }

        /// <summary>
        /// Get GM drum kit number.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The midi number or -1 if invalid.</returns>
        public static int GetDrumKitNumber(string which)
        {
            if(_drumKitNumbers.ContainsKey(which))
            {
                return _drumKitNumbers[which];
            }
            //throw new ArgumentException($"Invalid drum kit: {which}");
            return -1;
        }

        /// <summary>
        /// Get the instrument/patch or drum number.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The midi number or -1 if invalid.</returns>
        public static int GetInstrumentOrDrumKitNumber(string which)
        {
            if (_instrumentNumbers.ContainsKey(which))
            {
                return _instrumentNumbers[which];
            }
            else if (_drumKitNumbers.ContainsKey(which))
            {
                return _drumKitNumbers[which];
            }
            //throw new ArgumentException($"Invalid instrument or drum: {which}");
            return -1;
        }
        #endregion

        #region All the names
        /// <summary>The GM midi instrument definitions.</summary>
        static readonly List<string> _instruments = new()
        {
            "AcousticGrandPiano", "BrightAcousticPiano", "ElectricGrandPiano", "HonkyTonkPiano", "ElectricPiano1", "ElectricPiano2", "Harpsichord",
            "Clavinet", "Celesta", "Glockenspiel", "MusicBox", "Vibraphone", "Marimba", "Xylophone", "TubularBells", "Dulcimer", "DrawbarOrgan",
            "PercussiveOrgan", "RockOrgan", "ChurchOrgan", "ReedOrgan", "Accordion", "Harmonica", "TangoAccordion", "AcousticGuitarNylon",
            "AcousticGuitarSteel", "ElectricGuitarJazz", "ElectricGuitarClean", "ElectricGuitarMuted", "OverdrivenGuitar", "DistortionGuitar",
            "GuitarHarmonics", "AcousticBass", "ElectricBassFinger", "ElectricBassPick", "FretlessBass", "SlapBass1", "SlapBass2", "SynthBass1",
            "SynthBass2", "Violin", "Viola", "Cello", "Contrabass", "TremoloStrings", "PizzicatoStrings", "OrchestralHarp", "Timpani",
            "StringEnsemble1", "StringEnsemble2", "SynthStrings1", "SynthStrings2", "ChoirAahs", "VoiceOohs", "SynthVoice", "OrchestraHit",
            "Trumpet", "Trombone", "Tuba", "MutedTrumpet", "FrenchHorn", "BrassSection", "SynthBrass1", "SynthBrass2", "SopranoSax", "AltoSax",
            "TenorSax", "BaritoneSax", "Oboe", "EnglishHorn", "Bassoon", "Clarinet", "Piccolo", "Flute", "Recorder", "PanFlute", "BlownBottle",
            "Shakuhachi", "Whistle", "Ocarina", "Lead1Square", "Lead2Sawtooth", "Lead3Calliope", "Lead4Chiff", "Lead5Charang", "Lead6Voice",
            "Lead7Fifths", "Lead8BassAndLead", "Pad1NewAge", "Pad2Warm", "Pad3Polysynth", "Pad4Choir", "Pad5Bowed", "Pad6Metallic", "Pad7Halo",
            "Pad8Sweep", "Fx1Rain", "Fx2Soundtrack", "Fx3Crystal", "Fx4Atmosphere", "Fx5Brightness", "Fx6Goblins", "Fx7Echoes", "Fx8SciFi",
            "Sitar", "Banjo", "Shamisen", "Koto", "Kalimba", "BagPipe", "Fiddle", "Shanai", "TinkleBell", "Agogo", "SteelDrums", "Woodblock",
            "TaikoDrum", "MelodicTom", "SynthDrum", "ReverseCymbal", "GuitarFretNoise", "BreathNoise", "Seashore", "BirdTweet", "TelephoneRing",
            "Helicopter", "Applause", "Gunshot"
        };

        /// <summary>The GM midi drum kit definitions.</summary>
        static readonly Dictionary<int, string> _drumKits = new()
        {
            { 0, "Standard" }, { 8, "Room" }, { 16, "Power" }, { 24, "Electronic" }, { 25, "TR808" },
            { 32, "Jazz" }, { 40, "Brush" }, { 48, "Orchestra" }, { 56, "SFX" }
        };

        /// <summary>The GM midi drum definitions.</summary>
        static readonly Dictionary<int, string> _drums = new()
        {
            { 035, "AcousticBassDrum" }, { 036, "BassDrum1" }, { 037, "SideStick" }, { 038, "AcousticSnare" }, { 039, "HandClap" }, 
            { 040, "ElectricSnare" }, { 041, "LowFloorTom" }, { 042, "ClosedHiHat" }, { 043, "HighFloorTom" }, { 044, "PedalHiHat" }, 
            { 045, "LowTom" }, { 046, "OpenHiHat" }, { 047, "LowMidTom" }, { 048, "HiMidTom" }, { 049, "CrashCymbal1" }, 
            { 050, "HighTom" }, { 051, "RideCymbal1" }, { 052, "ChineseCymbal" }, { 053, "RideBell" }, { 054, "Tambourine" }, 
            { 055, "SplashCymbal" }, { 056, "Cowbell" }, { 057, "CrashCymbal2" }, { 058, "Vibraslap" }, { 059, "RideCymbal2" }, 
            { 060, "HiBongo" }, { 061, "LowBongo" }, { 062, "MuteHiConga" }, { 063, "OpenHiConga" }, { 064, "LowConga" }, 
            { 065, "HighTimbale" }, { 066, "LowTimbale" }, { 067, "HighAgogo" }, { 068, "LowAgogo" }, { 069, "Cabasa" }, 
            { 070, "Maracas" }, { 071, "ShortWhistle" }, { 072, "LongWhistle" }, { 073, "ShortGuiro" }, { 074, "LongGuiro" }, 
            { 075, "Claves" }, { 076, "HiWoodBlock" }, { 077, "LowWoodBlock" }, { 078, "MuteCuica" }, { 079, "OpenCuica" }, 
            { 080, "MuteTriangle" }, { 081, "OpenTriangle" }
        };

        /// <summary>The midi controller definitions.</summary>
        static readonly Dictionary<int, string> _controllers = new()
        {
            { 000, "BankSelect" }, { 001, "Modulation" }, { 002, "BreathController" }, { 004, "FootController" }, { 005, "PortamentoTime" }, 
            { 007, "Volume" }, { 008, "Balance" }, { 010, "Pan" }, { 011, "Expression" }, { 032, "BankSelectLSB" }, { 033, "ModulationLSB" }, 
            { 034, "BreathControllerLSB" }, { 036, "FootControllerLSB" }, { 037, "PortamentoTimeLSB" }, { 039, "VolumeLSB" }, 
            { 040, "BalanceLSB" }, { 042, "PanLSB" }, { 043, "ExpressionLSB" }, { 064, "Sustain" }, { 065, "Portamento" }, { 066, "Sostenuto" }, 
            { 067, "SoftPedal" }, { 068, "Legato" }, { 069, "Sustain2" }, { 084, "PortamentoControl" }, { 120, "AllSoundOff" }, 
            { 121, "ResetAllControllers" }, { 122, "LocalKeyboard" }, { 123, "AllNotesOff" }
        };
        #endregion
    }
}    