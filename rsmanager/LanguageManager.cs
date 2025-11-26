using System.Collections.Generic;

namespace RSBotManager
{
    /// <summary>
    /// Çoklu dil desteği sağlayan sınıf
    /// </summary>
    public static class LanguageManager
    {
        // Mevcut dil
        public static string CurrentLanguage { get; set; } = "Türkçe";
        
        // Desteklenen diller
        public static readonly List<string> SupportedLanguages = new List<string>
        {
            "Türkçe",
            "English",
            "Русский",
            "Deutsch"
        };
        
        // Dil çevirileri - Her UI elementi için tüm dillerde karşılıklar
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
            // Form başlığı
            { "FormTitle", new Dictionary<string, string>
                {
                    { "Türkçe", "RSBot Manager" },
                    { "English", "RSBot Manager" },
                    { "Русский", "RSBot Manager" },
                    { "Deutsch", "RSBot Manager" }
                }
            },
            
            // Sol panel - Profil listesi
            { "ProfileList", new Dictionary<string, string>
                {
                    { "Türkçe", "Profil Listesi" },
                    { "English", "Profile List" },
                    { "Русский", "Список профилей" },
                    { "Deutsch", "Profilliste" }
                }
            },
            
            // Butonlar
            { "Add", new Dictionary<string, string>
                {
                    { "Türkçe", "Ekle" },
                    { "English", "Add" },
                    { "Русский", "Добавить" },
                    { "Deutsch", "Hinzufügen" }
                }
            },
            { "Remove", new Dictionary<string, string>
                {
                    { "Türkçe", "Çıkar" },
                    { "English", "Remove" },
                    { "Русский", "Удалить" },
                    { "Deutsch", "Entfernen" }
                }
            },
            
            // Sağ panel - Ayarlar
            { "RSBotPath", new Dictionary<string, string>
                {
                    { "Türkçe", "RSBot Yolu:" },
                    { "English", "RSBot Path:" },
                    { "Русский", "Путь к RSBot:" },
                    { "Deutsch", "RSBot-Pfad:" }
                }
            },
            { "Browse", new Dictionary<string, string>
                {
                    { "Türkçe", "Gözat" },
                    { "English", "Browse" },
                    { "Русский", "Обзор" },
                    { "Deutsch", "Durchsuchen" }
                }
            },
            { "StartDelay", new Dictionary<string, string>
                {
                    { "Türkçe", "Başlatma Aralığı:" },
                    { "English", "Start Interval:" },
                    { "Русский", "Интервал запуска:" },
                    { "Deutsch", "Startintervall:" }
                }
            },
            { "Seconds", new Dictionary<string, string>
                {
                    { "Türkçe", "saniye" },
                    { "English", "seconds" },
                    { "Русский", "секунд" },
                    { "Deutsch", "Sekunden" }
                }
            },
            
            // Kontrol butonları
            { "StartSelected", new Dictionary<string, string>
                {
                    { "Türkçe", "▶ Seçili Profili Başlat" },
                    { "English", "▶ Start Selected Profile" },
                    { "Русский", "▶ Запустить выбранный профиль" },
                    { "Deutsch", "▶ Ausgewähltes Profil starten" }
                }
            },
            { "Stop", new Dictionary<string, string>
                {
                    { "Türkçe", "⏹ Durdur" },
                    { "English", "⏹ Stop" },
                    { "Русский", "⏹ Остановить" },
                    { "Deutsch", "⏹ Stoppen" }
                }
            },
            { "HideShow", new Dictionary<string, string>
                {
                    { "Türkçe", "👁 Gizle/Göster" },
                    { "English", "👁 Hide/Show" },
                    { "Русский", "👁 Скрыть/Показать" },
                    { "Deutsch", "👁 Ausblenden/Anzeigen" }
                }
            },
            { "Hide", new Dictionary<string, string>
                {
                    { "Türkçe", "👁 Gizle" },
                    { "English", "👁 Hide" },
                    { "Русский", "👁 Скрыть" },
                    { "Deutsch", "👁 Ausblenden" }
                }
            },
            { "Show", new Dictionary<string, string>
                {
                    { "Türkçe", "👁 Göster" },
                    { "English", "👁 Show" },
                    { "Русский", "👁 Показать" },
                    { "Deutsch", "👁 Anzeigen" }
                }
            },
            { "StartAll", new Dictionary<string, string>
                {
                    { "Türkçe", "▶ Tümünü Başlat" },
                    { "English", "▶ Start All" },
                    { "Русский", "▶ Запустить все" },
                    { "Deutsch", "▶ Alle starten" }
                }
            },
            { "StopAll", new Dictionary<string, string>
                {
                    { "Türkçe", "⏹ Tümünü Durdur" },
                    { "English", "⏹ Stop All" },
                    { "Русский", "⏹ Остановить все" },
                    { "Deutsch", "⏹ Alle stoppen" }
                }
            },
            { "HideAll", new Dictionary<string, string>
                {
                    { "Türkçe", "👁 Tümünü Gizle" },
                    { "English", "👁 Hide All" },
                    { "Русский", "👁 Скрыть все" },
                    { "Deutsch", "👁 Alle ausblenden" }
                }
            },
            { "ShowAll", new Dictionary<string, string>
                {
                    { "Türkçe", "👁 Tümünü Göster" },
                    { "English", "👁 Show All" },
                    { "Русский", "👁 Показать все" },
                    { "Deutsch", "👁 Alle anzeigen" }
                }
            },
            
