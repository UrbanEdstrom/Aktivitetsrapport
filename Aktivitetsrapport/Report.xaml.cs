using LiveCharts.Wpf;
using LiveCharts;
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
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Globalization;
using LiveCharts.Helpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using static System.Net.Mime.MediaTypeNames;

namespace Aktivitetsrapport
{
    /// <summary>
    /// Interaction logic for Report.xaml
    /// </summary>
    public partial class Report : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        public SeriesCollection[] PieChartData { get => _pieChartData; set { _pieChartData = value; RaisePropertyChanged(); } }
        public SeriesCollection[] DayStackedBarChartData { get => _dayStackedBarChartData; set { _dayStackedBarChartData = value; RaisePropertyChanged(); } }
        public SeriesCollection WeekStackedBarChartData { get => _weekStackedBarChartData; set { _weekStackedBarChartData = value; RaisePropertyChanged(); } }
        public string[] DayStackedBarChartLabels { get => _dayStackedBarChartLabels; set { _dayStackedBarChartLabels = value; RaisePropertyChanged(); } }
        public string[] WeekStackedBarChartLabels { get => _weekStackedBarChartLabels; set { _weekStackedBarChartLabels = value; RaisePropertyChanged(); } }

        public Func<double, string> AbsFormatter => (x) => Math.Abs(x).ToString();
        public event PropertyChangedEventHandler PropertyChanged;

        private SeriesCollection[] _pieChartData = new SeriesCollection[8];
        private SeriesCollection[] _dayStackedBarChartData = new SeriesCollection[8];
        private SeriesCollection _weekStackedBarChartData = new SeriesCollection();
        private string[] _dayStackedBarChartLabels = new string[24];
        private string[] _weekStackedBarChartLabels = new string[24];

        CultureInfo swedish = new CultureInfo("sv-SE");
        public Report()
        {
            InitializeComponent();

            DataContext = this;

        }

        public void Clear()
        { 
            foreach (SeriesCollection Series in PieChartData) {
                if (Series!=null) Series.Clear();
            }
            foreach (SeriesCollection Series in DayStackedBarChartData)
            { 
                if(Series!=null) Series.Clear();
            }
            WeekStackedBarChartData.Clear();

            StackPanel_Days.Children.Clear();

            nameTextBlock.Text = "";
            periodTextBlock.Text = "";

            txt_daymeanstep.Text = "";
            txt_daymeanwalk.Text = "";
            txt_daymeanstand.Text = "";
            txt_daymeanlie.Text = "";
            txt_daymeansleep.Text = "";
            txt_workmeanstep.Text = "";
            txt_workmeanwalk.Text = "";
            txt_workmeanstand.Text = "";
            txt_workmeanlie.Text = "";
            txt_workmeansleep.Text = "";
            txt_weekendmeanstep.Text = "";
            txt_weekendmeanwalk.Text = "";
            txt_weekendmeanstand.Text = "";
            txt_weekendmeanlie.Text = "";
            txt_weekendmeansleep.Text = "";

        }

