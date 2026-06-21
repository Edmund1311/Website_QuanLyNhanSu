using Website_Quản_Lý_Nhân_Sự.Models;
using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Helpers;

public static class SnapshotHelper
{
    public static void ApplyEmployeeSnapshot(Employee employee, Attendance attendance)
    {
        attendance.DepartmentSnapshot = employee.Department?.Name ?? string.Empty;
        attendance.PositionSnapshot = employee.Position?.Name ?? string.Empty;
    }

    public static void ApplyEmployeeSnapshot(Employee employee, LeaveRequest request)
    {
        request.DepartmentSnapshot = employee.Department?.Name ?? string.Empty;
        request.PositionSnapshot = employee.Position?.Name ?? string.Empty;
    }

    public static void ApplyEmployeeSnapshot(Employee employee, Contract contract)
    {
        contract.DepartmentSnapshot = employee.Department?.Name ?? string.Empty;
        contract.PositionSnapshot = employee.Position?.Name ?? string.Empty;
        contract.BaseSalarySnapshot = employee.Position?.BaseSalary ?? 0;
    }

    public static void ApplyEmployeeSnapshot(Employee employee, Salary salary)
    {
        salary.DepartmentSnapshot = employee.Department?.Name ?? string.Empty;
        salary.PositionSnapshot = employee.Position?.Name ?? string.Empty;
        salary.BaseSalarySnapshot = employee.Position?.BaseSalary ?? 0;
    }

    public static ContractStatus ResolveContractStatus(DateTime endDate)
    {
        var today = DateTime.UtcNow.Date;
        if (endDate.Date < today)
        {
            return ContractStatus.Expired;
        }

        if (endDate.Date <= today.AddDays(30))
        {
            return ContractStatus.ExpiringSoon;
        }

        return ContractStatus.Active;
    }

    public static decimal CalculateTotalSalary(decimal baseSalary, decimal allowance, decimal bonus, decimal deduction)
        => baseSalary + allowance + bonus - deduction;
}
