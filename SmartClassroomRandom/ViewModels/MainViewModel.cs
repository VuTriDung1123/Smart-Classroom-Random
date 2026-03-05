using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartClassroomRandom.Models;
using SmartClassroomRandom.Services;
using SmartClassroomRandom.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data;
using System.Threading.Tasks;

namespace SmartClassroomRandom.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // ================= 1. CÁC BIẾN TOÀN CỤC (TOP BAR) =================
        [ObservableProperty] private int _totalStudents = 0;
        [ObservableProperty] private int _attendCount = 0;
        [ObservableProperty] private int _bannedCount = 0;
        [ObservableProperty] private string _currentFileName = "Chưa tải file";
        [ObservableProperty] private DataTable _excelData = new DataTable();
        [ObservableProperty] private string _bestStudentName = "Chưa có";
        [ObservableProperty] private string _bestStudentPoints = "0";

        // Biến này quyết định xem màn hình bên phải đang hiển thị trang nào
        [ObservableProperty] private object? _currentView;

        // Danh sách gốc (Tất cả sinh viên từ Excel)
        [ObservableProperty] private ObservableCollection<Student> _students = new();

        // ================= BIẾN CHO PHẦN GIỌNG NÓI =================
        public List<string> AudienceList { get; } = new List<string> { "sinh viên", "học sinh" };
        public List<string> ActionList { get; } = new List<string> { "trả bài", "trả lời", "kiểm tra bài tập về nhà" };

        [ObservableProperty] private string _selectedAudience = "sinh viên";
        [ObservableProperty] private string _selectedAction = "trả bài";

        // ================= CÁC BIẾN CÀI ĐẶT (SETTINGS GACHA) =================
        [ObservableProperty] private string _gachaSsrTitle = "KIM BÀI MIỄN TỬ";
        [ObservableProperty] private string _gachaSsrDesc = "Miễn trả bài hôm nay! Hệ thống tự động vinh danh và cộng 3 điểm.";

        [ObservableProperty] private string _gachaSrTitle = "TRIỆU HỒI HỘ GIÁ";
        [ObservableProperty] private string _gachaSrDesc = "Chỉ định 1 đồng đội trả lời hộ. Đúng cùng hưởng 1 điểm cộng, sai cùng chịu 1 điểm trừ!";

        [ObservableProperty] private string _gachaRTitle = "NHÂN ĐÔI SÁT THƯƠNG";
        [ObservableProperty] private string _gachaRDesc = "Trả lời đúng được x2 điểm cộng. Trả lời sai bị 2 điểm trừ!";

        [ObservableProperty] private string _gachaNTitle = "LỜI NGUYỀN CÂM LẶNG";
        [ObservableProperty] private string _gachaNDesc = "Áp lực thời gian: Chỉ có đúng 15 giây để suy nghĩ và trả lời! Hết giờ là trừ 1 điểm.";

        // ================= 2. CÁC BIẾN CHO TỪNG TRANG =================
        // Trang Random 1 người & Gacha
        [ObservableProperty] private Student? _selectedSingleStudent;
        [ObservableProperty] private string _gachaTitle = string.Empty;
        [ObservableProperty] private string _gachaDescription = string.Empty;
        [ObservableProperty] private string _gachaColor = "Transparent";
        [ObservableProperty] private string _gachaRarity = string.Empty;
        [ObservableProperty] private bool _isGachaRevealed = false;

        // Trang Random N người
        [ObservableProperty] private int _generateCount = 10;
        [ObservableProperty] private ObservableCollection<Student> _selectedStudents = new();

        // Trang Xếp lịch
        [ObservableProperty] private int _totalGroups = 15;
        [ObservableProperty] private int _totalDays = 3;
        [ObservableProperty] private ObservableCollection<ScheduleDay> _scheduleDays = new();

        // Cache các trang để chuyển đổi không bị lag
        private readonly ExcelListView _excelView = new();
        private readonly RandomOneView _randomOneView = new();
        private readonly RandomMultipleView _randomMultipleView = new();
        private readonly GroupScheduleView _groupScheduleView = new();
        private readonly SettingsView _settingsView = new(); // Trang cài đặt

        public MainViewModel()
        {
            // Mặc định mở app lên là trang danh sách Excel
            CurrentView = _excelView;
        }

        // ================= 3. COMMAND: ĐIỀU HƯỚNG CHUYỂN TRANG =================
        [RelayCommand] private void NavigateExcel() => CurrentView = _excelView;
        [RelayCommand] private void NavigateRandomOne() => CurrentView = _randomOneView;
        [RelayCommand] private void NavigateRandomN() => CurrentView = _randomMultipleView;
        [RelayCommand] private void NavigateGroup() => CurrentView = _groupScheduleView;
        [RelayCommand] private void NavigateSettings() => CurrentView = _settingsView; // Nút chuyển trang Settings

        // ================= 4. COMMAND: TẢI FILE EXCEL =================
        [RelayCommand]
        private void SyncExcel()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Chọn danh sách sinh viên"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var result = ExcelService.ImportDynamicExcel(openFileDialog.FileName);
                ExcelData = result.table;

                Students.Clear();
                foreach (var st in result.students)
                {
                    Students.Add(st);
                }

                TotalStudents = Students.Count;
                AttendCount = Students.Count;
                CurrentFileName = System.IO.Path.GetFileName(openFileDialog.FileName);

                var best = Students.OrderByDescending(s => s.DiemCong).FirstOrDefault();
                if (best != null)
                {
                    BestStudentName = best.Name;
                    BestStudentPoints = best.DiemCong.ToString();
                }

                NavigateExcel();
            }
        }

        // ================= 5. COMMAND: RANDOM 1 NGƯỜI =================
        [RelayCommand]
        private async Task RandomOneAsync()
        {
            if (Students.Count == 0) return;
            SelectedSingleStudent = null;
            IsGachaRevealed = false;

            string intro = $"Chọn ngẫu nhiên 1 {SelectedAudience} {SelectedAction}";
            await VoiceService.SpeakAsync(intro);

            var random = new Random();
            Student? pickedStudent = null;

            if (SelectedAction == "trả lời")
            {
                int maxPhatBieu = Students.Max(s => s.PhatBieu);
                var weightedStudents = Students.Select(s => new { Student = s, Weight = (maxPhatBieu - s.PhatBieu) + 1 }).ToList();
                int totalWeight = weightedStudents.Sum(x => x.Weight);
                int randomNumber = random.Next(0, totalWeight);
                int currentSum = 0;
                foreach (var item in weightedStudents)
                {
                    currentSum += item.Weight;
                    if (randomNumber < currentSum) { pickedStudent = item.Student; break; }
                }
            }
            else
            {
                pickedStudent = Students[random.Next(Students.Count)];
            }

            SelectedSingleStudent = pickedStudent;
            if (pickedStudent != null) await VoiceService.SpeakAsync(pickedStudent.Name);
        }

        // ================= LỆNH MỚI: RÚT THẺ GACHA =================
        [RelayCommand]
        private async Task RollGachaAsync()
        {
            if (SelectedSingleStudent == null) return;

            IsGachaRevealed = false;
            await Task.Delay(100);

            var rnd = new Random();
            int roll = rnd.Next(1, 101);

            // Gán dữ liệu dựa trên phần Cài Đặt (Settings)
            if (roll <= 5) // 5% SSR
            {
                GachaRarity = "SSR";
                GachaTitle = GachaSsrTitle;
                GachaDescription = GachaSsrDesc;
                GachaColor = "#FFD700";
            }
            else if (roll <= 20) // 15% SR
            {
                GachaRarity = "SR";
                GachaTitle = GachaSrTitle;
                GachaDescription = GachaSrDesc;
                GachaColor = "#B465DA";
            }
            else if (roll <= 50) // 30% R
            {
                GachaRarity = "R";
                GachaTitle = GachaRTitle;
                GachaDescription = GachaRDesc;
                GachaColor = "#4A90E2";
            }
            else // 50% N
            {
                GachaRarity = "N";
                GachaTitle = GachaNTitle;
                GachaDescription = GachaNDesc;
                GachaColor = "#607D8B";
            }

            IsGachaRevealed = true;
            await VoiceService.SpeakAsync($"Thẻ hiệu ứng: {GachaTitle}. {GachaDescription}");
        }

        // ================= 6. COMMAND: RANDOM N NGƯỜI =================
        [RelayCommand]
        private async Task GenerateMultipleAsync()
        {
            if (Students.Count == 0 || GenerateCount <= 0) return;
            SelectedStudents.Clear();

            string intro = $"Chọn ngẫu nhiên {GenerateCount} {SelectedAudience} {SelectedAction}";
            await VoiceService.SpeakAsync(intro);

            int takeCount = Math.Min(GenerateCount, Students.Count);
            var random = new Random();
            var randomizedList = Students.OrderBy(x => random.Next()).Take(takeCount).ToList();

            double avgPhatBieu = Students.Count > 0 ? Students.Average(s => s.PhatBieu) : 0;

            foreach (var st in randomizedList)
            {
                if (st.PhatBieu == 0 || st.PhatBieu < avgPhatBieu * 0.5) st.CardColor = "#E24A4A";
                else if (st.PhatBieu > avgPhatBieu * 1.2) st.CardColor = "#4CAF50";
                else st.CardColor = "#4A90E2";

                SelectedStudents.Add(st);
                await VoiceService.SpeakAsync(st.Name);
            }
        }

        // ================= 7. COMMAND: XẾP LỊCH BÁO CÁO NHÓM =================
        [RelayCommand]
        private async Task GenerateScheduleAsync()
        {
            if (TotalGroups <= 0 || TotalDays <= 0) return;
            ScheduleDays.Clear();

            string intro = $"Bắt đầu xếp lịch báo cáo cho {TotalGroups} nhóm, trong {TotalDays} ngày.";
            await VoiceService.SpeakAsync(intro);

            var random = new Random();
            var allGroups = Enumerable.Range(1, TotalGroups).OrderBy(x => random.Next()).ToList();

            int currentGroupIndex = 0;
            // Math.Ceiling sẽ tự làm tròn lên. Số nhóm lẻ sẽ dồn vào các ngày đầu.
            int groupsPerDay = (int)Math.Ceiling((double)TotalGroups / TotalDays);

            for (int i = 1; i <= TotalDays; i++)
            {
                var day = new ScheduleDay { DayName = $"Ngày {i}" };

                for (int j = 0; j < groupsPerDay && currentGroupIndex < allGroups.Count; j++)
                {
                    day.Groups.Add($"Nhóm {allGroups[currentGroupIndex]}");
                    currentGroupIndex++;
                }

                if (day.Groups.Count > 0)
                {
                    ScheduleDays.Add(day);

                    // Cơ chế bảo vệ độ dài câu nói
                    string textToRead = "";
                    if (day.Groups.Count <= 5)
                    {
                        string groupNames = string.Join(", ", day.Groups);
                        textToRead = $"{day.DayName} gồm: {groupNames}";
                    }
                    else
                    {
                        textToRead = $"{day.DayName} gồm {day.Groups.Count} nhóm báo cáo.";
                    }

                    await VoiceService.SpeakAsync(textToRead);
                }
            }

            await VoiceService.SpeakAsync("Đã xếp lịch xong. Chúc các nhóm chuẩn bị tốt!");
        }
    }
}