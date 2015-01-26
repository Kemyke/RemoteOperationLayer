using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ArdinHelpers;

namespace ArdinRemoteOperations
{
    [Serializable]
	public class RemoteRequest : RemoteRequestBase
	{
        internal RemoteOperationDescriptor ExecuteOnRemoteSideOperation = null;

        #region IXmlSerializable Members
        public override void ReadXml(XmlReader reader)
        {
            base.ReadXml(reader);
            try
            {
                reader.ReadStartElement("ExecuteOnRemoteSideOperation");
                this.ExecuteOnRemoteSideOperation = (RemoteOperationDescriptor)SerializationHelper.DeserializeFromXml(reader);
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
                writer.WriteStartElement("ExecuteOnRemoteSideOperation");
                SerializationHelper.SerializeToXml(writer, this.ExecuteOnRemoteSideOperation);
                writer.WriteEndElement();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        #endregion

        public string GetDisplayName()
        {
            string ret = string.Join("+", ExecuteOnRemoteSideOperation.InterfaceType, ExecuteOnRemoteSideOperation.MethodName);
            return ret;
        }
    }

}
