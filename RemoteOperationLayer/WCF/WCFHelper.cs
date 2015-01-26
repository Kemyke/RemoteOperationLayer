using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Xml;
using System.Runtime.Serialization;

namespace ArdinRemoteOperations.WCF
{
    /// <summary>
    /// Based on http://social.msdn.microsoft.com/Forums/en-US/wcf/thread/86bcc9bf-bfbb-46c7-ad76-84a59bf48114
    /// </summary>
    public static class WCFHelper
    {
        public static void ApplyWCFEndpointLimits(ServiceEndpoint oEndPoint, int nMaxItemsInObjectGraph)
        {
            foreach (OperationDescription op in oEndPoint.Contract.Operations)
            {
                DataContractSerializerOperationBehavior dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>() as DataContractSerializerOperationBehavior;
                if (dataContractBehavior != null)
                {
                    dataContractBehavior.MaxItemsInObjectGraph = nMaxItemsInObjectGraph;
                }
            }
        }

        public static void ApplyWCFBindingLimits(Binding oBinding, int nMaxSizeInBytes, int nTimeoutInSecs)
        {
            oBinding.ReceiveTimeout = TimeSpan.MaxValue;
            oBinding.SendTimeout = TimeSpan.MaxValue;

            TimeSpan ts = (nTimeoutInSecs > 0) ? new TimeSpan(0, 0, nTimeoutInSecs) : TimeSpan.MaxValue;
            oBinding.OpenTimeout = ts;
            oBinding.CloseTimeout = ts;

            if (oBinding is BasicHttpBinding)
            {
                BasicHttpBinding oBasicHttpBinding = oBinding as BasicHttpBinding;
                oBasicHttpBinding.MaxBufferSize = (nMaxSizeInBytes > 0) ? nMaxSizeInBytes : int.MaxValue; ;
                oBasicHttpBinding.MaxBufferPoolSize = long.MaxValue;
                oBasicHttpBinding.MaxReceivedMessageSize = (nMaxSizeInBytes > 0) ? nMaxSizeInBytes : int.MaxValue;
                ApplyUnlimitedReaderQuotaConfiguration(oBasicHttpBinding.ReaderQuotas);
            }
            else if (oBinding is WSHttpBinding)
            {
                WSHttpBinding oWSHttpBinding = oBinding as WSHttpBinding;
                oWSHttpBinding.MaxBufferPoolSize = long.MaxValue;
                oWSHttpBinding.MaxReceivedMessageSize = (nMaxSizeInBytes > 0) ? nMaxSizeInBytes : int.MaxValue;
                ApplyUnlimitedReaderQuotaConfiguration(oWSHttpBinding.ReaderQuotas);
            }
            else if (oBinding is NetTcpBinding)
            {
                NetTcpBinding oNetTcpBinding = oBinding as NetTcpBinding;
                oNetTcpBinding.MaxBufferPoolSize = long.MaxValue;
                oNetTcpBinding.MaxBufferSize = (nMaxSizeInBytes > 0) ? nMaxSizeInBytes : int.MaxValue;
                oNetTcpBinding.MaxReceivedMessageSize = (nMaxSizeInBytes > 0) ? nMaxSizeInBytes : int.MaxValue;
                ApplyUnlimitedReaderQuotaConfiguration(oNetTcpBinding.ReaderQuotas);
            }
            else
            {
                throw new NotImplementedException(oBinding.GetType().ToString());
            }
        }

        private static void ApplyUnlimitedReaderQuotaConfiguration(XmlDictionaryReaderQuotas readerQuotas)
        {
            XmlDictionaryReaderQuotas oMax = XmlDictionaryReaderQuotas.Max;

            readerQuotas.MaxArrayLength = oMax.MaxArrayLength;
            readerQuotas.MaxBytesPerRead = oMax.MaxBytesPerRead;
            readerQuotas.MaxDepth = oMax.MaxDepth;
            readerQuotas.MaxNameTableCharCount = oMax.MaxNameTableCharCount;
            readerQuotas.MaxStringContentLength = oMax.MaxStringContentLength;
        }
    }
}
