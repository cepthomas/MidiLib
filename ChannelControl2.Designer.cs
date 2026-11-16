using System.Windows.Forms;


namespace Ephemera.MidiLib
{
    partial class ChannelControl2
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblInfo = new Label();
            toolTip = new ToolTip(components);
            sldVolume = new Ephemera.NBagOfUis.Slider();
            panChannel = new Ephemera.NBagOfUis.Pan();
            lblSolo = new Label();
            lblMute = new Label();
            btnFx = new Button();
            meter1 = new Ephemera.NBagOfUis.Meter();
            meter2 = new Ephemera.NBagOfUis.Meter();
            SuspendLayout();
            // 
            // lblInfo
            // 
            lblInfo.BorderStyle = BorderStyle.FixedSingle;
            lblInfo.Location = new System.Drawing.Point(3, 0);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new System.Drawing.Size(196, 24);
            lblInfo.TabIndex = 1;
            lblInfo.Text = "info";
            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.Location = new System.Drawing.Point(75, 27);
            sldVolume.Name = "sldVolume";
            sldVolume.Size = new System.Drawing.Size(78, 34);
            sldVolume.TabIndex = 2;
            // 
            // panChannel
            // 
            panChannel.BorderStyle = BorderStyle.FixedSingle;
            panChannel.Location = new System.Drawing.Point(3, 27);
            panChannel.Name = "panChannel";
            panChannel.Size = new System.Drawing.Size(66, 34);
            panChannel.TabIndex = 3;
            // 
            // lblSolo
            // 
            lblSolo.BorderStyle = BorderStyle.FixedSingle;
            lblSolo.Location = new System.Drawing.Point(205, 0);
            lblSolo.Name = "lblSolo";
            lblSolo.Size = new System.Drawing.Size(22, 19);
            lblSolo.TabIndex = 4;
            lblSolo.Text = "S";
            // 
            // lblMute
            // 
            lblMute.BorderStyle = BorderStyle.FixedSingle;
            lblMute.Location = new System.Drawing.Point(205, 22);
            lblMute.Name = "lblMute";
            lblMute.Size = new System.Drawing.Size(22, 19);
            lblMute.TabIndex = 5;
            lblMute.Text = "M";
            // 
            // btnFx
            // 
            btnFx.FlatStyle = FlatStyle.Flat;
            btnFx.Location = new System.Drawing.Point(163, 27);
            btnFx.Name = "btnFx";
            btnFx.Size = new System.Drawing.Size(36, 34);
            btnFx.TabIndex = 6;
            btnFx.Text = "FX";
            btnFx.UseVisualStyleBackColor = true;
            // 
            // meter1
            // 
            meter1.BorderStyle = BorderStyle.FixedSingle;
            meter1.Location = new System.Drawing.Point(233, 0);
            meter1.Name = "meter1";
            meter1.Size = new System.Drawing.Size(20, 61);
            meter1.TabIndex = 7;
            // 
            // meter2
            // 
            meter2.BorderStyle = BorderStyle.FixedSingle;
            meter2.Location = new System.Drawing.Point(259, 0);
            meter2.Name = "meter2";
            meter2.Size = new System.Drawing.Size(20, 61);
            meter2.TabIndex = 8;
            // 
            // ChannelControl2
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(meter2);
            Controls.Add(meter1);
            Controls.Add(btnFx);
            Controls.Add(lblMute);
            Controls.Add(lblSolo);
            Controls.Add(panChannel);
            Controls.Add(sldVolume);
            Controls.Add(lblInfo);
            Name = "ChannelControl2";
            Size = new System.Drawing.Size(284, 65);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.ToolTip toolTip;
        private NBagOfUis.Slider sldVolume;
        private NBagOfUis.Pan panChannel;
        private System.Windows.Forms.Label lblSolo;
        private System.Windows.Forms.Label lblMute;
        private Button btnFx;
        private NBagOfUis.Meter meter1;
        private NBagOfUis.Meter meter2;
    }
}
