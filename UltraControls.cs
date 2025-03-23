#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste
{
    namespace UltraControls
    {
        public class UltraTabControl : TabControl
        {
            public UltraTabControl()
            {
                Dock = DockStyle.Fill;
                DrawMode = TabDrawMode.OwnerDrawFixed;
                //Appearance = TabAppearance.FlatButtons;
                SizeMode = TabSizeMode.Fixed;
                Multiline = true;

                DrawItem += (o, e) =>
                {
                    e.Graphics.FillRegion(new SolidBrush(Common.UIColors[0]), new System.Drawing.Region(new Rectangle(0, 0, Width, Height)));
                    StringFormat sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    };
                    Font font = new Font(L.Font, 9);
                    SolidBrush sb = new SolidBrush(Common.UIColors[1]);
                    for (int i = 0; i < TabCount; i++)
                    {
                        e.Graphics.DrawString(TabPages[i].Text, font, sb, GetTabRect(i), sf);
                    }
                };
            }
        }

        public class UltraTabPage : TabPage
        {
            public UltraTabPage()
            {
                BackColor = Common.UIColors[0];
                ForeColor = Common.UIColors[1];
                BorderStyle = BorderStyle.FixedSingle;
            }

            public static UltraTabPage From(Panel p)
            {
                UltraTabPage page = new UltraTabPage();
                if (p != null)
                {
                    page.Text = p.Name;
                    page.Controls.Add(p);
                }
                return page;
            }
        }

        public class UltraOneClickGroupBox : GroupBox
        {
            public UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel)
            {
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Dock = DockStyle.Fill;
                Text = L.OneClick;
                ForeColor = Common.UIColors[1];

                oneClickPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    Anchor = (AnchorStyles.Top | AnchorStyles.Left),
                    GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                    ColumnCount = 2
                };
                oneClickPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                oneClickPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                Controls.Add(oneClickPanel);
            }
        }

        public class UltraTableLayoutPanel : TableLayoutPanel
        {
            public UltraTableLayoutPanel()
            {
                Dock = DockStyle.Fill;
                AutoSize = true;
                Anchor = (AnchorStyles.Top | AnchorStyles.Left);
                GrowStyle = TableLayoutPanelGrowStyle.AddRows;
                ColumnCount = 4;
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }

            public static UltraTableLayoutPanel From(UltraPasteSettings.GeneralSettings set, ContainerControl formControl, bool addOneClickGroup = true)
            {
                UltraTableLayoutPanel p = new UltraTableLayoutPanel() { Name = L.General };

                Label label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.Language,
                    AutoSize = true
                };
                p.Controls.Add(label);
                p.SetColumnSpan(label, 2);

                ComboBox combo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    DataSource = new BindingSource(L.LanguageList, null),
                    ValueMember = "Key",
                    DisplayMember = "Value",
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(combo);
                p.SetColumnSpan(combo, 2);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.ExcludedFiles,
                    AutoSize = true
                };
                p.Controls.Add(label);
                p.SetColumnSpan(label, 2);

                TextBox text = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = set?.ExcludedFiles ?? string.Empty,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(text);
                p.SetColumnSpan(text, 2);

                if (set != null)
                {
                    if (formControl != null)
                    {
                        if (formControl is Form form)
                        {
                            form.Load += (o, e) =>
                            {
                                if (L.LanguageList.ContainsKey(set?.CurrentLanguage))
                                {
                                    combo.SelectedValue = set?.CurrentLanguage;
                                }
                            };
                        }
                        else if (formControl is UserControl control)
                        {
                            control.Load += (o, e) =>
                            {
                                if (L.LanguageList.ContainsKey(set?.CurrentLanguage))
                                {
                                    combo.SelectedValue = set?.CurrentLanguage;
                                }
                            };
                        }
                    }

                    combo.SelectedIndexChanged += (o, e) =>
                    {
                        string key = (combo.SelectedItem as KeyValuePair<string, string>?)?.Key;
                        if (L.LanguageList.ContainsKey(key))
                        {
                            if (set.CurrentLanguage == key)
                            {
                                return;
                            }
                            set.CurrentLanguage = key;
                            L.Localize();

                            if (MessageBox.Show(L.LanguageChange, L.UltraPaste, MessageBoxButtons.OKCancel) == DialogResult.OK)
                            {
                                if (UltraPasteCommon.Vegas.FindDockView("UltraPaste_Window", out IDockView dock) && dock is DockableControl dc)
                                {
                                    dc.Close();
                                }
                                if (UltraPasteCommon.Vegas.FindDockView("UltraPaste_Window_SubtitlesInput", out dock) && dock is DockableControl dcl)
                                {
                                    dcl.Close();
                                }
                            }
                        }
                    };

                    text.TextChanged += (o, e) =>
                    {
                        set.ExcludedFiles = text.Text;
                    };
                }

                if (addOneClickGroup)
                {
                    GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel);
                    p.Controls.Add(oneClickGroup);
                    p.SetColumnSpan(oneClickGroup, 4);

                    Button button = new Button
                    {
                        Text = L.SupportMe,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += (o, e) =>
                    {
                        string link = L.Language == "zh" ? @"https://afdian.tv/a/zzzzzz9125" : @"https://ko-fi.com/zzzzzz9125";
                        System.Diagnostics.Process.Start(link);
                    };

                    button = new Button
                    {
                        Text = L.CheckForUpdate,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += (o, e) =>
                    {
                        System.Diagnostics.Process.Start(@"https://github.com/zzzzzz9125/UltraPaste");
                    };
                }

                label = new Label();
                p.Controls.Add(label);
                p.SetColumnSpan(label, 4);

                return p;
            }

            public static UltraTableLayoutPanel From(UltraPasteSettings.ClipboardImageSettings set, ContainerControl formControl, bool addOneClickGroup = true)
            {
                UltraTableLayoutPanel p = FromBaseImport(set, formControl);
                p.Name = L.ClipboardImage;

                Label label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.ClipboardImageFilePath,
                    AutoSize = true
                };
                p.Controls.Add(label);

                TextBox text = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = set?.FilePath ?? string.Empty,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(text);
                p.SetColumnSpan(text, 3);

                if (set != null)
                {
                    text.TextChanged += (o, e) =>
                    {
                        set.FilePath = text.Text;
                    };
                }

                if (addOneClickGroup)
                {
                    GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel);
                    p.Controls.Add(oneClickGroup);
                    p.SetColumnSpan(oneClickGroup, 4);

                    Button button = new Button
                    {
                        Text = L.SaveSnapshotToClipboard,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += (o, e) => { UltraPasteCommon.Vegas.SaveSnapshot(); };

                    button = new Button
                    {
                        Text = L.SaveSnapshotToClipboardAndFile,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += UltraPasteCommon.SaveSnapshotToClipboardAndFile;
                }

                label = new Label();
                p.Controls.Add(label);
                p.SetColumnSpan(label, 4);

                return p;
            }

            public static UltraTableLayoutPanel From(UltraPasteSettings.ReaperDataSettings set, ContainerControl formControl, bool addOneClickGroup = true)
            {
                UltraTableLayoutPanel p = FromBaseImport(set, formControl);
                p.Name = L.ReaperData;

                CheckBox closeGap = new CheckBox
                {
                    Text = L.CloseGap,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.CloseGap ?? true
                };
                p.Controls.Add(closeGap);
                p.SetColumnSpan(closeGap, 2);

                CheckBox addVideoStreams = new CheckBox
                {
                    Text = L.AddVideoStreams,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.AddVideoStreams ?? true
                };
                p.Controls.Add(addVideoStreams);
                p.SetColumnSpan(addVideoStreams, 2);

                if (set != null)
                {
                    closeGap.CheckedChanged += (o, e) =>
                    {
                        set.CloseGap = closeGap.Checked;
                    };

                    addVideoStreams.CheckedChanged += (o, e) =>
                    {
                        set.AddVideoStreams = addVideoStreams.Checked;
                    };
                }

                if (addOneClickGroup)
                {
                    GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel);
                    p.Controls.Add(oneClickGroup);
                    p.SetColumnSpan(oneClickGroup, 4);

                    Button button = new Button
                    {
                        Text = L.ExportSelectedEventsToReaperData,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += UltraPasteCommon.ExportSelectedEventsToReaperData;

                    button = new Button
                    {
                        Text = L.ExportSelectedTracksToReaperData,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += UltraPasteCommon.ExportSelectedTracksToReaperData;
                }

                Label label = new Label();
                p.Controls.Add(label);
                p.SetColumnSpan(label, 4);

                return p;
            }

            public static UltraTableLayoutPanel From(UltraPasteSettings.PsdImportSettings set, ContainerControl formControl, bool addOneClickGroup = true)
            {
                UltraTableLayoutPanel p = FromBaseImport(set, formControl);
                p.Name = L.PsdImport;

                CheckBox check = new CheckBox
                {
                    Text = L.ExpandAllLayers,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.ExpandAllLayers ?? true
                };
                p.Controls.Add(check);
                p.SetColumnSpan(check, 2);

                if (set != null)
                {
                    check.CheckedChanged += (o, e) =>
                    {
                        set.ExpandAllLayers = check.Checked;
                    };
                }

                if (addOneClickGroup)
                {
                    GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel);
                    p.Controls.Add(oneClickGroup);
                    p.SetColumnSpan(oneClickGroup, 4);

                    Button button = new Button
                    {
                        Text = L.PsdAddOtherLayers,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);
                    oneClickPanel.SetColumnSpan(button, 2);

                    button.Click += UltraPasteCommon.PsdAddOtherLayers;
                }

                Label label = new Label();
                p.Controls.Add(label);
                p.SetColumnSpan(label, 4);

                return p;
            }

            public static UltraTableLayoutPanel GetInputPanel(UltraPasteSettings.SubtitlesImportSettings set)
            {
                UltraTableLayoutPanel p = new UltraTableLayoutPanel
                {
                    Name = L.SubtitlesInputBox,
                    RowCount = 6
                };
                p.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
                p.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
                p.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
                p.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
                p.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

                TextBox currentText = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = "",
                    Font = new Font(L.Font, 10),
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    BackColor = Common.UIColors[1].R > 0x7F ? Common.UIColors[0] : Common.UIColors[1],
                    ForeColor = Common.UIColors[1].R > 0x7F ? Common.UIColors[1] : Common.UIColors[0],
                    ReadOnly = true
                };
                p.Controls.Add(currentText);
                p.SetColumnSpan(currentText, 4);

                Label label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.SubtitlesMaxCharacters,
                    AutoSize = true
                };
                p.Controls.Add(label);

                TextBox maxCharactersTextBox = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = set?.InputBoxMaxCharacters.ToString() ?? "0",
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(maxCharactersTextBox);

                CheckBox ignoreWord = new CheckBox
                {
                    Text = L.SubtitlesIgnoreWord,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.InputBoxIgnoreWord ?? false
                };
                p.Controls.Add(ignoreWord);
                p.SetColumnSpan(ignoreWord, 2);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.SubtitlesMaxLines,
                    AutoSize = true
                };
                p.Controls.Add(label);

                TextBox maxLinesTextBox = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = set?.InputBoxMaxLines.ToString() ?? "0",
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(maxLinesTextBox);

                CheckBox multipleTracks = new CheckBox
                {
                    Text = L.SubtitlesMultipleTracks,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.InputBoxMultipleTracks ?? false
                };
                p.Controls.Add(multipleTracks);

                CheckBox useUniversal = new CheckBox
                {
                    Text = L.SubtitlesInputBoxUseUniversal,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.InputBoxUseUniversal ?? true
                };
                p.Controls.Add(useUniversal);

                Button button = new Button
                {
                    Text = L.SubtitlesInputBoxApplyTextSplitting,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                p.Controls.Add(button);
                p.SetColumnSpan(button, 2);

                Button buttonAdd = new Button
                {
                    Text = L.SubtitlesInputBoxAddToTimeline,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                buttonAdd.FlatAppearance.BorderSize = 1;
                buttonAdd.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                p.Controls.Add(buttonAdd);
                p.SetColumnSpan(buttonAdd, 2);

                TextBox text = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = "",
                    Font = new Font(L.Font, 15),
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = Common.UIColors[1].R > 0x7F ? Common.UIColors[1] : Common.UIColors[0],
                    ForeColor = Common.UIColors[1].R > 0x7F ? Common.UIColors[0] : Common.UIColors[1]
                };
                p.Controls.Add(text);
                p.SetColumnSpan(text, 4);

                UltraPasteCommon.InputBoxTextBox = text;

                text.MouseWheel += (o, e) =>
                {
                    if ((ModifierKeys & Keys.Control) != 0)
                    {
                        text.Font = new Font(text.Font.Name, Math.Max(1, text.Font.Size + (e.Delta > 0 ? 1 : -1)));
                    }
                };

                if (set != null)
                {
                    useUniversal.CheckedChanged += (o, e) =>
                    {
                        set.InputBoxUseUniversal = useUniversal.Checked;
                    };

                    maxCharactersTextBox.TextChanged += (o, e) =>
                    {
                        if (TextBox_Int_TryParse(maxCharactersTextBox, out int tmp))
                        {
                            set.InputBoxMaxCharacters = Math.Max(0, tmp);
                            UltraPasteCommon.InputBoxString = text.Text;
                            UltraPasteCommon.InputBoxSubtitlesData = SubtitlesData.Parser.ParseFromStrings(UltraPasteCommon.InputBoxString);
                            UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(set.InputBoxMaxCharacters, set.InputBoxIgnoreWord, set.InputBoxMaxLines, set.InputBoxMultipleTracks);
                            currentText.Text = string.Join("\r\n", UltraPasteCommon.InputBoxSubtitlesData.Subtitles[0].TextLines);
                        }
                    };

                    maxCharactersTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                    ignoreWord.CheckedChanged += (o, e) =>
                    {
                        set.InputBoxIgnoreWord = ignoreWord.Checked;
                        UltraPasteCommon.InputBoxString = text.Text;
                        UltraPasteCommon.InputBoxSubtitlesData = SubtitlesData.Parser.ParseFromStrings(UltraPasteCommon.InputBoxString);
                        UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(set.InputBoxMaxCharacters, set.InputBoxIgnoreWord, set.InputBoxMaxLines, set.InputBoxMultipleTracks);
                        currentText.Text = string.Join("\r\n", UltraPasteCommon.InputBoxSubtitlesData.Subtitles[0].TextLines);
                    };

                    maxLinesTextBox.TextChanged += (o, e) =>
                    {
                        if (TextBox_Int_TryParse(maxLinesTextBox, out int tmp))
                        {
                            set.InputBoxMaxLines = Math.Max(0, tmp);
                            UltraPasteCommon.InputBoxString = text.Text;
                            UltraPasteCommon.InputBoxSubtitlesData = SubtitlesData.Parser.ParseFromStrings(UltraPasteCommon.InputBoxString);
                            UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(set.InputBoxMaxCharacters, set.InputBoxIgnoreWord, set.InputBoxMaxLines, set.InputBoxMultipleTracks);
                            currentText.Text = string.Join("\r\n", UltraPasteCommon.InputBoxSubtitlesData.Subtitles[0].TextLines);
                        }
                    };

                    maxLinesTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                    multipleTracks.CheckedChanged += (o, e) =>
                    {
                        set.InputBoxMultipleTracks = multipleTracks.Checked;
                        UltraPasteCommon.InputBoxString = text.Text;
                        UltraPasteCommon.InputBoxSubtitlesData = SubtitlesData.Parser.ParseFromStrings(UltraPasteCommon.InputBoxString);
                        UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(set.InputBoxMaxCharacters, set.InputBoxIgnoreWord, set.InputBoxMaxLines, set.InputBoxMultipleTracks);
                        currentText.Text = string.Join("\r\n", UltraPasteCommon.InputBoxSubtitlesData.Subtitles[0].TextLines);
                    };

                    button.Click += (o, e) =>
                    {
                        if (UltraPasteCommon.InputBoxSubtitlesData?.Subtitles.Count > 0)
                        {
                            UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(set.InputBoxMaxCharacters, set.InputBoxIgnoreWord, set.InputBoxMaxLines, set.InputBoxMultipleTracks);
                            List<string> strs = new List<string>();
                            foreach (SubtitlesData.Subtitle sub in UltraPasteCommon.InputBoxSubtitlesData.Subtitles)
                            {
                                strs.Add(string.Join("\r\n", sub.TextLines));
                            }
                            text.Text = string.Join("\r\n", strs);
                        }
                    };

                    buttonAdd.Click += (o, e) =>
                    {
                        using (UndoBlock undo = new UndoBlock(UltraPasteCommon.Vegas.Project, L.SubtitlesInputBox))
                        {
                            UltraPasteCommon.DoPaste(true);
                        }
                    };

                    text.TextChanged += (o, e) =>
                    {
                        UltraPasteCommon.InputBoxString = text.Text;
                        UltraPasteCommon.InputBoxSubtitlesData = SubtitlesData.Parser.ParseFromStrings(UltraPasteCommon.InputBoxString);
                        UltraPasteCommon.InputBoxSubtitlesData.SplitCharactersAndLines(set.InputBoxMaxCharacters, set.InputBoxIgnoreWord, set.InputBoxMaxLines, set.InputBoxMultipleTracks);
                        currentText.Text = string.Join("\r\n", UltraPasteCommon.InputBoxSubtitlesData.Subtitles[0].TextLines);
                    };
                }
                return p;
            }

            public static UltraTableLayoutPanel From(UltraPasteSettings.SubtitlesImportSettings set, ContainerControl formControl, bool addOneClickGroup = true)
            {
                UltraTableLayoutPanel p = FromBaseImport(set, formControl);
                p.Name = L.SubtitlesImport;

                Label label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.TextMediaGenerator,
                    AutoSize = true
                };
                p.Controls.Add(label);

                ComboBox mediaGeneratorCombo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    DataSource = new BindingSource(TextMediaGenerator.ValidTextNumbersAndNames, null),
                    ValueMember = "Key",
                    DisplayMember = "Value",
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(mediaGeneratorCombo);

                CheckBox addTextMediaGenerators = new CheckBox
                {
                    Text = L.AddTextMediaGenerators,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.AddTextMediaGenerators ?? true
                };
                p.Controls.Add(addTextMediaGenerators);
                p.SetColumnSpan(addTextMediaGenerators, 2);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.TextMediaGeneratorPresetName,
                    AutoSize = true
                };
                p.Controls.Add(label);

                ComboBox presetNameCombo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(presetNameCombo);

                CheckBox addRegions = new CheckBox
                {
                    Text = L.AddRegions,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.AddRegions ?? false
                };
                p.Controls.Add(addRegions);
                p.SetColumnSpan(addRegions, 2);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.SubtitlesMaxCharacters,
                    AutoSize = true
                };
                p.Controls.Add(label);

                TextBox maxCharactersTextBox = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = set?.MaxCharacters.ToString() ?? "0",
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(maxCharactersTextBox);

                CheckBox ignoreWord = new CheckBox
                {
                    Text = L.SubtitlesIgnoreWord,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.IgnoreWord ?? false
                };
                p.Controls.Add(ignoreWord);
                p.SetColumnSpan(ignoreWord, 2);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.SubtitlesMaxLines,
                    AutoSize = true
                };
                p.Controls.Add(label);

                TextBox maxLinesTextBox = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = set?.MaxLines.ToString() ?? "0",
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(maxLinesTextBox);

                CheckBox multipleTracks = new CheckBox
                {
                    Text = L.SubtitlesMultipleTracks,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.MultipleTracks ?? false
                };
                p.Controls.Add(multipleTracks);
                p.SetColumnSpan(multipleTracks, 2);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.SubtitlesDefaultLength,
                    AutoSize = true
                };
                p.Controls.Add(label);

                TextBox defaultLengthTextBox = new TextBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    Text = set?.DefaultLengthSeconds.ToString() ?? "5",
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(defaultLengthTextBox);

                CheckBox closeGap = new CheckBox
                {
                    Text = L.CloseGap,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.CloseGap ?? true
                };
                p.Controls.Add(closeGap);
                p.SetColumnSpan(closeGap, 2);

                if (set != null)
                {
                    addTextMediaGenerators.CheckedChanged += (o, e) =>
                    {
                        set.AddTextMediaGenerators = addTextMediaGenerators.Checked;
                    };

                    addRegions.CheckedChanged += (o, e) =>
                    {
                        set.AddRegions = addRegions.Checked;
                    };

                    if (formControl != null)
                    {
                        if (formControl is Form form)
                        {
                            form.Load += (o, e) =>
                            {
                                mediaGeneratorCombo.SelectedItem = ((mediaGeneratorCombo.DataSource as BindingSource)?.DataSource as Dictionary<int, string>)[set.MediaGeneratorType];
                                presetNameCombo.DataSource = TextMediaGenerator.TextPlugIns[set.MediaGeneratorType].GetAvailablePresets();
                                presetNameCombo.Text = set.PresetNames[set.MediaGeneratorType];
                            };
                        }
                        else if (formControl is UserControl control)
                        {
                            control.Load += (o, e) =>
                            {
                                mediaGeneratorCombo.SelectedItem = ((mediaGeneratorCombo.DataSource as BindingSource)?.DataSource as Dictionary<int, string>)[set.MediaGeneratorType];
                                presetNameCombo.Tag = "ChangedByCode";
                                presetNameCombo.DataSource = TextMediaGenerator.TextPlugIns[set.MediaGeneratorType].GetAvailablePresets();
                                presetNameCombo.Text = set.PresetNames[set.MediaGeneratorType];
                                presetNameCombo.Tag = null;
                            };
                        }
                    }

                    mediaGeneratorCombo.SelectedIndexChanged += (o, e) =>
                    {
                        set.MediaGeneratorType = mediaGeneratorCombo.SelectedIndex;

                        int? key = (mediaGeneratorCombo.SelectedItem as KeyValuePair<int, string>?)?.Key;
                        if (key != null)
                        {
                            set.MediaGeneratorType = (int)key;
                        }
                        presetNameCombo.Tag = "ChangedByCode";
                        presetNameCombo.DataSource = TextMediaGenerator.TextPlugIns[set.MediaGeneratorType].GetAvailablePresets();
                        presetNameCombo.Text = set.PresetNames[set.MediaGeneratorType];
                        presetNameCombo.Tag = null;
                    };

                    presetNameCombo.TextChanged += (o, e) =>
                    {
                        if (presetNameCombo.Tag as string != "ChangedByCode")
                        {
                            set.PresetNames[set.MediaGeneratorType] = presetNameCombo.Text;
                        }
                    };

                    maxCharactersTextBox.TextChanged += (o, e) =>
                    {
                        if (TextBox_Int_TryParse(maxCharactersTextBox, out int tmp))
                        {
                            set.MaxCharacters = Math.Max(0, tmp);
                        }
                    };

                    maxCharactersTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                    ignoreWord.CheckedChanged += (o, e) =>
                    {
                        set.IgnoreWord = ignoreWord.Checked;
                    };

                    maxLinesTextBox.TextChanged += (o, e) =>
                    {
                        if (TextBox_Int_TryParse(maxLinesTextBox, out int tmp))
                        {
                            set.MaxLines = Math.Max(0, tmp);
                        }
                    };

                    maxLinesTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                    multipleTracks.CheckedChanged += (o, e) =>
                    {
                        set.MultipleTracks = multipleTracks.Checked;
                    };

                    defaultLengthTextBox.TextChanged += (o, e) =>
                    {
                        if (TextBox_Int_TryParse(defaultLengthTextBox, out int tmp))
                        {
                            set.DefaultLengthSeconds = Math.Max(0, tmp);
                        }
                    };

                    defaultLengthTextBox.MouseWheel += TextBox_MouseWheel_Int_Max_Zero;

                    closeGap.CheckedChanged += (o, e) =>
                    {
                        set.CloseGap = closeGap.Checked;
                    };
                }

                if (addOneClickGroup)
                {
                    GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel);
                    p.Controls.Add(oneClickGroup);
                    p.SetColumnSpan(oneClickGroup, 4);

                    Button button = new Button
                    {
                        Text = L.SubtitlesApplyToSelectedEvents,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += UltraPasteCommon.SubtitlesApplyToSelectedEvents;

                    button = new Button
                    {
                        Text = L.SubtitlesTitlesAndTextToProTypeTitler.Replace("&", "&&"),
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += UltraPasteCommon.SubtitlesTitlesAndTextToProTypeTitler;
                }

                label = new Label();
                p.Controls.Add(label);
                p.SetColumnSpan(label, 4);

                return p;
            }

            public static void TextBox_MouseWheel_Int_Max_Zero(object o, MouseEventArgs e)
            {
                if (e.Delta != 0 && o is TextBox tb && TextBox_Int_TryParse(tb, out int tmp))
                {
                    tb.Text = Math.Max(0, tmp + (e.Delta > 0 ? 1 : -1)).ToString();
                }
            }

            public static bool TextBox_Int_TryParse(TextBox tb, out int result)
            {
                result = 0;
                if (double.TryParse(tb?.Text, out double tmp) || double.TryParse(tb?.Text.Replace('０', '0').Replace('１', '1').Replace('２', '2').Replace('３', '3').Replace('４', '4').Replace('５', '5').Replace('６', '6').Replace('７', '7').Replace('８', '8').Replace('９', '9'), out tmp))
                {
                    result = (int)tmp;
                    return true;
                }

                return false;
            }

            public static UltraTableLayoutPanel From(UltraPasteSettings.MediaImportSettings set, ContainerControl formControl, bool addOneClickGroup = true)
            {
                UltraTableLayoutPanel p = FromBaseImport(set, formControl);
                p.Name = L.MediaImport;

                Label label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.MediaImportAdd,
                    AutoSize = true
                };
                p.Controls.Add(label);

                ComboBox addCombo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    DataSource = L.MediaImportAddType.Clone(),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(addCombo);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.MediaImportStream,
                    AutoSize = true
                };
                p.Controls.Add(label);

                ComboBox streamCombo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    DataSource = L.MediaImportStreamType.Clone(),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(streamCombo);

                label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.MediaImportEventLength,
                    AutoSize = true
                };
                p.Controls.Add(label);

                ComboBox eventLengthCombo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    DataSource = L.MediaImportEventLengthType.Clone(),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(eventLengthCombo);

                CheckBox check = new CheckBox
                {
                    Text = L.MediaImportImageSequence,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.ImageSequence ?? true
                };
                p.Controls.Add(check);
                p.SetColumnSpan(check, 2);

                if (set != null)
                {
                    if (formControl != null)
                    {
                        if (formControl is Form form)
                        {
                            form.Load += (o, e) =>
                            {
                                addCombo.SelectedIndex = set.AddType;
                                streamCombo.SelectedIndex = set.StreamType;
                                eventLengthCombo.SelectedIndex = set.EventLengthType;
                            };
                        }
                        else if (formControl is UserControl control)
                        {
                            control.Load += (o, e) =>
                            {
                                addCombo.SelectedIndex = set.AddType;
                                streamCombo.SelectedIndex = set.StreamType;
                                eventLengthCombo.SelectedIndex = set.EventLengthType;
                            };
                        }
                    }

                    addCombo.SelectedIndexChanged += (o, e) =>
                    {
                        set.AddType = addCombo.SelectedIndex;
                    };

                    streamCombo.SelectedIndexChanged += (o, e) =>
                    {
                        set.StreamType = streamCombo.SelectedIndex;
                    };

                    eventLengthCombo.SelectedIndexChanged += (o, e) =>
                    {
                        set.EventLengthType = eventLengthCombo.SelectedIndex;
                    };

                    check.CheckedChanged += (o, e) =>
                    {
                        set.ImageSequence = check.Checked;
                    };
                }

                if (addOneClickGroup)
                {
                    GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel);
                    p.Controls.Add(oneClickGroup);
                    p.SetColumnSpan(oneClickGroup, 4);

                    Button button = new Button
                    {
                        Text = L.AddMissingStreams,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);

                    button.Click += UltraPasteCommon.MediaAddMissingStreams;

                    {
                        button = new Button
                        {
                            Text = L.MediaImportCustom,
                            Margin = new Padding(3, 0, 3, 9),
                            TextAlign = ContentAlignment.MiddleCenter,
                            AutoSize = true,
                            FlatStyle = FlatStyle.Flat,
                            Anchor = AnchorStyles.None
                        };
                        button.FlatAppearance.BorderSize = 1;
                        button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                        oneClickPanel.Controls.Add(button);

                        button.Click += (o, e) =>
                        {
                            Form form = new Form
                            {
                                ShowInTaskbar = false,
                                AutoSize = true,
                                BackColor = Common.UIColors[0],
                                ForeColor = Common.UIColors[1],
                                Font = new Font(L.Font, 9),
                                Text = L.MediaImportCustom,
                                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                                StartPosition = FormStartPosition.CenterScreen,
                                AutoSizeMode = AutoSizeMode.GrowAndShrink
                            };

                            UltraTableLayoutPanel pc = new UltraTableLayoutPanel();
                            form.Controls.Add(pc);

                            label = new Label
                            {
                                Margin = new Padding(6, 9, 0, 6),
                                Text = L.MediaImportCustomIncludedFiles,
                                AutoSize = true
                            };
                            pc.Controls.Add(label);

                            ComboBox combo = new ComboBox
                            {
                                AutoSize = true,
                                Margin = new Padding(9, 6, 11, 6),
                                DataSource = UltraPasteCommon.Settings.Customs,
                                DisplayMember = "IncludedFiles",
                                DropDownStyle = ComboBoxStyle.DropDown,
                                Dock = DockStyle.Fill
                            };
                            pc.Controls.Add(combo);
                            pc.SetColumnSpan(combo, 2);

                            TableLayoutPanel pb = new TableLayoutPanel()
                            {
                                Dock = DockStyle.Fill,
                                AutoSize = true,
                                Anchor = (AnchorStyles.Top | AnchorStyles.Left),
                                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                                ColumnCount = 2
                            };
                            pb.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                            pb.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                            pc.Controls.Add(pb);

                            Button buttonSave = new Button
                            {
                                Text = "√",
                                Margin = new Padding(3, 0, 3, 9),
                                TextAlign = ContentAlignment.MiddleCenter,
                                AutoSize = true,
                                FlatStyle = FlatStyle.Flat,
                                Anchor = AnchorStyles.None
                            };
                            buttonSave.FlatAppearance.BorderSize = 0;
                            pb.Controls.Add(buttonSave);

                            Button buttonRemove = new Button
                            {
                                Text = "×",
                                Margin = new Padding(3, 0, 3, 9),
                                TextAlign = ContentAlignment.MiddleCenter,
                                AutoSize = true,
                                FlatStyle = FlatStyle.Flat,
                                Anchor = AnchorStyles.None
                            };
                            buttonRemove.FlatAppearance.BorderSize = 0;
                            pb.Controls.Add(buttonRemove);

                            label = new Label
                            {
                                Margin = new Padding(6, 9, 0, 6),
                                Text = L.StartPosition,
                                AutoSize = true
                            };
                            pc.Controls.Add(label);

                            ComboBox startPositionCustom = new ComboBox
                            {
                                AutoSize = true,
                                Margin = new Padding(9, 6, 11, 6),
                                DataSource = L.StartPositionType.Clone(),
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                Dock = DockStyle.Fill
                            };
                            pc.Controls.Add(startPositionCustom);

                            CheckBox cursorToEndCustom = new CheckBox
                            {
                                Text = L.CursorToEnd,
                                Margin = new Padding(6, 8, 6, 6),
                                AutoSize = true,
                                Checked = set?.CursorToEnd ?? true
                            };
                            pc.Controls.Add(cursorToEndCustom);
                            pc.SetColumnSpan(cursorToEndCustom, 2);

                            label = new Label
                            {
                                Margin = new Padding(6, 9, 0, 6),
                                Text = L.MediaImportAdd,
                                AutoSize = true
                            };
                            pc.Controls.Add(label);

                            ComboBox addComboCustom = new ComboBox
                            {
                                AutoSize = true,
                                Margin = new Padding(9, 6, 11, 6),
                                DataSource = L.MediaImportAddType.Clone(),
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                Dock = DockStyle.Fill
                            };
                            pc.Controls.Add(addComboCustom);

                            label = new Label
                            {
                                Margin = new Padding(6, 9, 0, 6),
                                Text = L.MediaImportStream,
                                AutoSize = true
                            };
                            pc.Controls.Add(label);

                            ComboBox streamComboCustom = new ComboBox
                            {
                                AutoSize = true,
                                Margin = new Padding(9, 6, 11, 6),
                                DataSource = L.MediaImportStreamType.Clone(),
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                Dock = DockStyle.Fill
                            };
                            pc.Controls.Add(streamComboCustom);

                            label = new Label
                            {
                                Margin = new Padding(6, 9, 0, 6),
                                Text = L.MediaImportEventLength,
                                AutoSize = true
                            };
                            pc.Controls.Add(label);

                            ComboBox eventLengthComboCustom = new ComboBox
                            {
                                AutoSize = true,
                                Margin = new Padding(9, 6, 11, 6),
                                DataSource = L.MediaImportEventLengthType.Clone(),
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                Dock = DockStyle.Fill
                            };
                            pc.Controls.Add(eventLengthComboCustom);

                            CheckBox imageSequenceCustom = new CheckBox
                            {
                                Text = L.MediaImportImageSequence,
                                Margin = new Padding(6, 8, 6, 6),
                                AutoSize = true,
                                Checked = set?.ImageSequence ?? true
                            };
                            pc.Controls.Add(imageSequenceCustom);
                            pc.SetColumnSpan(imageSequenceCustom, 2);

                            buttonSave.Click += (oo, ee) =>
                            {
                                string str = combo.Text;
                                if (string.IsNullOrEmpty(str))
                                {
                                    return;
                                }
                                UltraPasteSettings.CustomMediaImportSettings cmis = null;
                                foreach (UltraPasteSettings.CustomMediaImportSettings c in UltraPasteCommon.Settings.Customs)
                                {
                                    if (c.IncludedFiles == str)
                                    {
                                        cmis = c;
                                        break;
                                    }
                                }
                                if (cmis == null)
                                {
                                    cmis = new UltraPasteSettings.CustomMediaImportSettings() { IncludedFiles = str };
                                    UltraPasteCommon.Settings.Customs.Add(cmis);
                                }
                                cmis.StartPositionType = startPositionCustom.SelectedIndex;
                                cmis.CursorToEnd = cursorToEndCustom.Checked;
                                cmis.AddType = addComboCustom.SelectedIndex;
                                cmis.StreamType = streamComboCustom.SelectedIndex;
                                cmis.EventLengthType = eventLengthComboCustom.SelectedIndex;
                                cmis.ImageSequence = imageSequenceCustom.Checked;
                                combo.DataSource = null;
                                combo.DataSource = UltraPasteCommon.Settings.Customs;
                                combo.DisplayMember = "IncludedFiles";
                                combo.SelectedItem = cmis;
                            };

                            buttonRemove.Click += (oo, ee) =>
                            {
                                UltraPasteSettings.CustomMediaImportSettings cmis = null;
                                foreach (UltraPasteSettings.CustomMediaImportSettings c in UltraPasteCommon.Settings.Customs)
                                {
                                    if (c.IncludedFiles == combo.Text)
                                    {
                                        cmis = c;
                                        break;
                                    }
                                }
                                if (cmis != null)
                                {
                                    UltraPasteCommon.Settings.Customs.Remove(cmis);
                                    combo.DataSource = null;
                                    combo.DataSource = UltraPasteCommon.Settings.Customs;
                                    combo.DisplayMember = "IncludedFiles";
                                }

                            };

                            combo.SelectedValueChanged += (oo, ee) =>
                            {
                                UltraPasteSettings.CustomMediaImportSettings cmis = combo.SelectedItem as UltraPasteSettings.CustomMediaImportSettings;
                                if (cmis != null)
                                {
                                    startPositionCustom.SelectedIndex = cmis.StartPositionType;
                                    cursorToEndCustom.Checked = cmis.CursorToEnd;
                                    addComboCustom.SelectedIndex = cmis.AddType;
                                    streamComboCustom.SelectedIndex = cmis.StreamType;
                                    eventLengthComboCustom.SelectedIndex = cmis.EventLengthType;
                                    imageSequenceCustom.Checked = cmis.ImageSequence;
                                }
                            };

                            form.ShowDialog();
                        };
                    }
                }

                label = new Label();
                p.Controls.Add(label);
                p.SetColumnSpan(label, 4);

                return p;
            }

            public static UltraTableLayoutPanel FromBaseImport(UltraPasteSettings.BaseImportSettings set, ContainerControl formControl)
            {
                UltraTableLayoutPanel p = new UltraTableLayoutPanel();

                Label label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.StartPosition,
                    AutoSize = true
                };
                p.Controls.Add(label);

                ComboBox combo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    DataSource = L.StartPositionType.Clone(),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(combo);

                CheckBox check = new CheckBox
                {
                    Text = L.CursorToEnd,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.CursorToEnd ?? true
                };
                p.Controls.Add(check);
                p.SetColumnSpan(check, 2);

                if (set != null)
                {
                    if (formControl != null)
                    {
                        if (formControl is Form form)
                        {
                            form.Load += (o, e) =>
                            {
                                combo.SelectedIndex = set.StartPositionType;
                            };
                        }
                        else if (formControl is UserControl control)
                        {
                            control.Load += (o, e) =>
                            {
                                combo.SelectedIndex = set.StartPositionType;
                            };
                        }
                    }

                    combo.SelectedIndexChanged += (o, e) =>
                    {
                        set.StartPositionType = combo.SelectedIndex;
                    };

                    check.CheckedChanged += (o, e) =>
                    {
                        set.CursorToEnd = check.Checked;
                    };
                }

                return p;
            }

            public static UltraTableLayoutPanel From(UltraPasteSettings.VegasDataSettings set, ContainerControl formControl, bool addOneClickGroup = true)
            {
                UltraTableLayoutPanel p = new UltraTableLayoutPanel() { Name = L.VegasData };

                Label label = new Label
                {
                    Margin = new Padding(6, 9, 0, 6),
                    Text = L.VegImport,
                    AutoSize = true
                };
                p.Controls.Add(label);
                p.SetColumnSpan(label, 2);

                ComboBox combo = new ComboBox
                {
                    AutoSize = true,
                    Margin = new Padding(9, 6, 11, 6),
                    DataSource = L.VegImportType.Clone(),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Dock = DockStyle.Fill
                };
                p.Controls.Add(combo);
                p.SetColumnSpan(combo, 2);

                CheckBox selectivelyPasteEventAttributes = new CheckBox
                {
                    Text = L.SelectivelyPasteEventAttributes,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.SelectivelyPasteEventAttributes ?? true
                };
                p.Controls.Add(selectivelyPasteEventAttributes);
                p.SetColumnSpan(selectivelyPasteEventAttributes, 2);

                CheckBox runScript = new CheckBox
                {
                    Text = L.RunScript,
                    Margin = new Padding(6, 8, 6, 6),
                    AutoSize = true,
                    Checked = set?.RunScript ?? true
                };
                p.Controls.Add(runScript);
                p.SetColumnSpan(runScript, 2);

                if (set != null)
                {
                    if (formControl != null)
                    {
                        if (formControl is Form form)
                        {
                            form.Load += (o, e) =>
                            {
                                combo.SelectedIndex = set.VegImportType;
                            };
                        }
                        else if (formControl is UserControl control)
                        {
                            control.Load += (o, e) =>
                            {
                                combo.SelectedIndex = set.VegImportType;
                            };
                        }
                    }

                    combo.SelectedIndexChanged += (o, e) =>
                    {
                        set.VegImportType = combo.SelectedIndex;
                    };

                    selectivelyPasteEventAttributes.CheckedChanged += (o, e) =>
                    {
                        set.SelectivelyPasteEventAttributes = selectivelyPasteEventAttributes.Checked;
                    };

                    runScript.CheckedChanged += (o, e) =>
                    {
                        set.RunScript = runScript.Checked;
                    };
                }

                if (addOneClickGroup)
                {
                    GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel oneClickPanel);
                    p.Controls.Add(oneClickGroup);
                    p.SetColumnSpan(oneClickGroup, 4);

                    Button button = new Button
                    {
                        Text = L.GenerateMixedVegasClipboardData,
                        Margin = new Padding(3, 0, 3, 9),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        Anchor = AnchorStyles.None
                    };
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                    oneClickPanel.Controls.Add(button);
                    oneClickPanel.SetColumnSpan(button, 2);

                    button.Click += UltraPasteCommon.GenerateMixedVegasClipboardData;
                }

                label = new Label();
                p.Controls.Add(label);
                p.SetColumnSpan(label, 4);

                return p;
            }
        }
    }
}
