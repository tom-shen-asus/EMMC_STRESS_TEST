// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using DeviceInfo.Utils;
using System.Diagnostics;
using Windows.UI;
using System;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EMMC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool isEMMCExist = false;
        private bool isSDboot = false;
        private bool isEMMCboot = false;
        private bool isDdriveMount = true;
        private bool isTest_C_Drvive = false;
        private bool isTest_D_Drvive = false;
        private bool start = false;

        public MainPage()
        {
            InitializeComponent();
            routineWork();
            deleteTestAsync();
        }

        private void ClickStart_Click(object sender, RoutedEventArgs e)
        {
            if (isTest_D_Drvive && !isDdriveMount) //sd boot, test D: in eMMC
            {
                EMMC_Burn.Text = "SD card bootup, but EMMC is not mount D drive. Please flash FFU into EMMC first.";
                return;
            }
            EMMC_Burn.Text = "starting now, Test C: " + isTest_C_Drvive.ToString() + " , Test D: " + isTest_D_Drvive.ToString();
            start = true;

            createTestAsync();
            stressTestRoutine();
        }

        private void stressTestRoutine()
        {
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            bool srcToDest = true;
            Task task = Task.Factory.StartNew(async () =>
            {
                while (start)
                {
                    Thread.Sleep(3 * 1000);
                    Task.Factory.StartNew(async () =>
                    {
                        if (isTest_C_Drvive)
                        {
                            string output = await DeviceUtil.checkFileExist(srcToDest);
                            if (output.Contains("is not"))
                            {
                                EMMC_Burn.Text = "Fail !! please stop then start again ! reason: " + output;
                                start = false;
                                return;
                            }
                            else
                            {
                                EMMC_Burn.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(srcToDest ? Colors.Yellow : Colors.HotPink);
                                if (srcToDest)
                                    EMMC_Burn.Text = ">>>>>>>>> Move test file from src to dest <<<<<<<<<";
                                else
                                    EMMC_Burn.Text = "<<<<< Move test file from dest to src >>>>>";

                                await DeviceUtil.moveTestFile(srcToDest);
                                srcToDest = !srcToDest;
                            }
                        }
                        else if (isTest_D_Drvive)
                        {
                            string output = await DeviceUtil.checkFileDExist(srcToDest);
                            if (output.Contains("is not"))
                            {
                                EMMC_Burn.Text = "Fail !! please stop then start again ! reason: " + output;
                                start = false;
                                return;
                            }
                            else
                            {
                                if (srcToDest)
                                    EMMC_Burn.Text = ">>>>> Move test file from src to dest <<<<<";
                                else
                                    EMMC_Burn.Text = "<<<<< Move test file from dest to src >>>>>";

                                await DeviceUtil.moveTestDFile(srcToDest);
                                srcToDest = !srcToDest;
                            }
                        } else
                        {
                            EMMC_Burn.Text = "===== fail to start =====";
                        }

                    }, CancellationToken.None, TaskCreationOptions.None, uiContext);


                }
            });
        }

        private void ClickStop_Click(object sender, RoutedEventArgs e)
        {
            start = false;
            deleteTestAsync();
            EMMC_Burn.Text = "please press Start to test";
            Process.GetCurrentProcess().Kill();
        }

        private void ClickBack_Click(object sender, RoutedEventArgs e)
        {
            BackIoTDefaultAppAsync();;
        }

        private void BackIoTDefaultAppAsync()
        {
            DeviceUtil.backIoTCoreDefaultApp();
        }

        private async Task createTestAsync()
        {
            if (isTest_C_Drvive)
            {
                DeviceUtil.createTestCfolder();
            } else if (isTest_D_Drvive)
            {
                DeviceUtil.createTestDfolder();
            }
        }

        private async Task deleteTestAsync()
        {
            if (isTest_C_Drvive)
            {
                DeviceUtil.deleteTestCfolder();
            }
            else if (isTest_D_Drvive)
            {
                DeviceUtil.deleteTestDfolder();
            }
        }

        private void routineWork()
        {
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            Task task = Task.Factory.StartNew(async () =>
            {
                int i = 0;
                int h = 0;
                int m = 0;
                int s = 0;
                while (true) {
                    Task.Factory.StartNew(async () =>
                    {
                        IP_Address.Text = "IP: " + getIpV4Address();

                        string physicalDiskInfo = await DeviceUtil.getPhysicalDiskInfo();
                        if (physicalDiskInfo.ToUpper().Contains("PHYSICALDRIVE0") &&
                        (physicalDiskInfo.Contains("SanDisk DG4016") ||
                         physicalDiskInfo.Contains("Hynix hA8aP")))
                        {
                            h = (i / 3600);
                            m = (i - (3600 * h)) / 60;
                            s = (i - (3600 * h) - (m * 60));
                            EMMC_Exist.Text = "EMMC is Exist, " + h + " h " + m + " m " + s + " s ";
                            isEMMCExist = true;
                            i++;
                        }
                        else
                        {
                            EMMC_Exist.Text = "EMMC is not present, Please HW check EMMC";
                            isEMMCExist = false;
                        }

                        //string test = await DeviceUtil.test();
                        //EMMC_Burn.Text = test;

                        if (isEMMCExist)
                        {
                            string bc = await DeviceUtil.getBootConfiguration();
                            if (bc.Contains("Harddisk0"))
                            {
                                isEMMCboot = true;
                                isSDboot = false;
                                isTest_C_Drvive = true;
                                isTest_D_Drvive = false;
                            }
                            else if (bc.Contains("Harddisk1"))
                            {
                                isEMMCboot = false;
                                isSDboot = true;
                                isTest_C_Drvive = false;
                                isTest_D_Drvive = true;
                            }

                            string exist = await DeviceUtil.checkD_DriveMount();
                            if (exist.Contains("The system cannot find the drive specified."))
                            {
                                isDdriveMount = false;
                            }

                        }

                    }, CancellationToken.None, TaskCreationOptions.None, uiContext);
                    Thread.Sleep(1000);
                }
            });
        }

        private string getIpV4Address()
        {
            string strHostName = "";
            //把HostName換成網址可查該網址對應的IP位址
            string name = Dns.GetHostName();
            IPAddress[] ip = Dns.GetHostEntry(name).AddressList;
            for (int i = 0; i < ip.Length; i++)
            {
                //System.Net.Sockets.AddressFamily.InterNetwork為IPv4位址
                //System.Net.Sockets.AddressFamily.InterNetworkV6為IPv6位址
                if (ip[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    strHostName += " " + ip[i].ToString();
                }
            }
            return strHostName;
        }

    }
}
