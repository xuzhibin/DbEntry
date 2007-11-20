
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Lephone.Util.Text;
using NUnit.Framework;

namespace Lephone.UnitTest.util
{
    [TestFixture]
    public class XmlSerializerTest
    {
        [XmlRoot("List")]
        public class MyList2 : List<MyItem> { }

        [XmlType("List")]
        public class MyList : List<MyItem> { }

        [XmlType("Item")]
        public class MyItem
        {
            public string Name;

            public MyItem() { }

            public MyItem(string Name)
            {
                this.Name = Name;
            }
        }

        [Test]
        public void Test1()
        {
            MyList l = new MyList();
            l.Add(new MyItem("tom"));
            string act = XmlSerializer<MyList>.Xml.Serialize(l);
            Assert.AreEqual("<?xml version=\"1.0\"?>\r\n<List xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Item>\r\n    <Name>tom</Name>\r\n  </Item>\r\n</List>", act);
        }

        [Test]
        public void Test2()
        {
            MyList2 l = new MyList2();
            l.Add(new MyItem("tom"));
            string act = XmlSerializer<MyList2>.Xml.Serialize(l);
            Assert.AreEqual("<?xml version=\"1.0\"?>\r\n<List xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Item>\r\n    <Name>tom</Name>\r\n  </Item>\r\n</List>", act);
        }
    }
}
