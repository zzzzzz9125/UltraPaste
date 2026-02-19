using System.Drawing;
using System.Windows.Forms;
using UltraPaste.Core;
using UltraPaste.Localization;
using UltraPaste.Models;

namespace UltraPaste.UI.Controls.Panels
{
    internal partial class UltraTableLayoutPanel_ReaperData : UltraTableLayoutPanel
    {
        private CheckBox _closeGapCheckBox;
        private CheckBox _addVideoStreamsCheckBox;
        private Button _exportEventsButton;
        private Button _exportTracksButton;

        public UltraTableLayoutPanel_ReaperData(UltraPasteSettings.ReaperDataSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.ReaperData;

            _closeGapCheckBox = new CheckBox
            {
                Text = I18n.Translation.CloseGap,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.CloseGap ?? true
            };
            Controls.Add(_closeGapCheckBox);
            SetColumnSpan(_closeGapCheckBox, 2);

            _addVideoStreamsCheckBox = new CheckBox
            {
                Text = I18n.Translation.AddVideoStreams,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.AddVideoStreams ?? true
            };
            Controls.Add(_addVideoStreamsCheckBox);
            SetColumnSpan(_addVideoStreamsCheckBox, 2);

            if (settings != null)
            {
                _closeGapCheckBox.CheckedChanged += (o, e) => { settings.CloseGap = _closeGapCheckBox.Checked; };
                _addVideoStreamsCheckBox.CheckedChanged += (o, e) => { settings.AddVideoStreams = _addVideoStreamsCheckBox.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                _exportEventsButton = new Button
                {
                    Text = I18n.Translation.ExportSelectedEventsToReaperData,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _exportEventsButton.FlatAppearance.BorderSize = 1;
                _exportEventsButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_exportEventsButton);

                _exportEventsButton.Click += UltraPasteCommon.ExportSelectedEventsToReaperData;

                _exportTracksButton = new Button
                {
                    Text = I18n.Translation.ExportSelectedTracksToReaperData,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _exportTracksButton.FlatAppearance.BorderSize = 1;
                _exportTracksButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_exportTracksButton);

                _exportTracksButton.Click += UltraPasteCommon.ExportSelectedTracksToReaperData;
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);

            I18n.LanguageChanged += (o, e) => RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            Name = I18n.Translation.ReaperData;
            _closeGapCheckBox.Text = I18n.Translation.CloseGap;
            _addVideoStreamsCheckBox.Text = I18n.Translation.AddVideoStreams;
            if (_exportEventsButton != null)
            {
                _exportEventsButton.Text = I18n.Translation.ExportSelectedEventsToReaperData;
            }
            if (_exportTracksButton != null)
            {
                _exportTracksButton.Text = I18n.Translation.ExportSelectedTracksToReaperData;
            }
        }
    }
}