        public void Init(List<MainWindow.Hour> newlist, int[] walkacts, int[] sitlieacts, int[] standacts, int[] sleepacts, string filename)
        {

            List<string> weekdays = new List<string>();
            List<ChartValues<double>> dayWalk = new List<ChartValues<double>>();
            List<ChartValues<double>> dayStand = new List<ChartValues<double>>();
            List<ChartValues<double>> daySitLie = new List<ChartValues<double>>();
            List<ChartValues<double>> daySleep = new List<ChartValues<double>>();
            List<double[]> daySteps = new List<double[]>();

            
            //Get weekdata from list
            DayOfWeek day = newlist[0].Date.DayOfWeek;
            
            dayWalk.Add(new ChartValues<double>(new double[24]));
            dayStand.Add(new ChartValues<double>(new double[24]));
            daySitLie.Add(new ChartValues<double>(new double[24]));
            daySleep.Add(new ChartValues<double>(new double[24]));
            daySteps.Add(new double[24]);


            foreach (var item in newlist)
            {

                if (!item.Date.DayOfWeek.Equals(day))
                {

                    weekdays.Add(swedish.DateTimeFormat.DayNames[(int)day]);
                    day = item.Date.DayOfWeek;
                    dayWalk.Add(new ChartValues<double>(new double[24]));
                    dayStand.Add(new ChartValues<double>(new double[24]));
                    daySitLie.Add(new ChartValues<double>(new double[24]));
                    daySleep.Add(new ChartValues<double>(new double[24]));
                    daySteps.Add(new double[24]);
                }

                int hour = item.Date.Hour;

                //NonWear=0, Lie=1, Sit=2, Stand=3, Move=4, Walk=5, Run=6, Stair=7, Cycle=8, Other=9, Sleep=10, LieStill=11

                double sumWalkActs = 0;
                double sumSitLieActs = 0;
                double sumStandActs = 0;
                double sumSleepActs = 0;

                foreach (int num in walkacts)
                {
                    sumWalkActs += item.Activity[num];
                }
                foreach (int num in sitlieacts)
                {
                    sumSitLieActs += item.Activity[num];
                }
                foreach (int num in standacts)
                {
                    sumStandActs += item.Activity[num];
                }
                foreach (int num in sleepacts)
                {
                    sumSleepActs += item.Activity[num];
                }

                dayWalk[dayWalk.Count - 1][hour] += Math.Round((sumWalkActs) / 60, 1);
                dayStand[dayStand.Count - 1][hour] += Math.Round(sumStandActs / 60, 1);
                daySitLie[daySitLie.Count - 1][hour] -= Math.Round((sumSitLieActs) / 60, 1);
                daySleep[daySleep.Count - 1][hour] -= Math.Round(sumSleepActs / 60, 1);
                daySteps[daySteps.Count - 1][hour] += item.Steps;

            }
            weekdays.Add(swedish.DateTimeFormat.DayNames[(int)day]);

            ChartValues<double> weekWalk = new ChartValues<double>();
            ChartValues<double> wwekStand = new ChartValues<double>();
            ChartValues<double> weekSitLie = new ChartValues<double>();
            ChartValues<double> weekSleep = new ChartValues<double>();
            List<double> weekSteps = new List<double>();

            //Conditioning the day lists
            for (int i = 0; i < dayWalk.Count(); i++)
            {
                weekWalk.Add(dayWalk[i].ToList().Sum(x => Math.Round(x / 60, 1)));
                wwekStand.Add(dayStand[i].ToList().Sum(x => Math.Round(x / 60, 1)));
                weekSitLie.Add(daySitLie[i].ToList().Sum(x => Math.Round(x / 60, 1)));
                weekSleep.Add(daySleep[i].ToList().Sum(x => Math.Round(x / 60, 1)));
                weekSteps.Add(daySteps[i].Sum());
            }

            nameTextBlock.Text = filename;
            periodTextBlock.Text = swedish.DateTimeFormat.DayNames[(int)newlist[0].Date.DayOfWeek] + " " + newlist[0].Date.ToShortDateString() + " - " + swedish.DateTimeFormat.DayNames[(int)newlist[newlist.Count - 1].Date.DayOfWeek] + " " + newlist[newlist.Count - 1].Date.ToShortDateString();


            InitWeekStackedBarChart(weekWalk, wwekStand, weekSitLie, weekSleep, weekSteps, weekdays);

            //Get daydata from lists
            InitDayStackedBarChart(dayWalk, dayStand, daySitLie, daySleep, daySteps, weekdays);

        }

        public void InitWeekStackedBarChart(ChartValues<double> walk, ChartValues<double> stand, ChartValues<double> sitlie, ChartValues<double> sleep, List<double> steps, List<string> weekdaylist)
        {


            // Add data for stacked bar chart
            WeekStackedBarChartData = new SeriesCollection
            {

                new StackedColumnSeries
                {
                    Title = "Rörelse",
                    Values = walk,
                    DataLabels = false
                },
                new StackedColumnSeries
                {
                    Title = "Stå",
                    Values = stand,
                    DataLabels = false
                },
                new StackedColumnSeries
                {
                    Title = "Sitta/ligga",
                    Values = sitlie,
                    DataLabels = false
                },
                 new StackedColumnSeries
                {
                    Title = "Sova",
                    Values = sleep,
                    DataLabels = false
                }
            };


            weekdaylist = weekdaylist.Select(x => x = x.Substring(0, 3)).ToList();

            WeekStackedBarChartLabels = weekdaylist.ToArray();
            WeekStackedBarChartAxisY.LabelFormatter = AbsFormatter;

            double daymeanstep = 0, workmeanstep = 0, weekendmeanstep = 0,
                    daymeanwalk = 0, workmeanwalk = 0, weekendmeanwalk = 0,
                    daymeanstand = 0, workmeanstand = 0, weekendmeanstand = 0,
                    daymeanlie = 0, workmeanlie = 0, weekendmeanlie = 0,
                    daymeansleep = 0, workmeansleep = 0, weekendmeansleep = 0;


            for (int i = 0; i < steps.Count(); i++)
            {
                daymeanstep += steps[i] / 7;
                daymeanwalk += walk[i] / 7;
                daymeanstand += stand[i] / 7;
                daymeanlie -= sitlie[i] / 7;
                daymeansleep -= sleep[i] / 7;

                if (weekdaylist[i].Equals("lör") || weekdaylist[i].Equals("sön"))
                {
                    weekendmeanstep += steps[i] / 2;
                    weekendmeanwalk += walk[i] / 2;
                    weekendmeanstand += stand[i] / 2;
                    weekendmeanlie -= sitlie[i] / 2;
                    weekendmeansleep -= sleep[i] / 2;
                }
                else
                {
                    workmeanstep += steps[i] / 5;
                    workmeanwalk += walk[i] / 5;
                    workmeanstand += stand[i] / 5;
                    workmeanlie -= sitlie[i] / 5;
                    workmeansleep -= sleep[i] / 5;
                }

            }

            txt_daymeanstep.Text = String.Format("{0:0}", daymeanstep);
            txt_daymeanwalk.Text = String.Format("{0:0.0}", daymeanwalk);
            txt_daymeanstand.Text = String.Format("{0:0.0}", daymeanstand);
            txt_daymeanlie.Text = String.Format("{0:0.0}", daymeanlie);
            txt_daymeansleep.Text = String.Format("{0:0.0}", daymeansleep);
            txt_workmeanstep.Text = String.Format("{0:0}", workmeanstep);
            txt_workmeanwalk.Text = String.Format("{0:0.0}", workmeanwalk);
            txt_workmeanstand.Text = String.Format("{0:0.0}", workmeanstand);
            txt_workmeanlie.Text = String.Format("{0:0.0}", workmeanlie);
            txt_workmeansleep.Text = String.Format("{0:0.0}", workmeansleep);
            txt_weekendmeanstep.Text = String.Format("{0:0}", weekendmeanstep);
            txt_weekendmeanwalk.Text = String.Format("{0:0.0}", weekendmeanwalk);
            txt_weekendmeanstand.Text = String.Format("{0:0.0}", weekendmeanstand);
            txt_weekendmeanlie.Text = String.Format("{0:0.0}", weekendmeanlie);
            txt_weekendmeansleep.Text = String.Format("{0:0.0}", weekendmeansleep);

        }

