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
    using Utilities;
    using UltraControls;

    internal sealed partial class UltraWindow_SubtitlesInput : DockableControl
    {
        public Vegas MyVegas;

        public UltraWindow_SubtitlesInput()
            : base("UltraWindow_SubtitlesInput")
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
            get { return new Size(500, 450); }
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
            MyVegas = UltraPasteCommon.Vegas;
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            MinimumSize = new Size(433, 172);
            BackColor = VegasCommonHelper.UIColors[0];
            ForeColor = VegasCommonHelper.UIColors[1];
            DisplayName = string.Format("{0} - {1}", I18n.Translation.UltraPaste, I18n.Translation.SubtitlesInputBox);

            Font = new Font(I18n.Translation.Font, 9);

            TableLayoutPanel l = new UltraTableLayoutPanel_SubtitlesInput(UltraPasteCommon.Settings.SubtitlesImport);
            Controls.Add(l);

            Closing += (o, e) =>
            {
                UltraPasteCommon.InputBoxSubtitlesData = null;
            };

            Closed += (o, e) =>
            {
                UltraPasteCommon.InputBoxSubtitlesData = null;
            };

            ResumeLayout(false);
            PerformLayout();
        }
    }
}