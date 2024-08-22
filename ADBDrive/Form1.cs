using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace ADBDrive
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        readonly string TEMP_DIR = Environment.GetEnvironmentVariable("TEMP");
        string url = "https://dl.google.com/android/repository/usb_driver_r13-windows.zip";
        string tools_url = "https://dl.google.com/android/repository/platform-tools_r35.0.0-windows.zip";
        string md5_driver = "1264c572565b136c4a5b5ef75a7a1f47";
        string md5_tools = "e8a786dc533d91b1e6c3dc38722bbaba";

        private void Button1_Click(object sender, EventArgs e)
        {
            string zip_file = TEMP_DIR + "\\usb_driver_r13-windows.zip";
            if (Directory.Exists(TEMP_DIR + "\\usb_driver") && File.Exists(zip_file) && CalMD5(zip_file) == md5_driver)
            {
                MessageBox.Show("已下载，切勿重新下载！");
            }
            else
            {
                var t = Task.Run(() => D(url, zip_file));
                t.Wait();
                if (CalMD5(zip_file) == md5_driver)
                {
                    MessageBox.Show("下载完成！");
                }
                else
                {
                    MessageBox.Show("下载失败！");
                }
                if (Directory.Exists(TEMP_DIR + "\\usb_driver"))
                {
                    Directory.Delete(TEMP_DIR + "\\usb_driver", true);
                }
                ZipFile.ExtractToDirectory(zip_file, TEMP_DIR);
            }
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            string infp = TEMP_DIR + "\\usb_driver\\android_winusb.inf";
            string arguments = "pnputil.exe /add-driver " + infp;
            if (Execute(true, arguments) == 0)
            {
                MessageBox.Show("驱动程序安装成功！");
            }
            else
            {
                MessageBox.Show("驱动程序安装失败！");
            }
        }

        public static void D(string url, string save)
        {
            using (var web = new WebClient())
            {
                web.DownloadFile(url, save);
            }
        }

        public static void 复制目录下所有文件到另外一个路径(string sourceDirectory, string destinationDirectory)
        {
            try
            {
                // 获取源目录中的所有文件
                string[] files = Directory.GetFiles(sourceDirectory);

                // 遍历所有文件并移动它们到目标目录
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destinationFile = Path.Combine(destinationDirectory, fileName);
                    File.Copy(file, destinationFile);
                    Console.WriteLine("文件移动成功: " + fileName);
                }

                Console.WriteLine("所有文件移动完成");
            }
            catch (Exception e)
            {
                Console.WriteLine("文件移动失败: " + e.Message);
            }
        }

        public static int Execute(bool NoWindow, string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
            process.StartInfo.CreateNoWindow = NoWindow;//不显示程序窗口
            process.Start();
            process.WaitForExit(); //等待程序执行完退出进程
            var t = process.ExitCode;
            process.Close();
            return t;
        }

        public string CalMD5(string file)
        {
            var md5 = MD5.Create();
            var stream = File.OpenRead(file);
            byte[] data = md5.ComputeHash(stream);
            return BitConverter.ToString(data).Replace("-", "").ToLower();
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (IsAdministrator())
            {
                this.Text = Text + "（管理员运行）";
            }
            else
            {
                this.Text = Text + "（非管理员运行）";
            }
            ShowIcon = false;
            //MessageBox.Show(Environment.GetEnvironmentVariable("PATH"));
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string zip_file = TEMP_DIR + "\\platform-tools_r35.0.0-windows.zip";
            if (Directory.Exists(TEMP_DIR + "\\platform-tools") && File.Exists(zip_file) && CalMD5(zip_file) == md5_tools)
            {
                MessageBox.Show("已下载，切勿重新下载！");
            }
            else
            {
                var t = Task.Run(() => D(tools_url, zip_file));
                t.Wait();
                if (CalMD5(zip_file) == md5_tools)
                {
                    MessageBox.Show("下载完成！");
                }
                else
                {
                    MessageBox.Show("下载失败！");
                }
                if (Directory.Exists(TEMP_DIR + "\\platform-tools"))
                {
                    Directory.Delete(TEMP_DIR + "\\platform-tools", true);
                }
                ZipFile.ExtractToDirectory(zip_file, TEMP_DIR);
                
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string zip_file = TEMP_DIR + "\\platform-tools_r35.0.0-windows.zip";
            if (Directory.Exists(TEMP_DIR + "\\platform-tools") && File.Exists(zip_file) && CalMD5(zip_file) == md5_tools)
            {
                string infp = TEMP_DIR + "\\platform-tools";
                复制目录下所有文件到另外一个路径(infp, "C:\\Windows");
                //string arguments = "adb.exe";
                MessageBox.Show("工具安装成功！");
            }
            else
            {
                MessageBox.Show("未下载");
            }

/*            if (Execute(true, arguments) == 0)
            {
                MessageBox.Show("工具安装成功！");
            }
            else
            {
                MessageBox.Show("工具安装失败！");
            }*/
        }
    }
}
