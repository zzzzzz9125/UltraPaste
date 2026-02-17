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
    using UltraPaste.Utilities;

    internal partial class UltraTableLayoutPanel_SubtitlesEdit : UltraPaste.UI.Controls.UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_SubtitlesEdit() : base()
        {
            Name = I18n.Translation.SubtitlesEditBox;
            RowCount = 3;
            RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            RowStyles.Add(new RowStyle(SizeType.Percent, 80));

            TableLayoutPanel line1Panel = new TableLayoutPanel
            {
                ColumnCount = 5,
                RowCount = 1,
                AutoSize = true,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.AddColumns
            };
            line1Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < line1Panel.ColumnCount; i++)
            {
                line1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / line1Panel.ColumnCount));
            }

            ComboBox fontCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            foreach (FontFamily family in FontFamily.Families)
            {
                fontCombo.Items.Add(family.Name);
            }
            if (fontCombo.Items.Contains(SystemFonts.DefaultFont.Name))
            {
                fontCombo.SelectedItem = SystemFonts.DefaultFont.Name;
            }
            line1Panel.Controls.Add(fontCombo);

            ComboBox sizeCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DropDownStyle = ComboBoxStyle.DropDown,
                Dock = DockStyle.Fill
            };
            sizeCombo.Items.AddRange(new object[] { "9", "10", "12", "14", "16", "18", "20", "22", "24", "36", "42", "48", "52" });
            sizeCombo.Text = "12";
            line1Panel.Controls.Add(sizeCombo);

            ToolTip toolTip = new ToolTip();

            Button boldButton = new Button
            {
                Text = "B",
                Margin = new Padding(6, 6, 6, 6),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.DefaultFont.Name, SystemFonts.DefaultFont.Size, FontStyle.Bold)
            };
            boldButton.FlatAppearance.BorderSize = 1;
            boldButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            toolTip.SetToolTip(boldButton, I18n.Translation.FontBold);
            line1Panel.Controls.Add(boldButton);

            Button italicButton = new Button
            {
                Text = "I",
                Margin = new Padding(6, 6, 6, 6),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.DefaultFont.Name, SystemFonts.DefaultFont.Size, FontStyle.Italic)
            };
            italicButton.FlatAppearance.BorderSize = 1;
            italicButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            toolTip.SetToolTip(italicButton, I18n.Translation.FontItalic);
            line1Panel.Controls.Add(italicButton);

            ComboBox alignmentCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            alignmentCombo.Items.AddRange(new object[]
            {
                I18n.Translation.TextAlignmentLeft,
                I18n.Translation.TextAlignmentCenter,
                I18n.Translation.TextAlignmentRight
            });
            alignmentCombo.SelectedIndex = 0;
            line1Panel.Controls.Add(alignmentCombo);

            Controls.Add(line1Panel);
            SetColumnSpan(line1Panel, 4);

            TableLayoutPanel line2Panel = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                Dock = DockStyle.Fill,
                GrowStyle = TableLayoutPanelGrowStyle.AddColumns
            };
            line2Panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < line2Panel.ColumnCount; i++)
            {
                line2Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / line2Panel.ColumnCount));
            }

            Label colorLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.TextColor,
                AutoSize = true,
                Dock = DockStyle.Fill
            };
            line2Panel.Controls.Add(colorLabel);

            Button colorButton = new Button
            {
                Text = I18n.Translation.ChooseColor,
                Margin = new Padding(9, 6, 11, 6),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill
            };
            colorButton.FlatAppearance.BorderSize = 1;
            colorButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            line2Panel.Controls.Add(colorButton);

            ColorDialog colorDialog = new ColorDialog();
            colorButton.Click += (o, e) =>
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    colorButton.BackColor = colorDialog.Color;
                    colorButton.ForeColor = colorDialog.Color.GetBrightness() < 0.5f ? Color.White : Color.Black;
                }
            };

            Button applyButton = new Button
            {
                Text = "Apply",
                Margin = new Padding(6, 6, 6, 6),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill
            };
            applyButton.FlatAppearance.BorderSize = 1;
            applyButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
            toolTip.SetToolTip(applyButton, "Apply");
            line2Panel.Controls.Add(applyButton);

            CheckBox autoApplyCheckBox = new CheckBox
            {
                Text = "Auto Apply",
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = true
            };
            line2Panel.Controls.Add(autoApplyCheckBox);

            Controls.Add(line2Panel);
            SetColumnSpan(line2Panel, 4);

            RichTextBox editor = new RichTextBox
            {
                AutoSize = true,
                Margin = new Padding(6, 6, 6, 6),
                Dock = DockStyle.Fill,
                Font = new Font(I18n.Translation.Font, 12)
            };
            Controls.Add(editor);
            SetColumnSpan(editor, 4);

            Action applySelectionChange = () =>
            {
                editor.Focus();
                editor.Select(editor.SelectionStart, editor.SelectionLength);
            };

            fontCombo.SelectedIndexChanged += (o, e) =>
            {
                string fontName = fontCombo.SelectedItem as string ?? editor.Font.FontFamily.Name;
                float fontSize = editor.SelectionFont?.Size ?? editor.Font.Size;
                FontStyle fontStyle = editor.SelectionFont?.Style ?? editor.Font.Style;
                editor.SelectionFont = new Font(fontName, fontSize, fontStyle);
                applySelectionChange();
            };

            sizeCombo.TextChanged += (o, e) =>
            {
                if (!float.TryParse(sizeCombo.Text, out float size) || size <= 0)
                {
                    return;
                }

                string fontName = editor.SelectionFont?.FontFamily.Name ?? editor.Font.FontFamily.Name;
                FontStyle fontStyle = editor.SelectionFont?.Style ?? editor.Font.Style;
                editor.SelectionFont = new Font(fontName, size, fontStyle);
                applySelectionChange();
            };

            boldButton.Click += (o, e) =>
            {
                FontStyle currentStyle = editor.SelectionFont?.Style ?? editor.Font.Style;
                FontStyle newStyle = currentStyle ^ FontStyle.Bold;
                string fontName = editor.SelectionFont?.FontFamily.Name ?? editor.Font.FontFamily.Name;
                float fontSize = editor.SelectionFont?.Size ?? editor.Font.Size;
                editor.SelectionFont = new Font(fontName, fontSize, newStyle);
                applySelectionChange();
            };

            italicButton.Click += (o, e) =>
            {
                FontStyle currentStyle = editor.SelectionFont?.Style ?? editor.Font.Style;
                FontStyle newStyle = currentStyle ^ FontStyle.Italic;
                string fontName = editor.SelectionFont?.FontFamily.Name ?? editor.Font.FontFamily.Name;
                float fontSize = editor.SelectionFont?.Size ?? editor.Font.Size;
                editor.SelectionFont = new Font(fontName, fontSize, newStyle);
                applySelectionChange();
            };

            alignmentCombo.SelectedIndexChanged += (o, e) =>
            {
                switch (alignmentCombo.SelectedIndex)
                {
                    case 0:
                        editor.SelectionAlignment = HorizontalAlignment.Left;
                        break;
                    case 1:
                        editor.SelectionAlignment = HorizontalAlignment.Center;
                        break;
                    case 2:
                        editor.SelectionAlignment = HorizontalAlignment.Right;
                        break;
                }

                applySelectionChange();
            };

            applyButton.Click += (o, e) =>
            {
                using (UndoBlock undo = new UndoBlock(UltraPasteCommon.Vegas.Project, "SubtitlesEdit"))
                {
                    TextMediaGeneratorHelper.SetRichTextToEvents(UltraPasteCommon.Vegas.Project.GetSelectedEvents<VideoEvent>(), editor.Rtf);
                }
            };

            bool updatingFromEditor = false;

            Action updateControlsFromEditor = () =>
            {
                if (updatingFromEditor)
                {
                    return;
                }

                updatingFromEditor = true;

                try
                {
                    Font currentFont = editor.SelectionFont ?? editor.Font;

                    if (fontCombo.Items.Contains(currentFont.FontFamily.Name))
                    {
                        fontCombo.SelectedItem = currentFont.FontFamily.Name;
                    }

                    sizeCombo.Text = currentFont.Size.ToString();

                    boldButton.Font = new Font(boldButton.Font.FontFamily, boldButton.Font.Size,
                        (currentFont.Style & FontStyle.Bold) != 0 ? FontStyle.Bold : FontStyle.Regular);

                    italicButton.Font = new Font(italicButton.Font.FontFamily, italicButton.Font.Size,
                        (currentFont.Style & FontStyle.Italic) != 0 ? FontStyle.Italic : FontStyle.Regular);

                    switch (editor.SelectionAlignment)
                    {
                        case HorizontalAlignment.Left:
                            alignmentCombo.SelectedIndex = 0;
                            break;
                        case HorizontalAlignment.Center:
                            alignmentCombo.SelectedIndex = 1;
                            break;
                        case HorizontalAlignment.Right:
                            alignmentCombo.SelectedIndex = 2;
                            break;
                    }
                }
                finally
                {
                    updatingFromEditor = false;
                }
            };

            editor.Tag = true;

            editor.SelectionChanged += (o, e) =>
            {
                //updateControlsFromEditor();
            };

            editor.TextChanged += (o, e) =>
            {
                //updateControlsFromEditor();

                if (autoApplyCheckBox.Checked && (bool)editor.Tag)
                {
                    List<VideoEvent> evs = UltraPasteCommon.Vegas.Project.GetSelectedEvents<VideoEvent>();


                    if (evs.Count > 0)
                    {
                        using (UndoBlock undo = new UndoBlock(UltraPasteCommon.Vegas.Project, "SubtitlesEdit"))
                        {
                            TextMediaGeneratorHelper.SetRichTextToEvents(new List<VideoEvent>() { evs[0] }, editor.Rtf, false);
                            evs.RemoveAt(0);
                            if (evs.Count > 0)
                            {
                                TextMediaGeneratorHelper.SetRichTextToEvents(evs, editor.Rtf, true);
                            }
                        }
                    }

                }
            };

            //UltraPasteCommon.Vegas.TrackEventStateChanged += (o, e) =>
            //{
            //    if (autoApplyCheckBox != null && editor != null && autoApplyCheckBox.Checked)
            //    {
            //        List<VideoEvent> selectedEvents = UltraPasteCommon.Vegas.Project.GetSelectedEvents<VideoEvent>();
            //        if (selectedEvents.Count > 0)
            //        {
            //            List<string> strs = TextMediaGeneratorHelper.GetRichTextFromEvent(selectedEvents);
            //            if (strs.Count > 0)
            //            {
            //                if (editor.Rtf != strs[0])
            //                {
            //                    editor.Tag = false;
            //                    editor.Rtf = strs[0];
            //                    updateControlsFromEditor();
            //                    editor.Tag = true;
            //                }
            //            }
            //        }
            //    }x
            //};
        }
    }
}
