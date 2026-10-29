

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
            btnTimebar = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            btnProperty = new System.Windows.Forms.ToolStripButton();
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            sldMasterVolume = new Ephemera.NBagOfUis.Slider();
            ch_ctrl1 = new ChannelControl();
            ch_ctrl2 = new ChannelControl();
            timeBar = new TimeBar();
            timer1 = new System.Windows.Forms.Timer(components);
            chkLoop = new System.Windows.Forms.CheckBox();
            btnRewind = new System.Windows.Forms.Button();
            toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            btnSend = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnLogMidi, toolStripSeparator2, btnKillMidi, toolStripSeparator1, btnGo, toolStripSeparator4, btnTimebar, toolStripSeparator3, btnProperty, toolStripSeparator5, btnSend, toolStripSeparator6 });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(773, 26);
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
            btnGo.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnGo.Name = "btnGo";
            btnGo.Size = new System.Drawing.Size(33, 23);
            btnGo.Text = "go!";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new System.Drawing.Size(6, 26);
            // 
            // btnTimebar
            // 
            btnTimebar.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnTimebar.Image = (System.Drawing.Image)resources.GetObject("btnTimebar.Image");
            btnTimebar.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnTimebar.Name = "btnTimebar";
            btnTimebar.Size = new System.Drawing.Size(60, 23);
            btnTimebar.Text = "timebar";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new System.Drawing.Size(6, 26);
            // 
            // btnProperty
            // 
            btnProperty.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnProperty.Image = (System.Drawing.Image)resources.GetObject("btnProperty.Image");
            btnProperty.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnProperty.Name = "btnProperty";
            btnProperty.Size = new System.Drawing.Size(66, 23);
            btnProperty.Text = "property";
            // 
            // tvInfo
            // 
            tvInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            tvInfo.Location = new System.Drawing.Point(8, 182);
            tvInfo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tvInfo.Name = "tvInfo";
            tvInfo.Size = new System.Drawing.Size(753, 251);
            tvInfo.TabIndex = 58;
            // 
            // sldMasterVolume
            // 
            sldMasterVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            sldMasterVolume.Location = new System.Drawing.Point(8, 38);
            sldMasterVolume.Name = "sldMasterVolume";
            sldMasterVolume.Size = new System.Drawing.Size(138, 48);
            sldMasterVolume.TabIndex = 99;
            // 
            // ch_ctrl1
            // 
            ch_ctrl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            ch_ctrl1.Location = new System.Drawing.Point(6, 101);
            ch_ctrl1.Name = "ch_ctrl1";
            ch_ctrl1.Size = new System.Drawing.Size(116, 74);
            ch_ctrl1.TabIndex = 105;
            // 
            // ch_ctrl2
            // 
            ch_ctrl2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            ch_ctrl2.Location = new System.Drawing.Point(134, 101);
            ch_ctrl2.Name = "ch_ctrl2";
            ch_ctrl2.Size = new System.Drawing.Size(116, 74);
            ch_ctrl2.TabIndex = 106;
            // 
            // timeBar
            // 
            timeBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            timeBar.Location = new System.Drawing.Point(204, 38);
            timeBar.Name = "timeBar";
            timeBar.Size = new System.Drawing.Size(557, 45);
            timeBar.TabIndex = 110;
            // 
            // chkLoop
            // 
            chkLoop.Appearance = System.Windows.Forms.Appearance.Button;
            chkLoop.Location = new System.Drawing.Point(152, 36);
            chkLoop.Name = "chkLoop";
            chkLoop.Size = new System.Drawing.Size(46, 26);
            chkLoop.TabIndex = 111;
            chkLoop.Text = "loop";
            chkLoop.UseVisualStyleBackColor = true;
            // 
            // btnRewind
            // 
            btnRewind.Location = new System.Drawing.Point(152, 61);
            btnRewind.Name = "btnRewind";
            btnRewind.Size = new System.Drawing.Size(46, 26);
            btnRewind.TabIndex = 112;
            btnRewind.Text = "<=";
            btnRewind.UseVisualStyleBackColor = true;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new System.Drawing.Size(6, 26);
            // 
            // btnSend
            // 
            btnSend.CheckOnClick = true;
            btnSend.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnSend.Image = (System.Drawing.Image)resources.GetObject("btnSend.Image");
            btnSend.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnSend.Name = "btnSend";
            btnSend.Size = new System.Drawing.Size(42, 23);
            btnSend.Text = "send";
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new System.Drawing.Size(6, 26);
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(773, 443);
            Controls.Add(btnRewind);
            Controls.Add(chkLoop);
            Controls.Add(timeBar);
            Controls.Add(ch_ctrl1);
            Controls.Add(ch_ctrl2);
            Controls.Add(sldMasterVolume);
            Controls.Add(tvInfo);
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
        private NBagOfUis.TextViewer tvInfo;
        private System.Windows.Forms.ToolStripButton btnLogMidi;
        private System.Windows.Forms.ToolStripButton btnKillMidi;
        private ChannelControl ch_ctrl1;
        private ChannelControl ch_ctrl2;
        private TimeBar timeBar;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.CheckBox chkLoop;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnGo;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton btnTimebar;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton btnProperty;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripButton btnSend;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
    }
}

