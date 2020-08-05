using Android.App;
using Android.Graphics;
using Infrastracture.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace TcpMobile.Droid.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private Activity Context => Platform.CurrentActivity;
        private async Task<byte[]> CaptureAsync()
        {
            var rootView = Context.Window.DecorView.RootView;

            using (var screenshot = Bitmap.CreateBitmap(
                                    rootView.Width,
                                    rootView.Height,
                                    Bitmap.Config.Argb8888))
            {
                var canvas = new Canvas(screenshot);
                rootView.Draw(canvas);

                using (var stream = new MemoryStream())
                {
                    await screenshot.CompressAsync(Bitmap.CompressFormat.Png, 90, stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<string> CaptureAndSaveAsync()
        {
            try
            {
                var bytes = await CaptureAsync();

                Java.IO.File picturesFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures);
                string date = DateTime.Now.ToString().Replace("/", "-").Replace(":", "-");

                var directory = $"{picturesFolder.AbsolutePath}/Screenshots";

                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                string filePath = System.IO.Path.Combine(directory, "Screnshot-" + date + ".png");
                using (System.IO.FileStream SourceStream = System.IO.File.Open(filePath, System.IO.FileMode.OpenOrCreate))
                {
                    SourceStream.Seek(0, System.IO.SeekOrigin.End);
                    await SourceStream.WriteAsync(bytes, 0, bytes.Length);
                }
                return filePath;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
