# Database Configuration - Website Quản Lý Nhân Sự

## 1. Connection String

### Development (Local)
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=localhost;Database=WebsiteQL_NhanSu;Integrated Security=true;TrustServerCertificate=true;Encrypt=false"
  }
}
```

### Production
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=YOUR_SERVER;Database=WebsiteQL_NhanSu;User Id=sa;Password=YOUR_PASSWORD;Encrypt=true;TrustServerCertificate=false"
  }
}
```

**Yêu cầu**: 
- SQL Server 2019 trở lên
- .NET 10 runtime
- Entity Framework Core 10.0.9

---

## 2. Database Schema Overview

### Bảng và Quan Hệ

```
┌─────────────────────────────────────────────────────────────┐
│                    ASPNETCORE IDENTITY                      │
├─────────────────────────────────────────────────────────────┤
│ AspNetUsers (ApplicationUser)                               │
│ AspNetRoles                                                 │
│ AspNetUserRoles                                             │
│ AspNetUserClaims, AspNetUserLogins, AspNetUserTokens       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     BUSINESS CORE                            │
├─────────────────────────────────────────────────────────────┤
│ Companies (1) ──────────────┐                               │
│   ├─ Departments           │                               │
│   ├─ Positions             │ 1:N                           │
│   ├─ Employees             │                               │
│   └─ JoinCompanyRequests   │                               │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                   EMPLOYEE MANAGEMENT                        │
├─────────────────────────────────────────────────────────────┤
│ Employees (1) ─────────────┐                               │
│   ├─ Attendances          │                               │
│   ├─ LeaveRequests        │ 1:N                           │
│   ├─ Contracts            │                               │
│   ├─ Salaries             │                               │
│   └─ EmployeeMedias       │                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Bảng Chi Tiết

### 3.1 Companies
```sql
CREATE TABLE [Companies] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[Name] nvarchar(200) NOT NULL,
	[Code] nvarchar(50) NOT NULL UNIQUE,
	[Status] int NOT NULL DEFAULT 0,  -- 0=Active, 1=Locked
	[Phone] nvarchar(20),
	[Email] nvarchar(200),
	[Address] nvarchar(500),
	[TaxCode] nvarchar(50),
	[Website] nvarchar(200),
	[LogoPath] nvarchar(500),
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX [IX_Companies_Code] ON [Companies]([Code]);
```

**Giải Thích**:
- `Status`: 0=Active (hoạt động), 1=Locked (bị khóa)
- `Code`: Mã công ty unique, dùng cho URLs/APIs
- `IsDeleted`: Soft delete flag (khi true, record ẩn khỏi queries bình thường)

---

### 3.2 Departments
```sql
CREATE TABLE [Departments] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[CompanyId] int NOT NULL,
	[Name] nvarchar(200) NOT NULL,
	[Code] nvarchar(50) NOT NULL,
	[Description] nvarchar(500),
	[HeadEmployeeId] int NULL,
	[Status] int NOT NULL DEFAULT 0,  -- 0=Active, 1=Inactive
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id]) ON DELETE CASCADE,
	FOREIGN KEY ([HeadEmployeeId]) REFERENCES [Employees]([Id]) ON DELETE SET NULL
);

