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
using System.Drawing;
using System.Drawing.Text;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Diagnostics;

namespace Angel_Island_Launcher_2._0
{
    public partial class AILauncher : Form
    {
        public const string SplashPageUrl = "https://game-master.net/resources/html/index.html";

        private PrivateFontCollection m_PrivateFontCollection;
        private SoundPlayer m_SoundPlayerAmbient;
        private SoundPlayer m_SoundPlayerLaunch;

        public AILauncher()
        {
            LogHelper logger = new LogHelper("AILauncher Log.log", true, false);
            logger.Log("AILauncher started");

            logger.Log("InitializeComponent...");
            InitializeComponent();
            logger.Log("Done.");

            panelProfilePage.Visible = false;

            textBoxPassword.UseSystemPasswordChar = !checkBoxShowPassword.Checked;
            progressBar.Value = 0;
            labelProgress.Text = String.Empty;

            logger.Log(String.Format("Launcher Version: {0}", Program.AppVersion));
            labelVersion.Text = String.Format("Launcher Version: {0}", Program.AppVersion);

            InitializeWebBrowser();
            AdjustFonts();
            AdjustCursors();

            m_SoundPlayerAmbient = new SoundPlayer(Properties.Resources.beach_bg);
            m_SoundPlayerAmbient.Load();

            m_SoundPlayerLaunch = new SoundPlayer(Properties.Resources.stones_harp);
            m_SoundPlayerLaunch.Load();

            foreach (string UODirectory in Program.UODirectories)
                comboBoxUODirectory.Items.Add(UODirectory);

            logger.Log(string.Format("UODirectories.Count == {0}.", Program.UODirectories.Count));
            if (Program.UODirectories.Count == 0)
            {
                comboBoxUODirectory.Visible = false;
                textBoxUODirectory.Visible = true;
            }

            logger.Log(string.Format("ClientRestrictions == {0}.", Program.ClientRestrictions));
            if (Program.ClientRestrictions)
            {
                textBoxUODirectory.ReadOnly = true;
                buttonBrowseUODirectory.Hide();

                textBoxCUODirectory.ReadOnly = true;
                buttonBrowseCUODirectory.Hide();

                textBoxRazorDirectory.ReadOnly = true;
                buttonBrowseRazorDirectory.Hide();

                checkBoxUseCUO.AutoCheck = false;
                checkBoxUseCUO.Hide();

                checkBoxUseRazor.AutoCheck = false;
                checkBoxUseRazor.Hide();
            }

            logger.Finish();
        }

        protected override void OnLoad(EventArgs e)
        {
            LogHelper logger = new LogHelper("OnLoad Log.log", true, false);
            base.OnLoad(e);

            this.Icon = Properties.Resources.SiegePerilous;

            if (Program.AppState.EnableUpdates)
            {
                logger.Log("Updates enabled, checking...");
                Program.CheckUpdateLauncher();
                logger.Log("CheckUpdateLauncher complete.");
            }
            else
            {
                logger.Log("Updates disabled");
            }

            logger.Finish();
        }

        private void InitializeWebBrowser()
        {
            WebBrowser wb = new WebBrowser();

            wb.Url = new Uri(SplashPageUrl);
            wb.Parent = panelBrowser;
            wb.Dock = DockStyle.Fill;
        }

        private void AdjustFonts()
        {
            m_PrivateFontCollection = new PrivateFontCollection();

            byte[] fontData = Properties.Resources.Mendoza_Roman_SC_ITC_TT_Book;
            IntPtr fontDataPtr = Marshal.AllocCoTaskMem(fontData.Length);
            Marshal.Copy(fontData, 0, fontDataPtr, fontData.Length);

            m_PrivateFontCollection.AddMemoryFont(fontDataPtr, fontData.Length);

            FontFamily fontFamily = m_PrivateFontCollection.Families[0];

            foreach (Control c in GetAllChildren(this))
            {
                if (c is Button)
                    c.Font = new Font(fontFamily, c.Font.Size);
            }
        }

        private void AdjustCursors()
        {
            Cursor cursorGlove = new Cursor(Properties.Resources.cursor_glove.GetHicon());
            Cursor cursorHover = new Cursor(Properties.Resources.cursor_hover.GetHicon());
            Cursor cursorFeather = new Cursor(Properties.Resources.cursor_feather.GetHicon());

            this.Cursor = cursorGlove;

            foreach (Control c in GetAllChildren(this))
            {
                if (c is Button || c is CheckBox || c is ListBox || c is ComboBox)
                    c.Cursor = cursorHover;
                else if (c is TextBox)
                    c.Cursor = cursorFeather;
                else if (c is Panel)
                    c.Cursor = cursorGlove;
            }
        }

