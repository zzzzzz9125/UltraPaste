using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    internal partial class UltraTableLayoutPanel_ReaperData : UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_ReaperData(UltraPasteSettings.ReaperDataSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.ReaperData;

            CheckBox closeGap = new CheckBox
            {
                Text = I18n.Translation.CloseGap,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.CloseGap ?? true
            };
            Controls.Add(closeGap);
            SetColumnSpan(closeGap, 2);

            CheckBox addVideoStreams = new CheckBox
            {
                Text = I18n.Translation.AddVideoStreams,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.AddVideoStreams ?? true
            };
            Controls.Add(addVideoStreams);
            SetColumnSpan(addVideoStreams, 2);

            if (settings != null)
            {
                closeGap.CheckedChanged += (o, e) => { settings.CloseGap = closeGap.Checked; };
                addVideoStreams.CheckedChanged += (o, e) => { settings.AddVideoStreams = addVideoStreams.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                Button button = new Button
                {
                    Text = I18n.Translation.ExportSelectedEventsToReaperData,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += UltraPasteCommon.ExportSelectedEventsToReaperData;

                button = new Button
                {
                    Text = I18n.Translation.ExportSelectedTracksToReaperData,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += UltraPasteCommon.ExportSelectedTracksToReaperData;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
        }
    }
}
