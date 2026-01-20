#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste.UltraControls
{
    using Utilities;
    internal partial class UltraTableLayoutPanel_SubtitlesInput : UltraTableLayoutPanel
    {
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

            Label label = new Label
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
                Text = settings?.InputBoxMaxCharacters.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxCharactersTextBox);

            CheckBox ignoreWord = new CheckBox
            {
                Text = I18n.Translation.SubtitlesIgnoreWord,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.InputBoxIgnoreWord ?? false
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
                Text = settings?.InputBoxMaxLines.ToString() ?? "0",
                Dock = DockStyle.Fill
            };
            Controls.Add(maxLinesTextBox);

            CheckBox multipleTracks = new CheckBox
            {
                Text = I18n.Translation.SubtitlesMultipleTracks,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.InputBoxMultipleTracks ?? false
            };
            Controls.Add(multipleTracks);

            CheckBox useUniversal = new CheckBox
            {
                Text = I18n.Translation.SubtitlesInputBoxUseUniversal,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.InputBoxUseUniversal ?? true
            };
            Controls.Add(useUniversal);

            Button applySplitButton = new Button
            {
                Text = I18n.Translation.SubtitlesInputBoxApplyTextSplitting,
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            applySplitButton.FlatAppearance.BorderSize = 1;
            applySplitButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            Controls.Add(applySplitButton);
            SetColumnSpan(applySplitButton, 2);

            Button addToTimelineButton = new Button
            {
                Text = I18n.Translation.SubtitlesInputBoxAddToTimeline,
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            addToTimelineButton.FlatAppearance.BorderSize = 1;
            addToTimelineButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            Controls.Add(addToTimelineButton);
            SetColumnSpan(addToTimelineButton, 2);

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
                useUniversal.CheckedChanged += (o, e) => { settings.InputBoxUseUniversal = useUniversal.Checked; };

                maxCharactersTextBox.TextChanged += (o, e) =>
                {
                    if (TextBox_Int_TryParse(maxCharactersTextBox, out int value))
                    {
                        settings.InputBoxMaxCharacters = Math.Max(0, value);
                        UpdateSubtitlesPreview(settings, inputText, currentText);
                    }
                };
                maxCharactersTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                ignoreWord.CheckedChanged += (o, e) =>
                {
                    settings.InputBoxIgnoreWord = ignoreWord.Checked;
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

                multipleTracks.CheckedChanged += (o, e) =>
                {
                    settings.InputBoxMultipleTracks = multipleTracks.Checked;
                    UpdateSubtitlesPreview(settings, inputText, currentText);
                };

                applySplitButton.Click += (o, e) =>
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

                addToTimelineButton.Click += (o, e) =>
                {
                    using (UndoBlock undo = new UndoBlock(UltraPasteCommon.Vegas.Project, I18n.Translation.SubtitlesInputBox))
                    {
                        //UltraPasteCommon.DoPaste(true);
                    }
                };

                inputText.TextChanged += (o, e) => { UpdateSubtitlesPreview(settings, inputText, currentText); };
            }
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
