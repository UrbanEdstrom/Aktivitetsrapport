using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using MatFileHandler;

using PrintDialog = System.Windows.Controls.PrintDialog;
using System.Printing;
using Aktivitetsrapport.Properties;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Diagnostics;
using Aktivitetsrapport.Properties;
using System.Globalization;

namespace Aktivitetsrapport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public class Hour
        {

            private DateTime date;

            public DateTime Date
            {
                get { return date; }
                set { date = value; }
            }

            private double[] activity;

            public double[] Activity
            {
                get { return activity; }
                set { activity = value; }
            }


            private double steps = 0;

            public double Steps
            {
                get { return steps; }
                set { steps = value; }
            }

            public Hour(DateTime newdate, double[] newactivity, double newsteps)
            {
                date = newdate;
                activity = newactivity;
                steps = newsteps;

            }
        }

        private int[] walkActs = null;
        private int[] sitlieActs = null;
        private int[] standActs = null;
        private int[] sleepActs = null;

        private string matpath = null;
        private string cli_command = null;

        public MainWindow()
        {
            InitializeComponent();

            btn_path.ToolTip = "Leta efter matlab-filen (.mat) som genereras av Actipass och skapa rapporten";
            btn_print.ToolTip = "Skriv ut rapporten till valfri skrivare eller spara som pdf med 'Microsoft Print to PDF'";
            btn_save.ToolTip = "Spara rapporten som png-fil";
            btn_cli.ToolTip = "Kör Actipass vilket genererar matlab filen som sedan hittas med bläddra knappen";

            txt_info.Text = "1. För att skapa rapporten används en matlab-fil (.mat) som genereras av Actipass.\n" +
                            "   Filen finns i arbetskatalogen som Actipass använder och sökvägen är:\n" +
                            "   Actipass arbetskatalog\\IndividualOut\\sensor\\sensor - Activity_per_s.mat\n" +
                            "\n" +
                            "2. När filen valts ut med bläddra knappen kommer rapporten att skapas.\n" +
                            "\n" +
                            "3. Rapporten kan skrivas ut eller sparas till png-fil.";


            try
            {
                ReadSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

        }


        private void open_mat_file()
        {
            try
            {
                List<Hour> newhourlist = read_mat(txt_path.Text);

                ax_report.Init(newhourlist, walkActs, sitlieActs, standActs, sleepActs);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);

            }

        }

        //NonWear=0, Lie=1, Sit=2, Stand=3, Move=4, Walk=5, Run=6, Stair=7, Cycle=8, Other=9, Sleep=10, LieStill=11

        private List<Hour> read_mat(string matpath)
        {

            List<Hour> hourlist = new List<Hour>();

            using (var fileStream = new System.IO.FileStream(matpath, System.IO.FileMode.Open))
            {
                var reader = new MatFileReader(fileStream);

                IMatFile matFile = reader.Read();

                IVariable variable = matFile["aktTbl"];

                var matObject = matFile["aktTbl"].Value as IMatObject;

                //vararray = (IArray)

                TableAdapter ta = new TableAdapter(matObject);

                double[] d = ta["DateTime"].ConvertToDoubleArray();

                double[] a = ta["Activity"].ConvertToDoubleArray();

                double[] steps = ta["Steps"].ConvertToDoubleArray();

                //Need to remove 1900 years
                DateTime[] datetime = d.Select(p => DateTime.FromOADate(p).AddYears(-1900)).ToArray();

                int[] allactivities = a.Select(p => (int)p).ToArray();

                double[] houractivity = new double[12];

                double asteps = 0;

                int startitem = 0;


                //Find first midnight and continue 
                for (int i = 0; i < allactivities.Length; i++)
                {
                    if (datetime[i].Hour == 0)
                    {
                        startitem = i;
                        break;
                    }

                }

                int currenthour = datetime[startitem].Hour;

                for (int i = startitem; i < allactivities.Length; i++)
                {

                    houractivity[allactivities[i]] += 1;

                    asteps += steps[i];

                    if (!datetime[i].Hour.Equals(currenthour))
                    {
                        //Add to last hour 
                        hourlist.Add(new Hour(datetime[i].AddHours(-1), houractivity, asteps));
                        houractivity = new double[12];
                        asteps = 0;
                        currenthour = datetime[i].Hour;
                    }

                }

            }
            return hourlist;

        }

        private void btn_path_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Open folder chooser
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    dialog.Title = "Välj mat-fil från Actipass";
                    //dialog.FileName = matpath;
                    dialog.DefaultExt = "mat";
                    dialog.Filter = "Matlab datafiler (*.mat)|*.mat|Alla filer (*.*)|*.*";


                    DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        txt_path.Text = dialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                    Properties.Settings.Default.MatPath = dialog.FileName;

                    Properties.Settings.Default.Save();

                    open_mat_file();

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

        }

        private void btn_print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintReport();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }


        private void PrintReport()
        {
            System.Windows.Controls.PrintDialog printDlg = new System.Windows.Controls.PrintDialog();

            if ((bool)printDlg.ShowDialog())
            {
                ax_report.Measure(new Size((int)ax_report.ActualWidth, (int)ax_report.ActualHeight));
                ax_report.Arrange(new Rect(new Size((int)ax_report.ActualWidth, (int)ax_report.ActualHeight)));

                RenderTargetBitmap bitmap = new RenderTargetBitmap((int)ax_report.ActualWidth, (int)ax_report.ActualHeight, 96, 96, PixelFormats.Pbgra32);

                bitmap.Render(ax_report);

                Image reportimage = new Image();

                reportimage.Source = bitmap;


                //now print the visual to printer to fit on the one page.
                printDlg.PrintVisual(reportimage, "First Fit to Page WPF Print");

            }


        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveReportAsPng();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void SaveReportAsPng()
        {

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Rapport"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "PNG bilder (.png)|*.png"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document

                ax_report.Measure(new Size((int)ax_report.ActualWidth, (int)ax_report.ActualHeight));
                ax_report.Arrange(new Rect(new Size((int)ax_report.ActualWidth, (int)ax_report.ActualHeight)));

                RenderTargetBitmap bitmap = new RenderTargetBitmap((int)ax_report.ActualWidth, (int)ax_report.ActualHeight, 96, 96, PixelFormats.Pbgra32);

                bitmap.Render(ax_report);

                Image reportimage = new Image();

                reportimage.Source = bitmap;

                using (FileStream stream = File.Create(@dlg.FileName))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);

                }
            }
        }

        private void ReadSettings()
        {
            this.matpath = Properties.Settings.Default.MatPath;
            this.cli_command = Properties.Settings.Default.CLI_Command;

            this.walkActs = Properties.Settings.Default.Walk.Split(',').Select(s => Int32.Parse(s)).ToArray();
            this.sitlieActs = Properties.Settings.Default.SitLie.Split(',').Select(s => Int32.Parse(s)).ToArray();
            this.standActs = Properties.Settings.Default.Stand.Split(',').Select(s => Int32.Parse(s)).ToArray();
            this.sleepActs = Properties.Settings.Default.Sleep.Split(',').Select(s => Int32.Parse(s)).ToArray();


            if (cli_command.Equals(""))
            {
                txt_cli.Text = cli_command;
                txt_cli.Visibility = Visibility.Collapsed;
                btn_cli.Visibility = Visibility.Collapsed;

                txt_cli.SetValue(Grid.RowProperty, 3);
                btn_cli.SetValue(Grid.RowProperty, 3);
                txt_path.SetValue(Grid.RowProperty, 1);
                btn_path.SetValue(Grid.RowProperty, 2);
                btn_print.SetValue(Grid.RowProperty, 2);
                btn_save.SetValue(Grid.RowProperty, 2);

            }
            else
            {
                txt_cli.Text = cli_command;
                txt_cli.Visibility = Visibility.Visible;
                btn_cli.Visibility = Visibility.Visible;

                txt_cli.SetValue(Grid.RowProperty, 1);
                btn_cli.SetValue(Grid.RowProperty, 1);
                txt_path.SetValue(Grid.RowProperty, 2);
                btn_path.SetValue(Grid.RowProperty, 3);
                btn_print.SetValue(Grid.RowProperty, 3);
                btn_save.SetValue(Grid.RowProperty, 3);

            }

            txt_path.Text = matpath;

        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {

                try
                {
                    Settings setting = new Settings();

                    bool result = (bool)setting.ShowDialog();

                    if (result)
                    {
                        Properties.Settings.Default.Save();
                        ReadSettings();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
        }

        private void btn_cli_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (!txt_cli.Text.Equals(""))
                {
                    Process cmd = new Process();
                    cmd.StartInfo.FileName = "cmd.exe";
                    cmd.StartInfo.RedirectStandardInput = true;
                    cmd.StartInfo.RedirectStandardOutput = true;
                    cmd.StartInfo.CreateNoWindow = false;
                    cmd.StartInfo.UseShellExecute = false;
                    cmd.Start();

                    cmd.StandardInput.WriteLine(txt_cli.Text);
                    cmd.StandardInput.Flush();
                    cmd.StandardInput.Close();
                    cmd.WaitForExit();
                    Console.WriteLine(cmd.StandardOutput.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
    }

}
