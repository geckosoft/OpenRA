namespace OpenRA.IEPlug.Test
{
	partial class Form1
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.oraHost1 = new OpenRA.IEPlug.ORAHost();
			this.SuspendLayout();
			// 
			// oraHost1
			// 
			this.oraHost1.BackColor = System.Drawing.Color.Red;
			this.oraHost1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.oraHost1.Location = new System.Drawing.Point(0, 0);
			this.oraHost1.Margin = new System.Windows.Forms.Padding(0);
			this.oraHost1.Name = "oraHost1";
			this.oraHost1.Size = new System.Drawing.Size(834, 616);
			this.oraHost1.TabIndex = 0;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(834, 616);
			this.Controls.Add(this.oraHost1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Form1";
			this.ResumeLayout(false);

		}

		#endregion

		private ORAHost oraHost1;
	}
}

