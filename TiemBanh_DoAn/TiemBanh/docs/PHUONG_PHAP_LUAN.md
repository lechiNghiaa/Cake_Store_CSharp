# Phương pháp luận sản phẩm và quá trình xây dựng

## 1. Bài toán và phạm vi

Sản phẩm giải quyết quản lý bánh thành phẩm, khả dụng, đặt COD, phân công giao và đối soát tiền giữa người giao, nhân viên và Admin. Không quản lý nguyên liệu, công thức, sản xuất, thanh toán online, hãng vận chuyển hoặc nhiều chi nhánh. Bốn vai trò được kiểm tra tại API và Business, không chỉ ẩn nút giao diện.

## 2. Phương pháp: hướng đối tượng, lấy Use Case làm trung tâm, tăng dần

1. **Phân tích yêu cầu:** tách tác nhân, tiền/hậu điều kiện, luồng thuận, ngoại lệ và bất biến cho UC01–UC15. Xác lập ngôn ngữ chung: OnHand, Reserved, COD, kỳ bàn giao.
2. **Phân rã thiết kế:** dùng C4 để đi từ ngữ cảnh đến ứng dụng, thành phần, lớp; bố trí trách nhiệm trong 3 lớp. Bounded Context là ranh giới logic, không biến mỗi nhóm thành microservice.
3. **Đặc tả trước khi viết mã:** định nghĩa DTO, HTTP, mã lỗi, trạng thái, Version, RequestId, transaction và schema. Chọn các bất biến làm oracle kiểm thử.
4. **Hiện thực tăng dần:** triển khai lát cắt tài khoản → danh mục → đặt đơn → giao COD → đối soát → báo cáo. Một lát cắt gồm Business, DAL, API, UI và kiểm thử liên quan.
5. **Kiểm chứng dựa trên rủi ro:** ưu tiên sai quyền, bán vượt tồn, ghi lặp COD, mất dữ liệu giữa transaction; sau đó mới đo trải nghiệm và hiệu năng.
6. **Đánh giá:** ghi kết quả thực tế, adapter/môi trường, phiên bản; chỉ kết luận trong phạm vi đã quan sát. Mục tiêu nghiệm thu và kết quả thực nghiệm là hai loại dữ liệu riêng.

## 3. Chất lượng và quyết định kiến trúc

| Mã | Quyết định | Căn cứ / đánh đổi |
|---|---|---|
| ADR-01 | 3 lớp Presentation → Business → DAL | Nghiệp vụ dùng chung cho Web/WinForms; thêm project công cụ không tạo lớp thứ tư |
| ADR-02 | API tập trung, SQL transaction | Đặt hàng liên quan chặt với tồn/COD; giảm phức tạp giao dịch phân tán |
| ADR-03 | .NET 9 / EF Core 9 / SQL Server | Thống nhất C#, dữ liệu quan hệ và ràng buộc; SQL Server là mục tiêu runtime |
| ADR-04 | JWT + vai trò + phạm vi dữ liệu | Vai trò không đủ; tiếp tục kiểm CustomerId/DriverId và SecurityStamp |
| ADR-05 | Serializable + Version | Bảo toàn bất biến ghi; có thể deadlock, client phải xử lý xung đột và tải lại |
| ADR-06 | RequestId + RequestHash + unique | Retry không tạo đơn trùng; RequestId khác payload trả 409 |
| ADR-07 | Compose, volume, migration một lần | Dựng lại môi trường; một host vẫn là điểm lỗi đơn; cần backup riêng |
| ADR-08 | Chú thích theo chức năng giao diện | Từ báo cáo đến file/method dễ tìm; tránh chú thích chỉ lặp lại cú pháp |

## 4. Mô hình hóa C4 và ánh xạ 3 lớp

### C4 mức 1 — Context

Khách hàng xem/đặt/theo dõi; Employee xử lý đơn/lập kỳ; Delivery giao/thu tiền; Admin quản trị/duyệt/khóa. Ranh giới gồm một hệ thống tiệm bánh.

### C4 mức 2 — Container

Browser → Web ASP.NET Core (HTML/CSS/JS + YARP) → API .NET 9 → SQL Server. WinForms → cùng API. Web/API/DB đóng gói Docker; Admin chạy Windows; Migrator là tác vụ ngắn. Kho ảnh là named volume, không phải bảng nhị phân.

### C4 mức 3 — Component

