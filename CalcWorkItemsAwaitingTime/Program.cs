using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WITDataAccess;
using System.Configuration;
using System.Collections.Specialized;

namespace CalcWorkItemsAwaitingTime
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Help message...
            // CalcWorkItemsAwaitingTime PAT [Alias] [StartTime] [EndTime]
            if (args.Length == 0
                || (args.Length == 1 && (args[0] == "/?" || args[0] == "/help")))
            {
                ShowHelpMessage();
            }
            else
            {
                if (RequestArgs.TryParse(args, out RequestArgs requestArgs))
                {
                    Console.WriteLine("Start to validate args...");

                    var msg = requestArgs.Validate();

                    Console.WriteLine("Validation completed.");

                    if (string.IsNullOrEmpty(msg))
                    {
                        Console.WriteLine("Collecting Data...");

                        var q = new QueryExecutor(requestArgs.PAT);

                        var workItemEntities = q.GenerateWorkItemEntity(requestArgs.Alias, requestArgs.StartTime, requestArgs.EndTime);

                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "result.csv");
                        CheckFileExists(filePath);

                        Console.WriteLine($"Generating Report to {filePath}...");

                        OutputResult(workItemEntities, filePath);

                        Console.WriteLine($"All Done! Check your data at {filePath}.");
                    }
                    else
                        Console.WriteLine(msg);
                }
                else
                {
                    Console.WriteLine("Failed to parse args.");
                    ShowHelpMessage();
                }
            }

            Console.Read();
            return;
        }

        private static void CheckFileExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static void OutputResult(IList<WorkItemEntity> workItemEntities, string filePath)
        {
            using (StreamWriter w = File.CreateText(filePath))
            {
                Console.WriteLine("Id Title CreatedDate CloseDate SupportTeam ServiceCategory SubCategory ReopenTimes TimeSpent AwaitingTimeInMin");

                var csv = new StringBuilder();
                var columnName = "Id,Title,CreatedDate,CloseDate,SupportTeam,ServiceCategory,SubCategory,ReopenTimes,TimeSpent,AwaitingTimeInMin";
                csv.AppendLine(columnName);
                foreach (var entity in workItemEntities)
                {
                    if (entity.WitType == WitType.ServiceTicket){
                        //string a = ConfigurationManager.AppSettings.Get("Key0");
                        //Console.WriteLine(a);
                        Console.WriteLine($"{entity.Id} " +
                            $"{entity.Title} " +
                            $"{entity.CreatedDate} " +
                            $"{entity.CloseDate} " +
                            $"{entity.SupportTeam} " +
                            $"{entity.ServiceCategory} " +
                            $"{entity.SubCategory} " +
                            $"{entity.ReopenTimes} " +
                            $"{entity.TimeSpent} " +
                            $"{entity.AwaitingTimeInMinutesInTotal}");
                        var title =  string.Format(("\"{0}\""), entity.Title);
                        var newLine = $"{entity.Id}," +
                            $"{title}," +
                            $"{entity.CreatedDate}," +
                            $"{entity.CloseDate}," +
                            $"{entity.SupportTeam}," +
                            $"{entity.ServiceCategory}," +
                            $"{entity.SubCategory}," +
                            $"{entity.ReopenTimes}," +
                            $"{entity.TimeSpent}," +
                            $"{entity.AwaitingTimeInMinutesInTotal}";
                        csv.AppendLine(newLine);
                    }
                    
                }

                w.WriteLine(csv.ToString());
            }
        }

        private static void ShowHelpMessage()
        {
            Console.WriteLine("Usage: CalcWorkItemsAwaitingTime [PAT] [Alias] [StartTime] [EndTime].");
            Console.WriteLine("Eg. CalcWorkItemsAwaitingTime PAT v-beche 07/01/2021 08/01/2021");
        }
    }
}