CREATE UNIQUE INDEX [IX_Departments_CompanyId_Code] ON [Departments]([CompanyId], [Code]);
```

**Giải Thích**:
- `CompanyId`: Khóa ngoài đến Companies (cascade delete)
- `Code`: Mã phòng ban unique per company
- `HeadEmployeeId`: Người đứng đầu phòng ban (nullable, SET NULL nếu xóa nhân viên)

---

### 3.3 Positions
```sql
CREATE TABLE [Positions] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[CompanyId] int NOT NULL,
	[Name] nvarchar(200) NOT NULL,
	[Code] nvarchar(50) NOT NULL,
	[BaseSalary] decimal(18,2) NOT NULL,
	[Description] nvarchar(500),
	[Status] int NOT NULL DEFAULT 0,  -- 0=Active, 1=Inactive
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Positions_CompanyId_Code] ON [Positions]([CompanyId], [Code]);
```

**Giải Thích**:
- `BaseSalary`: Lương cơ bản theo position (decimal 18,2 = max 9,999,999,999.99)
- Unique per company

---

### 3.4 Employees
```sql
CREATE TABLE [Employees] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[CompanyId] int NOT NULL,
	[UserId] nvarchar(450) NOT NULL,
	[EmployeeCode] nvarchar(50) NOT NULL,
	[FullName] nvarchar(200) NOT NULL,
	[DateOfBirth] date NOT NULL,
	[Email] nvarchar(200) NOT NULL,
	[Phone] nvarchar(20),
	[Address] nvarchar(500),
	[Gender] int NOT NULL,  -- 0=Male, 1=Female, 2=Other
	[IdentityCardNumber] nvarchar(50),
	[IdentityCardIssueDate] date,
	[IdentityCardIssuePlace] nvarchar(200),
	[TaxCode] nvarchar(50),
	[AvatarPath] nvarchar(500),
	[DepartmentId] int NOT NULL,
	[PositionId] int NOT NULL,
	[HireDate] date NOT NULL,
	[LeaveDate] date NULL,
	[WorkStatus] int NOT NULL DEFAULT 0,  -- 0=Working, 1=OnLeave, 2=Resigned
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id]) ON DELETE CASCADE,
	FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
	FOREIGN KEY ([DepartmentId]) REFERENCES [Departments]([Id]) ON DELETE RESTRICT,
	FOREIGN KEY ([PositionId]) REFERENCES [Positions]([Id]) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX [IX_Employees_CompanyId_EmployeeCode] ON [Employees]([CompanyId], [EmployeeCode]);
CREATE UNIQUE INDEX [IX_Employees_CompanyId_Email] ON [Employees]([CompanyId], [Email]);
```

**Giải Thích**:
- `EmployeeCode`: NV001, NV002, v.v. (unique per company)
- `Email`: Unique per company (mỗi công ty có 1 email unique)
- `WorkStatus`: 0=Đang làm, 1=Tạm nghỉ, 2=Đã thôi việc
- FK đến Department/Position: `ON DELETE RESTRICT` (không xóa được nếu có nhân viên)
- Soft delete: `IsDeleted` flag

---

### 3.5 Attendances
```sql
CREATE TABLE [Attendances] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[EmployeeId] int NOT NULL,
	[WorkDate] date NOT NULL,
	[CheckInTime] time NOT NULL,
	[CheckOutTime] time NULL,
	[Status] int NOT NULL DEFAULT 0,  -- 0=Present, 1=Late, 2=Absent, 3=Leave
	[Note] nvarchar(500),
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_Attendances_EmployeeId_WorkDate] ON [Attendances]([EmployeeId], [WorkDate]);
```

**Giải Thích**:
- `WorkDate`: Ngày làm việc
- `CheckInTime`: Thời gian vào (08:30 có trễ)
- `CheckOutTime`: Thời gian ra (nullable)
- `Status`: 
  - 0 = Present (vào đúng giờ)
  - 1 = Late (vào muộn)
  - 2 = Absent (không vào)
  - 3 = Leave (đã xin phép nghỉ)
- `Unique(EmployeeId, WorkDate)`: Không duplicate attendance cùng ngày
- FK: `ON DELETE NO ACTION` (không xóa đơn giản nếu có attendance)

---

### 3.6 LeaveRequests
```sql
CREATE TABLE [LeaveRequests] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[EmployeeId] int NOT NULL,
	[LeaveType] int NOT NULL,  -- 0=Casual, 1=Sick, 2=Maternity, 3=Annual
	[StartDate] date NOT NULL,
	[EndDate] date NOT NULL,
	[Reason] nvarchar(500),
	[Status] int NOT NULL DEFAULT 0,  -- 0=Pending, 1=Approved, 2=Rejected
	[ApprovedBy] nvarchar(450) NULL,
	[ApprovedAt] datetime2 NULL,
	[ApprovalReason] nvarchar(500),
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE NO ACTION,
	FOREIGN KEY ([ApprovedBy]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL
);

