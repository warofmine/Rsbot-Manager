using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

#nullable disable

namespace RSBotManager
{
    public partial class Form1 : Form
    {
        private string rsbotPath = string.Empty;
        private List<BotInstance> bots = new List<BotInstance>();
        private List<Profile> savedProfiles = new List<Profile>(); // Kaydedilen profiller
        private string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private string botsStateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bots_state.json");
        private string profilesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");
        private string commandFormat = "profile"; // Default command format: "profile" (uses --profile parameter)
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel botCountLabel;
        private ToolStripStatusLabel languageLabel;
        private ToolStripComboBox languageComboBox;
        private System.Windows.Forms.Timer refreshTimer;
        private int startDelay = 5; // Başlatma gecikmesi (saniye)
        private string settingsLanguageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.json");

        public Form1()
        {
            InitializeComponent();
            LoadIcon();
            LoadLanguageSettings();
            InitializeUI();
            SetupStatusBar();
            LoadSettings();
            LoadProfiles();
            LoadRunningBots();
            RefreshBotList();
        }
        
        private void LoadIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rsbotmanager.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                // Icon yüklenemezse sessizce devam et
                Debug.WriteLine($"Icon yüklenemedi: {ex.Message}");
            }
        }

        private void SetupStatusBar()
        {
            // Status bar at the bottom
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.BackColor = Color.FromArgb(240, 240, 240);
            
            // Sol tarafta dil seçici
            languageLabel = new ToolStripStatusLabel(LanguageManager.GetText("Language"));
            languageLabel.Margin = new Padding(5, 3, 0, 2);
            
            languageComboBox = new ToolStripComboBox();
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.Width = 100;
            foreach (var lang in LanguageManager.SupportedLanguages)
            {
                languageComboBox.Items.Add(lang);
            }
            languageComboBox.SelectedItem = LanguageManager.CurrentLanguage;
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            
            statusLabel = new ToolStripStatusLabel(LanguageManager.GetText("Ready"));
            statusLabel.Spring = true;
            
            botCountLabel = new ToolStripStatusLabel(LanguageManager.GetText("RunningBots") + " 0");
            botCountLabel.Alignment = ToolStripItemAlignment.Right;
            
            statusStrip.Items.Add(languageLabel);
            statusStrip.Items.Add(languageComboBox);
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(botCountLabel);
            
            this.Controls.Add(statusStrip);
            
            // Auto-refresh timer
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }
        
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            int runningBots = bots.Count(b => b.Process != null && !b.Process.HasExited);
            botCountLabel.Text = $"{LanguageManager.GetText("RunningBots")} {runningBots}";
            RefreshBotList();
        }
        
        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (languageComboBox.SelectedItem != null)
            {
                LanguageManager.CurrentLanguage = languageComboBox.SelectedItem.ToString();
                SaveLanguageSettings();
                UpdateUILanguage();
            }
        }
        
        private void LoadLanguageSettings()
        {
            try
            {
                if (File.Exists(settingsLanguageFilePath))
                {
                    string json = File.ReadAllText(settingsLanguageFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (settings != null && settings.TryGetValue("Language", out string language))
                    {
                        if (LanguageManager.SupportedLanguages.Contains(language))
                        {
                            LanguageManager.CurrentLanguage = language;
                        }
                    }
                }
            }
            catch
            {
                // Hata durumunda Türkçe varsayılan dil olarak kalacak
            }
        }
        
        private void SaveLanguageSettings()
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    { "Language", LanguageManager.CurrentLanguage }
                };
                
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsLanguageFilePath, json);
            }
            catch
            {
                // Hata durumunda sessizce devam et
            }
        }
        
        private void UpdateUILanguage()
        {
            // Form başlığı
            this.Text = LanguageManager.GetText("FormTitle");
            
            // Sol panel
            if (this.Controls.Count > 0)
            {
                var mainSplitContainer = this.Controls.OfType<SplitContainer>().FirstOrDefault();
                if (mainSplitContainer != null)
                {
                    var leftPanel = mainSplitContainer.Panel1.Controls.OfType<Panel>().FirstOrDefault();
                    if (leftPanel != null)
                    {
                        var lblProfiles = leftPanel.Controls.OfType<Label>().FirstOrDefault();
                        if (lblProfiles != null)
                        {
                            lblProfiles.Text = LanguageManager.GetText("ProfileList");
                        }
                    }
                }
            }
            
            // Butonlar
            btnAddProfile.Text = LanguageManager.GetText("Add");
            btnEditProfile.Text = LanguageManager.GetText("Edit");
            btnRemoveProfile.Text = LanguageManager.GetText("Remove");
            btnBrowse.Text = LanguageManager.GetText("Browse");
            btnStartSelected.Text = LanguageManager.GetText("StartSelected");
            btnStop.Text = LanguageManager.GetText("Stop");
            btnStartAll.Text = LanguageManager.GetText("StartAll");
            btnStopAll.Text = LanguageManager.GetText("StopAll");
            
            // ListView kolonları
            if (lvwBots.Columns.Count >= 4)
            {
                lvwBots.Columns[0].Text = LanguageManager.GetText("ProfileName");
                lvwBots.Columns[1].Text = LanguageManager.GetText("PID");
                lvwBots.Columns[2].Text = LanguageManager.GetText("Status");
                lvwBots.Columns[3].Text = LanguageManager.GetText("Display");
            }
            
            // Status bar
            languageLabel.Text = LanguageManager.GetText("Language");
            statusLabel.Text = LanguageManager.GetText("Ready");
            int runningBots = bots.Count(b => b.Process != null && !b.Process.HasExited);
            botCountLabel.Text = $"{LanguageManager.GetText("RunningBots")} {runningBots}";
            
            // Context menu (no longer needed for command format, but kept for compatibility)
            
            // Label'ları güncelle
            UpdateLabels();
            
            // Gizle/Göster butonunu güncelle
            UpdateHideShowButtonText();
            UpdateToggleAllVisibilityButtonText();
            
            // Bot listesini yenile
            RefreshBotList();
        }
        
        private void UpdateLabels()
        {
            // Sağ panel içindeki label'ları güncelle
            var mainSplitContainer = this.Controls.OfType<SplitContainer>().FirstOrDefault();
            if (mainSplitContainer != null)
            {
                var rightPanel = mainSplitContainer.Panel2.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                if (rightPanel != null && rightPanel.Controls.Count > 0)
                {
                    var topPanel = rightPanel.Controls[0] as TableLayoutPanel;
                    if (topPanel != null)
                    {
                        foreach (Control ctrl in topPanel.Controls)
                        {
                            if (ctrl is Label label)
                            {
                                if (label.Text.Contains("RSBot") || label.Text.Contains("Путь"))
                                {
                                    label.Text = LanguageManager.GetText("RSBotPath");
                                }
                                else if (label.Text.Contains("Başlatma") || label.Text.Contains("Интервал") || label.Text.Contains("Start"))
                                {
                                    label.Text = LanguageManager.GetText("StartDelay");
                                }
                                else if (label.Text.Contains("saniye") || label.Text.Contains("second") || label.Text.Contains("секунд") || label.Text.Contains("Sekunden"))
                                {
                                    label.Text = LanguageManager.GetText("Seconds");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void UpdateToggleAllVisibilityButtonText()
        {
            if (btnToggleAllVisibility != null)
            {
                int visibleCount = bots.Count(b => b.Process != null && !b.Process.HasExited && !b.IsHidden);
                bool shouldHide = visibleCount > 0;
                btnToggleAllVisibility.Text = shouldHide ? LanguageManager.GetText("HideAll") : LanguageManager.GetText("ShowAll");
            }
        }

        private void InitializeUI()
        {
            // Form settings
            this.Text = LanguageManager.GetText("FormTitle");
            this.MinimumSize = new Size(1100, 600);
            this.Size = new Size(1200, 700);
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.Font = new Font("Segoe UI", 9F);
            this.FormClosing += Form1_FormClosing;
            
            // Create context menu (kept for compatibility, but command format is now fixed to --profile)
            var contextMenuStrip = new ContextMenuStrip();

            // Ana layout: Sol panel (profiller) + Sağ panel (üst ayarlar + alt bot listesi)
            var mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 280,
                FixedPanel = FixedPanel.Panel1,
                IsSplitterFixed = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(mainSplitContainer);

            // === SOL PANEL: Profil Listesi ===
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(250, 250, 252)
            };
            mainSplitContainer.Panel1.Controls.Add(leftPanel);

            var lblProfiles = new Label
            {
                Text = LanguageManager.GetText("ProfileList"),
                Dock = DockStyle.Top,
                Font = new Font(this.Font.FontFamily, 11F, FontStyle.Bold),
                Height = 35,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 8, 0, 0)
            };

            // Profil butonları paneli
            var profileButtonsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(0, 10, 0, 0)
            };
            profileButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            profileButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            profileButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            profileButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            profileButtonsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

            // Profil listesi (ListBox)
            lstProfiles = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font(this.Font.FontFamily, 12F),
                IntegralHeight = false,
                ItemHeight = 28
            };

            // Kontrolleri doğru sırada ekle (ters sıra: önce bottom, sonra fill, en son top)
            leftPanel.Controls.Add(profileButtonsPanel);
            leftPanel.Controls.Add(lstProfiles);
            leftPanel.Controls.Add(lblProfiles);

            btnAddProfile = new Button
            {
                Text = LanguageManager.GetText("Add"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 2, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 10F, FontStyle.Bold)
            };
            btnAddProfile.FlatAppearance.BorderSize = 0;

            btnEditProfile = new Button
            {
                Text = LanguageManager.GetText("Edit"),
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 0, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(13, 110, 253),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 10F, FontStyle.Bold)
            };
            btnEditProfile.FlatAppearance.BorderSize = 0;

            btnRemoveProfile = new Button
            {
                Text = LanguageManager.GetText("Remove"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 10F, FontStyle.Bold)
            };
            btnRemoveProfile.FlatAppearance.BorderSize = 0;

            btnMoveUp = new Button
            {
                Text = "⬆⬆",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 2, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 12F, FontStyle.Bold)
            };
            btnMoveUp.FlatAppearance.BorderSize = 0;

            btnMoveDown = new Button
            {
                Text = "⬇⬇",
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 12F, FontStyle.Bold)
            };
            btnMoveDown.FlatAppearance.BorderSize = 0;

            // Butonları panele ekle
            profileButtonsPanel.Controls.Add(btnAddProfile, 0, 0);
            profileButtonsPanel.Controls.Add(btnEditProfile, 1, 0);
            
            profileButtonsPanel.Controls.Add(btnRemoveProfile, 0, 1);
            profileButtonsPanel.SetColumnSpan(btnRemoveProfile, 2); // Çıkar butonu 2 sütun genişliğinde
            
            profileButtonsPanel.Controls.Add(btnMoveUp, 0, 2);
            profileButtonsPanel.Controls.Add(btnMoveDown, 1, 2);

            // === SAĞ PANEL: Üst ayarlar + Alt bot listesi ===
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10),
                BackColor = Color.Transparent
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainSplitContainer.Panel2.Controls.Add(rightPanel);

            // Üst panel (RSBot yolu ve kontroller)
            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

            var lblRSBotPath = new Label
            {
                Text = LanguageManager.GetText("RSBotPath"),
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            // RSBot yolu için panel (TextBox + Gözat butonu)
            var rsbotPathPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            rsbotPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rsbotPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

            txtRSBotPath = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 3, 0),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            txtRSBotPath.ContextMenuStrip = contextMenuStrip;

            btnBrowse = new Button
            {
                Text = LanguageManager.GetText("Browse"),
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 4, 0, 0),
                FlatStyle = FlatStyle.System
            };

            rsbotPathPanel.Controls.Add(txtRSBotPath, 0, 0);
            rsbotPathPanel.Controls.Add(btnBrowse, 1, 0);

            var lblDelay = new Label
            {
                Text = LanguageManager.GetText("StartDelay"),
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            txtDelay = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 5, 0),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "5"
            };

            var lblDelayUnit = new Label
            {
                Text = LanguageManager.GetText("Seconds"),
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 5, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Satır 1 butonları: Seçili Profili Başlat, Durdur, Gizle/Göster
            btnStartSelected = new Button
            {
                Text = LanguageManager.GetText("StartSelected"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 3, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 9F, FontStyle.Bold),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnStartSelected.FlatAppearance.BorderSize = 0;

            btnStop = new Button
            {
                Text = LanguageManager.GetText("Stop"),
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 5, 3, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 9F, FontStyle.Bold),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnStop.FlatAppearance.BorderSize = 0;

            btnHideShow = new Button
            {
                Text = LanguageManager.GetText("HideShow"),
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 5, 0, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(13, 110, 253),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 9F, FontStyle.Bold),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnHideShow.FlatAppearance.BorderSize = 0;

            // Satır 2 butonları: Tümünü Başlat, Tümünü Durdur, Tümünü Gizle/Göster
            btnStartAll = new Button
            {
                Text = LanguageManager.GetText("StartAll"),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 3, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 9F, FontStyle.Bold),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnStartAll.FlatAppearance.BorderSize = 0;

            btnStopAll = new Button
            {
                Text = LanguageManager.GetText("StopAll"),
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 3, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 9F, FontStyle.Bold),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnStopAll.FlatAppearance.BorderSize = 0;

            btnToggleAllVisibility = new Button
            {
                Text = LanguageManager.GetText("HideAll"),
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(13, 110, 253),
                ForeColor = Color.White,
                Font = new Font(this.Font.FontFamily, 9F, FontStyle.Bold),
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnToggleAllVisibility.FlatAppearance.BorderSize = 0;

            topPanel.Controls.Add(lblRSBotPath, 0, 0);
            topPanel.Controls.Add(rsbotPathPanel, 1, 0);
            topPanel.SetColumnSpan(rsbotPathPanel, 2);
            
            topPanel.Controls.Add(lblDelay, 0, 1);
            topPanel.Controls.Add(txtDelay, 1, 1);
            topPanel.Controls.Add(lblDelayUnit, 2, 1);
            
            // Satır 2: Seçili Profili Başlat, Durdur, Gizle/Göster
            topPanel.Controls.Add(btnStartSelected, 0, 2);
            topPanel.Controls.Add(btnStop, 1, 2);
            topPanel.Controls.Add(btnHideShow, 2, 2);
            
            // Satır 3: Tümünü Başlat, Tümünü Durdur, Tümünü Gizle/Göster
            topPanel.Controls.Add(btnStartAll, 0, 3);
            topPanel.Controls.Add(btnStopAll, 1, 3);
            topPanel.Controls.Add(btnToggleAllVisibility, 2, 3);

            // Bot listesi
            lvwBots = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            lvwBots.Columns.Add(LanguageManager.GetText("ProfileName"), 200);
            lvwBots.Columns.Add(LanguageManager.GetText("PID"), 80);
            lvwBots.Columns.Add(LanguageManager.GetText("Status"), 120);
            lvwBots.Columns.Add(LanguageManager.GetText("Display"), 100);

            rightPanel.Controls.Add(topPanel, 0, 0);
            rightPanel.Controls.Add(lvwBots, 0, 1);

            // Event handlers
            btnBrowse.Click += btnBrowse_Click;
            btnStartAll.Click += BtnStartAll_Click;
            btnStop.Click += btnStop_Click;
            btnHideShow.Click += btnHideShow_Click;
            lvwBots.SelectedIndexChanged += lvwBots_SelectedIndexChanged;
            btnAddProfile.Click += BtnAddProfile_Click;
            btnEditProfile.Click += BtnEditProfile_Click;
            btnRemoveProfile.Click += BtnRemoveProfile_Click;
            btnMoveUp.Click += BtnMoveUp_Click;
            btnMoveDown.Click += BtnMoveDown_Click;
            lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;
            btnStartSelected.Click += BtnStartSelected_Click;
            btnStopAll.Click += BtnStopAll_Click;
            btnToggleAllVisibility.Click += BtnToggleAllVisibility_Click;

            // Başlangıçta butonları devre dışı bırak
            btnStop.Enabled = false;
            btnStop.BackColor = Color.FromArgb(150, 150, 150);
            
            btnHideShow.Enabled = false;
            btnHideShow.BackColor = Color.FromArgb(150, 150, 150);
            
            btnEditProfile.Enabled = false;
            btnRemoveProfile.Enabled = false;
            btnMoveUp.Enabled = false;
            btnMoveDown.Enabled = false;
            btnStartAll.Enabled = false;
            btnStartSelected.Enabled = false;
            btnStopAll.Enabled = false;
            btnToggleAllVisibility.Enabled = false;
        }

        private void LstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasSelection = lstProfiles.SelectedIndex >= 0;
            btnEditProfile.Enabled = hasSelection;
            btnRemoveProfile.Enabled = hasSelection;
            btnMoveUp.Enabled = hasSelection && lstProfiles.SelectedIndex > 0;
            btnMoveDown.Enabled = hasSelection && lstProfiles.SelectedIndex < lstProfiles.Items.Count - 1;
            btnStartSelected.Enabled = hasSelection && !string.IsNullOrWhiteSpace(txtRSBotPath.Text);
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedIndex <= 0) return;
            
            int index = lstProfiles.SelectedIndex;
            var item = savedProfiles[index];
            
            savedProfiles.RemoveAt(index);
            savedProfiles.Insert(index - 1, item);
            
            RefreshProfileList();
            lstProfiles.SelectedIndex = index - 1;
            SaveProfiles();
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedIndex < 0 || lstProfiles.SelectedIndex >= lstProfiles.Items.Count - 1) return;
            
            int index = lstProfiles.SelectedIndex;
            var item = savedProfiles[index];
            
            savedProfiles.RemoveAt(index);
            savedProfiles.Insert(index + 1, item);
            
            RefreshProfileList();
            lstProfiles.SelectedIndex = index + 1;
            SaveProfiles();
        }

        private void RefreshProfileList()
        {
            lstProfiles.Items.Clear();
            foreach (var profile in savedProfiles)
            {
                lstProfiles.Items.Add(profile.Name);
            }
            
            btnStartAll.Enabled = savedProfiles.Count > 0 && !string.IsNullOrWhiteSpace(txtRSBotPath.Text);
        }
        
        private void BtnAddProfile_Click(object sender, EventArgs e)
        {
            ShowProfileDialog(null);
        }
        
        private void ShowProfileDialog(Profile existingProfile)
        {
            bool isEdit = existingProfile != null;
            
            // Profil adı girmek için dialog oluştur
            using (var inputForm = new Form())
            {
                inputForm.Text = isEdit ? LanguageManager.GetText("EditProfile") : LanguageManager.GetText("AddNewProfile");
                inputForm.Size = new Size(400, 200);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var lblPrompt = new Label
                {
                    Text = LanguageManager.GetText("ProfileNameLabel"),
                    Location = new Point(20, 20),
                    Size = new Size(350, 20)
                };

                var txtInput = new TextBox
                {
                    Location = new Point(20, 45),
                    Size = new Size(340, 25),
                    Text = isEdit ? existingProfile.Name : ""
                };

                var chkStartCl = new CheckBox
                {
                    Text = LanguageManager.GetText("StartWithClient"),
                    Location = new Point(20, 80),
                    Size = new Size(340, 20),
                    Checked = isEdit ? existingProfile.StartCl : false
                };

                var chkStartCls = new CheckBox
                {
                    Text = LanguageManager.GetText("StartWithoutClient"),
                    Location = new Point(20, 105),
                    Size = new Size(340, 20),
                    Checked = isEdit ? existingProfile.StartCls : false
                };

                // Checkbox'lar birbirini dışlasın
                chkStartCl.CheckedChanged += (s, e) => {
                    if (chkStartCl.Checked) chkStartCls.Checked = false;
                };
                chkStartCls.CheckedChanged += (s, e) => {
                    if (chkStartCls.Checked) chkStartCl.Checked = false;
                };

                var btnOK = new Button
                {
                    Text = LanguageManager.GetText("Save"),
                    DialogResult = DialogResult.OK,
                    Location = new Point(200, 135),
                    Size = new Size(80, 30)
                };

                var btnCancel = new Button
                {
                    Text = LanguageManager.GetText("Cancel"),
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(290, 135),
                    Size = new Size(70, 30)
                };

                inputForm.Controls.AddRange(new Control[] { lblPrompt, txtInput, chkStartCl, chkStartCls, btnOK, btnCancel });
                inputForm.AcceptButton = btnOK;
                inputForm.CancelButton = btnCancel;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string profileName = txtInput.Text.Trim();
                    
                    if (string.IsNullOrWhiteSpace(profileName))
                    {
                        MessageBox.Show(LanguageManager.GetText("EnterProfileName"), LanguageManager.GetText("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Aynı isimde profil varsa uyar (düzenleme sırasında mevcut profili hariç tut)
                    if (savedProfiles.Any(p => p.Name == profileName && (!isEdit || p != existingProfile)))
                    {
                        MessageBox.Show(LanguageManager.GetText("ProfileAlreadyExists", profileName), LanguageManager.GetText("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    if (isEdit)
                    {
                        // Mevcut profili güncelle
                        existingProfile.Name = profileName;
                        existingProfile.StartCl = chkStartCl.Checked;
                        existingProfile.StartCls = chkStartCls.Checked;
                        RefreshProfileList();
                        SaveProfiles();
                        
                        // Güncellenen profili seç
                        int index = savedProfiles.IndexOf(existingProfile);
                        if (index >= 0)
                            lstProfiles.SelectedIndex = index;
                        
                        MessageBox.Show(LanguageManager.GetText("ProfileUpdated", profileName), LanguageManager.GetText("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Yeni profili kaydet
                        var newProfile = new Profile
                        {
                            Name = profileName,
                            StartCl = chkStartCl.Checked,
                            StartCls = chkStartCls.Checked
                        };
                        savedProfiles.Add(newProfile);
                        RefreshProfileList();
                        SaveProfiles();
                        
                        // Yeni eklenen profili seç
                        lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1;
                        
                        MessageBox.Show(LanguageManager.GetText("ProfileSaved", profileName), LanguageManager.GetText("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        
        private void BtnRemoveProfile_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedIndex < 0) return;
            
            string profileName = lstProfiles.SelectedItem.ToString();
            var profile = savedProfiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null) return;
            
            if (MessageBox.Show(LanguageManager.GetText("ConfirmDeleteProfile", profileName), LanguageManager.GetText("DeleteProfile"), 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int selectedIndex = lstProfiles.SelectedIndex;
                savedProfiles.Remove(profile);
                RefreshProfileList();
                SaveProfiles();
                
                // Silindikten sonra bir sonraki öğeyi seç
                if (lstProfiles.Items.Count > 0)
                {
                    if (selectedIndex >= lstProfiles.Items.Count)
                        lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1;
                    else
                        lstProfiles.SelectedIndex = selectedIndex;
                }
            }
        }
        
        private void BtnEditProfile_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedIndex < 0) return;
            
            string profileName = lstProfiles.SelectedItem.ToString();
            var profile = savedProfiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null) return;
            
            ShowProfileDialog(profile);
        }
        
        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(profilesFilePath))
                {
                    string json = File.ReadAllText(profilesFilePath);
                    
                    // Eski format (List<string>) kontrolü
                    try
                    {
                        var profiles = JsonSerializer.Deserialize<List<Profile>>(json);
                        if (profiles != null)
                        {
                            savedProfiles = profiles;
                            RefreshProfileList();
                            return;
                        }
                    }
                    catch
                    {
                        // Yeni format başarısız oldu, eski formatı dene
                    }
                    
                    // Eski format (List<string>) - geriye dönük uyumluluk
                    var oldProfiles = JsonSerializer.Deserialize<List<string>>(json);
                    if (oldProfiles != null)
                    {
                        savedProfiles = oldProfiles.Select(name => new Profile { Name = name, StartCl = false, StartCls = false }).ToList();
                        RefreshProfileList();
                        SaveProfiles(); // Yeni formata dönüştür ve kaydet
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Profiller yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SaveProfiles()
        {
            try
            {
                string json = JsonSerializer.Serialize(savedProfiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(profilesFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Profiller kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            SaveRunningBots();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "RSBot Uygulaması (RSBot.exe)|RSBot.exe|Tüm Dosyalar (*.*)|*.*";
                dialog.Title = "RSBot uygulamasını seçin";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtRSBotPath.Text = dialog.FileName;
                    rsbotPath = dialog.FileName;
                    SaveSettings();
                    RefreshProfileList(); // Buton durumunu güncelle
                }
            }
        }

        private async void BtnStartAll_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRSBotPath.Text))
            {
                MessageBox.Show(LanguageManager.GetText("SelectRSBotPath"), LanguageManager.GetText("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (savedProfiles.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetText("NoProfilesFound"), LanguageManager.GetText("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Delay değerini al
            if (!int.TryParse(txtDelay.Text, out int delay) || delay < 0)
            {
                MessageBox.Show(LanguageManager.GetText("SelectRSBotPath"), LanguageManager.GetText("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtDelay.Focus();
                return;
            }

            startDelay = delay;

            // Butonları devre dışı bırak
            btnStartAll.Enabled = false;
            btnAddProfile.Enabled = false;
            btnRemoveProfile.Enabled = false;
            btnMoveUp.Enabled = false;
            btnMoveDown.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                statusLabel.Text = LanguageManager.GetText("BotsStarting");

                for (int i = 0; i < savedProfiles.Count; i++)
                {
                    var profile = savedProfiles[i];
                    
                    // Zaten çalışıyor mu kontrol et
                    var existingBot = bots.Find(b => b.Name == profile.Name && b.Process != null && !b.Process.HasExited);
                    if (existingBot != null)
                    {
                        statusLabel.Text = LanguageManager.GetText("AlreadyRunning", profile.Name);
                        await Task.Delay(1000);
                        continue;
                    }

                    statusLabel.Text = LanguageManager.GetText("StartingProfile", profile.Name, i + 1, savedProfiles.Count);
                    
                    // Botu başlat
                    await StartBot(profile);

                    // Son bot değilse delay uygula
                    if (i < savedProfiles.Count - 1 && startDelay > 0)
                    {
                        for (int countdown = startDelay; countdown > 0; countdown--)
                        {
                            statusLabel.Text = LanguageManager.GetText("NextBotIn", countdown);
                            await Task.Delay(1000);
                        }
                    }
                }

                statusLabel.Text = LanguageManager.GetText("AllBotsStarted");
                await Task.Delay(2000);
                statusLabel.Text = LanguageManager.GetText("Ready");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LanguageManager.GetText("Error")}: {ex.Message}", LanguageManager.GetText("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = LanguageManager.GetText("Error");
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnStartAll.Enabled = true;
                btnAddProfile.Enabled = true;
                RefreshBotList();
            }
        }

        private async Task StartBot(Profile profile)
        {
            try
            {
                // RSBot komut formatı: rsbot.exe -p profileadı [--launch-client|--launch-clientless]
                string arguments = $"-p \"{profile.Name}\"";
                
                if (profile.StartCl)
                {
                    arguments += " --launch-client";
                }
                else if (profile.StartCls)
                {
                    arguments += " --launch-clientless";
                }
                
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = txtRSBotPath.Text,
                    Arguments = arguments,
                    UseShellExecute = true
                };
                
                Process process = Process.Start(psi);
                if (process != null)
                {
                    BotInstance bot = new BotInstance
                    {
                        Name = profile.Name,
                        Process = process,
                        StartTime = DateTime.Now,
                        IsHidden = false
                    };
                    
                    bots.Add(bot);
                    
                    // Pencere handle'ını bulmak için kısa bir bekleme
                    await Task.Delay(3000);
                    if (!process.HasExited)
                    {
                        process.Refresh();
                        bot.WindowHandle = process.MainWindowHandle;
                    }
                    
                    RefreshBotList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"'{profile.Name}' başlatılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            if (lvwBots.SelectedItems.Count == 0) return;
            
            string selectedBotName = lvwBots.SelectedItems[0].Text;
            var botInstance = bots.Find(b => b.Name == selectedBotName);
            
            if (botInstance != null && botInstance.Process != null && !botInstance.Process.HasExited)
            {
                try
                {
                    botInstance.Process.Kill();
                    botInstance.Process.WaitForExit(3000);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bot durdurulurken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                RefreshBotList();                SaveRunningBots();
            }
        }


        private void lvwBots_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }
          private void UpdateButtonStates()
        {
            bool hasSelection = lvwBots.SelectedItems.Count > 0;
            bool hasBots = bots.Count > 0;
            
            btnStop.Enabled = hasSelection;
            btnHideShow.Enabled = hasSelection;
            
            // Yeni butonların durumları
            btnStopAll.Enabled = hasBots;
            btnToggleAllVisibility.Enabled = hasBots;
            
            // Buton renklerini duruma göre ayarla
            if (!hasSelection)
            {
                btnStop.BackColor = Color.FromArgb(150, 150, 150);
                btnHideShow.BackColor = Color.FromArgb(150, 150, 150);
            }
            else
            {
                btnStop.BackColor = Color.FromArgb(220, 53, 69);
                // btnHideShow rengi UpdateHideShowButtonText'te ayarlanacak
            }
            
            // Update the hide/show button text based on the selection
            UpdateHideShowButtonText();
        }
        
        private void UpdateHideShowButtonText()
        {
            if (lvwBots.SelectedItems.Count == 0)
            {
                btnHideShow.Text = LanguageManager.GetText("HideShow");
                btnHideShow.BackColor = Color.FromArgb(108, 117, 125); // Gray for inactive
                btnHideShow.ForeColor = Color.White;
                return;
            }
            
            string selectedBotName = lvwBots.SelectedItems[0].Text;
            var botInstance = bots.Find(b => b.Name == selectedBotName);
            
            if (botInstance != null && botInstance.Process != null && !botInstance.Process.HasExited)
            {
                if (botInstance.IsHidden)
                {
                    btnHideShow.Text = LanguageManager.GetText("Show");
                    btnHideShow.BackColor = Color.FromArgb(0, 123, 255); // Blue for Show
                    btnHideShow.ForeColor = Color.White;
                }
                else
                {
                    btnHideShow.Text = LanguageManager.GetText("Hide");
                    btnHideShow.BackColor = Color.FromArgb(40, 167, 69); // Green for Hide
                    btnHideShow.ForeColor = Color.White;
                }
            }
            else
            {
                btnHideShow.Text = LanguageManager.GetText("HideShow");
                btnHideShow.BackColor = Color.FromArgb(108, 117, 125); // Gray for inactive
                btnHideShow.ForeColor = Color.White;
            }
        }

        private void RefreshBotList()
        {
            // Mevcut seçimi hatırla
            string selectedBotName = null;
            if (lvwBots.SelectedItems.Count > 0)
            {
                selectedBotName = lvwBots.SelectedItems[0].Text;
            }
            
            lvwBots.Items.Clear();
            
            // Çıkış yapmış botları kontrol et ve kaldır
            for (int i = bots.Count - 1; i >= 0; i--)
            {
                if (bots[i].Process == null || bots[i].Process.HasExited)
                {
                    // Kaydedilmiş durumdan yüklenen ve hala çalışan ama yeniden bağlanması gereken bir bot mu diye kontrol et
                    if (bots[i].ProcessId > 0)
                    {
                        try
                        {
                            Process existingProcess = Process.GetProcessById(bots[i].ProcessId);
                            if (!existingProcess.HasExited)
                            {
                                bots[i].Process = existingProcess;
                                continue; // Kaldırma, yeniden bağlandık
                            }
                        }
                        catch
                        {
                            // Process artık mevcut değil
                        }
                    }
                    
                    bots.RemoveAt(i);
                }
            }
            
            // Tüm aktif botları listeye ekle
            foreach (var bot in bots)
            {
                if (bot.Process != null)
                {
                    try
                    {
                        bot.Process.Refresh();
                        
                        var item = new ListViewItem(bot.Name);
                        item.UseItemStyleForSubItems = false;
                        
                        var pidItem = item.SubItems.Add(bot.Process.Id.ToString());
                        
                        var statusItem = item.SubItems.Add(bot.Process.HasExited ? LanguageManager.GetText("Closed") : LanguageManager.GetText("Running"));
                        var displayItem = item.SubItems.Add(bot.IsHidden ? LanguageManager.GetText("Hidden") : LanguageManager.GetText("Visible"));
                        
                        // Bot durumuna göre renklendirme
                        if (bot.Process.HasExited)
                        {
                            // Kapalı bot - kırmızı
                            item.ForeColor = Color.Red;
                            item.BackColor = Color.FromArgb(255, 240, 240);
                            statusItem.Text = LanguageManager.GetText("Closed");
                            statusItem.ForeColor = Color.Red;
                        }
                        else if (bot.IsHidden)
                        {
                            // Gizli bot - mavi
                            item.ForeColor = Color.DarkBlue;
                            item.BackColor = Color.FromArgb(240, 240, 255);
                            statusItem.Text = LanguageManager.GetText("Running");
                            statusItem.ForeColor = Color.Green;
                            displayItem.Text = LanguageManager.GetText("Hidden");
                            displayItem.ForeColor = Color.Blue;
                        }
                        else
                        {
                            // Çalışan ve görünen bot - yeşil
                            item.ForeColor = Color.DarkGreen;
                            item.BackColor = Color.FromArgb(240, 255, 240);
                            statusItem.Text = LanguageManager.GetText("Running");
                            statusItem.ForeColor = Color.Green;
                            displayItem.Text = LanguageManager.GetText("Visible");
                            displayItem.ForeColor = Color.Black;
                        }
                        
                        // Bot adı için kalın font
                        item.Font = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Bold);
                        
                        lvwBots.Items.Add(item);
                        
                        // Önceden seçili olan bot varsa, tekrar seç
                        if (selectedBotName != null && bot.Name == selectedBotName)
                        {
                            item.Selected = true;
                            item.EnsureVisible();
                        }
                    }
                    catch
                    {
                        // Process kapanmış olabilir
                    }
                }
            }
            
            // Eğer hiç öğe renklendirme yapılmadıysa, alternatif satır renkleri ekle
            if (lvwBots.Items.Count > 0 && lvwBots.Items[0].BackColor == SystemColors.Window)
            {
                for (int i = 0; i < lvwBots.Items.Count; i++)
                {
                    if (i % 2 == 0)
                        lvwBots.Items[i].BackColor = Color.FromArgb(250, 250, 250);
                    else
                        lvwBots.Items[i].BackColor = Color.FromArgb(245, 245, 245);
                }
            }
            
            // Buton durumlarını güncelle
            UpdateButtonStates();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (settings != null)
                    {
                        if (settings.TryGetValue("RSBotPath", out string path))
                        {
                            rsbotPath = path;
                            txtRSBotPath.Text = path;
                        }
                        
                        // CommandFormat artık kullanılmıyor, her zaman --profile formatı kullanılıyor
                        commandFormat = "profile";
                        
                        if (settings.TryGetValue("StartDelay", out string delayStr) && int.TryParse(delayStr, out int delay))
                        {
                            startDelay = delay;
                            txtDelay.Text = delay.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    { "RSBotPath", txtRSBotPath.Text },
                    { "CommandFormat", commandFormat },
                    { "StartDelay", startDelay.ToString() }
                };
                
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRunningBots()
        {
            try
            {
                if (File.Exists(botsStateFilePath))
                {
                    string json = File.ReadAllText(botsStateFilePath);
                    var botStates = JsonSerializer.Deserialize<List<BotState>>(json);
                    
                    if (botStates != null)
                    {
                        foreach (var state in botStates)
                        {
                            try
                            {
                                Process process = Process.GetProcessById(state.ProcessId);
                                if (!process.HasExited && process.ProcessName.Contains("RSBot"))
                                {
                                    BotInstance bot = new BotInstance
                                    {
                                        Name = state.Name,
                                        Process = process,
                                        ProcessId = state.ProcessId,
                                        StartTime = state.StartTime,
                                        IsHidden = state.IsHidden,
                                        WindowHandle = new IntPtr(state.WindowHandleValue)
                                    };
                                    
                                    bots.Add(bot);
                                }
                            }
                            catch
                            {
                                // Process no longer exists, skip it
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bot durumları yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveRunningBots()
        {
            try
            {
                var botStates = new List<BotState>();
                
                foreach (var bot in bots)
                {
                    if (bot.Process != null && !bot.Process.HasExited)
                    {
                        botStates.Add(new BotState
                        {
                            Name = bot.Name,
                            ProcessId = bot.Process.Id,
                            StartTime = bot.StartTime,
                            IsHidden = bot.IsHidden,
                            WindowHandleValue = bot.WindowHandle.ToInt64()
                        });
                    }
                }
                
                string json = JsonSerializer.Serialize(botStates, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(botsStateFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bot durumları kaydedilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnStartSelected_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedIndex < 0) return;
            
            string profileName = lstProfiles.SelectedItem.ToString();
            var profile = savedProfiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null) return;
            
            // Zaten çalışıyor mu kontrol et
            var existingBot = bots.Find(b => b.Name == profileName && b.Process != null && !b.Process.HasExited);
            if (existingBot != null)
            {
                MessageBox.Show(LanguageManager.GetText("AlreadyRunning", profileName), LanguageManager.GetText("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            btnStartSelected.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            
            try
            {
                statusLabel.Text = LanguageManager.GetText("BotsStarting");
                await StartBot(profile);
                statusLabel.Text = LanguageManager.GetText("Ready");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LanguageManager.GetText("Error")}: {ex.Message}", LanguageManager.GetText("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnStartSelected.Enabled = lstProfiles.SelectedIndex >= 0;
            }
        }

        private void BtnStopAll_Click(object sender, EventArgs e)
        {
            if (bots.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetText("NoRunningBots"), LanguageManager.GetText("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            if (MessageBox.Show(LanguageManager.GetText("ConfirmStopAll", bots.Count), 
                LanguageManager.GetText("StopAll"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            
            int stoppedCount = 0;
            foreach (var bot in bots.ToList())
            {
                if (bot.Process != null && !bot.Process.HasExited)
                {
                    try
                    {
                        bot.Process.Kill();
                        bot.Process.WaitForExit(1000);
                        stoppedCount++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{LanguageManager.GetText("Error")}: {ex.Message}", LanguageManager.GetText("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            
            RefreshBotList();
            SaveRunningBots();
            MessageBox.Show(LanguageManager.GetText("BotsStopped", stoppedCount), LanguageManager.GetText("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnToggleAllVisibility_Click(object sender, EventArgs e)
        {
            if (bots.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetText("NoRunningBots"), LanguageManager.GetText("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            btnToggleAllVisibility.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            
            try
            {
                // Tüm botların durumunu kontrol et
                int visibleCount = bots.Count(b => b.Process != null && !b.Process.HasExited && !b.IsHidden);
                bool shouldHide = visibleCount > 0;
                
                foreach (var bot in bots)
                {
                    if (bot.Process != null && !bot.Process.HasExited)
                    {
                        bot.Process.Refresh();
                        
                        // Pencere handle'ı bul (tek tek gizleme ile aynı mantık)
                        IntPtr handle = IntPtr.Zero;
                        
                        // 1. Önce kayıtlı handle'ı kontrol et
                        if (bot.WindowHandle != IntPtr.Zero && NativeMethods.IsWindow(bot.WindowHandle))
                        {
                            int length = NativeMethods.GetWindowTextLength(bot.WindowHandle);
                            if (length > 0)
                            {
                                var sb = new StringBuilder(length + 1);
                                NativeMethods.GetWindowText(bot.WindowHandle, sb, sb.Capacity);
                                string title = sb.ToString();
                                
                                if (title.Contains("RSBot") || title.Contains("Silkroad") || title.Contains("SRO"))
                                {
                                    handle = bot.WindowHandle;
                                }
                            }
                        }
                        
                        // 2. Process'in MainWindowHandle'ını dene
                        if (handle == IntPtr.Zero && bot.Process.MainWindowHandle != IntPtr.Zero && 
                            NativeMethods.IsWindow(bot.Process.MainWindowHandle))
                        {
                            int length = NativeMethods.GetWindowTextLength(bot.Process.MainWindowHandle);
                            if (length > 0)
                            {
                                var sb = new StringBuilder(length + 1);
                                NativeMethods.GetWindowText(bot.Process.MainWindowHandle, sb, sb.Capacity);
                                string title = sb.ToString();
                                
                                if (title.Contains("RSBot") || title.Contains("Silkroad") || title.Contains("SRO"))
                                {
                                    handle = bot.Process.MainWindowHandle;
                                }
                            }
                        }
                        
                        // 3. Son çare olarak FindMainWindow'u kullan
                        if (handle == IntPtr.Zero)
                        {
                            handle = NativeMethods.FindMainWindow(bot.Process.Id);
                        }
                        
                        if (handle != IntPtr.Zero)
                        {
                            bot.WindowHandle = handle;
                            
                            if (shouldHide)
                            {
                                // Gizleme - tek tek gizleme ile aynı mantık
                                NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
                                
                                // Başarısız olursa minimize et
                                if (NativeMethods.IsWindowVisible(handle))
                                {
                                    NativeMethods.ShowWindow(handle, NativeMethods.SW_MINIMIZE);
                                }
                                
                                bot.IsHidden = true;
                            }
                            else
                            {
                                // Gösterme - tek tek gösterme ile aynı mantık
                                NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                                await Task.Delay(100);
                                
                                NativeMethods.ShowWindow(handle, NativeMethods.SW_NORMAL);
                                await Task.Delay(100);
                                
                                NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
                                await Task.Delay(100);
                                
                                bot.IsHidden = false;
                            }
                        }
                    }
                }
                
                // Buton metnini güncelle
                btnToggleAllVisibility.Text = shouldHide ? LanguageManager.GetText("ShowAll") : LanguageManager.GetText("HideAll");
                
                RefreshBotList();
                SaveRunningBots();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnToggleAllVisibility.Enabled = true;
            }
        }

        private async void btnHideShow_Click(object sender, EventArgs e)
        {
            // Seçili bot yok ise bir şey yapma
            if (lvwBots.SelectedItems.Count == 0) return;

            // Seçili botu bul
            string selectedBotName = lvwBots.SelectedItems[0].Text;
            var botInstance = bots.Find(b => b.Name == selectedBotName);
            
            // Bot bulunamadıysa veya çalışmıyorsa uyarı ver
            if (botInstance == null || botInstance.Process == null || botInstance.Process.HasExited) {
                MessageBox.Show("Seçilen bot çalışmıyor veya bulunamıyor.");
                RefreshBotList();
                return;
            }
            
            // Butonu devre dışı bırak
            btnHideShow.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            
            try {
                // Process bilgilerini güncelle
                botInstance.Process.Refresh();
                
                // Pencere handle'ı bul (en güvenilir yöntemle)
                IntPtr handle = IntPtr.Zero;
                
                // 1. Önce kayıtlı handle'ı kontrol et
                if (botInstance.WindowHandle != IntPtr.Zero && NativeMethods.IsWindow(botInstance.WindowHandle)) {
                    // Pencere başlığını kontrol et - RSBot ana penceresi olduğundan emin ol
                    int length = NativeMethods.GetWindowTextLength(botInstance.WindowHandle);
                    if (length > 0) {
                        var sb = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(botInstance.WindowHandle, sb, sb.Capacity);
                        string title = sb.ToString();
                        
                        // RSBot ana penceresi başlığını kontrol et
                        if (title.Contains("RSBot") || title.Contains("Silkroad") || title.Contains("SRO")) {
                            handle = botInstance.WindowHandle;
                        }
                    }
                }
                
                // 2. Process'in MainWindowHandle'ını dene
                if (handle == IntPtr.Zero && botInstance.Process.MainWindowHandle != IntPtr.Zero && 
                    NativeMethods.IsWindow(botInstance.Process.MainWindowHandle)) {
                    // Pencere başlığını kontrol et
                    int length = NativeMethods.GetWindowTextLength(botInstance.Process.MainWindowHandle);
                    if (length > 0) {
                        var sb = new StringBuilder(length + 1);
                        NativeMethods.GetWindowText(botInstance.Process.MainWindowHandle, sb, sb.Capacity);
                        string title = sb.ToString();
                        
                        if (title.Contains("RSBot") || title.Contains("Silkroad") || title.Contains("SRO")) {
                            handle = botInstance.Process.MainWindowHandle;
                        }
                    }
                }
                
                // 3. Son çare olarak FindMainWindow'u kullan
                if (handle == IntPtr.Zero) {
                    handle = NativeMethods.FindMainWindow(botInstance.Process.Id);
                }
                
                // Pencere bulunamadıysa hata ver
                if (handle == IntPtr.Zero) {
                    MessageBox.Show("Bot penceresi bulunamadı. Botun tam açıldığından emin olun.");
                    return;
                }
                
                // Bulunan handle'ı kaydet
                botInstance.WindowHandle = handle;
                
                // Mevcut pencere durumunu kontrol et
                bool isCurrentlyVisible = NativeMethods.IsWindowVisible(handle);
                
                // Gizleme/gösterme işlemini gerçekleştir
                if (botInstance.IsHidden || !isCurrentlyVisible)  // Gizliyse göster
                {
                    // Komut dizisi uygula (daha güvenilir)
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                    await Task.Delay(100);
                    
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_NORMAL);
                    await Task.Delay(100);
                    
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
                    await Task.Delay(100);
                    
                    NativeMethods.SetForegroundWindow(handle);
                    
                    // Bot durumunu güncelle
                    botInstance.IsHidden = false;
                    
                    // Sonucu kontrol et
                    bool nowVisible = NativeMethods.IsWindowVisible(handle);
                    
                    if (!nowVisible) {
                        MessageBox.Show("RSBot penceresi gösterme işlemi başarısız oldu. Lütfen tekrar deneyin.");
                    }
                }
                else  // Görünürse gizle
                {
                    // Basit ve hızlı gizleme
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
                    
                    // Başarısız olursa minimize et
                    if (NativeMethods.IsWindowVisible(handle))
                    {
                        NativeMethods.ShowWindow(handle, NativeMethods.SW_MINIMIZE);
                    }
                    
                    // Her durumda gizli olarak işaretle
                    botInstance.IsHidden = true;
                }
                
                // Buton metnini güncelle
                UpdateHideShowButtonText();
                
                // Listeyi ve ayarları güncelle
                RefreshBotList();
                SaveRunningBots();
            }
            catch (Exception ex) {
                MessageBox.Show($"Gizle/Göster işlemi sırasında hata: {ex.Message}");
            }
            finally {
                this.Cursor = Cursors.Default;
                btnHideShow.Enabled = true;
            }
        }

        // Form controls
        private TextBox txtRSBotPath = null!;
        private TextBox txtDelay = null!;
        private ListBox lstProfiles = null!;
        private Button btnBrowse = null!;
        private Button btnStartAll = null!;
        private Button btnStop = null!;
        private Button btnHideShow = null!;
        private Button btnAddProfile = null!;
        private Button btnEditProfile = null!;
        private Button btnRemoveProfile = null!;
        private Button btnMoveUp = null!;
        private Button btnMoveDown = null!;
        private Button btnStartSelected = null!;
        private Button btnStopAll = null!;
        private Button btnToggleAllVisibility = null!;
        private ListView lvwBots = null!;
    }

    public class BotInstance
    {
        public string Name { get; set; } = string.Empty;
        public Process Process { get; set; }
        public DateTime StartTime { get; set; }
        public int ProcessId 
        { 
            get { return Process?.Id ?? 0; }
            set { /* Setter for deserialization */ }
        }
        public bool IsHidden { get; set; }
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;
        
        public string DisplayStatus
        {
            get
            {
                if (Process == null || Process.HasExited)
                    return "Kapalı";
                    
                return "Çalışıyor";
            }
        }
    }

    public class BotState
    {
        public string Name { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsHidden { get; set; }
        public long WindowHandleValue { get; set; } // Store as long instead of IntPtr for serialization
    }
    
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public bool StartCl { get; set; } = false; // Clientli başlat
        public bool StartCls { get; set; } = false; // Clientsiz başlat
    }

    public static class NativeMethods
    {
        #region Constants
        
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE = 9;
        public const int SW_NORMAL = 1;
        public const int SW_FORCEMINIMIZE = 11;
        
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_HIDE = 0xF040;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int GWL_STYLE = -16;
        private const long WS_VISIBLE = 0x10000000L;
        
        private const int MAXTITLELEN = 255;
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        #endregion
        
        #region Imports
        
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        private static extern long GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern long SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Process ID'ye göre ana pencereyi bul
        /// </summary>
        public static IntPtr FindMainWindow(int processId)
        {
            IntPtr result = IntPtr.Zero;
            
            // Ana pencereleri bulup process ID ile eşleştir
            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                // Görünür pencerelere bak
                if (!IsWindowVisible(hWnd))
                    return true;
                
                // Process ID'sini kontrol et
                GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                if (windowProcessId != processId)
                    return true;
                
                // Pencere başlığını kontrol et
                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return true;
                
                var sb = new StringBuilder(length + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();
                
                // RSBot ana penceresi olup olmadığını kontrol et
                if (title.Contains("RSBot") || title.Contains("Silkroad") || title.Contains("SRO"))
                {
                    // Ana pencereyi bulduk
                    result = hWnd;
                    return false; // Aramayı durdur
                }
                
                return true;
            }, IntPtr.Zero);
            
            return result;
        }
        
        /// <summary>
        /// WM_SYSCOMMAND mesajı göndererek pencereyi gizle
        /// </summary>
        public static bool HideWindowWithMessage(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            
            try
            {
                IntPtr result = new IntPtr(SendMessage(hWnd, WM_SYSCOMMAND, new IntPtr(SC_HIDE), IntPtr.Zero));
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Alternatif pencere gizleme yöntemi (WS_VISIBLE style'ı kaldır)
        /// </summary>
        public static bool HideWindowAlternative(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            
            try
            {
                // Mevcut style'ı al
                long style = GetWindowLong(hWnd, GWL_STYLE);
                
                // WS_VISIBLE'ı kaldır
                SetWindowLong(hWnd, GWL_STYLE, style & ~WS_VISIBLE);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
    }
}

