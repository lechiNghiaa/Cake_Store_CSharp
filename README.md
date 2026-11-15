# Bakery Docs — Nhóm 07

Website tài liệu phân tích và thiết kế hệ thống quản lý tiệm bánh

Link github-website giới thiệu đồ án: https://lechinghiaa.github.io/Cake_Store_CSharp

# Cấu trúc thư mục
```text
Cake_Store_CSharp/
├── .github/
│   └── workflows/
│       └── pages.yml               # CI/CD deploy GitHub Pages
├── github-web/                     # Trang tài liệu / giới thiệu dự án
│   ├── assets/
│   ├── download/
│   ├── index.html
│   ├── design-microservices.html
│   ├── development.html
│   └── model-microservices.html
├── TiemBanh_DoAn/                  # Source code chính (.NET / C#)
│   └── TiemBanh/
│       ├── src/
│       │   ├── TiemBanh.Api/           # RESTful API
│       │   ├── TiemBanh.Web/           # Giao diện người dùng
│       │   │   └── wwwroot/
│       │   │       ├── index.html
│       │   │       └── css/
│       │   │           └── site.css
│       │   ├── TiemBanh.Admin/         # Trang quản trị
│       │   ├── TiemBanh.Business/      # Xử lý nghiệp vụ (Service)
│       │   └── TiemBanh.DataAccess/    # Truy xuất dữ liệu (Repository/EF)
│       ├── TiemBanh.sln
│       └── ...
└── README.md
```