CREATE INDEX [IX_LeaveRequests_EmployeeId_Status] ON [LeaveRequests]([EmployeeId], [Status]);
```

**Giải Thích**:
- `LeaveType`: 
  - 0 = Casual (phép thường)
  - 1 = Sick (phép ốm)
  - 2 = Maternity (phép thai sản)
  - 3 = Annual (phép năm)
- `Status`: 0=Pending (chờ), 1=Approved (duyệt), 2=Rejected (từ chối)
- `StartDate/EndDate`: Range ngày nghỉ
- `ApprovedBy`: UserId người duyệt

---

### 3.7 Contracts
```sql
CREATE TABLE [Contracts] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[EmployeeId] int NOT NULL,
	[ContractType] int NOT NULL,  -- 0=Probation, 1=FullTime, 2=PartTime, 3=Temporary
	[ContractNumber] nvarchar(100) NOT NULL,
	[StartDate] date NOT NULL,
	[EndDate] date NOT NULL,
	[Salary] decimal(18,2) NOT NULL,
	[SalaryType] int NOT NULL,  -- 0=Monthly, 1=Daily, 2=Hourly
	[ContractStatus] int NOT NULL DEFAULT 0,  -- 0=Active, 1=ExpiringSoon, 2=Expired
	[TerminationDate] date NULL,
	[TerminationReason] nvarchar(500),
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_Contracts_EmployeeId_ContractNumber] ON [Contracts]([EmployeeId], [ContractNumber]);
CREATE INDEX [IX_Contracts_EmployeeId_ContractStatus] ON [Contracts]([EmployeeId], [ContractStatus]);
```

**Giải Thích**:
- `ContractType`: 0=Thử việc, 1=Toàn thời gian, 2=Bán thời gian, 3=Tạm thời
- `ContractStatus`: 0=Hiệu lực, 1=Sắp hết hạn, 2=Hết hạn
- `SalaryType`: 0=Lương tháng, 1=Lương ngày, 2=Lương giờ
- `TerminationDate`: Ngày chấm dứt (khi status = Expired)

---

### 3.8 Salaries
```sql
CREATE TABLE [Salaries] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[EmployeeId] int NOT NULL,
	[Month] int NOT NULL,  -- 1-12
	[Year] int NOT NULL,   -- YYYY
	[BaseSalary] decimal(18,2) NOT NULL,
	[BaseSalarySnapshot] decimal(18,2) NOT NULL,  -- Lương cơ bản lúc tính
	[Allowance] decimal(18,2) NOT NULL DEFAULT 0,  -- Phụ cấp
	[Bonus] decimal(18,2) NOT NULL DEFAULT 0,  -- Thưởng
	[Deduction] decimal(18,2) NOT NULL DEFAULT 0,  -- Khấu trừ
	[TotalSalary] decimal(18,2) NOT NULL,  -- = BaseSalary + Allowance + Bonus - Deduction
	[Status] int NOT NULL DEFAULT 0,  -- 0=Draft, 1=Approved, 2=Paid
	[ApprovedBy] nvarchar(450) NULL,
	[ApprovedAt] datetime2 NULL,
	[PaidDate] datetime2 NULL,
	[Note] nvarchar(500),
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE NO ACTION,
	FOREIGN KEY ([ApprovedBy]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL
);

