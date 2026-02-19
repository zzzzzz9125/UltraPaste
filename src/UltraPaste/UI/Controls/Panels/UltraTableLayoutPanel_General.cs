#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste.UI.Controls.Panels
{
    using UltraPaste.Models;
    using UltraPaste.Localization;

    internal partial class UltraTableLayoutPanel_General : UltraTableLayoutPanel
    {
        private Label _languageLabel;
        private Label _excludedFilesLabel;
        private Button _supportButton;
        private Button _updateButton;

        public UltraTableLayoutPanel_General(UltraPasteSettings.GeneralSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base()
        {
            Name = I18n.Translation.General;

            _languageLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.Language,
                AutoSize = true
            };
            Controls.Add(_languageLabel);
            SetColumnSpan(_languageLabel, 2);

            ComboBox combo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = new BindingSource(I18n.LanguageDictionary, null),
                ValueMember = "Key",
                DisplayMember = "Value",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Tag = "LanguageCombo"
            };
            Controls.Add(combo);
            SetColumnSpan(combo, 2);

            _excludedFilesLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.ExcludedFiles,
                AutoSize = true
            };
            Controls.Add(_excludedFilesLabel);
            SetColumnSpan(_excludedFilesLabel, 2);

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
                };

                excludedFiles.TextChanged += (o, e) => { settings.ExcludedFiles = excludedFiles.Text; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                _supportButton = new Button
                {
                    Text = I18n.Translation.SupportMe,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None,
                    Tag = "SupportButton"
                };
                _supportButton.FlatAppearance.BorderSize = 1;
                _supportButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_supportButton);

                _supportButton.Click += (o, e) =>
                {
                    string link = I18n.Translation.Language == "zh" ? "https://afdian.com/a/zzzzzz9125" : "https://ko-fi.com/zzzzzz9125";
                    Process.Start(link);
                };

                _updateButton = new Button
                {
                    Text = I18n.Translation.CheckForUpdate,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None,
                    Tag = "UpdateButton"
                };
                _updateButton.FlatAppearance.BorderSize = 1;
                _updateButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_updateButton);

                _updateButton.Click += (o, e) => { Process.Start("https://github.com/zzzzzz9125/UltraPaste"); };
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
                Name = I18n.Translation.General;
                _languageLabel.Text = I18n.Translation.Language;
                _excludedFilesLabel.Text = I18n.Translation.ExcludedFiles;
                if (_supportButton != null)
                {
                    _supportButton.Text = I18n.Translation.SupportMe;
                }
                if (_updateButton != null)
                {
                    _updateButton.Text = I18n.Translation.CheckForUpdate;
                }
            }
            finally
            {
                ResumeLayout(true);
                PerformLayout();
            }
        }
    }
}
