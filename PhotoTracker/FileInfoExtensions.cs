using System;
using System.IO;

namespace PhotoTracker
{
    public static class FilenameExtensions
    {
        public static string GetThumbnailPath(this string imagePath)
        {
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoTracker_ThumbnailCache");
            return Path.Combine(appDataFolder, imagePath.Insert(imagePath.Length - 4, "_thumbnail"));
        }
    }
}