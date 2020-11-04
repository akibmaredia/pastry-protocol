namespace MessageType

open System.Collections.Generic

module MessageType = 
    type InitSupervisor = {
        NumberOfNodes: int;
        NumberOfRequests: int;
    }

    type InitSupervisorWithFailure = {
        NumberOfNodes: int;
        NumberOfRequests: int;
        NumberOfFailureNodes: int;
    }

    type InitPastryNode = {
        Id: int;
        MaxRows: int;
    }

    type AddFirstNode = {
        NodeGroup: List<int>;
    }

    type FinishRoute = {
        NumberOfHops: int;
    }

    type Task = {
        FromNodeId: int;
        ToNodeId: int;
        HopCount: int;
    }

    type RowInfo = {
        RowIndex: int;
        RowData: List<int>;
    }

    type NeighborInfo = {
        NodeIdList: List<int>;
    }

    type AckInfo = {
        NewNodeId: int;
    }

    type RoutingInfo = {
        NodeCount: int;
        RequestCount: int;
    }

    type RemoveNodeInfo = {
        NodeId: int;
    }

    type RoutingTableInfo = {
        Row: int;
        Col: int;
        Val: int;
    }

    type RecoverLeafNodeInfo = {
        NodeList: List<int>;
        DeadNodeId: int;
    }

    type MessageType = 
        | InitSupervisorWithFailure of InitSupervisorWithFailure
        | CreateFailures
        | InitSupervisor of InitSupervisor
        | JoinFinish
        | JoinNodesInDT
        | StartRouting of RoutingInfo
        | StartRoutingSupervisor
        | FinishRoute of FinishRoute
        | NodeNotFound
        | RouteToNodeNotFound
        | InitPastryNode of InitPastryNode
        | AddFirstNode of AddFirstNode
        | JoinTask of Task
        | RouteTask of Task
        | UpdateRow of RowInfo
        | UpdateNeighborSet of NeighborInfo
        | SendAckToSupervisor of AckInfo
        | Ack
        | Die
        | RemoveNodeDetails of RemoveNodeInfo
        | RemoveNodeDetails2 of RemoveNodeInfo
        | CheckRoutingTable of RoutingTableInfo
        | RecoverRoutingTable of RoutingTableInfo
        | RecoverLeafNodes of RecoverLeafNodeInfo
        