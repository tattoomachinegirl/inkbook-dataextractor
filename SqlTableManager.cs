using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Data;

namespace TattooMachineGirl.Inkbook.Data.Extract
{
    public class SqlTableManager
    {
        private DataTable table;
        private IDbConnection db;
        private readonly Logger log;



        public SqlTableManager(DataTable table, IDbConnection db, Logger log)
        {
            this.table = table;
            this.db = db;
            this.log = log;
        }

        public void SyncTableSchemas()
        {
            log.Information($"Table: {table.TableName} Records: {table.Rows.Count}");
        }
        public void UploadTableToSql()
        {
            //log.Information($"Writing CSV: Records {table.Rows.Count}");
            
        }
    }
}
