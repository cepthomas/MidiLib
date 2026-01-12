using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    #region Types
    /// <summary>Some channels have specialized behavior.</summary>
    //public enum ChannelFlavor { Normal, Drums }

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

        public static string Format(int handle)
        {
            return $"{(Output(handle) ? "out" : "in")} {ChannelNumber(handle)}{Environment.NewLine}on D{DeviceId(handle)}";
        }
    }
    #endregion

    //----------------------------------------------------------------
    /// <summary>Describes one midi input channel.</summary>
    public class InputChannel
    {
        #region Fields

        // Backing fields.
        int _channelNumber;
        #endregion

        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { if (value is < 1 or > MidiDefs.NUM_CHANNELS) throw new ArgumentOutOfRangeException($"ChannelNumber:{value}");
                  else _channelNumber = value; }
        }

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
        #region Fields
        /// <summary>Associated device.</summary>
        readonly IOutputDevice? _device;

        /// <summary>Instruments from alias file.</summary>
        readonly Dictionary<int, string>? _aliases;

        // Backing fields.
        int _channelNumber;
        int _patch = 0;
        double _volume = VolumeDefs.DEFAULT_VOLUME;
        bool _isDrums = false;
        #endregion

        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { if (value is < 1 or > MidiDefs.NUM_CHANNELS) throw new ArgumentOutOfRangeException($"ChannelNumber:{value}");
                  else _channelNumber = value; }
        }

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0, VolumeDefs.MAX_VOLUME); }
        }

        /// <summary>Associated events - optional depending on implementation.</summary>
        public EventCollection Events { get; set; } = new();
        #endregion


        /////////////////////////// new ///////////////////////////
        /////////////////////////// new ///////////////////////////
        /////////////////////////// new ///////////////////////////
        /////////////////////////// new ///////////////////////////

        // TODO1 make into function???
        public string DeviceInfo { get { return $"Dev{_device.Id} {_device.DeviceName}"; } }

        // hide device access
        public void Send(BaseEvent bevt)
        {
            _device.Send(bevt);
        }

        /// <summary>Current instrument/patch number.</summary>
        public int Patch
        {
            get { return _patch; }
            //set
            //{
            //    if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Patch:{value}");
            //    else _patch = value; Device.Send(new Patch(ChannelNumber, _patch));
            //}
        }

        /// <summary>Current instrument/patch name. Set may update internal collection and sends a midi patch message.</summary>
        public string PatchName
        {
            get { return GetInstrumentName(_patch); }
            set
            {
                var id = GetInstrumentId(value);
                if (id < 0)
                {
                    throw new ArgumentException($"Invalid patch:{value}");
                }
                _patch = id;
                _device.Send(new Patch(ChannelNumber, _patch));
            }
        }

        public bool IsDrums
        {
            get { return _isDrums; }
            //    set midifrier will want to set this maybe
            //    {
            //        //if (_isDrums != value)
            //        {
            //            _isDrums = value;

            //            // Load specific instruments list.
            //            var ir = new IniReader();
            //            ir.ParseString(Properties.Resources.gm_defs);

            //            if (_isDrums)
            //            {
            //                ir.GetValues("drumkits").ForEach(kv => { _instruments[int.Parse(kv.Key)] = kv.Value; });
            //                //ir.GetValues("drums").ForEach(kv => { _drums[int.Parse(kv.Key)] = kv.Value; });
            //            }
            //            else
            //            {
            //                ir.GetValues("instruments").ForEach(kv => { _instruments[int.Parse(kv.Key)] = kv.Value; });
            //            }
            //        }
            //    }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchName"></param>
        /// <param name="aliasFile"></param>
        /// <exception cref="MidiLibException"></exception>
        public void InitInstruments(string patchName, string? aliasFile = null)
        {
            if (aliasFile is not null) // explicit defs file
            {
                try
                {
                    var ir = new IniReader();
                    ir.ParseFile(aliasFile);

                    var defs = ir.GetValues("instruments");

                    defs.ForEach(kv =>
                    {
                        int id = int.Parse(kv.Key); // can throw
                        if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Instrument:{id}"); }
                        if (kv.Value.Length == 0) { throw new ArgumentOutOfRangeException($"{id} has no value"); }

                        _aliases.Add(id, kv.Value);
                    });
                }
                catch (Exception ex)
                {
                    throw new MidiLibException($"Failed to load alias file {aliasFile}: {ex.Message}");
                }
            }
            else
            {
                var id = MidiDefs.GetDrumKitId(patchName);
                if (id >= 0) // it's GM drum kit
                {
                    _isDrums = true;
                    _patch = id;
                }
                else
                {
                    id = MidiDefs.GetInstrumentId(patchName);
                    if (id >= 0) // it's plain GM instrument
                    {
                        _isDrums = false;
                        _patch = id;
                    }
                    else // unknown
                    {
                        throw new ArgumentException($"Invalid instrument:{patchName}");
                    }
                }
                _device.Send(new Patch(ChannelNumber, _patch));
            }
        }





        // TODO1 midifrier:
        //
        //// Make new channel. Attach corresponding events in a less-than-elegant fashion.
        //var channel = (chnum == cmbDrumChannel.SelectedIndex + 1) ?
        //    MidiManager.Instance.OpenOutputChannelDrums(_settings.OutputDevice, chnum, $"chan{chnum}", patchName) :
        //    MidiManager.Instance.OpenOutputChannel(_settings.OutputDevice, chnum, $"chan{chnum}", patchName);
        //channel.Events = chEvents;
        //
        //cmbDrumChannel.SelectedIndexChanged += DrumChannel_SelectedIndexChanged;
        //>>> calls
        //void UpdateDrumChannels()
        //{
        //    _channelControls.ForEach(ctl => ctl.BoundChannel.IsDrums = ctl.BoundChannel.ChannelNumber == cmbDrumChannel.SelectedIndex + 1);
        //}
        //
        //// Reset stuff.
        //cmbDrumChannel.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL - 1;



        ////////////////////// old ////////////////////////////////
        ////////////////////// old ////////////////////////////////
        ////////////////////// old ////////////////////////////////
        ////////////////////// old ////////////////////////////////





        ///// <summary>Override default instrument list.</summary>
        //public string InstrumentFile
        //{
        //    set { _instrumentFile = value; LoadInstrumentFile(); }
        //}

        //public bool IsDrums
        //{
        //    get { return _isDrums; }
        //    set
        //    {
        //        //if (_isDrums != value)
        //        {
        //            _isDrums = value;

        //            // Load specific instruments list.
        //            var ir = new IniReader();
        //            ir.ParseString(Properties.Resources.gm_defs);

        //            if (_isDrums)
        //            {
        //                ir.GetValues("drumkits").ForEach(kv => { _instruments[int.Parse(kv.Key)] = kv.Value; });
        //                //ir.GetValues("drums").ForEach(kv => { _drums[int.Parse(kv.Key)] = kv.Value; });
        //            }
        //            else
        //            {
        //                ir.GetValues("instruments").ForEach(kv => { _instruments[int.Parse(kv.Key)] = kv.Value; });
        //            }
        //        }
        //    }
        //}

        ///// <summary>Current instrument/patch number. Set sends a midi patch message.</summary>
        //public int Patch
        //{
        //    get { return _patch; }
        //    set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Patch:{value}");
        //          else _patch = value;  Device.Send(new Patch(ChannelNumber, _patch)); }
        //}


        ///// <summary>Current instrument/patch name. Set may update internal collection and sends a midi patch message.</summary>
        //public string PatchName
        //{
        //    get { return GetInstrumentName(_patch); }
        //    set
        //    {
        //        var id = GetInstrumentId(value);
        //        if (id < 0)
        //        {
        //            // TO-DO1 reload instruments and IsDrums?
        //        }
        //        Patch = id;
        //    }
        //}

        /// <summary>Load instrument names.</summary>
        //void LoadInstrumentFile()
        //{
        //    _instruments.Clear();

        //    try
        //    {
        //        var ir = new IniReader();
        //        ir.ParseFile(_instrumentFile);

        //        var defs = ir.GetValues("instruments");

        //        defs.ForEach(kv =>
        //        {
        //            int id = int.Parse(kv.Key); // can throw
        //            if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Instrument:{id}"); }
        //            if (kv.Value.Length == 0) { throw new ArgumentOutOfRangeException($"{id} has no value"); }

        //            _instruments.Add(id, kv.Value);
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new MidiLibException($"Failed to load instruments file {_instrumentFile}: {ex.Message}");
        //    }
        //}





        #region Lifecycle
        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
    //    /// <param name="patch"></param>
        public OutputChannel(IOutputDevice device, int channelNumber)//, string patch)
        {
            _device = device;
            ChannelNumber = channelNumber;
            //Flavor = flavor;
            Volume = VolumeDefs.DEFAULT_VOLUME;
            Handle = HandleOps.Create(device.Id, ChannelNumber, true);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Get instrument name from id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The instrument name or a fabricated one if unknown.</returns>
        public string GetInstrumentName(int id)
        {
            if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Instrument:{id}"); }

            string? name;

            if (_aliases is not null)
            {
                _aliases.TryGetValue(id, out name);
            }
            else if (_isDrums)
            {
                name = MidiDefs.GetDrumName(id);
            }
            else
            {
                name = MidiDefs.GetInstrumentName(id);

            }

            if (name is null) { throw new ArgumentException($"Invalid instrument id:{id}"); }

            return name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        int GetInstrumentId(string name)
        {
            //if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Instrument:{id}"); }

            int id = -1;

            if (_aliases is not null)
            {
                var i = _aliases.Where(v => v.Value == name);
                id = i.Any() ? i.First().Key : -1;
            }
            else if (_isDrums)
            {
                id = MidiDefs.GetDrumId(name);
            }
            else
            {
                id = MidiDefs.GetInstrumentId(name);

            }

            if (name is null) { throw new ArgumentException($"Invalid instrument id:{id}"); }

            return id;
        }


        #endregion

        #region Private functions


        #endregion
    }
}
