namespace OpenRA.IEPlug
{
	partial class ORAHost : _ORAHost
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
			this.button1 = new System.Windows.Forms.Button();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.Ticker = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.BackColor = System.Drawing.Color.Black;
			this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.button1.Font = new System.Drawing.Font("Tahoma", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.button1.ForeColor = System.Drawing.Color.White;
			this.button1.Location = new System.Drawing.Point(0, 0);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(800, 600);
			this.button1.TabIndex = 1;
			this.button1.Text = "&Launch OpenRA";
			this.button1.UseVisualStyleBackColor = false;
			this.button1.Click += new System.EventHandler(this.OnClickStart);
			// 
			// timer1
			// 
			this.timer1.Interval = 1000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// Ticker
			// 
			this.Ticker.Interval = 1;
			this.Ticker.Tick += new System.EventHandler(this.Ticker_Tick);
			// 
			// ORAHost
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.button1);
			this.Name = "ORAHost";
			this.Size = new System.Drawing.Size(800, 600);
			this.Load += new System.EventHandler(this.ORAHost_Load);
			this.VisibleChanged += new System.EventHandler(this.ORAHost_VisibleChanged);
			this.MouseLeave += new System.EventHandler(this.ORAHost_MouseLeave);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);
			this.Click += new System.EventHandler(this.OnClick);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMoved);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnKeyUp);
			this.ParentChanged += new System.EventHandler(this.ORAHost_ParentChanged);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
			this.Resize += new System.EventHandler(this.OnResize);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ORAHost_KeyPress);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMouseUp);
			this.MouseEnter += new System.EventHandler(this.Renderer_MouseEnter);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Timer Ticker;
	}
}
