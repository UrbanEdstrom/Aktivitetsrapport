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

using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Threading;
using System.Reflection.Emit;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using static System.Windows.Forms.LinkLabel;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Windows.Controls.Image;

namespace Aktivitetsrapport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
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

        private string docpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
               
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                ReadSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

            DataContext = this;

            string[] args = Environment.GetCommandLineArgs();

            if (args.Count() > 1 ) 
            {
                Launch_File(args[1]);
            }

        }


        private void open_mat_file(string matpath)
        {
            
            try
            {
                List<Hour> newhourlist = read_mat(matpath);

                ax_report.Init(newhourlist, walkActs, sitlieActs, standActs, sleepActs,filename);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);

            }

        }

        private List<Hour> read_mat(string matpath)
        {

            List<Hour> hourlist = new List<Hour>();

            using (var fileStream = new System.IO.FileStream(matpath, System.IO.FileMode.Open))
            {
                var reader = new MatFileReader(fileStream);

                IMatFile matFile = reader.Read();

                var matObject = matFile.Variables[0].Value as IMatObject;

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
                    dialog.Filter = "Aktivitetsfiler (*.mat;*.cwa)|*.mat;*.cwa|Alla filer (*.*)|*.*";

                    DialogResult result = dialog.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.Cancel)
                    {
                        return;
                    }

                    Launch_File(dialog.FileName);
                    
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

                System.Windows.Controls.Image reportimage = new System.Windows.Controls.Image();

                reportimage.Source = bitmap;

                printDlg.PrintVisual(reportimage, "Aktivitetsrapport");

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
            dlg.FileName = filename; // Default file name
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
            
            this.walkActs = Properties.Settings.Default.Walk.Split(',').Select(s => Int32.Parse(s)).ToArray();
            this.sitlieActs = Properties.Settings.Default.SitLie.Split(',').Select(s => Int32.Parse(s)).ToArray();
            this.standActs = Properties.Settings.Default.Stand.Split(',').Select(s => Int32.Parse(s)).ToArray();
            this.sleepActs = Properties.Settings.Default.Sleep.Split(',').Select(s => Int32.Parse(s)).ToArray();
            this.progress_analys.Maximum = Int32.Parse(Properties.Settings.Default.CLI_steps);

        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Save();
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

        private void callCLI(string cwafile)
        {

            string workpath = System.IO.Path.Combine(docpath,"Aktivitetsrapport");

            Directory.CreateDirectory(workpath);

            string matFile = System.IO.Path.Combine(workpath,filename+".mat");

            ax_report.Clear();

            TxtOut = "Startar analys...\n";

            try
            {
                        
                Process cmd = new Process();
                cmd.StartInfo.FileName = "actipass_cli.exe";
                cmd.StartInfo.Arguments = "\"" + cwafile + "\"" + " out " + "\"" + matFile + "\""; //+ " daily " + "\"no\"";                
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.EnableRaisingEvents = true;
                cmd.OutputDataReceived += Cmd_OutputDataReceived;
                cmd.Exited += Cmd_Exited;
                cmd.Start();
                cmd.BeginOutputReadLine();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                if (ex.Message.Equals("Det går inte att hitta filen")) {
                    TxtOut += "För att öppna en cwa-fil direkt behöver ActiPASS_CLI vara installerat och tillagt i Windows PATH";
                }
            }
        }

        private void Cmd_Exited(object sender, EventArgs e)
        {

            Launch_File(System.IO.Path.Combine(docpath , "Aktivitetsrapport", filename + ".mat"));

        }
                
        private void Cmd_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Process cmd = sender as Process;
            string line = e.Data + "\n";

            TxtOut += line;
            AnalysProgress++;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try 
            {
                SaveSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private string txtOut = "";

        public string TxtOut
        {
            get => txtOut;

            set
            {
                txtOut = value;
                NotifyPropertyChanged(nameof(TxtOut));
            }
        }

        private int analysProgress;

        public int AnalysProgress
        {
            get { return analysProgress; }
            set 
            { 
                analysProgress = value;
                NotifyPropertyChanged(nameof(AnalysProgress));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        string filename = null;

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            foreach (string file in files)  //Not tested with multiple files
            {
                Launch_File(file);
            }

        }

        private void Launch_File(string aktfile)
        {
            AnalysProgress = 0;

            string ext = System.IO.Path.GetExtension(aktfile).ToLower();

            filename = System.IO.Path.GetFileNameWithoutExtension(aktfile);

            if (ext.Equals(".cwa"))
            {
                callCLI(aktfile);
            }
            if (ext.Equals(".mat"))
            {
                this.Dispatcher.Invoke(() =>
                {
                    open_mat_file(aktfile);
                });
            }


        }

    }

}
