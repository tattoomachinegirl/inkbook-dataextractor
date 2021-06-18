using CsvHelper;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace TattooMachineGirl.Inkbook.Data.Extract
{
    class Program
    {
        public static Logger Log { get; set; }
        private static Dictionary<string, string> exportTableNames = new Dictionary<string, string>
        {

           { "employees",       "tblEmployees"  },
           { "clients",         "tblClients"    },
           { "appointments",    "tblTicketsRow" }

        };

        static void Main(FileInfo file, DirectoryInfo logDirectory = null, DirectoryInfo outputDirectory = null, LogEventLevel logLevel = LogEventLevel.Information)
        {
            //Set default file location 
            file ??= new FileInfo("./data.xml");

            #region Configure Logger
            Log = Configuration.SerilogAdapter.GetLogger(logLevel, logDirectory);
            outputDirectory = outputDirectory ?? new DirectoryInfo("./output");
            DirectoryInfo outputPath = null;
            if (!outputDirectory.Exists)
            {
                Log.Verbose($"output directory {outputDirectory.FullName} does not exist, creating...");
                outputDirectory.Create();
                Log.Verbose($"done.");

            }
            #endregion

            #region Load DataSet
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
            #endregion

            #region Get Employee
            Console.WriteLine();
            DataTable employeeTable = dataSet.Tables[exportTableNames["employees"]];
            foreach (DataRow emp in employeeTable.AsEnumerable())
            {
                Log.Information($"{emp.Field<int>("fldEmployeeID")}: {emp.Field<string>("fldFirstName")} {emp.Field<string>("fldLastName")} ");
            }

            Console.Write("Enter Employee ID to Export: ");
            Console.WriteLine();
            

            var employeeId = Console.ReadLine().Trim();

            var employeeRecord = employeeTable.AsEnumerable().Where(q => q.Field<string>("fldEmployeeID") == employeeId);

            if (null == employeeId )
            {
                Log.Fatal($"Employee ID {employeeId } not found");
                Environment.Exit(1);
            }
            #endregion

            var exportTables = new Dictionary<string, DataTable>();

            var appointments  = dataSet.Tables["tblTicketsRow"].AsEnumerable().Where( q=> q.Field<int>("fldEmployeeID") == int.Parse(employeeId)).CopyToDataTable();
                                appointments.TableName = "tblTicketsRow";
            var appointmentSummaries = dataSet.Tables["tblTicketsSummary"];
            var allClients = dataSet.Tables["tblClients"].AsEnumerable();

            var filterClients = appointments.AsEnumerable()
                .Where(appointment => appointment.Field<int>("fldEmployeeID") == int.Parse(employeeId))
                .Join(appointmentSummaries.AsEnumerable(), (summary) => summary.Field<int>("fldTicketID"), appointment => appointment.Field<int>("fldTicketID"), (appointment, summary) => new
                {
                    fldClientID = summary.Field<int?>("fldClientID"),
                    fldEmployeeID = appointment.Field<int>("fldEmployeeID"),
                    fldTicketID = summary.Field<int>("fldTicketID"),
                    fldEmployeeName = appointment.Field<string>("fldEmployeeName"),
                    fldDescription = appointment.Field<string>("fldDescription"),

                }).Where(q=> null != q.fldClientID )
                .Join(allClients.AsEnumerable(), appointment => appointment.fldClientID, client => client.Field<int?>("fldClientID"), (appointment, client) => new {
                    appointment.fldClientID,
                    appointment.fldEmployeeID, 
                    appointment.fldTicketID,
                    appointment.fldEmployeeName,
                    appointment.fldDescription,
                    FirstName = client.Field<string>("fldFirstName"),
                    LastName = client.Field<string>("fldLastName")
                });

            var clientIds = filterClients.Select(client => client.fldClientID.Value).Distinct();
            var clients = allClients.Where(q => clientIds.Any(id => q.Field<int>("fldClientID") == id)).CopyToDataTable();
            clients.TableName = "tblClients";
            

            using var writer = new StreamWriter($"{outputPath}/tblRefClientTicket.csv");
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            csv.WriteRecords(filterClients);
            
            


            exportTables.Add("appointments", appointments);
            exportTables.Add("clients", clients);
            exportTables.Add("tblTicketsSummary", dataSet.Tables["tblTicketsSummary"]);
            exportTables.Add("tblServiceFinish", dataSet.Tables["tblServiceFinish"]);
            exportTables.Add("tblServiceDuration", dataSet.Tables["tblServiceDuration"]);
            exportTables.Add("tblCreate", dataSet.Tables["tblCreate"]);
            exportTables.Add("tblSoapNotes", dataSet.Tables["tblSoapNotes"]);



            //additional tables if defined 
            //var exportTables = dataSet.Tables.Cast<DataTable>().ToList().Where(q => exportTableNames.Any(a => a == q.TableName));

            var clientRefTables = dataSet.Tables.Cast<DataTable>().Where(q => q.Columns.Cast<DataColumn>().Any(q => q.ColumnName == "fldClientID"));
            foreach (var item in clientRefTables)
            {
                Console.WriteLine(item.TableName);
            }

            foreach (var table in exportTables)
            {
                var filePath = new FileInfo($"{ outputPath.FullName }/{table.Value.TableName}.csv");
                Log.Information($"Exporting table {table.Value.TableName} to csv {filePath.Name} ");
                ToCSV(table.Value, filePath.FullName);
         
            }
    
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = outputPath.FullName,
                UseShellExecute = true,
                Verb = "open"
            });

        }


        public static IEnumerable<DataRow> FilterTable(DataTable table,  string col , string value){
            var result = table.Select($"{col} = {value}");
            return result;
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
