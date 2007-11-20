
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Lephone.Data;
using Lephone.Data.Builder;
using Lephone.Data.SqlEntry;
using Lephone.Data.Definition;
using Lephone.MockSql.Recorder;

namespace Lephone.UnitTest.Data
{
    [TestFixture]
    public class DbNowTest
    {
        #region Init

        [DbTable("DateTable")]
        public abstract class DateTable : DbObjectModel<DateTable>
        {
            [SpecialName]
            public abstract DateTime CreatedOn { get; set; }

            [SpecialName]
            public abstract DateTime? UpdatedOn { get; set; }

            public abstract string Name { get; set; }
        }

        [DbTable("DateTable")]
        public class DateTable2 : DbObject
        {
            [SpecialName]
            public DateTime CreatedOn;

            [SpecialName]
            public DateTime? UpdatedOn;

            public string Name;
        }

        [DbTable("DateTable")]
        public class DateTable3 : DbObject
        {
            [SpecialName]
            public DateTime SavedOn;

            public string Name;
        }

        [DbTable("DateTable")]
        public abstract class DateTable4 : DbObjectModel<DateTable4>
        {
            [SpecialName]
            public abstract DateTime SavedOn { get; set; }

            public abstract string Name { get; set; }
        }

        private DbContext de = new DbContext("SQLite");

        [SetUp]
        public void SetUp()
        {
            StaticRecorder.ClearMessages();
        }

        #endregion

        [Test]
        public void TestCreatedOn()
        {
            InsertStatementBuilder sb = new InsertStatementBuilder("user");
            sb.Values.Add(new KeyValue("CreatedOn", DbNow.Value));
            SqlStatement sql = sb.ToSqlStatement(de.Dialect);
            Assert.AreEqual("Insert Into [user] ([CreatedOn]) Values (datetime(current_timestamp, 'localtime'));\n", sql.SqlCommandText);
        }

        [Test]
        public void TestUpdatedOn()
        {
            UpdateStatementBuilder sb = new UpdateStatementBuilder("user");
            sb.Values.Add(new KeyValue("UpdatedOn", DbNow.Value));
            SqlStatement sql = sb.ToSqlStatement(de.Dialect);
            Assert.AreEqual("Update [user] Set [UpdatedOn]=datetime(current_timestamp, 'localtime') ;\n", sql.SqlCommandText);
        }

        [Test]
        public void TestCreatedOnWithTable()
        {
            DateTable o = DateTable.New();
            o.Name = "tom";
            de.Insert(o);
            Assert.AreEqual("Insert Into [DateTable] ([CreatedOn],[Name]) Values (datetime(current_timestamp, 'localtime'),@Name_0);\nSELECT last_insert_rowid();\n", StaticRecorder.LastMessage);
        }

        [Test]
        public void TestUpdatedOnWithTable()
        {
            DateTable o = DateTable.New();
            o.Name = "tom";
            o.Id = 1;
            de.Update(o);
            Assert.AreEqual("Update [DateTable] Set [UpdatedOn]=datetime(current_timestamp, 'localtime'),[Name]=@Name_0  Where [Id] = @Id_1;\n", StaticRecorder.LastMessage);
        }

        [Test]
        public void TestUpdatedOnWithoutPartialUpdate()
        {
            DateTable2 o = new DateTable2();
            o.Name = "tom";
            o.Id = 1;
            de.Update(o);
            Assert.AreEqual("Update [DateTable] Set [UpdatedOn]=datetime(current_timestamp, 'localtime'),[Name]=@Name_0  Where [Id] = @Id_1;\n", StaticRecorder.LastMessage);
        }

        [Test]
        public void TestSelectDatabaseTime()
        {
            DateTime dt = DbEntry.Context.GetDatabaseTime();
            TimeSpan ts = DateTime.Now.Subtract(dt);
            Assert.IsTrue(ts.TotalSeconds < 10);
        }

        [Test]
        public void TestSavedOn()
        {
            DateTable3 o = new DateTable3();
            o.Name = "tom";
            de.Insert(o);
            Assert.AreEqual("Insert Into [DateTable] ([SavedOn],[Name]) Values (datetime(current_timestamp, 'localtime'),@Name_0);\nSELECT last_insert_rowid();\n", StaticRecorder.LastMessage);
        }

        [Test]
        public void TestSavedOnForUpdate()
        {
            DateTable3 o = new DateTable3();
            o.Name = "tom";
            o.Id = 1;
            de.Update(o);
            Assert.AreEqual("Update [DateTable] Set [SavedOn]=datetime(current_timestamp, 'localtime'),[Name]=@Name_0  Where [Id] = @Id_1;\n", StaticRecorder.LastMessage);
        }

        [Test]
        public void TestSavedOnForPartialUpdate()
        {
            DateTable4 o = DateTable4.New();
            o.Name = "tom";
            o.Id = 1;
            de.Update(o);
            Assert.AreEqual("Update [DateTable] Set [SavedOn]=datetime(current_timestamp, 'localtime'),[Name]=@Name_0  Where [Id] = @Id_1;\n", StaticRecorder.LastMessage);
        }
    }
}
