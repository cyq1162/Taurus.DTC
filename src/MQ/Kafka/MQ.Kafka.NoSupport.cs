
using System.Collections.Generic;

namespace Taurus.Plugin.DistributedTransaction
{

    internal class MQKafka : MQ
    {
        bool isClient = false;
        string servers = string.Empty;
        public MQKafka(string mqConn, bool isClient)
        {
            servers = mqConn;
            this.isClient = isClient;
        }
        public override MQType MQType =>  MQType.Kafka;

        public override bool Listen(string queueOrTopic, OnReceivedDelegate onReceivedDelegate, string exNameOrGroup, bool isAutoDelete)
        {
            throw new System.NotImplementedException();
        }

        public override bool Publish(MQMsg msg)
        {
            throw new System.NotImplementedException();
        }

        public override bool PublishBatch(List<MQMsg> msgList)
        {
            throw new System.NotImplementedException();
        }
    }
}
