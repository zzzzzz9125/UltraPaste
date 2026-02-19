using System.Drawing;
using System.Windows.Forms;
using UltraPaste.Core;
using UltraPaste.Localization;
using UltraPaste.Models;

namespace UltraPaste.UI.Controls.Panels
{
    internal partial class UltraTableLayoutPanel_PsdImport : UltraTableLayoutPanel
    {
        private CheckBox _expandAllLayersCheckBox;
        private Button _addOtherLayersButton;

        public UltraTableLayoutPanel_PsdImport(UltraPasteSettings.PsdImportSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.PsdImport;

            _expandAllLayersCheckBox = new CheckBox
            {
                Text = I18n.Translation.ExpandAllLayers,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.ExpandAllLayers ?? true
            };
            Controls.Add(_expandAllLayersCheckBox);
            SetColumnSpan(_expandAllLayersCheckBox, 2);

            if (settings != null)
            {
                _expandAllLayersCheckBox.CheckedChanged += (o, e) => { settings.ExpandAllLayers = _expandAllLayersCheckBox.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                _addOtherLayersButton = new Button
                {
                    Text = I18n.Translation.PsdAddOtherLayers,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _addOtherLayersButton.FlatAppearance.BorderSize = 1;
                _addOtherLayersButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_addOtherLayersButton);
                buttonsPanel.SetColumnSpan(_addOtherLayersButton, 2);

                _addOtherLayersButton.Click += UltraPasteCommon.PsdAddOtherLayers;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);

            I18n.LanguageChanged += (o, e) => RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            Name = I18n.Translation.PsdImport;
            _expandAllLayersCheckBox.Text = I18n.Translation.ExpandAllLayers;
            if (_addOtherLayersButton != null)
            {
                _addOtherLayersButton.Text = I18n.Translation.PsdAddOtherLayers;
            }
        }
    }
}