        private IEnumerable<Control> GetAllChildren(Control root)
        {
            Stack<Control> stack = new Stack<Control>();

            stack.Push(root);

            while (stack.Count != 0)
            {
                Control next = stack.Pop();

                foreach (Control child in next.Controls)
                    stack.Push(child);

                yield return next;
            }
        }

        public void ReloadProfileList()
        {
            List<Profile> profiles = Program.AppState.Profiles;

            comboBoxProfiles.Items.Clear();
            listBoxProfiles.Items.Clear();

            for (int i = 0; i < profiles.Count; i++)
            {
                comboBoxProfiles.Items.Add(profiles[i].Name);
                listBoxProfiles.Items.Add(profiles[i].Name);
            }
        }

        public void ReloadProfile()
        {
            Profile profile = Program.CurrentProfile;

            if (profile == null)
            {
                comboBoxProfiles.SelectedIndex = -1;
                comboBoxProfiles.Text = String.Empty;

                buttonCopyProfile.Enabled = false;
                buttonRemoveProfile.Enabled = false;

                SetSelection(listBoxProfiles, -1);

                textBoxProfileName.Text = String.Empty;
                textBoxProfileName.Enabled = false;
                textBoxServerAddress.Text = String.Empty;
                textBoxServerAddress.Enabled = false;
                textBoxServerPort.Text = String.Empty;
                textBoxServerPort.Enabled = false;
                textBoxUsername.Text = String.Empty;
                textBoxUsername.Enabled = false;
                textBoxPassword.Text = String.Empty;
                textBoxPassword.Enabled = false;

                checkBoxUseCUO.Checked = false;
                checkBoxUseCUO.Enabled = false;
                checkBoxUseRazor.Checked = false;
                checkBoxUseRazor.Enabled = false;

                comboBoxUODirectory.Text = String.Empty;
                comboBoxUODirectory.Enabled = false;
                textBoxUODirectory.Text = String.Empty;
                textBoxUODirectory.Enabled = false;
                textBoxCUODirectory.Text = String.Empty;
                textBoxCUODirectory.Enabled = false;
                textBoxRazorDirectory.Text = String.Empty;
                textBoxRazorDirectory.Enabled = false;

                textBoxServerName.Text = String.Empty;
                textBoxServerName.Enabled = false;

                buttonBrowseUODirectory.Enabled = false;
                buttonBrowseCUODirectory.Enabled = false;
                buttonBrowseRazorDirectory.Enabled = false;

                buttonInstall.Enabled = false;
            }
            else
            {
                int index = Program.AppState.Profiles.IndexOf(profile);

                comboBoxProfiles.SelectedIndex = index;
                comboBoxProfiles.Text = profile.Name;

                buttonCopyProfile.Enabled = true;
                buttonRemoveProfile.Enabled = true;

                SetSelection(listBoxProfiles, index);

                textBoxProfileName.Text = profile.Name;
                textBoxProfileName.Enabled = true;
                textBoxServerAddress.Text = profile.ServerAddress;
                textBoxServerAddress.Enabled = true;
                textBoxServerPort.Text = profile.ServerPort.ToString();
                textBoxServerPort.Enabled = true;
                textBoxUsername.Text = profile.Username;
                textBoxUsername.Enabled = true;
                textBoxPassword.Text = profile.Password;
                textBoxPassword.Enabled = true;

                checkBoxUseCUO.Checked = profile.UseCUO;
                checkBoxUseCUO.Enabled = true;
                checkBoxUseRazor.Checked = profile.UseRazor;
                checkBoxUseRazor.Enabled = true;

                comboBoxUODirectory.Text = Program.GetUODirectory(profile);
                comboBoxUODirectory.Enabled = true;
                textBoxUODirectory.Text = Program.GetUODirectory(profile);
                textBoxUODirectory.Enabled = true;
                textBoxCUODirectory.Text = Program.GetCUODirectory(profile);
                textBoxCUODirectory.Enabled = true;
                textBoxRazorDirectory.Text = Program.GetRazorDirectory(profile);
                textBoxRazorDirectory.Enabled = true;

                textBoxServerName.Text = profile.ServerName;
                textBoxServerName.Enabled = true;

                buttonBrowseUODirectory.Enabled = true;
                buttonBrowseCUODirectory.Enabled = true;
                buttonBrowseRazorDirectory.Enabled = true;

                buttonInstall.Enabled = true;
            }
        }

