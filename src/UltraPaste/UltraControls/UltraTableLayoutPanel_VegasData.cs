using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    internal partial class UltraTableLayoutPanel_VegasData : UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_VegasData(UltraPasteSettings.VegasDataSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base()
        {
            Name = I18n.Translation.VegasData;

            Label label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.VegImport,
                AutoSize = true
            };
            Controls.Add(label);
            SetColumnSpan(label, 2);

            ComboBox importCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.VegImportType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(importCombo);
            SetColumnSpan(importCombo, 2);

            CheckBox selectivelyPasteEventAttributes = new CheckBox
            {
                Text = I18n.Translation.SelectivelyPasteEventAttributes,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.SelectivelyPasteEventAttributes ?? true
            };
            Controls.Add(selectivelyPasteEventAttributes);
            SetColumnSpan(selectivelyPasteEventAttributes, 2);

            CheckBox runScript = new CheckBox
            {
                Text = I18n.Translation.RunScript,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.RunScript ?? true
            };
            Controls.Add(runScript);
            SetColumnSpan(runScript, 2);

            if (settings != null)
            {
                if (formControl is Form form)
                {
                    form.Load += (o, e) => { importCombo.SelectedIndex = settings.VegImportType; };
                }
                else if (formControl is UserControl uc)
                {
                    uc.Load += (o, e) => { importCombo.SelectedIndex = settings.VegImportType; };
                }

                importCombo.SelectedIndexChanged += (o, e) => { settings.VegImportType = importCombo.SelectedIndex; };
                selectivelyPasteEventAttributes.CheckedChanged += (o, e) => { settings.SelectivelyPasteEventAttributes = selectivelyPasteEventAttributes.Checked; };
                runScript.CheckedChanged += (o, e) => { settings.RunScript = runScript.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                Button button = new Button
                {
                    Text = I18n.Translation.GenerateMixedVegasClipboardData,
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

                button.Click += UltraPasteCommon.GenerateMixedVegasClipboardData;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
        }
    }
}
