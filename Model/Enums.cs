using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model
{
    public enum AccessLevel
    {
        Private,
        Family,
        Assembly,
        Public,
		FamilyOrAssembly
    }

    public enum EntityBehaviuor
    {
		/// <summary>
		/// Default behaviour when generator creates default classes(entity and schema) with full method set.
		/// </summary>
		Default = 0,
        /// <summary>
        /// 'Partial object' behaviour when generator creates classes(entity and schema) without user depended behaviour for future extension.
        /// </summary>
        PartialObjects = 1,
        /// <summary>
        /// Force 'partial' modifier with default behaviour.
        /// </summary>
        ForcePartial = 2,
        ///// <summary>
        ///// Set abstract modifier.
        ///// </summary>
        //Abstract
    }

    [Flags]
    public enum Field2DbRelations
    {
        Factory = 0x80,
        InsertDefault = 8,
        None = 0,
        PK = 0x20,
        /// <summary>
        /// PK or SyncInsert or [ReadOnly]
        /// </summary>
        PrimaryKey = 0x25,
        Private = 0x40,
        ReadOnly = 4,
        /// <summary>
        /// RV or [ReadOnly] or SyncUpdate or SyncInsert
        /// </summary>
        RowVersion = 0x17,
        RV = 0x10,
        SyncInsert = 1,
        SyncUpdate = 2
    }

    public enum MergeAction
    {
        Merge,
        Delete
    }

    public enum GenerateModeEnum
    {
        Full,
        SchemaOnly,
        EntityOnly
    }
}