        private void InitDayStackedBarChart(List<ChartValues<double>> Walk, List<ChartValues<double>> Stand, List<ChartValues<double>> SitLie, List<ChartValues<double>> Sleep, List<double[]> Steps, List<string> weekdaylist)
        {

            StackPanel_Days.Children.Clear();

            for (int daynum = 0; daynum < Walk.Count; daynum++)
            {

                DayBlock dayblock = new DayBlock();

                DayStackedBarChartData[daynum] = new SeriesCollection
                {

                    new StackedColumnSeries
                    {
                        Title = "Rörelse",
                        Values = Walk[daynum],
                        DataLabels = false,
                    },
                    new StackedColumnSeries
                    {
                        Title = "Stå",
                        Values = Stand[daynum],
                        DataLabels = false,
                    },
                    new StackedColumnSeries
                    {
                        Title = "Sitta/ligga",
                        Values = SitLie[daynum],
                        DataLabels = false,
                    },

                    new StackedColumnSeries
                    {
                        Title = "Sova",
                        Values = Sleep[daynum],
                        DataLabels = false,
                    }

                };

                dayblock.dayChart.Series =DayStackedBarChartData[daynum];

                // Init labels
                for (int i = 0; i < 24; i++)
                {
                    // Use TimeSpan to format hours and minutes
                    TimeSpan time = new TimeSpan(i, 0, 0);
                    DayStackedBarChartLabels[i] = time.ToString("hh");
                }

                dayblock.X_axislabels.Labels = DayStackedBarChartLabels;
                dayblock.StackedBarChartAxisY.LabelFormatter = AbsFormatter;

                //Init piechart 
                double sumwalk = CalcDailyActivityDistribution(daynum, 0);
                double sumstand = CalcDailyActivityDistribution(daynum, 1);
                double sumsitlie = CalcDailyActivityDistribution(daynum, 2);
                double sumsleep = CalcDailyActivityDistribution(daynum, 3);

                PieChartData[daynum] = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "Rörelse",
                        Values = new ChartValues<double> {sumwalk},
                        DataLabels = false
                    },
                    new PieSeries
                    {
                        Title = "Stå",
                        Values = new ChartValues<double> {sumstand},
                        DataLabels = false
                    },
                    new PieSeries
                    {
                        Title = "Sitta/ligga",
                        Values = new ChartValues<double> {sumsitlie},
                        DataLabels = false
                    },
                     new PieSeries
                    {
                        Title = "Sova",
                        Values = new ChartValues<double> {sumsleep},
                        DataLabels = false
                    }
                };

                double sumsteps = Steps[daynum].ToArray().Cast<double>().Sum();

                dayblock.txt_walk.Text = String.Format("{0:0.0} tim", sumwalk / 60);
                dayblock.txt_stand.Text = String.Format("{0:0.0} tim", sumstand / 60);
                dayblock.txt_sitlie.Text = String.Format("{0:0.0} tim", sumsitlie / 60);
                dayblock.txt_sleep.Text = String.Format("{0:0.0} tim", sumsleep / 60);
                dayblock.txt_steps.Text = String.Format("{0:0}", sumsteps);


                dayblock.pieChart.Series = PieChartData[daynum];

                dayblock.txt_day.Text = weekdaylist[daynum];

                StackPanel_Days.Children.Add(dayblock);

            }

        }

        private double CalcDailyActivityDistribution(int daynum, int i)
        {
            return DayStackedBarChartData[daynum][i].Values.Cast<double>().Select(Math.Abs).ToArray().Sum();
        }

        public void RaisePropertyChanged([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}
