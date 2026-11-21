# Hợp đồng API và giao tiếp

Base `/api`, JSON UTF-8. `Authorization: Bearer <token>`. Giá và quyền do server quyết định. Tên trường camelCase. Ngày thời điểm lưu UTC; UI chuyển giờ địa phương. Swagger được bật ở Development.

| HTTP | Route | Vai trò / chức năng |
|---|---|---|
| POST | /auth/login; /auth/register | Công khai; giới hạn 15 lần/phút/IP |
| GET, PUT | /auth/me | Hồ sơ người hiện tại |
| GET | /cakes; /cakes/categories | Công khai; search/categoryId/page/pageSize |
| GET | /cakes/manage | Admin/Employee; danh sách quản trị có tồn và version |
| POST; PUT | /cakes; /cakes/{id} | Admin; CakeRequest |
| PUT | /cakes/{id}/stock | Admin; {onHand,version,reason} |
| POST | /cakes/images | Admin; multipart file, max 5 MB, JPG/PNG/WebP signature |
| GET; POST | /orders | Customer/Employee/Delivery theo phạm vi; POST chỉ Customer |
| POST | /orders/{id}/actions | {action,version,driverId?,deliveryAttemptId?,collectedAmount?,reason?,damagedLines?} |
| GET | /management/drivers | Employee |
| GET | /management/users/customers | Employee/Admin, search |
| GET | /management/users/staff | Admin |
| POST; PUT | /management/staff; /management/staff/{id} | Admin |
| PUT | /management/users/{id}/active | Admin; {active,version} |
| GET | /management/receipts; /management/periods | Employee/Admin |
| POST; PUT | /management/periods; /management/periods/{id} | Employee; {items:[{receiptId,received,note}],version} |
| POST | /management/periods/{id}/approve; /lock | Admin; {version} |
| GET | /management/report | Admin |

## Đặt hàng COD

```json
{"requestId":"<UUID mới cho nội dung mới>","lines":[{"cakeId":1,"quantity":2}],"recipient":"Người nhận","phone":"0900000000","address":"Địa chỉ","note":""}
```

Cùng nội dung gửi lại phải dùng cùng RequestId. Đơn đã tồn tại trả cùng Id; payload khác trả 409 REQUEST_REUSED. Phiên bản hiện tại trả 201 cho POST thành công cả trường hợp nhận lại đơn cũ; client dựa Id, không suy ra số đơn từ số phản hồi 201. Không gửi giá/tổng để server tin cậy.

Lệnh assign: driverId; complete/fail: deliveryAttemptId của lần giao mới nhất; complete: collectedAmount phải bằng Total; reject/fail/close-failed: reason. close-failed có damagedLines với số hỏng từng CakeId, chỉ cần gửi dòng có hỏng hoặc gửi 0.

## Mã HTTP

400 dữ liệu sai; 401 cần đăng nhập; 403 thiếu quyền; 404 ngoài phạm vi/không tồn tại; 409 sai trạng thái, Version, tồn hoặc ràng buộc; 429 giới hạn đăng nhập; 500 lỗi nội bộ. Health ready trả 503 khi SQL/schema chưa sẵn sàng. Lỗi nghiệp vụ có code và traceId theo Problem Details; lỗi validation theo chuẩn ApiController.

## Thứ tự gọi an toàn

Client đọc đối tượng → gửi Version đang thấy → server kiểm quyền/phạm vi/trạng thái → transaction → trả phiên bản mới. Khi 409, tải lại; đặt hàng gặp mất phản hồi phải giữ RequestId. Không tự tăng Version ở UI để thử vượt kiểm tra.

## Cấu trúc Controller trên GitHub (21/11/2026)

Bản ZIP bàn giao đặt nhiều Controller trong `src/TiemBanh.Api/Controllers.cs`. Repository hiện tại tổ chức thành các file riêng: `AuthController.cs`, `CakesController.cs`, `OrdersController.cs`, `ManagementController.cs`. Việc tách file không yêu cầu thay đổi URL; route thực tế vẫn do attribute routing trong từng lớp quyết định. Không thêm `Controllers.cs` nguyên khối khi các lớp đã được tách vì có nguy cơ khai báo trùng.

Khi kiểm thử qua Docker Compose, API local dự kiến dùng `http://localhost:8081`, Web `http://localhost:8080`; Web chuyển tiếp `/api/*` đến API qua YARP. Swagger chỉ bật khi ASP.NET Core ở môi trường Development. Các endpoint cần kiểm tra với quyền và dữ liệu thực tế, không suy ra chức năng hoàn chỉnh chỉ từ danh sách route.
