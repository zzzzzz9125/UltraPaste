using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UI.Controls.Panels
{
    using UltraPaste.Core;
    using UltraPaste.Models;
    using UltraPaste.Utilities;
    using UltraPaste.Localization;

    internal partial class UltraTableLayoutPanel_MediaImport : UltraTableLayoutPanel
    {
        private Label _addLabel;
        private Label _streamLabel;
        private Label _eventLengthLabel;
        private CheckBox _imageSequenceCheckBox;
        private Button _addMissingStreamsButton;
        private Button _customButton;
        private ComboBox _addCombo;
        private ComboBox _streamCombo;
        private ComboBox _eventLengthCombo;

        public UltraTableLayoutPanel_MediaImport(UltraPasteSettings.MediaImportSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.MediaImport;

            _addLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportAdd,
                AutoSize = true
            };
            Controls.Add(_addLabel);

            _addCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportAddType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(_addCombo);

            _streamLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportStream,
                AutoSize = true
            };
            Controls.Add(_streamLabel);

            _streamCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportStreamType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(_streamCombo);

            _eventLengthLabel = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportEventLength,
                AutoSize = true
            };
            Controls.Add(_eventLengthLabel);

            _eventLengthCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportEventLengthType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(_eventLengthCombo);

            _imageSequenceCheckBox = new CheckBox
            {
                Text = I18n.Translation.MediaImportImageSequence,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.ImageSequence ?? true
            };
            Controls.Add(_imageSequenceCheckBox);
            SetColumnSpan(_imageSequenceCheckBox, 2);

            if (settings != null)
            {
                if (formControl is Form form)
                {
                    form.Load += (o, e) =>
                    {
                        _addCombo.SelectedIndex = settings.AddType;
                        _streamCombo.SelectedIndex = settings.StreamType;
                        _eventLengthCombo.SelectedIndex = settings.EventLengthType;
                    };
                }
                else if (formControl is UserControl uc)
                {
                    uc.Load += (o, e) =>
                    {
                        _addCombo.SelectedIndex = settings.AddType;
                        _streamCombo.SelectedIndex = settings.StreamType;
                        _eventLengthCombo.SelectedIndex = settings.EventLengthType;
                    };
                }

                _addCombo.SelectedIndexChanged += (o, e) => { settings.AddType = _addCombo.SelectedIndex; };
                _streamCombo.SelectedIndexChanged += (o, e) => { settings.StreamType = _streamCombo.SelectedIndex; };
                _eventLengthCombo.SelectedIndexChanged += (o, e) => { settings.EventLengthType = _eventLengthCombo.SelectedIndex; };
                _imageSequenceCheckBox.CheckedChanged += (o, e) => { settings.ImageSequence = _imageSequenceCheckBox.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                _addMissingStreamsButton = new Button
                {
                    Text = I18n.Translation.AddMissingStreams,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _addMissingStreamsButton.FlatAppearance.BorderSize = 1;
                _addMissingStreamsButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_addMissingStreamsButton);

                _addMissingStreamsButton.Click += UltraPasteCommon.MediaAddMissingStreams;

                _customButton = new Button
                {
                    Text = I18n.Translation.MediaImportCustom,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                _customButton.FlatAppearance.BorderSize = 1;
                _customButton.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(_customButton);

                _customButton.Click += (o, e) => ShowCustomMediaImportDialog(settings);
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
                Name = I18n.Translation.MediaImport;
                _addLabel.Text = I18n.Translation.MediaImportAdd;
                _streamLabel.Text = I18n.Translation.MediaImportStream;
                _eventLengthLabel.Text = I18n.Translation.MediaImportEventLength;
                _imageSequenceCheckBox.Text = I18n.Translation.MediaImportImageSequence;
                if (_addMissingStreamsButton != null)
                {
                    _addMissingStreamsButton.Text = I18n.Translation.AddMissingStreams;
                }
                if (_customButton != null)
                {
                    _customButton.Text = I18n.Translation.MediaImportCustom;
                }

                int savedAddIndex = _addCombo.SelectedIndex;
                int savedStreamIndex = _streamCombo.SelectedIndex;
                int savedEventLengthIndex = _eventLengthCombo.SelectedIndex;

                _addCombo.DataSource = null;
                _addCombo.DataSource = I18n.Translation.MediaImportAddType.Clone();
                if (savedAddIndex >= 0 && savedAddIndex < _addCombo.Items.Count)
                {
                    _addCombo.SelectedIndex = savedAddIndex;
                }

                _streamCombo.DataSource = null;
                _streamCombo.DataSource = I18n.Translation.MediaImportStreamType.Clone();
                if (savedStreamIndex >= 0 && savedStreamIndex < _streamCombo.Items.Count)
                {
                    _streamCombo.SelectedIndex = savedStreamIndex;
                }

                _eventLengthCombo.DataSource = null;
                _eventLengthCombo.DataSource = I18n.Translation.MediaImportEventLengthType.Clone();
                if (savedEventLengthIndex >= 0 && savedEventLengthIndex < _eventLengthCombo.Items.Count)
                {
                    _eventLengthCombo.SelectedIndex = savedEventLengthIndex;
                }
            }
            finally
            {
                ResumeLayout(true);
                PerformLayout();
                RefreshLayoutAfterLocalization();
            }
        }

        private static void ShowCustomMediaImportDialog(UltraPasteSettings.MediaImportSettings settings)
        {
            Form dialog = new Form
            {
                ShowInTaskbar = false,
                AutoSize = true,
                BackColor = VegasCommonHelper.UIColors[0],
                ForeColor = VegasCommonHelper.UIColors[1],
                Font = new Font(I18n.Translation.Font, 9),
                Text = I18n.Translation.MediaImportCustom,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                StartPosition = FormStartPosition.CenterScreen,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            UltraTableLayoutPanel container = new UltraTableLayoutPanel();
            dialog.Controls.Add(container);

            Label label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportCustomIncludedFiles,
                AutoSize = true
            };
            container.Controls.Add(label);

            ComboBox combo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = UltraPasteCommon.Settings.Customs,
                DisplayMember = "IncludedFiles",
                DropDownStyle = ComboBoxStyle.DropDown,
                Dock = DockStyle.Fill
            };
            container.Controls.Add(combo);
            container.SetColumnSpan(combo, 2);

            TableLayoutPanel buttonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                ColumnCount = 2
            };
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            container.Controls.Add(buttonsPanel);

            Button saveButton = new Button
            {
                Text = "√",
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            saveButton.FlatAppearance.BorderSize = 0;
            buttonsPanel.Controls.Add(saveButton);

            Button removeButton = new Button
            {
                Text = "×",
                Margin = new Padding(3, 0, 3, 9),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            removeButton.FlatAppearance.BorderSize = 0;
            buttonsPanel.Controls.Add(removeButton);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.StartPosition,
                AutoSize = true
            };
            container.Controls.Add(label);

            ComboBox startPositionCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.StartPositionType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            container.Controls.Add(startPositionCombo);

            CheckBox cursorToEnd = new CheckBox
            {
                Text = I18n.Translation.CursorToEnd,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.CursorToEnd ?? true
            };
            container.Controls.Add(cursorToEnd);
            container.SetColumnSpan(cursorToEnd, 2);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportAdd,
                AutoSize = true
            };
            container.Controls.Add(label);

            ComboBox addCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportAddType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            container.Controls.Add(addCombo);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportStream,
                AutoSize = true
            };
            container.Controls.Add(label);

            ComboBox streamCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportStreamType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            container.Controls.Add(streamCombo);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportEventLength,
                AutoSize = true
            };
            container.Controls.Add(label);

            ComboBox eventLengthCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportEventLengthType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            container.Controls.Add(eventLengthCombo);

            CheckBox imageSequence = new CheckBox
            {
                Text = I18n.Translation.MediaImportImageSequence,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.ImageSequence ?? true
            };
            container.Controls.Add(imageSequence);
            container.SetColumnSpan(imageSequence, 2);

            saveButton.Click += (o, e) =>
            {
                string includedFiles = combo.Text;
                if (string.IsNullOrEmpty(includedFiles))
                {
                    return;
                }

                UltraPasteSettings.CustomMediaImportSettings customSettings = UltraPasteCommon.Settings.Customs.Find(c => c.IncludedFiles == includedFiles);
                if (customSettings == null)
                {
                    customSettings = new UltraPasteSettings.CustomMediaImportSettings { IncludedFiles = includedFiles };
                    UltraPasteCommon.Settings.Customs.Add(customSettings);
                }

                customSettings.StartPositionType = startPositionCombo.SelectedIndex;
                customSettings.CursorToEnd = cursorToEnd.Checked;
                customSettings.AddType = addCombo.SelectedIndex;
                customSettings.StreamType = streamCombo.SelectedIndex;
                customSettings.EventLengthType = eventLengthCombo.SelectedIndex;
                customSettings.ImageSequence = imageSequence.Checked;

                combo.DataSource = null;
                combo.DataSource = UltraPasteCommon.Settings.Customs;
                combo.DisplayMember = "IncludedFiles";
                combo.SelectedItem = customSettings;
            };

            removeButton.Click += (o, e) =>
            {
                UltraPasteSettings.CustomMediaImportSettings customSettings = UltraPasteCommon.Settings.Customs.Find(c => c.IncludedFiles == combo.Text);
                if (customSettings == null)
                {
                    return;
                }

                UltraPasteCommon.Settings.Customs.Remove(customSettings);
                combo.DataSource = null;
                combo.DataSource = UltraPasteCommon.Settings.Customs;
                combo.DisplayMember = "IncludedFiles";
            };

            combo.SelectedValueChanged += (o, e) =>
            {
                if (combo.SelectedItem is UltraPasteSettings.CustomMediaImportSettings customSettings)
                {
                    startPositionCombo.SelectedIndex = customSettings.StartPositionType;
                    cursorToEnd.Checked = customSettings.CursorToEnd;
                    addCombo.SelectedIndex = customSettings.AddType;
                    streamCombo.SelectedIndex = customSettings.StreamType;
                    eventLengthCombo.SelectedIndex = customSettings.EventLengthType;
                    imageSequence.Checked = customSettings.ImageSequence;
                }
            };

            dialog.ShowDialog();
        }
    }
}
