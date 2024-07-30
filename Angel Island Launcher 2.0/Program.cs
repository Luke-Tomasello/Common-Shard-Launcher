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

/* Angel Island\Launcher 2.0\Angel Island Launcher 2.0\Program.cs
 * CHANGELOG:
 *  12/18/22, Adam (Launcher Daemon)
 *      LchrDaemon is now responsible for the installation of launcher updates and restarting the launcher 
 *          after the updates have completed.
 *      LchrDaemon is a separate project within this solution, and is built and published just like the main launcher. 
 *          It is then installed alongside the launcher.
 *      
 *      Special Characters, their meaning, and Convention:
 *      '?' is used to delineate command lines passed to LchrDaemon. i.e., ?Angel Island Launcher 2.0.exe?
 *      '*' is used denote a double quote, i.e., "hello world" would be passed to the LchrDaemon as *hello world*.
 *          this is because double quotes are processed and absorbed by a processes startup code, and then removed.
 *          
 *      Directories: 
 *          LchrDaemon is 'copied' into and run from the temp directory before an update occurs. This is to 
 *              prevent the install package (msi) from trying to update this module which would otherwise be in use.
 *              
 *      How it works: When the Launcher wishes to update, it copies the daemon files to the temp folder, creates a couple
 *          command lines, then launches the LchrDaemon with these command lines. For example:
 *          LchrDaemon ?Angel Island Launcher 2.0.exe? ?/qn /i *temp/Angel Island Launcher 2.0.zip*?
 *          The LchrDaemon will then launch msiexec and WaitForExit. It will then relaunch the Launcher.
 *  11/17/22, Adam (FindUODirectories())
 *      Comment out FindUODirectories() to ensure we are running the correct client with the correct maps.
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Diagnostics;

namespace Angel_Island_Launcher_2._0
{
    static class Program
    {
        public const string AppName = "Angel Island Launcher";

        public const string UODownloadUrl = "http://www.game-master.net/resources/AI_Golden_Client.zip";
        public const string CUODownloadUrl = "http://www.game-master.net/resources/ClassicUO.zip";  // https://classicuo.eu/dev/deploy/ClassicUO-dev-preview-release.zip
        public const string RazorDownloadUrl = "http://www.game-master.net/resources/Razor.zip";    // https://github.com/markdwags/Razor/releases/download/v1.7.3.36/Razor-v1.7.3.36.zip
        public const string DiffDownloadUrl = "http://www.game-master.net/resources/diff.zip";

        public const string ServerName = "Angel Island";
        public const string ServerAddress = "uoangelisland.com";
        public const int ServerPort = 2593;
        public const string UODirectory = "UO Angel Island";
        public const string CUODirectory = "ClassicUO";
        public const string RazorDirectory = "RazorCE";

        public const string LauncherVersionUrl = "http://www.game-master.net/resources/launcherversion.txt";
        public const string LauncherDownloadUrl = "http://www.game-master.net/resources/Angel Island Launcher-2.0.zip";

        public const string TmpDirectory = "temp";
        public const string MsiName = "Angel Island Launcher.msi";

        public const string PackageVersionsUrl = "http://www.game-master.net/resources/packageversions.txt";

        public const string LogsDirectory = "Logs";
        public const string JsonFileName = "appstate.json";

        public const bool ClientRestrictions = true;

        private static AppState m_AppState;
        private static Profile m_CurrentProfile;
        private static AILauncher m_Launcher;
        private static Version m_AppVersion;

        public static AppState AppState
        {
            get { return m_AppState; }
        }

        public static Profile CurrentProfile
        {
            get { return m_CurrentProfile; }
            set { m_CurrentProfile = value; }
        }

        public static Version AppVersion
        {
            get { return m_AppVersion; }
        }

        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;

            Application.ThreadException += GlobalThreadExceptionHandler;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

#if DEBUG
            AllocConsole();
#endif

            m_AppVersion = Utility.GetLauncherVersion();

            LogHelper logger = new LogHelper("Program Log.log", true, false);
            logger.Log("Setup started");

            // clean up the temp directory
            if (Directory.Exists(TmpDirectory))
            {
                logger.Log("clean up the temp directory");
                File.Delete(Path.Combine(TmpDirectory, Path.GetFileName(LauncherDownloadUrl)));
                File.Delete(Path.Combine(TmpDirectory, MsiName));
                File.Delete(Path.Combine(TmpDirectory, "setup.exe"));
                logger.Log("Uninstall Daemon");
                UninstallDaemon();

                if (Directory.GetFiles(TmpDirectory).Length == 0)
                    Directory.Delete(TmpDirectory);
            }

            if (!ClientRestrictions)
                FindUODirectories();

            logger.Log("Loading appstate...");
            LoadAppState();
            if (m_AppState != null)
                logger.Log("Done.");

            if (m_AppState == null)
            {
                logger.Log("Appstate was null, creating...");
                m_AppState = new AppState();

                m_AppState.EnableUpdates = true;
                m_AppState.EnableAudio = true;

                InitProfiles();

                if (m_AppState.Profiles.Count != 0)
                    m_AppState.LastProfile = m_AppState.Profiles[0].Name;
                logger.Log("Done.");
            }

            logger.Log("Loading profile...");
            LoadProfile(m_AppState.LastProfile);
            logger.Log("Done.");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            logger.Log("Running new AILauncher...");
            Application.Run(m_Launcher = new AILauncher());
            logger.Log("Done.");
            SaveAppState();

#if DEBUG
            FreeConsole();
#endif
            logger.Finish();
        }

        private static JsonSerializerOptions m_JsonOptions;

        private static JsonSerializerOptions JsonOptions
        {
            get
            {
                if (m_JsonOptions == null)
                {
                    m_JsonOptions = new JsonSerializerOptions();
                    m_JsonOptions.WriteIndented = true;
                }

                return m_JsonOptions;
            }
        }

        public static void SaveAppState()
        {
            File.WriteAllText(JsonFileName, JsonSerializer.Serialize(m_AppState, JsonOptions));
        }

        public static void LoadAppState()
        {
            if (File.Exists(JsonFileName))
                m_AppState = JsonSerializer.Deserialize<AppState>(File.ReadAllText(JsonFileName), JsonOptions);
        }

        public static void LoadProfile(string name)
        {
            int index = -1;

            for (int i = 0; i < AppState.Profiles.Count; i++)
            {
                if (AppState.Profiles[i].Name == name)
                {
                    index = i;
                    break;
                }
            }

            int result;

            // try loading by index
            if (index == -1 && int.TryParse(name, out result))
                index = result;

            LoadProfile(index);
        }

        public static void LoadProfile(int index)
        {
            Profile profile = null;

            if (index >= 0 && index < m_AppState.Profiles.Count)
                profile = m_AppState.Profiles[index];

            LoadProfile(profile);
        }

        public static void LoadProfile(Profile profile)
        {
            m_CurrentProfile = profile;

            if (profile != null)
                m_AppState.LastProfile = profile.Name;

            if (m_Launcher != null)
                m_Launcher.ReloadProfile();
        }

        public static void AddProfile(Profile profile)
        {
            m_AppState.Profiles.Add(profile);

            if (m_Launcher != null)
                m_Launcher.ReloadProfileList();

            LoadProfile(m_AppState.Profiles.Count - 1);
        }

        public static void RemoveProfile()
        {
            Profile profileRem = m_CurrentProfile;

            if (profileRem == null)
                return;

            if (MessageBox.Show(String.Format("Are you sure you wish to remove the profile \"{0}\"?", profileRem.Name), "Angel Island Launcher", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            int index = m_AppState.Profiles.IndexOf(profileRem);

            if (index != -1)
            {
                m_AppState.Profiles.RemoveAt(index);

                if (m_Launcher != null)
                    m_Launcher.ReloadProfileList();

                LoadProfile(index - 1);
            }
        }

        public static void InitProfiles()
        {
            Profile profile;

            // Game-Master.net
            m_AppState.Profiles.Add(profile = NewProfile());
            profile.Name = "Game-Master.net";
            profile.ServerName = null;

            // Angel Island
            m_AppState.Profiles.Add(profile = NewProfile());
            profile.Name = "Angel Island";
            profile.ServerName = "Angel Island";

            // Siege Perilous
            m_AppState.Profiles.Add(profile = NewProfile());
            profile.Name = "Siege Perilous";
            profile.ServerName = "Siege Perilous";
        }

        public static Profile NewProfile(Profile copy = null)
        {
            Profile profile = new Profile();

            profile.Name = FixName("New Profile");

            if (copy != null)
            {
                profile.ServerAddress = copy.ServerAddress;
                profile.ServerPort = copy.ServerPort;
                profile.Username = copy.Username;
                profile.Password = copy.Password;
                profile.UseCUO = copy.UseCUO;
                profile.UseRazor = copy.UseRazor;
                profile.UODirectory = copy.UODirectory;
                profile.CUODirectory = copy.CUODirectory;
                profile.RazorDirectory = copy.RazorDirectory;
            }
            else
            {
                profile.ServerAddress = ServerAddress;
                profile.ServerPort = ServerPort;
                profile.UseCUO = true;
                profile.UseRazor = true;

                if (m_UODirectories.Count != 0)
                    profile.UODirectory = m_UODirectories[0];
                else
                    profile.UODirectory = UODirectory;

                profile.CUODirectory = CUODirectory;
                profile.RazorDirectory = RazorDirectory;
            }

            return profile;
        }

        private static string FixName(string name)
        {
            int maxNumber = -1;

            foreach (Profile profile in m_AppState.Profiles)
            {
                if (profile.Name.StartsWith(name))
                {
                    if (profile.Name.Length == name.Length)
                    {
                        if (maxNumber < 0)
                            maxNumber = 0;
                    }
                    else if (profile.Name.Length > name.Length + 1 && profile.Name[name.Length] == ' ')
                    {
                        string word = profile.Name.Substring(name.Length + 1);

                        int number;

                        if (int.TryParse(word, out number) && number > maxNumber)
                            maxNumber = number;
                    }
                }
            }

            if (maxNumber >= 0)
                return String.Format("{0} {1}", name, maxNumber + 1);

            return name;
        }

        private static readonly List<string> m_UODirectories = new List<string>();

        public static List<string> UODirectories { get { return m_UODirectories; } }

        private static readonly string[] m_PossibleDriveNames = new string[]
            {
                @"C:\",
                @"D:\",
                @"E:\",
                @"F:\",
                @"G:\"
            };

        private static readonly string[] m_PossibleParentDirectories = new string[]
            {
                @"Program Files",
                @"Program Files (x86)"
            };

        private static readonly string[] m_PossibleUODirectories = new string[]
            {
                @"Ultima Online",
                @"Ultima Online Classic",
                @"UO Angel Island"
            };

        public static void FindUODirectories()
        {
            m_UODirectories.Clear();

            string uoPathFromRegistry = (string)Registry.GetValue("HKEY_LOCAL_MACHINE", @"Software\Origin Worlds Online\Ultima Online\1.0", "[none]");

            if (uoPathFromRegistry != "[none]")
                m_UODirectories.Add(uoPathFromRegistry);

            foreach (string driveName in m_PossibleDriveNames)
            {
                foreach (string parentDirectory in m_PossibleParentDirectories)
                {
                    foreach (string uoDirectory in m_PossibleUODirectories)
                    {
                        string uoPath = Path.Combine(driveName, parentDirectory, uoDirectory);

                        if (Directory.Exists(uoPath))
                            m_UODirectories.Add(uoPath);
                    }
                }
            }
        }

        private static Task m_Task = Task.CompletedTask;

        public static bool TaskInProgress
        {
            get { return !m_Task.IsCompleted; }
        }

        public static void InstallPackages()
        {
            Profile profile = m_CurrentProfile;

            if (profile == null)
            {
                MessageBox.Show("No profile was selected. Create a new profile in the Profile page.");
                return;
            }

            bool installing = false;

            string uoDirectory = GetUODirectory(profile);

            if (!Directory.Exists(uoDirectory))
            {
                if (MessageBox.Show("The Ultima Online client could not be found at the specified path. Would you like to download the Angel Island client from the Angel Island website?", "Angel Island Launcher", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (m_Launcher != null)
                        m_Launcher.OpenProfilePage();

                    // reset the install directory
                    profile.UODirectory = uoDirectory = UODirectory;

                    if (m_Launcher != null)
                        m_Launcher.ReloadInstallDirectories();

                    InstallUO(uoDirectory);
                    InstallDiff(uoDirectory);

                    installing = true;
                }
            }

            if (GetUseCUO(profile))
            {
                string cuoDirectory = GetCUODirectory(profile);

                if (!Directory.Exists(cuoDirectory))
                {
                    if (MessageBox.Show("Classic UO could not be found at the specified path. Would you like to download the Classic UO package from the Angel Island website?", "Angel Island Launcher", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (m_Launcher != null)
                            m_Launcher.OpenProfilePage();

                        // reset the install directory
                        profile.CUODirectory = cuoDirectory = CUODirectory;

                        if (m_Launcher != null)
                            m_Launcher.ReloadInstallDirectories();

                        InstallCUO(cuoDirectory);

                        installing = true;
                    }
                }
            }

            if (GetUseRazor(profile))
            {
                string razorDirectory = GetRazorDirectory(profile);

                if (!Directory.Exists(razorDirectory))
                {
                    if (MessageBox.Show("Razor CE could not be found at the specified path. Would you like to download the Razor CE package from the Angel Island website?", "Angel Island Launcher", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (m_Launcher != null)
                            m_Launcher.OpenProfilePage();

                        // reset the install directory
                        profile.RazorDirectory = razorDirectory = RazorDirectory;

                        if (m_Launcher != null)
                            m_Launcher.ReloadInstallDirectories();

                        InstallRazor(razorDirectory);

                        installing = true;
                    }
                }
            }

            if (installing)
            {
                m_Task = m_Task.ContinueWith(t =>
                {
                    SetProgress(100);

                    PostMessage("Installation complete.");
                });
            }
            else
            {
                MessageBox.Show("There is nothing to install.");
            }
        }

        // only do updates if the packages are at their default locations
        public static void CheckUpdatePackages()
        {
            Profile profile = m_CurrentProfile;

            if (profile == null)
                return;

            m_Task = m_Task.ContinueWith(t =>
            {
                VersionInfo versionInfo = LoadPackageVersions();

                // we are currently in a separate task/thread
                // call "PostAction" to ensure the following code is executed in the main form thread
                PostAction(() =>
                {
                    bool updating = false;

                    string uoDirectory = GetUODirectory(profile);

                    if (Utility.PathsEqual(uoDirectory, UODirectory) && Directory.Exists(uoDirectory) && AppState.UOVersion < versionInfo.UOVersion)
                    {
                        if (MessageBox.Show("A new package of the Ultima Online client is available. Would you like to download the Angel Island client from the Angel Island website?", "Angel Island Launcher", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            if (m_Launcher != null)
                                m_Launcher.OpenProfilePage();

                            InstallUO(uoDirectory);

                            updating = true;
                        }
                    }

                    if (Utility.PathsEqual(uoDirectory, UODirectory) && Directory.Exists(uoDirectory) && AppState.DiffVersion < versionInfo.DiffVersion)
                    {
                        if (MessageBox.Show("A new client modification is available. Would you like to download the client modification from the Angel Island website?", "Angel Island Launcher", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            if (m_Launcher != null)
                                m_Launcher.OpenProfilePage();

                            InstallDiff(uoDirectory);

                            updating = true;
                        }
                    }

                    string cuoDirectory = GetCUODirectory(profile);

                    if (Utility.PathsEqual(cuoDirectory, CUODirectory) && Directory.Exists(cuoDirectory) && AppState.CUOVersion < versionInfo.CUOVersion)
                    {
                        if (MessageBox.Show("A new package of Classic UO is available. Would you like to download the Classic UO package from the Angel Island website?", "Angel Island Launcher", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            if (m_Launcher != null)
                                m_Launcher.OpenProfilePage();

                            InstallCUO(cuoDirectory);

                            updating = true;
                        }
                    }

                    string razorDirectory = GetRazorDirectory(profile);

                    if (Utility.PathsEqual(razorDirectory, RazorDirectory) && Directory.Exists(razorDirectory) && AppState.RazorVersion < versionInfo.RazorVersion)
                    {
                        if (MessageBox.Show("A new package of Razor CE is available. Would you like to download the Razor CE package from the Angel Island website?", "Angel Island Launcher", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            if (m_Launcher != null)
                                m_Launcher.OpenProfilePage();

                            InstallRazor(razorDirectory);

                            updating = true;
                        }
                    }

                    if (updating)
                    {
                        m_Task = m_Task.ContinueWith(t =>
                        {
                            SetProgress(100);

                            PostMessage("Updates installed.");
                        });
                    }
                });
            });
        }

        private static void InstallUO(string uoDirectory)
        {
            m_Task = m_Task.ContinueWith(t =>
            {
                string uoArchive = Path.Combine(uoDirectory, Path.GetFileName(UODownloadUrl));

                Utility.EnsureDirectory(uoDirectory);

                Utility.DownloadFile(UODownloadUrl, uoArchive);

                Utility.ExtractArchive(uoArchive, uoDirectory);

                File.Delete(uoArchive);

                string gcDirectory = Path.Combine(uoDirectory, "AI_Golden_Client");

                if (Directory.Exists(gcDirectory))
                {
                    Utility.MoveFiles(gcDirectory, uoDirectory);

                    Directory.Delete(gcDirectory, true);
                }

                AppState.UOVersion = LoadPackageVersions().UOVersion;
            });
        }

        private static void InstallCUO(string cuoDirectory)
        {
            m_Task = m_Task.ContinueWith(t =>
            {
                string cuoArchive = Path.Combine(cuoDirectory, Path.GetFileName(CUODownloadUrl));

                Utility.EnsureDirectory(cuoDirectory);

                Utility.DownloadFile(CUODownloadUrl, cuoArchive);

                Utility.ExtractArchive(cuoArchive, cuoDirectory);

                File.Delete(cuoArchive);

                AppState.CUOVersion = LoadPackageVersions().CUOVersion;
            });
        }

        private static void InstallRazor(string razorDirectory)
        {
            m_Task = m_Task.ContinueWith(t =>
            {
                string razorArchive = Path.Combine(razorDirectory, Path.GetFileName(RazorDownloadUrl));

                Utility.EnsureDirectory(razorDirectory);

                Utility.DownloadFile(RazorDownloadUrl, razorArchive);

                Utility.ExtractArchive(razorArchive, razorDirectory);

                File.Delete(razorArchive);

                AppState.RazorVersion = LoadPackageVersions().RazorVersion;
            });
        }

        private static void InstallDiff(string uoDirectory)
        {
            m_Task = m_Task.ContinueWith(t =>
            {
                string diffArchive = Path.Combine(uoDirectory, Path.GetFileName(DiffDownloadUrl));

                Utility.EnsureDirectory(uoDirectory);

                Utility.DownloadFile(DiffDownloadUrl, diffArchive);

                Utility.ExtractArchive(diffArchive, uoDirectory);

                File.Delete(diffArchive);

                AppState.DiffVersion = LoadPackageVersions().DiffVersion;
            });
        }

        private static VersionInfo LoadPackageVersions()
        {
            VersionInfo versionInfo = new VersionInfo();

            string[] lines;

            using (WebClient client = new WebClient())
            {
                lines = client.DownloadString(PackageVersionsUrl).Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }

            int index = 0;

            for (int i = 0; i < lines.Length && index < 4; i++)
            {
                string line = lines[i];

                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                switch (index++)
                {
                    case 0: versionInfo.UOVersion = Utility.ToInt32(line); break;
                    case 1: versionInfo.CUOVersion = Utility.ToInt32(line); break;
                    case 2: versionInfo.RazorVersion = Utility.ToInt32(line); break;
                    case 3: versionInfo.DiffVersion = Utility.ToInt32(line); break;
                }
            }

            return versionInfo;
        }

        private struct VersionInfo
        {
            public int UOVersion;
            public int CUOVersion;
            public int RazorVersion;
            public int DiffVersion;
        }

        public static void Launch()
        {
            Profile profile = m_CurrentProfile;

            string uoDirectory = GetUODirectory(profile);
            string cuoDirectory = GetCUODirectory(profile);
            string razorDirectory = GetRazorDirectory(profile);

            if (profile == null)
            {
                MessageBox.Show("No profile was selected. Create a new profile in the Profile page.");
            }
            else if (String.IsNullOrEmpty(profile.ServerAddress))
            {
                MessageBox.Show("No server address was specified. Set the server address in the Profile page.");
            }
            else if (profile.ServerPort == 0)
            {
                MessageBox.Show("No server port was specified. Set the server port in the Profile page.");
            }
            else if (String.IsNullOrEmpty(uoDirectory))
            {
                MessageBox.Show("No Ultima Online directory was specified. Set the Ultima Online directory in the Profile page.");
            }
            else if (!Directory.Exists(uoDirectory))
            {
                MessageBox.Show("No Ultima Online directory was found at the specified path. Install Ultima Online in the Profile page.");
            }
            else if (GetUseCUO(profile) && String.IsNullOrEmpty(cuoDirectory))
            {
                MessageBox.Show("No Classic UO directory was specified. Set the Classic UO directory in the Profile page.");
            }
            else if (GetUseCUO(profile) && !Directory.Exists(cuoDirectory))
            {
                MessageBox.Show("No Classic UO directory was found at the specified path. Install Classic UO in the Profile page.");
            }
            else if (GetUseRazor(profile) && String.IsNullOrEmpty(razorDirectory))
            {
                MessageBox.Show("No Razor CE directory was specified. Set the Razor CE directory in the Profile page.");
            }
            else if (GetUseRazor(profile) && !Directory.Exists(razorDirectory))
            {
                MessageBox.Show("No Razor CE directory was found at the specified path. Install Razor CE in the Profile page.");
            }
            else if (GetUseCUO(profile))
            {
                // TODO: Write settings.json?
                /*
                string settingsPath = Path.Combine(cuoDirectory, "settings.json");

                JsonNode json;

                if (File.Exists(settingsPath))
                    json = JsonNode.Parse(File.ReadAllText(settingsPath));
                else
                    json = JsonNode.Parse("{}"); // TODO: Is this a proper way to instantiate an empty json node?

                json["ultimaonlinedirectory"] = Path.GetFullPath(uoDirectory);

                File.WriteAllText(settingsPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                */

                string cuoPath = Path.Combine(cuoDirectory, "ClassicUO.exe");

                List<string> args = new List<string>();

                args.Add($"-ip {profile.ServerAddress}");
                args.Add($"-port {profile.ServerPort}");
                args.Add($"-uopath \"{Path.GetFullPath(uoDirectory)}\"");

                bool hasUsername = !String.IsNullOrEmpty(profile.Username);
                bool hasPassword = !String.IsNullOrEmpty(profile.Password);

                if (hasUsername)
                    args.Add($"-username \"{profile.Username}\"");

                if (hasPassword)
                    args.Add($"-password \"{profile.Password}\"");

                if (hasUsername && hasPassword)
                    args.Add($"-skiploginscreen");

                if (!String.IsNullOrEmpty(profile.ServerName))
                    args.Add($"-last_server_name \"{profile.ServerName}\"");

                string plugins;

                if (GetUseRazor(profile))
                    plugins = Path.GetFullPath(Path.Combine(razorDirectory, "Razor.exe"));
                else
                    plugins = String.Empty; // makes sure that we use no plugins

                args.Add($"-plugins \"{plugins}\"");

                Process.Start(cuoPath, String.Join(' ', args));
            }
            else if (GetUseRazor(profile))
            {
                RazorConfiguration.Configure(profile);

                string razorPath = Path.Combine(razorDirectory, "Razor.exe");

                ProcessStartInfo processStartInfo = new ProcessStartInfo(razorPath);
                processStartInfo.Verb = "runas";

                Process.Start(processStartInfo);
            }
            else
            {
                MessageBox.Show("Launching the Ultima Online client without either Razor CE or Classic UO is currently not supported.");
            }
        }

        public static void CheckUpdateLauncher()
        {
            m_Task = m_Task.ContinueWith(t =>
            {
                Version remoteVersion;
                LogHelper logger = new LogHelper("CheckUpdateLauncher.log", true, false);
                using (WebClient client = new WebClient())
                {
                    logger.Log(string.Format("new WebClient: {0}.", client == null ? "failed" : "ok"));
                    remoteVersion = new Version(client.DownloadString(LauncherVersionUrl));
                    logger.Log(string.Format("remoteVersion: {0}.", remoteVersion.ToString()));
                }

                // we are currently in a separate task/thread
                // call "PostAction" to ensure the following code is executed in the main form thread
                PostAction(() =>
                {
                    if (remoteVersion > m_AppVersion)
                    {
                        logger.Log(string.Format("remoteVersion: {0} > AppVersion: {1}", remoteVersion.ToString(), m_AppVersion.ToString()));
                        if (MessageBox.Show(String.Format("A new version ({0}) of the Angel Island Launcher is available. Would you like to update your launcher?", remoteVersion), "Angel Island Launcher", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        {
                            PostMessage("Launcher update aborted.");
                            logger.Log("Launcher update aborted.");
                            CheckUpdatePackages();
                        }
                        else
                        {
                            logger.Log("UpdateLauncher called...");
                            UpdateLauncher();
                            logger.Log("UpdateLauncher returned.");
                        }
                    }
                    else
                    {
                        logger.Log(string.Format("remoteVersion: {0} <= AppVersion: {1}", remoteVersion.ToString(), m_AppVersion.ToString()));
                        logger.Log("Calling CheckUpdatePackages...");
                        CheckUpdatePackages();
                        logger.Log("CheckUpdatePackages returned.");
                    }
                });

                logger.Finish();
            });
        }

        private static void UpdateLauncher()
        {
            LogHelper logger = new LogHelper("UpdateLauncher.log", true, false);
            logger.Log(string.Format("Launcher == null", m_Launcher == null));
            if (m_Launcher != null)
                m_Launcher.OpenProfilePage();

            m_Task = m_Task.ContinueWith(t =>
            {
                Utility.EnsureDirectory(TmpDirectory);
                logger.Log(string.Format("TmpDirectory: {0}", TmpDirectory));
                string archivePath = Path.Combine(TmpDirectory, Path.GetFileName(LauncherDownloadUrl));
                logger.Log(string.Format("archivePath: {0}", archivePath));
                logger.Log(string.Format("Starting download from '{0}' to '{1}'", LauncherDownloadUrl, archivePath));
                Utility.DownloadFile(LauncherDownloadUrl, archivePath);
                logger.Log(string.Format("DownloadFile returned."));
                logger.Log(string.Format("ExtractArchive from '{0}' to '{1}'", archivePath, TmpDirectory));
                Utility.ExtractArchive(archivePath, TmpDirectory);
                logger.Log(string.Format("ExtractArchive returned."));
                logger.Log(string.Format("Cleaning up '{0}' and '{1}'", archivePath, TmpDirectory));
                File.Delete(archivePath);
                File.Delete(Path.Combine(TmpDirectory, "setup.exe"));
                logger.Log(string.Format("Installing InstallDaemon..."));
                InstallDaemon();
                logger.Log(string.Format("Installing InstallDaemon complete."));

                Process process = new Process();
                process.StartInfo.FileName = Path.Combine(TmpDirectory, "LchrDaemon.exe");
                process.StartInfo.Arguments = FormatDaemonArgs();
                process.StartInfo.Verb = "runas";
                logger.Log(string.Format("Starting InstallDaemon '{0}' with arguments '{1}'", process.StartInfo.FileName, process.StartInfo.Arguments));
                process.Start();

                if (m_Launcher != null)
                {
                    logger.Log(string.Format("Closing Launcher."));
                    logger.Finish();
                    m_Launcher.Close();
                }
            });
        }

        private static string FormatDaemonArgs()
        {
            // use "*" to denote a double quote
            // use "?" as argument delimiter

            // Note: While we run the Daemon from the temp folder (temp/LchrDaemon.exe),
            // the Daemon takes on the working directory from this application, i.e. the
            // main launcher folder. Therefore, when passing the path to the MSI file,
            // we have to prefix it with "temp\".

            string exeName = Path.GetFullPath(Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName));
            string msiArgs = String.Format("/qn /i *{0}* TARGETDIR=*{1}*", Path.Combine(TmpDirectory, MsiName), Environment.CurrentDirectory);

            return String.Format("{0}?{1}", exeName, msiArgs);
        }

        private static void InstallDaemon()
        {
            File.Copy("LchrDaemon.deps.json", Path.Combine(TmpDirectory, "LchrDaemon.deps.json"), true);
            File.Copy("LchrDaemon.dll", Path.Combine(TmpDirectory, "LchrDaemon.dll"), true);
            File.Copy("LchrDaemon.exe", Path.Combine(TmpDirectory, "LchrDaemon.exe"), true);
            File.Copy("LchrDaemon.pdb", Path.Combine(TmpDirectory, "LchrDaemon.pdb"), true);
            File.Copy("LchrDaemon.runtimeconfig.json", Path.Combine(TmpDirectory, "LchrDaemon.runtimeconfig.json"), true);
        }

        private static void UninstallDaemon()
        {
            File.Delete(Path.Combine(TmpDirectory, "LchrDaemon.deps.json"));
            File.Delete(Path.Combine(TmpDirectory, "LchrDaemon.dll"));
            File.Delete(Path.Combine(TmpDirectory, "LchrDaemon.exe"));
            File.Delete(Path.Combine(TmpDirectory, "LchrDaemon.pdb"));
            File.Delete(Path.Combine(TmpDirectory, "LchrDaemon.runtimeconfig.json"));
        }

        public static void SetProgress(int perc)
        {
            if (m_Launcher != null)
                m_Launcher.SetProgress(perc);
        }

        public static void PostMessage(string format, params object[] args)
        {
            if (m_Launcher != null)
                m_Launcher.PostMessage(format, args);
        }

        public static void PostAction(Action action)
        {
            if (m_Launcher != null)
                m_Launcher.PostAction(action);
        }

        public static string GetUODirectory(Profile profile)
        {
            if (ClientRestrictions)
                return UODirectory;

            return profile.UODirectory;
        }

        public static string GetCUODirectory(Profile profile)
        {
            if (ClientRestrictions)
                return CUODirectory;

            return profile.CUODirectory;
        }

        public static string GetRazorDirectory(Profile profile)
        {
            if (ClientRestrictions)
                return RazorDirectory;

            return profile.RazorDirectory;
        }

        public static bool GetUseCUO(Profile profile)
        {
            if (ClientRestrictions)
                return true;

            return profile.UseCUO;
        }

        public static bool GetUseRazor(Profile profile)
        {
            if (ClientRestrictions)
                return true;

            return profile.UseRazor;
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((Exception)e.ExceptionObject);
        }

        private static void GlobalThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            LogException(e.Exception);
        }

        private static void LogException(Exception ex)
        {
            Utility.EnsureDirectory(LogsDirectory);

            string path = Path.Join(LogsDirectory, String.Concat(DateTime.Now.ToString("y-M-d_h-m-s"), "_error.log"));

            string log = String.Concat(ex.Message, "\n", ex.StackTrace);

            File.WriteAllText(path, log);

#if DEBUG
            Console.WriteLine(log);
#endif

            MessageBox.Show($"Unhandled exception caught: {ex.Message}\nSee \"{path}\" for more information.");

            // TODO: Save AppState?
        }

        private static void Log(string text)
        {
            Utility.EnsureDirectory(LogsDirectory);

            string path = Path.Combine(LogsDirectory, "trace.log");

            string[] log = new string[] { text + "\n" };

            File.AppendAllLines(path, log);

#if DEBUG
            Console.WriteLine(log);
#endif
        }

#if DEBUG
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int FreeConsole();
#endif
    }
}