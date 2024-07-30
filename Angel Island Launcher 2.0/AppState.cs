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
using System.Text.Json.Serialization;

namespace Angel_Island_Launcher_2._0
{
    public class AppState
    {
        private bool m_EnableUpdates;
        private bool m_EnableAudio;
        private int m_UOVersion;
        private int m_CUOVersion;
        private int m_RazorVersion;
        private int m_DiffVersion;
        private string m_LastProfile;
        private List<Profile> m_Profiles;

        public bool EnableUpdates
        {
            get { return m_EnableUpdates; }
            set { m_EnableUpdates = value; }
        }

        public bool EnableAudio
        {
            get { return m_EnableAudio; }
            set { m_EnableAudio = value; }
        }

        public int UOVersion
        {
            get { return m_UOVersion; }
            set { m_UOVersion = value; }
        }

        public int CUOVersion
        {
            get { return m_CUOVersion; }
            set { m_CUOVersion = value; }
        }

        public int RazorVersion
        {
            get { return m_RazorVersion; }
            set { m_RazorVersion = value; }
        }

        public int DiffVersion
        {
            get { return m_DiffVersion; }
            set { m_DiffVersion = value; }
        }

        [JsonConverter(typeof(Utility.JsonConverterIntOrStr))]
        public string LastProfile
        {
            get { return m_LastProfile; }
            set { m_LastProfile = value; }
        }

        public List<Profile> Profiles
        {
            get { return m_Profiles; }
            set { m_Profiles = value; }
        }

        public AppState()
        {
            m_Profiles = new List<Profile>();
        }
    }
}