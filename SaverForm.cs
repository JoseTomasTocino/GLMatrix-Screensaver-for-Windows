using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MatrixSCR
{
    internal sealed class SaverForm : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr handle, out RECT rect);
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr handle);
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr handle);
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr value);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLong32(IntPtr handle, int index, IntPtr value);

        private const int GwlStyle = -16;
        private const long WsChild = 0x40000000L;
        private const long WsVisible = 0x10000000L;
        private const uint WmClose = 0x0010;

        public static event EventHandler ExitRequested;

        private readonly bool preview;
        private readonly IntPtr previewParent;
        private SaverSettings settings;
        private int rendererSeed;
        private Timer timer;
        private System.Threading.Timer previewWatchdog;
        private readonly Stopwatch lifetime = Stopwatch.StartNew();
        private readonly Stopwatch frameClock = Stopwatch.StartNew();
        private MatrixRenderer renderer;
        private Point initialMouse;
        private long lastFrameTicks;

        public SaverForm(Rectangle bounds, SaverSettings settings)
        {
            preview = false;
            Bounds = bounds;
            Initialize(settings);
            TopMost = true;
        }

        public SaverForm(IntPtr parent, SaverSettings settings)
        {
            preview = true;
            previewParent = parent;
            RECT rect;
            GetClientRect(parent, out rect);
            Bounds = new Rectangle(0, 0, Math.Max(1, rect.Right), Math.Max(1, rect.Bottom));
            Initialize(settings);
        }

        private void Initialize(SaverSettings settings)
        {
            this.settings = settings;
            rendererSeed = unchecked(Environment.TickCount ^ GetHashCode());
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            DoubleBuffered = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.Opaque, true);

            timer = new Timer();
            timer.Interval = Math.Max(15, 1000 / settings.Fps);
            timer.Tick += TickFrame;
            timer.Start();
            initialMouse = Control.MousePosition;

            KeyDown += ExitOnInput;
            MouseDown += ExitOnInput;
            MouseMove += CheckMouse;
            Shown += OnShown;
            Resize += OnResize;
            FormClosed += OnClosed;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (preview && previewParent != IntPtr.Zero)
            {
                SetParent(Handle, previewParent);
                SetWindowLongPtr(Handle, GwlStyle, new IntPtr(WsChild | WsVisible));
                previewWatchdog = new System.Threading.Timer(CheckPreviewParent, null, 250, 250);
            }
        }

        private void CheckPreviewParent(object state)
        {
            if (previewParent == IntPtr.Zero || !IsWindow(previewParent) ||
                GetParent(Handle) != previewParent)
                PostMessage(Handle, WmClose, IntPtr.Zero, IntPtr.Zero);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (renderer != null) renderer.Render();
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        private void OnShown(object sender, EventArgs e)
        {
            initialMouse = Control.MousePosition;
            renderer = new MatrixRenderer(Handle, Math.Max(1, Width), Math.Max(1, Height),
                settings, rendererSeed);
            renderer.Advance(0.03);
            lastFrameTicks = frameClock.ElapsedTicks;
            if (!preview) Activate();
        }

        private void TickFrame(object sender, EventArgs e)
        {
            if (preview)
            {
                if (previewParent == IntPtr.Zero || !IsWindow(previewParent) ||
                    GetParent(Handle) != previewParent)
                {
                    Close();
                    return;
                }
            }
            else if (lifetime.ElapsedMilliseconds > 500 && MouseHasMoved())
            {
                RequestExit();
                return;
            }

            long now = frameClock.ElapsedTicks;
            double dt = (now - lastFrameTicks) / (double)Stopwatch.Frequency;
            lastFrameTicks = now;
            if (renderer == null) return;
            renderer.Advance(Math.Max(0.001, Math.Min(0.1, dt)));
        }

        private void OnResize(object sender, EventArgs e)
        {
            if (renderer != null && Width > 0 && Height > 0)
                renderer.Resize(Width, Height);
        }

        private void ExitOnInput(object sender, EventArgs e)
        {
            if (!preview && lifetime.ElapsedMilliseconds > 500)
                RequestExit();
        }

        private void CheckMouse(object sender, MouseEventArgs e)
        {
            if (preview || lifetime.ElapsedMilliseconds < 500) return;
            if (MouseHasMoved()) RequestExit();
        }

        private bool MouseHasMoved()
        {
            Point current = Control.MousePosition;
            return Math.Abs(current.X - initialMouse.X) > 4 ||
                Math.Abs(current.Y - initialMouse.Y) > 4;
        }

        private static void RequestExit()
        {
            EventHandler handler = ExitRequested;
            if (handler != null) handler(null, EventArgs.Empty);
        }

        private void OnClosed(object sender, FormClosedEventArgs e)
        {
            timer.Stop();
            timer.Dispose();
            if (previewWatchdog != null) previewWatchdog.Dispose();
            if (renderer != null) renderer.Dispose();
        }

        private static IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(handle, index, value) :
                SetWindowLong32(handle, index, value);
        }
    }
}
