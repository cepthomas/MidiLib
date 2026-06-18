using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Sort of like DateTime but for musical terminology in midi.</summary>
    public class MusicTime : IEquatable<MusicTime>
    {
        #region Fields
        /// <summary>For hashing comparable.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _nextId = 1;

        /// <summary>Convenience.</summary>
        public static readonly MusicTime ZERO = new();
        #endregion

        #region Properties
        /// <summary>Only 4/4 time supported currently.</summary>
        public static int BeatsPerBar { get { return 4; } }

        /// <summary>Our resolution where 8 => 32nd note.</summary>
        public static int SubbeatsPerBeat { get { return 8; } }

        /// <summary>Convenience.</summary>
        public static int SubbeatsPerBar { get { return SubbeatsPerBeat * BeatsPerBar; } }

        /// <summary>Absolute tick => total Subbeats. Zero-based.</summary>
        public int Tick { get; private set; }

        /// <summary>Accessor.</summary>
        public int Id { get { return _id; } }

        /// <summary>The time in music form.</summary>
        public (int bar, int beat, int subbeat) Parts
        {
            get { return (Tick / SubbeatsPerBar, Tick / SubbeatsPerBeat % BeatsPerBar, Tick % SubbeatsPerBeat); }
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MusicTime()
        {
            Tick = 0;
            _id = _nextId++;
        }

        /// <summary>
        /// Constructor from tick.
        /// </summary>
        /// <param name="tick">Number of ticks.</param>
        public MusicTime(int tick)
        {
            if (tick < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            Tick = tick;
            _id = _nextId++;
        }

        /// <summary>
        /// Constructor from tick.
        /// </summary>
        /// <param name="tick">Number of ticks.</param>
        public MusicTime(long tick)
        {
            if (tick < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            Tick = (int)tick;
            _id = _nextId++;
        }

        /// <summary>
        /// Constructor from bar/beat/subbeat.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subbeat"></param>
        public MusicTime(int bar, int beat, int subbeat)
        {
            Tick = (bar * SubbeatsPerBar) + (beat * SubbeatsPerBeat) + subbeat;
            _id = _nextId++;
        }

        /// <summary>
        /// Construct a MusicTime from a string repr.
        /// </summary>
        /// <param name="s">time string can be "1.2.3" or "1.2" or "1".</param>
        public MusicTime(string s)
        {
            var parts = StringUtils.SplitByToken(s, ".");

            bool ok = true;
            int bars = 0;
            int beats = 0;
            int subbeats = 0;

            if (ok && parts.Count > 0) ok = int.TryParse(parts[0], out bars);
            if (ok && parts.Count > 1) ok = int.TryParse(parts[1], out beats);
            if (ok && parts.Count > 2) ok = int.TryParse(parts[2], out subbeats);

            if (ok &&
                bars >= 0 && bars <= 9999 &&
                beats >= 0 && beats < BeatsPerBar &&
                subbeats >= 0 && subbeats <= SubbeatsPerBeat)
            {
                Tick = bars * SubbeatsPerBar + beats * SubbeatsPerBeat + subbeats;
            }
            else
            {
                throw new ArgumentException($"Invalid MusicTime format [{s}]");
            }
        }

        /// <summary>
        /// Construct a MusicTime from Beat.Subbeat representation as a double in the range N.0 to N.7
        /// </summary>
        /// <param name="beat"></param>
        /// <returns>New BarTime.</returns>
        public MusicTime(double beat)
        {
            var (integral, fractional) = MathUtils.SplitDouble(beat);
            var beats = (int)integral;
            var subbeats = (int)Math.Round(fractional * 10.0);

            if (subbeats >= 8)
            {
                throw new ArgumentException($"beat:{beat}");
            }

            // Scale to native.
            subbeats = subbeats * SubbeatsPerBeat / 8;
            Tick = beats * SubbeatsPerBeat + subbeats;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Utility helper function.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public void Constrain(MusicTime lower, MusicTime upper)
        {
            Tick = MathUtils.Constrain(Tick, lower.Tick, upper.Tick);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="subbeats">By this number of subbeats. Can be negative = decrement.</param>
        public void Add(int subbeats)
        {
            Tick += subbeats;
            if (Tick < 0)
            {
                Tick = 0;
            }
        }

        /// <summary>
        /// Reset.
        /// </summary>
        public void Reset()
        {
            Tick = 0;
        }   

        /// <summary>
        /// Set the value from another MusicTime. Value assignment.
        /// </summary>
        /// <param name="other"></param>
        public void Set(MusicTime other)
        {
            Tick = other.Tick;
        }   

        /// <summary>
        /// Set the value using specified rounding.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="snapType"></param>
        /// <param name="up">T = round up</param>
        public void Set(int tick, SnapType snapType = SnapType.Tick, bool up = false)
        {
            if (tick > 0 && snapType != SnapType.Tick)
            {
                // res:32 in:27 floor=(in%aim)*aim  ceiling=floor+aim

                int res = snapType switch
                {
                    SnapType.Tick => 1,
                    SnapType.Beat => SubbeatsPerBeat,
                    SnapType.Bar => SubbeatsPerBar,
                    SnapType.FourBar => 4 * SubbeatsPerBar,
                    _ => 1
                };

                int floor = tick / res;
                int delta = tick % res;
                if (delta > res / 2 || up)
                {
                    floor++;
                }

                Tick = floor * res;
            }
            else
            {
                Tick = tick;
            }
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var (bar, beat, subbeat) = Parts;
            return $"{bar}.{beat}.{subbeat:0}";
        }
        #endregion

        #region Overrides - IEquatable etc
        // Override GetHashCode() for dictionary lookups.
        public override int GetHashCode() { return Tick; }

        // Override Equals() for value equality comparison.
        public override bool Equals(object? obj) => Equals(obj as MusicTime);
        public bool Equals(MusicTime? other) { return other is not null && Tick == other.Tick; }
        #endregion

        #region Operator overloads
        public static bool operator ==(MusicTime a, MusicTime b) { return a.Tick == b.Tick; }

        public static bool operator !=(MusicTime a, MusicTime b) { return !(a == b); }

        public static MusicTime operator +(MusicTime a, MusicTime b) { return new MusicTime(a.Tick + b.Tick); }

        public static MusicTime operator -(MusicTime a, MusicTime b) { return new MusicTime(a.Tick - b.Tick); }

        public static bool operator <(MusicTime a, MusicTime b) { return a.Tick < b.Tick; }

        public static bool operator >(MusicTime a, MusicTime b) { return a.Tick > b.Tick; }

        public static bool operator <=(MusicTime a, MusicTime b) { return a.Tick <= b.Tick; }

        public static bool operator >=(MusicTime a, MusicTime b) { return a.Tick >= b.Tick; }
        #endregion
    }
}