CREATE UNIQUE INDEX [IX_Salaries_EmployeeId_Month_Year] ON [Salaries]([EmployeeId], [Month], [Year]);
```

**Giải Thích**:
- `Month/Year`: Khoá duy nhất per employee (không duplicate lương cùng tháng/năm)
- `BaseSalarySnapshot`: Lưu lại lương cơ bản lúc tính (để audit trail)
- `TotalSalary`: Được tính bằng `BaseSalary + Allowance + Bonus - Deduction`
- `Status`: 0=Nháp, 1=Duyệt, 2=Đã trả

---

### 3.9 EmployeeMedias
```sql
CREATE TABLE [EmployeeMedias] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[EmployeeId] int NOT NULL,
	[MediaType] int NOT NULL,  -- 0=Avatar, 1=Document, 2=Certificate
	[FilePath] nvarchar(500) NOT NULL,
	[FileName] nvarchar(200) NOT NULL,
	[FileSize] bigint NOT NULL,
	[MimeType] nvarchar(100),
	[UploadedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE CASCADE
);
```

**Giải Thích**:
- `MediaType`: 0=Avatar, 1=Document (CMND, bằng cấp), 2=Certificate
- `FilePath`: Đường dẫn server (ví dụ: /uploads/media/emp_123_doc.pdf)
- `FileSize`: Bytes, để kiểm tra quota
- `MimeType`: ví dụ application/pdf, image/jpeg

---

### 3.10 JoinCompanyRequests
```sql
CREATE TABLE [JoinCompanyRequests] (
	[Id] int IDENTITY(1,1) PRIMARY KEY,
	[UserId] nvarchar(450) NOT NULL,
	[CompanyId] int NOT NULL,
	[Status] int NOT NULL DEFAULT 0,  -- 0=Pending, 1=Approved, 2=Rejected
	[ApprovedBy] nvarchar(450) NULL,
	[ApprovedAt] datetime2 NULL,
	[RejectionReason] nvarchar(500),
	[CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedAt] datetime2 NULL,
	[IsDeleted] bit NOT NULL DEFAULT 0,
	FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
	FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id]) ON DELETE CASCADE,
	FOREIGN KEY ([ApprovedBy]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL
);

CREATE UNIQUE INDEX [IX_JoinCompanyRequests_UserId_CompanyId_Pending] 
	ON [JoinCompanyRequests]([UserId], [CompanyId]) WHERE [Status] = 0;
```

**Giải Thích**:
- `Status`: 0=Chờ duyệt, 1=Duyệt, 2=Từ chối
- Filtered unique index: Chỉ có 1 pending request per (User, Company)
- Cho phép multiple Approved/Rejected records (audit trail)

---

## 4. Indexes và Performance

### Primary Indexes (Đã Tạo)
```sql
-- Unique Indexes (Constraint)
IX_Companies_Code
IX_Departments_CompanyId_Code
IX_Positions_CompanyId_Code
IX_Employees_CompanyId_EmployeeCode
IX_Employees_CompanyId_Email
IX_Attendances_EmployeeId_WorkDate
IX_Contracts_EmployeeId_ContractNumber
IX_Salaries_EmployeeId_Month_Year
IX_JoinCompanyRequests_UserId_CompanyId_Pending (filtered)

-- Performance Indexes
IX_LeaveRequests_EmployeeId_Status
IX_Contracts_EmployeeId_ContractStatus
```

### Recommended Additional Indexes (Tuỳ chọn)
```sql
-- Tìm kiếm employees
CREATE INDEX IX_Employees_CompanyId_WorkStatus ON [Employees]([CompanyId], [WorkStatus]);
CREATE INDEX IX_Employees_DepartmentId ON [Employees]([DepartmentId]);
CREATE INDEX IX_Employees_PositionId ON [Employees]([PositionId]);

-- Tìm kiếm attendance
CREATE INDEX IX_Attendances_WorkDate ON [Attendances]([WorkDate]);
CREATE INDEX IX_Attendances_Status ON [Attendances]([Status]);

-- Tìm kiếm leave
CREATE INDEX IX_LeaveRequests_StartDate_EndDate ON [LeaveRequests]([StartDate], [EndDate]);

-- Tìm kiếm contracts
CREATE INDEX IX_Contracts_StartDate_EndDate ON [Contracts]([StartDate], [EndDate]);

