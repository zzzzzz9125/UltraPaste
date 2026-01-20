using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    using Utilities;
    internal class UltraOneClickGroupBox : GroupBox
    {
        public UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel)
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Dock = DockStyle.Fill;
            Text = I18n.Translation.OneClick;
            ForeColor = VegasCommonHelper.UIColors[1];

            oneClickPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                ColumnCount = 2
            };
            oneClickPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            oneClickPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Controls.Add(oneClickPanel);
        }
    }
}
