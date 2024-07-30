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

/* Angel Island\Launcher 2.0\LchrDaemon\Program.cs
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
 */

using System.Diagnostics;

namespace LchrDaemon
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string exePath, msiArgs;

                ProcessArgs(args, out exePath, out msiArgs);

                Console.WriteLine("LchrDaemon: Starting...");
                Console.WriteLine("LchrDaemon: Asked to start {0}", exePath);
                Console.WriteLine("LchrDaemon: Asked to install {0}", msiArgs);

                Console.WriteLine("LchrDaemon: Installing...");
                StartMsi(msiArgs);

                Console.WriteLine("LchrDaemon: Waiting...");
                Thread.Sleep(1000);

                Console.WriteLine("LchrDaemon: Launching...");
                StartApp(exePath);

                Console.WriteLine("LchrDaemon: Exiting...");
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine("LchrDaemon: Error: {0}", e);
                Environment.Exit(-1);
            }
        }

        private static void StartMsi(string msiArgs)
        {
            Process process = new Process();
            process.StartInfo.FileName = "msiexec.exe";
            process.StartInfo.Arguments = msiArgs;
            process.StartInfo.Verb = "runas";
            process.Start();
            process.WaitForExit(15000);
        }

        private static void StartApp(string exePath)
        {
            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = null;
            process.StartInfo.Verb = "runas";
            process.Start();
        }

        private static void ProcessArgs(string[] args, out string exePath, out string msiArgs)
        {
            string[] split = String.Join(' ', args).Replace('*', '\"').Split(new char[] { '?' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (split.Length != 2)
                throw new InvalidOperationException();

            exePath = split[0];
            msiArgs = split[1];
        }
    }
}