# KẾ HOẠCH NHÓM 02 – 4 THÀNH VIÊN

## Ngày 14/11/2026 – Một lần tạo khung trên `main`
- Đưa thư mục `TiemBanh/` này vào gốc repository đang có `github-web/`.
- Kiểm tra solution có đầy đủ 6 project và `tests/TiemBanh.Tests`.
- Không sao chép ngay nội dung hoàn chỉnh của ZIP gốc; giữ khung và stub.
- Chia branch, mở Pull Request, merge từng chức năng sau review.

## Phân công tách biệt theo thư mục
| Thành viên | Branch | Chủ trì | File/thư mục ưu tiên | Mốc đầu tiên |
|---|---|---|---|---|
| 1 – Phạm Thanh Hòa | `feature/data-codefirst` | Data Access, Entity, DbContext, Migration | `src/TiemBanh.DataAccess/` | Entity + quan hệ + migration đầu |
| 2 – Lê Chí Nghĩa | `feature/business-orders` | Business, tồn kho, đơn, COD | `src/TiemBanh.Business/` | Rules + Contracts + OrderService |
| 3 – Bùi Thọ Khánh Trình | `feature/api-auth` | API, JWT, Controller | `src/TiemBanh.Api/` | Auth/Products endpoints + health |
| 4 – Xà Khải Thành | `feature/ui-admin-web` | Web HTML/CSS/JS, WinForms Admin | `src/TiemBanh.Web/`, `src/TiemBanh.Admin/` | Giao diện mẫu + gọi API mock |

## Quy tắc tích hợp
- Chỉ một người cập nhật Migration và schema (người phụ trách DataAccess).
- Các file chung `.sln`, `compose.yaml`, `deploy/Dockerfile`, `.github/workflows/` do trưởng nhóm xử lý sau khi review; không sửa song song.
- PR của DataAccess (Entity + DbContext) nên được merge trước PR Business phụ thuộc entity; tiếp theo API, rồi Web/Admin; các nhánh phải rebase/merge main cập nhật trước khi tích hợp.
- Chia thêm theo 15 Use Case, không tự phát sinh chức năng ngoài đề.
- Không commit `.env`, mật khẩu, `bin/`, `obj/`, log thực thi hoặc thư mục `evidence/` của ZIP gốc vào repo.
- Test/CI ban đầu là smoke test; SW01–SW20, SYS01–SYS10 sẽ bổ sung sau. Không công bố chức năng đã hoàn thành hoặc Docker chạy end-to-end khi chưa thực sự kiểm thử.

## Lộ trình đưa đến ZIP đích
1. Khung: giữ project, reference, test stub, giao diện stub (hôm nay).
2. DataAccess: Entities, DbContext, Fluent API, EF Core Code First migration, Repository.
3. Business: Account/Catalog/Order/COD + bất biến OnHand, Reserved, RequestId.
4. API: Auth JWT, phân quyền, Controller, DTO, lỗi HTTP thống nhất.
5. Presentation: Web giỏ/đặt hàng/vận hành, Admin WinForms quản trị.
6. Infra & QA: compose, Dockerfile, Migrator thật, seed, test/unit/integration/system, backup, docs; xác minh và gộp main.

> Tham chiếu: `TiemBanh_NET9_3Lop_Docker_Source(2).zip` có code hoàn thiện để đối chiếu ở giai đoạn cuối, không lấy trực tiếp làm lịch sử commit cho từng thành viên.
