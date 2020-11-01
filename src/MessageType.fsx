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

    type MessageType = 
        | InitSupervisor of InitSupervisor
        | JoinFinish
        | JoinNodesInDT
        | StartRouting
        | InitPastryNode of InitPastryNode
        | AddFirstNode of AddFirstNode