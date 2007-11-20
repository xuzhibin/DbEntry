
#region usings

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

#endregion

namespace Lephone.Util.Text
{
    public class XmlSerializer<T> : StringSerializer<T>
    {
        private string RootName;

        public XmlSerializer() {}

        public XmlSerializer(string RootName)
        {
            this.RootName = RootName;
        }

        private XmlRootAttribute GetXmlRootAttribute()
        {
            Type t = typeof(T);
            XmlTypeAttribute xt = ClassHelper.GetAttribute<XmlTypeAttribute>(t, false);
            if (xt == null)
            {
                XmlRootAttribute xr = ClassHelper.GetAttribute<XmlRootAttribute>(t, false);
                if (xr == null)
                {
                    string rn = (string.IsNullOrEmpty(RootName)) ? t.Name : RootName;
                    xr = new XmlRootAttribute(rn);
                }
                return xr;
            }
            return new XmlRootAttribute(xt.TypeName);
        }

        public override string Serialize(T obj)
        {
            Type t = typeof(T);
            XmlRootAttribute xr = GetXmlRootAttribute();
            XmlSerializer ois = new XmlSerializer(t, xr);
            using (MemoryStream ms = new MemoryStream())
            {
                ois.Serialize(ms, obj);
                byte[] bs = ms.ToArray();
                string s = Encoding.UTF8.GetString(bs);
                return s;
            }
        }

        public override T Deserialize(string Source)
        {
            Type t = typeof(T);
            XmlRootAttribute xr = GetXmlRootAttribute();
            XmlSerializer ois = new XmlSerializer(t, xr);
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] bs = Encoding.UTF8.GetBytes(Source);
                ms.Write(bs, 0, bs.Length);
                ms.Position = 0;
                return (T)ois.Deserialize(ms);
            }
        }
    }
}
