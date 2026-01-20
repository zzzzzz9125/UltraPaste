using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    internal partial class UltraTableLayoutPanel_PsdImport : UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_PsdImport(UltraPasteSettings.PsdImportSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.PsdImport;

            CheckBox expandAllLayers = new CheckBox
            {
                Text = I18n.Translation.ExpandAllLayers,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.ExpandAllLayers ?? true
            };
            Controls.Add(expandAllLayers);
            SetColumnSpan(expandAllLayers, 2);

            if (settings != null)
            {
                expandAllLayers.CheckedChanged += (o, e) => { settings.ExpandAllLayers = expandAllLayers.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                Button button = new Button
                {
                    Text = I18n.Translation.PsdAddOtherLayers,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);
                buttonsPanel.SetColumnSpan(button, 2);

                button.Click += UltraPasteCommon.PsdAddOtherLayers;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
        }
    }
}
