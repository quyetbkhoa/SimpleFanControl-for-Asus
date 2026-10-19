using System.Collections.Generic;

namespace AsusFanControlGUI
{
    internal enum UiLanguage
    {
        Vietnamese,
        English
    }

    internal sealed class Localizer
    {
        private static readonly Dictionary<string, string> Vietnamese =
            new Dictionary<string, string>
            {
                { "navControl", "Điều khiển quạt" },
                { "navSettings", "Cài đặt" },
                { "dashboardTitle", "Tổng quan làm mát" },
                { "modeLabel", "Chế độ điều khiển" },
                { "editPoint", "Chỉnh điểm quạt" },
                { "applyPoint", "Áp dụng điểm" },
                { "pointNumber", "Điểm {0}" },
                { "dashboardSubtitle", "Theo dõi nhiệt độ và tối ưu hiệu năng tản nhiệt." },
                { "settingsTitle", "Cài đặt" },
                { "settingsSubtitle", "Tùy chỉnh cách ứng dụng khởi động, cập nhật và vận hành." },
                { "cpuTemperature", "Nhiệt độ CPU" },
                { "fanSpeed", "Tốc độ quạt" },
                { "appliedOutput", "Mức đang áp dụng" },
                { "coolingControl", "Điều khiển quạt" },
                { "coolingControlDescription", "Bật điều khiển để áp dụng tốc độ thủ công hoặc đường cong nhiệt." },
                { "controlEnabled", "Đang bật" },
                { "controlDisabled", "Đang tắt" },
                { "manual", "Thủ công" },
                { "curve", "Đường cong" },
                { "manualTitle", "Tốc độ thủ công" },
                { "manualDescription", "Kéo thanh trượt để đặt tốc độ cố định cho tất cả quạt." },
                { "curveModeTitle", "Điều khiển theo nhiệt độ" },
                { "curveModeDescription", "Tốc độ quạt được nội suy tự động theo nhiệt độ CPU." },
                { "curveTitle", "Đường cong quạt" },
                { "curveSubtitle", "Kéo điểm hoặc nhập bên phải · Vạch cam: CPU hiện tại" },
                { "resetCurve", "Đặt lại" },
                { "waitingTemperature", "Đang chờ dữ liệu nhiệt độ…" },
                { "general", "Chung" },
                { "behavior", "Hành vi ứng dụng" },
                { "safety", "An toàn" },
                { "startWindows", "Khởi động cùng Windows" },
                { "startWindowsDescription", "Tự chạy sau khi bạn đăng nhập Windows." },
                { "polling", "Chu kỳ cập nhật" },
                { "pollingDescription", "Tần suất đọc nhiệt độ CPU và tốc độ quạt." },
                { "autoRefresh", "Tự động làm mới thống kê" },
                { "autoRefreshDescription", "Liên tục cập nhật các thông số trên bảng điều khiển." },
                { "minimizeTray", "Thu nhỏ xuống khay hệ thống" },
                { "minimizeTrayDescription", "Bấm nút đóng vẫn giữ ứng dụng chạy nền." },
                { "resetOnExit", "Trả quyền điều khiển khi thoát" },
                { "resetOnExitDescription", "Đưa quạt về chế độ tự động của firmware ASUS." },
                { "safeLimits", "Giới hạn tốc độ an toàn" },
                { "safeLimitsDescription", "Giới hạn lệnh điều khiển trong khoảng 40–99%." },
                { "refreshNow", "Làm mới ngay" },
                { "exit", "Thoát ứng dụng" },
                { "open", "Mở ứng dụng" },
                { "firmware", "Firmware tự động" },
                { "manualActive", "Đang chạy thủ công" },
                { "curveActive", "Đang chạy đường cong" },
                { "initializing", "Đang khởi tạo" },
                { "unavailable", "Phần cứng không khả dụng" },
                { "firmwareActive", "Firmware ASUS đang điều khiển quạt." },
                { "defaultCurveRestored", "Đã khôi phục đường cong mặc định." },
                { "startupInstalled", "Đã bật khởi động cùng Windows." },
                { "startupRemoved", "Đã tắt khởi động cùng Windows." },
                { "startupFailed", "Không thể thay đổi cài đặt khởi động: {0}" },
                { "pollingUpdated", "Đã đổi chu kỳ cập nhật thành {0}." },
                { "temperatureUnavailable", "Không thể đọc nhiệt độ: {0}" },
                { "curveTarget", "CPU {0}°C · Mục tiêu {1}%" },
                { "trayMessage", "Ứng dụng vẫn đang chạy dưới khay hệ thống." },
                { "seconds", "{0} giây" },
                { "auto", "TỰ ĐỘNG" },
                { "currentTemperature", "CPU hiện tại" },
                { "temperatureAxis", "Nhiệt độ CPU (°C)" },
                { "speedAxis", "Tốc độ quạt (%)" },
                { "languageName", "Tiếng Việt" }
            };

