using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UltraPaste
{
    public class TransparentMouseFollower : Form
    {
        private const int MouseOffsetX = 15;
        private const int MouseOffsetY = 15;

        public static TransparentMouseFollower Current {  get; private set; }
        public Label Label { get; set; }

        public TransparentMouseFollower(string str)
        {
            InitializeFormSettings(str);
            InitializeTimer();
        }

        private void InitializeFormSettings(string str)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;

            BackColor = Common.UIColors[0];
            TransparencyKey = Common.UIColors[0];

            Label textLabel = new Label
            {
                Text = str,
                ForeColor = Common.UIColors[1],
                Font = new Font(L.Font, 12),
                AutoSize = true
            };
            Label = textLabel;

            Controls.Add(textLabel);
        }

        private void InitializeTimer()
        {
            Timer followTimer = new Timer
            {
                Interval = 10
            };

            followTimer.Tick += (s, e) =>
            {
                Point mousePos = MousePosition;
                this.Location = new Point(
                    mousePos.X + MouseOffsetX,
                    mousePos.Y + MouseOffsetY);
            };

            followTimer.Start();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Cursor.Hide();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor.Show();
        }

        [STAThread]
        public static void Refresh(string str)
        {
            if (Current == null)
            {
                Current = new TransparentMouseFollower(str);
                Application.Run(Current);
            }
            else
            {
                Current.Label.Text = str;
            }
        }

        public static void Exit()
        {
            Current?.Close();
            Current?.Dispose();
            Current = null;
        }
    }
}
