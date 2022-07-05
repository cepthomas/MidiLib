
namespace MidiLib
{
    partial class SimpleChannelControl
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
            this.sldVolume = new NBagOfUis.Slider();
            this.lblPatch = new System.Windows.Forms.Label();
            this.cmbChannel = new System.Windows.Forms.ComboBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DrawColor = System.Drawing.Color.White;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(217, 5);
            this.sldVolume.Maximum = 10D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.Resolution = 0.1D;
            this.sldVolume.Size = new System.Drawing.Size(83, 30);
            this.sldVolume.TabIndex = 50;
            this.toolTip1.SetToolTip(this.sldVolume, "Channel Volume");
            this.sldVolume.Value = 5D;
            // 
            // lblPatch
            // 
            this.lblPatch.Location = new System.Drawing.Point(67, 9);
            this.lblPatch.Name = "lblPatch";
            this.lblPatch.Size = new System.Drawing.Size(144, 25);
            this.lblPatch.TabIndex = 49;
            this.lblPatch.Text = "?????";
            this.toolTip1.SetToolTip(this.lblPatch, "Patch");
            // 
            // cmbChannel
            // 
            this.cmbChannel.BackColor = System.Drawing.SystemColors.Control;
            this.cmbChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbChannel.FormattingEnabled = true;
            this.cmbChannel.Location = new System.Drawing.Point(5, 7);
            this.cmbChannel.Name = "cmbChannel";
            this.cmbChannel.Size = new System.Drawing.Size(52, 28);
            this.cmbChannel.TabIndex = 51;
            this.toolTip1.SetToolTip(this.cmbChannel, "Midi Channel Number");
            // 
            // SimpleChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmbChannel);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.lblPatch);
            this.Name = "SimpleChannelControl";
            this.Size = new System.Drawing.Size(309, 41);
            this.ResumeLayout(false);

        }

        #endregion

        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.Label lblPatch;
        private System.Windows.Forms.ComboBox cmbChannel;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
