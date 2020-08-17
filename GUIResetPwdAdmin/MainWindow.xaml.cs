using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Timers;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;

namespace GUIResetPwdAdmin
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            System.Timers.Timer aTimer = new System.Timers.Timer(2000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;


            System.Timers.Timer sTimer = new System.Timers.Timer(1300);
            sTimer.Elapsed += OnTimedEventStatus;
            sTimer.AutoReset = true;
            sTimer.Enabled = true;

            DataGridComputers.ItemsSource = UtilClass.TableComputersList;

        }

        private Thread runningScanThread = null;
        private Thread runningDoThread = null;


        private void OnTimedEventStatus(Object source, ElapsedEventArgs e)
        {

           try
            {
                DataGridComputers.Dispatcher.Invoke(() =>
                {
                    foreach (var fCell in DataGridComputers.Items)
                    {
                    
                       var cell= DataGridHelper.GetCell(new DataGridCellInfo(fCell, DataGridComputers.Columns[8]));

                        if (cell != null)
                            if (cell.ToString().ToLower().IndexOf("error")>=0) {
                                if (cell.Background == Brushes.OrangeRed)
                                {
                                    cell.Background = Brushes.White;
                                }
                                else
                                {
                                    cell.Background = Brushes.OrangeRed;
                                }
                            }

                    }
                  
                });

            }
            catch
            {

            }

        }


        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                DataGridComputers.Dispatcher.Invoke(() =>
                {
                   DataGridComputers.Items.Refresh();

                });

            }
            catch
            {

            }

            //завершение сканирования
            if(runningScanThread != null)
            if (runningScanThread.ThreadState == System.Threading.ThreadState.Stopped || runningScanThread.ThreadState == System.Threading.ThreadState.Aborted)
            {
                    btn_ScanStart.Dispatcher.Invoke(() =>
                    {
                        btn_ScanStart.IsEnabled = true;
                        btn_ScanStop.IsEnabled = false;
                        btn_DoStart.IsEnabled = true;
                    }
                    );
                    runningScanThread = null;
                    Console.WriteLine("Поток остановился.");
                }

            //завершение действий
            if (runningDoThread != null)
                if (runningDoThread.ThreadState == System.Threading.ThreadState.Stopped || runningDoThread.ThreadState == System.Threading.ThreadState.Aborted)
                {
                    btn_DoStart.Dispatcher.Invoke(() =>
                    {
                        btn_DoStart.IsEnabled = true;
                        btn_DoStop.IsEnabled = false;
                    }
                    );
                    runningDoThread = null;
                    Console.WriteLine("Поток остановился.");
                }

        }


        private void Btn_getListFromESK_Click(object sender, RoutedEventArgs e)
        { 
            AddPathESKForm frm = new AddPathESKForm();
            frm.ShowDialog();
            if (frm.Status)
            {
                DataGridComputers.Items.SortDescriptions.Clear();
                UtilClass.TableComputersList.Clear();
                UtilClass.LDAPFindComputers(frm.ESKPath);

                btn_ScanStart.IsEnabled = true;
               
            }

            




        }



        private void btn_ScanStart_Click(object sender, RoutedEventArgs e)
        {
            
            btn_ScanStart.IsEnabled = false;
            btn_ScanStop.IsEnabled = true;

            runningScanThread = new Thread(()=>
            {
                Console.WriteLine("Поток запущен.");

                for (int i = 0; i < UtilClass.TableComputersList.Count; i++)
                {
                    
                    UtilClass.TableComputersList[i].Ping = "не определен";
                    UtilClass.TableComputersList[i].Admin = "не определен";
                    UtilClass.TableComputersList[i].AdmEnable = "не определен";
                    UtilClass.TableComputersList[i].Guest = "не определен";
                    UtilClass.TableComputersList[i].GstEnable = "не определен";
                    UtilClass.TableComputersList[i].AdmPass = "не определен";
                    UtilClass.TableComputersList[i].Status = "не определен";
                    UtilClass.TableComputersList[i].BStatus = false;
                }

                    
                UtilClass.PingComputers();

            }
            );

            runningScanThread.Start();

        }

    private void Btn_ScanStop_Click(object sender, RoutedEventArgs e)
    {
            runningScanThread.Abort();


         while (true)
             {
                if (runningScanThread.ThreadState == System.Threading.ThreadState.Aborted)
                {
                    Console.WriteLine("Поток прерван.");
                    break;
                }
             }
 
    }

        private void DataGridComputers_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void Btn_getListFromFile_Click(object sender, RoutedEventArgs e)
        {
            string FilePath = "";
            string DataFile = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if ((bool)openFileDialog.ShowDialog())
            {
                FilePath = openFileDialog.FileName;
                UtilClass.TableComputersList.Clear();


                try {
                    using (StreamReader sr = new StreamReader(FilePath))
                    {

                        while ((DataFile = sr.ReadLine()) != null)
                        {
                            Console.WriteLine(DataFile);
                            UtilClass.TableComputersList.Add(
                                new TableComputers
                                {
                                    Name = DataFile.Trim()
                                }

                                );


                        }
                    }

                  btn_ScanStart.IsEnabled = true;

                } catch
                        {

                    MessageBox.Show("Ошибка чтения файла!");
                        }
                }

           
        }

        private void Btn_DoStart_Click(object sender, RoutedEventArgs e)
        {

            btn_DoStart.IsEnabled = false;
            btn_DoStop.IsEnabled = true;


            ControlForm ctrl = new ControlForm
            {

                EnableLocalAdm = (bool)radio_AdminEnable.IsChecked,
                DisableGHost = (bool)chk_GuestDisable.IsChecked,
                remLocalAdm = (bool)Chk_AdminRename.IsChecked,
                remGHost = (bool)chk_GuestRename.IsChecked,
                setPwdLocalAdm = (bool)chk_SetPassword.IsChecked,
                password = edit_NewPass.Password.Trim(),
                nameLocalAdm = edit_LocalAdm.Text.Trim(),
                nameGHost = edit_Guest.Text.Trim(),
                WaitReady = (bool)chk_WaitReady.IsChecked,
                IngnorPing = (bool)chk_IgnorPing.IsChecked,
                RemoveUS =(bool)chk_RemoveUS.IsChecked

            };

            if (ctrl.setPwdLocalAdm)
            {
                if (edit_NewPass.Password.Trim() != edit_newPassComfim.Password.Trim())
                {

                    MessageBox.Show("Пароли не совпадают!");
                    return;
                }

            }

            runningDoThread = new Thread(() =>
            {
            Console.WriteLine("Поток внесения изменений запущен.");

            foreach (var comp in UtilClass.TableComputersList)
            {
                comp.BStatus = false;
            }

            UtilClass.SetInfoComputers(ctrl);

            }
                );

            runningDoThread.Start();
        }

        private void Btn_DoStop_Click(object sender, RoutedEventArgs e)
        {
            runningDoThread.Abort();
        }
    }
}
