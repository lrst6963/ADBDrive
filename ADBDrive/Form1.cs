using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        string tools_url = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";
        string md5 = "1264c572565b136c4a5b5ef75a7a1f47";

        private void Button1_Click(object sender, EventArgs e)
        {
            string zip_file = TEMP_DIR + "\\usb_driver_r13-windows.zip";
            if (Directory.Exists(TEMP_DIR + "\\usb_driver") && File.Exists(zip_file) && CalMD5(zip_file) == "1264c572565b136c4a5b5ef75a7a1f47")
            {
                MessageBox.Show("已下载，切勿重新下载！");
            }
            else
            {
                var t = Task.Run(() => D(url, zip_file));
                t.Wait();
                if (CalMD5(zip_file) == md5)
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
            //WindowsBuiltInRole可以枚举出很多权限，例如系统用户、User、Guest等等   
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (IsAdministrator())
            {
                this.Text = Text + "（管理员运行）";
            }
            ShowIcon = false;
            //MessageBox.Show(Environment.GetEnvironmentVariable("PATH"));
        }
    }
}
