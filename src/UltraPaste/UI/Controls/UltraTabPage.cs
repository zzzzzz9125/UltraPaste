using System.Windows.Forms;

namespace UltraPaste.UI.Controls
{
    using UltraPaste.Utilities;
    using UltraPaste.Localization;

    internal class UltraTabPage : TabPage
    {
        private Panel _panel;

        public UltraTabPage()
        {
            BackColor = VegasCommonHelper.UIColors[0];
            ForeColor = VegasCommonHelper.UIColors[1];
            BorderStyle = BorderStyle.FixedSingle;
        }

        public UltraTabPage(Panel panel) : this()
        {
            if (panel != null)
            {
                _panel = panel;
                Text = panel.Name;
                Controls.Add(panel);

                I18n.LanguageChanged += (o, e) => RefreshLocalization();
            }
        }

        private void RefreshLocalization()
        {
            if (_panel != null)
            {
                Text = _panel.Name;
            }
        }
    }
}
