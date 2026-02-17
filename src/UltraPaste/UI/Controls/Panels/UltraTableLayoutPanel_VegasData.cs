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
    using UltraPaste.Localization;
    using UltraPaste.Models;
    using UltraPaste.Utilities;

    internal partial class UltraTableLayoutPanel_VegasData : UltraPaste.UI.Controls.UltraTableLayoutPanel
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
            fxButtonPanel.Controls.Add(okButton);

            // Refresh logic: enumerate registry keys under FX_PACKAGES_REGISTRY_PATH
            refreshButton.Click += (o, e) =>
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
            };

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
                        VegasClipboardDataHelper.ApplyFxPackageToClipboard(sel, length);
                        if (length == null && UltraPasteCommon.Vegas.Project.HasSelectedEventsInRange<VideoEvent>(1))
                        {
                            VegasClipboardDataHelper.DoPasteEventAttributes(false, false, true);
                        }
                        else
                        {
                            VegasClipboardDataHelper.DoNormalPaste();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(I18n.Translation.VegasData + ": " + ex.Message);
                    }
                }
            };

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
