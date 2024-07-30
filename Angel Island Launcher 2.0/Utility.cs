/***************************************************************************
 *
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using Diagnostics;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Angel_Island_Launcher_2._0
{
    public static class Utility
    {
        public static int ToInt32(string value, int defaultValue = 0)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
            }

            return defaultValue;
        }

        public static void DownloadFile(string downloadUrl, string targetPath)
        {
            LogHelper logger = new LogHelper("DownloadFile.log", true, false);
            Program.SetProgress(0);
            Program.PostMessage("Downloading {0}...", downloadUrl);
            logger.Log(string.Format("Downloading {0}...", downloadUrl));

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs args) =>
                    {
                        Program.SetProgress((int)(100 * args.BytesReceived / args.TotalBytesToReceive));
                    };

                    webClient.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) =>
                    {
                        Program.SetProgress(100);
                        Program.PostMessage("File download complete.");
                        logger.Log("File download complete.");
                    };

                    webClient.DownloadFileTaskAsync(downloadUrl, targetPath).Wait();
                }
            }
            catch (Exception ex)
            {
                logger.Log(string.Format("Exception caught while trying to download: {0}...", ex.Message));
            }
            logger.Finish();
        }

        public static void ExtractArchive(string sourceArchive, string targetDirectory)
        {
            LogHelper logger = new LogHelper("ExtractArchive.log", true, false);
            Program.SetProgress(0);
            Program.PostMessage("Extracting {0}...", sourceArchive);
            logger.Log(string.Format("Extracting {0}...", sourceArchive));

            try
            {
                using (FileStream fileStream = new FileStream(sourceArchive, FileMode.Open))
                using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    int entriesProcessed = 0;

                    foreach (ZipArchiveEntry zipEntry in zipArchive.Entries)
                    {
                        string targetPath = Path.Combine(targetDirectory, zipEntry.FullName);

                        if (Path.GetFileName(targetPath).Length == 0)
                        {
                            Directory.CreateDirectory(targetPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                            zipEntry.ExtractToFile(targetPath, true);
                        }

                        Program.SetProgress(100 * ++entriesProcessed / zipArchive.Entries.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(string.Format("Exception caught while extracting: {0}...", ex.Message));
            }
            Program.SetProgress(100);
            Program.PostMessage("Archive extraction complete.");
            logger.Log("Archive extraction complete.");
            logger.Finish();
        }

        public static void MoveFiles(string sourceDirectory, string targetDirectory)
        {
            LogHelper logger = new LogHelper("MoveFiles.log", true, false);
            Program.SetProgress(0);
            Program.PostMessage("Moving files from {0} to {1}...", sourceDirectory, targetDirectory);
            logger.Log(string.Format("Moving files from {0} to {1}...", sourceDirectory, targetDirectory));

            try
            {
                int filesMoved = 0;
                string[] files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);

                foreach (string sourcePath in files)
                {
                    string targetPath = Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, sourcePath));

                    EnsureDirectory(Path.GetDirectoryName(targetPath));

                    File.Move(sourcePath, targetPath, true);

                    Program.SetProgress(100 * ++filesMoved / files.Length);
                }
            }
            catch(Exception ex)
            {
                logger.Log(string.Format("Exception caught while moving files: {0}...", ex.Message));
            }

            Program.SetProgress(100);
            Program.PostMessage("Move operation complete.");
            logger.Log("Move operation complete.");
            logger.Finish();
        }

        public static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public class JsonConverterIntOrStr : JsonConverter<string>
        {
            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }

            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                try
                {
                    return JsonSerializer.Deserialize<int>(ref reader, options).ToString();
                }
                catch
                {
                    return JsonSerializer.Deserialize<string>(ref reader, options);
                }
            }
        }

        public static Version GetLauncherVersion()
        {
            LogHelper logger = new LogHelper("GetLauncherVersion.log", true, false);
            logger.Log(string.Format("GetLauncherVersion: {0}", GetAppVersion(Program.AppName)));
            logger.Finish();
            return GetAppVersion(Program.AppName);
        }

        public static Version GetAppVersion(string appName)
        {
            LogHelper logger = new LogHelper("GetAppVersion.log", true, false);
            logger.Log(string.Format("GetAppVersion for: {0}", appName));
            
            RegistryKey key = SearchRegistry(appName);

            if (key != null)
            {
                logger.Log(string.Format("GetAppVersion: {0}", new Version(key.GetValue("DisplayVersion") as string)));
                logger.Finish();
                return new Version(key.GetValue("DisplayVersion") as string);
            }
            else
            {
                logger.Log(string.Format("GetAppVersion: {0}", new Version()));
                logger.Finish();
                return new Version();
            }
        }

        public static RegistryKey SearchRegistry(string appName)
        {
            RegistryKey key;
            string displayName;

            // search in: CurrentUser
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey subKey = key.OpenSubKey(keyName);
                displayName = subKey.GetValue("DisplayName") as string;
                if (appName == displayName)
                    return subKey;
            }

            // search in: LocalMachine_32
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey subKey = key.OpenSubKey(keyName);
                displayName = subKey.GetValue("DisplayName") as string;
                if (appName == displayName)
                    return subKey;
            }

            // search in: LocalMachine_64
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (string keyName in key.GetSubKeyNames())
            {
                RegistryKey subKey = key.OpenSubKey(keyName);
                displayName = subKey.GetValue("DisplayName") as string;
                if (appName == displayName)
                    return subKey;
            }

            return null;
        }

        public static bool PathsEqual(string path1, string path2)
        {
            return (Path.GetFullPath(path1) == Path.GetFullPath(path2));
        }
    }
}