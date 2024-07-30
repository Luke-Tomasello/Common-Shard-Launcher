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

/* Scripts/Commands/LogHelper.cs
 * ChangeLog
 *  6/20/2023, Adam
 *      Port over to launcher
 *      Cleanup still needed.
 *  11/24/22, Adam
 *      Switch to using GameTime instead of local time to make deciphering logs a bit easier.
 *  8/26/22, Adam
 *      Add support for caller supplies paths - paths other than '.\logs
 *      Add support for 'quiet' mode
 *  7/30/22, Adam
 *      Add LogBlockedConnection()
 *      This logs blocked connections due to firewall, IPLimits, and HardwareLimits
 *  12/2/21, Adam
 *      Create the ./logs directory if it doesn't already exist
 *  9/26/21, Adam (logger Finish())
 *      why are we removing spaces from the outout text to the Console?
        It messes up one line output. Remove those lines for now
 *	6/18/10, Adam
 *		o Added a cleanup procedure to the Cheater function to prevent players comments from growing out of control
 *		o Add the region ID to the output
 *	5/17/10, Adam
 *		o Add new Format() command that takes no additional text data
 *			Format(LogType logtype, object data)
 *		o Don't output time stamp on intermediate results created with Format()
 *	3/22/10, adam
 *		separate the formatting the logging so we can format our own strings before write
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	8/28/07, Adam
 *		Add new EventSink for ItemAdded via [add
 *		Dedesign LogHelper EventSink logic to be static and not instance based.
 *  3/28/07, Adam
 *      Add protections around Cheater()
 *  3/26/07, Adam
 *      Limit game console output to the first 25 items
 *	01/07/07 - Pix
 *		Added new LogException override: LogException(Exception ex, string additionalMessage)
 *	10/20/06, Adam
 *		Put back auto-watchlisting and comments from Cheater logging.
 *		Removed auto-watchlisting and comments from TrackIt logging.
 *	10/20/06, Pix
 *		Removed auto-watchlisting and comments from Cheater logging.
 *	10/17/06, Adam
 *		Add new Cheater() logging functions.
 *	9/9/06, Adam
 *		- Add Name and Serial for type Item display
 *		- normalized LogType.Item and LogType.ItemSerial
 *  01/09/06, Taran Kain
 *		Added m_Finished, Crashed/Shutdown handlers to make sure we write the log
 *  12/24/05, Kit
 *		Added ItemSerial log type that adds serial number to standered item log type.
 *	11/14/05, erlein
 *		Added extra function to clear in-memory log.
 *  10/18/05, erlein
 *		Added constructors with additional parameter to facilitate single line logging.
 *	03/28/05, erlein
 *		Added additional parameter for Log() to allow more cases
 *		where generic item and mobile logging can take place.
 *		Normalized format of common fields at start of each log line.
 *	03/26/05, erlein
 *		Added public interface to m_Count via Count so can add
 *		allowance for headers & footers.
 *	03/25/05, erlein
 *		Updated to log decimal serials instead of hex.
 *		Replaced root type name output with serial for items
 *		with mobile roots.
 *	03/23/05, erlein
 *		Initial creation
 */

using System;
using System.Collections;
using System.IO;

using Mobile = System.Collections.ArrayList;
using Item = System.Collections.ArrayList;
using Serial = System.Collections.ArrayList;
using Angel_Island_Launcher_2._0;

namespace Diagnostics
{

    public class LogHelper
    {
        private ArrayList m_LogFile;
        private string m_LogFilename;
        private int m_MaxOutput = 25;   // only display first 25 lines
        private int m_Count;
        private static ArrayList m_OpenLogs = new ArrayList();
        public static ArrayList OpenLogs { get { return m_OpenLogs; } }

        public int Count
        {
            get
            {
                return m_Count;
            }
            set
            {
                m_Count = value;
            }
        }

        private bool m_Overwrite;
        private bool m_SingleLine;
        private bool m_Quiet;
        private DateTime m_StartTime;
        private bool m_Finished;

        private Mobile m_Person;


        // Construct with : LogHelper(string filename (, Mobile mobile ) (, bool overwrite) )