API Controllers gọi AccountService, CatalogService, OrderService, CodService. Business sử dụng Rules và BakeryStore. DAL hiện thực BakeryStore, BakeryDbContext, entities và migrations. Controller không tính giá hoặc tự chuyển trạng thái. ApiClient WinForms chỉ biết JSON/HTTP.

### C4 mức 4 — Code: Đặt hàng COD

OrdersController.Create(CreateOrderRequest) → OrderService.PlaceAsync(Actor, request) → Rules.Stock / Rules.Total → BakeryStore.WriteAsync → BakeryDbContext.

PlaceAsync chuẩn hóa Lines theo CakeId, băm payload, tìm RequestId cũ; đọc giá từ DB, giữ tồn, thêm snapshot lines, history và stock movement trong cùng transaction. Ngoại lệ rollback toàn bộ. Unique bảo vệ trường hợp kiểm tra trước khi ghi chưa đủ để ngăn cạnh tranh.

## 5. Bất biến dữ liệu và hậu điều kiện

- `0 ≤ Reserved ≤ OnHand`, `Available = OnHand − Reserved`; số lượng dòng > 0, tiền không âm.
- Đặt: chỉ tăng Reserved. Hủy/từ chối Pending: chỉ giảm Reserved. Giao thành công: giảm cả OnHand/Reserved.
- Giao thất bại: giữ reservation. Kết thúc sau kiểm: nhả reservation, giảm OnHand phần hỏng. Giao lại không giữ thêm lần nữa.
- OrderLine giữ CakeName/UnitPrice snapshot; tổng COD lấy từ giá máy chủ + Shipping.
- Một receipt cho mỗi Order và DeliveryAttempt; một handover cho mỗi receipt.
- Kỳ approve cần Submitted, có item và mỗi Received=Expected. Locked không sửa.
- Customer chỉ đơn mình, Delivery chỉ attempt hiện hành được giao, Admin không thay Employee ở UC14.
- `Version` là bigint ứng dụng tăng, không phải SQL rowversion. Mọi lệnh sửa đối tượng chính gửi Version hiện có.

## 6. Đặc tả chuyển trạng thái

| Hiện tại | Lệnh / vai trò | Tiếp theo |
|---|---|---|
| Pending | confirm / Employee | Confirmed |
| Pending | cancel / Customer chủ đơn; reject / Employee | Cancelled |
| Confirmed | prepare / Employee | Preparing |
| Preparing, DeliveryFailed | assign / Employee | Delivering |
| Delivering | complete / Delivery hiện hành, thu đủ COD | Completed |
| Delivering | fail / Delivery hiện hành, có lý do | DeliveryFailed |
| DeliveryFailed | close-failed / Employee, kiểm số hỏng | Cancelled |
| Completed, Cancelled | Không có lệnh đổi trạng thái hợp lệ | Giữ nguyên |

Đối soát: Submitted → Approved → Locked; Employee tạo/sửa Submitted, Admin approve/lock. Không có endpoint để sửa Locked.

## 7. Tổ chức công việc và sử dụng công cụ hỗ trợ viết mã

Mỗi nhiệm vụ phải có: mã UC; tên chức năng giống UI; project/file được sửa; DTO/route; bất biến; dữ liệu biên; test cần chạy. Công cụ sinh mã nhận đặc tả này thay vì tự bổ sung vai trò/enum/route. Sau mỗi lát cắt: build → unit/integration → migration trên DB test → kiểm tra UI → cập nhật truy vết. Không chấp nhận code chỉ hiển thị giả dữ liệu cho chức năng đã có API.

**Định nghĩa hoàn thành:** đúng hợp đồng và quyền; luật chỉ có một nơi trong Business; DAL giữ ràng buộc; log/error không lộ bí mật; migration được version hóa; kiểm thử có kết quả; giao diện có loading/rỗng/lỗi; chú thích khớp tên UI; tài liệu cập nhật.

## 8. Đánh giá có phương pháp

Đánh giá theo mục tiêu–sản phẩm–bằng chứng: chức năng (truy vết UC); nhất quán (review route/DTO/enum/schema); tính đúng (test bất biến); triển khai (Compose/migration/volume); vận hành (log/health/backup). Kiểm thử SQLite không thay SQL Server về isolation/concurrency. Ngưỡng p95/RPO/RTO được ghi là tiêu chí khi chưa có phép đo. Xem biên bản kiểm thử kèm gói mã nguồn.
