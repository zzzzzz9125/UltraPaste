using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    internal partial class UltraTableLayoutPanel_ClipboardImage : UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_ClipboardImage(UltraPasteSettings.ClipboardImageSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.ClipboardImage;

            Label label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.ClipboardImageFilePath,
                AutoSize = true
            };
            Controls.Add(label);

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

                Button button = new Button
                {
                    Text = I18n.Translation.SaveSnapshotToClipboard,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += (o, e) => { UltraPasteCommon.Vegas.SaveSnapshot(); };

                button = new Button
                {
                    Text = I18n.Translation.SaveSnapshotToClipboardAndFile,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += UltraPasteCommon.SaveSnapshotToClipboardAndFile;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
        }
    }
}
