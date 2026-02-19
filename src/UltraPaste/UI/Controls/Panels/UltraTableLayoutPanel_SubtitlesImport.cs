using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste.UI.Controls.Panels
{
    using UltraPaste.Core;
    using UltraPaste.Models;
    using UltraPaste.Utilities;
    using UltraPaste.Localization;

    internal partial class UltraTableLayoutPanel_SubtitlesImport : UltraTableLayoutPanel
    {
        private Label _mediaGeneratorLabel;
        private Label _presetLabel;
        private Label _maxCharactersLabel;
        private Label _maxLinesLabel;
        private Label _defaultLengthLabel;
        private CheckBox _addTextMediaGeneratorsCheckBox;
        private CheckBox _addRegionsCheckBox;
        private CheckBox _ignoreWordCheckBox;
        private CheckBox _multipleTracksCheckBox;
        private CheckBox _closeGapCheckBox;
        private Button _applyButton;
        private Button _titlesTextButton;

        public UltraTableLayoutPanel_SubtitlesImport(UltraPasteSettings.SubtitlesImportSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.SubtitlesImport;

            _mediaGeneratorLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.TextMediaGenerator,
                AutoSize = true
            };
            Controls.Add(_mediaGeneratorLabel);

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

            _addTextMediaGeneratorsCheckBox = new CheckBox
            {
                Text = I18n.Translation.AddTextMediaGenerators,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.AddTextMediaGenerators ?? true
            };
            Controls.Add(_addTextMediaGeneratorsCheckBox);
            SetColumnSpan(_addTextMediaGeneratorsCheckBox, 2);

            _presetLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.TextMediaGeneratorPresetName,
                AutoSize = true
            };
            Controls.Add(_presetLabel);

            ComboBox presetNameCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Dock = DockStyle.Fill
            };
            Controls.Add(presetNameCombo);

            _addRegionsCheckBox = new CheckBox
            {
                Text = I18n.Translation.AddRegions,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.AddRegions ?? false
            };
            Controls.Add(_addRegionsCheckBox);
            SetColumnSpan(_addRegionsCheckBox, 2);

            _maxCharactersLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.SubtitlesMaxCharacters,
                AutoSize = true
            };
            Controls.Add(_maxCharactersLabel);

            TextBox maxCharactersTextBox = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.MaxCharacters.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxCharactersTextBox);

            _ignoreWordCheckBox = new CheckBox
            {
                Text = I18n.Translation.SubtitlesIgnoreWord,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.IgnoreWord ?? false
            };
            Controls.Add(_ignoreWordCheckBox);
            SetColumnSpan(_ignoreWordCheckBox, 2);

            _maxLinesLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.SubtitlesMaxLines,
                AutoSize = true
            };
            Controls.Add(_maxLinesLabel);

            TextBox maxLinesTextBox = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.MaxLines.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxLinesTextBox);

            _multipleTracksCheckBox = new CheckBox
            {
                Text = I18n.Translation.SubtitlesMultipleTracks,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.MultipleTracks ?? false
            };
            Controls.Add(_multipleTracksCheckBox);
            SetColumnSpan(_multipleTracksCheckBox, 2);

            _defaultLengthLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.SubtitlesDefaultLength,
                AutoSize = true
            };
            Controls.Add(_defaultLengthLabel);

            TextBox defaultLengthTextBox = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.DefaultLengthSeconds.ToString() ?? "5",
                Dock = DockStyle.Fill
            };
            Controls.Add(defaultLengthTextBox);

            _closeGapCheckBox = new CheckBox
            {
                Text = I18n.Translation.CloseGap,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.CloseGap ?? true
            };
            Controls.Add(_closeGapCheckBox);
            SetColumnSpan(_closeGapCheckBox, 2);

            if (settings != null)
            {
                _addTextMediaGeneratorsCheckBox.CheckedChanged += (o, e) => { settings.AddTextMediaGenerators = _addTextMediaGeneratorsCheckBox.Checked; };
                _addRegionsCheckBox.CheckedChanged += (o, e) => { settings.AddRegions = _addRegionsCheckBox.Checked; };

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

                _ignoreWordCheckBox.CheckedChanged += (o, e) => { settings.IgnoreWord = _ignoreWordCheckBox.Checked; };

                maxLinesTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(maxLinesTextBox, out int value))
                    {
                        settings.MaxLines = Math.Max(0, value);
                    }
                };
                maxLinesTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                _multipleTracksCheckBox.CheckedChanged += (o, e) => { settings.MultipleTracks = _multipleTracksCheckBox.Checked; };

                defaultLengthTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(defaultLengthTextBox, out int value))
                    {
                        settings.DefaultLengthSeconds = Math.Max(0, value);
                    }
                };
                defaultLengthTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                _closeGapCheckBox.CheckedChanged += (o, e) => { settings.CloseGap = _closeGapCheckBox.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                _applyButton = new Button
                {
                    Text = I18n.Translation.SubtitlesApplyToSelectedEvents,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _applyButton.FlatAppearance.BorderSize = 1;
                _applyButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_applyButton);

                _applyButton.Click += UltraPasteCommon.SubtitlesApplyToSelectedEvents;

                _titlesTextButton = new Button
                {
                    Text = I18n.Translation.SubtitlesTitlesAndTextToProTypeTitler.Replace("&", "&&"),
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _titlesTextButton.FlatAppearance.BorderSize = 1;
                _titlesTextButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_titlesTextButton);

                _titlesTextButton.Click += UltraPasteCommon.SubtitlesTitlesAndTextToProTypeTitler;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);

            I18n.LanguageChanged += (o, e) => RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            Name = I18n.Translation.SubtitlesImport;
            _mediaGeneratorLabel.Text = I18n.Translation.TextMediaGenerator;
            _presetLabel.Text = I18n.Translation.TextMediaGeneratorPresetName;
            _maxCharactersLabel.Text = I18n.Translation.SubtitlesMaxCharacters;
            _maxLinesLabel.Text = I18n.Translation.SubtitlesMaxLines;
            _defaultLengthLabel.Text = I18n.Translation.SubtitlesDefaultLength;
            _addTextMediaGeneratorsCheckBox.Text = I18n.Translation.AddTextMediaGenerators;
            _addRegionsCheckBox.Text = I18n.Translation.AddRegions;
            _ignoreWordCheckBox.Text = I18n.Translation.SubtitlesIgnoreWord;
            _multipleTracksCheckBox.Text = I18n.Translation.SubtitlesMultipleTracks;
            _closeGapCheckBox.Text = I18n.Translation.CloseGap;
            if (_applyButton != null)
            {
                _applyButton.Text = I18n.Translation.SubtitlesApplyToSelectedEvents;
            }
            if (_titlesTextButton != null)
            {
                _titlesTextButton.Text = I18n.Translation.SubtitlesTitlesAndTextToProTypeTitler.Replace("&", "&&");
            }
        }
    }
}
