using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using ScriptPortal.Vegas;

namespace UltraPaste.UltraControls
{
    internal partial class UltraTableLayoutPanel_CapCutData : UltraTableLayoutPanel
    {
        private readonly ComboBox _draftCombo;

        private sealed class DraftItem
        {
            public DraftItem(string displayName, string jsonPath)
            {
                DisplayName = displayName;
                JsonPath = jsonPath;
            }

            public string DisplayName { get; }

            public string JsonPath { get; }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        public UltraTableLayoutPanel_CapCutData(UltraPasteSettings.CapCutDataSettings settings, ContainerControl formControl, bool addOneClickGroup = true) : base(settings, formControl)
        {
            Name = I18n.Translation.CapCutData;

            CheckBox closeGap = new CheckBox
            {
                Text = I18n.Translation.CloseGap,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.CloseGap ?? true
            };
            Controls.Add(closeGap);
            SetColumnSpan(closeGap, 2);

            CheckBox subtitlesOnly = new CheckBox
            {
                Text = I18n.Translation.SubtitlesOnly,
                Margin = new Padding(6, 8, 6, 6),
                AutoSize = true,
                Checked = settings?.SubtitlesOnly ?? true
            };
            Controls.Add(subtitlesOnly);
            SetColumnSpan(subtitlesOnly, 2);

            if (settings != null)
            {
                closeGap.CheckedChanged += (o, e) => { settings.CloseGap = closeGap.Checked; };
                subtitlesOnly.CheckedChanged += (o, e) => { settings.SubtitlesOnly = subtitlesOnly.Checked; };
            }

            _draftCombo = new ComboBox
            {
                AutoSize = true,
                Margin = new Padding(9, 6, 11, 6),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            Controls.Add(_draftCombo);
            SetColumnSpan(_draftCombo, 3);

            TableLayoutPanel buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                ColumnCount = 2
            };
            for (int i = 0; i < buttonPanel.ColumnCount; i++)
            {
                buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / buttonPanel.ColumnCount));
            }
            Controls.Add(buttonPanel);

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
            buttonPanel.Controls.Add(refreshButton);
            refreshButton.Click += (o, e) => RefreshDraftList();


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
                DraftItem selected = _draftCombo.SelectedItem as DraftItem;
                Timecode t = null;
                using (UndoBlock undo = new UndoBlock(UltraPasteCommon.Vegas.Project, I18n.Translation.UltraPaste))
                {
                    UltraPasteCommon.DoPaste_FileDrop_CapCutData(selected.JsonPath, ref t);
                }
            };
            buttonPanel.Controls.Add(okButton);

            if (formControl is Form form)
            {
                form.Load += (o, e) => RefreshDraftList();
            }
            else if (formControl is UserControl uc)
            {
                uc.Load += (o, e) => RefreshDraftList();
            }

            Label spacer = new Label();
            Controls.Add(spacer);
            SetColumnSpan(spacer, 4);
        }

        private void RefreshDraftList()
        {
            List<DraftItem> items = new List<DraftItem>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddDraftItems(items, seen, I18n.Translation.Jianying, GetAllJianyingRoots());
            AddDraftItems(items, seen, I18n.Translation.CapCut, GetAllCapCutRoots());

            _draftCombo.BeginUpdate();
            _draftCombo.Items.Clear();
            foreach (DraftItem item in items)
            {
                _draftCombo.Items.Add(item);
            }

            if (_draftCombo.Items.Count > 0)
            {
                _draftCombo.SelectedIndex = 0;
            }

            _draftCombo.EndUpdate();
        }

        private void AddDraftItems(ICollection<DraftItem> items, ISet<string> seen, string prefix, IEnumerable<string> roots)
        {
            foreach (string root in roots)
            {
                foreach (string folder in EnumerateDraftFolders(root))
                {
                    string jsonPath = ResolveDraftJsonPath(folder);
                    if (jsonPath == null || !seen.Add(jsonPath))
                    {
                        continue;
                    }

                    string folderName = GetFolderName(folder);
                    if (string.IsNullOrEmpty(folderName))
                    {
                        continue;
                    }

                    items.Add(new DraftItem($"[{prefix}] {folderName}", jsonPath));
                }
            }
        }

        private static IEnumerable<string> EnumerateDraftFolders(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                yield break;
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(root);
            }
            catch
            {
                yield break;
            }

            foreach (string directory in directories)
            {
                string folderName = GetFolderName(directory);
                if (string.IsNullOrEmpty(folderName) || folderName.StartsWith(".", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return directory;
            }
        }

        private static string GetFolderName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string normalized = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFileName(normalized);
        }

        private static string ResolveDraftJsonPath(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            string contentPath = Path.Combine(folder, "draft_content.json");
            if (File.Exists(contentPath))
            {
                return contentPath;
            }

            string infoPath = Path.Combine(folder, "draft_info.json");
            if (File.Exists(infoPath))
            {
                return infoPath;
            }

            return null;
        }

        private static IEnumerable<string> GetAllJianyingRoots()
        {
            List<string> paths = new List<string>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            paths.Add(Path.Combine(localAppData, @"JianyingPro\\User Data\\Projects\\com.lveditor.draft"));
            paths.AddRange(GetCustomDraftPaths(@"Software\\Bytedance\\JianyingPro\\GlobalSettings\\History"));
            return paths;
        }

        private static IEnumerable<string> GetAllCapCutRoots()
        {
            List<string> paths = new List<string>();
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            paths.Add(Path.Combine(localAppData, @"CapCut\\User Data\\Projects\\com.lveditor.draft"));
            paths.AddRange(GetCustomDraftPaths(@"Software\\Bytedance\\CapCut\\GlobalSettings\\History"));
            return paths;
        }

        private static IEnumerable<string> GetCustomDraftPaths(string registrySubKey)
        {
            List<string> paths = new List<string>();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registrySubKey))
                {
                    if (key == null)
                    {
                        return paths;
                    }

                    if (key.GetValue("oldCustomDraftPathList") is string[] values)
                    {
                        foreach (string value in values)
                        {
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                paths.Add(value.Trim());
                            }
                        }
                    }
                }
            }
            catch
            {
                return paths;
            }

            return paths;
        }
    }
}
