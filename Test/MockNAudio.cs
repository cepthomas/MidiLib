using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;


// Mock for entities in NAudio. See real class for doc.
// Not currently used. May be useful in the future.


namespace NAudio.Midi
{
    public enum MidiCommandCode : byte { NoteOff = 128, NoteOn = 144, ControlChange = 176, PatchChange = 192 }

    public enum MidiController : byte { AllNotesOff = 123 }

    public class MidiEvent
    {
        public virtual int Channel { get; set; } = 0;
        public int DeltaTime { get; set; } = 0;
        public long AbsoluteTime { get; set; } = 0;
        public MidiCommandCode CommandCode { get; set; } = 0;

        protected MidiEvent(long absoluteTime, int channel, MidiCommandCode commandCode)
        {
            AbsoluteTime = absoluteTime;
            Channel = channel;
            CommandCode = commandCode;
        }

        public int GetAsShortMessage()
        {
            return (Channel - 1) + (int)CommandCode;
        }

        public static MidiEvent FromRawMessage(int rawMessage)
        {
            long absoluteTime = 0;
            int b = rawMessage & 0xFF;
            int data1 = (rawMessage >> 8) & 0xFF;
            int data2 = (rawMessage >> 16) & 0xFF;
            MidiCommandCode commandCode;
            int channel = 1;

            if ((b & 0xF0) == 0xF0)
            {
                // both bytes are used for command code in this case
                commandCode = (MidiCommandCode)b;
            }
            else
            {
                commandCode = (MidiCommandCode)(b & 0xF0);
                channel = (b & 0x0F) + 1;
            }

            MidiEvent me = commandCode switch
            {
                MidiCommandCode.NoteOn or MidiCommandCode.NoteOff => (data2 > 0 && commandCode == MidiCommandCode.NoteOn) ?
                                        new NoteOnEvent(absoluteTime, channel, data1, data2, 0) :
                                        new NoteEvent(absoluteTime, channel, commandCode, data1, data2),
                MidiCommandCode.ControlChange => new ControlChangeEvent(absoluteTime, channel, (MidiController)data1, data2),
                MidiCommandCode.PatchChange => new PatchChangeEvent(absoluteTime, channel, data1),
                _ => throw new NotSupportedException($"Unsupported MIDI Command Code for Raw Message {commandCode}"),
            };
            return me;
        }
    }

    public class NoteEvent(long absoluteTime, int channel, MidiCommandCode commandCode, int noteNumber, int velocity) : MidiEvent(absoluteTime, channel, commandCode)
    {
        public virtual int NoteNumber { get; set; } = noteNumber;
        public int Velocity { get; set; } = velocity;
    }

    public class NoteOnEvent(long absoluteTime, int channel, int noteNumber, int velocity, int duration) : NoteEvent(absoluteTime, channel, MidiCommandCode.NoteOn, noteNumber, velocity)
    {
        public int NoteLength { get; set; } = duration;
    }

    public class ControlChangeEvent(long absoluteTime, int channel, MidiController controller, int controllerValue) : MidiEvent(absoluteTime, channel, MidiCommandCode.ControlChange)
    {
        public MidiController Controller { get; set; } = controller;
        public int ControllerValue { get; set; } = controllerValue;
    }

    public class PatchChangeEvent(long absoluteTime, int channel, int patchNumber) : MidiEvent(absoluteTime, channel, MidiCommandCode.PatchChange)
    {
        public int Patch { get; set; } = patchNumber;
    }

    public class MidiInMessageEventArgs(int message, int timestamp) : EventArgs
    {
        public int RawMessage { get; private set; } = message;
        public int Timestamp { get; private set; } = timestamp;
        public MidiEvent? MidiEvent { get; private set; }
    }

    public class DeviceInfo
    {
        public string ProductName { get; set; } = "";
    }

    public class MidiIn
    {
        public static int NumberOfDevices { get; set; } = 3;

        public event EventHandler<MidiInMessageEventArgs> MessageReceived;

        public event EventHandler<MidiInMessageEventArgs> ErrorReceived;

        public MidiIn(int i)
        {
        }

        public void Dispose()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public static DeviceInfo DeviceInfo(int i)
        {
            return new DeviceInfo() { ProductName = $"DeviceIn{i}" };
        }
    }

    public class MidiOut
    {
        public static int NumberOfDevices { get; set; } = 3;
        public static DeviceInfo DeviceInfo(int i)
        {
            return new DeviceInfo() { ProductName = $"DeviceOut{i}" };
        }

        public MidiOut(int  i)
        {
        }

        public void Dispose()
        {
        }

        public void Start()
        {
        }

        public void Send(int shortMsg)
        {
        }
    }
 }

