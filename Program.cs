using CsvHelper;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TattooMachineGirl.Inkbook.Data.Extract
{
    class Program
    {
        public static Logger Log { get; set; }
        private static List<string> exportTableNames = new List<string>
        {

           "tblEmployees", 
           "tblClients",
           "tblTicketsRow"

        };
        static void Main(FileInfo file, DirectoryInfo logDirectory = null, DirectoryInfo outputDirectory = null, LogEventLevel logLevel = LogEventLevel.Information)
        {

            file = file ?? new FileInfo("./data.xml");

            //Setup Test Data 
#if DEBUG
            file = new FileInfo(@"./test-data/data.xml");
            logLevel = LogEventLevel.Verbose;

#endif
            //Configure Serilog Action Logging 
            
            
            Log = Configuration.SerilogAdapter.GetLogger(logLevel, logDirectory);
            outputDirectory = outputDirectory ?? new DirectoryInfo("./output");
            DirectoryInfo outputPath = null;
            if (!outputDirectory.Exists)
            {
                Log.Verbose($"output directory {outputDirectory.FullName} does not exist, creating...");
                outputDirectory.Create();
                Log.Verbose($"done.");

            }

            string dir = $"export_{ DateTime.Now.ToString("MMddyyyy_hh_mm_ss")}";
            outputPath = outputDirectory.CreateSubdirectory(dir);
            Log.Verbose($"Created output directory {outputPath.FullName}");
            if (!file.Exists)
            {
                Log.Fatal($"{file.FullName} could not be found");
                Environment.Exit(1);
            }
            var dataSet = new DataSet();

            try
            {
                Log.Information($"Loading Dataset");
                //Read Inkbook Backup File into DataSet 
                dataSet.ReadXml(file.FullName);
                Log.Information($"Loaded {dataSet.Tables.Count} Tables from DataSet");

               


                foreach (DataTable table in dataSet.Tables)
                {
                    Log.Verbose($"Table { table.TableName } Rows: {table.Rows.Count }");
                }


            }
            catch (Exception e)
            {
                Log.Fatal($"{e.Message}");
                if (logLevel >= LogEventLevel.Verbose)
                    Log.Error(e.StackTrace);
                Environment.Exit(1);
            }

            DataTable employeeTable = dataSet.Tables["tblEmployees"];
            foreach (DataRow emp in employeeTable.AsEnumerable())
            {
                Log.Information($"{emp.Field<int>("fldEmployeeID")}: {emp.Field<string>("fldFirstName")} {emp.Field<string>("fldLastName")} ");
            }
            Console.Write("Enter Employee ID to Export: ");
            var employeeId = Console.ReadLine().Trim();

            var employeeRecord = employeeTable.AsEnumerable().Where(q => q.Field<string>("fldEmployeeID") == employeeId);


            var exportTables = dataSet.Tables.Cast<DataTable>().ToList().Where(q => exportTableNames.Any(a => a == q.TableName));

            //foreach (DataTable item in dataSet.Tables)
            //{
            //    if (exportTableNames.Any())
            //    exportTables.Add(item);
            //}
            try
            {
                Assert.Equal(exportTableNames.Count, exportTables.Count());
            }
            catch (Xunit.Sdk.EqualException e)
            {
                Log.Fatal(e.Message);
                Log.Verbose(e.StackTrace);
                Environment.Exit(1);
            }
            foreach (var table in exportTables)
            {
                var filePath = new FileInfo($"{ outputPath.FullName }/{table.TableName}.csv");
                Log.Information($"Exporting table {table.TableName} to csv {filePath.Name} ");
                ToCSV(table, filePath.FullName);
         
            }
    
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = outputPath.FullName,
                UseShellExecute = true,
                Verb = "open"
            });

        }

        public static void ToCSV( DataTable dtDataTable, string strFilePath)
        {
            StreamWriter sw = new StreamWriter(strFilePath, false);
            //headers    
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                sw.Write(dtDataTable.Columns[i]);
                if (i < dtDataTable.Columns.Count - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);

            

            foreach (DataRow dr in dtDataTable.Rows)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains(','))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(dr[i].ToString());
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }
    }
}
