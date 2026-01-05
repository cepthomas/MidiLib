using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    /// <summary>Encode device/channel info for round trip through script.</summary>
    public static class HandleOps
    {
        const int OUTPUT_FLAG = 0x0800;

        public static int Create(int deviceId, int channelNumber, bool output)
        {
            return (deviceId << 4) | channelNumber | (output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        public static int DeviceId(int handle) { return (handle >> 4) & 0x0F; }
        public static int ChannelNumber(int handle) { return handle & 0x0F; }
        public static bool Output(int handle) { return (handle & OUTPUT_FLAG) > 0; }

        /// <summary>See me.</summary>
        public static string Format(int handle)
        {
            return $"{(Output(handle) ? "out" : "in")} {ChannelNumber(handle)}{Environment.NewLine}on D{DeviceId(handle)}";
        }
    }

    //----------------------------------------------------------------
    /// <summary>Describes one midi input channel.</summary>
    public class InputChannel
    {
        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { if (value is < 1 or > MidiDefs.NUM_CHANNELS) throw new ArgumentOutOfRangeException(nameof(value));
                _channelNumber = value; }
        }
        int _channelNumber;

        /// <summary>Associated device.</summary>
        public IInputDevice Device { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public InputChannel(IInputDevice device, int channelNumber)
        {
            Device = device;
            ChannelNumber = channelNumber;
            Handle = HandleOps.Create(device.Id, ChannelNumber, false);
        }
    }

    //----------------------------------------------------------------
    /// <summary>Describes one midi output channel.</summary>
    public class OutputChannel
    {
        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { if (value is < 1 or > MidiDefs.NUM_CHANNELS) throw new ArgumentOutOfRangeException(nameof(value));
                    _channelNumber = value; }
        }
        int _channelNumber;

        /// <summary>Override default instrument list.</summary>
        public string InstrumentFile
        {
            //get { return _aliasFile; }
            //set { _aliasFile = value; LoadAliases(); }
            set { _instrumentFile = value; LoadInstrumentFile(); }
        }
        string _instrumentFile = "";

        // /// <summary>Instrument aliases - optional.</summary>
        // Dictionary<int, string> _aliases = [];

        /// <summary>All the possibl instruments. May be GM, from alias file, drum kits, ...</summary>
        Dictionary<int, string> _instruments = [];



        /// <summary>Current instrument/patch number. Set sends a midi patch message.</summary>
        public int Patch
        {
            get { return _patch; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException(nameof(value));
                  _patch = value;  Device.Send(new Patch(ChannelNumber, _patch, MusicTime.ZERO)); }
        }
        int _patch = 0;

        /// <summary>Current instrument/patch name. Set sends a midi patch message.</summary>
        public string PatchName
        {
            get { return GetInstrumentName(_patch); }
            set { Patch = GetInstrumentId(value); }
        }


        ////TODO1 decode/process patch >>> Patch
        //int i = GetInstrumentId(patch);
        //if (i< 0) { throw new ArgumentException($"Invalid patch: {patch}");
        //}

        //Patch = i;

        ///// <summary>
        ///// Get patch name.
        ///// </summary>
        ///// <param name="which"></param>
        ///// <returns>The name or a fabricated one if unknown.</returns>
        //public string GetPatchName(int which)
        //{
        //    //string res;
        //    return GetInstrumentName(which);


        //    //if (IsDrums)
        //    //{
        //    //    res = MidiDefs.GetDrumKitName(which);
        //    //}
        //    //else
        //    //{
        //    //    res = _aliases.Any() ?
        //    //            _aliases.TryGetValue(which, out string? value) ? value : $"INST_{which}" :
        //    //            MidiDefs.GetInstrumentName(which);
        //    //}
        //    //return res;
        //}





        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0, VolumeDefs.MAX_VOLUME); }
        }
        double _volume = VolumeDefs.DEFAULT_VOLUME;

        /// <summary>Associated device.</summary>
        public IOutputDevice Device { get; init; }

        /// <summary>Associated events - optional depending on implementation.</summary>
        public EventCollection Events { get; set; } = new();

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }

        /// <summary>Meta info only client knows. TODO1 all need to do this!!! General solution? </summary>
        public bool IsDrums { get; set; } = false;

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;
        #endregion

        /// <summary>
        /// Get instrument name.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The instrument name or a fabricated one if unknown.</returns>
        public string GetInstrumentName(int id)
        {
            if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }

            return _instruments.TryGetValue(id, out string? value) ? value : $"INST_{id}";
        }

        ///// <summary>
        ///// Get GM drum kit name. Throws if invalid.
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns>The drumkit name or a fabricated one if unknown.</returns>
        //public string GetDrumKitName(int id)
        //{
        //    if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }

        //    return _drumKits.TryGetValue(id, out string? value) ? value : $"DKIT_{id}";
        //}

        /// <summary>
        /// Get corresponding number.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetInstrumentId(string name) // TODO1 handle Channel aliases
        {
            var i = _instruments.Where(v => v.Value == name);
            return i.Any() ? i.First().Key : -1;
        }

        ///// <summary>
        ///// Get corresponding number.
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public int GetDrumKitId(string name)
        //{
        //    var i = _drumKits.Where(v => v.Value == name);
        //    return i.Any() ? i.First().Key : -1;
        //}






        #region Lifecycle
        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public OutputChannel(IOutputDevice device, int channelNumber)
        {
            Device = device;
            ChannelNumber = channelNumber;
            Volume = VolumeDefs.DEFAULT_VOLUME;
            Handle = HandleOps.Create(device.Id, ChannelNumber, true);

            var ir = new IniReader();
            ir.ParseString(Properties.Resources.gm_defs);
            ir.GetValues("instruments").ForEach(kv => { _instruments[int.Parse(kv.Key)] = kv.Value; });
        }

        #endregion



        /// <summary>Load instrument names.</summary>
        void LoadInstrumentFile()
        {
            _instruments.Clear();

            try
            {
                var ir = new IniReader();
                ir.ParseFile(_instrumentFile);

                var defs = ir.GetValues("instruments");

                defs.ForEach(kv =>
                {
                    int id = int.Parse(kv.Key); // can throw
                    if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }
                    if (kv.Value.Length == 0) { throw new ArgumentOutOfRangeException($"{id} has no value"); }

                    _instruments.Add(id, kv.Value);
                });
            }
            catch (Exception ex)
            {
                throw new MidiLibException($"Failed to load instruments file {_instrumentFile}: {ex.Message}");
            }
        }




        ///// <summary>Load aliases.</summary>
        //void LoadAliases()
        //{
        //    _aliases.Clear();

        //    // Alternate instrument names?
        //    if (_aliasFile != "")
        //    {
        //        try
        //        {
        //            var ir = new IniReader();
        //            ir.ParseFile(_aliasFile);

        //            var defs = ir.GetValues("instruments");

        //            defs.ForEach(kv =>
        //            {
        //                int id = int.Parse(kv.Key); // can throw
        //                if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }
        //                if (kv.Value.Length == 0) { throw new ArgumentOutOfRangeException($"{id} has no value"); }

        //                _aliases.Add(id, kv.Value);
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new MidiLibException($"Failed to load alias file {_aliasFile}: {ex.Message}");
        //        }
        //    }
        //}
    }
}
