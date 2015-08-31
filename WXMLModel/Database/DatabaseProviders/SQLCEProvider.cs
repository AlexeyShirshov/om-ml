using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Text;

namespace WXML.Model.Database.Providers
{
    public class SQLCEProvider : MSSQLProvider
    {

        public SQLCEProvider(string server, string psw)
        {
            _server = server;
            _psw = psw;
        }
        public override System.Data.Common.DbConnection GetDBConn()
        {
            var cb = new SqlCeConnectionStringBuilder();

            cb.DataSource = _server;
            if (!string.IsNullOrEmpty(_psw))
                cb.Password = _psw;

            return new SqlCeConnection(cb.ConnectionString);
        }

        protected override string AppendIdentity()
        {
            return "case when c.AUTOINC_INCREMENT is null then 0 else 1 end as [identity]";
        }
    }
}
