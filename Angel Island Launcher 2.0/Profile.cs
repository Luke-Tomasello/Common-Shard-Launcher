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

namespace Angel_Island_Launcher_2._0
{
    public class Profile
    {
        private string m_Name;
        private string m_ServerAddress;
        private int m_ServerPort;
        private string m_Username;
        private string m_Password;
        private bool m_UseCUO;
        private bool m_UseRazor;
        private string m_UODirectory;
        private string m_CUODirectory;
        private string m_RazorDirectory;
        private string m_ServerName;

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string ServerAddress
        {
            get { return m_ServerAddress; }
            set { m_ServerAddress = value; }
        }

        public int ServerPort
        {
            get { return m_ServerPort; }
            set { m_ServerPort = value; }
        }

        public string Username
        {
            get { return m_Username; }
            set { m_Username = value; }
        }

        public string Password
        {
            get { return m_Password; }
            set { m_Password = value; }
        }

        public bool UseCUO
        {
            get { return m_UseCUO; }
            set { m_UseCUO = value; }
        }

        public bool UseRazor
        {
            get { return m_UseRazor; }
            set { m_UseRazor = value; }
        }

        public string UODirectory
        {
            get { return m_UODirectory; }
            set { m_UODirectory = value; }
        }

        public string CUODirectory
        {
            get { return m_CUODirectory; }
            set { m_CUODirectory = value; }
        }

        public string RazorDirectory
        {
            get { return m_RazorDirectory; }
            set { m_RazorDirectory = value; }
        }

        public string ServerName
        {
            get { return m_ServerName; }
            set { m_ServerName = value; }
        }

        public Profile()
        {
        }
    }
}