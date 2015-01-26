using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Linq.Expressions;
using System.Xml.Linq;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections;
using System.Runtime.Serialization;

namespace ArdinHelpers
{
    public static class SerializationHelper
    {
        private const string NULLELEMENTNAME = "null";
        private const string B64ELEMENTNAME = "b64";
        private const string EXPELEMENTNAME = "exp";
        private const string XMLELEMENTNAME = "xml";
        private const string STRELEMENTNAME = "str";
        private const string INTELEMENTNAME = "int";
        private const string BOOLELEMENTNAME = "bool";
        private const string DECELEMENTNAME = "dec";
        private const string GENLISTELEMENTNAME = "glst";
        private const string TYPEMARKER = "Type";

        static SerializationHelper()
        {
        }

        private static List<Assembly> knownTypeAssemblies
        {
            get
            {
                return AppDomainHelper.GetAssembliesFromAppDomain().ToList();
            }
        }

        public static void SerializeToXml(XmlWriter writer, object o)
        {
            if (o != null)
            {
                var oType = o.GetType();
                if (o is Expression)
                {
                    XElement serializedExpression = ExpressionHelper.Serialize((Expression)o, knownTypeAssemblies);
                    writer.WriteStartElement(EXPELEMENTNAME);
                    serializedExpression.WriteTo(writer);
                    writer.WriteEndElement();
                }
                else if (o is IXmlSerializable)
                {
                    writer.WriteStartElement(XMLELEMENTNAME);
                    writer.WriteElementString(TYPEMARKER, TypeHelper.GetShortAssemblyQualifiedTypeName(oType));
                    XmlSerializer xs = new XmlSerializer(oType);
                    xs.Serialize(writer, o);
                    writer.WriteEndElement();
                }
                else if ((TypeHelper.IsSubclassOf(oType, typeof(List<>))) && (TypeHelper.IsSubclassOf(oType.GetGenericArguments()[0], typeof(Expression))))
                {
                    writer.WriteStartElement(GENLISTELEMENTNAME);
                    writer.WriteElementString(TYPEMARKER, TypeHelper.GetShortAssemblyQualifiedTypeName(oType));
                    foreach (var i in (IList)o)
                    {
                        SerializeToXml(writer, i);
                    }
                    writer.WriteEndElement();
                }
                else if (o is string)
                {
                    writer.WriteElementString(STRELEMENTNAME, (string)o);
                }
                else if (o is int)
                {
                    writer.WriteElementString(INTELEMENTNAME, ((int)o).ToString());
                }
                else if (o is bool)
                {
                    writer.WriteElementString(BOOLELEMENTNAME, ((bool)o).ToString());
                }
                else if (o is decimal)
                {
                    writer.WriteElementString(DECELEMENTNAME, ((decimal)o).ToString());
                }
                else
                {
                    string base64 = SerializeToBase64(o);
                    writer.WriteElementString(B64ELEMENTNAME, base64);
                }
            }
            else
            {
                writer.WriteElementString(NULLELEMENTNAME, null);
            }
        }

        public static string SerializeToXml(object o)
        {
            string ret = null;

            StringBuilder sb = new StringBuilder();

            XmlWriterSettings xws = new XmlWriterSettings();
            xws.ConformanceLevel = ConformanceLevel.Fragment;
            xws.Indent = true;
            xws.OmitXmlDeclaration = true;

            using (XmlWriter xw = XmlWriter.Create(sb, xws))
            {
                SerializeToXml(xw, o);
            }

            ret = sb.ToString();

            return ret;
        }

        private static string SerializeToBase64(object o)
        {
            string base64 = null;
            if (o != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();

                    bf.Serialize(ms, o);
                    ms.Seek(0, SeekOrigin.Begin);
                    base64 = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
                }
            }

            return base64;
        }

        public static object DeserializeFromXml(XmlReader reader)
        {
            object ret = null;

            reader.MoveToContent();