            // ListView kolonları
            { "ProfileName", new Dictionary<string, string>
                {
                    { "Türkçe", "Profil Adı" },
                    { "English", "Profile Name" },
                    { "Русский", "Имя профиля" },
                    { "Deutsch", "Profilname" }
                }
            },
            { "PID", new Dictionary<string, string>
                {
                    { "Türkçe", "PID" },
                    { "English", "PID" },
                    { "Русский", "PID" },
                    { "Deutsch", "PID" }
                }
            },
            { "Status", new Dictionary<string, string>
                {
                    { "Türkçe", "Durum" },
                    { "English", "Status" },
                    { "Русский", "Статус" },
                    { "Deutsch", "Status" }
                }
            },
            { "Display", new Dictionary<string, string>
                {
                    { "Türkçe", "Görüntü" },
                    { "English", "Display" },
                    { "Русский", "Отображение" },
                    { "Deutsch", "Anzeige" }
                }
            },
            
            // Durum metinleri
            { "Running", new Dictionary<string, string>
                {
                    { "Türkçe", "✓ Çalışıyor" },
                    { "English", "✓ Running" },
                    { "Русский", "✓ Работает" },
                    { "Deutsch", "✓ Läuft" }
                }
            },
            { "Closed", new Dictionary<string, string>
                {
                    { "Türkçe", "✘ Kapalı" },
                    { "English", "✘ Closed" },
                    { "Русский", "✘ Закрыто" },
                    { "Deutsch", "✘ Geschlossen" }
                }
            },
            { "Hidden", new Dictionary<string, string>
                {
                    { "Türkçe", "👁️ Gizli" },
                    { "English", "👁️ Hidden" },
                    { "Русский", "👁️ Скрыто" },
                    { "Deutsch", "👁️ Ausgeblendet" }
                }
            },
            { "Visible", new Dictionary<string, string>
                {
                    { "Türkçe", "🖥️ Görünür" },
                    { "English", "🖥️ Visible" },
                    { "Русский", "🖥️ Видимо" },
                    { "Deutsch", "🖥️ Sichtbar" }
                }
            },
            
            // Status bar
            { "Ready", new Dictionary<string, string>
                {
                    { "Türkçe", "RSBot Manager Hazır" },
                    { "English", "RSBot Manager Ready" },
                    { "Русский", "RSBot Manager готов" },
                    { "Deutsch", "RSBot Manager bereit" }
                }
            },
            { "RunningBots", new Dictionary<string, string>
                {
                    { "Türkçe", "Çalışan Bot:" },
                    { "English", "Running Bots:" },
                    { "Русский", "Запущено ботов:" },
                    { "Deutsch", "Laufende Bots:" }
                }
            },
            { "Language", new Dictionary<string, string>
                {
                    { "Türkçe", "Dil:" },
                    { "English", "Language:" },
                    { "Русский", "Язык:" },
                    { "Deutsch", "Sprache:" }
                }
            },
            
