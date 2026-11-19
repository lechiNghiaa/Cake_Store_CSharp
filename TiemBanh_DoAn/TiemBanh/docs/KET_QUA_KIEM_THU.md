# Biên bản xác minh bản bàn giao

## 1. Kết quả quan sát

| Hạng mục | Kết quả | Phạm vi bằng chứng |
|---|---|---|
| Build solution .NET 9 | Thành công, 0 warning/0 error | Web/API/BLL/DAL/Migrator; WinForms cross-build trên Linux |
| Unit tests | 25 đạt | Tiền, tồn, quyền và bảng chuyển trạng thái |
| HTTP integration | 2 kịch bản nhiều bước đạt | TestServer ASP.NET + EF Core SQLite in-memory; nhiều assertion |
| Tổng xUnit | 27/27 đạt | File all.trx và log trong evidence |
| EF Core migration | Sinh thành công | InitialCreate và snapshot; có unique/CHECK/FK |
| JavaScript | Kiểm tra cú pháp thành công | node --check app.js |
| Compose | Cấu hình hợp lệ | docker compose config --quiet với biến môi trường đã sinh |
| Docker build + SQL runtime | Chưa hoàn tất | Docker VFS hết đĩa khi giải nén SDK/container SQL; không coi là kết quả ứng dụng chạy đạt |
| Browser UI | Chưa thực thi | Công cụ browser của preview managed không sẵn có; cần kiểm UI bằng checklist |
| WinForms chạy trên Windows | Chưa thực thi | Môi trường kiểm chứng là Linux; cross-build không chứng minh hành vi UI Windows |
| Hiệu năng / khôi phục | Chưa đo | Không đưa ngưỡng mục tiêu thành số liệu thực nghiệm |

## 2. Kịch bản HTTP đã kiểm

Đăng nhập sai; đăng ký luôn Customer; trùng tên; hồ sơ; tạo bánh/điều chỉnh tồn; tìm kiếm; đặt và tính COD; lặp RequestId/cùng mã khác nội dung; đơn của người khác; Version cũ; Admin không xử lý thay Employee; hủy sau xác nhận; phân công; sai driver; thiếu tiền; hoàn thành; complete lặp; tồn hậu điều kiện; lập kỳ lệch; Employee không approve; sửa kỳ; approve/lock; chặn sửa Locked; doanh thu; quản lý staff; tra cứu/khóa khách và chặn token cũ; hủy Pending; fail và kiểm một bánh hỏng.

Trong lần chạy đầu, ca approve phát hiện route parameter `action` trùng từ khóa MVC gây 404. Đã đổi thành `operation` và chạy lại toàn bộ thành công. Đây là một ví dụ về vòng lặp đặc tả → kiểm thử → sửa lỗi → hồi quy.

## 3. Kiểm tra tiếp trên SQL Server và Docker

1. Chạy Compose trên máy đủ đĩa, đợi ready, chạy `python tests/system_sqlserver.py`.
2. Kiểm hai request cùng đặt lượng vượt tồn: chỉ một được nhận; không âm khả dụng; deadlock không gây ghi nửa chừng.
3. Đơn nhiều dòng có dòng sau hết hàng phải rollback toàn bộ reservation/history/order.
4. Restart API sau commit và gửi lại RequestId; không có đơn/receipt trùng.
5. Dừng DB: readiness 503; lỗi request phải rõ ràng; khởi động DB lại rồi kiểm dữ liệu.
6. Down/up không -v: đơn/ảnh còn nguyên. Thử backup/restore tại môi trường cô lập.
7. Browser: các form tìm kiếm, giỏ, đặt, giao COD và đối soát trên desktop/mobile; trạng thái loading/rỗng/lỗi và điều hướng vai trò.
8. WinForms: tất cả màn hình quản trị, upload ảnh, lỗi Version, export CSV, chuyển API URL.

## 4. Mục tiêu định lượng để nghiệm thu

20 người dùng đồng thời, warm-up 1 phút, đo 5 phút; p95 truy vấn <2 giây, đặt đơn <3 giây; ghi riêng 409 nghiệp vụ và 5xx. RPO mục tiêu 24 giờ, RTO mục tiêu 2 giờ; cần diễn tập để xác nhận. Hồ sơ phải ghi CPU/RAM, dữ liệu, image/commit, thời điểm, actual result và bằng chứng.
