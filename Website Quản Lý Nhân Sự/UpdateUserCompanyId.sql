-- Script: Cập nhật CompanyId cho các user Admin/HR từ công ty DEMO01
-- Mục đích: Sửa lỗi không thể thêm/xóa/sửa nhân viên vì user chưa có CompanyId

-- Bước 1: Lấy CompanyId của công ty DEMO01
DECLARE @DemoCompanyId INT;
SELECT @DemoCompanyId = Id FROM Companies WHERE Code = 'DEMO01';

IF @DemoCompanyId IS NULL
BEGIN
	PRINT 'LỖI: Không tìm thấy công ty DEMO01 trong database!'
	RETURN;
END

PRINT 'Tìm thấy CompanyId: ' + CONVERT(VARCHAR, @DemoCompanyId);

-- Bước 2: Cập nhật CompanyId cho user Admin Demo
UPDATE AspNetUsers 
SET CompanyId = @DemoCompanyId
WHERE Email = 'admin@demo-hrms.local' AND CompanyId IS NULL;

PRINT 'Cập nhật user admin@demo-hrms.local: ' + 
	  CASE WHEN @@ROWCOUNT > 0 THEN 'THÀNH CÔNG' ELSE 'Đã có CompanyId hoặc không tìm thấy' END;

-- Bước 3: Cập nhật CompanyId cho user HR Demo
UPDATE AspNetUsers 
SET CompanyId = @DemoCompanyId
WHERE Email = 'hr@demo-hrms.local' AND CompanyId IS NULL;

PRINT 'Cập nhật user hr@demo-hrms.local: ' + 
	  CASE WHEN @@ROWCOUNT > 0 THEN 'THÀNH CÔNG' ELSE 'Đã có CompanyId hoặc không tìm thấy' END;

-- Bước 4: Kiểm tra kết quả
PRINT '';
PRINT '=== KẾT QUẢ KIỂM TRA ===';
SELECT 
	Id,
	UserName,
	Email,
	CompanyId,
	CASE 
		WHEN CompanyId IS NULL THEN '❌ Chưa có CompanyId'
		ELSE '✅ Đã có CompanyId: ' + CONVERT(VARCHAR, CompanyId)
	END AS Status
FROM AspNetUsers 
WHERE Email IN ('admin@demo-hrms.local', 'hr@demo-hrms.local');

PRINT '';
PRINT '✅ Script hoàn thành! Vui lòng reload trang web để thay đổi có hiệu lực.';
