using Website_Quản_Lý_Nhân_Sự.Models.Enums;

namespace Website_Quản_Lý_Nhân_Sự.Helpers;

public static class DisplayHelper
{
    public static string GetCompanyStatusText(CompanyStatus status) => status switch
    {
        CompanyStatus.Active => "Hoạt động",
        CompanyStatus.Locked => "Đã khóa",
        _ => status.ToString()
    };

    public static string JoinStatus(JoinRequestStatus status) => status switch
    {
        JoinRequestStatus.Pending => "Chờ duyệt",
        JoinRequestStatus.Approved => "Đã duyệt",
        JoinRequestStatus.Rejected => "Từ chối",
        _ => status.ToString()
    };

    public static string WorkStatus(Models.Enums.WorkStatus status) => status switch
    {
        Models.Enums.WorkStatus.Active => "Đang làm việc",
        Models.Enums.WorkStatus.OnLeave => "Nghỉ phép",
        Models.Enums.WorkStatus.Resigned => "Đã nghỉ",
        _ => status.ToString()
    };

    public static string LeaveStatus(LeaveRequestStatus status) => status switch
    {
        LeaveRequestStatus.Pending => "Chờ duyệt",
        LeaveRequestStatus.Approved => "Đã duyệt",
        LeaveRequestStatus.Rejected => "Từ chối",
        _ => status.ToString()
    };

    public static string ContractStatus(Models.Enums.ContractStatus status) => status switch
    {
        Models.Enums.ContractStatus.Active => "Còn hiệu lực",
        Models.Enums.ContractStatus.ExpiringSoon => "Sắp hết hạn",
        Models.Enums.ContractStatus.Expired => "Hết hạn",
        _ => status.ToString()
    };

    public static string BadgeClassForJoinStatus(JoinRequestStatus status) => status switch
    {
        JoinRequestStatus.Pending => "bg-warning",
        JoinRequestStatus.Approved => "bg-success",
        JoinRequestStatus.Rejected => "bg-danger",
        _ => "bg-secondary"
    };

    public static string FormatMoney(decimal value) => string.Format("{0:N0} ₫", value);

    public static string Gender(Models.Enums.Gender gender) => gender switch
    {
        Models.Enums.Gender.Male => "Nam",
        Models.Enums.Gender.Female => "Nữ",
        _ => "Khác"
    };
}
