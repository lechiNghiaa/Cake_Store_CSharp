'use strict';
// ==================== TRẠNG THÁI GIAO DIỆN — đồng nhất UC / tên chức năng ====================
const $ = s => document.querySelector(s);
const state = { user: null, token: sessionStorage.getItem('tb-token'), cakes: [], categories: [], orders: [], drivers: [], periods: [], receipts: [], page: 1, total: 0, register: false, editPeriod: null };
const labels = { Pending: 'Chờ xác nhận', Confirmed: 'Đã xác nhận', Preparing: 'Đang chuẩn bị', Delivering: 'Đang giao', Completed: 'Hoàn thành', DeliveryFailed: 'Giao thất bại', Cancelled: 'Đã kết thúc / hủy', Submitted: 'Chưa duyệt', Approved: 'Đã duyệt', Locked: 'Đã khóa' };
const money = n => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const empty = t => `<div class="empty">${esc(t)}</div>`;
// Log & error — thông báo có traceId; không log mật khẩu hoặc token.
function toast(message) { $('#toast').textContent = message; $('#toast').hidden = false; clearTimeout(toast.timer); toast.timer = setTimeout(() => $('#toast').hidden = true, 5500); }
async function api(path, method = 'GET', body) {
    const res = await fetch('/api' + path, { method, headers: { ...(body ? { 'Content-Type': 'application/json' } : {}), ...(state.token ? { Authorization: 'Bearer ' + state.token } : {}) }, body: body ? JSON.stringify(body) : undefined });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) { if (res.status === 401 && path != '/auth/login') { logout(); } throw Error(data.title || data.detail || Object.values(data.errors || {}).flat().join(' ') || 'Không thể xử lý yêu cầu.'); }
    return data;
}
async function safe(fn) { try { await fn(); } catch (e) { toast(e.message); } }
// Chống bấm lặp — khóa nút gửi trong thời gian thực thi; server vẫn kiểm tra RequestId.
function submit(form, handler) { form.addEventListener('submit', async e => { e.preventDefault(); const b = form.querySelector('button[type=submit],button:not([type])'); if (b) b.disabled = true; try { await handler(new FormData(form)); } catch (err) { toast(err.message); } finally { if (b) b.disabled = form.id === "checkout-form" && (!cart().length || !canShop()); } }); }
// ==================== ĐIỀU HƯỚNG THEO VAI TRÒ ====================
function userUI() {
    $('#guest-receipt').hidden = true;
    $('#checkout-form').reset();
    const role = state.user?.role; document.querySelectorAll('.signed-nav').forEach(x => x.hidden = !role);
    document.querySelectorAll('.employee-nav').forEach(x => x.hidden = role !== 'Employee');
    document.querySelectorAll('.customer-nav').forEach(x => x.hidden = !!role && role !== 'Customer');
    $('#auth-open').hidden = !!role; $('#logout').hidden = !role;
    const nav = document.querySelector('[data-page=orders]'); nav.hidden = !role || role === 'Admin'; nav.textContent = role === 'Employee' ? 'Xử lý đơn hàng' : role === 'Delivery' ? 'Giao hàng và ghi nhận COD' : 'Theo dõi đơn hàng';
    $('#profile-open').textContent = state.user?.fullName || 'Hồ sơ cá nhân'; updateCartCount();
}
async function navigate(page) {
    if (['orders', 'profile', 'cod', 'customers'].includes(page) && !state.user) { $('#auth-dialog').showModal(); return; }
    if (['cod', 'customers'].includes(page) && state.user?.role !== 'Employee') { toast('Bạn không có quyền mở chức năng này.'); return; }
    document.querySelectorAll('.page').forEach(x => x.hidden = x.id !== 'page-' + page); document.querySelectorAll('[data-page]').forEach(x => x.classList.toggle('active', x.dataset.page === page));
    if (page === 'catalog') await loadCatalog(); if (page === 'cart') renderCart(); if (page === 'orders') await loadOrders(); if (page === 'cod') await loadCod(); if (page === 'customers') await loadCustomers(); if (page === 'profile') fillProfile(); window.scrollTo({ top: 0, behavior: 'smooth' });
}
document.querySelectorAll('[data-page]').forEach(b => b.addEventListener('click', () => safe(() => navigate(b.dataset.page))));
$('#profile-open').onclick = () => safe(() => navigate('profile')); $('#admin-profile').onclick = () => safe(() => navigate('profile'));
// ==================== UC01–UC03 — ĐĂNG NHẬP, ĐĂNG KÝ, HỒ SƠ CÁ NHÂN ====================
$('#auth-open').onclick = () => $('#auth-dialog').showModal(); $('#auth-close').onclick = () => $('#auth-dialog').close();
$('#auth-toggle').onclick = () => { state.register = !state.register; $('#auth-title').textContent = $('#auth-submit').textContent = state.register ? 'Đăng ký' : 'Đăng nhập'; $('#register-fields').hidden = !state.register; $('#auth-form').elements.fullName.required = state.register; $('#auth-form').elements.password.minLength = state.register ? 8 : 1; $('#auth-toggle').textContent = state.register ? 'Đã có tài khoản? Đăng nhập' : 'Chưa có tài khoản? Đăng ký'; $('#auth-error').textContent = ''; };
submit($('#auth-form'), async f => {
    $('#auth-error').textContent = ''; const body = Object.fromEntries(f);
    try {
        if (state.register) { await api('/auth/register', 'POST', body); toast('Đăng ký thành công. Bạn có thể đăng nhập.'); $('#auth-toggle').click(); return; }
        const result = await api('/auth/login', 'POST', { userName: body.userName, password: body.password }); state.user = result.user; state.token = result.token; sessionStorage.setItem('tb-token', state.token); $('#auth-dialog').close(); $('#auth-form').reset(); userUI(); toast('Đăng nhập thành công.'); await navigate(state.user.role === 'Admin' ? 'admin' : state.user.role === 'Customer' ? 'catalog' : 'orders');
    } catch (e) { $('#auth-error').textContent = e.message; }
});
function logout() { state.user = null; state.token = null; sessionStorage.removeItem('tb-token'); userUI(); safe(() => navigate('catalog')); }
$('#logout').onclick = logout;
function fillProfile() { for (const k of ['fullName', 'phone', 'address']) $('#profile-form').elements[k].value = state.user[k] || ''; }
submit($('#profile-form'), async f => { state.user = await api('/auth/me', 'PUT', Object.fromEntries(f)); userUI(); toast('Đã lưu hồ sơ cá nhân.'); });
// ==================== UC04–UC05 — DANH SÁCH BÁNH / TÌM KIẾM BÁNH ====================
async function loadCatalog() {
    const q = new URLSearchParams({ search: $('#search').value, page: state.page, pageSize: 6 }); if ($('#category').value) q.set('categoryId', $('#category').value);
    $('#catalog').innerHTML = empty('Đang tải danh sách bánh…');
    const data = await api('/cakes?' + q); state.cakes = data.items; state.total = data.total;
    $('#catalog-count').textContent = data.total + ' món bánh đang bán'; $('#page-label').textContent = `Trang ${state.page} / ${Math.max(1, Math.ceil(data.total / 6))}`; $('#prev-page').disabled = state.page <= 1; $('#next-page').disabled = state.page * 6 >= data.total;
    $('#catalog').innerHTML = data.items.length ? data.items.map(c => `<article class="cake-card"><div class="cake-image"><img src="${esc(c.image || '/images/bakery.svg')}" alt="${esc(c.name)}" loading="lazy"><span class="category-pill">${esc(state.categories.find(x => x.id === c.categoryId)?.name || 'Bánh của tiệm')}</span></div><div class="cake-content"><h3>${esc(c.name)}</h3><p>${esc(c.description)}</p><div class="cake-bottom"><span><span class="price">${money(c.price)}</span><small class="stock-note">Còn ${c.available} bánh khả dụng</small></span><button class="add-cart" data-add="${c.id}" ${c.available <= 0 ? 'disabled' : ''}>${c.available > 0 ? 'Thêm vào giỏ +' : 'Tạm hết bánh'}</button></div></div></article>`).join('') : empty('Không tìm thấy bánh phù hợp. Hãy thử từ khóa khác.');
    $('#catalog').querySelectorAll('[data-add]').forEach(b => b.onclick = () => addToCart(Number(b.dataset.add)));
}
submit($('#search-form'), async () => { state.page = 1; await loadCatalog(); }); $('#category').onchange = () => safe(async () => { state.page = 1; await loadCatalog(); });
$('#prev-page').onclick = () => safe(async () => { state.page--; await loadCatalog(); }); $('#next-page').onclick = () => safe(async () => { state.page++; await loadCatalog(); });
// ==================== UC06–UC07 — GIỎ HÀNG VÀ ĐẶT COD ====================
// Giỏ theo tài khoản — không dùng giỏ của khách hàng khác sau đăng xuất.
const cartKey = () => `tb-cart-${state.user?.id || 'guest'}`;
function cart() { try { return JSON.parse(localStorage.getItem(cartKey()) || '[]'); } catch { return []; } }
function saveCart(items) { localStorage.setItem(cartKey(), JSON.stringify(items)); sessionStorage.removeItem('tb-checkout'); updateCartCount(); }
function updateCartCount() { $('#cart-count').textContent = cart().reduce((n, x) => n + x.quantity, 0); }
function canShop() { return !state.user || state.user.role === 'Customer'; }
function addToCart(id) { if (!canShop()) { toast('Chức năng mua bánh dành cho khách hàng.'); return; } const c = state.cakes.find(x => x.id === id); const items = cart(); const line = items.find(x => x.cakeId === id); if ((line?.quantity || 0) >= c.available) { toast('Số lượng vượt tồn khả dụng.'); return; } if (line) line.quantity++; else items.push({ cakeId: id, name: c.name, price: c.price, quantity: 1 }); saveCart(items); toast('Đã thêm ' + c.name + ' vào giỏ.'); }
function renderCart() {
    const items = cart(); $('#cart-lines').innerHTML = items.length ? '<h2>Bánh trong giỏ</h2>' + items.map(l => `<div class="cart-line"><div><h3>${esc(l.name)}</h3><small>${money(l.price)} / bánh</small></div><input aria-label="Số lượng ${esc(l.name)}" data-quantity="${l.cakeId}" type="number" min="1" max="10000" value="${l.quantity}"><button type="button" class="link" data-remove="${l.cakeId}">Bỏ bánh</button></div>`).join('') : empty('Giỏ hàng đang trống. Hãy chọn một món bánh bạn thích.');
    // Tính tiền bánh / Tính tổng COD — preview; giá quyết định được tính lại tại Business.
    const subtotal = items.reduce((sum, l) => sum + l.price * l.quantity, 0); $('#cart-total').innerHTML = `<div><span>Tiền bánh</span><span>${money(subtotal)}</span></div><div><span>Phí giao hàng</span><span>${money(20000)}</span></div><div><span>Tổng COD dự kiến</span><span>${money(subtotal + (items.length ? 20000 : 0))}</span></div>`;
    $('#place-order').disabled = !items.length || !canShop();
    for (const k of ['recipient', 'phone', 'address']) if (!$('#checkout-form').elements[k].value) $('#checkout-form').elements[k].value = state.user?.[k === 'recipient' ? 'fullName' : k] || '';
    document.querySelectorAll('[data-quantity]').forEach(el => el.onchange = () => { const n = Number(el.value); if (!Number.isInteger(n) || n < 1 || n > 10000) { toast('Số lượng từ 1 đến 10.000.'); renderCart(); return; } const a = cart(); a.find(x => x.cakeId === Number(el.dataset.quantity)).quantity = n; saveCart(a); renderCart(); });
    document.querySelectorAll('[data-remove]').forEach(el => el.onclick = () => { saveCart(cart().filter(x => x.cakeId !== Number(el.dataset.remove))); renderCart(); });
}
submit($('#checkout-form'), async f => {
    if (!canShop()) throw Error('Chức năng đặt bánh dành cho khách hàng.'); const draft = { ...Object.fromEntries(f), lines: cart().map(x => ({ cakeId: x.cakeId, quantity: x.quantity })) }; const fingerprint = JSON.stringify(draft); let previous = JSON.parse(sessionStorage.getItem('tb-checkout') || 'null');
    // Chống gửi lặp — giữ nguyên RequestId khi phản hồi bị mất hoặc gửi lại cùng nội dung.
    if (!previous || previous.fingerprint !== fingerprint) previous = { fingerprint, requestId: crypto.randomUUID() }; sessionStorage.setItem('tb-checkout', JSON.stringify(previous));
    const guest = !state.user;
    const order = await api(guest ? '/orders/guest' : '/orders', 'POST', { ...draft, requestId: previous.requestId }); saveCart([]); $('#checkout-form').reset(); toast('Đặt hàng COD thành công: ' + order.code); if (guest) { await navigate('cart'); $('#guest-receipt').textContent = 'Đã nhận đơn ' + order.code + ' · Tổng thanh toán khi nhận bánh: ' + money(order.total); $('#guest-receipt').hidden = false; } else await navigate('orders');
});
// ==================== UC08 / UC09 / UC14 — ĐƠN HÀNG ====================
async function loadOrders() {
    $('#orders-title').textContent = state.user.role === 'Employee' ? 'Xử lý đơn hàng' : state.user.role === 'Delivery' ? 'Giao hàng và ghi nhận COD' : 'Theo dõi đơn hàng';
    if (state.user.role === 'Employee') state.drivers = await api('/management/drivers'); state.orders = await api('/orders');
    $('#orders').innerHTML = state.orders.length ? state.orders.map(o => {
        const role = state.user.role, delivery = o.deliveries.at(-1); let actions = '';
        if (role === 'Customer' && o.status === 'Pending') actions = '<button class="button danger" data-action="cancel">Hủy đơn hàng</button>';
        if (role === 'Employee') {
            if (o.status === 'Pending') actions = '<button class="button" data-action="confirm">Xác nhận đơn hàng</button><input class="reason" placeholder="Lý do từ chối" aria-label="Lý do từ chối"><button class="button secondary" data-action="reject">Từ chối đơn hàng</button>';
            if (o.status === 'Confirmed') actions = '<button class="button" data-action="prepare">Bắt đầu chuẩn bị</button>';
            if (['Preparing', 'DeliveryFailed'].includes(o.status)) actions = `<select class="driver" aria-label="Người giao hàng">${state.drivers.map(d => `<option value="${d.id}">${esc(d.fullName)}</option>`).join('')}</select><button class="button" data-action="assign">${o.status === 'DeliveryFailed' ? 'Phân công giao lại' : 'Phân công giao hàng'}</button>`;
            if (o.status === 'DeliveryFailed') actions += '<input class="reason" placeholder="Kết quả kiểm bánh hoàn" aria-label="Lý do kết thúc"><button class="button danger" data-action="close-failed">Kết thúc đơn giao thất bại</button>';
        }
        if (role === 'Delivery') actions = `<input type="number" class="collected" min="0" value="${o.total}" aria-label="Tiền COD thực thu"><button class="button" data-action="complete">Giao thành công / Thu đủ COD</button><input class="reason" placeholder="Lý do giao thất bại" aria-label="Lý do giao thất bại"><button class="button secondary" data-action="fail">Giao thất bại</button>`;
        return `<article class="panel order-card" data-order="${o.id}"><div class="order-head"><div><h3>${esc(o.code)}</h3><small class="muted">${new Date(o.createdAt.endsWith('Z') ? o.createdAt : o.createdAt + 'Z').toLocaleString('vi-VN')}</small></div><span class="badge ${o.status === 'Cancelled' ? 'cancel' : ''}">${labels[o.status]}</span></div><p class="order-meta">${esc(o.recipient)} · ${esc(o.phone)}<br>${esc(o.address)}${o.note ? '<br>Ghi chú: ' + esc(o.note) : ''}</p><div class="order-lines">${o.lines.map(l => `<div><span>${esc(l.cakeName)} × ${l.quantity}</span><span>${money(l.unitPrice * l.quantity)}</span></div>`).join('')}<div><span>Phí giao hàng</span><span>${money(o.shipping)}</span></div><div><b>Tổng COD</b><b>${money(o.total)}</b></div></div>${role === 'Employee' && o.status === 'DeliveryFailed' ? '<div class="damaged">' + o.lines.map(l => `<label>Số hỏng: ${esc(l.cakeName)}<input type="number" data-damaged="${l.cakeId}" min="0" max="${l.quantity}" value="0"></label>`).join('') + '</div>' : ''}<div class="actions">${actions}</div><details class="history"><summary>Lịch sử xử lý${delivery ? ' · Lần giao ' + delivery.sequence : ''}</summary><ol>${o.history.map(h => `<li>${esc(labels[h.to] || h.to)} — ${esc(h.reason)}</li>`).join('')}</ol></details></article>`;
    }).join('') : empty('Chưa có đơn hàng trong phạm vi của bạn.');
    $('#orders').querySelectorAll('[data-action]').forEach(b => b.onclick = () => safe(async () => {
        b.disabled = true; try {
            const box = b.closest('[data-order]'); const o = state.orders.find(x => x.id === Number(box.dataset.order)); const action = b.dataset.action;
            if (['cancel', 'reject', 'close-failed'].includes(action) && !confirm('Xác nhận thao tác này với đơn ' + o.code + '?')) return;
            const body = { action, version: o.version, reason: box.querySelector('.reason')?.value, driverId: Number(box.querySelector('.driver')?.value) || null, deliveryAttemptId: o.deliveries.at(-1)?.id, collectedAmount: Number(box.querySelector('.collected')?.value), damagedLines: [...box.querySelectorAll('[data-damaged]')].map(x => ({ cakeId: Number(x.dataset.damaged), quantity: Number(x.value) })) };
            await api('/orders/' + o.id + '/actions', 'POST', body); toast('Đã cập nhật đơn hàng.'); await loadOrders();
        } finally { b.disabled = false; }
    }));
}
$('#refresh-orders').onclick = () => safe(loadOrders);
// ==================== UC11 — ĐỐI SOÁT COD / LẬP VÀ SỬA KỲ BÀN GIAO ====================
function receiptRows(items, editing = false) { return items.length ? items.map(r => `<div class="receipt-line" data-receipt="${r.id || r.receiptId}"><input type="checkbox" ${editing ? 'checked disabled' : ''} aria-label="Chọn phiếu ${r.id || r.receiptId}"><span>Phiếu #${r.id || r.receiptId}<small class="stock-note">Phải thu ${money(r.amount ?? r.expected)}</small></span><input class="received" type="number" min="0" step="0.01" value="${r.received ?? r.amount}" aria-label="Tiền thực nhận"><input class="note" value="${esc(r.note || '')}" placeholder="Ghi chú chênh lệch" aria-label="Ghi chú chênh lệch"></div>`).join('') : empty('Không có phiếu thu chưa bàn giao.'); }
async function loadCod() { state.receipts = await api('/management/receipts'); state.periods = await api('/management/periods'); state.editPeriod = null; $('#period-form-title').textContent = 'Lập kỳ bàn giao COD'; $('#receipts').innerHTML = receiptRows(state.receipts); $('#periods').innerHTML = state.periods.length ? state.periods.map(p => `<article class="panel period-card"><div class="order-head"><h3>Kỳ #${p.id}</h3><span class="badge">${labels[p.status]}</span></div><p>Phải thu: ${money(p.expected)} · Thực nhận: ${money(p.received)}</p>${p.status === 'Submitted' ? `<button class="button secondary" data-edit-period="${p.id}">Điều chỉnh kỳ chưa duyệt</button>` : ''}</article>`).join('') : empty('Chưa có kỳ đối soát COD.'); document.querySelectorAll('[data-edit-period]').forEach(b => b.onclick = () => { const p = state.periods.find(x => x.id === Number(b.dataset.editPeriod)); state.editPeriod = p; $('#period-form-title').textContent = 'Điều chỉnh kỳ chưa duyệt #' + p.id; $('#receipts').innerHTML = receiptRows(p.items, true); $('#period-form').scrollIntoView({ behavior: 'smooth' }); }); }
submit($('#period-form'), async () => { const items = [...$('#receipts').querySelectorAll('[data-receipt]')].filter(x => x.querySelector('[type=checkbox]').checked).map(x => ({ receiptId: Number(x.dataset.receipt), received: Number(x.querySelector('.received').value), note: x.querySelector('.note').value })); if (!items.length) throw Error('Vui lòng chọn ít nhất một phiếu thu.'); const p = state.editPeriod; await api('/management/periods' + (p ? '/' + p.id : ''), p ? 'PUT' : 'POST', { items, version: p?.version || 0 }); toast('Đã lưu kỳ bàn giao COD.'); await loadCod(); });
$('#refresh-cod').onclick = $('#period-reset').onclick = () => safe(loadCod);
// ==================== UC15 — TRA CỨU KHÁCH HÀNG / TÌM KIẾM KHÁCH HÀNG ====================
async function loadCustomers() { const users = await api('/management/users/customers?search=' + encodeURIComponent($('#customer-search').value)); $('#customers').innerHTML = users.length ? '<table><thead><tr><th>Khách hàng</th><th>Tài khoản</th><th>Số điện thoại</th><th>Trạng thái</th></tr></thead><tbody>' + users.map(u => `<tr><td>${esc(u.fullName)}</td><td>${esc(u.userName)}</td><td>${esc(u.phone)}</td><td>${u.active ? 'Hoạt động' : 'Đã khóa'}</td></tr>`).join('') + '</tbody></table>' : empty('Không tìm thấy khách hàng.'); }
submit($('#customer-search-form'), loadCustomers);
// Khởi động — phục hồi phiên bằng /auth/me; giỏ không được coi là giữ tồn.
safe(async () => { if (state.token) { try { state.user = await api('/auth/me'); } catch { state.token = null; sessionStorage.removeItem('tb-token'); } } userUI(); state.categories = await api('/cakes/categories'); $('#category').innerHTML = '<option value="">Tất cả danh mục</option>' + state.categories.map(c => `<option value="${c.id}">${esc(c.name)}</option>`).join(''); await navigate(state.user?.role === 'Admin' ? 'admin' : state.user && state.user.role !== 'Customer' ? 'orders' : 'catalog'); });
