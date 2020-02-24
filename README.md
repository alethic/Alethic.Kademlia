# Cogito.Kademlia

This library is an attempt to build an extensible Kademlia implementation in C# running on both .NET Framework and .NET Core. It is a primary goal that the library be easily embeddable into other software.

## Architecture

The `Cogito.Kademlia` library provides abstractions for most of the components and logic of a Kademlia implementation. Each of these abstractions has a default implementation; however, those default implementations can be substituted out.

The primary feature is the usage of generic `KNodeId` specifications for the Kademlia node ID structure. This allows the consumer to customize to one of the included NodeId implementations (32 bits through 256 bits), or substitute their own. This is an unmanaged structure whos in-memory layout should match the logical Kademlia node ID requirements of interpretation as a large integer value. Big endian.

The `IKEngine` component is the central "tying together" component. It's goal is to manage the activities related to incoming network requests, and determine their responses. It also provides a user-visible API surface for high-level Kademlia operations.

The `IKProtocol` component implements a network format. One sample network format exists in the `Cogito.Kademlia.Protocols.Protobuf` library, which implements a simple UDP protocol using Google Protocol Buffer format packets.

Every Kademlia implementation requires some sort of router. The `IKRouter` interface provides this. This governs how the storage and resolution of Kademlia network peers. Operations are provided to resolve peer connection information from the router, and to get the next known peer (route) for a given key ID. One default implementation is provided: `KFixedTableRouter` which implemnts a fixed binary prefix table as described in the original Kademlia paper. Obviously, a dynamic tree router would be desirable.

The `IKEndpoint` abstraction represents an endpoint at which another Kademlia peer would be accessible. The endpoints are delivered by the `IKProtocol` which is reponsible for communicating the endpoints over the network to other peers; and reporting the available local endpoints to the engine itself.

The Kademlia "iterative lookup" algorithm is provided by an implementation of `IKLookup`. Only one implementation of this exists at present: `KLookup`, though it is imagined versions could be tailored to custom network requirements.

`IKEndpointInvoker` wraps communication with an `IKEndpoint`, adding a retry policy; and dispatching the attempt potentially multiple endpoints per-peer.

`IKMessageDecoder` and `IKMessageEncoder` classes implement the wire formats. Again, implementations for Protocol Buffers are available in `Cogito.Kademlia.Protocols.Protobuf`.

## UDP Multicast

An example `KUdpMulticastDiscovery` class is provided to seed peer nodes using UDP multicast requests.

