using ClosedXML.Excel;
using SmartClassroomRandom.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows; // Thêm thư viện này để dùng MessageBox

namespace SmartClassroomRandom.Services
{
    public class ExcelService
    {
        public static (DataTable table, List<Student> students) ImportDynamicExcel(string filePath)
        {
            DataTable dt = new DataTable();
            List<Student> students = new List<Student>();

            try
            {
                // Dùng FileStream + FileShare.ReadWrite để ép đọc file kể cả khi file đang được mở trong Excel
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var workbook = new XLWorkbook(fs))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var firstRow = worksheet.FirstRowUsed();

                        if (firstRow == null)
                        {
                            MessageBox.Show("File Excel trống, không có dòng tiêu đề!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return (dt, students);
                        }

                        // 1. LẤY TÊN CÁC CỘT CHO DATATABLE VÀ KIỂM TRA TRÙNG LẶP
                        foreach (var cell in firstRow.Cells())
                        {
                            string colName = cell.Value.ToString().Trim();

                            // Nếu tên cột rỗng, tự đặt tên là Cột 1, Cột 2...
                            if (string.IsNullOrEmpty(colName))
                            {
                                colName = $"Cột {cell.Address.ColumnNumber}";
                            }

                            // Nếu bị trùng tên cột, thêm số vào đuôi
                            if (!dt.Columns.Contains(colName))
                            {
                                dt.Columns.Add(colName);
                            }
                            else
                            {
                                dt.Columns.Add($"{colName} ({cell.Address.ColumnNumber})");
                            }
                        }

                        // 2. ĐỌC DỮ LIỆU TỪNG DÒNG
                        var rows = worksheet.RowsUsed().Skip(1);
                        int index = 1;

                        foreach (var row in rows)
                        {
                            var dtRow = dt.NewRow();
                            var student = new Student { Id = index++ };

                            string ho = "";
                            string ten = "";

                            // Duyệt qua từng cột đã được định nghĩa trong DataTable
                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                string colName = dt.Columns[i].ColumnName.ToLower();

                                // ClosedXML đếm ô bắt đầu từ 1
                                string cellValue = row.Cell(i + 1).Value.ToString().Trim();

                                dtRow[i] = cellValue;

                                // -- Thuật toán map Model thông minh --
                                if (colName == "họ") { ho = cellValue; }
                                else if (colName == "tên") { ten = cellValue; }
                                else if (colName == "họ tên" || colName == "họ và tên" || colName == "name") { student.Name = cellValue; }
                                else if (colName.Contains("điểm cộng")) // Tách riêng điểm cộng
                                {
                                    if (int.TryParse(cellValue, out int dc)) student.DiemCong = dc;
                                }
                                else if (colName.Contains("phát biểu") || colName.Contains("số lần")) // Tách riêng phát biểu
                                {
                                    if (int.TryParse(cellValue, out int pb)) student.PhatBieu = pb;
                                }
                                else if (colName.Contains("vắng") || colName.Contains("không đi học"))
                                {
                                    if (int.TryParse(cellValue, out int vang)) student.KhongDiHoc = vang;
                                }
                            }

                            // Xử lý riêng cho file của bạn: Ghép Họ và Tên lại với nhau
                            if (string.IsNullOrEmpty(student.Name))
                            {
                                student.Name = $"{ho} {ten}".Trim();
                            }

                            dt.Rows.Add(dtRow);
                            students.Add(student);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu có bất kỳ lỗi nào, popup sẽ hét toáng lên cho chúng ta biết!
                MessageBox.Show($"Không thể đọc file Excel. Chi tiết lỗi:\n{ex.Message}", "Lỗi File", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return (dt, students);
        }
    }
}