-- Tìm kiếm salaries
CREATE INDEX IX_Salaries_Month_Year ON [Salaries]([Month], [Year]);
```

---

## 5. Foreign Key Relationships

### Cascade Delete (ON DELETE CASCADE)
```
Companies → Departments ✓
Companies → Positions ✓
Companies → Employees ✓
Companies → JoinCompanyRequests ✓
Departments → Employees (HeadEmployeeId) → SET NULL
```

### Restrict Delete (ON DELETE RESTRICT)
```
Departments → Employees ✗ (Không xóa phòng ban nếu có nhân viên)
Positions → Employees ✗ (Không xóa chức vụ nếu có nhân viên)
```

### No Action Delete (ON DELETE NO ACTION)
```
Employees → Attendances ✗ (Không xóa nhân viên nếu có attendance)
Employees → LeaveRequests ✗ (Không xóa nhân viên nếu có leave request)
Employees → Contracts ✗ (Không xóa nhân viên nếu có contract)
Employees → Salaries ✗ (Không xóa nhân viên nếu có salary)
```

**Giải Thích**: NO ACTION ngăn chặn việc xóa nhân viên có dữ liệu chi tiết, yêu cầu phải xử lý thủ công trước

---

## 6. Data Constraints

### NOT NULL Fields (Bắt buộc)
```
Companies: Name, Code
Departments: CompanyId, Name, Code
Positions: CompanyId, Name, Code, BaseSalary
Employees: CompanyId, UserId, EmployeeCode, FullName, Email, DepartmentId, PositionId, HireDate, WorkStatus
Attendances: EmployeeId, WorkDate, CheckInTime, Status
LeaveRequests: EmployeeId, LeaveType, StartDate, EndDate, Status
Contracts: EmployeeId, ContractType, ContractNumber, StartDate, EndDate, Salary, SalaryType, ContractStatus
Salaries: EmployeeId, Month, Year, BaseSalary, BaseSalarySnapshot, TotalSalary, Status
```

### Default Values
```
Companies.Status = 0 (Active)
Companies.IsDeleted = 0 (false)
Companies.CreatedAt = GETUTCDATE()

Departments.Status = 0 (Active)
Attendances.Status = 0 (Present)
LeaveRequests.Status = 0 (Pending)
Contracts.ContractStatus = 0 (Active)
Salaries.Status = 0 (Draft)

Salaries.Allowance = 0
Salaries.Bonus = 0
Salaries.Deduction = 0

(Tất cả).IsDeleted = 0
(Tất cả).CreatedAt = GETUTCDATE()
```

---

## 7. Soft Delete Implementation

### GlobalQueryFilter (EF Core)

```csharp
// ApplicationDbContext.OnModelCreating()
modelBuilder.Entity<Company>().HasQueryFilter(c => !c.IsDeleted);
modelBuilder.Entity<Department>().HasQueryFilter(d => !d.IsDeleted);
modelBuilder.Entity<Position>().HasQueryFilter(p => !p.IsDeleted);
modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
modelBuilder.Entity<Attendance>().HasQueryFilter(a => !a.IsDeleted);
modelBuilder.Entity<LeaveRequest>().HasQueryFilter(l => !l.IsDeleted);
modelBuilder.Entity<Contract>().HasQueryFilter(c => !c.IsDeleted);
modelBuilder.Entity<Salary>().HasQueryFilter(s => !s.IsDeleted);
modelBuilder.Entity<EmployeeMedia>().HasQueryFilter(m => !m.IsDeleted);
modelBuilder.Entity<JoinCompanyRequest>().HasQueryFilter(j => !j.IsDeleted);
```

### IgnoreQueryFilters Usage

```csharp
// Khi cần query deleted records
var allRecords = context.Employees.IgnoreQueryFilters().ToList();

// Khi seeding, kiểm tra existing
var existing = context.Employees
	.IgnoreQueryFilters()
	.FirstOrDefault(e => e.CompanyId == 1 && e.EmployeeCode == "NV001");
