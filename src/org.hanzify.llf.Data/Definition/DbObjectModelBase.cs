
using System;
using System.Collections.Generic;
using System.Text;
using Lephone.Data.Common;

namespace Lephone.Data.Definition
{
    [Serializable]
    public class DbObjectModelBase<T, TKey> : DbObjectSmartUpdate
    {
        protected static CK Col
        {
            get { return CK.Column; }
        }

        public virtual void Save()
        {
            DbEntry.Save(this);
        }

        public virtual void Delete()
        {
            DbEntry.Delete(this);
        }

        public virtual ValidateHandler Validate()
        {
            ValidateHandler v = new ValidateHandler();
            v.ValidateObject(this);
            return v;
        }

        public virtual bool IsValid()
        {
            return Validate().IsValid;
        }

        public static T FindById(TKey Id)
        {
            return DbEntry.GetObject<T>(Id);
        }

        public static DbObjectList<T> FindBySql(string SqlStr)
        {
            return DbEntry.Context.ExecuteList<T>(SqlStr);
        }

        public static DbObjectList<T> FindBySql(SqlEntry.SqlStatement Sql)
        {
            return DbEntry.Context.ExecuteList<T>(Sql);
        }

        public static DbObjectList<T> FindAll()
        {
            return FindAll(null);
        }

        public static DbObjectList<T> FindAll(OrderBy ob)
        {
            return DbEntry.From<T>().Where(null).OrderBy(ob).Select();
        }

        public static DbObjectList<T> Find(WhereCondition con)
        {
            return Find(con, null);
        }

        public static DbObjectList<T> Find(WhereCondition con, OrderBy ob)
        {
            return DbEntry.From<T>().Where(con).OrderBy(ob).Select();
        }

        public static T FindOne(WhereCondition con)
        {
            return DbEntry.GetObject<T>(con);
        }

        public static T FindOne(WhereCondition con, OrderBy ob)
        {
            return DbEntry.GetObject<T>(con, ob);
        }

        public static DbObjectList<T> FindRecent(int Count)
        {
            string Id = DbObjectHelper.GetKeyField(typeof(T)).Name;
            return DbEntry.From<T>().Where(null).OrderBy((DESC)Id).Range(1, Count).Select();
        }

        public static long GetCount(WhereCondition con)
        {
            return DbEntry.From<T>().Where(con).GetCount();
        }

        public static T New()
        {
            return DynamicObject.NewObject<T>();
        }

        public static T New(params object[] os)
        {
            return DynamicObject.NewObject<T>(os);
        }

        public static void DeleteAll()
        {
            DeleteAll(null);
        }

        public static void DeleteAll(WhereCondition con)
        {
            DbEntry.Delete<T>(con);
        }
    }
}
