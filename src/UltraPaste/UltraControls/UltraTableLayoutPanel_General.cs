#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste.UltraControls
{
    internal partial class UltraTableLayoutPanel_General : UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_General(UltraPasteSettings.GeneralSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base()
        {
            Name = I18n.Translation.General;

            Label label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.Language,
                AutoSize = true
            };
            Controls.Add(label);
            SetColumnSpan(label, 2);

            ComboBox combo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = new BindingSource(I18n.LanguageDictionary, null),
                ValueMember = "Key",
                DisplayMember = "Value",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(combo);
            SetColumnSpan(combo, 2);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.ExcludedFiles,
                AutoSize = true
            };
            Controls.Add(label);
            SetColumnSpan(label, 2);

            TextBox excludedFiles = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.ExcludedFiles ?? string.Empty,
                Dock = DockStyle.Fill
            };
            Controls.Add(excludedFiles);
            SetColumnSpan(excludedFiles, 2);

            if (settings != null)
            {
                if (formControl is Form form)
                {
                    form.Load += (o, e) =>
                    {
                        if (I18n.LanguageDictionary.ContainsKey(I18n.Settings.Current))
                        {
                            combo.SelectedValue = I18n.Settings.Current;
                        }
                    };
                }
                else if (formControl is UserControl control)
                {
                    control.Load += (o, e) =>
                    {
                        if (I18n.LanguageDictionary.ContainsKey(I18n.Settings.Current))
                        {
                            combo.SelectedValue = I18n.Settings.Current;
                        }
                    };
                }

                combo.SelectedIndexChanged += (o, e) =>
                {
                    string key = (combo.SelectedItem as KeyValuePair<string, string>?)?.Key;
                    if (!I18n.LanguageDictionary.ContainsKey(key))
                    {
                        return;
                    }

                    if (I18n.Settings.Current == key)
                    {
                        return;
                    }

                    I18n.Settings.Current = key;
                    I18n.SaveSettingsToXml();
                    I18n.Localize();

                    if (MessageBox.Show(I18n.Translation.LanguageChange, I18n.Translation.UltraPaste, MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        if (UltraPasteCommon.Vegas.FindDockView("UltraWindow_Main", out IDockView dock) && dock is DockableControl dockableControl)
                        {
                            dockableControl.Close();
                        }

                        if (UltraPasteCommon.Vegas.FindDockView("UltraWindow_SubtitlesInput", out dock) && dock is DockableControl subtitlesControl)
                        {
                            subtitlesControl.Close();
                        }
                    }
                };

                excludedFiles.TextChanged += (o, e) => { settings.ExcludedFiles = excludedFiles.Text; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                Button button = new Button
                {
                    Text = I18n.Translation.SupportMe,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += (o, e) =>
                {
                    string link = I18n.Translation.Language == "zh" ? "https://afdian.com/a/zzzzzz9125" : "https://ko-fi.com/zzzzzz9125";
                    Process.Start(link);
                };

                button = new Button
                {
                    Text = I18n.Translation.CheckForUpdate,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += (o, e) => { Process.Start("https://github.com/zzzzzz9125/UltraPaste"); };
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
        }
    }
}