```

---

## 8. Enum Values

### CompanyStatus
```csharp
public enum CompanyStatus
{
	Active = 0,
	Locked = 1
}
```

### DepartmentStatus
```csharp
public enum DepartmentStatus
{
	Active = 0,
	Inactive = 1
}
```

### PositionStatus
```csharp
public enum PositionStatus
{
	Active = 0,
	Inactive = 1
}
```

### Gender
```csharp
public enum Gender
{
	Male = 0,
	Female = 1,
	Other = 2
}
```

### WorkStatus
```csharp
public enum WorkStatus
{
	Working = 0,
	OnLeave = 1,
	Resigned = 2
}
```

### AttendanceStatus
```csharp
public enum AttendanceStatus
{
	Present = 0,
	Late = 1,
	Absent = 2,
	Leave = 3
}
```

### LeaveType
```csharp
public enum LeaveType
{
	Casual = 0,
	Sick = 1,
	Maternity = 2,
	Annual = 3
}
```

### LeaveRequestStatus
```csharp
public enum LeaveRequestStatus
{
	Pending = 0,
	Approved = 1,
	Rejected = 2
}
```

### ContractType
```csharp
public enum ContractType
{
	Probation = 0,
	FullTime = 1,
	PartTime = 2,
	Temporary = 3
}
```

### ContractStatus
```csharp
public enum ContractStatus
{
	Active = 0,
	ExpiringSoon = 1,
	Expired = 2
}
```

### SalaryType
```csharp
public enum SalaryType
{
	Monthly = 0,
	Daily = 1,
	Hourly = 2
}
```

### SalaryStatus
```csharp
public enum SalaryStatus
{
	Draft = 0,
	Approved = 1,
	Paid = 2
}
```

### MediaType
```csharp
public enum MediaType
{
	Avatar = 0,
	Document = 1,
	Certificate = 2
}
```

### JoinCompanyRequestStatus
```csharp
public enum JoinCompanyRequestStatus
{
	Pending = 0,
	Approved = 1,
	Rejected = 2
}
```

---

## 9. Validation Rules

### Attendance Validation
```csharp
// Không có check-in trong tương lai
WorkDate <= DateTime.Today

// Check-in <= Check-out
CheckInTime < CheckOutTime

// Thời gian hợp lý
CheckInTime >= 06:00 && CheckInTime <= 23:59
CheckOutTime >= 00:00 && CheckOutTime <= 23:59

// Status auto-calculate
if (CheckInTime <= 09:00) → Present
if (CheckInTime > 09:00) → Late
```

### LeaveRequest Validation
```csharp
// Ngày kết thúc >= ngày bắt đầu
EndDate >= StartDate

// Max 30 ngày liên tiếp
(EndDate - StartDate).Days <= 30

// Không xung đột với request khác
if (request1.StartDate <= request2.EndDate && 
	request1.EndDate >= request2.StartDate) → Conflict

// Phép năm có limit
AnnualLeaveUsed + BusinessDaysInRequest <= AnnualLeaveAllowance
```

### Contract Validation
```csharp
// Ngày kết thúc > ngày bắt đầu
EndDate > StartDate

// Duration >= 1 tháng
(EndDate - StartDate).Days >= 30

// Lương > 0
Salary > 0

// Chỉ 1 Active contract per employee
if (ContractStatus == Active) 
	→ ActiveContractCount == 1
```

### Salary Validation
```csharp
// BaseSalary > 0
BaseSalary > 0

// TotalSalary = BaseSalary + Allowance + Bonus - Deduction >= 0
TotalSalary >= 0

// Không duplicate per (EmployeeId, Month, Year)
Unique(EmployeeId, Month, Year)

// Không tạo salary cho resigned employee
if (Employee.WorkStatus == Resigned) → Forbidden
```

### Employee Validation
```csharp
// EmployeeCode format: NV[0-9]{4}
Regex: ^NV\d{4}$

// Email format
IsValidEmail(email)

// Phone format (VN)
Regex: ^(\+84|0)[1-9]\d{8,9}$

// DOB không trong tương lai & age >= 18
DateOfBirth <= DateTime.Today.AddYears(-18)

// Email unique per company
Unique(CompanyId, Email)
```

---

## 10. Seeding Strategy

### Demo Data
```csharp
// 1 Company
Companies: "ABC Corp" (Code: ABC)

// 2 Departments
- "Human Resources" (HR)
- "Engineering" (ENG)

// 3 Positions
- "HR Manager" - $3,000/month
- "HR Staff" - $2,000/month
- "Engineer" - $2,500/month

// 5 Employees
- HR Manager (HR)
- HR Staff (HR)
- Engineer (ENG)
- Engineer (ENG)
- Engineer (ENG)

