﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3074
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LinqCodeGenTests
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	[System.Data.Linq.Mapping.DatabaseAttribute(Name="test")]
	public partial class TestCtxDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    partial void Insertent1(ent1 instance);
    partial void Updateent1(ent1 instance);
    partial void Deleteent1(ent1 instance);
    partial void Insertent2(ent2 instance);
    partial void Updateent2(ent2 instance);
    partial void Deleteent2(ent2 instance);
    partial void Insert_1to2(_1to2 instance);
    partial void Update_1to2(_1to2 instance);
    partial void Delete_1to2(_1to2 instance);
    #endregion
		
		public TestCtxDataContext() : 
				base(global::LinqCodeGenTests.Properties.Settings.Default.testConnectionString, mappingSource)
		{
			OnCreated();
		}
		
		public TestCtxDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public TestCtxDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public TestCtxDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public TestCtxDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<ent1> ent1s
		{
			get
			{
				return this.GetTable<ent1>();
			}
		}
		
		public System.Data.Linq.Table<ent2> ent2s
		{
			get
			{
				return this.GetTable<ent2>();
			}
		}
		
		public System.Data.Linq.Table<_1to2> _1to2s
		{
			get
			{
				return this.GetTable<_1to2>();
			}
		}
	}
	
	[Table(Name="dbo.ent1")]
	public partial class ent1 : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _id;
		
		private EntitySet<_1to2> @__1to2s;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnidChanging(int value);
    partial void OnidChanged();
    #endregion
		
		public ent1()
		{
			this.@__1to2s = new EntitySet<_1to2>(new Action<_1to2>(this.attach__1to2s), new Action<_1to2>(this.detach__1to2s));
			OnCreated();
		}
		
		[Column(Storage="_id", AutoSync=AutoSync.OnInsert, DbType="Int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int id
		{
			get
			{
				return this._id;
			}
			set
			{
				if ((this._id != value))
				{
					this.OnidChanging(value);
					this.SendPropertyChanging();
					this._id = value;
					this.SendPropertyChanged("id");
					this.OnidChanged();
				}
			}
		}
		
		[Association(Name="ent1__1to2", Storage="__1to2s", ThisKey="id", OtherKey="ent1_id")]
		public EntitySet<_1to2> _1to2s
		{
			get
			{
				return this.@__1to2s;
			}
			set
			{
				this.@__1to2s.Assign(value);
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		
		private void attach__1to2s(_1to2 entity)
		{
			this.SendPropertyChanging();
			entity.ent1 = this;
		}
		
		private void detach__1to2s(_1to2 entity)
		{
			this.SendPropertyChanging();
			entity.ent1 = null;
		}
	}
	
	[Table(Name="dbo.ent2")]
	public partial class ent2 : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _id;
		
		private string _name;
		
		private EntitySet<_1to2> @__1to2s;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnidChanging(int value);
    partial void OnidChanged();
    partial void OnnameChanging(string value);
    partial void OnnameChanged();
    #endregion
		
		public ent2()
		{
			this.@__1to2s = new EntitySet<_1to2>(new Action<_1to2>(this.attach__1to2s), new Action<_1to2>(this.detach__1to2s));
			OnCreated();
		}
		
		[Column(Storage="_id", AutoSync=AutoSync.OnInsert, DbType="Int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int id
		{
			get
			{
				return this._id;
			}
			set
			{
				if ((this._id != value))
				{
					this.OnidChanging(value);
					this.SendPropertyChanging();
					this._id = value;
					this.SendPropertyChanged("id");
					this.OnidChanged();
				}
			}
		}
		
		[Column(Storage="_name", DbType="VarChar(50)")]
		public string name
		{
			get
			{
				return this._name;
			}
			set
			{
				if ((this._name != value))
				{
					this.OnnameChanging(value);
					this.SendPropertyChanging();
					this._name = value;
					this.SendPropertyChanged("name");
					this.OnnameChanged();
				}
			}
		}
		
		[Association(Name="ent2__1to2", Storage="__1to2s", ThisKey="id", OtherKey="ent2_id")]
		public EntitySet<_1to2> _1to2s
		{
			get
			{
				return this.@__1to2s;
			}
			set
			{
				this.@__1to2s.Assign(value);
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		
		private void attach__1to2s(_1to2 entity)
		{
			this.SendPropertyChanging();
			entity.ent2 = this;
		}
		
		private void detach__1to2s(_1to2 entity)
		{
			this.SendPropertyChanging();
			entity.ent2 = null;
		}
	}
	
	[Table(Name="dbo.[1to2]")]
	public partial class _1to2 : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _ent1_id;
		
		private int _ent2_id;
		
		private EntityRef<ent1> _ent1;
		
		private EntityRef<ent2> _ent2;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void Onent1_idChanging(int value);
    partial void Onent1_idChanged();
    partial void Onent2_idChanging(int value);
    partial void Onent2_idChanged();
    #endregion
		
		public _1to2()
		{
			this._ent1 = default(EntityRef<ent1>);
			this._ent2 = default(EntityRef<ent2>);
			OnCreated();
		}
		
		[Column(Storage="_ent1_id", DbType="Int NOT NULL", IsPrimaryKey=true)]
		public int ent1_id
		{
			get
			{
				return this._ent1_id;
			}
			set
			{
				if ((this._ent1_id != value))
				{
					if (this._ent1.HasLoadedOrAssignedValue)
					{
						throw new System.Data.Linq.ForeignKeyReferenceAlreadyHasValueException();
					}
					this.Onent1_idChanging(value);
					this.SendPropertyChanging();
					this._ent1_id = value;
					this.SendPropertyChanged("ent1_id");
					this.Onent1_idChanged();
				}
			}
		}
		
		[Column(Storage="_ent2_id", DbType="Int NOT NULL", IsPrimaryKey=true)]
		public int ent2_id
		{
			get
			{
				return this._ent2_id;
			}
			set
			{
				if ((this._ent2_id != value))
				{
					if (this._ent2.HasLoadedOrAssignedValue)
					{
						throw new System.Data.Linq.ForeignKeyReferenceAlreadyHasValueException();
					}
					this.Onent2_idChanging(value);
					this.SendPropertyChanging();
					this._ent2_id = value;
					this.SendPropertyChanged("ent2_id");
					this.Onent2_idChanged();
				}
			}
		}
		
		[Association(Name="ent1__1to2", Storage="_ent1", ThisKey="ent1_id", OtherKey="id", IsForeignKey=true, DeleteOnNull=true, DeleteRule="CASCADE")]
		public ent1 ent1
		{
			get
			{
				return this._ent1.Entity;
			}
			set
			{
				ent1 previousValue = this._ent1.Entity;
				if (((previousValue != value) 
							|| (this._ent1.HasLoadedOrAssignedValue == false)))
				{
					this.SendPropertyChanging();
					if ((previousValue != null))
					{
						this._ent1.Entity = null;
						previousValue._1to2s.Remove(this);
					}
					this._ent1.Entity = value;
					if ((value != null))
					{
						value._1to2s.Add(this);
						this._ent1_id = value.id;
					}
					else
					{
						this._ent1_id = default(int);
					}
					this.SendPropertyChanged("ent1");
				}
			}
		}
		
		[Association(Name="ent2__1to2", Storage="_ent2", ThisKey="ent2_id", OtherKey="id", IsForeignKey=true)]
		public ent2 ent2
		{
			get
			{
				return this._ent2.Entity;
			}
			set
			{
				ent2 previousValue = this._ent2.Entity;
				if (((previousValue != value) 
							|| (this._ent2.HasLoadedOrAssignedValue == false)))
				{
					this.SendPropertyChanging();
					if ((previousValue != null))
					{
						this._ent2.Entity = null;
						previousValue._1to2s.Remove(this);
					}
					this._ent2.Entity = value;
					if ((value != null))
					{
						value._1to2s.Add(this);
						this._ent2_id = value.id;
					}
					else
					{
						this._ent2_id = default(int);
					}
					this.SendPropertyChanged("ent2");
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
#pragma warning restore 1591
