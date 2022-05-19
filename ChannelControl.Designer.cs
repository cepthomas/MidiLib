
namespace MidiLib
{
    partial class ChannelControl
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
            this.components = new System.ComponentModel.Container();
            this.lblChannelNumber = new System.Windows.Forms.Label();
            //this.sldVolume = new NBagOfUis.Slider();
            this.lblPatch = new System.Windows.Forms.Label();
            this.lblSolo = new System.Windows.Forms.Label();
            this.lblMute = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // lblChannelNumber
            // 
            this.lblChannelNumber.AutoSize = true;
            this.lblChannelNumber.Location = new System.Drawing.Point(2, 8);
            this.lblChannelNumber.Name = "lblChannelNumber";
            this.lblChannelNumber.Size = new System.Drawing.Size(18, 20);
            this.lblChannelNumber.TabIndex = 3;
            this.lblChannelNumber.Text = "#";
            this.toolTip1.SetToolTip(this.lblChannelNumber, "Select channel");
            //// 
            //// sldVolume
            //// 
            //this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            //this.sldVolume.DrawColor = System.Drawing.Color.White;
            //this.sldVolume.Label = "vol";
            //this.sldVolume.Location = new System.Drawing.Point(195, 2);
            //this.sldVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            //this.sldVolume.Maximum = 2D;
            //this.sldVolume.Minimum = 0D;
            //this.sldVolume.Name = "sldVolume";
            //this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            //this.sldVolume.Resolution = 0.05D;
            //this.sldVolume.Size = new System.Drawing.Size(80, 30);
            //this.sldVolume.TabIndex = 43;
            //this.toolTip1.SetToolTip(this.sldVolume, "Channel volume");
            //this.sldVolume.Value = 1D;
            // 
            // lblPatch
            // 
            this.lblPatch.Location = new System.Drawing.Point(44, 7);
            this.lblPatch.Name = "lblPatch";
            this.lblPatch.Size = new System.Drawing.Size(144, 25);
            this.lblPatch.TabIndex = 44;
            this.lblPatch.Text = "?????";
            this.lblPatch.Click += new System.EventHandler(this.Patch_Click);
            // 
            // lblSolo
            // 
            this.lblSolo.Location = new System.Drawing.Point(290, 7);
            this.lblSolo.Name = "lblSolo";
            this.lblSolo.Size = new System.Drawing.Size(20, 20);
            this.lblSolo.TabIndex = 45;
            this.lblSolo.Text = "S";
            this.toolTip1.SetToolTip(this.lblSolo, "Solo");
            // 
            // lblMute
            // 
            this.lblMute.Location = new System.Drawing.Point(315, 7);
            this.lblMute.Name = "lblMute";
            this.lblMute.Size = new System.Drawing.Size(20, 20);
            this.lblMute.TabIndex = 46;
            this.lblMute.Text = "M";
            this.toolTip1.SetToolTip(this.lblMute, "Mute");
            // 
            // ChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblMute);
            this.Controls.Add(this.lblSolo);
            this.Controls.Add(this.lblPatch);
            //this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.lblChannelNumber);
            this.Name = "ChannelControl";
            this.Size = new System.Drawing.Size(345, 38);
            this.Load += new System.EventHandler(this.ChannelControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lblChannelNumber;
        //private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.Label lblPatch;
        private System.Windows.Forms.Label lblSolo;
        private System.Windows.Forms.Label lblMute;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