// 30 Attendances (last month)
- Realistic CheckIn 08:00-09:30
- Realistic CheckOut 17:00-18:30
- Status auto-calculated

// 3 LeaveRequests
- Pending, Approved, Rejected (mixed)

// 3 Contracts
- Each employee has Active contract

// 3 Salaries (current & last month)
- Mix of Draft, Approved, Paid
```

### Seeding Code Pattern
```csharp
using var transaction = await context.Database.BeginTransactionAsync();
try
{
	// Check if data exists
	var existingCompanies = await context.Companies.IgnoreQueryFilters()
		.Where(c => c.Code == "ABC")
		.CountAsync();

	if (existingCompanies == 0)
	{
		// Seed companies
		context.Companies.Add(company);
		await context.SaveChangesAsync();

		// Seed departments
		context.Departments.AddRange(departments);
		await context.SaveChangesAsync();

		// ... more seeding
	}

	await transaction.CommitAsync();
}
catch
{
	await transaction.RollbackAsync();
	throw;
}
```

---

## 11. Database Maintenance

### Backup Strategy
```sql
-- Full backup
BACKUP DATABASE [WebsiteQL_NhanSu] 
TO DISK = 'C:\Backups\WebsiteQL_NhanSu_Full.bak'
WITH FORMAT, INIT;

-- Differential backup
BACKUP DATABASE [WebsiteQL_NhanSu] 
TO DISK = 'C:\Backups\WebsiteQL_NhanSu_Diff.bak'
WITH DIFFERENTIAL;

-- Transaction log backup
BACKUP LOG [WebsiteQL_NhanSu] 
TO DISK = 'C:\Backups\WebsiteQL_NhanSu_Log.trn';
```

### Maintenance Jobs
```sql
-- Rebuild fragmented indexes (> 30%)
ALTER INDEX ALL ON [Employees] REBUILD;

-- Reorganize fragmented indexes (10-30%)
ALTER INDEX ALL ON [Attendances] REORGANIZE;

-- Update statistics
UPDATE STATISTICS [Employees];

-- Cleanup soft-deleted records (older than 1 year)
DELETE FROM [EmployeeMedias] 
WHERE IsDeleted = 1 AND UpdatedAt < DATEADD(YEAR, -1, GETUTCDATE());
```

### Monitoring Queries
```sql
-- Database size
EXEC sp_spaceused @updateusage = 'TRUE';

-- Index fragmentation
SELECT 
	OBJECT_NAME(ips.object_id) AS TableName,
	i.name AS IndexName,
	ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id 
	AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
ORDER BY ips.avg_fragmentation_in_percent DESC;

-- Locked queries
EXEC sp_who2;
```

---

## 12. EF Core Configuration (ApplicationDbContext)

### DbSet Properties
```csharp
public DbSet<Company> Companies { get; set; }
public DbSet<Department> Departments { get; set; }
public DbSet<Position> Positions { get; set; }
public DbSet<Employee> Employees { get; set; }
public DbSet<Attendance> Attendances { get; set; }
public DbSet<LeaveRequest> LeaveRequests { get; set; }
public DbSet<Contract> Contracts { get; set; }
public DbSet<Salary> Salaries { get; set; }
public DbSet<EmployeeMedia> EmployeeMedias { get; set; }
public DbSet<JoinCompanyRequest> JoinCompanyRequests { get; set; }
```

### OnModelCreating Highlights
```csharp
// Unique indexes
builder.Entity<Employee>()
	.HasIndex(e => new { e.CompanyId, e.EmployeeCode })
	.IsUnique();

builder.Entity<Employee>()
	.HasIndex(e => new { e.CompanyId, e.Email })
	.IsUnique();

builder.Entity<Attendance>()
	.HasIndex(a => new { a.EmployeeId, a.WorkDate })
	.IsUnique();

// Filtered unique index
builder.Entity<JoinCompanyRequest>()
	.HasIndex(j => new { j.UserId, j.CompanyId })
	.IsUnique()
	.HasFilter("[Status] = 0");

// Global query filters
builder.Entity<Company>().HasQueryFilter(c => !c.IsDeleted);
builder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
// ... more filters

