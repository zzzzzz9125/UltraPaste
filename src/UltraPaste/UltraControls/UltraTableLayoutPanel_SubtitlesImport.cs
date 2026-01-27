using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste.UltraControls
{
    using Utilities;
    internal partial class UltraTableLayoutPanel_SubtitlesImport : UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_SubtitlesImport(UltraPasteSettings.SubtitlesImportSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.SubtitlesImport;

            Label label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.TextMediaGenerator,
                AutoSize = true
            };
            Controls.Add(label);

            ComboBox mediaGeneratorCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = new BindingSource(TextMediaGeneratorHelper.ValidTextNumbersAndNames, null),
                ValueMember = "Key",
                DisplayMember = "Value",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(mediaGeneratorCombo);

            CheckBox addTextMediaGenerators = new CheckBox
            {
                Text = I18n.Translation.AddTextMediaGenerators,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.AddTextMediaGenerators ?? true
            };
            Controls.Add(addTextMediaGenerators);
            SetColumnSpan(addTextMediaGenerators, 2);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.TextMediaGeneratorPresetName,
                AutoSize = true
            };
            Controls.Add(label);

            ComboBox presetNameCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Dock = DockStyle.Fill
            };
            Controls.Add(presetNameCombo);

            CheckBox addRegions = new CheckBox
            {
                Text = I18n.Translation.AddRegions,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.AddRegions ?? false
            };
            Controls.Add(addRegions);
            SetColumnSpan(addRegions, 2);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.SubtitlesMaxCharacters,
                AutoSize = true
            };
            Controls.Add(label);

            TextBox maxCharactersTextBox = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.MaxCharacters.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxCharactersTextBox);

            CheckBox ignoreWord = new CheckBox
            {
                Text = I18n.Translation.SubtitlesIgnoreWord,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.IgnoreWord ?? false
            };
            Controls.Add(ignoreWord);
            SetColumnSpan(ignoreWord, 2);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.SubtitlesMaxLines,
                AutoSize = true
            };
            Controls.Add(label);

            TextBox maxLinesTextBox = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.MaxLines.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxLinesTextBox);

            CheckBox multipleTracks = new CheckBox
            {
                Text = I18n.Translation.SubtitlesMultipleTracks,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.MultipleTracks ?? false
            };
            Controls.Add(multipleTracks);
            SetColumnSpan(multipleTracks, 2);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.SubtitlesDefaultLength,
                AutoSize = true
            };
            Controls.Add(label);

            TextBox defaultLengthTextBox = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.DefaultLengthSeconds.ToString() ?? "5",
                Dock = DockStyle.Fill
            };
            Controls.Add(defaultLengthTextBox);

            CheckBox closeGap = new CheckBox
            {
                Text = I18n.Translation.CloseGap,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.CloseGap ?? true
            };
            Controls.Add(closeGap);
            SetColumnSpan(closeGap, 2);

            if (settings != null)
            {
                addTextMediaGenerators.CheckedChanged += (o, e) => { settings.AddTextMediaGenerators = addTextMediaGenerators.Checked; };
                addRegions.CheckedChanged += (o, e) => { settings.AddRegions = addRegions.Checked; };

                if (formControl is Form form)
                {
                    form.Load += (o, e) =>
                    {
                        mediaGeneratorCombo.SelectedItem = ((mediaGeneratorCombo.DataSource as BindingSource)?.DataSource as Dictionary<int, string>)[settings.MediaGeneratorType];
                        presetNameCombo.DataSource = TextMediaGeneratorHelper.TextPlugIns[settings.MediaGeneratorType].GetAvailablePresets();
                        presetNameCombo.Text = settings.PresetNames[settings.MediaGeneratorType];
                    };
                }
                else if (formControl is UserControl uc)
                {
                    uc.Load += (o, e) =>
                    {
                        mediaGeneratorCombo.SelectedItem = ((mediaGeneratorCombo.DataSource as BindingSource)?.DataSource as Dictionary<int, string>)[settings.MediaGeneratorType];
                        presetNameCombo.Tag = "ChangedByCode";
                        presetNameCombo.DataSource = TextMediaGeneratorHelper.TextPlugIns[settings.MediaGeneratorType].GetAvailablePresets();
                        presetNameCombo.Text = settings.PresetNames[settings.MediaGeneratorType];
                        presetNameCombo.Tag = null;
                    };
                }

                mediaGeneratorCombo.SelectedIndexChanged += (o, e) =>
                {
                    settings.MediaGeneratorType = mediaGeneratorCombo.SelectedIndex;

                    int? key = (mediaGeneratorCombo.SelectedItem as KeyValuePair<int, string>?)?.Key;
                    if (key != null)
                    {
                        settings.MediaGeneratorType = key.Value;
                    }

                    presetNameCombo.Tag = "ChangedByCode";
                    presetNameCombo.DataSource = TextMediaGeneratorHelper.TextPlugIns[settings.MediaGeneratorType].GetAvailablePresets();
                    presetNameCombo.Text = settings.PresetNames[settings.MediaGeneratorType];
                    presetNameCombo.Tag = null;
                };

                presetNameCombo.TextChanged += (o, e) =>
                {
                    if (Equals(presetNameCombo.Tag, "ChangedByCode"))
                    {
                        return;
                    }

                    settings.PresetNames[settings.MediaGeneratorType] = presetNameCombo.Text;
                };

                maxCharactersTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(maxCharactersTextBox, out int value))
                    {
                        settings.MaxCharacters = Math.Max(0, value);
                    }
                };
                maxCharactersTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                ignoreWord.CheckedChanged += (o, e) => { settings.IgnoreWord = ignoreWord.Checked; };

                maxLinesTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(maxLinesTextBox, out int value))
                    {
                        settings.MaxLines = Math.Max(0, value);
                    }
                };
                maxLinesTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                multipleTracks.CheckedChanged += (o, e) => { settings.MultipleTracks = multipleTracks.Checked; };

                defaultLengthTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(defaultLengthTextBox, out int value))
                    {
                        settings.DefaultLengthSeconds = Math.Max(0, value);
                    }
                };
                defaultLengthTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                closeGap.CheckedChanged += (o, e) => { settings.CloseGap = closeGap.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                Button button = new Button
                {
                    Text = I18n.Translation.SubtitlesApplyToSelectedEvents,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += UltraPasteCommon.SubtitlesApplyToSelectedEvents;

                button = new Button
                {
                    Text = I18n.Translation.SubtitlesTitlesAndTextToProTypeTitler.Replace("&", "&&"),
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += UltraPasteCommon.SubtitlesTitlesAndTextToProTypeTitler;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
        }
    }
}
