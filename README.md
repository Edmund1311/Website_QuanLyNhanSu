
Tài liệu Dự án Quản lý Nhân sự
1. Danh sách tài liệu
-LOGIC_FIXES_SUMMARY.md: Chi tiết các phần sửa đổi logic nghiệp vụ.
-CHANGES_QUICK_REFERENCE.md: Tra cứu nhanh các tệp tin đã chỉnh sửa.
-DATABASE_CONFIG.md: Cấu trúc cơ sở dữ liệu và các ràng buộc.
-DEPLOYMENT_GUIDE.md: Hướng dẫn cài đặt chi tiết trên Azure, VPS và Docker.
-DEPLOYMENT_QUICK_CHECKLIST.md: Danh sách câu lệnh triển khai nhanh.
-SECURITY_HARDENING_CHECKLIST.md: Các cấu hình bảo mật bắt buộc.
-DEPLOYMENT_SUMMARY.md: Tổng kết và lưu ý sau triển khai.
2. Hướng dẫn nhanh theo vai trò
-Quản lý / Lead: Đọc LOGIC_FIXES_SUMMARY.md (nghiệp vụ) và DEPLOYMENT_SUMMARY.md (vận hành).
-Kỹ thuật / DevOps: Đọc DEPLOYMENT_GUIDE.md và làm theo các lệnh trong DEPLOYMENT_QUICK_CHECKLIST.md.
An toàn thông tin / DBA: Đọc DATABASE_CONFIG.md (Cơ sở dữ liệu) và SECURITY_HARDENING_CHECKLIST.md (Bảo mật).
3. Các bước triển khai cơ bản
-Bảo mật cấu hình: Chuyển toàn bộ mật khẩu, chuỗi kết nối và khóa JWT sang biến môi trường (Environment Variables) thay vì để trong file cấu hình tĩnh.
-Triển khai: Lựa chọn môi trường phù hợp (Azure, VPS hoặc Docker) và thực hiện theo hướng dẫn tương ứng trong DEPLOYMENT_GUIDE.md.
-Bảo mật kết nối: Cài đặt chứng chỉ SSL (HTTPS) và cấu hình sao lưu cơ sở dữ liệu định kỳ trước khi đưa hệ thống vào vận hành thực tế.