// FK constraints
builder.Entity<Attendance>()
	.HasOne(a => a.Employee)
	.WithMany(e => e.Attendances)
	.HasForeignKey(a => a.EmployeeId)
	.OnDelete(DeleteBehavior.Restrict);

builder.Entity<Contract>()
	.HasOne(c => c.Employee)
	.WithMany(e => e.Contracts)
	.HasForeignKey(c => c.EmployeeId)
	.OnDelete(DeleteBehavior.Restrict);

// ... more configs
```

---

## 13. Migration Workflow

### Create Migration
```powershell
cd C:\Users\nguye\source\repos\Website Quản Lý Nhân Sự
dotnet ef migrations add MigrationName --project "Website Quản Lý Nhân Sự"
```

### Apply Migration
```powershell
dotnet ef database update --project "Website Quản Lý Nhân Sự"
```

### View Migration History
```sql
SELECT * FROM [__EFMigrationsHistory] 
ORDER BY [MigrationId] DESC;
```

### Rollback Last Migration
```powershell
dotnet ef migrations remove --project "Website Quản Lý Nhân Sự"
```

---

## 14. Performance Tips

### Query Optimization
```csharp
// ✅ Good: Include related data
var employees = await context.Employees
	.Include(e => e.Department)
	.Include(e => e.Position)
	.Where(e => e.CompanyId == companyId)
	.ToListAsync();

// ❌ Bad: N+1 queries
foreach (var emp in employees)
{
	var dept = context.Departments.FirstOrDefault(d => d.Id == emp.DepartmentId);
}

// ✅ Good: Use projections
var employeeNames = await context.Employees
	.Where(e => e.CompanyId == companyId)
	.Select(e => new { e.Id, e.FullName })
	.ToListAsync();

// ✅ Good: Use AsNoTracking for read-only
var employees = await context.Employees
	.AsNoTracking()
	.Where(e => e.CompanyId == companyId)
	.ToListAsync();
```

### Connection Pooling
```csharp
// Program.cs
services.AddDbContextPool<ApplicationDbContext>(options =>
	options.UseSqlServer(
		connectionString,
		sqlOptions => sqlOptions.CommandTimeout(30)
	)
);
```

---

## 15. Current Status

### Latest Migration
```
20260621105551_FixLogicIssuesAndAddConstraints
```

### Schema Version
```
.NET 10 + EF Core 10.0.9
SQL Server 2019+
```

### Applied Constraints
- ✅ Unique (CompanyId, EmployeeCode) on Employees
- ✅ Unique (CompanyId, Email) on Employees
- ✅ Unique (EmployeeId, WorkDate) on Attendances
- ✅ Filtered Unique (UserId, CompanyId) on JoinCompanyRequests where Status=Pending
- ✅ FK Delete Behavior: Restrict for Attendance, LeaveRequest, Contract, Salary
- ✅ Global Query Filters for soft-deleted entities
- ✅ All validations in models

**Status**: ✅ Production Ready

---

## 16. Troubleshooting

### Common Issues

#### Issue: "Foreign key constraint failed"
**Cause**: Attempting to delete parent with child records
**Solution**: 
- Use `ON DELETE RESTRICT` to get clear error
- Delete child records first
- Or use cascade if appropriate

#### Issue: "Unique index violation"
**Cause**: Duplicate data violates unique constraint
**Solution**:
- Check `IgnoreQueryFilters()` to find soft-deleted duplicates
- Clean up duplicates before migration
- Use `MERGE` statement for idempotent updates

#### Issue: "Index fragmentation > 50%"
**Cause**: Many insert/update/delete operations
**Solution**:
```sql
ALTER INDEX ALL ON [Attendances] REBUILD;
UPDATE STATISTICS [Attendances];
```

#### Issue: "Query timeout"
**Cause**: Missing index or inefficient query
**Solution**:
- Add index on frequently filtered columns
- Use projections instead of selecting all fields
- Enable query performance logging

---

**Document Version**: 1.0
**Created**: 2025-06-21
**Last Updated**: 2025-06-21
**Database**: WebsiteQL_NhanSu
**Server**: SQL Server 2019+
