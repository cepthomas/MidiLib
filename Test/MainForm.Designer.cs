

namespace Ephemera.MidiLib.Test
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            btnLogMidi = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            btnKillMidi = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            btnGo = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            txtViewer = new Ephemera.NBagOfUis.TextViewer();
            sldMasterVolume = new Ephemera.NBagOfUis.Slider();
            ch_ctrl1 = new ChannelControl();
            ch_ctrl2 = new ChannelControl();
            propGrid = new System.Windows.Forms.PropertyGrid();
            timeBar = new TimeBar();
            timer1 = new System.Windows.Forms.Timer(components);
            chkLoop = new System.Windows.Forms.CheckBox();
            btnRewind = new System.Windows.Forms.Button();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnLogMidi, toolStripSeparator2, btnKillMidi, toolStripSeparator1, btnGo, toolStripSeparator4 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1092, 26);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnLogMidi
            // 
            btnLogMidi.CheckOnClick = true;
            btnLogMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnLogMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnLogMidi.Name = "btnLogMidi";
            btnLogMidi.Size = new System.Drawing.Size(62, 23);
            btnLogMidi.Text = "log midi";
            btnLogMidi.ToolTipText = "Enable logging midi events";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(6, 26);
            // 
            // btnKillMidi
            // 
            btnKillMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnKillMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnKillMidi.Name = "btnKillMidi";
            btnKillMidi.Size = new System.Drawing.Size(29, 23);
            btnKillMidi.Text = "kill";
            btnKillMidi.ToolTipText = "Kill all midi channels";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 26);
            // 
            // btnGo
            // 
            btnGo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnGo.Image = (System.Drawing.Image)resources.GetObject("btnGo.Image");
            btnGo.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnGo.Name = "btnGo";
            btnGo.Size = new System.Drawing.Size(49, 23);
            btnGo.Text = "go go";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new System.Drawing.Size(6, 26);
            // 
            // txtViewer
            // 
            txtViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtViewer.Location = new System.Drawing.Point(12, 345);
            txtViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtViewer.MaxText = 5000;
            txtViewer.Name = "txtViewer";
            txtViewer.Prompt = "";
            txtViewer.Size = new System.Drawing.Size(753, 242);
            txtViewer.TabIndex = 58;
            txtViewer.WordWrap = true;
            // 
            // sldMasterVolume
            // 
            sldMasterVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            sldMasterVolume.DrawColor = System.Drawing.Color.Red;
            sldMasterVolume.Label = "";
            sldMasterVolume.Location = new System.Drawing.Point(8, 38);
            sldMasterVolume.Maximum = 10D;
            sldMasterVolume.Minimum = 0D;
            sldMasterVolume.Name = "sldMasterVolume";
            sldMasterVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            sldMasterVolume.Resolution = 0.1D;
            sldMasterVolume.Size = new System.Drawing.Size(138, 48);
            sldMasterVolume.TabIndex = 99;
            sldMasterVolume.Value = 5D;
            // 
            // ch_ctrl1
            // 
            ch_ctrl1.Location = new System.Drawing.Point(8, 101);
            ch_ctrl1.Name = "ch_ctrl1";
            ch_ctrl1.Size = new System.Drawing.Size(356, 56);
            ch_ctrl1.TabIndex = 105;
            // 
            // ch_ctrl2
            // 
            ch_ctrl2.Location = new System.Drawing.Point(409, 101);
            ch_ctrl2.Name = "ch_ctrl2";
            ch_ctrl2.Size = new System.Drawing.Size(356, 56);
            ch_ctrl2.TabIndex = 106;
            // 
            // propGrid
            // 
            propGrid.Location = new System.Drawing.Point(813, 345);
            propGrid.Name = "propGrid";
            propGrid.Size = new System.Drawing.Size(251, 231);
            propGrid.TabIndex = 109;
            // 
            // timeBar
            // 
            timeBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            timeBar.DrawColor = System.Drawing.Color.Red;
            timeBar.DoLoop = false;
            timeBar.FontLarge = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            timeBar.FontSmall = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            timeBar.Location = new System.Drawing.Point(434, 41);
            timeBar.Name = "timeBar";
            timeBar.SelectedColor = System.Drawing.Color.Blue;
            timeBar.Size = new System.Drawing.Size(638, 45);
            timeBar.Snap = SnapType.Bar;
            timeBar.TabIndex = 110;
            // 
            // chkLoop
            // 
            chkLoop.Appearance = System.Windows.Forms.Appearance.Button;
            chkLoop.AutoSize = true;
            chkLoop.Location = new System.Drawing.Point(373, 41);
            chkLoop.Name = "chkLoop";
            chkLoop.Size = new System.Drawing.Size(46, 29);
            chkLoop.TabIndex = 111;
            chkLoop.Text = "loop";
            chkLoop.UseVisualStyleBackColor = true;
            // 
            // btnRewind
            // 
            btnRewind.Location = new System.Drawing.Point(373, 71);
            btnRewind.Name = "btnRewind";
            btnRewind.Size = new System.Drawing.Size(46, 26);
            btnRewind.TabIndex = 112;
            btnRewind.Text = "<=";
            btnRewind.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1092, 600);
            Controls.Add(btnRewind);
            Controls.Add(chkLoop);
            Controls.Add(timeBar);
            Controls.Add(propGrid);
            Controls.Add(ch_ctrl1);
            Controls.Add(ch_ctrl2);
            Controls.Add(sldMasterVolume);
            Controls.Add(txtViewer);
            Controls.Add(toolStrip1);
            Location = new System.Drawing.Point(300, 50);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Text = "Midi Lib Test";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private NBagOfUis.Slider sldMasterVolume;
        private NBagOfUis.TextViewer txtViewer;
        private System.Windows.Forms.ToolStripButton btnLogMidi;
        private System.Windows.Forms.ToolStripButton btnKillMidi;
        private ChannelControl ch_ctrl1;
        private ChannelControl ch_ctrl2;
        private System.Windows.Forms.PropertyGrid propGrid;
        private TimeBar timeBar;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.CheckBox chkLoop;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnGo;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
    }
}

