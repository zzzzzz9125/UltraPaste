using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    using Utilities;
    internal class UltraTabControl : TabControl
    {
        public UltraTabControl()
        {
            Dock = DockStyle.Fill;
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            Multiline = true;

            DrawItem += (o, e) =>
            {
                using (SolidBrush backgroundBrush = new SolidBrush(VegasCommonHelper.UIColors[0]))
                {
                    e.Graphics.FillRegion(backgroundBrush, new Region(new Rectangle(0, 0, Width, Height)));
                }

                using (StringFormat format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                })
                using (Font font = new Font(I18n.Translation.Font, 9))
                using (SolidBrush textBrush = new SolidBrush(VegasCommonHelper.UIColors[1]))
                {
                    for (int i = 0; i < TabCount; i++)
                    {
                        e.Graphics.DrawString(TabPages[i].Text, font, textBrush, GetTabRect(i), format);
                    }
                }
            };
        }
    }
}
