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

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using KVP = System.Collections.Generic.KeyValuePair<System.String, System.String>;

namespace Angel_Island_Launcher_2._0
{
    public static class RazorConfiguration
    {
        public static void Configure(Profile profile)
        {
            string xmlPath = Path.Combine(Program.GetRazorDirectory(profile), "Razor.exe.Config");
            string csvPath = Path.Combine(Program.GetRazorDirectory(profile), "settings.csv");

            XmlDocument xmlDoc = new XmlDocument();

            if (File.Exists(xmlPath))
                xmlDoc.Load(xmlPath);

            int serverID = AddServer(xmlDoc, profile);

            PatchXml(xmlDoc, profile, serverID);

            xmlDoc.Save(xmlPath);

            string[] lines;

            if (File.Exists(csvPath))
                lines = File.ReadAllLines(csvPath);
            else
                lines = new string[0];

            File.WriteAllLines(csvPath, PatchCsv(lines, profile, serverID));
        }

        private static int AddServer(XmlDocument xmlDoc, Profile profile)
        {
            XmlNode configuration = xmlDoc["configuration"];

            XmlNode servers = configuration["Servers"];

            bool found = false;
            int serverID = 0;

            foreach (XmlElement xmlElem in servers)
            {
                if (xmlElem.GetAttribute("key") == profile.Name)
                {
                    xmlElem.SetAttribute("value", String.Concat(profile.ServerAddress, ',', profile.ServerPort.ToString()));
                    found = true;
                    break;
                }

                serverID++;
            }

            if (!found)
            {
                XmlElement xmlElem = xmlDoc.CreateElement("add");

                xmlElem.SetAttribute("key", profile.Name);
                xmlElem.SetAttribute("value", String.Concat(profile.ServerAddress, ',', profile.ServerPort.ToString()));

                servers.AppendChild(xmlElem);
            }

            return serverID;
        }

        private static void PatchXml(XmlDocument xmlDoc, Profile profile, int serverID)
        {
            XmlNode configuration = xmlDoc["configuration"];

            XmlNode appSettings = configuration["appSettings"];

            KeyValueList keyVals = new KeyValueList();

            foreach (XmlElement xmlElem in appSettings)
                keyVals.Add(new KVP(xmlElem.GetAttribute("key"), xmlElem.GetAttribute("value")));

            PatchKeyVals(keyVals, profile, serverID);

            XmlElement appSettingsPatched = xmlDoc.CreateElement("appSettings");

            foreach (KVP kvp in keyVals)
            {
                XmlElement xmlElem = xmlDoc.CreateElement("add");

                xmlElem.SetAttribute("key", kvp.Key);
                xmlElem.SetAttribute("value", kvp.Value);

                appSettingsPatched.AppendChild(xmlElem);
            }

            configuration.InsertAfter(appSettingsPatched, appSettings);
            configuration.RemoveChild(appSettings);
        }

        private static string[] PatchCsv(string[] lines, Profile profile, int serverID)
        {
            KeyValueList keyVals = new KeyValueList();

            foreach (string line in lines)
            {
                int index = line.IndexOf(',');

                string key = line.Substring(0, index).Trim();
                string value;

                if (index >= 0 && index < line.Length - 1)
                    value = line.Substring(index + 1).Trim();
                else
                    value = String.Empty;

                keyVals.Add(new KVP(key, value));
            }

            PatchKeyVals(keyVals, profile, serverID);

            string[] linesPatched = new string[keyVals.Count];

            for (int i = 0; i < keyVals.Count; i++)
            {
                KVP kvp = keyVals[i];

                linesPatched[i] = String.Format("{0}, {1}", kvp.Key, kvp.Value);
            }

            return linesPatched;
        }

        private static void PatchKeyVals(KeyValueList keyVals, Profile profile, int serverID)
        {
            keyVals["UODataDir"] = Path.GetFullPath(Program.GetUODirectory(profile));
            keyVals["UOClient"] = Path.GetFullPath(Path.Combine(Program.GetUODirectory(profile), "client.exe"));
            keyVals["LastPort"] = profile.ServerPort.ToString();
            keyVals["LastProfile"] = profile.Name;
            keyVals["LastServer"] = profile.ServerAddress;
            keyVals["LastServerId"] = serverID.ToString();
            keyVals["ShowWelcome"] = "1";
        }

        private class KeyValueList : List<KVP>
        {
            public string this[string key]
            {
                get
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        KVP kvp = this[i];

                        if (kvp.Key == key)
                            return kvp.Value;
                    }

                    return null;
                }
                set
                {
                    for (int i = 0; i < this.Count; i++)
                    {
                        KVP kvp = this[i];

                        if (kvp.Key == key)
                        {
                            if (value != null)
                                this[i] = new KVP(key, value);
                            else
                                this.RemoveAt(i);

                            return;
                        }
                    }

                    if (value != null)
                        this.Add(new KVP(key, value));
                }
            }

            public KeyValueList()
                : base()
            {
            }
        }
    }
}