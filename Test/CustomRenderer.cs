using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib.Test
{
    public class CustomRenderer : UserRenderer
    {
        #region Fields
        /// <summary>Required designer variable.</summary>
        readonly Container components = new();

        /// <summary>Cosmetics.</summary>
        readonly ToolTip toolTip;

        /// <summary>Cosmetics.</summary>
        readonly Pen _pen = new(Color.Black, 3);

        /// <summary>Tracking for note off.</summary>
        int _lastNote = -1;
        #endregion

        //#region Properties
        ///// <summary>Context.</summary>
        //public int ChannelNumber { get; init; }
        //#endregion

        //#region Events
        ///// <summary>UI midi send.</summary>
        //public event EventHandler<BaseMidi>? SendMidi;
        //#endregion

        

        /// <summary>Constructor.</summary>
        public CustomRenderer()
        {
            toolTip = new(components);
            Size = new(231, 174);
        }


        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>Paint the surface.</summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.FillRectangle(Brushes.LightCoral, ClientRectangle);

            // Border.
            g.DrawRectangle(_pen, ClientRectangle);

            // Grid.
            for (int x = ClientRectangle.Left; x < ClientRectangle.Right; x += 25)
            {
                g.DrawLine(Pens.Black, x, ClientRectangle.Top, x, ClientRectangle.Bottom);
            }

            for (int y = ClientRectangle.Bottom; y > ClientRectangle.Top; y -= 25)
            {
                g.DrawLine(Pens.Black, ClientRectangle.Left, y, ClientRectangle.Right, y);
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// Show the pixel info.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var res = MouseToUser();

            if (res is not null)
            {
                toolTip.SetToolTip(this, $"X:{res.Value.ux} Y:{res.Value.uy}");

                // Also gen click?
                if (e.Button == MouseButtons.Left)
                {
                    // Dragging. Did it change?
                    if (_lastNote != res.Value.ux)
                    {
                        if (_lastNote != -1)
                        {
                            // Turn off last note.
                            OnSendMidi(new NoteOff(ChannelNumber, _lastNote, MusicTime.ZERO));
                        }

                        // Start the new note.
                        _lastNote = res.Value.ux;
                        OnSendMidi(new NoteOn(ChannelNumber, res.Value.ux, res.Value.uy, MusicTime.ZERO));
                    }
                }
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var res = MouseToUser();
            if (res is not null)
            {
                _lastNote = res.Value.ux;
                OnSendMidi(new NoteOn(ChannelNumber, res.Value.ux, res.Value.uy, MusicTime.ZERO));
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_lastNote != -1)
            {
                OnSendMidi(new NoteOff(ChannelNumber, _lastNote, MusicTime.ZERO));
                _lastNote = -1;
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Disable control
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            // Turn off last click.
            if (_lastNote != -1)
            {
                OnSendMidi(new NoteOff(ChannelNumber, _lastNote, MusicTime.ZERO));
            }

            // Reset and tell client.
            _lastNote = -1;

            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Get mouse x and y mapped to useful coordinates.
        /// </summary>
        /// <returns>Tuple of x and y.</returns>
        (int ux, int uy)? MouseToUser()
        {
            // Map and check.
            var mp = PointToClient(MousePosition);
            int x = NBagOfTricks.MathUtils.Map(mp.X, ClientRectangle.Left, ClientRectangle.Right, 0, MidiDefs.MAX_MIDI);
            int y = NBagOfTricks.MathUtils.Map(mp.Y, ClientRectangle.Bottom, ClientRectangle.Top, 0, MidiDefs.MAX_MIDI);
            return (x, y);
        }
    }
}
