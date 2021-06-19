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

        static void Main(FileInfo file, DirectoryInfo logDirectory = null, DirectoryInfo output = null, LogEventLevel loglevel = LogEventLevel.Information)
        {
            //Set default file location 
            file ??= new FileInfo("./data.xml");
            logDirectory ??= new DirectoryInfo("./logs");

            #region Configure Logger
            Log = Configuration.SerilogAdapter.GetLogger(loglevel, logDirectory);

            #endregion
            #region Load DataSet

            if (!file.Exists)
            {
                Log.Error($"{file.FullName} could not be found");
                Environment.Exit(1);
            }
            var dataSet = new DataSet();

            try
            {
                Log.Information($"Loading Dataset");
                //Read Inkbook Backup File into DataSet 

                Log.Verbose($"Loading dataset file {file.FullName}");
                dataSet.ReadXml(file.FullName);
                Log.Information($"Loaded {dataSet.Tables.Count} Tables from DataSet");

                foreach (DataTable table in dataSet.Tables)
                {
                    Log.Verbose($"Table { table.TableName } Rows: {table.Rows.Count }");
                }


            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}");
                Log.Verbose(e.StackTrace);
                
                Environment.Exit(1);
            }
            #endregion
            #region Get Employee
            Console.WriteLine();
            DataTable employeeTable = dataSet.Tables["tblEmployees"];
            foreach (DataRow emp in employeeTable.AsEnumerable())
            {
                Log.Information($"{emp.Field<int>("fldEmployeeID")}: {emp.Field<string>("fldFirstName")} {emp.Field<string>("fldLastName")} ");
            }

            Log.Information("Enter Employee ID to Export: ");
            Console.WriteLine();

            string input = Console.ReadLine().Trim();


            var valid = int.TryParse(input, out var employeeId);

            if (!valid)
            {
                Log.Error($"Invalid Entry {input}");
                Environment.Exit(1);
            }
            
            Log.Verbose($"User entered :{employeeId}");


            var employeeRecord = employeeTable.AsEnumerable().Where(q => q.Field<int>("fldEmployeeID") == employeeId).FirstOrDefault();

            if (null == employeeRecord)
            {
                Log.Fatal($"Employee ID {employeeId } not found");
                Environment.Exit(1);
            }



            #endregion
            #region Collect Export Data 
            var exportTables = new Dictionary<string, DataTable>();

            var appointments = dataSet.Tables["tblTicketsRow"].AsEnumerable().Where(q => q.Field<int>("fldEmployeeID") == employeeId);
            if (!appointments.Any())
            {
                Log.Error($"No Appointments for Employee {employeeId}. Nothing to export");
                Environment.Exit(1);
            }

            var apptTable = appointments?.CopyToDataTable();

            apptTable.TableName = "tblTicketsRow";

            //refernce table to connect appointments to clients
            var appointmentSummaries = dataSet.Tables["tblTicketsSummary"];

            //clients table ,  this will be filtered after we assemble the referece data from 
            var allClients = dataSet.Tables["tblClients"].AsEnumerable();

            var filterClients = appointments.AsEnumerable()
                .Where(appointment => appointment.Field<int>("fldEmployeeID") == employeeId)
                .Join(appointmentSummaries.AsEnumerable(), (summary) => summary.Field<int>("fldTicketID"), appointment => appointment.Field<int>("fldTicketID"), (appointment, summary) => new
                {
                    fldClientID = summary.Field<int?>("fldClientID"),
                    fldEmployeeID = appointment.Field<int>("fldEmployeeID"),
                    fldTicketID = summary.Field<int>("fldTicketID"),
                    fldEmployeeName = appointment.Field<string>("fldEmployeeName"),
                    fldDescription = appointment.Field<string>("fldDescription"),

                }).Where(q => null != q.fldClientID)
                .Join(allClients.AsEnumerable(), appointment => appointment.fldClientID, client => client.Field<int?>("fldClientID"), (appointment, client) => new
                {
                    appointment.fldClientID,
                    appointment.fldEmployeeID,
                    appointment.fldTicketID,
                    appointment.fldEmployeeName,
                    appointment.fldDescription,
                    fldFirstName = client.Field<string>("fldFirstName"),
                    fldLastName = client.Field<string>("fldLastName"),
                    client
                });

            var clientIds = filterClients.Select(client => client.fldClientID.Value).Distinct();

            var clients = filterClients.Select(c => c.client).Distinct().CopyToDataTable();
            clients.TableName = "tblClients";
            #endregion
            #region Create Output Directories 
            //create output directory
            output = output ?? new DirectoryInfo("./output");
            DirectoryInfo outputPath = null;
            if (!output.Exists)
            {
                Log.Verbose($"Base dutput directory {output.FullName} does not exist, creating...");
                output.Create();
                Log.Verbose($"done.");

            }

            Log.Verbose($"Creating output subdirectory for table extraction");

            string dir = $"export_{employeeRecord.Field<string>("fldFirstName").ToLower()}_{employeeRecord.Field<string>("fldLastName").ToLower()}_{ DateTime.Now.ToString("MMddyyyy_hh_mm_ss")}";
            outputPath = output.CreateSubdirectory(dir);
            Log.Verbose($"Created output directory {outputPath.FullName}");
            #endregion
            #region Write Table Data to csv
            //write in-memory reference collection to csv 
            using var writer = new StreamWriter($"{outputPath}/tblRefClientTicket.csv");
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            Log.Information($"Exporting table tblRefClientTicket to csv tblRefClientTicket.csv ");
            
            csv.WriteRecords(filterClients.Select(client => new
            {

                client.fldClientID,
                client.fldFirstName,
                client.fldLastName,
                client.fldTicketID,
                client.fldDescription,
                client.fldEmployeeID,
                client.fldEmployeeName
            }));

            //collect filtered table data we want to export 
            exportTables.Add("tblTicketsRow", apptTable);
            exportTables.Add("tblClients", clients);
            exportTables.Add("tblTicketsSummary", dataSet.Tables["tblTicketsSummary"]);

            foreach (var table in exportTables)
            {
                var filePath = new FileInfo($"{ outputPath.FullName }/{table.Value.TableName}.csv");
                Log.Information($"Exporting table {table.Value.TableName} to csv {filePath.Name} ");
                ToCSV(table.Value, filePath.FullName);

            }
            #endregion
            #region Finish Up
            //Open output directory 
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = outputPath.FullName,
                UseShellExecute = true,
                Verb = "open"
            });
            #endregion

        }

        #region Helper Methods 
        public static void ToCSV(DataTable dtDataTable, string strFilePath)
        {
            var file = new FileInfo(strFilePath);
            Log.Verbose($"Writing {file.Name}");
            StreamWriter sw = new StreamWriter(strFilePath, false);
            //headers
            Log.Verbose($"Writing {file.Name}: header");
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                

                sw.Write(dtDataTable.Columns[i]);
                Log.Debug($"Writing Column Header {dtDataTable.Columns[i]}");
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
        #endregion
    }
}
