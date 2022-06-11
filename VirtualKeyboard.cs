using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using NBagOfTricks;


//TODO slow startup - too many sub controls?


namespace MidiLib
{
    /// <summary>
    /// Virtual keyboard control borrowed from Leslie Sanford with extras.
    /// </summary>
    public partial class VirtualKeyboard : UserControl
    {
        #region Properties
        /// <summary>Draw the names on the keys.</summary>
        public bool ShowNoteNames { get; set; } = false;

        /// <summary>Determines the overall size.</summary>
        public int KeySize { get; set; } = 14;
        #endregion

        #region Events
        /// <summary>Key press info.</summary>
        public class KeyboardEventArgs : EventArgs
        {
            /// <summary>Midi note id.</summary>
            public int NoteId { get; set; } = 0;

            /// <summary>Midi velocity. 0 means note off.</summary>
            public int Velocity { get; set; } = 0;
        }
        public event EventHandler<KeyboardEventArgs>? KeyboardEvent;
        #endregion

        #region Constants
        const int LOW_NOTE = 21;
        const int HIGH_NOTE = 109;
        const int MIDDLE_C = 60;
        #endregion

        #region Fields
        /// <summary>All the created piano keys.</summary>
        readonly List<VirtualKey> _keys = new();

        /// <summary>Map from Keys value to the index in _keys.</summary>
        readonly Dictionary<Keys, int> _keyMap = new();

        /// <summary>Known bug?</summary>
        bool _keyDown = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public VirtualKeyboard()
        {
            // Intercept all keyboard events.
            // KeyPreview = true;

            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(773, 173);
            Name = "VirtualKeyboard";
            Text = "Virtual Keyboard";

            CreateKeys();
            CreateKeyMap();
            DrawKeys();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            DrawKeys();
            Invalidate();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Is it a white key?
        /// </summary>
        /// <param name="notenum">Which note</param>
        /// <returns>True/false</returns>
        bool IsNatural(int notenum)
        {
            int[] naturals = { 0, 2, 4, 5, 7, 9, 11 };
            return naturals.Contains(notenum % 12);
        }

        /// <summary>
        /// Create the midi note/keyboard mapping.
        /// </summary>
        void CreateKeyMap()
        {
            _keyMap.Clear();

            int indexOfMiddleC = _keys.IndexOf(_keys.Where(k => k.NoteId == MIDDLE_C).First());

            string[] keyDefs =
            {
                    "Z  -12  ;  C-3",
                    "S  -11  ;  C#",
                    "X  -10  ;  D",
                    "D  -9   ;  D#",
                    "C  -8   ;  E",
                    "V  -7   ;  F",
                    "G  -6   ;  F#",
                    "B  -5   ;  G",
                    "H  -4   ;  G#",
                    "N  -3   ;  A",
                    "J  -2   ;  A#",
                    "M  -1   ;  B",
                    ",   0   ;  C-4",
                    "L  +1   ;  C#-4",
                    ".  +2   ;  D",
                    ";  +3   ;  D#",
                    "/  +4   ;  E-4",
                    "Q   0   ;  C-4",
                    "2  +1   ;  C#",
                    "W  +2   ;  D",
                    "3  +3   ;  D#",
                    "E  +4   ;  E",
                    "R  +5   ;  F",
                    "5  +6   ;  F#",
                    "T  +7   ;  G",
                    "6  +8   ;  G#",
                    "Y  +9   ;  A",
                    "7  +10  ;  A#",
                    "U  +11  ;  B",
                    "I  +12  ;  C-5",
                    "9  +13  ;  C#-5",
                    "O  +14  ;  D",
                    "0  +15  ;  D#",
                    "P  +16  ;  E-5"
                };

            foreach (string l in keyDefs)
            {
                var parts = l.SplitByToken(" ");

                if (parts.Count >= 2 && parts[0] != ";")
                {
                    string key = parts[0];
                    char ch = key[0];
                    int offset = int.Parse(parts[1]);
                    int note = indexOfMiddleC + offset;

                    switch (key)
                    {
                        case ",": _keyMap.Add(Keys.Oemcomma, note); break;
                        case "=": _keyMap.Add(Keys.Oemplus, note); break;
                        case "-": _keyMap.Add(Keys.OemMinus, note); break;
                        case "/": _keyMap.Add(Keys.OemQuestion, note); break;
                        case ".": _keyMap.Add(Keys.OemPeriod, note); break;
                        case "\'": _keyMap.Add(Keys.OemQuotes, note); break;
                        case "\\": _keyMap.Add(Keys.OemPipe, note); break;
                        case "]": _keyMap.Add(Keys.OemCloseBrackets, note); break;
                        case "[": _keyMap.Add(Keys.OemOpenBrackets, note); break;
                        case "`": _keyMap.Add(Keys.Oemtilde, note); break;
                        case ";": _keyMap.Add(Keys.OemSemicolon, note); break;

                        default:
                            if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
                            {
                                _keyMap.Add((Keys)ch, note);
                            }
                            break;
                    }
                }
            }
        }
        #endregion

        #region User input handlers
        /// <summary>
        /// Use alpha keyboard to drive piano.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!_keyDown)
            {
                if (_keyMap.ContainsKey(e.KeyCode))
                {
                    VirtualKey pk = _keys[_keyMap[e.KeyCode]];
                    if (!pk.IsPressed)
                    {
                        pk.PressVKey(100);
                        e.Handled = true;
                    }
                }

                _keyDown = true;
            }
        }

        /// <summary>
        /// Use alpha keyboard to drive piano.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            _keyDown = false;