        private static void SetSelection(ListBox lb, int index)
        {
            if (index != -1)
            {
                if (lb.SelectedIndices.Count != 1 || lb.SelectedIndices[0] != index)
                {
                    lb.SelectedIndices.Clear();
                    lb.SelectedIndices.Add(index);
                }
            }
            else
            {
                if (lb.SelectedIndices.Count != 0)
                    lb.SelectedIndices.Clear();
            }
        }

        public void ReloadInstallDirectories()
        {
            Profile profile = Program.CurrentProfile;

            if (profile == null)
            {
                comboBoxUODirectory.Text = String.Empty;
                comboBoxUODirectory.Enabled = false;
                textBoxUODirectory.Text = String.Empty;
                textBoxUODirectory.Enabled = false;
                textBoxCUODirectory.Text = String.Empty;
                textBoxCUODirectory.Enabled = false;
                textBoxRazorDirectory.Text = String.Empty;
                textBoxRazorDirectory.Enabled = false;
            }
            else
            {
                comboBoxUODirectory.Text = Program.GetUODirectory(profile);
                comboBoxUODirectory.Enabled = true;
                textBoxUODirectory.Text = Program.GetUODirectory(profile);
                textBoxUODirectory.Enabled = true;
                textBoxCUODirectory.Text = Program.GetCUODirectory(profile);
                textBoxCUODirectory.Enabled = true;
                textBoxRazorDirectory.Text = Program.GetRazorDirectory(profile);
                textBoxRazorDirectory.Enabled = true;
            }
        }

        private void AILauncher_Load(object sender, EventArgs e)
        {
            checkBoxUpdates.Checked = Program.AppState.EnableUpdates;
            checkBoxAudio.Checked = Program.AppState.EnableAudio;

            ReloadProfileList();
            ReloadProfile();

            if (Program.AppState.EnableAudio)
                m_SoundPlayerAmbient.PlayLooping();
        }

        private Point m_Offset;
        private bool m_MouseDown;

        private void AILauncher_MouseDown(object sender, MouseEventArgs e)
        {
            m_Offset.X = e.X;
            m_Offset.Y = e.Y;
            m_MouseDown = true;
        }

