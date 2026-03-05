using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SmartClassroomRandom.Services
{
    public class VoiceService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                string url = $"https://translate.googleapis.com/translate_tts?client=gtx&ie=UTF-8&tl=vi&q={Uri.EscapeDataString(text)}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return;

                // TẠO TÊN FILE ĐỘC LẬP BẰNG GUID ĐỂ KHÔNG BỊ TRÙNG VÀ LỖI KHÓA FILE
                string tempFile = Path.Combine(Path.GetTempPath(), $"tts_{Guid.NewGuid()}.mp3");

                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                var tcs = new TaskCompletionSource<bool>();
                var player = new MediaPlayer();

                player.MediaEnded += (s, e) =>
                {
                    player.Close();
                    try { File.Delete(tempFile); } catch { } // Dọn rác
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