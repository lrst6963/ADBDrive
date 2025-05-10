using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Linq;
using System.Globalization;
using System.Security;

namespace ADBDrive
{
    public partial class Form1 : Form
    {
        private const string DriverUrl = "https://dl.google.com/android/repository/usb_driver_r13-windows.zip";
        private const string ToolsUrl = "https://dl.google.com/android/repository/platform-tools_r35.0.0-windows.zip";
        private const string DriverMd5 = "1264c572565b136c4a5b5ef75a7a1f47";
        private const string ToolsMd5 = "e8a786dc533d91b1e6c3dc38722bbaba";
        private static readonly string TempDir = Path.GetTempPath();

        public Form1()
        {
            InitializeComponent();
        }

        // 记录检测过程
        private void Log(string message)
        {
            textBox1.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }



        // 增强版驱动检测方法
        public bool IsDriverInstalled()
        {
            return CheckViaPnpUtil() || CheckViaRegistry();
        }

        // ADB工具是否安装
        public class ADBChecker
        {
            private const string TARGET_FOLDER = "C:/Windows";
            private static readonly string[] FILE_NAMES = new string[]
            {
            "adb.exe",
            "AdbWinApi.dll",
            "AdbWinUsbApi.dll",
            "fastboot.exe"
            };

            public static bool CheckByFiles()
            {
                foreach (string fileName in FILE_NAMES)
                {
                    string filePath = Path.Combine(TARGET_FOLDER, fileName);

                    if (!File.Exists(filePath))
                    {
                        return false;
                    }
                }

                return true;
            }
            public bool CheckByCommand(out string versionInfo)
            {
                versionInfo = string.Empty;

                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = "/c adb version";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();
                        versionInfo = process.StandardOutput.ReadToEnd();
                        process.WaitForExit(2000); // 设置2秒超时 

                        return versionInfo.Contains("Android Debug Bridge");
                    }
                }
                catch (Exception ex)
                {
                    versionInfo = $"检测异常: {ex.Message}";
                    return false;
                }
            }

            /// <summary>
            /// 综合检测方法 
            /// </summary>
            public bool FullCheck(out string message)
            {
                bool fileCheck = CheckByFiles();
                bool commandCheck = CheckByCommand(out string version);

                message = $"文件检测结果: {fileCheck}\n" +
                         $"命令检测结果: {commandCheck}\n" +
                         $"版本信息: {version}";

                return fileCheck && commandCheck;
            }

