const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../src/TiemBanh.Web/wwwroot');
const html = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
const asset = (name) => path.join(root, decodeURIComponent(name.replace(/^\//, '')));

test('Trang chủ khai báo ngôn ngữ và biểu mẫu tìm kiếm', () => {
  assert.match(html, /<html\s+lang="vi"/i);
  assert.match(html, /id="search-form"/);
});
test('Các tệp CSS/JS được HTML tham chiếu phải tồn tại', () => {
  const refs = [...html.matchAll(/(?:src|href)="(\/(?:js|css)\/[^"?#]+)"/g)].map(m => m[1]);
  assert.ok(refs.length >= 2, 'Thiếu tham chiếu CSS/JS');
  for (const ref of refs) assert.ok(fs.existsSync(asset(ref)), `Thiếu tài nguyên: ${ref}`);
});
test('Ảnh nội bộ được HTML tham chiếu phải tồn tại', () => {
  const refs = [...html.matchAll(/<img\b[^>]*\bsrc="(\/images\/[^"?#]+)"/gi)].map(m => m[1]);
  for (const ref of refs) assert.ok(fs.existsSync(asset(ref)), `Thiếu ảnh: ${ref}`);
});
test('Các biểu mẫu và trang cơ bản phải có', () => {
  for (const id of ['page-catalog', 'search-form', 'cart-count']) {
    assert.ok(html.includes(`id="${id}"`), `Thiếu phần tử ${id}`);
  }
});
