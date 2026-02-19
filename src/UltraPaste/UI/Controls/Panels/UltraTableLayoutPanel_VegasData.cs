#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste.UI.Controls.Panels
{
    using UltraPaste.Core;
    using UltraPaste.Models;
    using UltraPaste.Utilities;
    using UltraPaste.Localization;

    internal partial class UltraTableLayoutPanel_VegasData : UltraTableLayoutPanel
    {
        private Label _vegImportLabel;
        private CheckBox _selectivelyPasteCheckBox;
        private CheckBox _runScriptCheckBox;
        private Button _generateMixedButton;
        private ComboBox _importCombo;

        public UltraTableLayoutPanel_VegasData(UltraPasteSettings.VegasDataSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base()
        {
            Name = I18n.Translation.VegasData;

            _vegImportLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.VegImport,
                AutoSize = true
            };
            Controls.Add(_vegImportLabel);
            SetColumnSpan(_vegImportLabel, 2);

            _importCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.VegImportType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(_importCombo);
            SetColumnSpan(_importCombo, 2);

            _selectivelyPasteCheckBox = new CheckBox
            {
                Text = I18n.Translation.SelectivelyPasteEventAttributes,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.SelectivelyPasteEventAttributes ?? true
            };
            Controls.Add(_selectivelyPasteCheckBox);
            SetColumnSpan(_selectivelyPasteCheckBox, 2);

            _runScriptCheckBox = new CheckBox
            {
                Text = I18n.Translation.RunScript,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.RunScript ?? true
            };
            Controls.Add(_runScriptCheckBox);
            SetColumnSpan(_runScriptCheckBox, 2);

            if (settings != null)
            {
                if (formControl is Form form)
                {
                    form.Load += (o, e) => { _importCombo.SelectedIndex = settings.VegImportType; };
                }
                else if (formControl is UserControl uc)
                {
                    uc.Load += (o, e) => { _importCombo.SelectedIndex = settings.VegImportType; };
                }

                _importCombo.SelectedIndexChanged += (o, e) => { settings.VegImportType = _importCombo.SelectedIndex; };
                _selectivelyPasteCheckBox.CheckedChanged += (o, e) => { settings.SelectivelyPasteEventAttributes = _selectivelyPasteCheckBox.Checked; };
                _runScriptCheckBox.CheckedChanged += (o, e) => { settings.RunScript = _runScriptCheckBox.Checked; };
            }

            ComboBox fxCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(fxCombo);
            SetColumnSpan(fxCombo, 3);

            TableLayoutPanel fxButtonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                ColumnCount = 2
            };
            for (int i = 0; i < fxButtonPanel.ColumnCount; i++)
            {
                fxButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / fxButtonPanel.ColumnCount));
            }
            Controls.Add(fxButtonPanel);

            Button refreshButton = new Button
            {
                Text = "↻",
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            fxButtonPanel.Controls.Add(refreshButton);
            refreshButton.Click += (o, e) => RefreshFxCombo(fxCombo);


            Button okButton = new Button
            {
                Text = "√",
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += (o, e) =>
            {
                string sel = fxCombo.SelectedItem as string;
                if (!string.IsNullOrEmpty(sel))
                {
                    try
                    {
                        Timecode length = null;
                        if (UltraPasteCommon.Vegas.Transport.SelectionLength.Nanos != 0)
                        {
                            length = UltraPasteCommon.Vegas.Transport.SelectionLength;
                            Timecode start = UltraPasteCommon.Vegas.Transport.SelectionStart;
                            if (UltraPasteCommon.Vegas.Transport.SelectionLength.Nanos < 0)
                            {
                                start += UltraPasteCommon.Vegas.Transport.SelectionLength;
                                UltraPasteCommon.Vegas.RefreshCursorPosition(start);
                            }
                        }
                        VegasClipDataHelper.ApplyFxPackageToClipboard(sel, length);
                        if (length == null && UltraPasteCommon.Vegas.Project.HasSelectedEventsInRange<VideoEvent>(1))
                        {
                            VegasClipDataHelper.DoPasteEventAttributes(false, false, true);
                        }
                        else
                        {
                            VegasClipDataHelper.DoNormalPaste();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(I18n.Translation.VegasData + ": " + ex.Message);
                    }
                }
            };
            fxButtonPanel.Controls.Add(okButton);

            // Auto-refresh when window loads
            if (formControl is Form f)
            {
                f.Load += (o, e) => refreshButton.PerformClick();
            }
            else if (formControl is UserControl uc2)
            {
                uc2.Load += (o, e) => refreshButton.PerformClick();
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                _generateMixedButton = new Button
                {
                    Text = I18n.Translation.GenerateMixedVegasClipboardData,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _generateMixedButton.FlatAppearance.BorderSize = 1;
                _generateMixedButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_generateMixedButton);
                buttonsPanel.SetColumnSpan(_generateMixedButton, 2);

                _generateMixedButton.Click += UltraPasteCommon.GenerateMixedVegasClipboardData;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);

            I18n.LanguageChanged += (o, e) => RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            SuspendLayout();
            try
            {
                Name = I18n.Translation.VegasData;
                _vegImportLabel.Text = I18n.Translation.VegImport;
                _selectivelyPasteCheckBox.Text = I18n.Translation.SelectivelyPasteEventAttributes;
                _runScriptCheckBox.Text = I18n.Translation.RunScript;
                if (_generateMixedButton != null)
                {
                    _generateMixedButton.Text = I18n.Translation.GenerateMixedVegasClipboardData;
                }

                int savedIndex = _importCombo.SelectedIndex;
                _importCombo.DataSource = null;
                _importCombo.DataSource = I18n.Translation.VegImportType.Clone();
                if (savedIndex >= 0 && savedIndex < _importCombo.Items.Count)
                {
                    _importCombo.SelectedIndex = savedIndex;
                }
            }
            finally
            {
                ResumeLayout(true);
                PerformLayout();
                RefreshLayoutAfterLocalization();
            }
        }

        private void RefreshFxCombo(ComboBox fxCombo)
        {
            fxCombo.BeginUpdate();
            fxCombo.Items.Clear();
            List<string> fxNames = new List<string>();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\DirectShow\\Presets\\FX Packages"))
                {
                    if (key != null)
                    {
                        foreach (string sub in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey subKey = key.OpenSubKey(sub))
                                {
                                    if (subKey == null || subKey.GetValue("Stream") == null)
                                    {
                                        continue;
                                    }

                                    object typeObj = subKey.GetValue("Type");
                                    int typeVal = -1;
                                    if (typeObj is int)
                                    {
                                        typeVal = (int)typeObj;
                                    }
                                    else if (typeObj is long)
                                    {
                                        typeVal = Convert.ToInt32(typeObj);
                                    }

                                    if (typeVal != 5) continue;

                                    object nameObj = subKey.GetValue("Name");
                                    if (nameObj is string nameStr && !string.IsNullOrEmpty(nameStr))
                                    {
                                        fxNames.Add(nameStr);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            fxNames.Sort(StringComparer.CurrentCultureIgnoreCase);
            foreach (string name in fxNames)
            {
                fxCombo.Items.Add(name);
            }

            if (fxCombo.Items.Count > 0) fxCombo.SelectedIndex = 0;
            fxCombo.EndUpdate();
        }
    }
}
