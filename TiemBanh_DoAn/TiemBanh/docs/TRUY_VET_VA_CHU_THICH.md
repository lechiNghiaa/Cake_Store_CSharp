# Ma trận yêu cầu → giao diện → mã nguồn → kiểm chứng

| UC | Tên giao diện / chú thích | Business | Presentation / DAL | Kiểm chứng |
|---|---|---|---|---|
| UC01 | Đăng nhập | AccountService.LoginAsync | auth-form / LoginForm / AuthController; AppUser | Sai mật khẩu, khóa, token cũ |
| UC02 | Đăng ký | RegisterAsync | auth-form; AppUser | Trùng tên, không nhận Role=Admin |
| UC03 | Hồ sơ cá nhân | MeAsync/ProfileAsync | profile-form / MainForm.Profile | Chỉ hồ sơ người hiện tại |
| UC04 | Danh sách bánh | CatalogService.ListAsync | page-catalog, cake-grid; Cake/Category/Inventory | Chỉ active; trang rỗng |
| UC05 | Tìm kiếm bánh | ListAsync(search,categoryId) | search-form; cùng chú thích CSS/JS/HTML | Lọc tên/mã/danh mục |
| UC06 | Giỏ hàng và đặt COD | Rules.Total cho giá quyết định khi UC07 | cart-lines / renderCart; localStorage theo user | Số lượng, xóa, tổng dự kiến; cần test trình duyệt |
| UC07 | Đặt hàng COD / Tính tổng COD / Giữ tồn | OrderService.PlaceAsync; Rules.Total/Stock | checkout-form; Order/OrderLine/Inventory/History/Movement | Chống lặp, payload khác, snapshot, rollback |
| UC08 | Theo dõi đơn hàng / Hủy đơn hàng | ListAsync/ActionAsync | page-orders; OrderHistory | Phạm vi CustomerId, Pending, Version |
| UC09 | Giao hàng và ghi nhận COD | ActionAsync complete/fail | page-orders; DeliveryAttempt/CodReceipt | Đúng driver, đủ tiền, không receipt lặp |
| UC10 | Quản lý bánh và tồn kho / Tải ảnh bánh | CatalogService.SaveAsync/StockAsync | MainForm.EditCake/Stock/Image; Cake/Inventory | Giá/tồn/ảnh và quyền Admin |
| UC11 | Đối soát COD / Lập kỳ / Duyệt kỳ / Khóa kỳ | CodService | period-form / MainForm.PeriodAction; Period/Handover | Lệch/khớp, receipt unique, khóa bất biến |
| UC12 | Quản lý nhân viên / Tạo / Sửa nhân viên | StaffAsync/UpdateStaffAsync | MainForm.CreateStaff/EditStaff | Employee/Delivery, không Admin |
| UC13 | Báo cáo thống kê / Xuất báo cáo CSV | CodService.ReportAsync | MainForm report / ExportReport | Chỉ Completed đủ COD |
| UC14 | Xử lý đơn hàng / Kiểm bánh hoàn | OrderService.ActionAsync | page-orders; delivery/history/stock | Chuỗi trạng thái, số hỏng, giao lại |
| UC15 | Tra cứu khách hàng / Khóa/Mở tài khoản | UsersAsync/ActiveAsync | page-customers / MainForm.ToggleActive | Employee chỉ đọc; Admin thay trạng thái |

Các file HTML, CSS, JS dùng đúng các tên ở cột thứ hai. Trong C# có chú thích nhóm UC và chú thích từng thao tác nghiệp vụ. Dùng tìm kiếm toàn dự án theo chuỗi “Tính tổng COD”, “Tìm kiếm bánh”, “Giữ tồn”, “Đối soát COD” để đi từ chức năng sang mã.

## Những điều chỉnh so với mô hình phân tích ban đầu

- Ba lớp được hiện thực bằng Presentation, Business, DataAccess; không giữ các project Domain/Application/Infrastructure riêng của phương án phân lớp trước.
- Tài khoản dùng bảng AppUser để tránh tên SQL User; các vai trò và quan hệ giữ nguyên ý nghĩa.
- Giỏ nằm ở Web, chưa tạo Cart/CartLine; địa chỉ lấy từ hồ sơ và snapshot Order, chưa tạo SavedAddress riêng.
- Bổ sung RequestHash để phân biệt RequestId gửi lại nhưng nội dung khác.
- CodPeriod.CreatedById ghi nhận người lập; CodHandover thuộc kỳ nên không lặp ReceiverId.
- Tên DTO/phương thức trong tài liệu sản phẩm này đối chiếu trực tiếp mã nguồn; sơ đồ phân tích trước là tài liệu nền.
