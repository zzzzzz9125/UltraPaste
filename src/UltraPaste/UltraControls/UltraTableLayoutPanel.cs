using System;
using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    internal partial class UltraTableLayoutPanel : TableLayoutPanel
    {
        public UltraTableLayoutPanel()
        {
            Dock = DockStyle.Fill;
            AutoSize = true;
            Anchor = AnchorStyles.Top | AnchorStyles.Left;
            GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            ColumnCount = 4;
            for (int i = 0; i < ColumnCount; i++)
            {
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / ColumnCount));
            }
        }

        public UltraTableLayoutPanel(UltraPasteSettings.BaseImportSettings settings, ContainerControl formControl) : this()
        {
            Label label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.StartPosition,
                AutoSize = true
            };
            Controls.Add(label);

            ComboBox combo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.StartPositionType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(combo);

            CheckBox cursorToEnd = new CheckBox
            {
                Text = I18n.Translation.CursorToEnd,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.CursorToEnd ?? true
            };
            Controls.Add(cursorToEnd);
            SetColumnSpan(cursorToEnd, 2);

            if (settings != null)
            {
                if (formControl is Form form)
                {
                    form.Load += (o, e) => { combo.SelectedIndex = settings.StartPositionType; };
                }
                else if (formControl is UserControl control)
                {
                    control.Load += (o, e) => { combo.SelectedIndex = settings.StartPositionType; };
                }

                combo.SelectedIndexChanged += (o, e) => { settings.StartPositionType = combo.SelectedIndex; };
                cursorToEnd.CheckedChanged += (o, e) => { settings.CursorToEnd = cursorToEnd.Checked; };
            }
        }

        public static void TextBox_MouseWheel_Int_Max_Zero(object sender, MouseEventArgs e)
        {
            if (e.Delta == 0 || !(sender is TextBox textBox))
            {
                return;
            }

            if (TextBox_Int_TryParse(textBox, out int value))
            {
                textBox.Text = Math.Max(0, value + (e.Delta > 0 ? 1 : -1)).ToString();
            }
        }

        public static bool TextBox_Int_TryParse(TextBox textBox, out int result)
        {
            result = 0;
            string normalized = textBox?.Text?.Replace('０', '0').Replace('１', '1').Replace('２', '2').Replace('３', '3').Replace('４', '4').Replace('５', '5').Replace('６', '6').Replace('７', '7').Replace('８', '8').Replace('９', '9');
            if (!double.TryParse(normalized, out double parsed))
            {
                return false;
            }

            result = (int)parsed;
            return true;
        }
    }
}