        private static readonly Dictionary<string, string> English =
            new Dictionary<string, string>
            {
                { "navControl", "Fan control" },
                { "navSettings", "Settings" },
                { "dashboardTitle", "Cooling overview" },
                { "modeLabel", "Control mode" },
                { "editPoint", "Edit curve point" },
                { "applyPoint", "Apply point" },
                { "pointNumber", "Point {0}" },
                { "dashboardSubtitle", "Monitor temperatures and optimize cooling performance." },
                { "settingsTitle", "Settings" },
                { "settingsSubtitle", "Customize how the application starts, refreshes and behaves." },
                { "cpuTemperature", "CPU temperature" },
                { "fanSpeed", "Fan speed" },
                { "appliedOutput", "Applied output" },
                { "coolingControl", "Cooling control" },
                { "coolingControlDescription", "Enable control to apply a manual speed or temperature curve." },
                { "controlEnabled", "Enabled" },
                { "controlDisabled", "Disabled" },
                { "manual", "Manual" },
                { "curve", "Fan curve" },
                { "manualTitle", "Manual speed" },
                { "manualDescription", "Drag the slider to set a fixed speed for every fan." },
                { "curveModeTitle", "Temperature-based control" },
                { "curveModeDescription", "Fan speed is interpolated automatically from CPU temperature." },
                { "curveTitle", "Fan curve" },
                { "curveSubtitle", "Drag or enter values on the right · Orange: current CPU" },
                { "resetCurve", "Reset" },
                { "waitingTemperature", "Waiting for temperature data…" },
                { "general", "General" },
                { "behavior", "Application behavior" },
                { "safety", "Safety" },
                { "startWindows", "Start with Windows" },
                { "startWindowsDescription", "Launch automatically after you sign in to Windows." },
                { "polling", "Polling interval" },
                { "pollingDescription", "How often CPU temperature and fan speed are read." },
                { "autoRefresh", "Auto-refresh statistics" },
                { "autoRefreshDescription", "Continuously update the dashboard metrics." },
                { "minimizeTray", "Minimize to system tray" },
                { "minimizeTrayDescription", "Keep the application running when its window is closed." },
                { "resetOnExit", "Restore firmware control on exit" },
                { "resetOnExitDescription", "Return every fan to ASUS automatic firmware control." },
                { "safeLimits", "Safe output limits" },
                { "safeLimitsDescription", "Limit fan commands to the 40–99% range." },
                { "refreshNow", "Refresh now" },
                { "exit", "Exit application" },
                { "open", "Open application" },
                { "firmware", "Firmware auto" },
                { "manualActive", "Manual active" },
                { "curveActive", "Fan curve active" },
                { "initializing", "Initializing" },
                { "unavailable", "Hardware unavailable" },
                { "firmwareActive", "ASUS firmware is controlling the fans." },
                { "defaultCurveRestored", "Default fan curve restored." },
                { "startupInstalled", "Windows startup enabled." },
                { "startupRemoved", "Windows startup disabled." },
                { "startupFailed", "Could not update the startup setting: {0}" },
                { "pollingUpdated", "Polling interval changed to {0}." },
                { "temperatureUnavailable", "Could not read temperature: {0}" },
                { "curveTarget", "CPU {0}°C · Target {1}%" },
                { "trayMessage", "The application is still running in the system tray." },
                { "seconds", "{0} seconds" },
                { "auto", "AUTO" },
                { "currentTemperature", "Current CPU" },
                { "temperatureAxis", "CPU temperature (°C)" },
                { "speedAxis", "Fan speed (%)" },
                { "languageName", "English" }
            };

        public Localizer(UiLanguage language)
        {
            Language = language;
        }

        public UiLanguage Language { get; set; }

        public string this[string key]
        {
            get
            {
                var dictionary = Language == UiLanguage.Vietnamese
                    ? Vietnamese
                    : English;
                return dictionary.TryGetValue(key, out var value) ? value : key;
            }
        }

        public string Format(string key, params object[] values)
        {
            return string.Format(this[key], values);
        }
    }
}
