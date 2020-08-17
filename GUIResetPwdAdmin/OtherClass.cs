using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GUIResetPwdAdmin
{

    public class TableComputers
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Ping { get; set; }
        public bool PingFail { get; set; }
        public string Admin{ get; set; }
        public string AdmEnable { get; set; }
        public string Guest { get; set; }
        public string OtherAdmins { get; set; }
        public string OtherUsers { get; set; }
        public string GstEnable { get; set; }
        public string AdmPass { get; set; }
        public string Status { get; set; }
        public bool BStatus { get; set; }
    }

    public class Users
    {
        public DirectoryEntry LocalAdm { get; set; }
        public bool isEnableLocalAdm { get; set; }
        public DirectoryEntry Ghost { get; set; }
        public List<DirectoryEntry> OtherAdmins { get; set; }
        public List<Tuple<DirectoryEntry,string>> OtherUsers { get; set; }
        public bool isEnableGhost { get; set; }
        public string Error { get; set; }

    }

    public struct ControlForm
    {
        public bool EnableLocalAdm;
        public bool DisableGHost;
        public bool remLocalAdm;
        public string nameLocalAdm;
        public bool remGHost;
        public string nameGHost;
        public bool setPwdLocalAdm;
        public string password;
        public bool WaitReady;
        public bool IngnorPing;
        public bool RemoveUS;

    }

    public enum LdapFilter { none, UsersSN, UsersCN, Computers, Groups, OU, UsersName };


    class CellColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            SolidColorBrush color;

            if (value != null)
            {
                if (((string)value).ToLower().IndexOf("error") > -1)
                {
                    color = new SolidColorBrush(Colors.OrangeRed);

                }
                else
                {
                    color = new SolidColorBrush(Colors.White);
                }
            }else color = new SolidColorBrush(Colors.White);

            return color;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public static class DataGridHelper
    {
        public static DataGridCell FindCell(int iRow, int iColumn, DataGrid dg)
        {
            if (dg.Items.Count <= 1) return null;
            else 
            return GetCell(new DataGridCellInfo(dg.Items[iRow], dg.Columns[iColumn]));
        }

        public static DataGridCell GetCell(DataGridCellInfo dataGridCellInfo)
        {
            if (!dataGridCellInfo.IsValid)
            {
                return null;
            }

            var cellContent = dataGridCellInfo.Column.GetCellContent(dataGridCellInfo.Item);
            if (cellContent != null)
            {
                return (DataGridCell)cellContent.Parent;
            }
            else
            {
                return null;
            }
        }
        public static int GetRowIndex(DataGridCell dataGridCell)
        {
            PropertyInfo rowDataItemProperty = dataGridCell.GetType().GetProperty("RowDataItem", BindingFlags.Instance | BindingFlags.NonPublic);

            DataGrid dataGrid = GetDataGridFromChild(dataGridCell);

            return dataGrid.Items.IndexOf(rowDataItemProperty.GetValue(dataGridCell, null));
        }
        public static DataGrid GetDataGridFromChild(DependencyObject dataGridPart)
        {
            if (VisualTreeHelper.GetParent(dataGridPart) == null)
            {
                throw new NullReferenceException("Control is null.");
            }
            if (VisualTreeHelper.GetParent(dataGridPart) is DataGrid)
            {
                return (DataGrid)VisualTreeHelper.GetParent(dataGridPart);
            }
            else
            {
                return GetDataGridFromChild(VisualTreeHelper.GetParent(dataGridPart));
            }
        }
    }

}
