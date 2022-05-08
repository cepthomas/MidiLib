using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;


namespace MidiLib
{
    /// <summary>Readable versions of midi numbers.</summary>
    public class MidiDefs
    {
        /// <summary>Midi caps.</summary>
        public const int MAX_MIDI = 127;

        /// <summary>Midi caps.</summary>
        public const int NUM_CHANNELS = 16;

        /// <summary>all the notes.</summary>
        public const int NOTES_PER_OCTAVE = 12;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;

        /// <summary>
        /// Get patch name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns></returns>
        public static string GetInstrumentDef(int which)
        {
            if(which < 0 || which > MAX_MIDI)
            {
                throw new ArgumentOutOfRangeException(nameof(which));
            }

            return _instrumentNames[which];
        }

        /// <summary>
        /// Get drum name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns></returns>
        public static string GetDrumDef(int which)
        {
            if (which < 0 || which > MAX_MIDI)
            {
                throw new ArgumentOutOfRangeException(nameof(which));
            }

            return _drumNames[which];
        }

        /// <summary>
        /// Get controller name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns></returns>
        public static string GetControllerDef(int which)
        {
            if (which < 0 || which > MAX_MIDI)
            {
                throw new ArgumentOutOfRangeException(nameof(which));
            }

            return _controllerNames[which];
        }

        /// <summary>
        /// Convert note number into name.
        /// </summary>
        /// <param name="notenum"></param>
        /// <returns></returns>
        public static string NoteNumberToName(int notenum)
        {
            int root = notenum % NOTES_PER_OCTAVE;
            int octave = (notenum / NOTES_PER_OCTAVE) - 1;
            string s = $"{noteNames[root]}{octave}";
            return s;
        }

        #region The Names
        /// <summary>All the root notes.</summary>
        static readonly string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /// <summary>The GM midi instrument definitions.</summary>
        static readonly string[] _instrumentNames = new string[]
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
        static readonly string[] _drumKits = new string[]
        {
            "Standard" = 0, "Room" = 8, "Power" = 16, "Electronic" = 24, "TR808" = 25, "Jazz" = 32, "Brush" = 40, "Orchestra" = 48, "SFX" = 56
        };


        /// <summary>The GM midi drum definitions.</summary>
        static readonly string[] _drumNames = new string[]
        {
            "D000", "D001", "D002", "D003", "D004", "D005", "D006", "D007", "D008", "D009", "D010", "D011", "D012", "D013", "D014", "D015",
            "D016", "D017", "D018", "D019", "D020", "D021", "D022", "D023", "D024", "D025", "D026", "D027", "D028", "D029", "D030", "D031",
            "D032", "D033", "D034",
            "AcousticBassDrum", "BassDrum1", "SideStick", "AcousticSnare", "HandClap", "ElectricSnare", "LowFloorTom", "ClosedHiHat", "HighFloorTom",
            "PedalHiHat", "LowTom", "OpenHiHat", "LowMidTom", "HiMidTom", "CrashCymbal1", "HighTom", "RideCymbal1", "ChineseCymbal", "RideBell",
            "Tambourine", "SplashCymbal", "Cowbell", "CrashCymbal2", "Vibraslap", "RideCymbal2", "HiBongo", "LowBongo", "MuteHiConga", "OpenHiConga",
            "LowConga", "HighTimbale", "LowTimbale", "HighAgogo", "LowAgogo", "Cabasa", "Maracas", "ShortWhistle", "LongWhistle", "ShortGuiro",
            "LongGuiro", "Claves", "HiWoodBlock", "LowWoodBlock", "MuteCuica", "OpenCuica", "MuteTriangle", "OpenTriangle",
            "D082", "D083", "D084", "D085", "D086", "D087", "D088", "D089", "D090", "D091", "D092", "D093", "D094", "D095", "D096", "D097",
            "D098", "D099", "D100", "D101", "D102", "D103", "D104", "D105", "D106", "D107", "D108", "D109", "D110", "D111", "D112", "D113",
            "D114", "D115", "D116", "D117", "D118", "D119", "D120", "D121", "D122", "D123", "D124", "D125", "D126", "D127"
        };

        /// <summary>The midi controller definitions.</summary>
        static readonly string[] _controllerNames = new string[]
        {
            "BankSelect", "Modulation", "BreathController", "C003", "FootController", "PortamentoTime", "C006", "Volume", "Balance", "C009",
            "Pan", "Expression", "C012", "C013", "C014", "C015", "C016", "C017", "C018", "C019", "C020", "C021", "C022", "C023", "C024",
            "C025", "C026", "C027", "C028", "C029", "C030", "C031", "BankSelectLSB", "ModulationLSB", "BreathControllerLSB", "C035",
            "FootControllerLSB", "PortamentoTimeLSB", "C038", "VolumeLSB", "BalanceLSB", "C041", "PanLSB", "ExpressionLSB", "C044",
            "C045", "C046", "C047", "C048", "C049", "C050", "C051", "C052", "C053", "C054", "C055", "C056", "C057", "C058", "C059", "C060",
            "C061", "C062", "C063", "Sustain", "Portamento", "Sostenuto", "SoftPedal", "Legato", "Sustain2", "C070", "C071", "C072", "C073",
            "C074", "C075", "C076", "C077", "C078", "C079", "C080", "C081", "C082", "C083", "PortamentoControl", "C085", "C086", "C087", "C088",
            "C089", "C090", "C091", "C092", "C093", "C094", "C095", "C096", "C097", "C098", "C099", "C100", "C101", "C102", "C103", "C104",
            "C105", "C106", "C107", "C108", "C109", "C110", "C111", "C112", "C113", "C114", "C115", "C116", "C117", "C118", "C119", "AllSoundOff",
            "ResetAllControllers", "LocalKeyboard", "AllNotesOff", "C124", "C125", "C126", "C127", "C128", "C129"
        };
        #endregion
    }
}    