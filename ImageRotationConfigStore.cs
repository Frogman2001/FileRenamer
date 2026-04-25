using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace FileRenamer
{
    internal sealed class ImageRotationConfigStore
    {
        private const string ConfigFileName = "ImageConfig.json";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public int? TryGetRotation(string imagePath)
        {
            var entry = GetOrUpdateEntryForImage(imagePath, out _);
            return entry?.Rotation;
        }

        public bool SaveRotation(string imagePath, int rotation)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return false;
            }

            try
            {
                var folderPath = Path.GetDirectoryName(imagePath);
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return false;
                }

                var fileName = Path.GetFileName(imagePath);
                var hash = ComputeMd5Hex(imagePath);
                var config = LoadConfig(folderPath);

                var existing = config.Images.FirstOrDefault(i => string.Equals(i.Md5, hash, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    config.Images.Add(new ImageRotationConfigEntry
                    {
                        Md5 = hash,
                        FileName = fileName,
                        Rotation = NormalizeRotation(rotation)
                    });
                }
                else
                {
                    existing.FileName = fileName;
                    existing.Rotation = NormalizeRotation(rotation);
                }

                SaveConfig(folderPath, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return false;
            }

            try
            {
                var folderPath = Path.GetDirectoryName(imagePath);
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return false;
                }

                var hash = ComputeMd5Hex(imagePath);
                var config = LoadConfig(folderPath);
                var removed = config.Images.RemoveAll(i => string.Equals(i.Md5, hash, StringComparison.OrdinalIgnoreCase)) > 0;
                if (!removed)
                {
                    return false;
                }

                SaveConfig(folderPath, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private ImageRotationConfigEntry? GetOrUpdateEntryForImage(string imagePath, out bool updated)
        {
            updated = false;

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                var folderPath = Path.GetDirectoryName(imagePath);
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return null;
                }

                var fileName = Path.GetFileName(imagePath);
                var hash = ComputeMd5Hex(imagePath);
                var config = LoadConfig(folderPath);
                var entry = config.Images.FirstOrDefault(i => string.Equals(i.Md5, hash, StringComparison.OrdinalIgnoreCase));

                if (entry is null)
                {
                    return null;
                }

                if (!string.Equals(entry.FileName, fileName, StringComparison.Ordinal))
                {
                    entry.FileName = fileName;
                    SaveConfig(folderPath, config);
                    updated = true;
                }

                entry.Rotation = NormalizeRotation(entry.Rotation);
                return entry;
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeMd5Hex(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }

        private static int NormalizeRotation(int rotation)
        {
            return ((rotation % 360) + 360) % 360;
        }

        private static ImageRotationConfigDocument LoadConfig(string folderPath)
        {
            var configPath = Path.Combine(folderPath, ConfigFileName);
            if (!File.Exists(configPath))
            {
                return new ImageRotationConfigDocument();
            }

            var json = File.ReadAllText(configPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ImageRotationConfigDocument();
            }

            try
            {
                return JsonSerializer.Deserialize<ImageRotationConfigDocument>(json) ?? new ImageRotationConfigDocument();
            }
            catch
            {
                return new ImageRotationConfigDocument();
            }
        }

        private static void SaveConfig(string folderPath, ImageRotationConfigDocument config)
        {
            var configPath = Path.Combine(folderPath, ConfigFileName);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(configPath, json);
        }

        private sealed class ImageRotationConfigDocument
        {
            public List<ImageRotationConfigEntry> Images { get; init; } = new();
        }

        private sealed class ImageRotationConfigEntry
        {
            public string Md5 { get; init; } = string.Empty;

            public string FileName { get; set; } = string.Empty;

            public int Rotation { get; set; }
        }
    }
}