            if (_keyMap.ContainsKey(e.KeyCode))
            {
                VirtualKey pk = _keys[_keyMap[e.KeyCode]];
                pk.ReleaseVKey();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Pass along an event from a virtual key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Keyboard_VKeyEvent(object? sender, VirtualKey.VKeyEventArgs e)
        {
            if (KeyboardEvent is not null)
            {
                KeyboardEvent.Invoke(this, new KeyboardEventArgs()
                {
                    NoteId = e.NoteId,
                    Velocity = e.Velocity
                });
            }
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Create the key controls.
        /// </summary>
        void CreateKeys()
        {
            _keys.Clear();

            for (int i = 0; i < HIGH_NOTE - LOW_NOTE; i++)
            {
                int noteId = i + LOW_NOTE;
                VirtualKey pk;
                if (IsNatural(noteId))
                {
                    pk = new VirtualKey(this, true, noteId);
                }
                else
                {
                    pk = new VirtualKey(this, false, noteId);
                    pk.BringToFront();
                }

                pk.VKeyEvent += Keyboard_VKeyEvent;
                _keys.Add(pk);
                Controls.Add(pk);
            }
        }

        /// <summary>
        /// Re/draw the keys.
        /// </summary>
        void DrawKeys()
        {
            if(_keys.Count > 0)
            {
                int whiteKeyWidth = _keys.Count * KeySize / _keys.Count(k => k.IsNatural);
                int blackKeyWidth = (int)(whiteKeyWidth * 0.6);
                int whiteKeyHeight = (int)(Height); // KeyHeight
                int blackKeyHeight = (int)(whiteKeyHeight * 0.65);
                int offset = whiteKeyWidth - blackKeyWidth / 2;

                int numWhiteKeys = 0;

                for (int i = 0; i < _keys.Count; i++)
                {
                    VirtualKey pk = _keys[i];

                    // Note that controls have to have integer width so resizing is a bit lumpy.
                    if (pk.IsNatural)
                    {
                        pk.Height = whiteKeyHeight;
                        pk.Width = whiteKeyWidth;
                        pk.Location = new Point(numWhiteKeys * whiteKeyWidth, 0);
                        numWhiteKeys++;
                    }
                    else
                    {
                        pk.Height = blackKeyHeight;
                        pk.Width = blackKeyWidth;
                        pk.Location = new Point(offset + (numWhiteKeys - 1) * whiteKeyWidth);
                        pk.BringToFront();
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>One individual key.</summary>
    public class VirtualKey : Control
    {
        #region Fields
        /// <summary>Hook to owner.</summary>
        readonly VirtualKeyboard _owner;

        /// <summary>For showing names.</summary>
        static readonly string[] _noteNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
        #endregion

        #region Properties
        /// <summary>Key status.</summary>
        public bool IsPressed { get; private set; } = false;

        /// <summary>Key status.</summary>
        public bool IsNatural { get; private set; } = false;

        /// <summary>Associated midi note.</summary>
        public int NoteId { get; private set; } = 0;
        #endregion

        #region Events
        /// <summary>Notify handlers of key change.</summary>
        public class VKeyEventArgs : EventArgs
        {
            public int NoteId { get; set; }
            public int Velocity { get; set; }
        }
        public event EventHandler<VKeyEventArgs>? VKeyEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="isNatural"></param>
        /// <param name="noteId"></param>
        public VirtualKey(VirtualKeyboard owner, bool isNatural, int noteId)
        {
            _owner = owner;
            TabStop = false;
            IsNatural = isNatural;
            NoteId = noteId;
            Font = new Font("Consolas", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// 
        /// </summary>
        /// <param name="velocity"></param>
        public void PressVKey(int velocity)
        {
            IsPressed = true;
            Invalidate();
            VKeyEvent?.Invoke(this, new VKeyEventArgs() { NoteId = NoteId, Velocity = velocity });
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReleaseVKey()
        {
            IsPressed = false;
            Invalidate();
            VKeyEvent?.Invoke(this, new VKeyEventArgs() { NoteId = NoteId, Velocity = 0 });
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Calc velocity from Y position.
        /// </summary>
        /// <returns></returns>
        int CalcVelocity()
        {
            var p = PointToClient(Cursor.Position);
            var vel = p.Y * 127 / Height;
            return vel;
        }
        #endregion

        #region Mouse handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            if (MouseButtons == MouseButtons.Left)
            {
                PressVKey(CalcVelocity());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            if (IsPressed)
            {
                ReleaseVKey();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            PressVKey(CalcVelocity());

            if (!_owner.Focused)
            {
                _owner.Focus();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            ReleaseVKey();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.X < 0 || e.X > Width || e.Y < 0 || e.Y > Height)
            {
                Capture = false;
            }
        }
        #endregion

        #region Draw the control
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsPressed)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.SkyBlue), 0, 0, Size.Width, Size.Height);
            }
            else
            {
                e.Graphics.FillRectangle(IsNatural ? new SolidBrush(Color.White) : new SolidBrush(Color.Black), 0, 0, Size.Width, Size.Height);
            }

            // Outline.
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, Size.Width - 1, Size.Height - 1);

            // Note name.
            if(_owner.ShowNoteNames)
            {
                int root = NoteId % 12;
                int octave = (NoteId / 12) - 1;
                int x = IsNatural ? 3 : 0;
                e.Graphics.DrawString($"{_noteNames[root]}", Font, Brushes.Black, x, 3);
                e.Graphics.DrawString($"{octave}", Font, Brushes.Black, x, 13);
            }
        }
        #endregion
    }
}
