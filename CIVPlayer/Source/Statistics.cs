using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIVPlayer.Source
{
    public class Statistics
    {
        private string file;
        public List<StatisticRow> statisticsData;
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Statistics(string filePath)
        {
            log.Info("Statistics object created.");
            this.file = filePath;
            log.Info("Reading statistics data from file: " + filePath);
            ReadStatistics(true);
        }

        public StatisticRow WriteStatLine(string passingPlayer)
        {
            StatisticRow row = new StatisticRow();
            row.passingPlayer = passingPlayer;
            row.time = DateTime.Now;
            row.roundTime = new TimeSpan(0, 0, 10);
            if (statisticsData.Any())
            {
                row.roundTime = row.time - statisticsData.Last().time;
            }
            statisticsData.Add(row);
            using (StreamWriter sw = new StreamWriter(file, true))
            {
                sw.WriteLine(row.ConvertToString());
            }
            log.Info("Statistics row logged.");
            return row;
        }

        public StatisticRow ReadStatistics(bool onlyLast)
        {
            statisticsData = new List<StatisticRow>();
            string line;
            if (!onlyLast)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        statisticsData.Add(processLine(line));
                    }
                }
                log.Info("Statistics data loaded.");
            }
            else
            {
                string lastLine = File.ReadLines(file).Last();
                if (lastLine != null)
                {
                    statisticsData.Add(processLine(lastLine));
                }
                log.Info("Statistics data last row loaded.");
            }
            if (statisticsData.Any())
            {
                return statisticsData.Last();
            }
            else
            {
                return new StatisticRow
                {
                    time = new DateTime(2014, 06, 22),
                    passingPlayer = "Még senki",
                    roundTime = new TimeSpan(0, 0, 15, 22)
                };
            }
        }

        private StatisticRow processLine(string line)
        {
            StatisticRow row = new StatisticRow();
            string[] rowData = line.Split('|');
            row.time = DateTime.Parse(rowData[0]);
            row.passingPlayer = rowData[1];
            row.roundTime = TimeSpan.Parse(rowData[2]);
            return row;
        }

    }

    public struct StatisticRow
    {
        public DateTime time;
        public string passingPlayer;
        public TimeSpan roundTime;

        private static string delimeter = "|";

        public string ConvertToString()
        {
            return time.ToString() + delimeter + passingPlayer.ToString() + delimeter + roundTime.ToString();
        }
    }
}
