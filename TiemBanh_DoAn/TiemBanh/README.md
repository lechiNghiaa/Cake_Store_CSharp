# Cake Store C# – Khung dự án để 4 thành viên phát triển dần

Khung được tách từ cấu trúc của `TiemBanh_NET9_3Lop_Docker_Source(2).zip` (không đưa chức năng hoàn chỉnh lên ngay ngày đầu).
Sử dụng **.NET 9 – 3 lớp Presentation / Business / Data Access – EF Core Code First – Docker Compose**.

## Cấu trúc
- `src/TiemBanh.Api`: API Presentation
- `src/TiemBanh.Business`: tầng nghiệp vụ
- `src/TiemBanh.DataAccess`: entity, EF Core và migration
- `src/TiemBanh.Web`: website
- `src/TiemBanh.Admin`: WinForms quản trị
- `src/TiemBanh.Migrator`: công cụ migration
- `tests/TiemBanh.Tests`: test ban đầu
- `docs/KE_HOACH_4_THANH_VIEN.md`: phân công và tiến độ

## Kiểm tra khung (cần .NET SDK 9, Internet NuGet nếu chưa cache)
```powershell
dotnet restore TiemBanh.sln
dotnet build TiemBanh.sln
dotnet test tests/TiemBanh.Tests/TiemBanh.Tests.csproj
dotnet run --project src/TiemBanh.Web
```
Để chạy solution Admin trên non-Windows cần Windows targeting pack. Khung API/Web hiện chỉ có endpoint health và giao diện mẫu, không có nghiệp vụ hoạt động.

## Docker
`compose.yaml` và `deploy/Dockerfile` được giữ làm **mẫu cấu hình đích** từ ZIP. **Không chạy Docker Compose ở giai đoạn khung**: các service này chỉ hoạt động sau khi nhóm hoàn thiện EF Core Migration, JWT, API, YARP và Migrator thật. Không nhập secret thật vào GitHub.

## Cách tích hợp
Không sửa `github-web/` cũ. Đưa riêng `TiemBanh/` vào gốc repo. Mỗi thành viên làm branch riêng, tạo Pull Request vào main; xem kế hoạch tại `docs/KE_HOACH_4_THANH_VIEN.md`.
