using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Rat6.App.Localization;

public sealed class AppStrings : INotifyPropertyChanged
{
    public static AppStrings Instance { get; } = new();

    public static IReadOnlyList<LanguageOption> AvailableLanguages { get; } =
    [
        new("en", "English"),
        new("ru", "Русский"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("zh", "中文"),
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Tables =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>
            {
                ["profile"] = "Profile",
                ["activate"] = "Activate",
                ["refresh"] = "Refresh",
                ["language"] = "Language",
                ["lighting"] = "Lighting",
                ["sensitivity"] = "Sensitivity",
                ["zone"] = "Zone",
                ["mode"] = "Mode",
                ["period"] = "Period",
                ["brightness"] = "Brightness",
                ["apply_lighting"] = "Apply lighting",
                ["apply_dpi"] = "Apply DPI",
                ["allow_write"] = "Allow writing to mouse",
                ["profile_tooltip"] = "Profile: mouse wheel or drag.",
                ["activate_tooltip"] = "Command 0x07: make the slot active.",
                ["all_zones"] = "All zones",
                ["zone_n"] = "Zone {0}",
                ["mode_solid"] = "Solid",
                ["mode_breathing"] = "Breathing",
                ["mode_rainbow"] = "Rainbow",
                ["dpi_stage"] = "Stage {0}",
                ["firmware"] = "firmware {0}",
                ["firmware_unknown"] = "firmware —",
                ["active_slot"] = "active slot {0} of {1}",
                ["active_slot_unknown"] = "active —",
                ["period_value"] = "{0} s",
                ["status_reading_profiles"] = "Reading profiles...",
                ["status_profiles_loaded"] = "Profiles read. Active slot {0}.",
                ["status_profile_read"] = "Slot {0} read.",
                ["status_slot_activated"] = "Slot {0} activated. Backup: {1}",
                ["status_dpi_written"] = "DPI written to slot {0}. Backup: {1}",
                ["status_led_written"] = "Lighting ({0}) written to slot {1}. Backup: {2}",
                ["status_device_not_found"] = "Device not found.",
                ["status_enable_write"] = "Enable \"Allow writing to mouse\".",
                ["status_read_failed"] = "Read failed: {0}",
                ["status_write_failed"] = "Write failed: {0}",
            },
            ["ru"] = new Dictionary<string, string>
            {
                ["profile"] = "Профиль",
                ["activate"] = "Активировать",
                ["refresh"] = "Обновить",
                ["language"] = "Язык",
                ["lighting"] = "Подсветка",
                ["sensitivity"] = "Чувствительность",
                ["zone"] = "Зона",
                ["mode"] = "Режим",
                ["period"] = "Период",
                ["brightness"] = "Яркость",
                ["apply_lighting"] = "Применить подсветку",
                ["apply_dpi"] = "Применить DPI",
                ["allow_write"] = "Разрешить запись в мышь",
                ["profile_tooltip"] = "Профиль: колесо мыши или перетаскивание.",
                ["activate_tooltip"] = "Команда 0x07: сделать слот активным.",
                ["all_zones"] = "Все зоны",
                ["zone_n"] = "Зона {0}",
                ["mode_solid"] = "Сплошной",
                ["mode_breathing"] = "Дыхание",
                ["mode_rainbow"] = "Радуга",
                ["dpi_stage"] = "Ступень {0}",
                ["firmware"] = "прошивка {0}",
                ["firmware_unknown"] = "прошивка —",
                ["active_slot"] = "активен слот {0} из {1}",
                ["active_slot_unknown"] = "активен —",
                ["period_value"] = "{0} с",
                ["status_reading_profiles"] = "Чтение профилей...",
                ["status_profiles_loaded"] = "Профили прочитаны. Активен слот {0}.",
                ["status_profile_read"] = "Слот {0} прочитан.",
                ["status_slot_activated"] = "Слот {0} активирован. Backup: {1}",
                ["status_dpi_written"] = "DPI записан в слот {0}. Backup: {1}",
                ["status_led_written"] = "Подсветка ({0}) записана в слот {1}. Backup: {2}",
                ["status_device_not_found"] = "Устройство не найдено.",
                ["status_enable_write"] = "Включите «Разрешить запись в мышь».",
                ["status_read_failed"] = "Ошибка чтения: {0}",
                ["status_write_failed"] = "Ошибка записи: {0}",
            },
            ["de"] = new Dictionary<string, string>
            {
                ["profile"] = "Profil",
                ["activate"] = "Aktivieren",
                ["refresh"] = "Aktualisieren",
                ["language"] = "Sprache",
                ["lighting"] = "Beleuchtung",
                ["sensitivity"] = "Empfindlichkeit",
                ["zone"] = "Zone",
                ["mode"] = "Modus",
                ["period"] = "Periode",
                ["brightness"] = "Helligkeit",
                ["apply_lighting"] = "Beleuchtung anwenden",
                ["apply_dpi"] = "DPI anwenden",
                ["allow_write"] = "Schreiben auf Maus erlauben",
                ["profile_tooltip"] = "Profil: Mausrad oder Ziehen.",
                ["activate_tooltip"] = "Befehl 0x07: Slot aktivieren.",
                ["all_zones"] = "Alle Zonen",
                ["zone_n"] = "Zone {0}",
                ["mode_solid"] = "Dauerlicht",
                ["mode_breathing"] = "Atmen",
                ["mode_rainbow"] = "Regenbogen",
                ["dpi_stage"] = "Stufe {0}",
                ["firmware"] = "Firmware {0}",
                ["firmware_unknown"] = "Firmware —",
                ["active_slot"] = "aktiver Slot {0} von {1}",
                ["active_slot_unknown"] = "aktiv —",
                ["period_value"] = "{0} s",
                ["status_reading_profiles"] = "Profile werden gelesen...",
                ["status_profiles_loaded"] = "Profile gelesen. Aktiver Slot {0}.",
                ["status_profile_read"] = "Slot {0} gelesen.",
                ["status_slot_activated"] = "Slot {0} aktiviert. Backup: {1}",
                ["status_dpi_written"] = "DPI in Slot {0} geschrieben. Backup: {1}",
                ["status_led_written"] = "Beleuchtung ({0}) in Slot {1} geschrieben. Backup: {2}",
                ["status_device_not_found"] = "Gerät nicht gefunden.",
                ["status_enable_write"] = "„Schreiben auf Maus erlauben“ aktivieren.",
                ["status_read_failed"] = "Lesen fehlgeschlagen: {0}",
                ["status_write_failed"] = "Schreiben fehlgeschlagen: {0}",
            },
            ["fr"] = new Dictionary<string, string>
            {
                ["profile"] = "Profil",
                ["activate"] = "Activer",
                ["refresh"] = "Actualiser",
                ["language"] = "Langue",
                ["lighting"] = "Éclairage",
                ["sensitivity"] = "Sensibilité",
                ["zone"] = "Zone",
                ["mode"] = "Mode",
                ["period"] = "Période",
                ["brightness"] = "Luminosité",
                ["apply_lighting"] = "Appliquer l'éclairage",
                ["apply_dpi"] = "Appliquer le DPI",
                ["allow_write"] = "Autoriser l'écriture sur la souris",
                ["profile_tooltip"] = "Profil : molette ou glisser.",
                ["activate_tooltip"] = "Commande 0x07 : activer l'emplacement.",
                ["all_zones"] = "Toutes les zones",
                ["zone_n"] = "Zone {0}",
                ["mode_solid"] = "Fixe",
                ["mode_breathing"] = "Respiration",
                ["mode_rainbow"] = "Arc-en-ciel",
                ["dpi_stage"] = "Niveau {0}",
                ["firmware"] = "firmware {0}",
                ["firmware_unknown"] = "firmware —",
                ["active_slot"] = "emplacement actif {0} sur {1}",
                ["active_slot_unknown"] = "actif —",
                ["period_value"] = "{0} s",
                ["status_reading_profiles"] = "Lecture des profils...",
                ["status_profiles_loaded"] = "Profils lus. Emplacement actif {0}.",
                ["status_profile_read"] = "Emplacement {0} lu.",
                ["status_slot_activated"] = "Emplacement {0} activé. Sauvegarde : {1}",
                ["status_dpi_written"] = "DPI écrit dans l'emplacement {0}. Sauvegarde : {1}",
                ["status_led_written"] = "Éclairage ({0}) écrit dans l'emplacement {1}. Sauvegarde : {2}",
                ["status_device_not_found"] = "Appareil introuvable.",
                ["status_enable_write"] = "Activez « Autoriser l'écriture sur la souris ».",
                ["status_read_failed"] = "Échec de lecture : {0}",
                ["status_write_failed"] = "Échec d'écriture : {0}",
            },
            ["zh"] = new Dictionary<string, string>
            {
                ["profile"] = "配置文件",
                ["activate"] = "激活",
                ["refresh"] = "刷新",
                ["language"] = "语言",
                ["lighting"] = "灯光",
                ["sensitivity"] = "灵敏度",
                ["zone"] = "区域",
                ["mode"] = "模式",
                ["period"] = "周期",
                ["brightness"] = "亮度",
                ["apply_lighting"] = "应用灯光",
                ["apply_dpi"] = "应用 DPI",
                ["allow_write"] = "允许写入鼠标",
                ["profile_tooltip"] = "配置文件：滚轮或拖动。",
                ["activate_tooltip"] = "命令 0x07：激活该配置。",
                ["all_zones"] = "所有区域",
                ["zone_n"] = "区域 {0}",
                ["mode_solid"] = "常亮",
                ["mode_breathing"] = "呼吸",
                ["mode_rainbow"] = "彩虹",
                ["dpi_stage"] = "档位 {0}",
                ["firmware"] = "固件 {0}",
                ["firmware_unknown"] = "固件 —",
                ["active_slot"] = "当前配置 {0}/{1}",
                ["active_slot_unknown"] = "当前 —",
                ["period_value"] = "{0} 秒",
                ["status_reading_profiles"] = "正在读取配置文件...",
                ["status_profiles_loaded"] = "配置文件已读取。当前配置 {0}。",
                ["status_profile_read"] = "配置 {0} 已读取。",
                ["status_slot_activated"] = "配置 {0} 已激活。备份：{1}",
                ["status_dpi_written"] = "DPI 已写入配置 {0}。备份：{1}",
                ["status_led_written"] = "灯光（{0}）已写入配置 {1}。备份：{2}",
                ["status_device_not_found"] = "未找到设备。",
                ["status_enable_write"] = "请启用“允许写入鼠标”。",
                ["status_read_failed"] = "读取失败：{0}",
                ["status_write_failed"] = "写入失败：{0}",
            },
        };

    private string _language = "en";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language
    {
        get => _language;
        set
        {
            if (!Tables.ContainsKey(value))
            {
                value = "en";
            }

            if (_language == value)
            {
                return;
            }

            _language = value;
            foreach (var property in typeof(AppStrings).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.Name != nameof(Language))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));
                }
            }
        }
    }

    public string Profile => Get("profile");

    public string Activate => Get("activate");

    public string Refresh => Get("refresh");

    public string LanguageLabel => Get("language");

    public string Lighting => Get("lighting");

    public string Sensitivity => Get("sensitivity");

    public string Zone => Get("zone");

    public string Mode => Get("mode");

    public string Period => Get("period");

    public string Brightness => Get("brightness");

    public string ApplyLighting => Get("apply_lighting");

    public string ApplyDpi => Get("apply_dpi");

    public string AllowWrite => Get("allow_write");

    public string ProfileTooltip => Get("profile_tooltip");

    public string ActivateTooltip => Get("activate_tooltip");

    public string AllZones => Get("all_zones");

    public string ModeSolid => Get("mode_solid");

    public string ModeBreathing => Get("mode_breathing");

    public string ModeRainbow => Get("mode_rainbow");

    public string DeviceNotFound => Get("status_device_not_found");

    public string EnableWrite => Get("status_enable_write");

    public string FirmwareUnknown => Get("firmware_unknown");

    public string ActiveSlotUnknown => Get("active_slot_unknown");

    public string ZoneName(int index) => string.Format(CultureInfo.InvariantCulture, Get("zone_n"), index);

    public string DpiStage(int index) => string.Format(CultureInfo.InvariantCulture, Get("dpi_stage"), index);

    public string Firmware(int version) => string.Format(CultureInfo.InvariantCulture, Get("firmware"), version);

    public string ActiveSlot(int slot, int count) =>
        string.Format(CultureInfo.InvariantCulture, Get("active_slot"), slot, count);

    public string PeriodValue(int seconds) =>
        string.Format(CultureInfo.InvariantCulture, Get("period_value"), seconds);

    public string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(key), args);

    public string Get(string key)
    {
        if (Tables.TryGetValue(_language, out var table) && table.TryGetValue(key, out var value))
        {
            return value;
        }

        return Tables["en"].TryGetValue(key, out var fallback) ? fallback : key;
    }
}

public sealed record LanguageOption(string Code, string Name);
