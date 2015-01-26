using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;

namespace ArdinRemoteOperations
{
    [Serializable]
    public abstract class RemoteRequestResponseBase : IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }

        public RemoteSideIDType RemoteID = null;

        #region IXmlSerializable Members
        public virtual void ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            try
            {
                reader.ReadStartElement();
                this.RemoteID = RemoteSideIDType.Parse(reader.ReadElementString("RemoteID"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            try
            {
                writer.WriteElementString("RemoteID", (string)this.RemoteID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        #endregion
    }

}
