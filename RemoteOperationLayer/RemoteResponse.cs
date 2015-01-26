using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ArdinHelpers;

namespace ArdinRemoteOperations
{
    [Serializable]
    public class RemoteResponse : RemoteResponseBase
	{
		public object ReturnValue;

        #region IXmlSerializable Members
        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            try
            {
                reader.ReadStartElement("ReturnValue");
                this.ReturnValue = SerializationHelper.DeserializeFromXml(reader);
                reader.ReadEndElement();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);
            try
            {
                writer.WriteStartElement("ReturnValue");
                SerializationHelper.SerializeToXml(writer, this.ReturnValue);
                writer.WriteEndElement();
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