        private void AILauncher_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_MouseDown)
            {
                Point mouseLoc = PointToScreen(e.Location);
                Location = new Point(mouseLoc.X - m_Offset.X, mouseLoc.Y - m_Offset.Y);
            }
        }

        private void AILauncher_MouseUp(object sender, MouseEventArgs e)
        {
            m_MouseDown = false;
        }

        private void buttonWebsite_Click(object sender, EventArgs e)
        {
            Utility.OpenUrl("https://game-master.net");
        }

        private void buttonMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonLaunch_Click(object sender, EventArgs e)
        {
            if (Program.TaskInProgress)
                MessageBox.Show("Cannot launch the game while a task is in progress.");
            else
                Program.Launch();
        }

        private void buttonLaunch_MouseEnter(object sender, EventArgs e)
        {
            if (Program.AppState.EnableAudio)
                m_SoundPlayerLaunch.PlayLooping();
        }

        private void buttonLaunch_MouseLeave(object sender, EventArgs e)
        {
            if (Program.AppState.EnableAudio)
                m_SoundPlayerAmbient.PlayLooping();
        }

        private void buttonLaunch_MouseDown(object sender, MouseEventArgs e)
        {
            buttonLaunch.BackgroundImage = Properties.Resources.button_bg_extra_pressed;
            buttonLaunch.ForeColor = Color.FromArgb(0, 170, 170, 170);
        }

        private void buttonLaunch_MouseUp(object sender, MouseEventArgs e)
        {
            buttonLaunch.BackgroundImage = Properties.Resources.button_bg_extra;
            buttonLaunch.ForeColor = Color.FromArgb(0, 255, 255, 255);
        }

        private void comboBoxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_RearrangingProfiles)
                Program.LoadProfile(comboBoxProfiles.SelectedIndex);
        }

        private void buttonProfile_Click(object sender, EventArgs e)
        {
            panelProfilePage.Visible = !panelProfilePage.Visible;
            panelBrowser.Visible = !panelBrowser.Visible;
        }

        private void buttonLesser_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Control)
            {
                Control c = (Control)sender;

                c.BackgroundImage = Properties.Resources.button_bg_lesser_pressed;
                c.ForeColor = Color.FromArgb(0, 170, 170, 170);
            }
        }

        private void buttonLesser_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is Control)
            {
                Control c = (Control)sender;

                c.BackgroundImage = Properties.Resources.button_bg_lesser;
                c.ForeColor = Color.FromArgb(0, 255, 255, 255);
            }
        }

        private void buttonCommunity_Click(object sender, EventArgs e)
        {
            Utility.OpenUrl("https://discord.com/invite/KYBYGDv");
        }

        private void buttonNews_Click(object sender, EventArgs e)
        {
            Utility.OpenUrl("https://game-master.net/forums/ubbthreads.php/forums/5/1/news");
        }

        private void buttonPatchNotes_Click(object sender, EventArgs e)
        {
            Utility.OpenUrl("https://game-master.net/forums/ubbthreads.php/forums/3/1/patch-notes");
        }

        private void buttonWiki_Click(object sender, EventArgs e)
        {
            Utility.OpenUrl("https://game-master.net/aiwiki/index.php?title=Main_Page");
        }

        private void checkBoxEnableUpdates_CheckedChanged(object sender, EventArgs e)
        {
            Program.AppState.EnableUpdates = checkBoxUpdates.Checked;
        }

        private void checkBoxEnableAudio_CheckedChanged(object sender, EventArgs e)
        {
            Program.AppState.EnableAudio = checkBoxAudio.Checked;

            if (checkBoxAudio.Checked)
            {
                m_SoundPlayerAmbient.PlayLooping();
            }
            else
            {
                m_SoundPlayerAmbient.Stop();
                m_SoundPlayerLaunch.Stop();
            }
        }

        private void buttonNewProfile_Click(object sender, EventArgs e)
        {
            Program.AddProfile(Program.NewProfile());
        }

        private void buttonCopyProfile_Click(object sender, EventArgs e)
        {
            Program.AddProfile(Program.NewProfile(Program.CurrentProfile));
        }

        private void buttonRemoveProfile_Click(object sender, EventArgs e)
        {
            Program.RemoveProfile();
        }

        private void listBoxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_RearrangingProfiles && listBoxProfiles.SelectedIndices.Count != 0)
                Program.LoadProfile(listBoxProfiles.SelectedIndices[0]);
        }

        private void textBoxProfileName_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
            {
                Program.CurrentProfile.Name = textBoxProfileName.Text;

                int index = Program.AppState.Profiles.IndexOf(Program.CurrentProfile);

                if (index >= 0 && index < comboBoxProfiles.Items.Count)
                    comboBoxProfiles.Items[index] = textBoxProfileName.Text;

                if (index >= 0 && index < listBoxProfiles.Items.Count)
                    listBoxProfiles.Items[index] = textBoxProfileName.Text;
            }
        }

        private void textBoxServerAddress_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.ServerAddress = textBoxServerAddress.Text;
        }

        private void textBoxServerPort_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.ServerPort = Utility.ToInt32(textBoxServerPort.Text);
        }

        private void textBoxUsername_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.Username = textBoxUsername.Text;
        }

        private void textBoxPassword_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.Password = textBoxPassword.Text;
        }

        private void checkBoxShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            textBoxPassword.UseSystemPasswordChar = !checkBoxShowPassword.Checked;
        }

        private void checkBoxUseCUO_CheckedChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.UseCUO = checkBoxUseCUO.Checked;
        }

        private void checkBoxUseRazor_CheckedChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.UseRazor = checkBoxUseRazor.Checked;
        }

        private void comboBoxUODirectory_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.UODirectory = comboBoxUODirectory.Text;
        }

        private void comboBoxUODirectory_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxUODirectory.Text = (string)comboBoxUODirectory.Items[comboBoxUODirectory.SelectedIndex];
        }

        private void textBoxUODirectory_TextChanged(object sender, EventArgs e)
        {
            comboBoxUODirectory.Text = textBoxUODirectory.Text;
        }

        private void buttonBrowseUODirectory_Click(object sender, EventArgs e)
        {
            if (browserUODirectory.ShowDialog() == DialogResult.OK)
                comboBoxUODirectory.Text = browserUODirectory.SelectedPath;
        }

        private void textBoxCUODirectory_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.CUODirectory = textBoxCUODirectory.Text;
        }

        private void buttonBrowseCUODirectory_Click(object sender, EventArgs e)
        {
            if (browserCUODirectory.ShowDialog() == DialogResult.OK)
                textBoxCUODirectory.Text = browserCUODirectory.SelectedPath;
        }

        private void textBoxRazorDirectory_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.RazorDirectory = textBoxRazorDirectory.Text;
        }

        private void buttonBrowseRazorDirectory_Click(object sender, EventArgs e)
        {
            if (browserRazorDirectory.ShowDialog() == DialogResult.OK)
                textBoxRazorDirectory.Text = browserRazorDirectory.SelectedPath;
        }

        private void buttonInstall_Click(object sender, EventArgs e)
        {
            if (Program.TaskInProgress)
                MessageBox.Show("Cannot start installation while a task is in progress.");
            else
                Program.InstallPackages();
        }

        private delegate void SetProgressDelegate(int perc);

        public void SetProgress(int perc)
        {
            if (InvokeRequired)
                Invoke(new SetProgressDelegate(SetProgress), perc);
            else
                progressBar.Value = Math.Max(0, Math.Min(100, perc));
        }

        private delegate void PostMessageDelegate(string format, params object[] args);

        public void PostMessage(string format, params object[] args)
        {
            if (InvokeRequired)
                Invoke(new PostMessageDelegate(PostMessage), format, args);
            else
                labelProgress.Text = String.Format(format, args);
        }

        public void PostAction(Action action)
        {
            if (InvokeRequired)
                Invoke(action);
            else
                action();
        }

        private void listBoxProfiles_MouseDown(object sender, MouseEventArgs e)
        {
            int index = listBoxProfiles.IndexFromPoint(new Point(e.X, e.Y));

            if (index >= 0)
            {
                Program.LoadProfile(index);

                listBoxProfiles.DoDragDrop(index, DragDropEffects.Move);
            }
        }

        private void listBoxProfiles_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private bool m_RearrangingProfiles;

        private void listBoxProfiles_DragDrop(object sender, DragEventArgs e)
        {
            int oldIndex = (int)e.Data.GetData(typeof(int));

            if (oldIndex < 0)
                return;

            int newIndex = listBoxProfiles.IndexFromPoint(listBoxProfiles.PointToClient(new Point(e.X, e.Y)));

            if (newIndex < 0)
                newIndex = listBoxProfiles.Items.Count - 1;

            if (oldIndex == newIndex)
                return;

            m_RearrangingProfiles = true;

            int sign = (oldIndex < newIndex ? +1 : -1);
            int numSwaps = sign * (newIndex - oldIndex);

            for (int i = 0; i < numSwaps; i++)
            {
                int idx1 = oldIndex + sign * i;
                int idx2 = oldIndex + sign * (i + 1);

                Profile tempProfile = Program.AppState.Profiles[idx1];
                Program.AppState.Profiles[idx1] = Program.AppState.Profiles[idx2];
                Program.AppState.Profiles[idx2] = tempProfile;

                object tempObj = listBoxProfiles.Items[idx1];
                listBoxProfiles.Items[idx1] = listBoxProfiles.Items[idx2];
                listBoxProfiles.Items[idx2] = tempObj;

                tempObj = comboBoxProfiles.Items[idx1];
                comboBoxProfiles.Items[idx1] = comboBoxProfiles.Items[idx2];
                comboBoxProfiles.Items[idx2] = tempObj;

                if (listBoxProfiles.SelectedIndex == idx1)
                    listBoxProfiles.SelectedIndex = idx2;
                else
                if (listBoxProfiles.SelectedIndex == idx2)
                    listBoxProfiles.SelectedIndex = idx1;

                if (comboBoxProfiles.SelectedIndex == idx1)
                    comboBoxProfiles.SelectedIndex = idx2;
                else
                if (comboBoxProfiles.SelectedIndex == idx2)
                    comboBoxProfiles.SelectedIndex = idx1;
            }

            m_RearrangingProfiles = false;
        }

        private void listBoxProfiles_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            // don't change the cursor while drag-dropping
            e.UseDefaultCursors = false;
        }

        public void OpenProfilePage()
        {
            panelProfilePage.Visible = true;
            panelBrowser.Visible = false;
        }

        const int WS_MINIMIZEBOX = 0x20000;
        const int CS_DBLCLKS = 0x8;

        // the following allows us to minimize/maximize the launcher by clicking in the task bar
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX;
                cp.ClassStyle |= CS_DBLCLKS;
                return cp;
            }
        }

        private void textBoxServerName_TextChanged(object sender, EventArgs e)
        {
            if (Program.CurrentProfile != null)
                Program.CurrentProfile.ServerName = textBoxServerName.Text;
        }
    }
}