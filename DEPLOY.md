# Hướng dẫn deploy HRMS SaaS (Sinh viên)

## Yêu cầu
- Visual Studio 2022 hoặc .NET 10 SDK
- SQL Server LocalDB (dev) hoặc Azure SQL (production)

## Chạy local
```powershell
cd "Website Quản Lý Nhân Sự"
dotnet restore
dotnet ef database update
dotnet run
```
Mở: http://localhost:5122

### Tài khoản demo
| Vai trò | Email | Mật khẩu |
|---|---|---|
| Super Admin | admin@hrms.local | Admin@123 |
| Company Admin | admin@demo-hrms.local | Admin@123 |
| HR | hr@demo-hrms.local | Hr@123456 |
| Employee | employee@demo-hrms.local | Employee@123 |

Mã công ty demo: `DEMO01`

## Deploy lên Azure (khuyến nghị)

### 1. Tạo Azure SQL Database
1. Vào [Azure Portal](https://portal.azure.com)
2. Tạo **SQL Database** → chọn tier Basic/Standard
3. Tạo Server + admin password
4. Firewall: bật **Allow Azure services**

### 2. Tạo App Service
1. Tạo **Web App** (Windows, .NET 10)
2. Vào **Configuration → Connection strings**
3. Thêm `DefaultConnection`:
```
Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=HRMS_V1_Db;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 3. Publish từ Visual Studio
1. Chuột phải project → **Publish**
2. Chọn **Azure → Azure App Service (Windows)**
3. Chọn App vừa tạo → **Publish**

### 4. Migration database
Chạy từ máy local (đã allow IP):
```powershell
$env:ConnectionStrings__DefaultConnection="Server=tcp:...;"
dotnet ef database update
```
Hoặc app tự migrate khi khởi động (đã cấu hình trong `Program.cs`).

## Lưu ý
- Không commit password vào git
- Upload file lưu tại `wwwroot/uploads` — với production nên dùng Azure Blob Storage nếu cần mở rộng
- Nếu publish lỗi, kiểm tra runtime .NET 10 trên App Service
