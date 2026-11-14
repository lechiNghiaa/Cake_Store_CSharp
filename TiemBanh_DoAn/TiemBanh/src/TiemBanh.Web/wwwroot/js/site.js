
"use strict";

// Dữ liệu mẫu, chưa kết nối API.
const cakes = [
    {
        id: 1,
        name: "Bánh kem dâu",
        price: 150000
    },
    {
        id: 2,
        name: "Bánh tiramisu",
        price: 120000
    },
    {
        id: 3,
        name: "Bánh socola",
        price: 100000
    }
];

const cakeList = document.getElementById("cake-list");

const formatPrice = (price) => {
    return price.toLocaleString("vi-VN") + " đ";
};

cakes.forEach((cake) => {
    const card = document.createElement("article");
    card.className = "cake-card";

    const title = document.createElement("h3");
    title.textContent = cake.name;

    const price = document.createElement("p");
    price.className = "price";
    price.textContent = formatPrice(cake.price);

    card.append(title, price);
    cakeList.appendChild(card);
});
