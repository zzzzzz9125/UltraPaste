using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    using Utilities;
    internal class UltraTabPage : TabPage
    {
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
                Text = panel.Name;
                Controls.Add(panel);
            }
        }
    }
}
