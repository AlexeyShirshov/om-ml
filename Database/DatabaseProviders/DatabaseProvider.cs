﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using WXML.Model.Descriptors;
using System.Linq;

namespace WXML.Model.Database.Providers
{
    public abstract class DatabaseProvider : ISourceProvider
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

        public abstract SourceView GetSourceView(string schemas, string namelike, bool escapeTableNames, bool escapeColumnNames);
        public abstract void GenerateCreateScript(IEnumerable<PropertyDefinition> props, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateCreateScript(RelationDefinitionBase rel, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateDropConstraintScript(SourceFragmentDefinition table, string constraintName, StringBuilder script);
        public abstract void GenerateCreatePKScript(IEnumerable<SourceFieldDefinition> pks, string constraintName, StringBuilder script, bool pk, bool clustered);
        public abstract void GenerateCreateFKsScript(SourceFragmentDefinition table, IEnumerable<FKDefinition> fks, StringBuilder script);
        public abstract void GenerateAddColumnsScript(IEnumerable<PropDefinition> props, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateAddColumnsScript(IEnumerable<SourceFieldDefinition> props, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateCreateIndexScript(SourceFragmentDefinition table, IndexDefinition indexes, StringBuilder script);
        public abstract void GenerateDropIndexScript(SourceFragmentDefinition table, string indexName, StringBuilder script);
        public abstract void GenerateDropTableScript(SourceFragmentDefinition table, StringBuilder script);

        public abstract DbConnection GetDBConn();

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

        #region ISourceProvider Members


        public void GenerateCreatePKScript(IEnumerable<PropDefinition> pks, string constraintName, StringBuilder script, bool pk, bool clustered)
        {
            GenerateCreatePKScript(pks.Select((item)=>item.Field), constraintName, script, pk, clustered);
        }

        #endregion
    }
}
