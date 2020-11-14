using System.Fabric;

using Microsoft.ServiceFabric.Services.Runtime;

namespace Alethic.Kademlia.ServiceFabric.Sample.Service
{

    public class SampleService : StatefulService
    {

        public SampleService(StatefulServiceContext serviceContext) :
            base(serviceContext)
        {

        }

    }

}