        // Default append, no mobile constructor
        public LogHelper(string filename)
        {
            m_Overwrite = false;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Mob spec. constructor
        public LogHelper(string filename, Mobile from)
        {
            m_Overwrite = false;
            m_Person = from;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Overwrite spec. constructor
        public LogHelper(string filename, bool overwrite)
        {
            m_Overwrite = overwrite;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Overwrite and singleline constructor
        public LogHelper(string filename, bool overwrite, bool sline, bool quiet = false)
        {
            m_Overwrite = overwrite;
            m_LogFilename = filename;
            m_SingleLine = sline;
            m_Quiet = quiet;
            Start();
        }

        // Overwrite + mobile spec. constructor
        public LogHelper(string filename, Mobile from, bool overwrite)
        {
            m_Overwrite = overwrite;
            m_Person = from;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Overwrite, mobile spec. and singleline constructor
        public LogHelper(string filename, Mobile from, bool overwrite, bool sline)
        {
            m_Overwrite = overwrite;
            m_Person = from;
            m_LogFilename = filename;
            m_SingleLine = sline;

            Start();
        }

        public static void LogException(Exception ex, string additionalMessage)
        {
            try
            {
                LogHelper Logger = new LogHelper("Exception.log", false);
                string text = String.Format("{0}\r\n{1}\r\n{2}", additionalMessage, ex.Message, ex.StackTrace);
                Logger.Log(LogType.Text, text);
                Logger.Finish();
                Console.WriteLine(text);
            }
            catch
            {
                // do nothing here as we do not want to enter a "cycle of doom!"
                //  Basically, we do not want the caller to catch an exception here, and call
                //  LogException() again, where it throws another exception, and so forth
            }
        }
        // Clear in memory log
        public void Clear()
        {
            m_LogFile.Clear();
        }

        // Record start time and init counter + list
        private void Start()
        {
            m_StartTime = DateTime.Now;
            m_Count = 0;
            m_Finished = false;
            m_LogFile = new ArrayList();

            if (!m_SingleLine && !m_Quiet)
                m_LogFile.Add(string.Format("Log start : {0}", m_StartTime));

            m_OpenLogs.Add(this);
        }

        // Log all the data and close the file
        public void Finish()
        {
            if (!m_Finished)
            {
                m_Finished = true;
                TimeSpan ts = DateTime.Now - m_StartTime;

                if (!m_SingleLine && !m_Quiet)
                    m_LogFile.Add(string.Format("Completed in {0} seconds, {1} entr{2} logged", ts.TotalSeconds, m_Count, m_Count == 1 ? "y" : "ies"));

                // Report
                string sFilename = string.Empty;

                string path = Path.GetDirectoryName(m_LogFilename);
                if (string.IsNullOrEmpty(path))
                {   // default to "logs" directory
                    sFilename = Path.Combine(Program.LogsDirectory, m_LogFilename);
                }
                else
                {   // use the supplied path
                    sFilename = m_LogFilename;
                }

                path = Path.GetDirectoryName(sFilename);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                StreamWriter LogFile = null;

                try
                {
                    LogFile = new StreamWriter(sFilename, !m_Overwrite);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to open logfile '{0}' for writing : {1}", sFilename, e);
                }

                // Loop through the list stored and log
                for (int i = 0; i < m_LogFile.Count; i++)
                {

                    if (LogFile != null)
                        LogFile.WriteLine(m_LogFile[i]);

                    // Send message to the player too
                    if (m_Person is Mobile) // was PlayerMobile
                    {
                        m_MaxOutput--;

                        if (m_MaxOutput > 0)
                        {   // 9/26/21, Adam, why are we removing spaces from the outout text?
                            //  It messes up one line output
                            /*if (i + 1 < m_LogFile.Count && i != 0)
                                m_Person.SendMessage(((string)m_LogFile[i]).Replace(" ", ""));
                            else*/
                            //m_Person.SendMessage((string)m_LogFile[i]);
                        }
                        else if (m_MaxOutput == 0)
                        {
                            //m_Person.SendMessage("Skipping remainder of output. See log file.");
                        }
                    }
                }

                // If successfully opened a stream just now, close it off!

                if (LogFile != null)
                    LogFile.Close();

                if (m_OpenLogs.Contains(this))
                    m_OpenLogs.Remove(this);
            }
        }

        // Add data to list to be logged : Log( (LogType ,) object (, additional) )

        // Default to mixed type
        public void Log(object data)
        {
            this.Log(LogType.Mixed, data, null);
        }

        // Default to no additional
        public void Log(LogType logtype, object data)
        {
            this.Log(logtype, data, null);
        }

        // Specify LogType
        public void Log(LogType logtype, object data, string additional)
        {
            string LogLine = Format(logtype, data, additional);

            // If this is a "single line" loghelper instance, we need to replace all newline characters
            if (m_SingleLine && !m_Quiet)
            {
                LogLine = LogLine.Replace("\r\n", " || ");
                LogLine = LogLine.Replace("\r", " || ");
                LogLine = LogLine.Replace("\n", " || ");
                LogLine = m_StartTime.ToString() + ": " + LogLine;
            }

            m_LogFile.Add(LogLine);
            m_Count++;
        }

        public string Format(LogType logtype, object data)
        {
            return Format(logtype, data, null);
        }

        public string Format(LogType logtype, object data, string additional)
        {
            string LogLine = "";
            additional = (additional == null) ? "" : additional;

            if (logtype == LogType.Mixed)
            {
                // Work out most appropriate in absence of specific

                if (data is Mobile)
                    logtype = LogType.Mobile;
                else if (data is Item)
                    logtype = LogType.Item;
                else
                    logtype = LogType.Text;

            }

            switch (logtype)
            {

                case LogType.Mobile:
                    break;

                case LogType.ItemSerial:
                case LogType.Item:
                    break;

                case LogType.Text:

                    LogLine = data.ToString();
                    break;
            }

            return LogLine;
        }
    }

    public enum LogType
    {
        Mobile,
        Item,
        Mixed,
        Text,
        ItemSerial
    }
}