﻿using System;
using System.Collections.Generic;
using System.Data;
using Lephone.Core;
using Lephone.Data.Caching;
using Lephone.Data.Common;
using Lephone.Data.Definition;
using Lephone.Data.Model;
using Lephone.Data.Model.Composer;
using Lephone.Data.Model.Handler;
using Lephone.Data.Model.Member;
using Lephone.Data.Model.QuerySyntax;
using Lephone.Data.SqlEntry;

namespace Lephone.Data
{
    public class ModelContext
    {
        internal static bool LoadHandler = true;

        #region flyweight

        private class ContextFactory : FlyweightBase<Type, ModelContext>
        {
            protected override ModelContext CreateInst(Type t)
            {
                return new ModelContext(t);
            }
        }

        private static readonly ContextFactory Factory = new ContextFactory();

        public static ModelContext GetInstance(Type type)
        {
            return Factory.GetInstance(type);
        }

        #endregion

        #region ctor

        public readonly ObjectInfo Info;
        public readonly DataProvider Provider;
        internal QueryComposer Composer;
        public readonly IDbObjectHandler Handler;
        public readonly ModelOperator Operator;

        private ModelContext(Type t)
        {
            Info = new ObjectInfo(t);
            Provider = DataProviderFactory.Instance.GetInstance(Info.ContextName);
            Composer = GetQueryComposer();
            if(LoadHandler)
            {
                Handler = CreateDbObjectHandler();
            }
            if(DataSettings.CacheEnabled && Info.HasOnePrimaryKey && Info.Cacheable)
            {
                Operator = new CachedModelOperator(Info, Composer, Provider, Handler);
            }
            else
            {
                Operator = new ModelOperator(Info, Composer, Provider, Handler);
            }
        }

        private QueryComposer GetQueryComposer()
        {
            if (!string.IsNullOrEmpty(Info.SoftDeleteColumnName))
            {
                return new SoftDeleteQueryComposer(this, Info.SoftDeleteColumnName);
            }
            if (!string.IsNullOrEmpty(Info.DeleteToTableName))
            {
                return new DeleteToQueryComposer(this);
            }
            if (Info.LockVersion != null)
            {
                return new OptimisticLockingQueryComposer(this);
            }
            return new QueryComposer(this);
        }

        public IDbObjectHandler CreateDbObjectHandler()
        {
            if (Info.HandleType.IsGenericType)
            {
                switch (Info.HandleType.Name)
                {
                    case "GroupByObject`1":
                        var t = typeof(GroupbyObjectHandler<>).MakeGenericType(Info.HandleType.GetGenericArguments());
                        return (IDbObjectHandler)ClassHelper.CreateInstance(t);
                    case "GroupBySumObject`2":
                        var ts = typeof(GroupbySumObjectHandler<,>).MakeGenericType(Info.HandleType.GetGenericArguments());
                        return (IDbObjectHandler)ClassHelper.CreateInstance(ts);
                    default:
                        throw new NotSupportedException();
                }
            }
            var attr = Info.HandleType.GetAttribute<ModelHandlerAttribute>(false);
            if (attr != null)
            {
                var o = (EmitObjectHandlerBase)ClassHelper.CreateInstance(attr.Type);
                o.Init(Info);
                return o;
            }
            throw new ModelException(Info.HandleType, "Can not find ObjectHandler.");
        }

        #endregion

        public IWhere<T> From<T>() where T : class, IDbObject
        {
            return new QueryContent<T>(this);
        }

        public object GetPrimaryKeyDefaultValue()
        {
            if (Info.KeyMembers.Length > 1)
            {
                throw new DataException("GetPrimaryKeyDefaultValue don't support multi key.");
            }
            return CommonHelper.GetEmptyValue(Info.KeyMembers[0].MemberType, false, "only supported int long guid as primary key.");
        }

        public bool IsNewObject(object obj)
        {
            return Info.KeyMembers[0].UnsavedValue.Equals(Handler.GetKeyValue(obj));
        }

        public object NewObject()
        {
            return Handler.CreateInstance();
        }

        #region static functions

        public static object CreateObject(Type dbObjectType, IDataReader dr, bool useIndex, bool noLazy)
        {
            if (dbObjectType.Name.StartsWith("<"))
            {
                return DynamicLinqObjectHandler.Factory.GetInstance(dbObjectType).CreateObject(dr, useIndex);
            }

