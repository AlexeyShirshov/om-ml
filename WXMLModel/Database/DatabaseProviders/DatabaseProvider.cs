using System;
using System.Collections.Generic;
using System.Data.Common;
using WXML.Model.Descriptors;

namespace WXML.Model.Database.Providers
{
    public abstract class DatabaseProvider
    {
        protected string _server;
        protected string _db;
        protected bool _integratedSecurity;
        protected string _user;
        protected string _psw;

        public delegate void DatabaseConnectingDelegate(DatabaseProvider sender, string conn);
        public event DatabaseConnectingDelegate OnDatabaseConnecting;
        public event Action OnStartLoadDatabase;
        public event Action OnEndLoadDatabase;

        protected DatabaseProvider(string server, string db, bool integratedSecurity, string user, string psw)
        {
            _server = server;
            _db = db;
            _integratedSecurity = integratedSecurity;
            _user = user;
            _psw = psw;
        }

        public abstract SourceView GetDatabase(string schemas, string namelike);

        protected abstract DbConnection GetDBConn();

        protected abstract string AppendIdentity();
        
        protected void RaiseOnDatabaseConnecting(string conn)
        {
            if (OnDatabaseConnecting != null)
                OnDatabaseConnecting(this, conn);
        }

        protected void RaiseOnStartLoadDatabase()
        {
            if (OnStartLoadDatabase != null)
                OnStartLoadDatabase();
        }

        protected void RaiseOnEndLoadDatabase()
        {
            if (OnEndLoadDatabase != null)
                OnEndLoadDatabase();
        }
    }
}
