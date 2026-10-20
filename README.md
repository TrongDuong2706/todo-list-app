# Todo List — Vue 3 + .NET 10 + PostgreSQL

Ứng dụng đơn giản: thêm, sửa, xóa và đánh dấu hoàn thành công việc. Dữ liệu lưu trong PostgreSQL. Có kiểm tra tên công việc (1–200 ký tự), trạng thái tải/lỗi và giao diện tiếng Việt dùng được trên điện thoại.

```text
frontend/       Vue 3 + Vite, Dockerfile + Nginx
backend/        ASP.NET Core Minimal API + EF Core + Npgsql, Dockerfile
compose.yaml    Chạy frontend, backend và PostgreSQL
```

## 1. Chạy toàn bộ app bằng Docker

Chỉ cần Docker và Docker Compose. Chạy từ thư mục gốc:

```bash
# Tùy chọn: sao chép nếu muốn thay cấu hình mặc định.
cp .env.example .env
docker compose up -d --build
```

Compose tự tạo PostgreSQL, database/tài khoản, network và volume lưu dữ liệu. Backend chờ PostgreSQL healthy rồi tự tạo bảng.

- Giao diện: **http://localhost:5173**
- API: **http://localhost:5000/api/todos**
- Kiểm tra database: **http://localhost:5000/api/health**
- PostgreSQL từ máy host: `localhost:5432`, database `todo_db`, user `todo_user`, password `todo_password`.

```bash
docker compose ps
docker compose logs -f
docker compose down
```

`docker compose down` dừng/xóa cả ba container nhưng giữ dữ liệu trong volume `postgres_data`. Chạy `docker compose up -d` để khởi động lại. Lệnh `docker compose down -v` sẽ xóa cả volume và dữ liệu.

Sửa `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_PORT` trong `.env` nếu cần. `DB_PORT` chỉ là cổng truy cập từ máy host; backend Docker luôn kết nối `postgres:5432`. Mật khẩu mẫu dùng cho localhost. Với volume đã có dữ liệu, thay biến môi trường không tự đổi tài khoản/mật khẩu/database cũ.

Nếu đã tạo PostgreSQL riêng theo hướng dẫn cũ và nó chiếm cổng 5432, dừng container đó hoặc đặt `DB_PORT=5433` trong `.env`. Compose tạo database mới; không tự chuyển dữ liệu từ container cũ.

## 2. Chạy code trực tiếp để phát triển


Cần **.NET SDK 10** và **Node.js 22.18+ hoặc 24.12+**, chạy riêng PostgreSQL bằng Compose:

```bash
docker compose up -d postgres
```

Nếu trước đó đã chạy cả app bằng Compose, dừng frontend/backend để nhường cổng:

```bash
docker compose stop frontend backend
```

Terminal 1:

```bash
cd backend
dotnet restore
dotnet run --launch-profile http
```

Terminal 2:

```bash
cd frontend
npm ci
npm run dev
```

Mở **http://localhost:5173**. Vite chuyển tiếp `/api` tới `http://localhost:5000`, nên không cần cấu hình CORS. Trong Docker, Nginx chuyển tiếp `/api` tới `backend:8080`.

Khi cần thay cấu hình:

- Backend chạy trực tiếp: sửa `backend/appsettings.json`, mục `ConnectionStrings:DefaultConnection`, hoặc đặt biến môi trường `ConnectionStrings__DefaultConnection`.
- Backend Docker: sửa các biến `DB_*` trong `.env` ở thư mục gốc rồi chạy lại `docker compose up -d`.
- Frontend dev: sao chép `frontend/.env.example` thành `frontend/.env`, đổi `API_PROXY_TARGET` nếu backend dùng cổng khác, rồi khởi động lại Vite.
- `.env` ở thư mục gốc chỉ được Docker Compose đọc; `dotnet run` dùng appsettings/biến môi trường của shell.

## 3. Deploy frontend trên Kubernetes

Trình duyệt vẫn chỉ gọi `/api/todos`. Nginx trong pod frontend sẽ chuyển tiếp request đó đến Kubernetes Service của backend. Vì vậy tên Service nội bộ không xuất hiện trong mã Vue và không cần mở CORS.

Image frontend nhận biến môi trường runtime `API_UPSTREAM`. Đặt biến này trong Deployment frontend theo tên và cổng Service backend của bạn. Nếu cả hai ở cùng namespace, dùng tên ngắn của Service:

```yaml
env:
  - name: API_UPSTREAM
    value: http://todo-backend:8080
```

Ví dụ nếu Service backend là `todo-backend` ở namespace `todo`, dùng địa chỉ đầy đủ:

```yaml
env:
  - name: API_UPSTREAM
    value: http://todo-backend.todo.svc.cluster.local:8080
```

