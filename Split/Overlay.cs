using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Split
{
	public class Overlay : Form
	{
		public const string WINDOW_NAME = "Grand Theft Auto V";

		private IntPtr handle = Overlay.FindWindow(null, "Grand Theft Auto V");

		private ScreenCapture sc1 = new ScreenCapture();

		private Image shot;

		private Image shot2;

		private IContainer components = null;

		public Overlay()
		{
			this.InitializeComponent();
			this.shot = this.sc1.CaptureWindow(this.handle);
			this.shot2 = this.shot;
		}

		protected override void Dispose(bool disposing)
		{
			if ((!disposing ? false : this.components != null))
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		private void InitializeComponent()
		{
			base.SuspendLayout();
			base.AutoScaleDimensions = new SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(284, 261);
			base.Name = "Overlay";
			this.Text = "Screen Split";
			base.TopMost = true;
			base.Load += new EventHandler(this.Overlay_Load);
			base.ResumeLayout(false);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.DrawImage(this.shot, 0, 0, 800, 600);
			e.Graphics.DrawImage(this.shot2, 800, 0, 800, 600);
		}

		private void Overlay_Load(object sender, EventArgs e)
		{
			this.BackColor = Color.Wheat;
			base.TransparencyKey = Color.Wheat;
			base.TopMost = true;
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.DoubleBuffered = true;
			int windowLong = Overlay.GetWindowLong(base.Handle, -20);
			Overlay.SetWindowLong(base.Handle, -20, windowLong | 524288 | 32);
			base.Size = new System.Drawing.Size(1600, 600);
			base.Top = 240;
			base.Left = 50;
			Overlay.SetForegroundWindow(this.handle);
			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer()
			{
				Interval = 1
			};
			timer.Tick += new EventHandler(this.redraw);
			timer.Start();
		}

		private void redraw(object sender, EventArgs e)
		{
			if (!base.IsDisposed)
			{
				Split.Splitter.change = true;
				Split.Splitter.changed = true;
				this.shot2.Dispose();
				Split.Splitter.mrse.WaitOne();
				this.shot2 = this.sc1.CaptureWindow(this.handle);
				Split.Splitter.change = false;
				Split.Splitter.changed = true;
				this.shot.Dispose();
				Split.Splitter.mrse.WaitOne();
				this.shot = this.sc1.CaptureWindow(this.handle);
				base.Invalidate();
			}
		}

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		public struct RECT
		{
			public int left;

			public int top;

			public int right;

			public int bottom;
		}
	}
}