            var ctx = Factory.GetInstance(dbObjectType);
            object obj = ctx.NewObject();
            var sudi = obj as DbObjectSmartUpdate;
            if (sudi != null)
            {
                sudi.m_InternalInit = true;
            }
            LoadValues(obj, ctx, dr, useIndex, noLazy);
            if (sudi != null)
            {
                sudi.m_InternalInit = false;
            }
            return obj;
        }

        private static void LoadValues(object obj, ModelContext ctx, IDataReader dr, bool useIndex, bool noLazy)
        {
            try
            {
                ctx.Handler.LoadSimpleValues(obj, useIndex, dr);
                ctx.Handler.LoadRelationValues(obj, useIndex, noLazy, dr);
            }
            catch(InvalidCastException)
            {
                if(useIndex)
                {
                    FindMemberCastFailByIndex(ctx, dr);
                }
                else
                {
                    FindMemberCastFailByName(ctx, dr);
                }
                throw;
            }
        }

        private static void FindMemberCastFailByIndex(ModelContext ctx, IDataReader dr)
        {
            var ms = ctx.Info.SimpleMembers;
            for(int i = 0; i < ms.Length; i++)
            {
                CheckMemberCast(ms[i], dr[i]);
            }
        }

        private static void FindMemberCastFailByName(ModelContext ctx, IDataReader dr)
        {
            var ms = ctx.Info.SimpleMembers;
            foreach(var member in ms)
            {
                CheckMemberCast(member, dr[member.Name]);
            }
        }

        private static void CheckMemberCast(MemberHandler member, object value)
        {
            if (value == DBNull.Value && member.Is.AllowNull)
            {
                return;
            }
            if(member.MemberType.IsEnum && value is Int32)
            {
                return;
            }
            if (member.MemberType != value.GetType())
            {
                throw new DataException("The type of member [{0}] is [{1}] but sql value of it is [{2}]",
                                        member.Name, member.MemberType, value.GetType());
            }
        }

        public static Condition GetKeyWhereClause(object obj)
        {
            Type t = obj.GetType();
            var ctx = GetInstance(t);
            if (ctx.Info.KeyMembers == null)
            {
                throw new DataException("dbobject do not have key field : " + t);
            }
            Condition ret = null;
            Dictionary<string, object> dictionary = ctx.Handler.GetKeyValues(obj);
            foreach (string s in dictionary.Keys)
            {
                ret &= (CK.K[s] == dictionary[s]);
            }
            return ret;
        }

        public static void SetKey(object obj, object key)
        {
            var t = obj.GetType();
            var ctx = GetInstance(t);
            if (!ctx.Info.HasSystemKey)
            {
                throw new DataException("dbobject do not have SystemGeneration key field : " + t);
            }
            var fh = ctx.Info.KeyMembers[0];
            object sKey;
            if (fh.MemberType == typeof(long))
            {
                sKey = Convert.ToInt64(key);
            }
            else
            {
                sKey = fh.MemberType == typeof(int) ? Convert.ToInt32(key) : key;
            }
            fh.SetValue(obj, sKey);
        }

        public static object CloneObject(object obj)
        {
            if (obj == null) { return null; }
            var ctx = GetInstance(obj.GetType());
            object o = ctx.NewObject();
            var os = o as DbObjectSmartUpdate;
            if (os != null)
            {
                os.m_InternalInit = true;
                InnerCloneObject(obj, ctx.Info, o);
                os.m_InternalInit = false;
            }
            else
            {
                InnerCloneObject(obj, ctx.Info, o);
            }
            return o;
        }

        private static void InnerCloneObject(object obj, ObjectInfo oi, object o)
        {
            foreach (var m in oi.SimpleMembers)
            {
                object v = m.GetValue(obj);
                m.SetValue(o, v);
            }
            foreach (var f in oi.RelationMembers)
            {
                if (f.Is.BelongsTo)
                {
                    var os = (IBelongsTo)f.GetValue(obj);
                    var od = (IBelongsTo)f.GetValue(o);
                    od.ForeignKey = os.ForeignKey;
                }
            }
        }

        #endregion
    }
}