            internal static object FullCheck()
            {
                throw new NotImplementedException();
            }
        }
        public bool IsToolsInstalled()
        {
            var checker = new ADBChecker();
            if (checker.FullCheck(out string report))
            {
                MessageBox.Show("工具已安装，无需重复操作\n" + report);
                Log(report);
                return true;
            }
            else
            {
                Log("ADB未正确安装\n" + report);
                return false;
            }

        }
        // 方法1：改进的pnputil检测（兼容多语言系统）
        private bool CheckViaPnpUtil()
        {
            try
            {
                Log("开始执行pnputil检测...");
                string output = ExecuteCommandWithOutput("pnputil.exe /enum-drivers");
                Log($"pnputil输出:\n{output}");
                // 匹配多语言关键词（英文/中文）
                var patterns = new[] {
            @"Published\s+Name\s*:\s*oem\d+\.inf.*?Original\s+Name\s*:\s*android_winusb\.inf",
            @"发布名称\s*:\s*oem\d+\.inf.*?原始名称\s*:\s*android_winusb\.inf"
            };

                return patterns.Any(p => Regex.IsMatch(output, p, RegexOptions.IgnoreCase | RegexOptions.Singleline));
            }
            catch (Exception ex)
            {
                Log($"pnputil检测异常: {ex.Message}");
                return false;
            }
        }
        // 方法2：注册表检测
        private bool CheckViaRegistry()
        {
            const string targetGuid = "{3f966bd9-fa04-4ec5-991c-d326973b5128}";
            const string regPath = @"SYSTEM\CurrentControlSet\Control\Class\" + targetGuid;
            const string targetValueName = "Class";
            const string expectedValue = "AndroidUsbDeviceClass";

            try
            {
                using (var baseKey = Registry.LocalMachine.OpenSubKey(regPath))
                {
                    if (baseKey == null)
                    {
                        Log($"注册表路径不存在: HKLM\\{regPath}");
                        return false;
                    }

                    // 检查Class键值
                    string classValue = baseKey.GetValue(targetValueName, "").ToString();
                    bool classMatch = classValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
                    Log($"注册表检测 - Class值: {classValue} | 匹配结果: {classMatch}");

                    Log($"注册表检测最终结果: {classMatch}");
                    return classMatch;
                }
            }
            catch (SecurityException)
            {
                Log("注册表访问被拒绝，需要管理员权限");
                return false;
            }
            catch (Exception ex)
            {
                Log($"注册表检测异常: {ex.Message}");
                return false;
            }
        }



        // 通用下载方法
        private async Task<bool> DownloadAndExtractAsync(string url, string fileName, string targetDir, string expectedMd5)
        {
            string zipPath = Path.Combine(TempDir, fileName);

            try
            {
                // 清理旧文件
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                if (File.Exists(zipPath)) File.Delete(zipPath);

                // 异步下载
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(new Uri(url), zipPath);
                }

                // 验证MD5
                if (CalculateMd5(zipPath) != expectedMd5)
                {
                    MessageBox.Show("文件校验失败");
                    return false;
                }

                // 解压文件
                ZipFile.ExtractToDirectory(zipPath, TempDir);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}");
                return false;
            }
        }

        // 安装驱动程序
        private async void BtnInstallDriver_Click(object sender, EventArgs e)
        {
            if (IsDriverInstalled())
            {
                DialogResult result = MessageBox.Show("驱动已安装，是否重新安装？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    return;
                }
            }

            if (!IsAdministrator())
            {
                MessageBox.Show("请以管理员权限运行");
                return;
            }
            button2.Enabled = false;
            string targetDir = Path.Combine(TempDir, "usb_driver");
            bool success = await DownloadAndExtractAsync(DriverUrl, "driver.zip", targetDir, DriverMd5);

            if (success)
            {
                string infPath = Path.Combine(targetDir, "android_winusb.inf");
                int result = ExecuteCommand($"pnputil.exe /add-driver \"{infPath}\"");
                MessageBox.Show(result == 0 ? "驱动安装成功" : "驱动安装失败");
            }
            button2.Enabled = true;
        }

        // 安装平台工具
        private async void BtnInstallTools_Click(object sender, EventArgs e)
        {
            if (IsToolsInstalled())
            {
                Log("工具已安装，无需重复操作");
                return;
            }
            if (!IsAdministrator())
            {
                MessageBox.Show("请以管理员权限运行");
                return;
            }
            string targetDir = Path.Combine(TempDir, "platform-tools");
            bool success = await DownloadAndExtractAsync(ToolsUrl, "tools.zip", targetDir, ToolsMd5);

            if (success)
            {
                try
                {
                    CopyAllFiles(targetDir, Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                    MessageBox.Show("工具安装成功");
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("需要管理员权限");
                }
            }
        }

        // 文件复制方法
        private static void CopyAllFiles(string source, string destination)
        {
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }
        }
        // 命令行执行方法（带输出捕获）
        private string ExecuteCommandWithOutput(string command)
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage),
                    WorkingDirectory = Environment.SystemDirectory // 关键路径
                };

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) output.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) error.AppendLine(e.Data);
                };

                try
                {
                    Log($"启动命令: {command}");
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!process.WaitForExit(30000))
                    {
                        Log("命令执行超时，强制终止");
                        process.Kill();
                    }

                    Log($"退出代码: {process.ExitCode}\n输出内容:\n{output}\n错误信息:\n{error}");
                    return output.ToString();
                }
                catch (Exception ex)
                {
                    Log($"命令执行异常: {ex}");
                    return string.Empty;
                }
            }
        }
        // 命令执行方法
        private static int ExecuteCommand(string command)
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        // MD5计算
        private static string CalculateMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }
        }

        // 管理员检查
        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        // 窗体加载时显示权限状态
        private void Form1_Load(object sender, EventArgs e)
        {
            Text += IsAdministrator() ? "（管理员运行）" : "（非管理员运行）";
            bool driverStatus = IsDriverInstalled();
            Text += $"驱动状态: {(driverStatus ? "已安装" : "未安装")}";
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ToolStripMenuItem.Text == "启用日志")
            {
                this.Height = 300;
                textBox1.Visible = true;
                ToolStripMenuItem.Text = "禁用日志";
                return;
            }
            this.Height = 230;
            textBox1.Visible = false;
            ToolStripMenuItem.Text = "启用日志";
        }
    }
}