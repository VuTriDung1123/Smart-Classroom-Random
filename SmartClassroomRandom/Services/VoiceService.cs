using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SmartClassroomRandom.Services
{
    public class VoiceService
    {
        // Dùng 1 HttpClient duy nhất cho toàn app để tải nhanh và không tốn tài nguyên
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                // 1. Tạo link chuẩn của Google (giọng nữ tiếng Việt)
                string url = $"https://translate.googleapis.com/translate_tts?client=gtx&ie=UTF-8&tl=vi&q={Uri.EscapeDataString(text)}";

                // 2. Tải trực tiếp file mp3 về máy để tránh lỗi rớt dấu tiếng Việt
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return;

                // Lưu vào thư mục Temp của Windows (sẽ tự bị xóa dọn sau này, không rác máy)
                string tempFile = Path.Combine(Path.GetTempPath(), "smart_class_tts.mp3");

                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                // 3. Cho MediaPlayer phát file offline vừa tải
                var tcs = new TaskCompletionSource<bool>();
                var player = new MediaPlayer();

                player.MediaEnded += (s, e) =>
                {
                    player.Close();
                    tcs.TrySetResult(true);
                };
                player.MediaFailed += (s, e) =>
                {
                    player.Close();
                    tcs.TrySetResult(false);
                };

                player.Open(new Uri(tempFile, UriKind.Absolute));
                player.Play();

                // Chờ đọc xong, hoặc tự đi tiếp sau tối đa 8 giây
                await Task.WhenAny(tcs.Task, Task.Delay(8000));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi âm thanh: {ex.Message}");
            }
        }
    }
}