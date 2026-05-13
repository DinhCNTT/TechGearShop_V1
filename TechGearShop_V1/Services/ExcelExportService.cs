using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using TechGearShop_V1.Models.ViewModels;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public ExcelExportService()
        {
            // Cấu hình bản quyền EPPlus cho dự án học tập (phi thương mại)
            ExcelPackage.License.SetNonCommercialPersonal("TechGearShop");
        }

        public byte[] GenerateDashboardExcelReport(DashboardViewModel data, int month, int year)
        {
            using var package = new ExcelPackage();

            // ── Sheet 1: Tổng quan KPI ──
            var sheet1 = package.Workbook.Worksheets.Add("Tổng quan");
            
            // Header
            sheet1.Cells["A1:D1"].Merge = true;
            sheet1.Cells["A1"].Value = $"BÁO CÁO KINH DOANH THÁNG {month}/{year}";
            sheet1.Cells["A1"].Style.Font.Size = 16;
            sheet1.Cells["A1"].Style.Font.Bold = true;
            sheet1.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            sheet1.Cells["A1"].Style.Font.Color.SetColor(Color.DarkBlue);

            // KPI Table
            string[] kpiHeaders = { "Chỉ số", "Giá trị", "Tăng trưởng so với kỳ trước" };
            for (int i = 0; i < kpiHeaders.Length; i++)
            {
                sheet1.Cells[3, i + 1].Value = kpiHeaders[i];
                sheet1.Cells[3, i + 1].Style.Font.Bold = true;
                sheet1.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet1.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                sheet1.Cells[3, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Dữ liệu KPI
            var kpis = new List<(string, string, string)>
            {
                ("Doanh thu ròng", $"{data.MonthlyRevenue:N0} VNĐ", $"{data.RevenueGrowthPercent}%"),
                ("Lợi nhuận ròng", $"{data.MonthlyProfit:N0} VNĐ", $"{data.ProfitGrowthPercent}%"),
                ("Số đơn hàng hoàn thành", data.OrderCountData.LastOrDefault().ToString(), $"{data.OrderGrowthPercent}%"),
                ("Khách hàng mới", data.NewUsersThisMonth.ToString(), $"{data.UserGrowthPercent}%"),
                ("Đơn hàng đang chờ xử lý", data.PendingOrders.ToString(), "-"),
                ("Sản phẩm sắp hết hàng", data.LowStockProductCount.ToString(), "-")
            };

            for (int i = 0; i < kpis.Count; i++)
            {
                int row = i + 4;
                sheet1.Cells[row, 1].Value = kpis[i].Item1;
                sheet1.Cells[row, 2].Value = kpis[i].Item2;
                sheet1.Cells[row, 3].Value = kpis[i].Item3;

                // Căn lề và border
                for(int col=1; col<=3; col++) {
                    sheet1.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }
            sheet1.Cells[sheet1.Dimension.Address].AutoFitColumns();

            // ── Sheet 2: 10 Đơn hàng mới nhất ──
            var sheet2 = package.Workbook.Worksheets.Add("Đơn hàng gần đây");
            sheet2.Cells["A1:F1"].Merge = true;
            sheet2.Cells["A1"].Value = $"CÁC ĐƠN HÀNG GẦN NHẤT TRONG KỲ";
            sheet2.Cells["A1"].Style.Font.Size = 14;
            sheet2.Cells["A1"].Style.Font.Bold = true;

            string[] orderHeaders = { "Mã Đơn", "Khách hàng", "Tổng tiền", "Trạng thái", "Ngày đặt" };
            for (int i = 0; i < orderHeaders.Length; i++)
            {
                sheet2.Cells[3, i + 1].Value = orderHeaders[i];
                sheet2.Cells[3, i + 1].Style.Font.Bold = true;
                sheet2.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet2.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                sheet2.Cells[3, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            for (int i = 0; i < data.RecentOrders.Count; i++)
            {
                var o = data.RecentOrders[i];
                int row = i + 4;
                sheet2.Cells[row, 1].Value = $"#{o.Id}";
                sheet2.Cells[row, 2].Value = o.CustomerName;
                sheet2.Cells[row, 3].Value = o.FinalAmount;
                sheet2.Cells[row, 3].Style.Numberformat.Format = "#,##0";
                sheet2.Cells[row, 4].Value = o.Status;
                sheet2.Cells[row, 5].Value = o.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

                for(int col=1; col<=5; col++) {
                    sheet2.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }
            sheet2.Cells[sheet2.Dimension.Address].AutoFitColumns();

            // ── Sheet 3: Top Sản Phẩm ──
            var sheet3 = package.Workbook.Worksheets.Add("Top Sản phẩm");
            sheet3.Cells["A1:D1"].Merge = true;
            sheet3.Cells["A1"].Value = $"TOP SẢN PHẨM BÁN CHẠY";
            sheet3.Cells["A1"].Style.Font.Size = 14;
            sheet3.Cells["A1"].Style.Font.Bold = true;

            string[] topHeaders = { "Xếp hạng", "Tên sản phẩm", "Số lượng bán", "Doanh thu" };
            for (int i = 0; i < topHeaders.Length; i++)
            {
                sheet3.Cells[3, i + 1].Value = topHeaders[i];
                sheet3.Cells[3, i + 1].Style.Font.Bold = true;
                sheet3.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet3.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                sheet3.Cells[3, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            for (int i = 0; i < data.TopProducts.Count; i++)
            {
                var p = data.TopProducts[i];
                int row = i + 4;
                sheet3.Cells[row, 1].Value = i + 1;
                sheet3.Cells[row, 2].Value = p.Name;
                sheet3.Cells[row, 3].Value = p.SoldQty;
                sheet3.Cells[row, 4].Value = p.Revenue;
                sheet3.Cells[row, 4].Style.Numberformat.Format = "#,##0";

                for(int col=1; col<=4; col++) {
                    sheet3.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }
            if(sheet3.Dimension != null) sheet3.Cells[sheet3.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}
