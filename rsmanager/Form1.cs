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
        private List<string> savedProfiles = new List<string>(); // Kaydedilen profil adları
        private string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private string botsStateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bots_state.json");
        private string profilesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");
        private string commandFormat = "direct"; // Default command format: "direct", "name", or "profile"
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel botCountLabel;
        private System.Windows.Forms.Timer refreshTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
            LoadSettings();
            LoadRunningBots();
            RefreshBotList();
            SetupStatusBar();
        }

        private void SetupStatusBar()
        {
            // Status bar at the bottom
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;
            statusStrip.BackColor = Color.FromArgb(240, 240, 240);
            
            statusLabel = new ToolStripStatusLabel("RSBot Manager Hazır");
            statusLabel.Spring = true;
            
            botCountLabel = new ToolStripStatusLabel("Çalışan Bot: 0");
            botCountLabel.Alignment = ToolStripItemAlignment.Right;
            
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
            botCountLabel.Text = $"Çalışan Bot: {runningBots}";
            RefreshBotList();
        }

        private void InitializeUI()
        {
            // Form settings
            this.Text = "RSBot Manager";
            this.MinimumSize = new Size(800, 450);
            this.BackColor = Color.FromArgb(245, 245, 250); // Daha açık ve modern bir arka plan
            this.Font = new Font("Segoe UI", 9F); // Modern font
            this.FormClosing += Form1_FormClosing;
            
            // Status bar ekleme
            var statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(240, 240, 245),
                SizingGrip = false
            };
            
            var statusLabel = new ToolStripStatusLabel("RSBot Manager Hazır")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            var botCountLabel = new ToolStripStatusLabel("Çalışan Bot: 0")
            {
                Alignment = ToolStripItemAlignment.Right
            };
            
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(botCountLabel);
            this.Controls.Add(statusStrip);
            
            // Auto-refresh timer
            var refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000,
                Enabled = true
            };
            
            refreshTimer.Tick += (s, e) => {
                int runningBots = bots.Count(b => b.Process != null && !b.Process.HasExited);
                botCountLabel.Text = $"Çalışan Bot: {runningBots}";
                RefreshBotList();
            };
            
            // Create context menu for changing command format
            var contextMenuStrip = new ContextMenuStrip();
            var resetCommandFormatItem = contextMenuStrip.Items.Add("Komut Formatını Sıfırla");
            resetCommandFormatItem.Click += (s, e) => 
            {
                commandFormat = "ask";
                SaveSettings();
                MessageBox.Show("Komut formatı sıfırlandı. Bir sonraki bot başlatıldığında format seçimi istenecek.", 
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Ana panel (2 bölmeli: üst kontroller ve alt liste)
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10),
                BackColor = Color.Transparent
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); // Üst panel için sabit yükseklik
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Alt panel için kalan alan
            this.Controls.Add(mainPanel);

            // Üst panel (ayarlar ve butonlar için)
            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Etiketler için genişlik
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // Giriş alanları için genişlik
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // Butonlar için genişlik
            
            // Satır yükseklikleri
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); 
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // Son satır biraz daha yüksek

            // Etiketler
            var lblRSBotPath = new Label { 
                Text = "RSBot Yolu:", 
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Padding = new Padding(5, 0, 0, 0),
                Font = new Font(this.Font, FontStyle.Regular)
            };
            var lblProfileName = new Label { 
                Text = "Profil Adı:", 
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Padding = new Padding(5, 0, 0, 0),
                Font = new Font(this.Font, FontStyle.Regular)
            };
            var lblSavedProfiles = new Label { 
                Text = "Kayıtlı Profiller:", 
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Padding = new Padding(5, 0, 0, 0),
                Font = new Font(this.Font, FontStyle.Regular)
            };
            
            // Metin kutuları ve ComboBox
            txtRSBotPath = new TextBox { 
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 5, 5, 0),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            txtRSBotPath.ContextMenuStrip = contextMenuStrip;
            
            txtProfileName = new TextBox { 
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 5, 5, 0),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            
            cmbSavedProfiles = new ComboBox { 
                Dock = DockStyle.Fill, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 5, 5, 0),
                FlatStyle = FlatStyle.Popup,
                BackColor = Color.White
            };
            cmbSavedProfiles.SelectedIndexChanged += CmbSavedProfiles_SelectedIndexChanged;
            
            // Butonlar
            btnBrowse = new Button { 
                Text = "Gözat", 
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 4, 0, 0),
                FlatStyle = FlatStyle.System,
                UseVisualStyleBackColor = true
            };
            
            btnStart = new Button { 
                Text = "Başlat", 
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 4, 0, 0),
                FlatStyle = FlatStyle.System,
                UseVisualStyleBackColor = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            
            // Profil yönetim butonları paneli
            var profileButtonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(5, 4, 0, 0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            profileButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            profileButtonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            
            btnAddProfile = new Button { 
                Text = "Ekle", 
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 2, 0),
                FlatStyle = FlatStyle.System,
                UseVisualStyleBackColor = true
            };
            
            btnRemoveProfile = new Button { 
                Text = "Sil", 
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 0, 0, 0),
                FlatStyle = FlatStyle.System,
                UseVisualStyleBackColor = true
            };
            
            profileButtonPanel.Controls.Add(btnAddProfile, 0, 0);
            profileButtonPanel.Controls.Add(btnRemoveProfile, 1, 0);
            
            // Alt buton paneli (Durdur, Yenile, Gizle/Göster)
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4F));
              btnStop = new Button { 
                Text = "Durdur", 
                Dock = DockStyle.Fill, 
                Margin = new Padding(0, 0, 2, 0),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 53, 69),
                Font = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Bold),
                Padding = new Padding(10, 3, 10, 3),
                Image = SystemIcons.Error.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            
            btnRefresh = new Button { 
                Text = "Yenile", 
                Dock = DockStyle.Fill, 
                Margin = new Padding(2, 0, 2, 0),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(13, 110, 253),
                Font = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Regular),
                Padding = new Padding(10, 3, 10, 3),
                Image = SystemIcons.Information.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            
            btnHideShow = new Button { 
                Text = "Gizle/Göster", 
                Dock = DockStyle.Fill, 
                Margin = new Padding(2, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 167, 69),
                Font = new Font(this.Font.FontFamily, this.Font.Size, FontStyle.Regular),
                Padding = new Padding(10, 3, 10, 3),
                Image = SystemIcons.Shield.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText
            };
            
            buttonPanel.Controls.Add(btnStop, 0, 0);
            buttonPanel.Controls.Add(btnRefresh, 1, 0);
            buttonPanel.Controls.Add(btnHideShow, 2, 0);
            
            // Kontrolleri panellere ekleme
            topPanel.Controls.Add(lblRSBotPath, 0, 0);
            topPanel.Controls.Add(txtRSBotPath, 1, 0);
            topPanel.Controls.Add(btnBrowse, 2, 0);
            topPanel.Controls.Add(lblProfileName, 0, 1);
            topPanel.Controls.Add(txtProfileName, 1, 1);
            topPanel.Controls.Add(btnStart, 2, 1);
            topPanel.Controls.Add(lblSavedProfiles, 0, 2);
            topPanel.Controls.Add(cmbSavedProfiles, 1, 2);
            topPanel.Controls.Add(profileButtonPanel, 2, 2);
            topPanel.Controls.Add(buttonPanel, 1, 3);
            
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
            
            lvwBots.Columns.Add("Profil Adı", 220);  // Daha geniş
            lvwBots.Columns.Add("PID", 80);
            lvwBots.Columns.Add("Durum", 120);      // Daha geniş
            lvwBots.Columns.Add("Görüntü", 120);    // Daha geniş
            
            // Ana panele ekleme
            mainPanel.Controls.Add(topPanel, 0, 0);
            mainPanel.Controls.Add(lvwBots, 0, 1);
            
            // Olayları kaydet
            btnBrowse.Click += btnBrowse_Click;
            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            btnRefresh.Click += btnRefresh_Click;
            btnHideShow.Click += btnHideShow_Click;
            lvwBots.SelectedIndexChanged += lvwBots_SelectedIndexChanged;
            btnAddProfile.Click += BtnAddProfile_Click;
            btnRemoveProfile.Click += BtnRemoveProfile_Click;
            
            // Başlangıçta butonları devre dışı bırak
            btnStop.Enabled = false;
            btnHideShow.Enabled = false;
            btnRemoveProfile.Enabled = false;
            
            // Kayıtlı profilleri yükle
            LoadProfiles();
        }

        private void CmbSavedProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemoveProfile.Enabled = cmbSavedProfiles.SelectedIndex >= 0;
            
            if (cmbSavedProfiles.SelectedIndex >= 0)
            {
                txtProfileName.Text = cmbSavedProfiles.SelectedItem.ToString();
            }
        }
        
        private void BtnAddProfile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProfileName.Text))
            {
                MessageBox.Show("Lütfen bir profil adı girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string profileName = txtProfileName.Text.Trim();
            
            // Aynı isimde profil varsa uyar
            if (savedProfiles.Contains(profileName))
            {
                MessageBox.Show($"'{profileName}' isimli profil zaten kayıtlı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            // Profili kaydet
            savedProfiles.Add(profileName);
            cmbSavedProfiles.Items.Add(profileName);
            cmbSavedProfiles.SelectedItem = profileName;
            SaveProfiles();
            
            MessageBox.Show($"'{profileName}' profili kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void BtnRemoveProfile_Click(object sender, EventArgs e)
        {
            if (cmbSavedProfiles.SelectedIndex < 0) return;
            
            string profileName = cmbSavedProfiles.SelectedItem.ToString();
            
            if (MessageBox.Show($"'{profileName}' profilini silmek istediğinizden emin misiniz?", "Profil Silme", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                savedProfiles.Remove(profileName);
                cmbSavedProfiles.Items.Remove(profileName);
                SaveProfiles();
                
                if (cmbSavedProfiles.Items.Count > 0)
                    cmbSavedProfiles.SelectedIndex = 0;
                else
                    btnRemoveProfile.Enabled = false;
            }
        }
        
        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(profilesFilePath))
                {
                    string json = File.ReadAllText(profilesFilePath);
                    var profiles = JsonSerializer.Deserialize<List<string>>(json);
                    
                    if (profiles != null)
                    {
                        savedProfiles = profiles;
                        cmbSavedProfiles.Items.Clear();
                        
                        foreach (var profile in savedProfiles)
                        {
                            cmbSavedProfiles.Items.Add(profile);
                        }
                        
                        if (cmbSavedProfiles.Items.Count > 0)
                            cmbSavedProfiles.SelectedIndex = 0;
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
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRSBotPath.Text))
            {
                MessageBox.Show("Lütfen önce RSBot yolunu seçin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtRSBotPath.Focus();
                txtRSBotPath.BackColor = Color.FromArgb(255, 230, 230); // Highlight error
                return;
            }
            else
            {
                txtRSBotPath.BackColor = SystemColors.Window;
            }

            if (string.IsNullOrWhiteSpace(txtProfileName.Text))
            {
                MessageBox.Show("Lütfen bir profil adı girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtProfileName.Focus();
                txtProfileName.BackColor = Color.FromArgb(255, 230, 230); // Highlight error
                return;
            }
            else
            {
                txtProfileName.BackColor = SystemColors.Window;
            }

            string profileName = txtProfileName.Text.Trim();
            
            // Check if a bot with this name is already running
            var existingBot = bots.Find(b => b.Name == profileName && b.Process != null && !b.Process.HasExited);
            if (existingBot != null)
            {
                MessageBox.Show($"'{profileName}' adlı bot zaten çalışıyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                // Select the existing bot in the list
                for (int i = 0; i < lvwBots.Items.Count; i++)
                {
                    if (lvwBots.Items[i].Text == profileName)
                    {
                        lvwBots.Items[i].Selected = true;
                        lvwBots.EnsureVisible(i);
                        break;
                    }
                }
                
                return;
            }
            
            try
            {
                // Show wait cursor
                this.Cursor = Cursors.WaitCursor;
                btnStart.Enabled = false;
                
                // Use the saved command format or ask user
                string arguments = string.Empty;
                
                if (commandFormat == null || commandFormat == "ask")
                {
                    // Display a selection dialog for command format
                    DialogResult result = MessageBox.Show(
                        $"Profil adı \"{profileName}\" için hangi komut formatını kullanmak istersiniz?\n\n" +
                        "Evet = Doğrudan profil adını kullan (\"profil\")\n" + 
                        "Hayır = --name parametresini kullan (--name \"profil\")\n" +
                        "İptal = --profile parametresini kullan (--profile \"profil\")", 
                        "Komut Formatı Seçimi", 
                        MessageBoxButtons.YesNoCancel, 
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        arguments = $"\"{profileName}\""; // Just the profile name
                        commandFormat = "direct";
                    }
                    else if (result == DialogResult.No)
                    {
                        arguments = $"--name \"{profileName}\""; // --name parameter
                        commandFormat = "name";
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        arguments = $"--profile \"{profileName}\""; // --profile parameter
                        commandFormat = "profile";
                    }
                    else
                    {
                        this.Cursor = Cursors.Default;
                        btnStart.Enabled = true;
                        return; // User closed the dialog
                    }
                        
                    // Save the chosen format
                    SaveSettings();
                }
                else
                {
                    // Use the saved format
                    if (commandFormat == "direct")
                        arguments = $"\"{profileName}\"";
                    else if (commandFormat == "name")
                        arguments = $"--name \"{profileName}\"";
                    else if (commandFormat == "profile")
                        arguments = $"--profile \"{profileName}\"";
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
                        Name = profileName,
                        Process = process,
                        StartTime = DateTime.Now,
                        IsHidden = false
                    };
                    
                    bots.Add(bot);
                    
                    // Try to find window handle after a short delay
                    Task.Run(async () =>
                    {
                        await Task.Delay(3000); // Wait for RSBot to initialize
                        if (!process.HasExited)
                        {
                            process.Refresh();
                            bot.WindowHandle = process.MainWindowHandle;
                            this.Invoke(new Action(() => { RefreshBotList(); }));
                        }
                    });
                    
                    RefreshBotList();
                    txtProfileName.Clear();
                    
                    // If this is a new profile, ask if user wants to save it
                    if (!savedProfiles.Contains(profileName) && 
                        MessageBox.Show($"'{profileName}' profilini kaydetmek ister misiniz?", 
                                       "Profili Kaydet", MessageBoxButtons.YesNo, 
                                       MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        savedProfiles.Add(profileName);
                        cmbSavedProfiles.Items.Add(profileName);
                        SaveProfiles();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bot başlatılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnStart.Enabled = true;
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

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshBotList();
        }

        private void lvwBots_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }
          private void UpdateButtonStates()
        {
            bool hasSelection = lvwBots.SelectedItems.Count > 0;
            btnStop.Enabled = hasSelection;
            btnHideShow.Enabled = hasSelection;
            
            // Update the hide/show button text based on the selection
            UpdateHideShowButtonText();
        }
        
        private void UpdateHideShowButtonText()
        {
            if (lvwBots.SelectedItems.Count == 0)
            {
                btnHideShow.Text = "Gizle/Göster";
                btnHideShow.BackColor = Color.FromArgb(108, 117, 125); // Gray for inactive
                btnHideShow.ForeColor = Color.White;
                btnHideShow.Image = SystemIcons.Shield.ToBitmap();
                return;
            }
            
            string selectedBotName = lvwBots.SelectedItems[0].Text;
            var botInstance = bots.Find(b => b.Name == selectedBotName);
            
            if (botInstance != null && botInstance.Process != null && !botInstance.Process.HasExited)
            {
                if (botInstance.IsHidden)
                {
                    btnHideShow.Text = "Göster";
                    btnHideShow.BackColor = Color.FromArgb(0, 123, 255); // Blue for Show
                    btnHideShow.ForeColor = Color.White;
                    btnHideShow.Image = SystemIcons.Application.ToBitmap();
                }
                else
                {
                    btnHideShow.Text = "Gizle";
                    btnHideShow.BackColor = Color.FromArgb(40, 167, 69); // Green for Hide
                    btnHideShow.ForeColor = Color.White;
                    btnHideShow.Image = SystemIcons.Shield.ToBitmap();
                }
            }
            else
            {
                btnHideShow.Text = "Gizle/Göster";
                btnHideShow.BackColor = Color.FromArgb(108, 117, 125); // Gray for inactive
                btnHideShow.ForeColor = Color.White;
                btnHideShow.Image = SystemIcons.Shield.ToBitmap();
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
                        
                        var statusItem = item.SubItems.Add(bot.Process.HasExited ? "Kapalı" : "Çalışıyor");
                        var displayItem = item.SubItems.Add(bot.IsHidden ? "Gizli" : "Görünür");
                        
                        // Bot durumuna göre renklendirme
                        if (bot.Process.HasExited)
                        {
                            // Kapalı bot - kırmızı
                            item.ForeColor = Color.Red;
                            item.BackColor = Color.FromArgb(255, 240, 240);
                            statusItem.Text = "✘ Kapalı";
                            statusItem.ForeColor = Color.Red;
                        }
                        else if (bot.IsHidden)
                        {
                            // Gizli bot - mavi
                            item.ForeColor = Color.DarkBlue;
                            item.BackColor = Color.FromArgb(240, 240, 255);
                            statusItem.Text = "✓ Çalışıyor";
                            statusItem.ForeColor = Color.Green;
                            displayItem.Text = "👁️ Gizli";
                            displayItem.ForeColor = Color.Blue;
                        }
                        else
                        {
                            // Çalışan ve görünen bot - yeşil
                            item.ForeColor = Color.DarkGreen;
                            item.BackColor = Color.FromArgb(240, 255, 240);
                            statusItem.Text = "✓ Çalışıyor";
                            statusItem.ForeColor = Color.Green;
                            displayItem.Text = "🖥️ Görünür";
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
                        
                        if (settings.TryGetValue("CommandFormat", out string format))
                        {
                            commandFormat = format;
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
                    { "CommandFormat", commandFormat }
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

        private void btnHideShow_Click(object sender, EventArgs e)
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
                            Console.WriteLine($"Kayıtlı RSBot penceresi bulundu: {title}");
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
                            Console.WriteLine($"MainWindow RSBot penceresi bulundu: {title}");
                        }
                    }
                }
                
                // 3. Son çare olarak FindMainWindow'u kullan
                if (handle == IntPtr.Zero) {
                    handle = NativeMethods.FindMainWindow(botInstance.Process.Id);
                    if (handle != IntPtr.Zero) {
                        int length = NativeMethods.GetWindowTextLength(handle);
                        if (length > 0) {
                            var sb = new StringBuilder(length + 1);
                            NativeMethods.GetWindowText(handle, sb, sb.Capacity);
                            Console.WriteLine($"FindMainWindow ile RSBot penceresi bulundu: {sb.ToString()}");
                        }
                    }
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
                    Console.WriteLine("RSBot penceresini gösterme işlemi başlatılıyor...");
                    
                    // Komut dizisi uygula (daha güvenilir)
                    // 1. İlk restore (minimize veya hide edilmiş olabilir)
                    bool step1 = NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                    System.Threading.Thread.Sleep(300);
                    
                    // 2. Sonra normal modda göster
                    bool step2 = NativeMethods.ShowWindow(handle, NativeMethods.SW_NORMAL);
                    System.Threading.Thread.Sleep(300);
                    
                    // 3. Sonra SHOW komutunu uygula
                    bool step3 = NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
                    System.Threading.Thread.Sleep(300);
                    
                    // 4. Son olarak pencereyi öne getir
                    NativeMethods.SetForegroundWindow(handle);
                    
                    // Bot durumunu güncelle
                    botInstance.IsHidden = false;
                    
                    // Sonucu kontrol et
                    bool nowVisible = NativeMethods.IsWindowVisible(handle);
                    Console.WriteLine($"Gösterme işlemi sonucu: {nowVisible} (adımlar: {step1}-{step2}-{step3})");
                    
                    if (!nowVisible) {
                        MessageBox.Show("RSBot penceresi gösterme işlemi başarısız oldu. Lütfen tekrar deneyin.");
                    }
                }
                else  // Görünürse gizle
                {
                    Console.WriteLine("RSBot penceresini gizleme işlemi başlatılıyor...");
                    
                    // En etkili gizleme yöntemini dene
                    // 1. Önce WM_SYSCOMMAND mesaj yöntemini dene
                    bool success = NativeMethods.HideWindowWithMessage(handle);
                    
                    if (!success || NativeMethods.IsWindowVisible(handle))
                    {
                        // 2. Doğrudan gizleme yöntemini dene
                        bool step1 = NativeMethods.ShowWindow(handle, NativeMethods.SW_MINIMIZE);
                        System.Threading.Thread.Sleep(300);
                        bool step2 = NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
                        
                        // 3. Başarısızsa style manipülasyonunu dene
                        if (NativeMethods.IsWindowVisible(handle)) 
                        {
                            bool altMethod = NativeMethods.HideWindowAlternative(handle);
                            
                            // 4. Hala başarısızsa son çare
                            if (!altMethod || NativeMethods.IsWindowVisible(handle)) 
                            {
                                NativeMethods.ShowWindow(handle, NativeMethods.SW_FORCEMINIMIZE);
                                Console.WriteLine("Son çare: FORCEMINIMIZE kullanıldı");
                            }
                        }
                    }
                    
                    // Her durumda gizli olarak işaretle
                    botInstance.IsHidden = true;
                    
                    // Sonucu kontrol et ve rapor et
                    bool stillVisible = NativeMethods.IsWindowVisible(handle);
                    Console.WriteLine($"Gizleme işlemi sonucu: {!stillVisible}");
                    
                    if (stillVisible) {
                        MessageBox.Show("RSBot penceresi tam olarak gizlenemedi. En azından minimize edildi.");
                    }
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
        }

        // Form controls
        private TextBox txtRSBotPath = null!;
        private TextBox txtProfileName = null!;
        private ComboBox cmbSavedProfiles = null!;
        private Button btnBrowse = null!;
        private Button btnStart = null!;
        private Button btnStop = null!;
        private Button btnRefresh = null!;
        private Button btnHideShow = null!;
        private Button btnAddProfile = null!;
        private Button btnRemoveProfile = null!;
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
