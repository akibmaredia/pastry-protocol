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
        Message: string;
        FromNodeId: int;
        ToNodeId: int;
        HopCount: int;
    }

    type MessageType = 
        | InitSupervisor of InitSupervisor
        | JoinFinish
        | JoinNodesInDT
        | StartRouting
        | FinishRoute of FinishRoute
        | NodeNotFound
        | RouteNodeNotFound
        | InitPastryNode of InitPastryNode
        | AddFirstNode of AddFirstNode
        | Task of Task
        