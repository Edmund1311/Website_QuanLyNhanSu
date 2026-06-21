using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Website_Quản_Lý_Nhân_Sự.Models;

namespace Website_Quản_Lý_Nhân_Sự.Services;

public static class ExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] ExportEmployees(IEnumerable<Employee> employees)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("NhanVien");
        sheet.Cell(1, 1).Value = "Mã NV";
        sheet.Cell(1, 2).Value = "Họ tên";
        sheet.Cell(1, 3).Value = "Email";
        sheet.Cell(1, 4).Value = "Phòng ban";
        sheet.Cell(1, 5).Value = "Chức vụ";
        sheet.Cell(1, 6).Value = "Trạng thái";

        var row = 2;
        foreach (var employee in employees)
        {
            sheet.Cell(row, 1).Value = employee.EmployeeCode;
            sheet.Cell(row, 2).Value = employee.FullName;
            sheet.Cell(row, 3).Value = employee.Email;
            sheet.Cell(row, 4).Value = employee.Department?.Name;
            sheet.Cell(row, 5).Value = employee.Position?.Name;
            sheet.Cell(row, 6).Value = employee.WorkStatus.ToString();
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ExportAttendance(IEnumerable<Attendance> records)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("ChamCong");
        sheet.Cell(1, 1).Value = "Nhân viên";
        sheet.Cell(1, 2).Value = "Ngày";
        sheet.Cell(1, 3).Value = "Giờ vào";
        sheet.Cell(1, 4).Value = "Giờ ra";
        sheet.Cell(1, 5).Value = "Trạng thái";

        var row = 2;
        foreach (var item in records)
        {
            sheet.Cell(row, 1).Value = item.Employee.FullName;
            sheet.Cell(row, 2).Value = item.WorkDate.ToString("dd/MM/yyyy");
            sheet.Cell(row, 3).Value = item.CheckIn?.ToString(@"hh\:mm");
            sheet.Cell(row, 4).Value = item.CheckOut?.ToString(@"hh\:mm");
            sheet.Cell(row, 5).Value = item.Status.ToString();
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ExportSalaries(IEnumerable<Salary> salaries)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BangLuong");
        sheet.Cell(1, 1).Value = "Nhân viên";
        sheet.Cell(1, 2).Value = "Tháng/Năm";
        sheet.Cell(1, 3).Value = "Lương cơ bản";
        sheet.Cell(1, 4).Value = "Phụ cấp";
        sheet.Cell(1, 5).Value = "Thưởng";
        sheet.Cell(1, 6).Value = "Khấu trừ";
        sheet.Cell(1, 7).Value = "Tổng lương";

        var row = 2;
        foreach (var item in salaries)
        {
            sheet.Cell(row, 1).Value = item.Employee.FullName;
            sheet.Cell(row, 2).Value = $"{item.Month}/{item.Year}";
            sheet.Cell(row, 3).Value = item.BaseSalary;
            sheet.Cell(row, 4).Value = item.Allowance;
            sheet.Cell(row, 5).Value = item.Bonus;
            sheet.Cell(row, 6).Value = item.Deduction;
            sheet.Cell(row, 7).Value = item.TotalSalary;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ExportContractPdf(Contract contract)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("HỢP ĐỒNG LAO ĐỘNG").SemiBold().FontSize(20).AlignCenter();
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Mã hợp đồng: {contract.ContractCode}");
                    col.Item().Text($"Nhân viên: {contract.Employee.FullName}");
                    col.Item().Text($"Phòng ban: {contract.DepartmentSnapshot}");
                    col.Item().Text($"Chức vụ: {contract.PositionSnapshot}");
                    col.Item().Text($"Ngày bắt đầu: {contract.StartDate:dd/MM/yyyy}");
                    col.Item().Text($"Ngày kết thúc: {contract.EndDate:dd/MM/yyyy}");
                    col.Item().Text($"Mức lương: {contract.Salary:N0} VND");
                    col.Item().Text($"Trạng thái: {contract.Status}");
                });
            });
        }).GeneratePdf();
    }

    public static byte[] ExportSalaryPdf(Salary salary)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("PHIẾU LƯƠNG").SemiBold().FontSize(20).AlignCenter();
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Nhân viên: {salary.Employee.FullName}");
                    col.Item().Text($"Kỳ lương: {salary.Month}/{salary.Year}");
                    col.Item().Text($"Lương cơ bản: {salary.BaseSalary:N0} VND");
                    col.Item().Text($"Phụ cấp: {salary.Allowance:N0} VND");
                    col.Item().Text($"Thưởng: {salary.Bonus:N0} VND");
                    col.Item().Text($"Khấu trừ: {salary.Deduction:N0} VND");
                    col.Item().Text($"Tổng lương: {salary.TotalSalary:N0} VND").Bold();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] ExportEmployeeProfilePdf(Employee employee)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("HỒ SƠ NHÂN VIÊN").SemiBold().FontSize(20).AlignCenter();
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Mã nhân viên: {employee.EmployeeCode}");
                    col.Item().Text($"Họ tên: {employee.FullName}");
                    col.Item().Text($"Email: {employee.Email}");
                    col.Item().Text($"Phòng ban: {employee.Department?.Name}");
                    col.Item().Text($"Chức vụ: {employee.Position?.Name}");
                    col.Item().Text($"Ngày vào làm: {employee.HireDate:dd/MM/yyyy}");
                    col.Item().Text($"Trạng thái: {employee.WorkStatus}");
                });
            });
        }).GeneratePdf();
    }
}
