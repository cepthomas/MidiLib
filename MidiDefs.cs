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

        /// <summary>Doesn't have a specific channel.</summary>
        public const int ALL_CHANNEL = 0xFF;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;
        #endregion

        #region Standard GM defs
        public enum InstrumentId
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

        public enum DrumKitId
        {
            Standard = 0, Room = 8, Power = 16, Electronic = 24, TR808 = 25, Jazz = 32, Brush = 40, Orchestra = 48, SFX = 56
        }

        public enum ControllerId
        {
            BankSelect = 000, Modulation = 001, BreathController = 002, FootController = 004, PortamentoTime = 005, Volume = 007,
            Balance = 008, Pan = 010, Expression = 011, BankSelectLSB = 032, ModulationLSB = 033, BreathControllerLSB = 034,
            FootControllerLSB = 036, PortamentoTimeLSB = 037, VolumeLSB = 039, BalanceLSB = 040, PanLSB = 042, ExpressionLSB = 043,
            Sustain = 064, Portamento = 065, Sostenuto = 066, SoftPedal = 067, Legato = 068, Sustain2 = 069, PortamentoControl = 084,
            AllSoundOff = 120, ResetAllControllers = 121, LocalKeyboard = 122, AllNotesOff = 123,
            // NAudio names: MainVolume = 7, LegatoFootswitch = 68,
            };

        public enum DrumId
        {
            AcousticBassDrum = 035, BassDrum1 = 036, SideStick = 037, AcousticSnare = 038, HandClap = 039, ElectricSnare = 040,
            LowFloorTom = 041, ClosedHiHat = 042, HighFloorTom = 043, PedalHiHat = 044, LowTom = 045, OpenHiHat = 046, LowMidTom = 047,
            HiMidTom = 048, CrashCymbal1 = 049, HighTom = 050, RideCymbal1 = 051, ChineseCymbal = 052, RideBell = 053, Tambourine = 054,
            SplashCymbal = 055, Cowbell = 056, CrashCymbal2 = 057, Vibraslap = 058, RideCymbal2 = 059, HiBongo = 060, LowBongo = 061,
            MuteHiConga = 062, OpenHiConga = 063, LowConga = 064, HighTimbale = 065, LowTimbale = 066, HighAgogo = 067, LowAgogo = 068,
            Cabasa = 069, Maracas = 070, ShortWhistle = 071, LongWhistle = 072, ShortGuiro = 073, LongGuiro = 074, Claves = 075,
            HiWoodBlock = 076, LowWoodBlock = 077, MuteCuica = 078, OpenCuica = 079, MuteTriangle = 080, OpenTriangle = 081,
        };
        #endregion

        #region Utilities
        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public static List<string> GenMarkdown()
        {
            List<string> ls = [];
            ls.Add("# Midi GM Instruments");
            ls.Add("|Instrument   | Number|");
            ls.Add("|----------   | ------|");
            Enum.GetValues<InstrumentId>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
            ls.Add("");

            ls.Add("# Midi GM Controllers");
            ls.Add("- Undefined: 3, 9, 14-15, 20-31, 85-90, 102-119");
            ls.Add("- For most controllers marked on/off, on=127 and off=0");
            ls.Add("|Controller   | Number|");
            ls.Add("|----------   | ------|");
            Enum.GetValues<ControllerId>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drums");
            ls.Add("- These may vary depending on your Soundfont file.");
            ls.Add("|Drum         | Number|");
            ls.Add("|----         | ------|");
            Enum.GetValues<DrumId>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drum Kits");
            ls.Add("- These may vary depending on your Soundfont file.");
            ls.Add("|Kit          | Number|");
            ls.Add("|---          | ------|");
            Enum.GetValues<DrumKitId>().ForEach(v => { ls.Add($"|{v}|{(int)v}|"); });
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
            Enum.GetValues<InstrumentId>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Controllers");
            ls.Add("M.controllers =");
            ls.Add("{");
            Enum.GetValues<ControllerId>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Drums");
            ls.Add("M.drums =");
            ls.Add("{");
            Enum.GetValues<DrumId>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Drum kits");
            ls.Add("M.drum_kits =");
            ls.Add("{");
            Enum.GetValues<DrumKitId>().ForEach(v => { ls.Add($"    {v} = {(int)v},"); });
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
