using TechGearShop_V1.Models.ViewModels;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IExcelExportService
    {
        byte[] GenerateDashboardExcelReport(DashboardViewModel data, int month, int year);
    }
}
