using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace PhotoTracker
{
    public class ThumnailGenerator
    {
        public static IObservable<int> Start(IScheduler scheduler, IList<FileInfo> files)
        {
            return Observable.Create<int>(obs => {
                Action<int, Action<int>> iterator = (index, self) => {
                    if (index < files.Count) {
                        var photo = files[index];
                        var thumbnailPath = photo.Name.GetThumbnailPath();
                        var thumbnailDirectory = Path.GetDirectoryName(thumbnailPath);

                        if (!Directory.Exists(thumbnailDirectory))
                            Directory.CreateDirectory(thumbnailDirectory);

                        if (!File.Exists(thumbnailPath)) {
                            using (var bitmap = new Bitmap(photo.FullName)) {
                                var thumbnail =
                                    (Bitmap)bitmap.GetThumbnailImage(bitmap.Width / 20, bitmap.Height / 20, null, IntPtr.Zero);
                                thumbnail.Save(thumbnailPath);
                            }
                        }

                        obs.OnNext((int)((index / (float)files.Count) * 100));
                        self(index + 1);
                    } else obs.OnCompleted();
                };

                return scheduler.Schedule(0, iterator);
            });
        }
    }
}