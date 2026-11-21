# KẾ HOẠCH VÀ TRẠNG THÁI NHÓM 02 – 4 THÀNH VIÊN

**Cập nhật tài liệu:** 21/11/2026. **Đối chiếu:** nhánh `main` của `lechiNghiaa/Cake_Store_CSharp`, bộ mã nguồn `TiemBanh_DoAn.zip` đã bàn giao. Đây là báo cáo **hiện diện mã nguồn**, không khẳng định đã nghiệm thu mọi chức năng trên SQL Server thật.

## 1. Phạm vi thực hiện

Hệ thống quản lý bánh thành phẩm, đặt hàng COD, xử lý đơn/giao hàng, đối soát tiền và báo cáo. Sử dụng .NET 9, kiến trúc Presentation–Business–Data Access, SQL Server và EF Core, Web, WinForms và REST API. Không quản lý nguyên liệu, không tích hợp thanh toán trực tuyến.

## 2. Phân công và mã nguồn hiện có

| Thành viên | Phần phụ trách | File/thư mục bàn giao chính | Trạng thái đối chiếu |
|---|---|---|---|
| Phạm Thanh Hòa | Data Access, quan hệ dữ liệu, Repository, migration, script DB | `src/TiemBanh.DataAccess/`, `scripts/Initialize.ps1`, `scripts/Backup.ps1`, `scripts/backup.sql`, `scripts/initialize.sh`, `tests/system_sqlserver.py` | Đã có file chính; `DesignFactory.cs` ở đúng thư mục |
| Lê Chí Nghĩa | Business, tài khoản, danh mục/tồn kho, đơn hàng, COD | `src/TiemBanh.Business/` | Đã có các Service, Rules, Contracts, Bootstrap |
| Bùi Thọ Khánh Trình | API, JWT, Controller, Testing API, CI | `src/TiemBanh.Api/`, `tests/TiemBanh.Tests/`, `.github/workflows/tiembanh-ci.yml`, `scripts/verify-local.ps1` | Đã có các Controller, file test và CI |
| Xà Khải Thành | Web, WinForms, tích hợp API phía giao diện và kiểm thử Web | `src/TiemBanh.Web/`, `src/TiemBanh.Admin/`, `tests/web/` | Đã có giao diện, hình ảnh, Admin và test tĩnh |

**Lưu ý:** `.github/workflows/tiembanh-ci.yml` nằm tại **gốc repository**, không nằm trong `TiemBanh/`. `Controllers.cs` bản ZIP được tách thành `AuthController.cs`, `CakesController.cs`, `OrdersController.cs`, `ManagementController.cs` và không nên upload thêm file gốc gây trùng class.

## 3. Quy tắc tích hợp

1. Hòa chịu trách nhiệm review thay đổi schema và migration; không chỉnh sửa migration đã áp dụng tuỳ tiện.
2. File dùng chung (`TiemBanh.sln`, `compose.yaml`, `Dockerfile`, workflow) cần được review để tránh xung đột.
3. Mỗi commit phải phản ánh thay đổi thực; tách Controller không làm thay đổi bản chất nghiệp vụ.
4. Không đưa `.env`, mật khẩu, `bin/`, `obj/`, log và dữ liệu riêng tư lên GitHub.
5. Đối chiếu mã nguồn chỉ chứng minh có file; cần build, chạy test và thử vận hành để nghiệm thu.

## 4. Kết quả kiểm tra tự động được ghi nhận

Lần GitHub Actions `44875598058` (21/11/2026, commit `ee45d2e`, trạng thái **success**) đã hoàn tất workflow hiện tại. Workflow có ba job: `dotnet-tests`, `web-tests`, `docker-build` (chỉ build image API/Web). Xem: https://github.com/lechiNghiaa/Cake_Store_CSharp/actions/runs/44875598058

Kết quả này **không** tự xác nhận ứng dụng đã chạy cùng SQL Server trong Docker Compose, không chứng minh WinForms chạy trên Windows, không thay thế kiểm thử trình duyệt và bài kiểm thử đồng thời trên SQL Server.

## 5. Các bước nghiệm thu còn cần thực hiện

- [ ] Chạy `docker compose up --build -d` trên máy đủ tài nguyên; kiểm tra `db`, `migrate`, `api`, `web` và endpoint readiness.
- [ ] Chạy `python tests/system_sqlserver.py` với môi trường SQL Server thực; lưu log kết quả, không commit thông tin bí mật.
- [ ] Thử luồng khách hàng → nhân viên → người giao → đối soát COD → Admin.
- [ ] Chạy WinForms Admin trên Windows; kiểm tra đăng nhập, báo cáo, sản phẩm và quyền.
- [ ] Kiểm tra backup/restore tại môi trường riêng và hoạt động sau khởi động lại.
- [ ] Ghi lại ngày, commit SHA, môi trường, expected/actual, ảnh hoặc log đã ẩn dữ liệu nhạy cảm.

## 6. Tài liệu liên quan

- `docs/API.md`: hợp đồng endpoint.
- `docs/TRUY_VET_VA_CHU_THICH.md`: ma trận 15 Use Case.
- `docs/KET_QUA_KIEM_THU.md`: bằng chứng/giới hạn xác minh.
- `docs/TaiLieu_SanPham_3Lop_NET9_Docker.docx`: báo cáo Word.
