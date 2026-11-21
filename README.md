# Cake Store C# — Hệ thống quản lý tiệm bánh

Hệ thống quản lý tiệm bánh viết bằng **C# / .NET 9**, theo mô hình phân lớp **Presentation – Business – Data Access**, kết hợp **SQL Server 2022**, **Entity Framework Core**, Web, WinForms Admin và REST API.

**Website giới thiệu và tài liệu đồ án:** https://lechinghiaa.github.io/Cake_Store_CSharp/

> GitHub Pages là trang giới thiệu/tài liệu, không phải bản triển khai backend và cơ sở dữ liệu.

## Chức năng chính

- **Khách hàng:** xem và tìm bánh, giỏ hàng, đặt hàng thanh toán khi nhận (**COD**), theo dõi đơn và quản lý tài khoản.
- **Nhân viên:** xử lý đơn hàng, phân công giao hàng, theo dõi quy trình và đối soát COD theo phân quyền.
- **Người giao hàng:** cập nhật trạng thái giao hàng.
- **Quản trị viên:** quản lý sản phẩm, nhân viên, khách hàng và báo cáo theo quyền được cấp.
- **Giới hạn nghiệp vụ:** quản lý tồn kho bánh thành phẩm, không quản lý nguyên liệu; không tích hợp thanh toán trực tuyến.

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| API | ASP.NET Core Web API (.NET 9), Swagger, JWT |
| Business | C# services, quy tắc tồn kho, đơn hàng và COD |
| Data Access | SQL Server 2022, EF Core 9, Code First migrations |
| Web | HTML, CSS, JavaScript, ASP.NET Core / YARP reverse proxy |
| Admin | Windows Forms (.NET 9, Windows) |
| Testing | xUnit, Node.js test runner, kiểm thử SQL Server |
| DevOps | Docker Compose, GitHub Actions |

## Cấu trúc repository

```text
Cake_Store_CSharp/
├── .github/workflows/
│   ├── pages.yml                  # GitHub Pages
│   └── tiembanh-ci.yml            # Kiểm thử .NET/Web và Docker build
├── github-web/                    # Website giới thiệu và tài liệu
├── TiemBanh_DoAn/
│   └── TiemBanh/
│       ├── src/
│       │   ├── TiemBanh.Api/      # REST API, JWT, controllers
│       │   ├── TiemBanh.Business/ # Nghiệp vụ
│       │   ├── TiemBanh.DataAccess/ # Entities, DbContext, Repository, Migration
│       │   ├── TiemBanh.Web/      # Web (wwwroot)
│       │   ├── TiemBanh.Admin/    # WinForms Admin
│       │   └── TiemBanh.Migrator/ # Chạy migrations và seed
│       ├── tests/
│       │   ├── TiemBanh.Tests/    # xUnit
│       │   ├── web/              # Kiểm thử tệp giao diện
│       │   └── system_sqlserver.py
│       ├── scripts/              # Khởi tạo, sao lưu, kiểm tra local
│       ├── deploy/Dockerfile
│       ├── compose.yaml
│       ├── .env.example
│       ├── TiemBanh.sln
│       └── docs/
└── README.md
```

Các Controller đã được tách thành `AuthController.cs`, `CakesController.cs`, `OrdersController.cs` và `ManagementController.cs`. ASP.NET Core nhận diện Controller theo lớp và routing, không phụ thuộc việc chúng nằm chung trong một tệp `Controllers.cs`.

## Chạy dự án trên máy bằng Docker

**Yêu cầu:** Docker Desktop có Docker Compose. Trên Windows, mở PowerShell tại thư mục repository rồi chạy:

```powershell
cd TiemBanh_DoAn/TiemBanh
.\scripts\Initialize.ps1
docker compose up --build -d
docker compose ps
```

- **Web:** http://localhost:8080
- **API:** http://localhost:8081
- **Swagger:** http://localhost:8081/swagger (môi trường Development)
- **Health check API:** http://localhost:8081/health/live

`Initialize.ps1` tạo `.env` cục bộ nếu chưa có. Có thể tham khảo `.env.example` để cấu hình `SQL_PASSWORD`, `JWT_KEY`, `ADMIN_PASSWORD`. **Không commit `.env`, mật khẩu hoặc tài khoản thật lên GitHub.**

Dừng hệ thống:

```powershell
docker compose down
```

Không dùng `docker compose down -v` nếu muốn giữ dữ liệu trong Docker volume. WinForms Admin chạy riêng trên Windows, không nằm trong các container Web/API/SQL Server.

## Build và kiểm thử

Từ thư mục `TiemBanh_DoAn/TiemBanh`:

```powershell
dotnet restore TiemBanh.sln
dotnet build TiemBanh.sln
dotnet test tests/TiemBanh.Tests/TiemBanh.Tests.csproj
node --test tests/web/ui-contract.test.cjs
```

Có thể chạy script kiểm tra local khi đã cài các công cụ cần thiết:

```powershell
.\scripts\verify-local.ps1
```

GitHub Actions ở `.github/workflows/tiembanh-ci.yml` gồm các job `dotnet-tests`, `web-tests` và `docker-build`. Xem trạng thái chạy thực tế tại tab [Actions](https://github.com/lechiNghiaa/Cake_Store_CSharp/actions). CI thành công không thay thế kiểm thử chạy hệ thống thật với SQL Server và đầy đủ luồng nghiệp vụ.

## Phân công nhóm

| Thành viên | Trách nhiệm chính |
| --- | --- |
| **Phạm Thanh Hòa** | Data Access: Entities, DbContext, Repository, EF Core migrations, script cơ sở dữ liệu |
| **Lê Chí Nghĩa** | Business: tài khoản, danh mục/tồn kho, đơn hàng, COD, quy tắc nghiệp vụ |
| **Bùi Thọ Khánh Trình** | API, JWT, Controllers, xử lý lỗi, kiểm thử API và CI/DevOps |
| **Xà Khải Thành** | Web, WinForms Admin, tích hợp API phía giao diện và kiểm thử giao diện |

Chi tiết: [Kế hoạch 4 thành viên](TiemBanh_DoAn/TiemBanh/docs/KE_HOACH_4_THANH_VIEN.md).

## Tài liệu

- [Hướng dẫn API](TiemBanh_DoAn/TiemBanh/docs/API.md)
- [Kết quả kiểm thử](TiemBanh_DoAn/TiemBanh/docs/KET_QUA_KIEM_THU.md)
- [Phương pháp luận](TiemBanh_DoAn/TiemBanh/docs/PHUONG_PHAP_LUAN.md)
- [Truy vết và chú thích](TiemBanh_DoAn/TiemBanh/docs/TRUY_VET_VA_CHU_THICH.md)

> Trước khi nghiệm thu cần xác minh môi trường chạy thực tế, quyền truy cập, dữ liệu SQL Server, quy trình đặt/giao hàng và đối soát COD.
