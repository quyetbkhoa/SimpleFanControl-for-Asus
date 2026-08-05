# SimpleFanControl for Asus

SimpleFanControl for Asus là ứng dụng Windows giúp theo dõi và điều khiển quạt
trên các laptop ASUS tương thích. Ứng dụng cung cấp giao diện song ngữ
Anh–Việt, chế độ điều khiển thủ công và biểu đồ quạt tự động theo nhiệt độ CPU.

## Tính năng

- Hiển thị nhiệt độ CPU, tốc độ quạt (RPM) và mức quạt đang áp dụng.
- Điều chỉnh tốc độ quạt thủ công từ giao diện.
- Tự động điều chỉnh quạt theo biểu đồ nhiệt độ có thể chỉnh sửa.
- Nội suy tốc độ quạt giữa các điểm trên biểu đồ để thay đổi mượt hơn.
- Hiển thị vị trí nhiệt độ CPU hiện tại trực tiếp trên biểu đồ.
- Hỗ trợ chu kỳ cập nhật 1, 2, 3, 5 hoặc 10 giây.
- Chế độ giới hạn an toàn giữ mức điều khiển trong khoảng 40–99%.
- Lưu thiết lập riêng cho từng tài khoản Windows.
- Tự khởi động cùng Windows bằng Scheduled Task với quyền cao nhất.
- Thu nhỏ xuống khay hệ thống và tiếp tục theo dõi ở nền.
- Có thể trả quyền điều khiển quạt lại cho firmware khi tắt ứng dụng.

## Cách hoạt động

Ứng dụng sử dụng thư viện `AsusWinIO64.dll` của ASUS để đọc nhiệt độ CPU,
tốc độ quạt và gửi mức PWM đến các quạt được hệ thống nhận diện.

Khi bật **Fan control / Điều khiển quạt**, ứng dụng hoạt động theo một trong hai
chế độ:

- **Manual / Thủ công:** mức quạt được lấy trực tiếp từ thanh trượt.
- **Temperature curve / Biểu đồ nhiệt độ:** ứng dụng đọc nhiệt độ CPU theo chu
  kỳ đã chọn, nội suy mức quạt từ biểu đồ rồi áp dụng cho tất cả quạt.

Biểu đồ mặc định:

| Nhiệt độ CPU | Mức quạt |
|---:|---:|
| 30°C | 40% |
| 55°C | 50% |
| 70°C | 65% |
| 80°C | 90% |
| 90°C | 100% |

Khi tắt **Fan control**, ứng dụng gửi giá trị `0` để thoát chế độ kiểm tra quạt
và trả quyền điều khiển lại cho firmware ASUS. Tùy chọn
**Restore firmware control on exit** thực hiện thao tác tương tự khi ứng dụng
thoát.

Một số mẫu máy yêu cầu tiến trình chạy dưới tài khoản `SYSTEM` để giao tiếp với
dịch vụ ASUS. Vì vậy, bản phát hành đi kèm `run.bat` và PsExec. Khi mở
`SimpleFanControlForAsus.exe`, ứng dụng sẽ yêu cầu quyền quản trị rồi tự khởi
động lại trong ngữ cảnh cần thiết.

## Tải và sử dụng

1. Tải file ZIP mới nhất trong
   [GitHub Releases](https://github.com/quyetbkhoa/SimpleFanControl-for-Asus/releases).
2. Giải nén toàn bộ ZIP vào cùng một thư mục.
3. Nếu đang chạy bản cũ, thoát hoàn toàn từ biểu tượng ở khay hệ thống.
4. Mở `SimpleFanControlForAsus.exe` và chấp nhận yêu cầu UAC.
5. Chọn điều khiển thủ công hoặc biểu đồ nhiệt độ, sau đó bật
   **Fan control / Điều khiển quạt**.

Không tách riêng hoặc xóa `AsusWinIO64.dll`, `PsExec.exe`, `run.bat` và các file
`.config` khỏi thư mục ứng dụng.

## Tương thích

Ứng dụng dành cho Windows x64 và yêu cầu:

- ASUS System Control Interface.
- Dịch vụ ASUS System Analysis đang hoạt động.
- Laptop có chức năng **Fan Diagnosis** hoạt động trong MyASUS.

Các dòng máy có khả năng tương thích gồm VivoBook, ZenBook, TUF Gaming,
ROG Strix, ROG Zephyrus và ROG Flow. Khả năng điều khiển thực tế còn phụ thuộc
vào model, firmware và phiên bản ASUS System Control Interface.

Việc đặt tốc độ quạt quá thấp có thể làm tăng nhiệt độ thiết bị. Hãy theo dõi
nhiệt độ CPU trong quá trình sử dụng và giữ **Safe output limits** nếu bạn không
chắc mức quạt phù hợp.