            // Mesajlar
            { "SelectRSBotPath", new Dictionary<string, string>
                {
                    { "Türkçe", "Lütfen önce RSBot yolunu seçin." },
                    { "English", "Please select RSBot path first." },
                    { "Русский", "Пожалуйста, сначала выберите путь к RSBot." },
                    { "Deutsch", "Bitte wählen Sie zuerst den RSBot-Pfad aus." }
                }
            },
            { "NoProfilesFound", new Dictionary<string, string>
                {
                    { "Türkçe", "Başlatılacak profil bulunamadı." },
                    { "English", "No profiles found to start." },
                    { "Русский", "Не найдено профилей для запуска." },
                    { "Deutsch", "Keine Profile zum Starten gefunden." }
                }
            },
            { "AddNewProfile", new Dictionary<string, string>
                {
                    { "Türkçe", "Yeni Profil Ekle" },
                    { "English", "Add New Profile" },
                    { "Русский", "Добавить новый профиль" },
                    { "Deutsch", "Neues Profil hinzufügen" }
                }
            },
            { "ProfileNameLabel", new Dictionary<string, string>
                {
                    { "Türkçe", "Profil Adı:" },
                    { "English", "Profile Name:" },
                    { "Русский", "Имя профиля:" },
                    { "Deutsch", "Profilname:" }
                }
            },
            { "Save", new Dictionary<string, string>
                {
                    { "Türkçe", "Kaydet" },
                    { "English", "Save" },
                    { "Русский", "Сохранить" },
                    { "Deutsch", "Speichern" }
                }
            },
            { "Cancel", new Dictionary<string, string>
                {
                    { "Türkçe", "İptal" },
                    { "English", "Cancel" },
                    { "Русский", "Отмена" },
                    { "Deutsch", "Abbrechen" }
                }
            },
            { "Warning", new Dictionary<string, string>
                {
                    { "Türkçe", "Uyarı" },
                    { "English", "Warning" },
                    { "Русский", "Предупреждение" },
                    { "Deutsch", "Warnung" }
                }
            },
            { "Error", new Dictionary<string, string>
                {
                    { "Türkçe", "Hata" },
                    { "English", "Error" },
                    { "Русский", "Ошибка" },
                    { "Deutsch", "Fehler" }
                }
            },
            { "Info", new Dictionary<string, string>
                {
                    { "Türkçe", "Bilgi" },
                    { "English", "Info" },
                    { "Русский", "Информация" },
                    { "Deutsch", "Info" }
                }
            },
            { "EnterProfileName", new Dictionary<string, string>
                {
                    { "Türkçe", "Lütfen bir profil adı girin." },
                    { "English", "Please enter a profile name." },
                    { "Русский", "Пожалуйста, введите имя профиля." },
                    { "Deutsch", "Bitte geben Sie einen Profilnamen ein." }
                }
            },
            { "ProfileAlreadyExists", new Dictionary<string, string>
                {
                    { "Türkçe", "'{0}' isimli profil zaten kayıtlı." },
                    { "English", "Profile '{0}' already exists." },
                    { "Русский", "Профиль '{0}' уже существует." },
                    { "Deutsch", "Profil '{0}' existiert bereits." }
                }
            },
            { "ProfileSaved", new Dictionary<string, string>
                {
                    { "Türkçe", "'{0}' profili kaydedildi." },
                    { "English", "Profile '{0}' saved." },
                    { "Русский", "Профиль '{0}' сохранен." },
                    { "Deutsch", "Profil '{0}' gespeichert." }
                }
            },
            { "DeleteProfile", new Dictionary<string, string>
                {
                    { "Türkçe", "Profil Silme" },
                    { "English", "Delete Profile" },
                    { "Русский", "Удалить профиль" },
                    { "Deutsch", "Profil löschen" }
                }
            },
            { "ConfirmDeleteProfile", new Dictionary<string, string>
                {
                    { "Türkçe", "'{0}' profilini silmek istediğinizden emin misiniz?" },
                    { "English", "Are you sure you want to delete profile '{0}'?" },
                    { "Русский", "Вы уверены, что хотите удалить профиль '{0}'?" },
                    { "Deutsch", "Sind Sie sicher, dass Sie Profil '{0}' löschen möchten?" }
                }
            },
            { "AlreadyRunning", new Dictionary<string, string>
                {
                    { "Türkçe", "'{0}' zaten çalışıyor." },
                    { "English", "'{0}' is already running." },
                    { "Русский", "'{0}' уже запущен." },
                    { "Deutsch", "'{0}' läuft bereits." }
                }
            },
            { "BotsStarting", new Dictionary<string, string>
                {
                    { "Türkçe", "Botlar başlatılıyor..." },
                    { "English", "Starting bots..." },
                    { "Русский", "Запуск ботов..." },
                    { "Deutsch", "Bots werden gestartet..." }
                }
            },
            { "AllBotsStarted", new Dictionary<string, string>
                {
                    { "Türkçe", "Tüm botlar başlatıldı!" },
                    { "English", "All bots started!" },
                    { "Русский", "Все боты запущены!" },
                    { "Deutsch", "Alle Bots gestartet!" }
                }
            },
            { "StartingProfile", new Dictionary<string, string>
                {
                    { "Türkçe", "Başlatılıyor: {0} ({1}/{2})" },
                    { "English", "Starting: {0} ({1}/{2})" },
                    { "Русский", "Запуск: {0} ({1}/{2})" },
                    { "Deutsch", "Start: {0} ({1}/{2})" }
                }
            },
            { "NextBotIn", new Dictionary<string, string>
                {
                    { "Türkçe", "Sonraki bot {0} saniye içinde başlatılacak..." },
                    { "English", "Next bot will start in {0} seconds..." },
                    { "Русский", "Следующий бот запустится через {0} секунд..." },
                    { "Deutsch", "Nächster Bot startet in {0} Sekunden..." }
                }
            },
            { "NoRunningBots", new Dictionary<string, string>
                {
                    { "Türkçe", "Çalışan bot bulunamadı." },
                    { "English", "No running bots found." },
                    { "Русский", "Запущенных ботов не найдено." },
                    { "Deutsch", "Keine laufenden Bots gefunden." }
                }
            },
            { "ConfirmStopAll", new Dictionary<string, string>
                {
                    { "Türkçe", "{0} adet çalışan botu durdurmak istediğinizden emin misiniz?" },
                    { "English", "Are you sure you want to stop {0} running bots?" },
                    { "Русский", "Вы уверены, что хотите остановить {0} работающих ботов?" },
                    { "Deutsch", "Sind Sie sicher, dass Sie {0} laufende Bots stoppen möchten?" }
                }
            },
            { "BotsStopped", new Dictionary<string, string>
                {
                    { "Türkçe", "{0} bot durduruldu." },
                    { "English", "{0} bots stopped." },
                    { "Русский", "{0} ботов остановлено." },
                    { "Deutsch", "{0} Bots gestoppt." }
                }
            },
            { "ResetCommandFormat", new Dictionary<string, string>
                {
                    { "Türkçe", "Komut Formatını Sıfırla" },
                    { "English", "Reset Command Format" },
                    { "Русский", "Сбросить формат команды" },
                    { "Deutsch", "Befehlsformat zurücksetzen" }
                }
            },
            { "CommandFormatReset", new Dictionary<string, string>
                {
                    { "Türkçe", "Komut formatı sıfırlandı. Bir sonraki bot başlatıldığında format seçimi istenecek." },
                    { "English", "Command format reset. Format selection will be requested on next bot start." },
                    { "Русский", "Формат команды сброшен. При следующем запуске бота будет запрошен выбор формата." },
                    { "Deutsch", "Befehlsformat zurückgesetzt. Bei nächstem Bot-Start wird Formatauswahl angefordert." }
                }
            },
            { "Edit", new Dictionary<string, string>
                {
                    { "Türkçe", "Düzenle" },
                    { "English", "Edit" },
                    { "Русский", "Редактировать" },
                    { "Deutsch", "Bearbeiten" }
                }
            },
            { "EditProfile", new Dictionary<string, string>
                {
                    { "Türkçe", "Profil Düzenle" },
                    { "English", "Edit Profile" },
                    { "Русский", "Редактировать профиль" },
                    { "Deutsch", "Profil bearbeiten" }
                }
            },
            { "StartWithClient", new Dictionary<string, string>
                {
                    { "Türkçe", "Clientli Başlat" },
                    { "English", "Start with Client" },
                    { "Русский", "Запустить с клиентом" },
                    { "Deutsch", "Mit Client starten" }
                }
            },
            { "StartWithoutClient", new Dictionary<string, string>
                {
                    { "Türkçe", "Clientsiz Başlat" },
                    { "English", "Start without Client" },
                    { "Русский", "Запустить без клиента" },
                    { "Deutsch", "Ohne Client starten" }
                }
            },
            { "ProfileUpdated", new Dictionary<string, string>
                {
                    { "Türkçe", "'{0}' profili güncellendi." },
                    { "English", "Profile '{0}' updated." },
                    { "Русский", "Профиль '{0}' обновлен." },
                    { "Deutsch", "Profil '{0}' aktualisiert." }
                }
            }
        };
        
        /// <summary>
        /// Verilen anahtar için geçerli dildeki karşılığı döndürür
        /// </summary>
        public static string GetText(string key)
        {
            if (Translations.ContainsKey(key) && Translations[key].ContainsKey(CurrentLanguage))
            {
                return Translations[key][CurrentLanguage];
            }
            
            // Eğer çeviri bulunamazsa Türkçe döndür
            if (Translations.ContainsKey(key) && Translations[key].ContainsKey("Türkçe"))
            {
                return Translations[key]["Türkçe"];
            }
            
            // Hiçbir şey bulunamazsa anahtarı döndür
            return key;
        }
        
        /// <summary>
        /// Formatlı metin için çeviri döndürür
        /// </summary>
        public static string GetText(string key, params object[] args)
        {
            string text = GetText(key);
            try
            {
                return string.Format(text, args);
            }
            catch
            {
                return text;
            }
        }
    }
}

