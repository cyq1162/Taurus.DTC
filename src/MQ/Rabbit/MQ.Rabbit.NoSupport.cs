

using System;
using System.Collections.Generic;

namespace Taurus.Plugin.DistributedTransaction
{

    internal class MQRabbit : MQ
    {
 
        public override MQType MQType
        {
            get
            {
                return MQType.Rabbit;
            }
        }
        public MQRabbit(string mqConn)
        {
            
        }

        public override bool Publish(MQMsg msg)
        {
            throw new NotImplementedException();
        }

        public override bool PublishBatch(List<MQMsg> msgList)
        {
            throw new NotImplementedException();
        }

        public override bool Listen(string queueNameOrGroupName, OnReceivedDelegate onReceivedDelegate, string bindExNameOrTopicName)
        {
            throw new NotImplementedException();
        }
    }
}
