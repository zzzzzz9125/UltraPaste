using System.Drawing;
using System.Windows.Forms;
using UltraPaste.Core;
using UltraPaste.Localization;
using UltraPaste.Models;

namespace UltraPaste.UI.Controls.Panels
{
    internal partial class UltraTableLayoutPanel_ClipboardImage : UltraTableLayoutPanel
    {
        private Label _filePathLabel;
        private Button _snapshotButton;
        private Button _snapshotFileButton;

        public UltraTableLayoutPanel_ClipboardImage(UltraPasteSettings.ClipboardImageSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.ClipboardImage;

            _filePathLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.ClipboardImageFilePath,
                AutoSize = true
            };
            Controls.Add(_filePathLabel);

            TextBox filePathBox = new TextBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                Text = settings?.FilePath ?? string.Empty,
                Dock = DockStyle.Fill
            };
            Controls.Add(filePathBox);
            SetColumnSpan(filePathBox, 3);

            if (settings != null)
            {
                filePathBox.TextChanged += (o, e) => { settings.FilePath = filePathBox.Text; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                _snapshotButton = new Button
                {
                    Text = I18n.Translation.SaveSnapshotToClipboard,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _snapshotButton.FlatAppearance.BorderSize = 1;
                _snapshotButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_snapshotButton);

                _snapshotButton.Click += (o, e) => { UltraPasteCommon.Vegas.SaveSnapshot(); };

                _snapshotFileButton = new Button
                {
                    Text = I18n.Translation.SaveSnapshotToClipboardAndFile,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _snapshotFileButton.FlatAppearance.BorderSize = 1;
                _snapshotFileButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_snapshotFileButton);

                _snapshotFileButton.Click += UltraPasteCommon.SaveSnapshotToClipboardAndFile;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);

            I18n.LanguageChanged += (o, e) => RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            Name = I18n.Translation.ClipboardImage;
            _filePathLabel.Text = I18n.Translation.ClipboardImageFilePath;
            if (_snapshotButton != null)
            {
                _snapshotButton.Text = I18n.Translation.SaveSnapshotToClipboard;
            }
            if (_snapshotFileButton != null)
            {
                _snapshotFileButton.Text = I18n.Translation.SaveSnapshotToClipboardAndFile;
            }
        }
    }
}
