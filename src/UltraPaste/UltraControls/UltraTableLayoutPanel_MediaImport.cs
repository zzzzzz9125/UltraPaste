using System.Drawing;
using System.Windows.Forms;

namespace UltraPaste.UltraControls
{
    using Utilities;
    internal partial class UltraTableLayoutPanel_MediaImport : UltraTableLayoutPanel
    {
        public UltraTableLayoutPanel_MediaImport(UltraPasteSettings.MediaImportSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.MediaImport;

            Label label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportAdd,
                AutoSize = true
            };
            Controls.Add(label);

            ComboBox addCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportAddType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(addCombo);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportStream,
                AutoSize = true
            };
            Controls.Add(label);

            ComboBox streamCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportStreamType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(streamCombo);

            label = new Label
            {
                Margin = new Padding(6, 9, 0, 6),
                Text = I18n.Translation.MediaImportEventLength,
                AutoSize = true
            };
            Controls.Add(label);

            ComboBox eventLengthCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DataSource = I18n.Translation.MediaImportEventLengthType.Clone(),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(eventLengthCombo);

            CheckBox imageSequence = new CheckBox
            {
                Text = I18n.Translation.MediaImportImageSequence,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.ImageSequence ?? true
            };
            Controls.Add(imageSequence);
            SetColumnSpan(imageSequence, 2);

            if (settings != null)
            {
                if (formControl is Form form)
                {
                    form.Load += (o, e) =>
                    {
                        addCombo.SelectedIndex = settings.AddType;
                        streamCombo.SelectedIndex = settings.StreamType;
                        eventLengthCombo.SelectedIndex = settings.EventLengthType;
                    };
                }
                else if (formControl is UserControl uc)
                {
                    uc.Load += (o, e) =>
                    {
                        addCombo.SelectedIndex = settings.AddType;
                        streamCombo.SelectedIndex = settings.StreamType;
                        eventLengthCombo.SelectedIndex = settings.EventLengthType;
                    };
                }

                addCombo.SelectedIndexChanged += (o, e) => { settings.AddType = addCombo.SelectedIndex; };
                streamCombo.SelectedIndexChanged += (o, e) => { settings.StreamType = streamCombo.SelectedIndex; };
                eventLengthCombo.SelectedIndexChanged += (o, e) => { settings.EventLengthType = eventLengthCombo.SelectedIndex; };
                imageSequence.CheckedChanged += (o, e) => { settings.ImageSequence = imageSequence.Checked; };
            }

            if (addOneClickGroup)
            {
                GroupBox oneClickGroup = new UltraOneClickGroupBox(out TableLayoutPanel buttonsPanel);
                Controls.Add(oneClickGroup);
                SetColumnSpan(oneClickGroup, 4);

                Button button = new Button
                {
                    Text = I18n.Translation.AddMissingStreams,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += UltraPasteCommon.MediaAddMissingStreams;

                button = new Button
                {
                    Text = I18n.Translation.MediaImportCustom,
                    Margin = new Padding(3, 0, 3, 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.None
                };
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Color.FromArgb(127, 127, 127);
                buttonsPanel.Controls.Add(button);

                button.Click += (o, e) => ShowCustomMediaImportDialog(settings);
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
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
