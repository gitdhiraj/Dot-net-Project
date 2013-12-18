using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay;
using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Reflection;
using Microsoft.Reporting.WinForms;
using System.Windows.Controls.Primitives;

namespace IntelliOpticsReport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isReportViewerLoaded;
        int minvalue = 0, MinValue = -9999999,
         maxvalue = 999999,
         startvalue = -3000, Senssorvalue = 3000, MaxHour = 12, MaxMinute = 59;
        string Starthour, StartMinute, EndHour, EndMinute, Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate, peakselected = string.Empty, SelectedLastDays, Reportingquery, SearchSensorvalue;
        List<TripInfo> tripList;
        SqlParameter MaxParm;
        string settings = ConfigurationSettings.AppSettings["ConnectionString"].ToString();
        static ArrayList Checklistobj = new ArrayList();
        static ArrayList listChkboxobj = new ArrayList();
        SqlConnection connection;
        SqlCommand command;
        SqlDataAdapter adapter = new SqlDataAdapter();
        DataSet ds, DSGridGraph, DsMax, DSMin, DSAvg;
        static string SelectedSensser;

        public MainWindow()
        {
            InitializeComponent();
            txtStrhr.Text = MaxHour.ToString();
            txtStrMin.Text = minvalue.ToString() + "0";
            txtEndhr.Text = MaxHour.ToString();
            txtEndMinute.Text = minvalue.ToString() + "0";
            NUDTextBox.Text = startvalue.ToString();
            txtSensorLess.Text = Senssorvalue.ToString();
            SearchSensorvalue = string.Empty;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                lblReadingFrom.Visibility = Visibility.Hidden;
                lblMaximum.Visibility = Visibility.Hidden;
                lblMinimum.Visibility = Visibility.Hidden;
                lblAverage.Visibility = Visibility.Hidden;
                bindSensors();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error  :" + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        private void rdbSelectPeak_Click(object sender, RoutedEventArgs e)
        {
            if (rdbSelectPeak.IsChecked == true)
            {
                peakselected = "IsChecked";
            }
            else { peakselected = string.Empty; }
        }
        private void rdbtn7dys_Click(object sender, RoutedEventArgs e)
        {
            SelectedLastDays = "Is7DaysCheck";           
        }

        private void rdbtn30dys_Click(object sender, RoutedEventArgs e)
        {
            SelectedLastDays = "Is30DaysCheck";           
        }

        private void rdbtn90dys_Click(object sender, RoutedEventArgs e)
        {
            SelectedLastDays = "Is90DaysCheck";            
        }
        private void btnViewRprt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dtpStartDate.SelectedDate.ToString() == string.Empty)
                {
                    MessageBox.Show("Please Select Start Date");
                    dtpStartDate.Focus();
                    return;
                }
                if (dtpEndDate.SelectedDate.ToString() == string.Empty)
                {
                    MessageBox.Show("Please Select End Date");
                    dtpEndDate.Focus();
                    return;
                }
                SearchSensorvalue = string.Empty;
                txtFind.Text = string.Empty;
                plotter.Children.RemoveAll(typeof(LineGraph));
                plotter.Children.RemoveAll(typeof(MarkerPointsGraph));
                ComboBoxItem cmbStrAMPM = (ComboBoxItem)combStrAMPM.SelectedItem;
                ComboBoxItem cmbEndAMPM = (ComboBoxItem)comboEndAMPM.SelectedItem;
                Starttime = Starthour + " " + ":" + StartMinute + " " + cmbStrAMPM.Content.ToString();
                Endtime = EndHour + " " + ":" + EndMinute + " " + cmbEndAMPM.Content.ToString();
                Strgreater = NUDTextBox.Text;
                Strless = txtSensorLess.Text;
                strStartDate = dtpStartDate.SelectedDate.Value.ToShortDateString();
                strEndDate = dtpEndDate.SelectedDate.Value.ToShortDateString();
                ds = new DataSet();
                DsMax = new DataSet();
                DSMin = new DataSet();
                DSAvg = new DataSet();
                if (SelectedSensser != null)
                {
                    List<string> SelectedItems = SelectedSensser.TrimEnd(',').Split(',').ToList();
                    ds = DrawGraph(SelectedItems, Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate);
                    if (peakselected == "IsChecked")
                    {
                        DsMax = FilterMaxReadingData(SelectedItems, Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate);
                        DsMax.Dispose();
                        DSAvg.Dispose();
                    }
                    else
                    {
                        DsMax = FilterMaxReadingData(SelectedItems, Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate);
                        DSMin = FilterMinReadingData(SelectedItems, Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate);
                        DSAvg = FilterAvgReading(SelectedItems, Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate);
                    }
                    BindFilterDataSetToObject(Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate);
                }
                else
                {
                    MessageBox.Show("Please Select Atleast One Optical Senssor of Any Zone"); return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error  :" + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        private DataSet FilterMaxReadingData(List<string> SelectedItems, string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                connection = new SqlConnection(settings);
                connection.Open();
                string strColumns = string.Empty;
                string maxColumns = string.Empty;
                string StrSenssor = string.Empty;

                if (SelectedItems.Count == 1)
                {
                    strColumns = "ROUND" + " " + "(" + SelectedItems[0] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[0];
                    StrSenssor = SelectedItems[0] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess;
                    maxColumns = "ROUND" + " " + "(" + "MAX(" + SelectedItems[0] + " " + ")" + "," + "2" + ") as" + " " + SelectedItems[0];
                }
                else
                {
                    for (int i = 0; i < SelectedItems.Count; i++)
                    {
                        strColumns += "ROUND" + " " + "(" + SelectedItems[i] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[i] + ",";
                        StrSenssor += SelectedItems[i] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess + " " + "OR" + " ";
                        maxColumns += "ROUND" + " " + "(" + "MAX(" + SelectedItems[i] + " " + ")" + "," + "2" + ") as" + " " + SelectedItems[i] + ",";

                    }
                    maxColumns = maxColumns.Remove(maxColumns.Length - 1, 1);
                    StrSenssor = StrSenssor.Remove(StrSenssor.Length - 3, 3);
                    strColumns = strColumns.Remove(strColumns.Length - 1, 1);
                }
                string SqlMaxFilter = string.Empty;
                DataSet dsMax = new DataSet();
                if (SelectedLastDays == "Is7DaysCheck")
                {
                    SqlMaxFilter = "select " + maxColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "7" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else if (SelectedLastDays == "Is30DaysCheck")
                {
                    SqlMaxFilter = "select " + maxColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "30" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else if (SelectedLastDays == "Is90DaysCheck")
                {
                    SqlMaxFilter = "select " + maxColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "90" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else
                {
                    SqlMaxFilter = "select " + maxColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")";
                }
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetFilterReading";
                MaxParm = new SqlParameter("@itemsAvg", SqlDbType.VarChar);
                MaxParm.Value = SqlMaxFilter;
                command.Parameters.Add(MaxParm);
                adapter.SelectCommand = command;
                adapter.Fill(dsMax);
                return dsMax;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                adapter.Dispose();
                command.Dispose();
                connection.Close();
            }
        }
        public DataSet FilterMinReadingData(List<string> SelectedItems, string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                DataSet dsMin = new DataSet();
                string SqlMinFilter = string.Empty;
                connection = new SqlConnection(settings);
                connection.Open();
                string strColumns = string.Empty;
                string MinColumns = string.Empty;
                string StrSenssor = string.Empty;
                if (SelectedItems.Count == 1)
                {
                    strColumns = "ROUND" + " " + "(" + SelectedItems[0] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[0];
                    StrSenssor = SelectedItems[0] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess;
                    MinColumns = "ROUND" + " " + "(" + "MIN(" + SelectedItems[0] + " " + ")" + "," + "2" + ") as" + " " + SelectedItems[0];
                }
                else
                {
                    for (int i = 0; i < SelectedItems.Count; i++)
                    {
                        strColumns += "ROUND" + " " + "(" + SelectedItems[i] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[i] + ",";
                        StrSenssor += SelectedItems[i] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess + " " + "OR" + " ";
                        MinColumns += "ROUND" + " " + "(" + "MIN(" + SelectedItems[i] + " " + ")" + "," + "2" + ") as" + " " + SelectedItems[i] + ",";
                    }
                    StrSenssor = StrSenssor.Remove(StrSenssor.Length - 3, 3);
                    MinColumns = MinColumns.Remove(MinColumns.Length - 1, 1);
                    strColumns = strColumns.Remove(strColumns.Length - 1, 1);
                }

                if (SelectedLastDays == "Is7DaysCheck")
                {
                    SqlMinFilter = "select " + MinColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "7" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else if (SelectedLastDays == "Is30DaysCheck")
                {
                    SqlMinFilter = "select " + MinColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "30" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else if (SelectedLastDays == "Is90DaysCheck")
                {
                    SqlMinFilter = "select " + MinColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "90" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else
                {
                    SqlMinFilter = "select " + MinColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")";
                }
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetFilterReading";
                MaxParm = new SqlParameter("@itemsAvg", SqlDbType.VarChar);
                MaxParm.Value = SqlMinFilter;
                command.Parameters.Add(MaxParm);
                adapter.SelectCommand = command;
                adapter.Fill(dsMin);
                return dsMin;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                adapter.Dispose();
                command.Dispose();
                connection.Close();
            }
        }
        private DataSet FilterAvgReading(List<string> SelectedItems, string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                DataSet dsAvg = new DataSet();
                string SqlAvgFilter = string.Empty;
                connection = new SqlConnection(settings);
                connection.Open();
                string strColumns = string.Empty;
                string AvgColumns = string.Empty;
                string StrSenssor = string.Empty;
                if (SelectedItems.Count == 1)
                {
                    strColumns = "ROUND" + " " + "(" + SelectedItems[0] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[0];
                    StrSenssor = SelectedItems[0] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess;
                    AvgColumns = "ROUND" + " " + "(" + "Avg(" + SelectedItems[0] + " " + ")" + "," + "2" + ") as" + " " + SelectedItems[0];
                }
                else
                {
                    for (int i = 0; i < SelectedItems.Count; i++)
                    {
                        strColumns += "ROUND" + " " + "(" + SelectedItems[i] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[i] + ",";
                        StrSenssor += SelectedItems[i] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess + " " + "OR" + " ";
                        AvgColumns += "ROUND" + " " + "(" + "Avg(" + SelectedItems[i] + " " + ")" + "," + "2" + ") as" + " " + SelectedItems[i] + ",";
                    }
                    StrSenssor = StrSenssor.Remove(StrSenssor.Length - 3, 3);
                    AvgColumns = AvgColumns.Remove(AvgColumns.Length - 1, 1);
                    strColumns = strColumns.Remove(strColumns.Length - 1, 1);
                }
                if (SelectedLastDays == "Is7DaysCheck")
                {
                    SqlAvgFilter = "select " + AvgColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "7" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else if (SelectedLastDays == "Is30DaysCheck")
                {
                    SqlAvgFilter = "select " + AvgColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "30" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else if (SelectedLastDays == "Is90DaysCheck")
                {
                    SqlAvgFilter = "select " + AvgColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "90" + " " + "AND" + " " + "GETDATE()" + " " + ")";
                }
                else
                {
                    SqlAvgFilter = "select " + AvgColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")";
                }
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetFilterReading";
                MaxParm = new SqlParameter("@itemsAvg", SqlDbType.VarChar);
                MaxParm.Value = SqlAvgFilter;
                command.Parameters.Add(MaxParm);
                adapter.SelectCommand = command;
                adapter.Fill(dsAvg);
                return dsAvg;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                adapter.Dispose();
                command.Dispose();
                connection.Close();
            }
        }
        private DataSet DrawGraph(List<string> SelectedItems, string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                DSGridGraph = new DataSet();
                string sql = string.Empty;
                connection = new SqlConnection(settings);
                connection.Open();
                string strColumns = string.Empty;
                string StrSenssor = string.Empty;
                if (SelectedItems.Count == 1)
                {
                    strColumns = "ROUND" + " " + "(" + SelectedItems[0] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[0];
                    StrSenssor = SelectedItems[0] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess;
                }
                else
                {
                    for (int i = 0; i < SelectedItems.Count; i++)
                    {
                        strColumns += "ROUND" + " " + "(" + SelectedItems[i] + " " + "," + "2" + " " + ")" + "as" + " " + SelectedItems[i] + ",";
                        StrSenssor += SelectedItems[i] + " " + "Between" + " " + NUDTextBox + " " + "And" + " " + txtSensorLess + " " + "OR" + " ";
                    }
                    StrSenssor = StrSenssor.Remove(StrSenssor.Length - 3, 3);
                    strColumns = strColumns.Remove(strColumns.Length - 1, 1);
                }
                if (SelectedLastDays == "Is7DaysCheck")
                {
                    sql = "select RECORD_TIMESTAMP ," + strColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "7" + " " + "AND" + " " + "GETDATE()" + " " + ")" + " " + "Order by" + " " + "RECORD_TIMESTAMP ASC";
                }
                else if (SelectedLastDays == "Is30DaysCheck")
                {
                    sql = "select RECORD_TIMESTAMP ," + strColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "30" + " " + "AND" + " " + "GETDATE()" + " " + ")" + " " + "Order by" + " " + "RECORD_TIMESTAMP ASC";
                }
                else if (SelectedLastDays == "Is90DaysCheck")
                {
                    sql = "select RECORD_TIMESTAMP ," + strColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "OR" + "(" + " " + "RECORD_TIMESTAMP" + " " + "Between" + " " + "GETDATE()" + "-" + "90" + " " + "AND" + " " + "GETDATE()" + " " + ")" + " " + "Order by" + " " + "RECORD_TIMESTAMP ASC";
                }
                else
                {
                    sql = "select RECORD_TIMESTAMP ," + strColumns + " from TBL_SENSORS Where RECORD_TIMESTAMP Between " + "'" + dtpStartDate + " " + Starttime + "'" + " and " + "'" + dtpEndDate + " " + Endtime + "'" + " " + "And" + " " + "(" + " " + StrSenssor + " " + ")" + " " + "Order by" + " " + "RECORD_TIMESTAMP ASC";
                }
                Reportingquery = sql;
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetFilterReading";
                MaxParm = new SqlParameter("@itemsAvg", SqlDbType.VarChar);
                MaxParm.Value = sql;
                command.Parameters.Add(MaxParm);
                adapter.SelectCommand = command;
                adapter.Fill(DSGridGraph);
                return DSGridGraph;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                adapter.Dispose();
                command.Dispose();
                connection.Close();
            }
        }
        private void BindFilterDataSetToObject(string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                /*Add for custom legend*/
                List<ItemVM> Items;
                List<string> lst = new List<string> { };
                var converter = new System.Windows.Media.BrushConverter();
                List<string> SelectedItems = SelectedSensser.TrimEnd(',').Split(',').ToList();
                Color[] colors = ColorHelper.CreateRandomColors(SelectedItems.Count);

                /*end*/
                #region variable Declaration
                string ChckEmptyMaxDS = string.Empty;
                string ChckMinFilterDS = string.Empty;
                string ChckAvgFilterDS = string.Empty;
                #endregion

                tabControl1.SelectedIndex = 1;
                Items = new List<ItemVM>();

                // This Dataset deals with Maximum results of the Peakselcted operation.
                if (peakselected == "IsChecked")
                {
                    for (int i = 0; i < DsMax.Tables[0].Columns.Count; i++)
                    {
                        ChckEmptyMaxDS += DsMax.Tables[0].Rows[0].ItemArray[i].ToString();
                    }
                    if (ChckEmptyMaxDS != string.Empty)
                    {
                        dtgMax.ItemsSource = DsMax.Tables[0].DefaultView;
                        lblReadingFrom.Content = "Senssor Readings From" + " " + dtpStartDate + " " + Starttime + " " + "To" + " " + dtpEndDate + " " + Endtime;
                        lblReadingFrom.Visibility = Visibility.Visible;
                        dtgMax.Visibility = Visibility.Visible;
                        lblMaximum.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        dtgMax.ItemsSource = null;
                        lblReadingFrom.Visibility = Visibility.Hidden;
                        dtgMax.Visibility = Visibility.Hidden;
                        lblMaximum.Visibility = Visibility.Hidden;
                    }
                    dtgMin.ItemsSource = null;
                    dtgMin.Visibility = Visibility.Hidden;
                    lblMinimum.Visibility = Visibility.Hidden;
                    dtgAvg.ItemsSource = null;
                    dtgAvg.Visibility = Visibility.Hidden;
                    lblAverage.Visibility = Visibility.Hidden;
                }
                else
                {
                    // This Dataset deals with Maximum results of the background operation.
                    for (int i = 0; i < DsMax.Tables[0].Columns.Count; i++)
                    {
                        ChckEmptyMaxDS += DsMax.Tables[0].Rows[0].ItemArray[i].ToString();
                    }
                    if (ChckEmptyMaxDS != string.Empty)
                    {
                        dtgMax.ItemsSource = DsMax.Tables[0].DefaultView;
                        lblReadingFrom.Content = "Senssor Readings From" + " " + dtpStartDate + " " + Starttime + " " + "To" + " " + dtpEndDate + " " + Endtime;
                        lblReadingFrom.Visibility = Visibility.Visible;
                        dtgMax.Visibility = Visibility.Visible;
                        lblMaximum.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        dtgMax.ItemsSource = null;
                        lblReadingFrom.Visibility = Visibility.Hidden;
                        dtgMax.Visibility = Visibility.Hidden;
                        lblMaximum.Visibility = Visibility.Hidden;
                    }
                    //This Dataset deals with Minimum results of the background operation.

                    for (int i = 0; i < DSMin.Tables[0].Columns.Count; i++)
                    {
                        ChckMinFilterDS += DSMin.Tables[0].Rows[0].ItemArray[i].ToString();
                    }
                    if (ChckMinFilterDS != string.Empty)
                    {
                        dtgMin.ItemsSource = DSMin.Tables[0].DefaultView;
                        lblReadingFrom.Content = "Senssor Readings From" + " " + dtpStartDate + " " + Starttime + " " + "To" + " " + dtpEndDate + " " + Endtime;
                        lblReadingFrom.Visibility = Visibility.Visible;
                        dtgMin.Visibility = Visibility.Visible;
                        lblMinimum.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        dtgMin.ItemsSource = null;
                        lblReadingFrom.Visibility = Visibility.Hidden;
                        dtgMin.Visibility = Visibility.Hidden;
                        lblMinimum.Visibility = Visibility.Hidden;
                    }

                    //This Dataset deals with Average results of the background operation.

                    for (int i = 0; i < DSAvg.Tables[0].Columns.Count; i++)
                    {
                        ChckAvgFilterDS += DSAvg.Tables[0].Rows[0].ItemArray[i].ToString();
                    }
                    if (ChckAvgFilterDS != string.Empty)
                    {
                        dtgAvg.ItemsSource = DSAvg.Tables[0].DefaultView;
                        lblReadingFrom.Content = "Senssor Readings From" + " " + dtpStartDate + " " + Starttime + " " + "To" + " " + dtpEndDate + " " + Endtime;
                        lblReadingFrom.Visibility = Visibility.Visible;
                        dtgAvg.Visibility = Visibility.Visible;
                        lblAverage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        dtgAvg.ItemsSource = null;
                        lblReadingFrom.Visibility = Visibility.Hidden;
                        dtgAvg.Visibility = Visibility.Hidden;
                        lblAverage.Visibility = Visibility.Hidden;
                    }
                }

                //This Dataset deals with Graph And Datagrid results of the background operation.

                if (ds.Tables[0].Rows.Count > 0)
                {
                    BindDsGridGrap(ds, Starttime, Endtime, NUDTextBox, txtSensorLess, dtpStartDate, dtpEndDate);
                }
                else
                {
                    for (int i = 0; i < SelectedItems.Count; i++)
                    {
                        SensorInfo objinfo = new SensorInfo();
                        objinfo.Date = DateTime.Now.Date;
                        objinfo.Rate = 0.0;
                        List<SensorInfo> Sensor = new List<SensorInfo>();
                        Sensor.Add(objinfo);
                        plotter.AddLineGraph(CreateSensorDataSource(Sensor), colors[i], 1, SelectedItems[i]);
                        plotter.LegendVisible = false;
                        plotter.FitToView();
                    }
                    dtgSensorReadingList.ItemsSource = ds.Tables[0].DefaultView;
                    dtgSensorReadingList.Visibility = Visibility.Hidden;
                    listview.ItemsSource = null;
                    listview.Visibility = Visibility.Hidden;
                    txtCurrentPgindex.Text = string.Empty;
                    lblTotalCount.Content = "of 1";
                    MessageBox.Show("There is no Data for Selected Sensors on :" + dtpStartDate + " " + "to" + dtpEndDate);
                    return;
                }
            }
            catch (Exception ex) { throw ex; }
        }
        private void BindDsGridGrap(DataSet ds, string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                #region custom legend Declaration..
                /*Add for custom legend*/
                List<ItemVM> Items;
                List<string> lst = new List<string> { };
                var converter = new System.Windows.Media.BrushConverter();
                List<string> SelectedItems = SelectedSensser.TrimEnd(',').Split(',').ToList();
                Color[] colors = ColorHelper.CreateRandomColors(SelectedItems.Count);
                /*end*/
                #endregion
                Items = new List<ItemVM>();
                if (ds.Tables[0].Rows.Count % paging_NoOfRecPerPage == 0)
                {
                    lblTotalCount.Content = "of" + " " + (ds.Tables[0].Rows.Count / paging_NoOfRecPerPage).ToString();
                }
                else
                {
                    lblTotalCount.Content = "of" + " " + ((ds.Tables[0].Rows.Count / paging_NoOfRecPerPage)).ToString();
                }
                dtgSensorReadingList.Visibility = Visibility.Visible;
                //Commented on 22-05-2012 By Dhiraj    
                DataSet lineDS, datDs;
                for (int i = 0; i < SelectedItems.Count; i++)
                {
                    lineDS = new DataSet();
                    datDs = new DataSet();
                    datDs = DatelineData(SelectedItems[i].ToString(), Starttime, Endtime, NUDTextBox, txtSensorLess, dtpStartDate, dtpEndDate);
                    lineDS = GraphlineDraw(SelectedItems[i].ToString(), Starttime, Endtime, NUDTextBox, txtSensorLess, dtpStartDate, dtpEndDate);
                    if (datDs.Tables[0].Rows.Count > 0 & lineDS.Tables[0].Rows.Count > 0)
                    {
                        var dates = (from dr in datDs.Tables[0].AsEnumerable()
                                     select new
                                     {
                                         date = dr.Field<DateTime>("DateRecorded")
                                     }.date).ToList();
                        var Rate = (from dr in lineDS.Tables[0].AsEnumerable()
                                    select new
                                    {
                                        rate = dr.Field<double>(SelectedItems[i])
                                    }.rate).ToList();
                        var datesDataSource = new EnumerableDataSource<DateTime>(dates);
                        datesDataSource.SetXMapping(x => dateAxis.ConvertToDouble(x));
                        var RateDataSource = new EnumerableDataSource<double>(Rate);
                        RateDataSource.SetYMapping(y => y);
                        CompositeDataSource compositeDataSourceSenssor = new CompositeDataSource(datesDataSource, RateDataSource);
                        plotter.AddLineGraph(compositeDataSourceSenssor, colors[i], 1, SelectedItems[i]);
                        Items.Add(new ItemVM((Brush)converter.ConvertFromString(colors[i].ToString()), SelectedItems[i]));
                    }
                }
                plotter.Viewport.FitToView();
                plotter.LegendVisible = false;
                listview.Visibility = Visibility.Visible;
                listview.ItemsSource = Items;                               
                lblRportStartDate.Content = dtpStartDate;                
                lblRportEndDate.Content = dtpEndDate;
                paging_PageIndex = 1;
                CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
            }
            catch (Exception ex) { throw ex; }
        }
        //comment on Rupinder 24-05-2012
        private DataSet GraphlineDraw(string selecteditem, string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                SqlParameter colomNamePrm, startdatePrm, EnddatePrm, StartPrm, EndPrm;
                connection = new SqlConnection(settings);
                connection.Open();
                DataSet objds = new DataSet();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "getAvgColumn";
                colomNamePrm = new SqlParameter("@ColumnName", SqlDbType.VarChar);
                startdatePrm = new SqlParameter("@StartDate", SqlDbType.VarChar);
                EnddatePrm = new SqlParameter("@EndDate", SqlDbType.VarChar);
                StartPrm = new SqlParameter("@Start", SqlDbType.VarChar);
                EndPrm = new SqlParameter("@End", SqlDbType.VarChar);
                colomNamePrm.Value = selecteditem;
                startdatePrm.Value = dtpStartDate + " " + Starttime;
                EnddatePrm.Value = dtpEndDate + " " + Endtime;
                StartPrm.Value = NUDTextBox;
                EndPrm.Value = txtSensorLess;
                command.Parameters.Add(colomNamePrm);
                command.Parameters.Add(startdatePrm);
                command.Parameters.Add(EnddatePrm);
                command.Parameters.Add(StartPrm);
                command.Parameters.Add(EndPrm);
                adapter.SelectCommand = command;
                adapter.Fill(objds);

                return objds;
            }
            catch (Exception ex) { throw ex; }
            finally
            {
                command.Dispose();
                adapter.Dispose();
                connection.Close();
            }
        }
        private DataSet DatelineData(string items, string Starttime, string Endtime, string NUDTextBox, string txtSensorLess, string dtpStartDate, string dtpEndDate)
        {
            try
            {
                SqlParameter colomNamePrm, startdatePrm, EnddatePrm, StartPrm, EndPrm;
                connection = new SqlConnection(settings);
                connection.Open();
                DataSet dateDs = new DataSet();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetAvgDate";
                colomNamePrm = new SqlParameter("@ColumnName", SqlDbType.VarChar);
                startdatePrm = new SqlParameter("@StartDate", SqlDbType.VarChar);
                EnddatePrm = new SqlParameter("@EndDate", SqlDbType.VarChar);
                StartPrm = new SqlParameter("@Start", SqlDbType.VarChar);
                EndPrm = new SqlParameter("@End", SqlDbType.VarChar);
                colomNamePrm.Value = items;
                startdatePrm.Value = dtpStartDate + " " + Starttime;
                EnddatePrm.Value = dtpEndDate + " " + Endtime;
                StartPrm.Value = NUDTextBox;
                EndPrm.Value = txtSensorLess;
                command.Parameters.Add(colomNamePrm);
                command.Parameters.Add(startdatePrm);
                command.Parameters.Add(EnddatePrm);
                command.Parameters.Add(StartPrm);
                command.Parameters.Add(EndPrm);
                adapter.SelectCommand = command;
                adapter.Fill(dateDs);

                return dateDs;
            }
            catch (Exception ex) { throw ex; }
            finally
            {
                command.Dispose();
                adapter.Dispose();
                connection.Close();
            }
        }
        //purpose to use Stored procedure

        //This provide Custom Paging of Datagrid.....

        private enum PagingMode { Next = 1, Previous = 2 };
        private int paging_PageIndex = 1;
        private int paging_NoOfRecPerPage = 10;
        private void CustomPaging(DataSet ds, int mode, DataGrid channelGrid)
        {
            try
            {
                DataTable dt = ds.Tables[0];
                int totalRecords = ds.Tables[0].Rows.Count;
                int pageSize = paging_NoOfRecPerPage;
                if (totalRecords <= pageSize) { return; }
                switch (mode)
                {
                    case (int)PagingMode.Next:
                        if (totalRecords > (paging_PageIndex * pageSize))
                        {
                            DataTable tmpTable = new DataTable();
                            DataSet DS = new DataSet();
                            tmpTable = dt.Clone();

                            if (totalRecords >= ((paging_PageIndex * pageSize) + pageSize))
                            {
                                for (int i = paging_PageIndex * pageSize; i < ((paging_PageIndex * pageSize) + pageSize); i++)
                                {
                                    tmpTable.ImportRow(dt.Rows[i]);
                                }
                                DS.Tables.Add(tmpTable);
                            }
                            else
                            {
                                for (int i = paging_PageIndex * pageSize; i < totalRecords; i++)
                                {
                                    tmpTable.ImportRow(dt.Rows[i]);
                                }
                                DS.Tables.Add(tmpTable);
                            }
                            txtCurrentPgindex.Text = Convert.ToString(paging_PageIndex);
                            channelGrid.ItemsSource = tmpTable.DefaultView;
                            tmpTable.Dispose();
                            DS.Dispose();
                        }
                        else if (totalRecords == (paging_PageIndex * pageSize))
                        {
                            DataTable tmpTable = new DataTable();
                            DataSet DS = new DataSet();
                            tmpTable = dt.Clone();
                            for (int i = ((paging_PageIndex * pageSize) - 1); i > totalRecords - pageSize; i--)
                            {
                                tmpTable.ImportRow(dt.Rows[i]);
                            }
                            DS.Tables.Add(tmpTable);
                            txtCurrentPgindex.Text = Convert.ToString(paging_PageIndex);
                            channelGrid.ItemsSource = tmpTable.DefaultView;
                            tmpTable.Dispose();
                            DS.Dispose();
                        }
                        break;

                    case (int)PagingMode.Previous:
                        if (paging_PageIndex >= 1)
                        {
                            DataTable tmpTable = new DataTable();
                            DataSet DS = new DataSet();
                            tmpTable = dt.Clone();
                            txtCurrentPgindex.Text = Convert.ToString(paging_PageIndex);
                            for (int i = ((paging_PageIndex * pageSize) - pageSize); i < (paging_PageIndex * pageSize); i++)
                            {
                                tmpTable.ImportRow(dt.Rows[i]);
                            }
                            DS.Tables.Add(tmpTable);
                            channelGrid.ItemsSource = tmpTable.DefaultView;
                            tmpTable.Dispose();
                            DS.Dispose();
                        }
                        break;
                }
                if (SearchSensorvalue == "btnfind")
                {
                    SearchingSensor(channelGrid);
                }
            }
            catch (Exception ex) { throw ex; }
        }
        private void SearchingSensor(DataGrid channelGrid)
        {
            try
            {
                for (int i = 0; i < channelGrid.Items.Count; i++)
                {
                    //channelGrid.ScrollIntoView(channelGrid.Items[i]);
                    DataGridRow row = (DataGridRow)channelGrid.ItemContainerGenerator.ContainerFromItem(channelGrid.Items[i]);
                    if (row == null)
                    {
                        // May be virtualized, bring into view and try again.
                        channelGrid.UpdateLayout();
                        channelGrid.ScrollIntoView(channelGrid.Items[i]);
                        row = (DataGridRow)channelGrid.ItemContainerGenerator.ContainerFromIndex(i);
                    }
                    for (int j = 0; j < channelGrid.Columns.Count; j++)
                    {
                        if (row != null)
                        {
                            TextBlock cellContent = channelGrid.Columns[j].GetCellContent(row) as TextBlock;
                            if (cellContent != null && cellContent.Text.Equals(txtFind.Text))
                            {
                                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);
                                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(j);
                                if (cell == null)
                                {
                                    // now try to bring into view and retreive the cell
                                    dtgMax.ScrollIntoView(row, channelGrid.Columns[j]);
                                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(j);
                                }
                                channelGrid.ScrollIntoView(row, channelGrid.Columns[j]);
                                channelGrid.SelectedItem = cell;
                                cell.Focus();
                                cell.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                                cell.IsSelected = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void imgFirst_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SelectedSensser != null)
                {
                    SearchSensorvalue = string.Empty;
                    paging_PageIndex = 1;
                    CustomPaging(ds, (int)PagingMode.Previous, dtgSensorReadingList);
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void imgLast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SelectedSensser != null)
                {
                    SearchSensorvalue = string.Empty;
                    if (ds.Tables[0].Rows.Count % paging_NoOfRecPerPage == 0)
                    {
                        paging_PageIndex = ds.Tables[0].Rows.Count / paging_NoOfRecPerPage;
                    }
                    else
                    {
                        paging_PageIndex = ds.Tables[0].Rows.Count / paging_NoOfRecPerPage;
                    }
                    CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                }
            }
            catch (Exception ex) { throw ex; }
        }
        private void imgNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SelectedSensser != null)
                {
                    SearchSensorvalue = string.Empty;
                    if (ds.Tables[0].Rows.Count % paging_NoOfRecPerPage == 0)
                    {
                        if (paging_PageIndex < ds.Tables[0].Rows.Count / paging_NoOfRecPerPage)
                        {
                            paging_PageIndex += 1;
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                        else if (paging_PageIndex == ds.Tables[0].Rows.Count / paging_NoOfRecPerPage)
                        {
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                    }
                    else
                    {
                        SearchSensorvalue = string.Empty;
                        if (paging_PageIndex < (ds.Tables[0].Rows.Count / paging_NoOfRecPerPage))
                        {
                            paging_PageIndex += 1;
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                        else if (paging_PageIndex == (ds.Tables[0].Rows.Count / paging_NoOfRecPerPage))
                        {
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                    }
                }

            }
            catch (Exception ex) { throw ex; }
        }

        private void imgPrevious_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SelectedSensser != null)
                {
                    if (paging_PageIndex > 1)
                    {
                        SearchSensorvalue = string.Empty;
                        paging_PageIndex -= 1;
                        CustomPaging(ds, (int)PagingMode.Previous, dtgSensorReadingList);
                    }
                }
            }
            catch (Exception ex) { throw ex; }
        }

        //End Custom Paging Code Of Datagrid.....

        private EnumerableDataSource<SensorInfo> CreateSensorDataSource(List<SensorInfo> rates)
        {
            try
            {
                EnumerableDataSource<SensorInfo> ds = new EnumerableDataSource<SensorInfo>(rates);
                ds.SetXMapping(ci => dateAxis.ConvertToDouble(ci.Date));
                ds.SetYMapping(ci => ci.Rate);
                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private static List<SensorInfo> LoadSensorRates(DataSet ds, string column)
        {
            try
            {
                var res = new List<SensorInfo>(ds.Tables[0].Rows.Count - 1);
                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (ds.Tables[0].Rows[i][column].ToString() != "")
                    {
                        res.Add(new SensorInfo { Date = DateTime.Parse(ds.Tables[0].Rows[i][0].ToString()), Rate = Double.Parse(ds.Tables[0].Rows[i][column].ToString(), CultureInfo.InvariantCulture) });
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void bindSensors()
        {
            try
            {
                tripList = new List<TripInfo>();
                connection = new SqlConnection(settings);
                connection.Open();
                int[] TempInt = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                for (int i = 0; i < TempInt.Length; i++)
                {
                    switch (TempInt[i])
                    {
                        case 1: List<TripInfo> tripList1 = new List<TripInfo>();
                            SqlParameter Parm1, ParmlikeS, ParmA, ParmlikeA, ParmT, ParmlikeT, ParmD, Parmliked;
                            DataSet ds, Ads, Tds, Dds;
                            SqlDataAdapter da, Ada, Tda, Dda;
                            Parm1 = new SqlParameter("@items", SqlDbType.Int);
                            ParmlikeS = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeA = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeT = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            Parmliked = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ds = new DataSet();
                            Ads = new DataSet();
                            Tds = new DataSet();
                            Dds = new DataSet();
                            da = new SqlDataAdapter();
                            Ada = new SqlDataAdapter();
                            Tda = new SqlDataAdapter();
                            Dda = new SqlDataAdapter();
                            SqlCommand command = new SqlCommand();
                            command.Connection = connection;
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandText = "sp_GetZoneSenssor";
                            Parm1.Value = TempInt[i];
                            ParmlikeS.Value = "S";
                            command.Parameters.Add(Parm1);
                            command.Parameters.Add(ParmlikeS);
                            da.SelectCommand = command;
                            da.Fill(ds, "SensorZones");
                            for (int j = 0; j < ds.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList1.Add(new TripInfo(false, ds.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone1.DataContext = tripList1;
                            List<TripInfo> tripListA = new List<TripInfo>();
                            ParmA = new SqlParameter("@items", SqlDbType.Int);
                            ParmA.Value = TempInt[i];
                            ParmlikeA.Value = "A";
                            SqlCommand commandA = new SqlCommand();
                            commandA.Connection = connection;
                            commandA.CommandType = CommandType.StoredProcedure;
                            commandA.CommandText = "sp_GetZoneSenssor";
                            commandA.Parameters.Add(ParmA);
                            commandA.Parameters.Add(ParmlikeA);
                            Ada.SelectCommand = commandA;
                            Ada.Fill(Ads);
                            for (int j = 0; j < Ads.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Ads.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListA.Add(new TripInfo(false, Ads.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone1A.DataContext = tripListA;

                            List<TripInfo> tripListT = new List<TripInfo>();
                            ParmT = new SqlParameter("@items", SqlDbType.Int);
                            ParmT.Value = TempInt[i];
                            ParmlikeT.Value = "T";
                            SqlCommand commandT = new SqlCommand();
                            commandT.Connection = connection;
                            commandT.CommandType = CommandType.StoredProcedure;
                            commandT.CommandText = "sp_GetZoneSenssor";
                            commandT.Parameters.Add(ParmT);
                            commandT.Parameters.Add(ParmlikeT);
                            Tda.SelectCommand = commandT;
                            Tda.Fill(Tds);
                            for (int j = 0; j < Tds.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Tds.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListT.Add(new TripInfo(false, Tds.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone1T.DataContext = tripListT;

                            List<TripInfo> tripListD = new List<TripInfo>();
                            ParmD = new SqlParameter("@items", SqlDbType.Int);
                            ParmD.Value = TempInt[i];
                            Parmliked.Value = "D";
                            SqlCommand commandD = new SqlCommand();
                            commandD.Connection = connection;
                            commandD.CommandType = CommandType.StoredProcedure;
                            commandD.CommandText = "sp_GetZoneSenssor";
                            commandD.Parameters.Add(ParmD);
                            commandD.Parameters.Add(Parmliked);
                            Dda.SelectCommand = commandD;
                            Dda.Fill(Dds);
                            for (int j = 0; j < Dds.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Dds.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListD.Add(new TripInfo(false, Dds.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone1D1.DataContext = tripListD;
                            break;


                        case 2:

                            List<TripInfo> tripList2 = new List<TripInfo>();
                            SqlParameter Parm2, ParmlikeS2, ParmA2, ParmlikeA2, ParmT2, ParmlikeT2, ParmD2, Parmliked2;
                            DataSet ds2, Ads2, Tds2, Dds2;
                            SqlDataAdapter da2, Ada2, Tda2, Dda2;
                            Parm2 = new SqlParameter("@items", SqlDbType.Int);
                            ParmlikeS2 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeA2 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeT2 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            Parmliked2 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ds2 = new DataSet();
                            Ads2 = new DataSet();
                            Tds2 = new DataSet();
                            Dds2 = new DataSet();
                            da2 = new SqlDataAdapter();
                            Ada2 = new SqlDataAdapter();
                            Tda2 = new SqlDataAdapter();
                            Dda2 = new SqlDataAdapter();
                            Parm2.Value = TempInt[i];
                            ParmlikeS2.Value = "S";
                            SqlCommand command2 = new SqlCommand();
                            command2.Connection = connection;
                            command2.CommandType = CommandType.StoredProcedure;
                            command2.CommandText = "sp_GetZoneSenssor";
                            Parm2.Value = TempInt[i];
                            command2.Parameters.Add(Parm2);
                            command2.Parameters.Add(ParmlikeS2);
                            da2.SelectCommand = command2;
                            da2.Fill(ds2, "SensorZones");
                            for (int j = 0; j < ds2.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds2.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList2.Add(new TripInfo(false, ds2.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lst2.DataContext = tripList2;

                            List<TripInfo> tripListA2 = new List<TripInfo>();
                            ParmA2 = new SqlParameter("@items", SqlDbType.Int);
                            ParmA2.Value = TempInt[i];
                            ParmlikeA2.Value = "A";
                            SqlCommand commandA2 = new SqlCommand();
                            commandA2.Connection = connection;
                            commandA2.CommandType = CommandType.StoredProcedure;
                            commandA2.CommandText = "sp_GetZoneSenssor";
                            commandA2.Parameters.Add(ParmA2);
                            commandA2.Parameters.Add(ParmlikeA2);
                            Ada2.SelectCommand = commandA2;
                            Ada2.Fill(Ads2);
                            for (int j = 0; j < Ads2.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Ads2.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListA2.Add(new TripInfo(false, Ads2.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone2A.DataContext = tripListA2;

                            List<TripInfo> tripListT2 = new List<TripInfo>();
                            ParmT2 = new SqlParameter("@items", SqlDbType.Int);
                            ParmT2.Value = TempInt[i];
                            ParmlikeT2.Value = "T";
                            SqlCommand commandT2 = new SqlCommand();
                            commandT2.Connection = connection;
                            commandT2.CommandType = CommandType.StoredProcedure;
                            commandT2.CommandText = "sp_GetZoneSenssor";
                            commandT2.Parameters.Add(ParmT2);
                            commandT2.Parameters.Add(ParmlikeT2);
                            Tda2.SelectCommand = commandT2;
                            Tda2.Fill(Tds2);
                            for (int j = 0; j < Tds2.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Tds2.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListT2.Add(new TripInfo(false, Tds2.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone2T.DataContext = tripListT2;
                            break;

                        case 3:

                            List<TripInfo> tripList3 = new List<TripInfo>();
                            SqlParameter Parm3, ParmlikeS3, ParmA3, ParmlikeA3, ParmT3, ParmlikeT3, ParmD3, Parmliked3;
                            DataSet ds3, Ads3, Tds3, Dds3;
                            SqlDataAdapter da3, Ada3, Tda3, Dda3;
                            Parm3 = new SqlParameter("@items", SqlDbType.Int);
                            ParmlikeS3 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeA3 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeT3 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            Parmliked3 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ds3 = new DataSet();
                            Ads3 = new DataSet();
                            Tds3 = new DataSet();
                            Dds3 = new DataSet();
                            da3 = new SqlDataAdapter();
                            Ada3 = new SqlDataAdapter();
                            Tda3 = new SqlDataAdapter();
                            Dda3 = new SqlDataAdapter();
                            Parm3 = new SqlParameter("@items", SqlDbType.Int);
                            Parm3.Value = TempInt[i];
                            ParmlikeS3.Value = "S";
                            SqlCommand command3 = new SqlCommand();
                            command3.Connection = connection;
                            command3.CommandType = CommandType.StoredProcedure;
                            command3.CommandText = "sp_GetZoneSenssor";
                            Parm3.Value = TempInt[i];
                            command3.Parameters.Add(Parm3);
                            command3.Parameters.Add(ParmlikeS3);
                            da3.SelectCommand = command3;
                            da3.Fill(ds3, "SensorZones");
                            for (int j = 0; j < ds3.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds3.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList3.Add(new TripInfo(false, ds3.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone3.DataContext = tripList3;

                            List<TripInfo> tripListA3 = new List<TripInfo>();
                            ParmA3 = new SqlParameter("@items", SqlDbType.Int);
                            ParmA3.Value = TempInt[i];
                            ParmlikeA3.Value = "A";
                            SqlCommand commandA3 = new SqlCommand();
                            commandA3.Connection = connection;
                            commandA3.CommandType = CommandType.StoredProcedure;
                            commandA3.CommandText = "sp_GetZoneSenssor";
                            commandA3.Parameters.Add(ParmA3);
                            commandA3.Parameters.Add(ParmlikeA3);
                            Ada3.SelectCommand = commandA3;
                            Ada3.Fill(Ads3);
                            for (int j = 0; j < Ads3.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Ads3.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListA3.Add(new TripInfo(false, Ads3.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone3A.DataContext = tripListA3;

                            List<TripInfo> tripListT3 = new List<TripInfo>();
                            ParmT3 = new SqlParameter("@items", SqlDbType.Int);
                            ParmT3.Value = TempInt[i];
                            ParmlikeT3.Value = "T";
                            SqlCommand commandT3 = new SqlCommand();
                            commandT3.Connection = connection;
                            commandT3.CommandType = CommandType.StoredProcedure;
                            commandT3.CommandText = "sp_GetZoneSenssor";
                            commandT3.Parameters.Add(ParmT3);
                            commandT3.Parameters.Add(ParmlikeT3);
                            Tda3.SelectCommand = commandT3;
                            Tda3.Fill(Tds3);
                            for (int j = 0; j < Tds3.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Tds3.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListT3.Add(new TripInfo(false, Tds3.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone3T.DataContext = tripListT3;
                            break;
                        case 4:

                            List<TripInfo> tripList4 = new List<TripInfo>();
                            SqlParameter Parm4, ParmlikeS4, ParmA4, ParmlikeA4, ParmT4, ParmlikeT4, ParmD4, Parmliked4;
                            DataSet ds4, Ads4, Tds4, Dds4;
                            SqlDataAdapter da4, Ada4, Tda4, Dda4;
                            Parm4 = new SqlParameter("@items", SqlDbType.Int);
                            ParmlikeS4 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeA4 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeT4 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            Parmliked4 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ds4 = new DataSet();
                            Ads4 = new DataSet();
                            Tds4 = new DataSet();
                            Dds4 = new DataSet();
                            da4 = new SqlDataAdapter();
                            Ada4 = new SqlDataAdapter();
                            Tda4 = new SqlDataAdapter();
                            Dda4 = new SqlDataAdapter();
                            Parm4.Value = TempInt[i];
                            ParmlikeS4.Value = "S";
                            SqlCommand command4 = new SqlCommand();
                            command4.Connection = connection;
                            command4.CommandType = CommandType.StoredProcedure;
                            command4.CommandText = "sp_GetZoneSenssor";
                            Parm4.Value = TempInt[i];
                            command4.Parameters.Add(Parm4);
                            command4.Parameters.Add(ParmlikeS4);
                            da4.SelectCommand = command4;
                            da4.Fill(ds4, "SensorZones");
                            for (int j = 0; j < ds4.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds4.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList4.Add(new TripInfo(false, ds4.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone4.DataContext = tripList4;

                            List<TripInfo> tripListA4 = new List<TripInfo>();
                            ParmA4 = new SqlParameter("@items", SqlDbType.Int);
                            ParmA4.Value = TempInt[i];
                            ParmlikeA4.Value = "A";
                            SqlCommand commandA4 = new SqlCommand();
                            commandA4.Connection = connection;
                            commandA4.CommandType = CommandType.StoredProcedure;
                            commandA4.CommandText = "sp_GetZoneSenssor";
                            commandA4.Parameters.Add(ParmA4);
                            commandA4.Parameters.Add(ParmlikeA4);
                            Ada4.SelectCommand = commandA4;
                            Ada4.Fill(Ads4);
                            for (int j = 0; j < Ads4.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Ads4.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListA4.Add(new TripInfo(false, Ads4.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone4A.DataContext = tripListA4;

                            List<TripInfo> tripListT4 = new List<TripInfo>();
                            ParmT4 = new SqlParameter("@items", SqlDbType.Int);
                            ParmT4.Value = TempInt[i];
                            ParmlikeT4.Value = "T";
                            SqlCommand commandT4 = new SqlCommand();
                            commandT4.Connection = connection;
                            commandT4.CommandType = CommandType.StoredProcedure;
                            commandT4.CommandText = "sp_GetZoneSenssor";
                            commandT4.Parameters.Add(ParmT4);
                            commandT4.Parameters.Add(ParmlikeT4);
                            Tda4.SelectCommand = commandT4;
                            Tda4.Fill(Tds4);
                            for (int j = 0; j < Tds4.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Tds4.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListT4.Add(new TripInfo(false, Tds4.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone4T.DataContext = tripListT4;

                            List<TripInfo> tripListD4 = new List<TripInfo>();
                            ParmD4 = new SqlParameter("@items", SqlDbType.Int);
                            ParmD4.Value = TempInt[i];
                            Parmliked4.Value = "D";
                            SqlCommand commandD4 = new SqlCommand();
                            commandD4.Connection = connection;
                            commandD4.CommandType = CommandType.StoredProcedure;
                            commandD4.CommandText = "sp_GetZoneSenssor";
                            commandD4.Parameters.Add(ParmD4);
                            commandD4.Parameters.Add(Parmliked4);
                            Dda4.SelectCommand = commandD4;
                            Dda4.Fill(Dds4);
                            for (int j = 0; j < Dds4.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Dds4.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListD4.Add(new TripInfo(false, Dds4.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone4D.DataContext = tripListD4;
                            break;
                        case 5:

                            List<TripInfo> tripList5 = new List<TripInfo>();
                            SqlParameter Parm5, ParmlikeS5, ParmA5, ParmlikeA5;
                            DataSet ds5, Ads5;
                            SqlDataAdapter da5, Ada5;
                            Parm5 = new SqlParameter("@items", SqlDbType.Int);
                            ParmlikeS5 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeA5 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ds5 = new DataSet();
                            Ads5 = new DataSet();
                            da5 = new SqlDataAdapter();
                            Ada5 = new SqlDataAdapter();
                            SqlCommand command5 = new SqlCommand();
                            command5.Connection = connection;
                            command5.CommandType = CommandType.StoredProcedure;
                            command5.CommandText = "sp_GetZoneSenssor";
                            Parm5.Value = TempInt[i];
                            ParmlikeS5.Value = "S";
                            command5.Parameters.Add(Parm5);
                            command5.Parameters.Add(ParmlikeS5);
                            da5.SelectCommand = command5;
                            da5.Fill(ds5, "SensorZones");
                            for (int j = 0; j < ds5.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds5.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList5.Add(new TripInfo(false, ds5.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone5.DataContext = tripList5;

                            List<TripInfo> tripListA5 = new List<TripInfo>();
                            ParmA5 = new SqlParameter("@items", SqlDbType.Int);
                            ParmA5.Value = TempInt[i];
                            ParmlikeA5.Value = "A";
                            SqlCommand commandA5 = new SqlCommand();
                            commandA5.Connection = connection;
                            commandA5.CommandType = CommandType.StoredProcedure;
                            commandA5.CommandText = "sp_GetZoneSenssor";
                            commandA5.Parameters.Add(ParmA5);
                            commandA5.Parameters.Add(ParmlikeA5);
                            Ada5.SelectCommand = commandA5;
                            Ada5.Fill(Ads5);
                            for (int j = 0; j < Ads5.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Ads5.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListA5.Add(new TripInfo(false, Ads5.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone5A.DataContext = tripListA5;
                            break;
                        case 6:

                            List<TripInfo> tripList6 = new List<TripInfo>();
                            SqlParameter Parm6, ParmlikeS6, ParmA6, ParmlikeA6;
                            DataSet ds6, Ads6;
                            SqlDataAdapter da6, Ada6;
                            Parm6 = new SqlParameter("@items", SqlDbType.Int);
                            ParmlikeS6 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ParmlikeA6 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            ds6 = new DataSet();
                            Ads6 = new DataSet();
                            da6 = new SqlDataAdapter();
                            Ada6 = new SqlDataAdapter();
                            SqlCommand command6 = new SqlCommand();
                            command6.Connection = connection;
                            command6.CommandType = CommandType.StoredProcedure;
                            command6.CommandText = "sp_GetZoneSenssor";
                            Parm6.Value = TempInt[i];
                            ParmlikeS6.Value = "S";
                            command6.Parameters.Add(Parm6);
                            command6.Parameters.Add(ParmlikeS6);
                            da6.SelectCommand = command6;
                            da6.Fill(ds6, "SensorZones");
                            for (int j = 0; j < ds6.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds6.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList6.Add(new TripInfo(false, ds6.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone6.DataContext = tripList6;

                            List<TripInfo> tripListA6 = new List<TripInfo>();
                            ParmA6 = new SqlParameter("@items", SqlDbType.Int);
                            ParmA6.Value = TempInt[i];
                            ParmlikeA6.Value = "A";
                            SqlCommand commandA6 = new SqlCommand();
                            commandA6.Connection = connection;
                            commandA6.CommandType = CommandType.StoredProcedure;
                            commandA6.CommandText = "sp_GetZoneSenssor";
                            commandA6.Parameters.Add(ParmA6);
                            commandA6.Parameters.Add(ParmlikeA6);
                            Ada6.SelectCommand = commandA6;
                            Ada6.Fill(Ads6);
                            for (int j = 0; j < Ads6.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, Ads6.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripListA6.Add(new TripInfo(false, Ads6.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone6A.DataContext = tripListA6;
                            break;
                        case 7:

                            List<TripInfo> tripList7 = new List<TripInfo>();
                            SqlParameter Parm7 = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter ParmlikeA7 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds7 = new DataSet();
                            SqlDataAdapter da7 = new SqlDataAdapter();
                            SqlCommand command7 = new SqlCommand();
                            command7.Connection = connection;
                            command7.CommandType = CommandType.StoredProcedure;
                            command7.CommandText = "sp_GetZoneSenssor";
                            Parm7.Value = TempInt[i];
                            ParmlikeA7.Value = "A";
                            command7.Parameters.Add(Parm7);
                            command7.Parameters.Add(ParmlikeA7);
                            da7.SelectCommand = command7;
                            da7.Fill(ds7, "SensorZones");
                            for (int j = 0; j < ds7.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds7.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList7.Add(new TripInfo(false, ds7.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone7.DataContext = tripList7;
                            break;
                        case 8:

                            List<TripInfo> tripList8 = new List<TripInfo>();
                            SqlParameter Parm8 = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter ParmlikeS8 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds8 = new DataSet();
                            SqlDataAdapter da8 = new SqlDataAdapter();
                            SqlCommand command8 = new SqlCommand();
                            command8.Connection = connection;
                            command8.CommandType = CommandType.StoredProcedure;
                            command8.CommandText = "sp_GetZoneSenssor";
                            Parm8.Value = TempInt[i];
                            ParmlikeS8.Value = "S";
                            command8.Parameters.Add(Parm8);
                            command8.Parameters.Add(ParmlikeS8);
                            da8.SelectCommand = command8;
                            da8.Fill(ds8, "SensorZones");
                            for (int j = 0; j < ds8.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds8.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList8.Add(new TripInfo(false, ds8.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone8.DataContext = tripList8;

                            List<TripInfo> tripList8D = new List<TripInfo>();
                            SqlParameter Parm8D = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter ParmlikeD8 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds8D = new DataSet();
                            SqlDataAdapter da8D = new SqlDataAdapter();
                            SqlCommand command8D = new SqlCommand();
                            command8D.Connection = connection;
                            command8D.CommandType = CommandType.StoredProcedure;
                            command8D.CommandText = "sp_GetZoneSenssor";
                            Parm8D.Value = TempInt[i];
                            ParmlikeD8.Value = "D";
                            command8D.Parameters.Add(Parm8D);
                            command8D.Parameters.Add(ParmlikeD8);
                            da8D.SelectCommand = command8D;
                            da8D.Fill(ds8D, "SensorZones");
                            for (int j = 0; j < ds8D.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds8D.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList8D.Add(new TripInfo(false, ds8D.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone8D.DataContext = tripList8D;
                            break;
                        case 9:

                            List<TripInfo> tripList9 = new List<TripInfo>();
                            SqlParameter Parm9 = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter ParmlikeP9 = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds9 = new DataSet();
                            SqlDataAdapter da9 = new SqlDataAdapter();
                            SqlCommand command9 = new SqlCommand();
                            command9.Connection = connection;
                            command9.CommandType = CommandType.StoredProcedure;
                            command9.CommandText = "sp_GetZoneSenssor";
                            Parm9.Value = TempInt[i];
                            ParmlikeP9.Value = "P";
                            command9.Parameters.Add(Parm9);
                            command9.Parameters.Add(ParmlikeP9);
                            da9.SelectCommand = command9;
                            da9.Fill(ds9, "SensorZones");
                            for (int j = 0; j < ds9.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds9.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList9.Add(new TripInfo(false, ds9.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone9.DataContext = tripList9;

                            List<TripInfo> tripList9D = new List<TripInfo>();
                            SqlParameter Parm9D = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter Parmlike9D = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds9D = new DataSet();
                            SqlDataAdapter da9D = new SqlDataAdapter();
                            SqlCommand command9D = new SqlCommand();
                            command9D.Connection = connection;
                            command9D.CommandType = CommandType.StoredProcedure;
                            command9D.CommandText = "sp_GetZoneSenssor";
                            Parm9D.Value = TempInt[i];
                            Parmlike9D.Value = "D";
                            command9D.Parameters.Add(Parm9D);
                            command9D.Parameters.Add(Parmlike9D);
                            da9D.SelectCommand = command9D;
                            da9D.Fill(ds9D, "SensorZones");
                            for (int j = 0; j < ds9D.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds9D.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList9D.Add(new TripInfo(false, ds9D.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone9D.DataContext = tripList9D;
                            break;
                        case 10:

                            List<TripInfo> tripList10 = new List<TripInfo>();
                            SqlParameter Parm10 = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter Parmlike10S = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds10 = new DataSet();
                            SqlDataAdapter da10 = new SqlDataAdapter();
                            SqlCommand command10 = new SqlCommand();
                            command10.Connection = connection;
                            command10.CommandType = CommandType.StoredProcedure;
                            command10.CommandText = "sp_GetZoneSenssor";
                            Parm10.Value = TempInt[i];
                            Parmlike10S.Value = "S";
                            command10.Parameters.Add(Parm10);
                            command10.Parameters.Add(Parmlike10S);
                            da10.SelectCommand = command10;
                            da10.Fill(ds10, "SensorZones");
                            for (int j = 0; j < ds10.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds10.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList10.Add(new TripInfo(false, ds10.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone10.DataContext = tripList10;

                            List<TripInfo> tripList10D = new List<TripInfo>();
                            SqlParameter Parm10D = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter Parmlike10D = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds10D = new DataSet();
                            SqlDataAdapter da10D = new SqlDataAdapter();
                            SqlCommand command10D = new SqlCommand();
                            command10D.Connection = connection;
                            command10D.CommandType = CommandType.StoredProcedure;
                            command10D.CommandText = "sp_GetZoneSenssor";
                            Parm10D.Value = TempInt[i];
                            Parmlike10D.Value = "D";
                            command10D.Parameters.Add(Parm10D);
                            command10D.Parameters.Add(Parmlike10D);
                            da10D.SelectCommand = command10D;
                            da10D.Fill(ds10D, "SensorZones");
                            for (int j = 0; j < ds10D.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds10D.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList10D.Add(new TripInfo(false, ds10D.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone10D.DataContext = tripList10D;

                            List<TripInfo> tripList10P = new List<TripInfo>();
                            SqlParameter Parm10P = new SqlParameter("@items", SqlDbType.Int);
                            SqlParameter Parmlike10P = new SqlParameter("@likeItems", SqlDbType.VarChar);
                            DataSet ds10P = new DataSet();
                            SqlDataAdapter da10P = new SqlDataAdapter();
                            SqlCommand command10P = new SqlCommand();
                            command10P.Connection = connection;
                            command10P.CommandType = CommandType.StoredProcedure;
                            command10P.CommandText = "sp_GetZoneSenssor";
                            Parm10P.Value = TempInt[i];
                            Parmlike10P.Value = "P";
                            command10P.Parameters.Add(Parm10P);
                            command10P.Parameters.Add(Parmlike10P);
                            da10P.SelectCommand = command10P;
                            da10P.Fill(ds10P, "SensorZones");
                            for (int j = 0; j < ds10P.Tables[0].Rows.Count; j++)
                            {
                                tripList.Add(new TripInfo(false, ds10P.Tables[0].Rows[j]["Sensors"].ToString()));
                                tripList10P.Add(new TripInfo(false, ds10P.Tables[0].Rows[j]["Sensors"].ToString()));
                            }
                            lstZone10P.DataContext = tripList10P;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void dtpStartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void dtpEndDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CheckBoxs1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lst2.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {

                            }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lst2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst = (ListView)sender;
            lst.SelectedItem = null;
        }

        private void lstZone1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst1 = (ListView)sender;
            lst1.SelectedItem = null;

        }

        private void CheckBoxZone1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone1.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {

                            }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst3 = (ListView)sender;
            lst3.SelectedItem = null;
        }

        private void CheckBoxZone3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone3.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst4 = (ListView)sender;
            lst4.SelectedItem = null;
        }

        private void CheckBoxZone4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone4.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone5_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst5 = (ListView)sender;
            lst5.SelectedItem = null;
        }

        private void CheckBoxZone5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone5.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone6_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst6 = (ListView)sender;
            lst6.SelectedItem = null;
        }

        private void CheckBoxZone6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone6.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone7_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst7 = (ListView)sender;
            lst7.SelectedItem = null;
        }

        private void CheckBoxZone7_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone7.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone8_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst8 = (ListView)sender;
            lst8.SelectedItem = null;
        }

        private void CheckBoxZone8_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone8.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone9_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst9 = (ListView)sender;
            lst9.SelectedItem = null;
        }

        private void CheckBoxZone9_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone9.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone10_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst10 = (ListView)sender;
            lst10.SelectedItem = null;
        }

        private void CheckBoxZone10_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone10.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxs1_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lst2.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }

        }

        private void CheckBoxZone1_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone1.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone3_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone3.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone4_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone4.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone5_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone5.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone6_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone6.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone7_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone7.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone8_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone8.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }
        private void lstZone8D_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lst8D = (ListView)sender;
            lst8D.SelectedItem = null;
        }

        private void CheckBoxZone8D_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone8D.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone8D_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone8D.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone9_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone9.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone10_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone10.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void NUDTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                NUDButtonUP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { true });
            }


            if (e.Key == Key.Down)
            {
                NUDButtonDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { true });
            }
        }

        private void NUDTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { false });

            if (e.Key == Key.Down)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { false });
        }

        private void NUDTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (NUDTextBox.Text != "")
                if (!int.TryParse(NUDTextBox.Text, out number)) NUDTextBox.Text = startvalue.ToString();
            if (number > maxvalue) NUDTextBox.Text = maxvalue.ToString();
            if (number < MinValue) NUDTextBox.Text = MinValue.ToString();
            NUDTextBox.SelectionStart = NUDTextBox.Text.Length;
            if (NUDTextBox.Text == string.Empty) { NUDTextBox.Text = startvalue.ToString(); }
        }

        private void NUDButtonUP_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (NUDTextBox.Text != "") number = Convert.ToInt32(NUDTextBox.Text);
            else number = 0;
            if (number < maxvalue)
                NUDTextBox.Text = Convert.ToString(number + 1);
        }

        private void NUDButtonDown_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (NUDTextBox.Text != "") number = Convert.ToInt32(NUDTextBox.Text);
            else number = 0;
            if (number > MinValue)
                NUDTextBox.Text = Convert.ToString(number - 1);
        }

        private void txtSensorLess_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDBtnlessUP, new object[] { false });

            if (e.Key == Key.Down)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDBtnlessDown, new object[] { false });
        }

        private void txtSensorLess_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                NUDBtnlessUP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDBtnlessUP, new object[] { true });
            }


            if (e.Key == Key.Down)
            {
                NUDBtnlessDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDBtnlessDown, new object[] { true });
            }
        }

        private void txtSensorLess_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (txtSensorLess.Text != "")
                if (!int.TryParse(txtSensorLess.Text, out number)) txtSensorLess.Text = startvalue.ToString();
            if (number > maxvalue) txtSensorLess.Text = maxvalue.ToString();
            if (number < MinValue) txtSensorLess.Text = MinValue.ToString();
            txtSensorLess.SelectionStart = txtSensorLess.Text.Length;
            if (txtSensorLess.Text == string.Empty) { txtSensorLess.Text = Senssorvalue.ToString(); }
        }

        private void NUDBtnlessUP_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtSensorLess.Text != "") number = Convert.ToInt32(txtSensorLess.Text);
            else number = 0;
            if (number < maxvalue)
                txtSensorLess.Text = Convert.ToString(number + 1);
        }

        private void NUDBtnlessDown_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtSensorLess.Text != "") number = Convert.ToInt32(txtSensorLess.Text);
            else number = 0;
            if (number > MinValue)
                txtSensorLess.Text = Convert.ToString(number - 1);
        }
        private void txtStrhr_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnHrUp, new object[] { false });

            if (e.Key == Key.Down)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnHrDown, new object[] { false });
        }

        private void txtStrhr_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                BtnHrUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnHrUp, new object[] { true });
            }


            if (e.Key == Key.Down)
            {
                BtnHrDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnHrDown, new object[] { true });
            }
        }

        private void txtStrhr_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (txtStrhr.Text != "")
                if (!int.TryParse(txtStrhr.Text, out number)) txtStrhr.Text = startvalue.ToString();
            if (number > MaxHour) txtStrhr.Text = MaxHour.ToString();
            if (number < minvalue) txtStrhr.Text = minvalue.ToString();
            txtStrhr.SelectionStart = txtStrhr.Text.Length;
            Starthour = txtStrhr.Text;
            if (Starthour.Length == 1 & txtStrhr.Text != "0") { Starthour = "0" + Starthour; txtStrhr.Text = Starthour; }
            else if (Starthour.Length == 1 & txtStrhr.Text == "0") { Starthour = "0" + "1"; txtStrhr.Text = Starthour; }
            else { Starthour = txtStrhr.Text; }
        }

        private void BtnHrUp_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtStrhr.Text != "") number = Convert.ToInt32(txtStrhr.Text);
            else number = 0;
            if (number < MaxHour)
                txtStrhr.Text = Convert.ToString(number + 1);
        }

        private void BtnHrDown_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtStrhr.Text != "") number = Convert.ToInt32(txtStrhr.Text);
            else number = 0;
            if (number > minvalue)
                txtStrhr.Text = Convert.ToString(number - 1);
        }

        private void txtStrMin_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnMinuteUp, new object[] { false });

            if (e.Key == Key.Down)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnMinuteDown, new object[] { false });
        }

        private void txtStrMin_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                BtnMinuteUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnMinuteUp, new object[] { true });
            }


            if (e.Key == Key.Down)
            {
                BtnMinuteDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnMinuteDown, new object[] { true });
            }
        }

        private void txtStrMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (txtStrMin.Text != "")
                if (!int.TryParse(txtStrMin.Text, out number)) txtStrMin.Text = startvalue.ToString();
            if (number > MaxMinute) txtStrMin.Text = MaxMinute.ToString();
            if (number < minvalue) txtStrMin.Text = minvalue.ToString();
            txtStrMin.SelectionStart = txtStrMin.Text.Length;
            StartMinute = txtStrMin.Text;
            if (StartMinute.Length == 1) { StartMinute = "0" + StartMinute; txtStrMin.Text = StartMinute; }
            else { StartMinute = txtStrMin.Text; }
        }

        private void BtnMinuteUp_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtStrMin.Text != "") number = Convert.ToInt32(txtStrMin.Text);
            else number = 0;
            if (number < MaxMinute)
                txtStrMin.Text = Convert.ToString(number + 1);
        }

        private void BtnMinuteDown_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtStrMin.Text != "") number = Convert.ToInt32(txtStrMin.Text);
            else number = 0;
            if (number > minvalue)
                txtStrMin.Text = Convert.ToString(number - 1);
        }

        private void txtEndhr_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndHrUp, new object[] { false });

            if (e.Key == Key.Down)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndHrDown, new object[] { false });
        }

        private void txtEndhr_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                BtnEndHrUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndHrUp, new object[] { true });
            }


            if (e.Key == Key.Down)
            {
                BtnEndHrDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndHrDown, new object[] { true });
            }
        }

        private void txtEndhr_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (txtEndhr.Text != "")
                if (!int.TryParse(txtEndhr.Text, out number)) txtEndhr.Text = startvalue.ToString();
            if (number > MaxHour) txtEndhr.Text = MaxHour.ToString();
            if (number < minvalue) txtEndhr.Text = minvalue.ToString();
            txtEndhr.SelectionStart = txtEndhr.Text.Length;
            EndHour = txtEndhr.Text;
            if (EndHour.Length == 1 & txtEndhr.Text != "0") { EndHour = "0" + EndHour; txtEndhr.Text = EndHour; }
            else if (EndHour.Length == 1 & txtEndhr.Text == "0") { EndHour = "0" + "1"; txtEndhr.Text = EndHour; }
            else { EndHour = txtEndhr.Text; }
        }

        private void BtnEndHrUp_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtEndhr.Text != "") number = Convert.ToInt32(txtEndhr.Text);
            else number = 0;
            if (number < MaxHour)
                txtEndhr.Text = Convert.ToString(number + 1);
        }

        private void BtnEndHrDown_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtEndhr.Text != "") number = Convert.ToInt32(txtEndhr.Text);
            else number = 0;
            if (number > minvalue)
                txtEndhr.Text = Convert.ToString(number - 1);
        }

        private void txtEndMinute_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                BtnEndMinUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndMinUp, new object[] { true });
            }


            if (e.Key == Key.Down)
            {
                BtnEndMinDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndMinDown, new object[] { true });
            }
        }

        private void txtEndMinute_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndMinUp, new object[] { false });

            if (e.Key == Key.Down)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(BtnEndMinDown, new object[] { false });
        }

        private void txtEndMinute_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (txtEndMinute.Text != "")
                if (!int.TryParse(txtStrMin.Text, out number)) txtEndMinute.Text = startvalue.ToString();
            if (number > MaxMinute) txtEndMinute.Text = MaxMinute.ToString();
            if (number < minvalue) txtEndMinute.Text = minvalue.ToString();
            txtEndMinute.SelectionStart = txtEndMinute.Text.Length;
            EndMinute = txtEndMinute.Text;
            if (EndMinute.Length == 1) { EndMinute = "0" + EndMinute; txtEndMinute.Text = EndMinute; }
            else { EndMinute = txtEndMinute.Text; }
        }

        private void BtnEndMinUp_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtEndMinute.Text != "") number = Convert.ToInt32(txtEndMinute.Text);
            else number = 0;
            if (number < MaxMinute)
                txtEndMinute.Text = Convert.ToString(number + 1);

        }

        private void BtnEndMinDown_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (txtEndMinute.Text != "") number = Convert.ToInt32(txtEndMinute.Text);
            else number = 0;
            if (number > minvalue)
                txtEndMinute.Text = Convert.ToString(number - 1);

        }

        private void lstZone1A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstA = (ListView)sender;
            lstA.SelectedItem = null;
        }
        private void CheckBoxZone1A_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone1A.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone1A_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone1A.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone1T_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstT = (ListView)sender;
            lstT.SelectedItem = null;
        }

        private void CheckBoxZone1T_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone1T.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone1T_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone1T.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone1D1_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            ListView lstD = (ListView)sender;
            lstD.SelectedItem = null;
        }
        private void CheckBoxZone1D1_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone1D1.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }
        private void CheckBoxZone1D1_Unchecked_1(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone1D1.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone1);
                listChkboxobj.Add("CheckBoxZone1");
                CheckSelectALL(lstZone1, null, null, "CheckBoxZone1", "", "");
            }
            catch (Exception ex) { throw ex; }
        }
        private void chkSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                UncheckBindlist(lstZone1, 1, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lstZone1);
                listChkboxobj.Remove("CheckBoxZone1");
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }
        private ChildControl FindVisualChild<ChildControl>(DependencyObject DependencyObj)
        where ChildControl : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(DependencyObj);
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(DependencyObj); i++)
            {
                DependencyObject Child = VisualTreeHelper.GetChild(DependencyObj, i);

                if (Child != null && Child is ChildControl)
                {
                    return (ChildControl)Child;
                }
                else
                {
                    ChildControl ChildOfChild = FindVisualChild<ChildControl>(Child);

                    if (ChildOfChild != null)
                    {
                        return ChildOfChild;
                    }
                }
            }
            return null;
        }

        private void lstZone2A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN2A = (ListView)sender;
            lstZN2A.SelectedItem = null;
        }

        private void ChkZone2A_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone2A.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void ChkZone2A_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone2A.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone2T_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN2T = (ListView)sender;
            lstZN2T.SelectedItem = null;
        }

        private void ChkZone2T_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone2T.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void ChkZone2T_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone2T.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone2SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lst2);
                listChkboxobj.Add("CheckBoxs1");
                CheckSelectALL(lst2, null, null, "CheckBoxs1", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone2SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                UncheckBindlist(lst2, 2, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lst2);
                listChkboxobj.Remove("CheckBoxs1");
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone3A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN3A = (ListView)sender;
            lstZN3A.SelectedItem = null;
        }

        private void CheckBoxZone3A_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone3A.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone3A_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone3A.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone3T_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN3T = (ListView)sender;
            lstZN3T.SelectedItem = null;
        }

        private void CheckBoxZone3T_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone3T.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone3T_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone3T.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone4A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN4A = (ListView)sender;
            lstZN4A.SelectedItem = null;
        }

        private void CheckBoxZone4A_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone4A.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone4A_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone4A.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }
        private void lstZone4T_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN4T = (ListView)sender;
            lstZN4T.SelectedItem = null;
        }
        private void CheckBoxZone4T_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone4T.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone4T_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone4T.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone4D_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN4D = (ListView)sender;
            lstZN4D.SelectedItem = null;
        }

        private void CheckBoxZone4D_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone4D.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone4D_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone4D.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone5A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN5A = (ListView)sender;
            lstZN5A.SelectedItem = null;
        }

        private void lstZone6A_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN6A = (ListView)sender;
            lstZN6A.SelectedItem = null;
        }

        private void CheckBoxZone6A_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone6A.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone5A_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone5A.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone5A_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone5A.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone6A_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone6A.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone9D_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN9D = (ListView)sender;
            lstZN9D.SelectedItem = null;
        }

        private void CheckBoxZone9D_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone9D.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone9D_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone9D.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone10D_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN10D = (ListView)sender;
            lstZN10D.SelectedItem = null;
        }

        private void CheckBoxZone10D_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone10D.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone10D_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone10D.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void lstZone10P_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView lstZN10P = (ListView)sender;
            lstZN10P.SelectedItem = null;
        }

        private void CheckBoxZone10P_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TripInfo cbObject in lstZone10P.Items)
                    if (cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                        }
                        else
                        {
                            sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                        }
                        SelectedSensser += sb.ToString().Trim();
                        sb.Clear();
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void CheckBoxZone10P_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = string.Empty;
                foreach (TripInfo cbObject in lstZone10P.Items)
                    if (!cbObject.IsSelected)
                    {
                        if (SelectedSensser != null)
                        {
                            if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                            {
                                str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                else { SelectedSensser = str; SelectedSensser = null; }
                            }
                        }
                    }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone3SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone3);
                listChkboxobj.Add("CheckBoxZone3");
                CheckSelectALL(lstZone3, null, null, "CheckBoxZone3", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone3SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone3;
                string chkList = "CheckBoxZone3";
                UncheckBindlist(lstObj, 3, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }

        }
        private void chkZone4SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone4);
                listChkboxobj.Add("CheckBoxZone4");
                CheckSelectALL(lstZone4, null, null, "CheckBoxZone4", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone4SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone4;
                string chkList = "CheckBoxZone4";
                UncheckBindlist(lstObj, 4, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone5SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone5);
                listChkboxobj.Add("CheckBoxZone5");
                CheckSelectALL(lstZone5, null, null, "CheckBoxZone5", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone5SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone5;
                string chkList = "CheckBoxZone5";
                UncheckBindlist(lstObj, 5, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone6SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone6);
                listChkboxobj.Add("CheckBoxZone6");
                CheckSelectALL(lstZone6, null, null, "CheckBoxZone6", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone6SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone6;
                string chkList = "CheckBoxZone6";
                UncheckBindlist(lstObj, 6, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone7SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone7);
                listChkboxobj.Add("CheckBoxZone7");
                CheckSelectALL(null, lstZone7, null, "", "CheckBoxZone7", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone7SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone7;
                string chkList = "CheckBoxZone7";
                UncheckBindlist(lstObj, 7, "A");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
                UncheckAllSelect(null, lstZone7, null, "", "CheckBoxZone7", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone8SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone8);
                listChkboxobj.Add("CheckBoxZone8");
                CheckSelectALL(lstZone8, null, null, "CheckBoxZone8", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone8SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone8;
                string chkList = "CheckBoxZone8";
                UncheckBindlist(lstObj, 8, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone9SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone9);
                listChkboxobj.Add("CheckBoxZone9");
                CheckSelectALL(lstZone9, null, null, "CheckBoxZone9", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone9SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone9;
                string chkList = "CheckBoxZone9";
                UncheckBindlist(lstObj, 9, "P");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone10SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone10);
                listChkboxobj.Add("CheckBoxZone10");
                CheckSelectALL(lstZone10, null, null, "CheckBoxZone10", "", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone10SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone10;
                string chkList = "CheckBoxZone10";
                UncheckBindlist(lstObj, 10, "S");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkAccelerometerZN1_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone1A);
                listChkboxobj.Add("CheckBoxZone1A");
                CheckSelectALL(null, lstZone1A, null, "", "CheckBoxZone1A", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkAccelerometerZN1_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                UncheckBindlist(lstZone1A, 1, "A");
                SelectedSensser = null;
                Checklistobj.Remove(lstZone1A);
                listChkboxobj.Remove("CheckBoxZone1A");
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkTiltALLZN1_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone1T);
                listChkboxobj.Add("CheckBoxZone1T");
                CheckSelectALL(null, null, lstZone1T, "", "", "CheckBoxZone1T");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkTiltALLZN1_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                UncheckBindlist(lstZone1T, 1, "T");
                SelectedSensser = null;
                Checklistobj.Remove(lstZone1T);
                listChkboxobj.Remove("CheckBoxZone1T");
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone2AcclrmtrAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone2A);
                listChkboxobj.Add("ChkZone2A");
                CheckSelectALL(null, lstZone2A, null, "", "ChkZone2A", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone2AcclrmtrAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                UncheckBindlist(lstZone2A, 2, "A");
                SelectedSensser = null;
                Checklistobj.Remove(lstZone2A);
                listChkboxobj.Remove("ChkZone2A");
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone2TiltAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone2T);
                listChkboxobj.Add("ChkZone2T");
                CheckSelectALL(null, null, lstZone2T, "", "", "ChkZone2T");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone2TiltAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                UncheckBindlist(lstZone2T, 2, "T");
                SelectedSensser = null;
                Checklistobj.Remove(lstZone2T);
                listChkboxobj.Remove("ChkZone2T");
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone3AcclrmtrAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone3A);
                listChkboxobj.Add("CheckBoxZone3A");
                CheckSelectALL(null, lstZone3A, null, "", "CheckBoxZone3A", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone3AcclrmtrAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone3A;
                string chkList = "CheckBoxZone3A";
                UncheckBindlist(lstObj, 3, "A");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone3tiltAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone3T);
                listChkboxobj.Add("CheckBoxZone3T");
                CheckSelectALL(null, null, lstZone3T, "", "", "CheckBoxZone3T");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone3tiltAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone3T;
                string chkList = "CheckBoxZone3T";
                UncheckBindlist(lstObj, 3, "T");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone4AcclermtrAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone4A);
                listChkboxobj.Add("CheckBoxZone4A");
                CheckSelectALL(null, lstZone4A, null, "", "CheckBoxZone4A", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone4AcclermtrAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone4A;
                string chkList = "CheckBoxZone4A";
                UncheckBindlist(lstObj, 4, "A");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone4TiltAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone4T);
                listChkboxobj.Add("CheckBoxZone4T");
                CheckSelectALL(null, null, lstZone4T, "", "", "CheckBoxZone4T");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone4TiltAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone4T;
                string chkList = "CheckBoxZone4T";
                UncheckBindlist(lstObj, 4, "T");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone5AcclrtmtrAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone5A);
                listChkboxobj.Add("CheckBoxZone5A");
                CheckSelectALL(null, lstZone5A, null, "", "CheckBoxZone5A", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone5AcclrtmtrAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone5A;
                string chkList = "CheckBoxZone5A";
                UncheckBindlist(lstObj, 5, "A");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone6AcclermtrAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone6A);
                listChkboxobj.Add("CheckBoxZone6A");
                CheckSelectALL(null, lstZone6A, null, "", "CheckBoxZone6A", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone6AcclermtrAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone6A;
                string chkList = "CheckBoxZone6A";
                UncheckBindlist(lstObj, 6, "A");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone9DispAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone9D);
                listChkboxobj.Add("CheckBoxZone9D");
                CheckSelectALL(null, lstZone9D, null, "", "CheckBoxZone9D", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone9DispAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone9D;
                string chkList = "CheckBoxZone9D";
                UncheckBindlist(lstObj, 9, "D");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone10DispAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone10D);
                listChkboxobj.Add("CheckBoxZone10D");
                CheckSelectALL(null, lstZone10D, null, "", "CheckBoxZone10D", "");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone10DispAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone10D;
                string chkList = "CheckBoxZone10D";
                UncheckBindlist(lstObj, 10, "D");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone10ptypeAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone10P);
                listChkboxobj.Add("CheckBoxZone10P");
                CheckSelectALL(null, null, lstZone10P, "", "", "CheckBoxZone10P");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone10ptypeAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone10P;
                string chkList = "CheckBoxZone10P";
                UncheckBindlist(lstObj, 10, "P");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }
        private void chkDispALLZN1_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone1D1);
                listChkboxobj.Add("CheckBoxZone1D1");
                CheckSelectALL(null, null, lstZone1D1, "", "", "CheckBoxZone1D1");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkDispALLZN1_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                UncheckBindlist(lstZone1D1, 1, "D");
                SelectedSensser = null;
                Checklistobj.Remove(lstZone1D1);
                listChkboxobj.Remove("CheckBoxZone1D1");
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }
        private void chkZone4DispAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone4D);
                listChkboxobj.Add("CheckBoxZone4D");
                CheckSelectALL(null, null, lstZone4D, "", "", "CheckBoxZone4D");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone4DispAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone4D;
                string chkList = "CheckBoxZone4D";
                UncheckBindlist(lstObj, 4, "D");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }
        private void chkZone8DispAll_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Checklistobj.Add(lstZone8D);
                listChkboxobj.Add("CheckBoxZone8D");
                CheckSelectALL(null, null, lstZone8D, "", "", "CheckBoxZone8D");
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkZone8DispAll_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ListView lstObj = lstZone8D;
                string chkList = "CheckBoxZone8D";
                UncheckBindlist(lstObj, 8, "D");
                SelectedSensser = null;
                Checklistobj.Remove(lstObj);
                listChkboxobj.Remove(chkList);
                string[] setCheckbox = (string[])listChkboxobj.ToArray(typeof(string));
                for (int i = 0; i < Checklistobj.Count; i++)
                {
                    ListView objlistitem = (ListView)Checklistobj[i];
                    string objCheckbox = setCheckbox[i];
                    CheckSelectALL(objlistitem, null, null, objCheckbox, "", "");
                }
            }
            catch (Exception ex) { throw ex; }
        }
        public void CheckSelectALL(ListView LstvwS, ListView LstvwA, ListView LstvwT, string ChkboxNMS, string ChkboxNMA, string ChkboxNMT)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                if (LstvwS != null)
                {
                    for (int i = 0; i < LstvwS.Items.Count; i++)
                    {
                        // Get a all list items from listbox
                        ListViewItem lstZone3ItemObj = (ListViewItem)LstvwS.ItemContainerGenerator.ContainerFromItem(LstvwS.Items[i]);
                        if (lstZone3ItemObj != null)
                        {
                            //bool check = ListBoxItemObj.HasContent;
                            // find a ContentPresenter of that list item.. [Call FindVisualChild Method]
                            ContentPresenter ContentPresenterlstZone3Obj = FindVisualChild<ContentPresenter>(lstZone3ItemObj);

                            // call FindName on the DataTemplate of that ContentPresenter
                            DataTemplate lstZone3DataTemplateObj = ContentPresenterlstZone3Obj.ContentTemplate;
                            CheckBox Chk = (CheckBox)lstZone3DataTemplateObj.FindName(ChkboxNMS, ContentPresenterlstZone3Obj);
                            Chk.IsChecked = true;
                        }
                    }
                }
                if (LstvwA != null)
                {
                    for (int i = 0; i < LstvwA.Items.Count; i++)
                    {
                        // Get a all list items from listbox
                        ListViewItem lstZone3ItemObj = (ListViewItem)LstvwA.ItemContainerGenerator.ContainerFromItem(LstvwA.Items[i]);
                        if (lstZone3ItemObj != null)
                        {
                            //bool check = ListBoxItemObj.HasContent;
                            // find a ContentPresenter of that list item.. [Call FindVisualChild Method]
                            ContentPresenter ContentPresenterlstZone3Obj = FindVisualChild<ContentPresenter>(lstZone3ItemObj);

                            // call FindName on the DataTemplate of that ContentPresenter
                            DataTemplate lstZone3DataTemplateObj = ContentPresenterlstZone3Obj.ContentTemplate;
                            CheckBox Chk = (CheckBox)lstZone3DataTemplateObj.FindName(ChkboxNMA, ContentPresenterlstZone3Obj);
                            Chk.IsChecked = true;
                        }
                    }
                }
                if (LstvwT != null)
                {
                    for (int i = 0; i < LstvwT.Items.Count; i++)
                    {
                        // Get a all list items from listbox
                        ListViewItem lstZone3ItemObj = (ListViewItem)LstvwT.ItemContainerGenerator.ContainerFromItem(LstvwT.Items[i]);
                        if (lstZone3ItemObj != null)
                        {
                            //bool check = ListBoxItemObj.HasContent;
                            // find a ContentPresenter of that list item.. [Call FindVisualChild Method]
                            ContentPresenter ContentPresenterlstZone3Obj = FindVisualChild<ContentPresenter>(lstZone3ItemObj);

                            // call FindName on the DataTemplate of that ContentPresenter
                            DataTemplate lstZone3DataTemplateObj = ContentPresenterlstZone3Obj.ContentTemplate;
                            CheckBox Chk = (CheckBox)lstZone3DataTemplateObj.FindName(ChkboxNMT, ContentPresenterlstZone3Obj);
                            Chk.IsChecked = true;
                        }
                    }
                }
                if (LstvwS != null)
                {
                    foreach (TripInfo cbObject in LstvwS.Items)
                    {
                        cbObject.IsSelected = true;
                    }
                }
                if (LstvwA != null)
                {
                    foreach (TripInfo cbObject in LstvwA.Items)
                    {
                        cbObject.IsSelected = true;
                    }
                }
                if (LstvwT != null)
                {
                    foreach (TripInfo cbObject in LstvwT.Items)
                    {
                        cbObject.IsSelected = true;
                    }
                }
                if (LstvwS != null)
                {
                    foreach (TripInfo cbObject in LstvwS.Items)
                    {
                        if (cbObject.IsSelected)
                        {
                            if (SelectedSensser != null)
                            {
                                if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                                else
                                {
                                    sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                                }
                            }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                            SelectedSensser += sb.ToString().Trim();
                            sb.Clear();
                        }
                    }
                }
                if (LstvwA != null)
                {
                    foreach (TripInfo cbObject in LstvwA.Items)
                    {
                        if (cbObject.IsSelected)
                        {
                            if (SelectedSensser != null)
                            {
                                if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                                else
                                {
                                    sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                                }
                            }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                            SelectedSensser += sb.ToString().Trim();
                            sb.Clear();
                        }
                    }
                }
                if (LstvwT != null)
                {
                    foreach (TripInfo cbObject in LstvwT.Items)
                    {
                        if (cbObject.IsSelected)
                        {
                            if (SelectedSensser != null)
                            {
                                if (SelectedSensser.Contains(cbObject.ObjectData.ToString())) { }
                                else
                                {
                                    sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                                }
                            }
                            else
                            {
                                sb.AppendFormat("{0}, ", cbObject.ObjectData.ToString());
                            }
                            SelectedSensser += sb.ToString().Trim();
                            sb.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void UncheckAllSelect(ListView LstvwS, ListView LstvwA, ListView LstvwT, string ChkboxNMS, string ChkboxNMA, string ChkboxNMT)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                string str = string.Empty;
                if (LstvwS != null)
                {
                    for (int i = 0; i < LstvwS.Items.Count; i++)
                    {
                        // Get a all list items from listbox
                        ListViewItem lstZone3ItemObj = (ListViewItem)LstvwS.ItemContainerGenerator.ContainerFromItem(LstvwS.Items[i]);
                        if (lstZone3ItemObj != null)
                        {
                            //bool check = ListBoxItemObj.HasContent;
                            // find a ContentPresenter of that list item.. [Call FindVisualChild Method]
                            ContentPresenter ContentPresenterlstZone3Obj = FindVisualChild<ContentPresenter>(lstZone3ItemObj);

                            // call FindName on the DataTemplate of that ContentPresenter
                            DataTemplate lstZone3DataTemplateObj = ContentPresenterlstZone3Obj.ContentTemplate;
                            CheckBox Chk = (CheckBox)lstZone3DataTemplateObj.FindName(ChkboxNMS, ContentPresenterlstZone3Obj);
                            Chk.IsChecked = false;
                        }
                    }
                }
                if (LstvwA != null)
                {
                    for (int i = 0; i < LstvwA.Items.Count; i++)
                    {
                        // Get a all list items from listbox
                        ListViewItem lstZone3ItemObj = (ListViewItem)LstvwA.ItemContainerGenerator.ContainerFromItem(LstvwA.Items[i]);
                        if (lstZone3ItemObj != null)
                        {
                            //bool check = ListBoxItemObj.HasContent;
                            // find a ContentPresenter of that list item.. [Call FindVisualChild Method]
                            ContentPresenter ContentPresenterlstZone3Obj = FindVisualChild<ContentPresenter>(lstZone3ItemObj);

                            // call FindName on the DataTemplate of that ContentPresenter
                            DataTemplate lstZone3DataTemplateObj = ContentPresenterlstZone3Obj.ContentTemplate;
                            CheckBox Chk = (CheckBox)lstZone3DataTemplateObj.FindName(ChkboxNMA, ContentPresenterlstZone3Obj);
                            Chk.IsChecked = false;
                        }
                    }
                }
                if (LstvwT != null)
                {
                    for (int i = 0; i < LstvwT.Items.Count; i++)
                    {
                        // Get a all list items from listbox
                        ListViewItem lstZone3ItemObj = (ListViewItem)LstvwT.ItemContainerGenerator.ContainerFromItem(LstvwT.Items[i]);
                        if (lstZone3ItemObj != null)
                        {
                            //bool check = ListBoxItemObj.HasContent;
                            // find a ContentPresenter of that list item.. [Call FindVisualChild Method]
                            ContentPresenter ContentPresenterlstZone3Obj = FindVisualChild<ContentPresenter>(lstZone3ItemObj);

                            // call FindName on the DataTemplate of that ContentPresenter
                            DataTemplate lstZone3DataTemplateObj = ContentPresenterlstZone3Obj.ContentTemplate;
                            CheckBox Chk = (CheckBox)lstZone3DataTemplateObj.FindName(ChkboxNMT, ContentPresenterlstZone3Obj);
                            Chk.IsChecked = false;
                        }
                    }
                }
                if (LstvwS != null)
                {
                    foreach (TripInfo cbObject in LstvwS.Items)
                    {
                        cbObject.IsSelected = false;
                        if (!cbObject.IsSelected)
                        {
                            if (SelectedSensser != null)
                            {
                                if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                                {
                                    str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                    if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                    else { SelectedSensser = str; SelectedSensser = null; }
                                }
                            }
                        }
                    }
                }
                if (LstvwA != null)
                {
                    foreach (TripInfo cbObject in LstvwA.Items)
                    {
                        cbObject.IsSelected = false;
                        if (!cbObject.IsSelected)
                        {
                            if (SelectedSensser != null)
                            {
                                if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                                {
                                    str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                    if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                    else { SelectedSensser = str; SelectedSensser = null; }
                                }
                            }
                        }
                    }
                }
                if (LstvwT != null)
                {
                    foreach (TripInfo cbObject in LstvwT.Items)
                    {
                        cbObject.IsSelected = false;
                        if (!cbObject.IsSelected)
                        {
                            if (SelectedSensser != null)
                            {
                                if (SelectedSensser.Contains(cbObject.ObjectData.ToString()))
                                {
                                    str += SelectedSensser.Remove(SelectedSensser.IndexOf(cbObject.ObjectData.ToString()), cbObject.ObjectData.ToString().Length + 1).TrimEnd(',');
                                    if (str != string.Empty) { SelectedSensser = null; SelectedSensser = str + ","; }
                                    else { SelectedSensser = str; SelectedSensser = null; }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void chkAllSensorReading_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckSelectALL(lstZone1, null, null, "CheckBoxZone1", "", "");
                CheckSelectALL(null, lstZone1A, null, "", "CheckBoxZone1A", "");
                CheckSelectALL(null, null, lstZone1T, "", "", "CheckBoxZone1T");
                CheckSelectALL(null, null, lstZone1D1, "", "", "CheckBoxZone1D1");
                CheckSelectALL(lst2, null, null, "CheckBoxs1", "", "");
                CheckSelectALL(null, lstZone2A, null, "", "ChkZone2A", "");
                CheckSelectALL(null, null, lstZone2T, "", "", "ChkZone2T");
                CheckSelectALL(lstZone3, null, null, "CheckBoxZone3", "", "");
                CheckSelectALL(null, lstZone3A, null, "", "CheckBoxZone3A", "");
                CheckSelectALL(null, null, lstZone3T, "", "", "CheckBoxZone3T");
                CheckSelectALL(lstZone4, null, null, "CheckBoxZone4", "", "");
                CheckSelectALL(null, lstZone4A, null, "", "CheckBoxZone4A", "");
                CheckSelectALL(null, null, lstZone4T, "", "", "CheckBoxZone4T");
                CheckSelectALL(null, null, lstZone4D, "", "", "CheckBoxZone4D");
                CheckSelectALL(lstZone5, null, null, "CheckBoxZone5", "", "");
                CheckSelectALL(null, lstZone5A, null, "", "CheckBoxZone5A", "");
                CheckSelectALL(lstZone6, null, null, "CheckBoxZone6", "", "");
                CheckSelectALL(null, lstZone6A, null, "", "CheckBoxZone6A", "");
                CheckSelectALL(null, lstZone7, null, "", "CheckBoxZone7", "");
                CheckSelectALL(lstZone8, null, null, "CheckBoxZone8", "", "");
                CheckSelectALL(null, null, lstZone8D, "", "", "CheckBoxZone8D");
                CheckSelectALL(lstZone9, null, null, "CheckBoxZone9", "", "");
                CheckSelectALL(null, lstZone9D, null, "", "CheckBoxZone9D", "");
                CheckSelectALL(lstZone10, null, null, "CheckBoxZone10", "", "");
                CheckSelectALL(null, lstZone10D, null, "", "CheckBoxZone10D", "");
                CheckSelectALL(null, null, lstZone10P, "", "", "CheckBoxZone10P");
                chkSelectAll.IsChecked = true;
                chkAccelerometerZN1.IsChecked = true;
                chkTiltALLZN1.IsChecked = true;
                chkDispALLZN1.IsChecked = true;
                chkZone2SelectAll.IsChecked = true;
                chkZone2AcclrmtrAll.IsChecked = true;
                chkZone2TiltAll.IsChecked = true;
                chkZone3SelectAll.IsChecked = true;
                chkZone3AcclrmtrAll.IsChecked = true;
                chkZone3tiltAll.IsChecked = true;
                chkZone4SelectAll.IsChecked = true;
                chkZone4AcclermtrAll.IsChecked = true;
                chkZone4TiltAll.IsChecked = true;
                chkZone4DispAll.IsChecked = true;
                chkZone5SelectAll.IsChecked = true;
                chkZone5AcclrtmtrAll.IsChecked = true;
                chkZone6SelectAll.IsChecked = true;
                chkZone6AcclermtrAll.IsChecked = true;
                chkZone7SelectAll.IsChecked = true;
                chkZone8SelectAll.IsChecked = true;
                chkZone8DispAll.IsChecked = true;
                chkZone9SelectAll.IsChecked = true;
                chkZone9DispAll.IsChecked = true;
                chkZone10SelectAll.IsChecked = true;
                chkZone10DispAll.IsChecked = true;
                chkZone10ptypeAll.IsChecked = true;
            }
            catch (Exception ex) { throw ex; }
        }

        private void chkAllSensorReading_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedSensser = null;
                bindSensors();
                chkSelectAll.IsChecked = false;
                chkAccelerometerZN1.IsChecked = false;
                chkTiltALLZN1.IsChecked = false;
                chkDispALLZN1.IsChecked = false;
                chkZone2SelectAll.IsChecked = false;
                chkZone2AcclrmtrAll.IsChecked = false;
                chkZone2TiltAll.IsChecked = false;
                chkZone3SelectAll.IsChecked = false;
                chkZone3AcclrmtrAll.IsChecked = false;
                chkZone3tiltAll.IsChecked = false;
                chkZone4SelectAll.IsChecked = false;
                chkZone4AcclermtrAll.IsChecked = false;
                chkZone4TiltAll.IsChecked = false;
                chkZone4DispAll.IsChecked = false;
                chkZone5SelectAll.IsChecked = false;
                chkZone5AcclrtmtrAll.IsChecked = false;
                chkZone6SelectAll.IsChecked = false;
                chkZone6AcclermtrAll.IsChecked = false;
                chkZone7SelectAll.IsChecked = false;
                chkZone8SelectAll.IsChecked = false;
                chkZone8DispAll.IsChecked = false;
                chkZone9SelectAll.IsChecked = false;
                chkZone9DispAll.IsChecked = false;
                chkZone10SelectAll.IsChecked = false;
                chkZone10DispAll.IsChecked = false;
                chkZone10ptypeAll.IsChecked = false;
            }
            catch (Exception ex) { throw ex; }
        }
        public void UncheckBindlist(ListView objlstview, int zoneid, string senssortype)
        {
            try
            {
                List<TripInfo> objtripList = new List<TripInfo>();
                SqlParameter objParm = new SqlParameter("@items", SqlDbType.Int);
                SqlParameter objParmlike = new SqlParameter("@likeItems", SqlDbType.VarChar);
                DataSet objds = new DataSet();
                SqlDataAdapter objda = new SqlDataAdapter();
                SqlCommand objcommand = new SqlCommand();
                objcommand.Connection = connection;
                objcommand.CommandType = CommandType.StoredProcedure;
                objcommand.CommandText = "sp_GetZoneSenssor";
                objParm.Value = zoneid;
                objParmlike.Value = senssortype;
                objcommand.Parameters.Add(objParm);
                objcommand.Parameters.Add(objParmlike);
                objda.SelectCommand = objcommand;
                objda.Fill(objds, "SensorZones");
                for (int j = 0; j < objds.Tables[0].Rows.Count; j++)
                {
                    tripList.Add(new TripInfo(false, objds.Tables[0].Rows[j]["Sensors"].ToString()));
                    objtripList.Add(new TripInfo(false, objds.Tables[0].Rows[j]["Sensors"].ToString()));
                }
                objlstview.DataContext = objtripList;
            }
            catch (Exception ex) { throw ex; }
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedSensser != null)
                {
                    if (Reportingquery != null)
                    {
                        tabControl1.SelectedIndex = 2;
                        if (!_isReportViewerLoaded)
                        {
                            this._reportViewer.Reset();
                            ReportDataSource reportDataSource1;
                            Tbl_SensorDataSet dataset;
                            string query = Reportingquery;
                            dataset = new Tbl_SensorDataSet();
                            reportDataSource1 = new ReportDataSource();
                            Tbl_SensorDataSetTableAdapters.GetFilterReadingTableAdapter TableAdapter1 = new Tbl_SensorDataSetTableAdapters.GetFilterReadingTableAdapter();
                            TableAdapter1.ClearBeforeFill = true;
                            TableAdapter1.Fill(dataset.GetFilterReading, query);
                            dataset.BeginInit();
                            reportDataSource1.Name = "DataSet1";
                            reportDataSource1.Value = dataset.GetFilterReading;
                            this._reportViewer.LocalReport.DataSources.Add(reportDataSource1);
                            this._reportViewer.LocalReport.ReportEmbeddedResource = "IntelliOpticsReport.SensorReport.rdlc";
                            dataset.EndInit();
                            _reportViewer.RefreshReport();
                            _isReportViewerLoaded = false;
                            SearchSensorvalue = string.Empty;
                            txtFind.Text = string.Empty;
                        }
                    }
                    else
                    {
                        tabControl1.SelectedIndex = 0;
                        MessageBox.Show("Please Select Start Date And End Date"); return;
                    }
                }
                else { MessageBox.Show("Please Select Atleast One Optical Senssor of Any Zone"); return; }
            }
            catch (Exception ex) { throw ex; }
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (tabControl1.SelectedIndex == 2) { scrollMain.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled; }
                else
                {
                    scrollMain.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void btnFind_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedSensser != null)
                {
                    SearchSensorvalue = "btnfind";
                    BindDsGridGrap(ds, Starttime, Endtime, Strgreater, Strless, strStartDate, strEndDate);
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedSensser != null)
                {
                    SearchSensorvalue = "btnfind";
                    if (ds.Tables[0].Rows.Count % paging_NoOfRecPerPage == 0)
                    {
                        if (paging_PageIndex < ds.Tables[0].Rows.Count / paging_NoOfRecPerPage)
                        {
                            paging_PageIndex += 1;
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                        else if (paging_PageIndex == ds.Tables[0].Rows.Count / paging_NoOfRecPerPage)
                        {
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                    }
                    else
                    {
                        if (paging_PageIndex < (ds.Tables[0].Rows.Count / paging_NoOfRecPerPage))
                        {
                            paging_PageIndex += 1;
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                        else if (paging_PageIndex == (ds.Tables[0].Rows.Count / paging_NoOfRecPerPage))
                        {
                            CustomPaging(ds, (int)PagingMode.Next, dtgSensorReadingList);
                        }
                    }
                }

            }
            catch (Exception ex) { throw ex; }
        }
    }
}
