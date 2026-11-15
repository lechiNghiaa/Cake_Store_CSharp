
"use strict";

// Dữ liệu mẫu - sẽ kết nối API sau
const cakes = [
    {
        id: 1,
        code: "B001",
        name: "Bánh kem dâu",
        category: "Bánh kem",
        price: 150000
    },
    {
        id: 2,
        code: "B002",
        name: "Bánh tiramisu",
        category: "Bánh lạnh",
        price: 120000
    },
    {
        id: 3,
        code: "B003",
        name: "Bánh socola",
        category: "Bánh kem",
        price: 100000
    }
];

// Lấy các thành phần giao diện
const cakeList = document.getElementById("cake-list");
const txtTimKiem = document.getElementById("txtTimKiem");
const cboDanhMuc = document.getElementById("cboDanhMuc");
const ketQua = document.getElementById("ketQuaTimKiem");

// Định dạng tiền Việt Nam
function formatPrice(price) {
    return price.toLocaleString("vi-VN") + " đ";
}

// Chuẩn hóa chữ để tìm kiếm không phân biệt dấu
function chuanHoa(text) {
    return String(text || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/g, "d")
        .replace(/Đ/g, "D")
        .toLowerCase()
        .trim();
}

// Hiển thị danh sách bánh
function hienThiBanh(danhSach) {
    cakeList.innerHTML = "";

    ketQua.textContent =
        "Tìm thấy " + danhSach.length + " sản phẩm";

    if (danhSach.length === 0) {
        const thongBao = document.createElement("p");
        thongBao.className = "empty-message";
        thongBao.textContent =
            "Không tìm thấy bánh phù hợp.";

        cakeList.appendChild(thongBao);
        return;
    }

    danhSach.forEach((cake) => {
        const card = document.createElement("article");
        card.className = "cake-card";

        const title = document.createElement("h3");
        title.textContent = cake.name;

        const code = document.createElement("p");
        code.textContent = "Mã bánh: " + cake.code;

        const category = document.createElement("p");
        category.className = "cake-category";
        category.textContent = cake.category;

        const price = document.createElement("p");
        price.className = "price";
        price.textContent = formatPrice(cake.price);

        card.append(title, code, category, price);
        cakeList.appendChild(card);
    });
}

// Tự động lấy các danh mục từ dữ liệu
function hienThiDanhMuc() {
    const danhMuc = [
        ...new Set(cakes.map(cake => cake.category))
    ];

    danhMuc.forEach((ten) => {
        const option = document.createElement("option");
        option.value = ten;
        option.textContent = ten;
        cboDanhMuc.appendChild(option);
    });
}

// UC05: Tìm kiếm bánh
function timKiemBanh() {
    const tuKhoa = chuanHoa(txtTimKiem.value);
    const danhMuc = cboDanhMuc.value;

    const ketQuaLoc = cakes.filter((cake) => {
        const dungTen =
            chuanHoa(cake.name).includes(tuKhoa);

        const dungMa =
            chuanHoa(cake.code).includes(tuKhoa);

        const dungDanhMuc =
            danhMuc === "" || cake.category === danhMuc;

        return (dungTen || dungMa) && dungDanhMuc;
    });

    hienThiBanh(ketQuaLoc);
}

// Nhấn nút tìm kiếm
document.getElementById("btnTimKiem")
    .addEventListener("click", timKiemBanh);

// Nhấn Enter cũng tìm kiếm
txtTimKiem.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
        timKiemBanh();
    }
});

// Thay đổi danh mục
cboDanhMuc.addEventListener("change", timKiemBanh);

// Làm mới danh sách
document.getElementById("btnLamMoi")
    .addEventListener("click", () => {
        txtTimKiem.value = "";
        cboDanhMuc.value = "";
        hienThiBanh(cakes);
    });

// Hiển thị dữ liệu ban đầu
hienThiDanhMuc();
hienThiBanh(cakes);