            if (reader.LocalName == B64ELEMENTNAME)
            {
                string base64 = reader.ReadElementString(B64ELEMENTNAME);
                ret = DeserializeFromBase64(base64);
            }
            else if (reader.LocalName == EXPELEMENTNAME)
            {
                reader.ReadStartElement();
                reader.MoveToContent();
                XElement serializedEXpression = (XElement)XElement.ReadFrom(reader);
                reader.ReadEndElement();

                ret = ExpressionHelper.Deserialize(serializedEXpression, knownTypeAssemblies);
            }
            else if (reader.LocalName == XMLELEMENTNAME)
            {
                reader.ReadStartElement();
                reader.MoveToContent();
                string typeString = reader.ReadElementString(TYPEMARKER);
                Type type = Type.GetType(typeString);
                if (type == null)
                {
                    throw new InvalidOperationException(String.Format("Cannot find type: {0}", typeString)); //LOCSTR
                }
                XmlSerializer xs = new XmlSerializer(type);
                ret = xs.Deserialize(reader);

                reader.ReadEndElement();
            }
            else if (reader.LocalName == GENLISTELEMENTNAME)
            {
                reader.ReadStartElement();
                reader.MoveToContent();
                string typeString = reader.ReadElementString(TYPEMARKER);
                Type type = Type.GetType(typeString);
                if (type == null)
                {
                    throw new InvalidOperationException(String.Format("Cannot find type: {0}", typeString)); //LOCSTR
                }

                ret = (IList)Activator.CreateInstance(type);
                reader.MoveToContent();
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    object o = DeserializeFromXml(reader);
                    ((IList)ret).Add(o);
                    reader.MoveToContent();
                }

                reader.ReadEndElement();
            }
            else if (reader.LocalName == STRELEMENTNAME)
            {
                ret = reader.ReadElementString(STRELEMENTNAME);
            }
            else if (reader.LocalName == INTELEMENTNAME)
            {
                ret = int.Parse(reader.ReadElementString(INTELEMENTNAME));
            }
            else if (reader.LocalName == BOOLELEMENTNAME)
            {
                ret = bool.Parse(reader.ReadElementString(BOOLELEMENTNAME));
            }
            else if (reader.LocalName == DECELEMENTNAME)
            {
                ret = decimal.Parse(reader.ReadElementString(DECELEMENTNAME));
            }
            else if (reader.LocalName == NULLELEMENTNAME)
            {
                reader.ReadElementString(NULLELEMENTNAME);
                ret = null;
            }
            else
            {
                throw new InvalidOperationException(reader.LocalName);
            }

            return ret;
        }

        public static object DeserializeFromXml(string xml)
        {
            object ret = null;

            using (StringReader srdr = new StringReader(xml))
            {
                using (XmlReader xrdr = XmlReader.Create(srdr))
                {
                    xrdr.MoveToContent();
                    ret = DeserializeFromXml(xrdr);
                }
            }

            return ret;
        }

        private class LoggingSimpleSerializationBinder : SerializationBinder
        {
            private SerializationBinder binder = null;
            public LoggingSimpleSerializationBinder(SerializationBinder binder)
            {
                this.binder = binder;
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                Type ret = null;
                if (binder != null)
                {
                    ret = binder.BindToType(assemblyName, typeName);
                }
                else
                {
                    ret = Type.GetType(string.Concat(typeName, ", ", assemblyName));
                }

                System.Diagnostics.Debug.WriteLine("SerializationBinder: {0} ({1}) to {2}", typeName, assemblyName, (ret != null) ? ret.AssemblyQualifiedName : "null");

                return ret;
            }

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = null;

                if (binder != null)
                {
                    binder.BindToName(serializedType, out assemblyName, out typeName);
                }
                else
                {
                    base.BindToName(serializedType, out assemblyName, out typeName);
                }
            }
        }

        private static object DeserializeFromBase64(string base64)
        {
            object ret = null;

            if (!String.IsNullOrEmpty(base64))
            {
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(base64)))
                {
                    var versionMismatchAssemblyResolveHandler = new ResolveEventHandler((s,ea)=>
                        {
                            Assembly ret2 = null;

                            var nameWithoutVersion = new AssemblyName(ea.Name).Name;
                            foreach (var a in knownTypeAssemblies)
                            {
                                if (a.GetName().Name == nameWithoutVersion)
                                {
                                    ret2 = a;
                                    break;
                                }
                            }

                            return ret2;
                        });

                    try
                    {
                        AppDomain.CurrentDomain.AssemblyResolve += versionMismatchAssemblyResolveHandler;

                        BinaryFormatter bf = new BinaryFormatter();

                        bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
                        
                        ret = bf.Deserialize(ms);
                    }
                    catch (SerializationException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Serialization buffer content: {0}", base64);
                        throw;
                    }
                    finally
                    {
                        AppDomain.CurrentDomain.TypeResolve -= versionMismatchAssemblyResolveHandler;
                    }
                }
            }

            return ret;
        }
    }
}
