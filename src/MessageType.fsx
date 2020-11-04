namespace MessageType

open System.Collections.Generic

module MessageType = 
    type InitSupervisor = {
        NumberOfNodes: int;
        NumberOfRequests: int;
    }

    type InitPastryNode = {
        Id: int;
        NumberOfNodes: int;
        NumberOfRequests: int;
        MaxRows: int;
    }

    type AddFirstNode = {
        NodeGroup: List<int>;
    }

    type FinishRoute = {
        FromNodeId: int;
        ToNodeId: int;
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

    type MessageType = 
        | InitSupervisor of InitSupervisor
        | JoinFinish
        | JoinNodesInDT
        | StartRouting
        | FinishRoute of FinishRoute
        | NodeNotFound
        | RouteToNodeNotFound
        | InitPastryNode of InitPastryNode
        | AddFirstNode of AddFirstNode
        | JoinTask of Task
        | RouteTask of Task
        | UpdateRow of RowInfo
        | UpdateNeighborSet of NeighborInfo
        | SendAckToSupervisor
        