#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste
{
    using Utilities;
    using UltraControls;

    internal sealed partial class UltraWindow_Main : DockableControl
    {
        public Vegas MyVegas;

        public UltraWindow_Main()
            : base("UltraWindow_Main")
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

        private static readonly List<char> InvalidChars = new List<char>(Path.GetInvalidFileNameChars());
        public static string SanitizeFileName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            char[] chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (InvalidChars.Contains(chars[i]))
                {
                    chars[i] = '_';
                }
            }
            return new string(chars);
        }

        private void InitializeComponent()
        {
            MyVegas = UltraPasteCommon.Vegas;
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            MinimumSize = new Size(433, 172);
            BackColor = VegasCommonHelper.UIColors[0];
            ForeColor = VegasCommonHelper.UIColors[1];
            DisplayName = string.Format("{0} {1}", I18n.Translation.UltraPaste, UltraPasteCommon.VERSION);
            Font = new Font(I18n.Translation.Font, 9);

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
            
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_General(UltraPasteCommon.Settings.General, this)));
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_ClipboardImage(UltraPasteCommon.Settings.ClipboardImage, this)));
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_ReaperData(UltraPasteCommon.Settings.ReaperData, this)));
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_CapCutData(UltraPasteCommon.Settings.CapCutData, this)));
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_PsdImport(UltraPasteCommon.Settings.PsdImport, this)));
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_SubtitlesImport(UltraPasteCommon.Settings.SubtitlesImport, this)));
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_MediaImport(UltraPasteCommon.Settings.MediaImport, this, true)));
            tab.TabPages.Add(new UltraTabPage(new UltraTableLayoutPanel_VegasData(UltraPasteCommon.Settings.VegasData, this)));

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