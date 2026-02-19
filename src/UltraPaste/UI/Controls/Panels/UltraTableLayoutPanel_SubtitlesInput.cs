#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste.UI.Controls.Panels
{
    using UltraPaste.Core;
    using UltraPaste.Localization;
    using UltraPaste.Models;
    using UltraPaste.Utilities;

    internal partial class UltraTableLayoutPanel_SubtitlesInput : UltraPaste.UI.Controls.UltraTableLayoutPanel
    {
        private Label _maxCharactersLabel;
        private Label _maxLinesLabel;
        private CheckBox _ignoreWordCheckBox;
        private CheckBox _multipleTracksCheckBox;
        private CheckBox _useUniversalCheckBox;
        private Button _applySplitButton;
        private Button _addToTimelineButton;

        public UltraTableLayoutPanel_SubtitlesInput(UltraPasteSettings.SubtitlesImportSettings settings) : base()
        {
            Name = I18n.Translation.SubtitlesInputBox;
            RowCount = 6;
            RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            TextBox currentText = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = string.Empty,
                Font = new Font(I18n.Translation.Font, 10),
                Dock = DockStyle.Fill,
                Multiline = true,
                BackColor = VegasCommonHelper.UIColors[1].R > 0x7F ? VegasCommonHelper.UIColors[0] : VegasCommonHelper.UIColors[1],
                ForeColor = VegasCommonHelper.UIColors[1].R > 0x7F ? VegasCommonHelper.UIColors[1] : VegasCommonHelper.UIColors[0],
                ReadOnly = true
            };
            Controls.Add(currentText);
            SetColumnSpan(currentText, 4);

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
                Text = settings?.InputBoxMaxCharacters.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxCharactersTextBox);

            _ignoreWordCheckBox = new CheckBox
            {
                Text = I18n.Translation.SubtitlesIgnoreWord,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.InputBoxIgnoreWord ?? false
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
                Text = settings?.InputBoxMaxLines.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxLinesTextBox);

            _multipleTracksCheckBox = new CheckBox
            {
                Text = I18n.Translation.SubtitlesMultipleTracks,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.InputBoxMultipleTracks ?? false
            };
            Controls.Add(_multipleTracksCheckBox);

            _useUniversalCheckBox = new CheckBox
            {
                Text = I18n.Translation.SubtitlesInputBoxUseUniversal,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.InputBoxUseUniversal ?? true
            };
            Controls.Add(_useUniversalCheckBox);

            _applySplitButton = new Button
            {
                Text = I18n.Translation.SubtitlesInputBoxApplyTextSplitting,
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            _applySplitButton.FlatAppearance.BorderSize = 1;
            _applySplitButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            Controls.Add(_applySplitButton);
            SetColumnSpan(_applySplitButton, 2);

            _addToTimelineButton = new Button
            {
                Text = I18n.Translation.SubtitlesInputBoxAddToTimeline,
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            _addToTimelineButton.FlatAppearance.BorderSize = 1;
            _addToTimelineButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            Controls.Add(_addToTimelineButton);
            SetColumnSpan(_addToTimelineButton, 2);

            TextBox inputText = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = string.Empty,
                Font = new Font(I18n.Translation.Font, 15),
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = VegasCommonHelper.UIColors[1].R > 0x7F ? VegasCommonHelper.UIColors[1] : VegasCommonHelper.UIColors[0],
                ForeColor = VegasCommonHelper.UIColors[1].R > 0x7F ? VegasCommonHelper.UIColors[0] : VegasCommonHelper.UIColors[1]
            };
            Controls.Add(inputText);
            SetColumnSpan(inputText, 4);

            UltraPasteCommon.InputBoxTextBox = inputText;

            inputText.MouseWheel += (o, e) =>
            {
                if ((ModifierKeys & Keys.Control) == 0)
                {
                    return;
                }

                float newSize = Math.Max(1, inputText.Font.Size + (e.Delta > 0 ? 1 : -1));
                inputText.Font = new Font(inputText.Font.Name, newSize);
            };

            if (settings != null)
            {
                _useUniversalCheckBox.CheckedChanged += (o, e) => { settings.InputBoxUseUniversal = _useUniversalCheckBox.Checked; };

                maxCharactersTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(maxCharactersTextBox, out int value))
                    {
                        settings.InputBoxMaxCharacters = Math.Max(0, value);
                        UpdateSubtitlesPreview(settings, inputText, currentText);
                    }
                };
                maxCharactersTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                _ignoreWordCheckBox.CheckedChanged += (o, e) =>
                {
                    settings.InputBoxIgnoreWord = _ignoreWordCheckBox.Checked;
                    UpdateSubtitlesPreview(settings, inputText, currentText);
                };

                maxLinesTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(maxLinesTextBox, out int value))
                    {
                        settings.InputBoxMaxLines = Math.Max(0, value);
                        UpdateSubtitlesPreview(settings, inputText, currentText);
                    }
                };
                maxLinesTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                _multipleTracksCheckBox.CheckedChanged += (o, e) =>
                {
                    settings.InputBoxMultipleTracks = _multipleTracksCheckBox.Checked;
                    UpdateSubtitlesPreview(settings, inputText, currentText);
                };

                _applySplitButton.Click += (o, e) =>
                {
                    if (UltraPasteCommon.InputBoxSubtitlesData?.Subtitles.Count > 0)
                    {
                        UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(settings.InputBoxMaxCharacters, settings.InputBoxIgnoreWord, settings.InputBoxMaxLines, settings.InputBoxMultipleTracks);
                        List<string> lines = new List<string>();
                        foreach (SubtitlesData.Subtitle subtitle in UltraPasteCommon.InputBoxSubtitlesData.Subtitles)
                        {
                            lines.Add(string.Join("\r\n", subtitle.TextLines));
                        }

                        inputText.Text = string.Join("\r\n", lines);
                    }
                };

                _addToTimelineButton.Click += (o, e) =>
                {
                    using (UndoBlock undo = new UndoBlock(UltraPasteCommon.Vegas.Project, I18n.Translation.SubtitlesInputBox))
                    {
                        //UltraPasteCommon.DoPaste(true);
                    }
                };

                inputText.TextChanged += (o, e) => { UpdateSubtitlesPreview(settings, inputText, currentText); };
            }

            I18n.LanguageChanged += (o, e) => RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            Name = I18n.Translation.SubtitlesInputBox;
            _maxCharactersLabel.Text = I18n.Translation.SubtitlesMaxCharacters;
            _maxLinesLabel.Text = I18n.Translation.SubtitlesMaxLines;
            _ignoreWordCheckBox.Text = I18n.Translation.SubtitlesIgnoreWord;
            _multipleTracksCheckBox.Text = I18n.Translation.SubtitlesMultipleTracks;
            _useUniversalCheckBox.Text = I18n.Translation.SubtitlesInputBoxUseUniversal;
            _applySplitButton.Text = I18n.Translation.SubtitlesInputBoxApplyTextSplitting;
            _addToTimelineButton.Text = I18n.Translation.SubtitlesInputBoxAddToTimeline;
        }

        private static void UpdateSubtitlesPreview(UltraPasteSettings.SubtitlesImportSettings settings, TextBox inputText, TextBox preview)
        {
            UltraPasteCommon.InputBoxString = inputText.Text;
            UltraPasteCommon.InputBoxSubtitlesData = SubtitlesData.Parser.ParseFromStrings(UltraPasteCommon.InputBoxString);
            UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(settings.InputBoxMaxCharacters, settings.InputBoxIgnoreWord, settings.InputBoxMaxLines, settings.InputBoxMultipleTracks);
            preview.Text = UltraPasteCommon.InputBoxSubtitlesData.Subtitles.Count > 0 ? string.Join("\r\n", UltraPasteCommon.InputBoxSubtitlesData.Subtitles[0].TextLines) : string.Empty;
        }
    }
}
