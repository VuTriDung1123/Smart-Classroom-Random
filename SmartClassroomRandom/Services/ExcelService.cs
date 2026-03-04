using ClosedXML.Excel;
using SmartClassroomRandom.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartClassroomRandom.Services
{
    public class ExcelService
    {
        public static List<Student> ImportStudents(string filePath)
        {
            var studentList = new List<Student>();
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // Lấy Sheet 1
                    // Bỏ qua dòng đầu tiên (Header), đọc các dòng có dữ liệu
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        var student = new Student
                        {
                            Id = row.Cell(1).GetValue<int>(),
                            Name = row.Cell(2).GetValue<string>()
                        };
                        studentList.Add(student);
                    }
                }
            }
            catch (Exception ex)
            {
                // Tạm thời in ra console, sau này sẽ bắn thông báo lên UI
                System.Diagnostics.Debug.WriteLine($"Lỗi đọc file Excel: {ex.Message}");
            }
            return studentList;
        }
    }
}