`API_UPSTREAM` phải bao gồm `http://` hoặc `https://`, không thêm `/api` ở cuối vì frontend đã gửi đường dẫn `/api/...`. Sau khi đổi biến môi trường, rollout lại Deployment frontend; không cần build lại image.

## 4. Kết nối backend với PostgreSQL ngoài Kubernetes

Cấu hình mặc định của backend dùng PostgreSQL tại `10.30.0.106:5432`. Khi deploy, nên đặt chuỗi kết nối đầy đủ qua biến môi trường của Deployment backend để không đưa thông tin đăng nhập vào image:

```yaml
env:
  - name: ConnectionStrings__DefaultConnection
    value: Host=10.30.0.106;Port=5432;Database=todo_db;Username=todo_user;Password=todo_password
```

Thay `Database`, `Username` và `Password` bằng thông tin PostgreSQL thực tế. Pod backend cần có network route đến `10.30.0.106:5432`, và PostgreSQL phải cho phép địa chỉ mạng của cluster kết nối qua `pg_hba.conf` cùng firewall/security group.

Ví dụ đổi kết nối cho backend chạy trực tiếp (Bash):

```bash
cd backend
ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=todo_db;Username=todo_user;Password=todo_password' dotnet run
```

## API

| Method | Endpoint | Chức năng |
| --- | --- | --- |
| GET | `/api/health` | Kiểm tra kết nối database |
| GET | `/api/todos` | Danh sách, mới nhất trước |
| GET | `/api/todos/{id}` | Chi tiết |
| POST | `/api/todos` | Tạo công việc |
| PUT | `/api/todos/{id}` | Sửa tên/trạng thái |
| DELETE | `/api/todos/{id}` | Xóa công việc |

Body POST/PUT:

```json
{ "title": "Học VueJS", "isCompleted": false }
```

PUT gửi cả `title` và `isCompleted`. API trả 400 khi tên rỗng/quá dài, 404 khi không tìm thấy; DELETE thành công trả 204.

```bash
curl -X POST http://localhost:5000/api/todos \
  -H 'Content-Type: application/json' \
  -d '{"title":"Hoc VueJS","isCompleted":false}'
curl http://localhost:5000/api/todos
```

## Database và xử lý lỗi

Kiểm thử API đang chạy (cần Python 3; script tự xóa bản ghi kiểm thử của nó):

```bash
python3 backend/smoke_test.py http://localhost:5000
# Kiểm tra cả proxy frontend:
python3 backend/smoke_test.py http://localhost:5173
```

Backend dùng `EnsureCreatedAsync()` để tạo schema lần đầu trong database riêng `todo_db`; không cần chạy lệnh migration. PostgreSQL phải sẵn sàng trước khi backend khởi động. Đây là cách khởi tạo đơn giản cho demo: khi thay đổi model về sau, cần chuyển sang EF migrations hoặc tạo database demo mới; `EnsureCreated` không nâng cấp bảng đã có. Không dùng chung database đã có bảng của ứng dụng khác.

- `Connection refused`: kiểm tra PostgreSQL đã chạy, đúng port và network.
- `password authentication failed`: kiểm tra tài khoản/mật khẩu thật của database, đặc biệt nếu đang dùng lại volume cũ.
- Frontend báo lỗi API: xem `docker compose logs backend`; khi dev kiểm tra terminal `dotnet run`.
- Cổng bị chiếm: đổi cổng host trong Compose; nếu đổi cổng PostgreSQL, cập nhật cấu hình tương ứng. Trong cùng Docker network, PostgreSQL vẫn dùng cổng nội bộ 5432.

Ứng dụng dành cho localhost, chưa có đăng nhập/phân quyền. Không public trực tiếp lên Internet.

## Kiểm tra đã thực hiện

- Frontend: `npm ci`, build production và build Docker image thành công.
- Backend: `dotnet publish -c Release` thành công với SDK 10.0.100.
- `docker compose config --quiet` hợp lệ.
- Smoke test CRUD/validation/404 đạt với PostgreSQL 17 tạm, cả gọi API trực tiếp và qua Nginx frontend; trang chủ trả HTTP 200.
- Docker image backend đã build thành công; cả frontend/backend đã chạy bằng Compose và vượt qua smoke test với PostgreSQL 17.
- Bản ghi trong PostgreSQL vẫn còn nguyên sau khi restart container backend.
- Chưa kiểm thử thao tác bằng trình duyệt.

Chạy toàn bộ app theo bước 1; không cần tạo PostgreSQL hay network thủ công.

Tài liệu nền tảng: [Vue](https://vuejs.org/guide/quick-start), [Npgsql EF Core 10](https://www.npgsql.org/efcore/release-notes/10.0.html).
