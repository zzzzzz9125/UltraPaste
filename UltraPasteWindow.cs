#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste
{
    using UltraControls;
    public sealed partial class UltraPasteWindow : DockableControl
    {
        public Vegas MyVegas;

        public UltraPasteWindow()
            : base("UltraPaste_Window")
        {
            InitializeComponent();
            PersistDockWindowState = true;
        }

        public override DockWindowStyle DefaultDockWindowStyle
        {
            get { return DockWindowStyle.Floating; }
        }

        public override Size DefaultFloatingSize
        {
            get { return new Size(500, 400); }
        }

        protected override void OnLoaded(EventArgs args)
        {
            base.OnLoaded(args);
        }

        protected override void InitLayout()
        {
            base.InitLayout();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)

                base.OnVisibleChanged(e);
        }

        private readonly System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            MinimumSize = new Size(433, 172);
            BackColor = Common.UIColors[0];
            ForeColor = Common.UIColors[1];
            DisplayName = string.Format("{0} {1}", L.UltraPaste, UltraPasteCommon.VERSION);
            Font = new Font(L.Font, 9);

            TableLayoutPanel l = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                ColumnCount = 2,
                Dock = DockStyle.Fill
            };
            l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            l.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            Controls.Add(l);

            UltraTabControl tab = new UltraTabControl();
            l.Controls.Add(tab);
            l.SetColumnSpan(tab, 2);

            tab.TabPages.Add(UltraTabPage.From(UltraTableLayoutPanel.From(UltraPasteCommon.Settings.General, this)));
            tab.TabPages.Add(UltraTabPage.From(UltraTableLayoutPanel.From(UltraPasteCommon.Settings.ClipboardImage, this)));
            tab.TabPages.Add(UltraTabPage.From(UltraTableLayoutPanel.From(UltraPasteCommon.Settings.ReaperData, this)));
            tab.TabPages.Add(UltraTabPage.From(UltraTableLayoutPanel.From(UltraPasteCommon.Settings.PsdImport, this)));
            tab.TabPages.Add(UltraTabPage.From(UltraTableLayoutPanel.From(UltraPasteCommon.Settings.SubtitlesImport, this)));
            tab.TabPages.Add(UltraTabPage.From(UltraTableLayoutPanel.From(UltraPasteCommon.Settings.MediaImport, this, true)));
            tab.TabPages.Add(UltraTabPage.From(UltraTableLayoutPanel.From(UltraPasteCommon.Settings.VegasData, this)));

            tab.SelectedIndex = UltraPasteCommon.Settings.General.LastTabIndex;
            //tab.MinimumSize = new Size(500, 400);

            Closed += (o, e) =>
            {
                if (tab != null)
                {
                    UltraPasteCommon.Settings.General.LastTabIndex = tab.SelectedIndex;
                }
                UltraPasteCommon.Settings.SaveToFile();
            };

            SetFocusToMainTrackViewForControlsMouseClick<Button>(this);

            ResumeLayout(false);
            PerformLayout();
        }

        void SetFocusToMainTrackViewForControlsMouseClick<T>(Control ctrl) where T : Control
        {
            if (ctrl is T)
            {
                ctrl.MouseClick += (o, e) =>
                {
                    SetFocusToMainTrackView();
                };
            }
            foreach (Control c in ctrl.Controls)
            {
                SetFocusToMainTrackViewForControlsMouseClick<T>(c);
            }
        }
    }
}