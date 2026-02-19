#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UI.Windows
{
    using UltraPaste.Core;
    using UltraPaste.Localization;
    using UltraPaste.UI.Controls.Panels;
    using UltraPaste.Utilities;

    internal sealed partial class UltraWindow_SubtitlesEdit : DockableControl
    {
        public Vegas MyVegas;
        private Control _contentPanel;

        public UltraWindow_SubtitlesEdit()
            : base("UltraWindow_SubtitlesEdit")
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
            DisplayName = string.Format("{0} - {1}", I18n.Translation.UltraPaste, I18n.Translation.SubtitlesEditBox);

            Font = new Font(I18n.Translation.Font, 9);

            _contentPanel = new UltraTableLayoutPanel_SubtitlesEdit();
            Controls.Add(_contentPanel);

            I18n.LanguageChanged += (o, e) => RefreshWindowLocalization();

            ResumeLayout(false);
            PerformLayout();
        }

        private void RefreshWindowLocalization()
        {
            BeginInvoke(new Action(() =>
            {
                try
                {
                    _contentPanel.SuspendLayout();
                    
                    DisplayName = string.Format("{0} - {1}", I18n.Translation.UltraPaste, I18n.Translation.SubtitlesEditBox);
                    Font = new Font(I18n.Translation.Font, 9);
                    
                    _contentPanel.ResumeLayout(true);
                    _contentPanel.PerformLayout();
                    
                    PerformLayout();
                    Invalidate(true);
                }
                catch { }
            }));
        }
    }
}