

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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLogMidi = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnKillMidi = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.btnPlay = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRewind = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExportCsv = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExportMidi = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.cmbDrumChannel1 = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.cmbDrumChannel2 = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.btnStuff = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnDocs = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnSettings = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.txtViewer = new NBagOfUis.TextViewer();
            this.lbPatterns = new System.Windows.Forms.CheckedListBox();
            this.btnAll = new System.Windows.Forms.Button();
            this.btnNone = new System.Windows.Forms.Button();
            this.vkey = new MidiLib.VirtualKeyboard();
            this.nudTempo = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.sldVolume = new NBagOfUis.Slider();
            this.barBar = new MidiLib.BarBar();
            this.bb = new MidiLib.BingBong();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempo)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnOpen,
            this.toolStripSeparator12,
            this.btnLogMidi,
            this.toolStripSeparator3,
            this.btnKillMidi,
            this.toolStripSeparator4,
            this.btnPlay,
            this.toolStripSeparator5,
            this.btnRewind,
            this.toolStripSeparator6,
            this.btnExportCsv,
            this.toolStripSeparator8,
            this.btnExportMidi,
            this.toolStripSeparator9,
            this.toolStripLabel1,
            this.cmbDrumChannel1,
            this.toolStripLabel2,
            this.cmbDrumChannel2,
            this.toolStripSeparator10,
            this.btnStuff,
            this.toolStripSeparator1,
            this.btnDocs,
            this.toolStripSeparator2,
            this.btnSettings,
            this.toolStripSeparator11});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1250, 28);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnOpen
            // 
            this.btnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(47, 25);
            this.btnOpen.Text = "open";
            this.btnOpen.Click += new System.EventHandler(this.Open_Click);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(6, 28);
            // 
            // btnLogMidi
            // 
            this.btnLogMidi.CheckOnClick = true;
            this.btnLogMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnLogMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLogMidi.Name = "btnLogMidi";
            this.btnLogMidi.Size = new System.Drawing.Size(69, 25);
            this.btnLogMidi.Text = "log midi";
            this.btnLogMidi.ToolTipText = "Enable logging midi events";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 28);
            // 
            // btnKillMidi
            // 
            this.btnKillMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnKillMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnKillMidi.Name = "btnKillMidi";
            this.btnKillMidi.Size = new System.Drawing.Size(32, 25);
            this.btnKillMidi.Text = "kill";
            this.btnKillMidi.ToolTipText = "Kill all midi channels";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 28);
            // 
            // btnPlay
            // 
            this.btnPlay.CheckOnClick = true;
            this.btnPlay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPlay.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(41, 25);
            this.btnPlay.Text = "play";
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 28);
            // 
            // btnRewind
            // 
            this.btnRewind.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnRewind.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(58, 25);
            this.btnRewind.Text = "rewind";
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 28);
            // 
            // btnExportCsv
            // 
            this.btnExportCsv.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnExportCsv.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExportCsv.Name = "btnExportCsv";
            this.btnExportCsv.Size = new System.Drawing.Size(80, 25);
            this.btnExportCsv.Text = "export csv";
            this.btnExportCsv.Click += new System.EventHandler(this.Export_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(6, 28);
            // 
            // btnExportMidi
            // 
            this.btnExportMidi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnExportMidi.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExportMidi.Name = "btnExportMidi";
            this.btnExportMidi.Size = new System.Drawing.Size(90, 25);
            this.btnExportMidi.Text = "export midi";
            this.btnExportMidi.Click += new System.EventHandler(this.Export_Click);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(6, 28);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(36, 25);
            this.toolStripLabel1.Text = "Dr1:";
            // 
            // cmbDrumChannel1
            // 
            this.cmbDrumChannel1.AutoSize = false;
            this.cmbDrumChannel1.Name = "cmbDrumChannel1";
            this.cmbDrumChannel1.Size = new System.Drawing.Size(50, 28);
            this.cmbDrumChannel1.SelectedIndexChanged += new System.EventHandler(this.DrumChannel_SelectedIndexChanged);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(36, 25);
            this.toolStripLabel2.Text = "Dr2:";
            // 
            // cmbDrumChannel2
            // 
            this.cmbDrumChannel2.AutoSize = false;
            this.cmbDrumChannel2.Name = "cmbDrumChannel2";
            this.cmbDrumChannel2.Size = new System.Drawing.Size(50, 28);
            this.cmbDrumChannel2.SelectedIndexChanged += new System.EventHandler(this.DrumChannel_SelectedIndexChanged);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(6, 28);
            // 
            // btnStuff
            // 
            this.btnStuff.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnStuff.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStuff.Name = "btnStuff";
            this.btnStuff.Size = new System.Drawing.Size(42, 25);
            this.btnStuff.Text = "stuff";
            this.btnStuff.Click += new System.EventHandler(this.Stuff_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 28);
            // 
            // btnDocs
            // 
            this.btnDocs.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnDocs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnDocs.Name = "btnDocs";
            this.btnDocs.Size = new System.Drawing.Size(44, 25);
            this.btnDocs.Text = "docs";
            this.btnDocs.Click += new System.EventHandler(this.Docs_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 28);
            // 
            // btnSettings
            // 
            this.btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(64, 25);
            this.btnSettings.Text = "settings";
            this.btnSettings.Click += new System.EventHandler(this.Settings_Click);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(6, 28);
            // 
            // txtViewer
            // 
            this.txtViewer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.txtViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtViewer.Location = new System.Drawing.Point(537, 40);
            this.txtViewer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtViewer.MaxText = 5000;
            this.txtViewer.Name = "txtViewer";
            this.txtViewer.Size = new System.Drawing.Size(701, 284);
            this.txtViewer.TabIndex = 58;
            this.txtViewer.WordWrap = true;
            // 
            // lbPatterns
            // 
            this.lbPatterns.BackColor = System.Drawing.SystemColors.Control;
            this.lbPatterns.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbPatterns.FormattingEnabled = true;
            this.lbPatterns.Location = new System.Drawing.Point(8, 137);
            this.lbPatterns.Name = "lbPatterns";
            this.lbPatterns.Size = new System.Drawing.Size(138, 354);
            this.lbPatterns.TabIndex = 89;
            this.lbPatterns.SelectedIndexChanged += new System.EventHandler(this.Patterns_SelectedIndexChanged);
            // 
            // btnAll
            // 
            this.btnAll.Location = new System.Drawing.Point(8, 99);
            this.btnAll.Name = "btnAll";
            this.btnAll.Size = new System.Drawing.Size(50, 30);
            this.btnAll.TabIndex = 90;
            this.btnAll.Text = "all";
            this.btnAll.UseVisualStyleBackColor = true;
            this.btnAll.Click += new System.EventHandler(this.AllOrNone_Click);
            // 
            // btnNone
            // 
            this.btnNone.Location = new System.Drawing.Point(66, 99);
            this.btnNone.Name = "btnNone";
            this.btnNone.Size = new System.Drawing.Size(50, 30);
            this.btnNone.TabIndex = 91;
            this.btnNone.Text = "none";
            this.btnNone.UseVisualStyleBackColor = true;
            this.btnNone.Click += new System.EventHandler(this.AllOrNone_Click);
            // 
            // vkey
            // 
            this.vkey.CaptureEnable = false;
            this.vkey.Channel = 1;
            this.vkey.DeviceName = "VirtualKeyboard";
            this.vkey.KeySize = 14;
            this.vkey.Location = new System.Drawing.Point(13, 663);
            this.vkey.LogEnable = false;
            this.vkey.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.vkey.Name = "vkey";
            this.vkey.ShowNoteNames = true;
            this.vkey.Size = new System.Drawing.Size(1084, 111);
            this.vkey.TabIndex = 93;
            // 
            // nudTempo
            // 
            this.nudTempo.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudTempo.Location = new System.Drawing.Point(159, 63);
            this.nudTempo.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudTempo.Minimum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.nudTempo.Name = "nudTempo";
            this.nudTempo.Size = new System.Drawing.Size(58, 27);
            this.nudTempo.TabIndex = 96;
            this.nudTempo.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(159, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 20);
            this.label1.TabIndex = 98;
            this.label1.Text = "BPM";
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DrawColor = System.Drawing.Color.White;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(8, 40);
            this.sldVolume.Maximum = 10D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.Resolution = 0.1D;
            this.sldVolume.Size = new System.Drawing.Size(138, 50);
            this.sldVolume.TabIndex = 99;
            this.sldVolume.Value = 5D;
            // 
            // barBar
            // 
            this.barBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.barBar.FontLarge = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.barBar.FontSmall = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.barBar.Location = new System.Drawing.Point(223, 40);
            this.barBar.MarkerColor = System.Drawing.Color.Black;
            this.barBar.Name = "barBar";
            this.barBar.ProgressColor = System.Drawing.Color.White;
            this.barBar.Size = new System.Drawing.Size(308, 50);
            this.barBar.TabIndex = 100;
            // 
            // bb
            // 
            this.bb.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.bb.CaptureEnable = false;
            this.bb.Channel = 1;
            this.bb.DeviceName = "BingBong";
            this.bb.DrawNoteGrid = true;
            this.bb.Location = new System.Drawing.Point(537, 332);
            this.bb.LogEnable = false;
            this.bb.MaxControl = 127;
            this.bb.MaxNote = 95;
            this.bb.MinControl = 0;
            this.bb.MinNote = 24;
            this.bb.Name = "bb";
            this.bb.Size = new System.Drawing.Size(300, 300);
            this.bb.TabIndex = 102;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1250, 779);
            this.Controls.Add(this.bb);
            this.Controls.Add(this.barBar);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nudTempo);
            this.Controls.Add(this.vkey);
            this.Controls.Add(this.btnNone);
            this.Controls.Add(this.btnAll);
            this.Controls.Add(this.lbPatterns);
            this.Controls.Add(this.txtViewer);
            this.Controls.Add(this.toolStrip1);
            this.Location = new System.Drawing.Point(300, 50);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Midi Lib";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTempo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private NBagOfUis.Slider sldVolume;
        private NBagOfUis.TextViewer txtViewer;
        private System.Windows.Forms.ToolStripButton btnLogMidi;
        private System.Windows.Forms.ToolStripButton btnKillMidi;
        private System.Windows.Forms.ToolStripButton btnPlay;
        private System.Windows.Forms.ToolStripButton btnRewind;
        private System.Windows.Forms.ToolStripButton btnExportCsv;
        private System.Windows.Forms.ToolStripButton btnExportMidi;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.CheckedListBox lbPatterns;
        private System.Windows.Forms.Button btnAll;
        private System.Windows.Forms.Button btnNone;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripComboBox cmbDrumChannel1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripComboBox cmbDrumChannel2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private VirtualKeyboard vkey;
        private System.Windows.Forms.NumericUpDown nudTempo;
        private System.Windows.Forms.Label label1;
        private BarBar barBar;
        private System.Windows.Forms.ToolStripButton btnStuff;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private BingBong bb;
        private System.Windows.Forms.ToolStripButton btnDocs;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnSettings;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripButton btnOpen;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
    }
}

