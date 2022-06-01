
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
            this.sldVolume = new NBagOfUis.Slider();
            this.lblPatch = new System.Windows.Forms.Label();
            this.lblChannelNumber = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DrawColor = System.Drawing.Color.White;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(197, 5);
            this.sldVolume.Maximum = 10D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.Resolution = 0.1D;
            this.sldVolume.Size = new System.Drawing.Size(83, 30);
            this.sldVolume.TabIndex = 50;
            this.sldVolume.Value = 5D;
            // 
            // lblPatch
            // 
            this.lblPatch.Location = new System.Drawing.Point(47, 9);
            this.lblPatch.Name = "lblPatch";
            this.lblPatch.Size = new System.Drawing.Size(144, 25);
            this.lblPatch.TabIndex = 49;
            this.lblPatch.Text = "?????";
            // 
            // lblChannelNumber
            // 
            this.lblChannelNumber.Location = new System.Drawing.Point(5, 10);
            this.lblChannelNumber.Name = "lblChannelNumber";
            this.lblChannelNumber.Size = new System.Drawing.Size(36, 20);
            this.lblChannelNumber.TabIndex = 48;
            this.lblChannelNumber.Text = "#";
            // 
            // ChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.lblPatch);
            this.Controls.Add(this.lblChannelNumber);
            this.Name = "ChannelControl";
            this.Size = new System.Drawing.Size(285, 41);
            this.ResumeLayout(false);

        }

        #endregion

        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.Label lblPatch;
        private System.Windows.Forms.Label lblChannelNumber;
    }
}
