#load @"./Utils.fsx"
#load @"./MessageType.fsx"

#r "nuget: Akka.FSharp"

open Utils
open MessageType

open Akka.FSharp
open Akka.Actor

open System
open System.Collections.Generic
open System.Threading

let isValidInput (numberOfNodes, numberOfRequests) =
    (numberOfNodes > 0) && (numberOfRequests > 0)

let system = ActorSystem.Create "System"

let mutable supervisor: IActorRef = null

let baseVal = 4

let PastryNode (mailbox: Actor<_>) = 
    let mutable id: int = 0
    let mutable maxRows: int = 0

    let leafSetSmaller = new List<int>()
    let leafSetLarger = new List<int>()
    
    let mutable routingTable = Array2D.zeroCreate<int> 1 1

    let mutable backCount: int = 0
    let mutable idSpace: int = 0

    let initRoutingTable() = 
        let initRow (row: int) = 
            [0 .. (baseVal - 1)]
            |> List.iter (fun col ->    routingTable.[row, col] <- -1)
            |> ignore
        
        routingTable <- Array2D.zeroCreate<int> maxRows baseVal

        [0 .. (maxRows - 1)]
        |> List.iter(fun row -> initRow (row))
        |> ignore

    let updateLeafSet(nodes: List<int>) = 
        for i in nodes do
            if (i > id && not (leafSetLarger.Contains(i))) then
                if (leafSetLarger.Count < baseVal) then
                    leafSetLarger.Add(i)
                else
                    let max = leafSetLarger |> Seq.max
                    if (i < max) then
                        leafSetLarger.Remove(max) |> ignore
                        leafSetLarger.Add(i)
            elif (i < id && not (leafSetSmaller.Contains(i))) then
                if (leafSetSmaller.Count < baseVal) then
                    leafSetSmaller.Add(i)
                else 
                    let min = leafSetSmaller |> Seq.min
                    if (i > min) then
                        leafSetSmaller.Remove(min) |> ignore
                        leafSetSmaller.Add(i)
            else ()

    let getFirstNonMatchingIndex(s1: string, s2: string) = 
        let len = String.length s1
        let mutable index = 0
        while index < len && s1.[index] = s2.[index] do index <- index + 1
        index

    let updateSamePrefixTableEntries(idInBaseVal: string, nodes: List<int>) = 
        for node in nodes do
            let iInBaseVal = Utils.numToBase(node, maxRows, baseVal)
            let index = getFirstNonMatchingIndex(idInBaseVal, iInBaseVal)
            if (routingTable.[index, (iInBaseVal.[index] |> string |> int)] = -1) then
                routingTable.[index, (iInBaseVal.[index] |> string |> int)] <- node

    let rec loop() = actor {
        let! message = mailbox.Receive()
        
        match message with
            | MessageType.InitPastryNode initMessage -> 
                id <- initMessage.Id
                maxRows <- initMessage.MaxRows
                idSpace <- Utils.powOf(baseVal, maxRows) |> int

                initRoutingTable()
            | MessageType.AddFirstNode addFirstNodeMessage -> 
                addFirstNodeMessage.NodeGroup.Remove(id) |> ignore
                
                let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                let col(row: int) = idInBaseVal.[row] |> string |> int
                [0 .. (maxRows - 1)]
                |> List.iter(fun row -> routingTable.[row, col(row)] <- id)
                |> ignore

                mailbox.Sender() <! MessageType.JoinFinish
            | MessageType.JoinTask taskInfo -> 
                let sendUpdateRowMessage (index) =
                    let rowInfo: MessageType.RowInfo = {
                        RowIndex = index;
                        RowData = new List<int>(routingTable.[index, *]);
                    }
                    let nodeName = "/user/PastryNode" + (taskInfo.ToNodeId |> string)
                    (select nodeName system) <! MessageType.UpdateRow rowInfo
                
                let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                let toIdInBaseVal = Utils.numToBase(taskInfo.ToNodeId, maxRows, baseVal)
                let nonMatchingIndex = getFirstNonMatchingIndex(idInBaseVal, toIdInBaseVal)
                if (taskInfo.HopCount = -1 && nonMatchingIndex > 0) then
                    [0 .. (nonMatchingIndex - 1)]
                    |> List.iter (fun i ->  sendUpdateRowMessage(i))
                    |> ignore
                sendUpdateRowMessage(nonMatchingIndex)

                let sendUpdateNeighborMessage() = 
                    let nodeSet = new List<int>()
                    nodeSet.Add(id)
                    for node in leafSetSmaller do nodeSet.Add(node)
                    for node in leafSetLarger do nodeSet.Add(node)
                    let nodeName = "/user/PastryNode" + (taskInfo.ToNodeId |> string)
                    let neighborInfo: MessageType.NeighborInfo = { NodeIdList = nodeSet; }
                    (select nodeName system) <! MessageType.UpdateNeighborSet neighborInfo

                let nextTaskInfo: MessageType.Task = {
                    FromNodeId = taskInfo.FromNodeId;
                    ToNodeId = taskInfo.ToNodeId;
                    HopCount = taskInfo.HopCount + 1;
                }

                if ((leafSetSmaller.Count > 0 && taskInfo.ToNodeId >= (leafSetSmaller |> Seq.min) && taskInfo.ToNodeId <= id) || 
                    (leafSetLarger.Count > 0 && taskInfo.ToNodeId <= (leafSetLarger |> Seq.max) && taskInfo.ToNodeId >= id)) then
                    let mutable diff = idSpace + 10
                    let mutable nearest = -1
                    if (taskInfo.ToNodeId < id) then
                        for node in leafSetSmaller do
                            if (abs (taskInfo.ToNodeId - node) < diff) then
                                nearest <- node
                                diff <- abs (taskInfo.ToNodeId - node)
                    else
                        for node in leafSetLarger do
                            if (abs (taskInfo.ToNodeId - node) < diff) then
                                nearest <- node
                                diff <- abs (taskInfo.ToNodeId - node)

                    if (abs (taskInfo.ToNodeId - id) > diff) then
                        let nodeName = "/user/PastryNode" + (nearest |> string)
                        (select nodeName system) <! MessageType.JoinTask nextTaskInfo
                    else
                        sendUpdateNeighborMessage()
                else if (leafSetSmaller.Count > 0 && leafSetSmaller.Count < baseVal && taskInfo.ToNodeId < (leafSetSmaller |> Seq.min)) then
                    let nodeName = "/user/PastryNode" + ((leafSetSmaller |> Seq.min) |> string)
                    (select nodeName system) <! MessageType.JoinTask nextTaskInfo
                else if (leafSetLarger.Count > 0 && leafSetLarger.Count < baseVal && taskInfo.ToNodeId > (leafSetLarger |> Seq.max)) then
                    let nodeName = "/user/PastryNode" + ((leafSetLarger |> Seq.max) |> string)
                    (select nodeName system) <! MessageType.JoinTask nextTaskInfo
                else if ((leafSetSmaller.Count = 0 && taskInfo.ToNodeId < id) || (leafSetLarger.Count = 0 && taskInfo.ToNodeId > id)) then
                    sendUpdateNeighborMessage()
                else if (routingTable.[nonMatchingIndex, (toIdInBaseVal.[nonMatchingIndex] |> string |> int)] <> -1) then
                    let nodeName = "/user/PastryNode" + ((routingTable.[nonMatchingIndex, (toIdInBaseVal.[nonMatchingIndex] |> string |> int)]) |> string)
                    (select nodeName system) <! MessageType.JoinTask nextTaskInfo
                else 
                    let mutable diff = idSpace + 10
                    let mutable nearest = -1
                    for i in [0 .. (baseVal - 1)] do
                        if ((routingTable.[nonMatchingIndex, i] <> -1) && (abs (routingTable.[nonMatchingIndex, i] - taskInfo.ToNodeId) < diff)) then
                            diff <- abs (routingTable.[nonMatchingIndex, i] - taskInfo.ToNodeId)
                            nearest <- routingTable.[nonMatchingIndex, i]

                    if nearest <> -1 then
                        if nearest = id then
                            if taskInfo.ToNodeId > id then
                                let nodeName = "/user/PastryNode" + ((leafSetLarger |> Seq.max) |> string)
                                (select nodeName system) <! MessageType.JoinTask nextTaskInfo
                                supervisor <! MessageType.NodeNotFound
                            else if taskInfo.ToNodeId < id then
                                let nodeName = "/user/PastryNode" + ((leafSetSmaller |> Seq.min) |> string)
                                (select nodeName system) <! MessageType.JoinTask nextTaskInfo
                                supervisor <! MessageType.NodeNotFound
                            else ()
                        else 
                            let nodeName = "/user/PastryNode" + (nearest |> string)
                            (select nodeName system) <! MessageType.JoinTask nextTaskInfo
                    else ()
            | MessageType.RouteTask taskInfo -> 
                if (taskInfo.ToNodeId = id) then
                    supervisor <! MessageType.FinishRoute { NumberOfHops = taskInfo.HopCount + 1; }
                else
                    let nextTaskInfo: MessageType.Task = {
                        FromNodeId = taskInfo.FromNodeId;
                        ToNodeId = taskInfo.ToNodeId;
                        HopCount = taskInfo.HopCount + 1;
                    }

                    let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                    let toIdInBaseVal = Utils.numToBase(taskInfo.ToNodeId, maxRows, baseVal)
                    let nonMatchingIndex = getFirstNonMatchingIndex(idInBaseVal, toIdInBaseVal)
                    if ((leafSetSmaller.Count > 0 && taskInfo.ToNodeId >= (leafSetSmaller |> Seq.min) && taskInfo.ToNodeId < id) || 
                        (leafSetLarger.Count > 0 && taskInfo.ToNodeId <= (leafSetLarger |> Seq.max) && taskInfo.ToNodeId > id)) then
                        let mutable diff = idSpace + 10
                        let mutable nearest = -1
                        if (taskInfo.ToNodeId < id) then
                            for node in leafSetSmaller do
                                if (abs (taskInfo.ToNodeId - node) < diff) then
                                    nearest <- node
                                    diff <- abs (taskInfo.ToNodeId - node)
                        else
                            for node in leafSetLarger do
                                if (abs (taskInfo.ToNodeId - node) < diff) then
                                    nearest <- node
                                    diff <- abs (taskInfo.ToNodeId - node)

                        if (abs (taskInfo.ToNodeId - id) > diff) then
                            let nodeName = "/user/PastryNode" + (nearest |> string)
                            (select nodeName system) <! MessageType.RouteTask nextTaskInfo
                        else
                            supervisor <! MessageType.FinishRoute { NumberOfHops = taskInfo.HopCount + 1; }
                    else if (leafSetSmaller.Count > 0 && leafSetSmaller.Count < baseVal && taskInfo.ToNodeId < (leafSetSmaller |> Seq.min)) then
                        let nodeName = "/user/PastryNode" + ((leafSetSmaller |> Seq.min) |> string)
                        (select nodeName system) <! MessageType.RouteTask nextTaskInfo
                    else if (leafSetLarger.Count > 0 && leafSetLarger.Count < baseVal && taskInfo.ToNodeId > (leafSetLarger |> Seq.max)) then
                        let nodeName = "/user/PastryNode" + ((leafSetLarger |> Seq.max) |> string)
                        (select nodeName system) <! MessageType.RouteTask nextTaskInfo
                    else if ((leafSetSmaller.Count = 0 && taskInfo.ToNodeId < id) || (leafSetLarger.Count = 0 && taskInfo.ToNodeId > id)) then
                        supervisor <! MessageType.FinishRoute { NumberOfHops = taskInfo.HopCount + 1; }
                    else if (routingTable.[nonMatchingIndex, (toIdInBaseVal.[nonMatchingIndex] |> string |> int)] <> -1) then
                        let nodeName = "/user/PastryNode" + ((routingTable.[nonMatchingIndex, (toIdInBaseVal.[nonMatchingIndex] |> string |> int)]) |> string)
                        (select nodeName system) <! MessageType.RouteTask nextTaskInfo
                    else 
                        let mutable diff = idSpace + 10
                        let mutable nearest = -1
                        for i in [0 .. (baseVal - 1)] do
                            if ((routingTable.[nonMatchingIndex, i] <> -1) && (abs (routingTable.[nonMatchingIndex, i] - taskInfo.ToNodeId) < diff)) then
                                diff <- abs (routingTable.[nonMatchingIndex, i] - taskInfo.ToNodeId)
                                nearest <- routingTable.[nonMatchingIndex, i]

                        if nearest <> -1 then
                            if nearest = id then
                                if taskInfo.ToNodeId > id then
                                    let nodeName = "/user/PastryNode" + ((leafSetLarger |> Seq.max) |> string)
                                    (select nodeName system) <! MessageType.RouteTask nextTaskInfo
                                    supervisor <! MessageType.RouteToNodeNotFound
                                else if taskInfo.ToNodeId < id then
                                    let nodeName = "/user/PastryNode" + ((leafSetSmaller |> Seq.min) |> string)
                                    (select nodeName system) <! MessageType.RouteTask nextTaskInfo
                                    supervisor <! MessageType.RouteToNodeNotFound
                                else ()
                            else 
                                let nodeName = "/user/PastryNode" + (nearest |> string)
                                (select nodeName system) <! MessageType.RouteTask nextTaskInfo
                        else ()
            | MessageType.UpdateRow rowInfo -> 
                let updateRoutingTableEntry (row, col, value) = 
                    if (routingTable.[row, col] = -1) then
                        routingTable.[row, col] <- value
                    else ()

                [0 .. baseVal - 1]
                |> List.iter (fun i ->  updateRoutingTableEntry (rowInfo.RowIndex, i, rowInfo.RowData.[i]))
                |> ignore
            | MessageType.UpdateNeighborSet neighborInfo -> 
                updateLeafSet (neighborInfo.NodeIdList)

                let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                updateSamePrefixTableEntries(idInBaseVal, neighborInfo.NodeIdList)

                for node in leafSetSmaller do 
                    backCount <- backCount + 1
                    let nodeName = "/user/PastryNode" + (node |> string)
                    (select nodeName system) <! MessageType.SendAckToSupervisor { NewNodeId = id; }

                for node in leafSetLarger do 
                    backCount <- backCount + 1
                    let nodeName = "/user/PastryNode" + (node |> string)
                    (select nodeName system) <! MessageType.SendAckToSupervisor { NewNodeId = id; }
                
                for i in [0 .. (maxRows - 1)] do
                    for j in [0 .. (baseVal - 1)] do
                        if (routingTable.[i, j] <> -1) then
                            backCount <- backCount + 1
                            let nodeName = "/user/PastryNode" + (routingTable.[i, j] |> string)
                            (select nodeName system) <! MessageType.SendAckToSupervisor { NewNodeId = id; }

                let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                let col(row: int) = idInBaseVal.[row] |> string |> int
                [0 .. (maxRows - 1)]
                |> List.iter(fun row -> routingTable.[row, col(row)] <- id)
                |> ignore
            | MessageType.SendAckToSupervisor ackInfo -> 
                let tempList = new List<int>()
                tempList.Add(ackInfo.NewNodeId)
                
                updateLeafSet(tempList)

                let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                updateSamePrefixTableEntries(idInBaseVal, tempList)

                mailbox.Sender() <! MessageType.Ack
            | MessageType.Ack -> 
                backCount <- backCount - 1
                if backCount = 0 then 
                    supervisor <! MessageType.JoinFinish
            | MessageType.StartRouting routingInfo -> 
                for i in [1 .. routingInfo.RequestCount] do
                    Thread.Sleep(200)
                    let taskInfo: MessageType.Task = {
                        FromNodeId = id;
                        ToNodeId = Random().Next(routingInfo.NodeCount)
                        HopCount = -1;
                    }
                    mailbox.Self <! MessageType.RouteTask taskInfo
            | MessageType.Die -> 
                (select "/user/PastryNode*" system) <! MessageType.RemoveNodeDetails { NodeId = id; }
                mailbox.Self <! PoisonPill.Instance
            | MessageType.RemoveNodeDetails nodeToRemove -> 
                if id <> nodeToRemove.NodeId then
                    if (nodeToRemove.NodeId > id && leafSetLarger.Contains(nodeToRemove.NodeId)) then
                        leafSetLarger.Remove(nodeToRemove.NodeId) |> ignore
                        if leafSetLarger.Count > 0 then
                            let nodeName = "/user/PastryNode" + ((leafSetLarger |> Seq.max) |> string)
                            (select nodeName system) <! MessageType.RemoveNodeDetails2 { NodeId = nodeToRemove.NodeId; }

                    if (nodeToRemove.NodeId < id && leafSetSmaller.Contains(nodeToRemove.NodeId)) then
                        leafSetSmaller.Remove(nodeToRemove.NodeId) |> ignore
                        if leafSetSmaller.Count > 0 then
                            let nodeName = "/user/PastryNode" + ((leafSetSmaller |> Seq.min) |> string)
                            (select nodeName system) <! MessageType.RemoveNodeDetails2 { NodeId = nodeToRemove.NodeId; }

                    let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                    let removeIdInBaseVal = Utils.numToBase(nodeToRemove.NodeId, maxRows, baseVal)
                    let nonMatchingIndex = getFirstNonMatchingIndex(idInBaseVal, removeIdInBaseVal)
                    if (routingTable.[nonMatchingIndex, (removeIdInBaseVal.[nonMatchingIndex] |> string |> int)] = nodeToRemove.NodeId) then
                        routingTable.[nonMatchingIndex, (removeIdInBaseVal.[nonMatchingIndex] |> string |> int)] <- -1
                        for i in [0 .. (baseVal - 1)] do
                            if (routingTable.[nonMatchingIndex, i] <> id && 
                                routingTable.[nonMatchingIndex, i] <> nodeToRemove.NodeId && 
                                routingTable.[nonMatchingIndex, i] <> -1) then
                                let nodeName = "/user/PastryNode" + (routingTable.[nonMatchingIndex, i] |> string)
                                let tableInfo: MessageType.RoutingTableInfo = {
                                    Row = nonMatchingIndex;
                                    Col = (removeIdInBaseVal.[nonMatchingIndex] |> string |> int)
                                    Val = -1
                                }
                                (select nodeName system) <! MessageType.CheckRoutingTable tableInfo
                else ()
            | MessageType.RemoveNodeDetails2 nodeToRemove -> 
                let tempList = new List<int>()
                for node in leafSetSmaller do tempList.Add(node)
                for node in leafSetLarger do tempList.Add(node)
                tempList.Remove(nodeToRemove.NodeId) |> ignore
                mailbox.Sender() <! MessageType.RecoverLeafNodes { NodeList = new List<int>(tempList); DeadNodeId = nodeToRemove.NodeId; }
            | MessageType.RecoverLeafNodes recoverLeafInfo -> 
                for node in recoverLeafInfo.NodeList do
                    if leafSetLarger.Contains(recoverLeafInfo.DeadNodeId) then 
                        leafSetLarger.Remove(recoverLeafInfo.DeadNodeId) |> ignore
                    if leafSetSmaller.Contains(recoverLeafInfo.DeadNodeId) then 
                        leafSetSmaller.Remove(recoverLeafInfo.DeadNodeId) |> ignore
                    if (node > id && not (leafSetLarger.Contains(node))) then
                        if (leafSetLarger.Count < baseVal) then
                            leafSetLarger.Add(node)
                        else
                            if (node < (leafSetLarger |> Seq.max)) then
                                leafSetLarger.Add(node)
                            else ()
                    else if (node < id && not (leafSetSmaller.Contains(node))) then
                        if (leafSetSmaller.Count < baseVal) then
                            leafSetSmaller.Add(node)
                        else
                            if (node > (leafSetSmaller |> Seq.min)) then
                                leafSetSmaller.Add(node)
                            else ()
                    else ()
            | MessageType.CheckRoutingTable tableInfo -> 
                if (routingTable.[tableInfo.Row, tableInfo.Col] <> -1) then
                    let tableInfo: MessageType.RoutingTableInfo = {
                        Row = tableInfo.Row;
                        Col = tableInfo.Col;
                        Val = routingTable.[tableInfo.Row, tableInfo.Col]
                    }
                    mailbox.Sender() <! MessageType.RecoverRoutingTable tableInfo
                else ()
            | MessageType.RecoverRoutingTable tableInfo -> 
                if (routingTable.[tableInfo.Row, tableInfo.Col] <> -1) then
                    routingTable.[tableInfo.Row, tableInfo.Col] <- tableInfo.Val
                else ()
            | _ -> ()
        
        return! loop()
    }
    loop()

let Supervisor (mailbox: Actor<_>) = 
    let mutable totalNumberOfNodes: int = 0
    let mutable numberOfRequests: int = 0
    let mutable numberOfFailureNodes: int = 0
    
    let mutable numberOfNodesJoined: int = 0
    let mutable numberOfNodesRouted: int = 0
    let mutable numberOfRouteNotFound: int = 0
    let mutable numberOfNodesNotInBoth: int = 0

    let mutable numberOfHops: int = 0

    let mutable maxRows: int = 0
    let mutable maxNodes: int = 0

    let nodeList = new List<int>()
    let groupOne = new List<int>()

    let mutable systemRef = null

    let initNodeList () = 
        let random = Random()

        let shuffle () =
            [0 .. (maxNodes - 2)]
            |> List.iter (fun i ->  let j = random.Next(i, maxNodes)
                                    let temp = nodeList.[i]
                                    nodeList.[i] <- nodeList.[j]
                                    nodeList.[j] <- temp)
            |> ignore
        
        [0 .. (maxNodes - 1)]
        |> List.iter(fun i ->   nodeList.Add(i))
        |> ignore
        
        shuffle ()

    let initPastryNodes () = 
        [0 .. (totalNumberOfNodes - 1)]
        |> List.iter (fun i ->  let name = "PastryNode" + (nodeList.[i] |> string)
                                let node = spawn system name PastryNode
                                let initMessage: MessageType.InitPastryNode = {
                                    Id = nodeList.[i];
                                    MaxRows = maxRows;
                                }
                                node <! MessageType.InitPastryNode initMessage)
        |> ignore

    let initPastryWork () =
        let firstNodeName = "/user/PastryNode" + (nodeList.[0] |> string)
        let addFirstNodeMessage: MessageType.AddFirstNode = {
            NodeGroup = new List<int>(groupOne);
        }
        (select firstNodeName system) <! MessageType.AddFirstNode addFirstNodeMessage

    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with 
            | MessageType.InitSupervisorWithFailure initMessage -> 
                systemRef <- mailbox.Sender()

                totalNumberOfNodes <- initMessage.NumberOfNodes
                numberOfRequests <- initMessage.NumberOfRequests
                numberOfFailureNodes <- initMessage.NumberOfFailureNodes

                maxRows <- (ceil ((Utils.logOf initMessage.NumberOfNodes) / (Utils.logOf baseVal))) |> int
                maxNodes <- Utils.powOf (baseVal, maxRows) |> int

                initNodeList()

                groupOne.Add(nodeList.[0])

                initPastryNodes()

                initPastryWork()
            | MessageType.JoinFinish -> 
                numberOfNodesJoined <- numberOfNodesJoined + 1
                if (numberOfNodesJoined = totalNumberOfNodes) then
                    mailbox.Self <! MessageType.CreateFailures
                else
                    mailbox.Self <! MessageType.JoinNodesInDT
            | MessageType.JoinNodesInDT -> 
                let nodeId = nodeList.[Random().Next(numberOfNodesJoined)]
                let nodeName = "/user/PastryNode" + (nodeId |> string)
                let node = select nodeName system
                let taskInfo: MessageType.Task = {
                    FromNodeId = nodeId;
                    ToNodeId = nodeList.[numberOfNodesJoined];
                    HopCount = -1;
                }
                node <! MessageType.JoinTask taskInfo
            | MessageType.CreateFailures -> 
                for i in [0 .. (numberOfFailureNodes - 1)] do
                    let nodeName = "/user/PastryNode" + (nodeList.[Random().Next(numberOfNodesJoined)] |> string)
                    (select nodeName system) <! MessageType.Die
                Thread.Sleep(1000)
                mailbox.Self <! MessageType.StartRoutingSupervisor
            | MessageType.StartRoutingSupervisor -> 
                (select "/user/PastryNode*" system) <! MessageType.StartRouting { NodeCount = totalNumberOfNodes; RequestCount = numberOfRequests; }
            | MessageType.FinishRoute finishRouteMessage -> 
                numberOfNodesRouted <- numberOfNodesRouted + 1
                numberOfHops <- numberOfHops + finishRouteMessage.NumberOfHops
                if (numberOfNodesRouted = (totalNumberOfNodes - numberOfFailureNodes) * numberOfRequests) then
                    let message = "Routing Finished!" + 
                                    "\nTotal Number of Routes: " + (numberOfNodesRouted |> string) + 
                                    "\nTotal Number of Hops: " + (numberOfHops |> string) + 
                                    "\nAverage Number of Hops per Route: " + 
                                    ((Utils.toDouble numberOfHops / Utils.toDouble numberOfNodesRouted) |> string)
                    systemRef <! message
            | MessageType.NodeNotFound -> 
                numberOfNodesNotInBoth <- numberOfNodesNotInBoth + 1
            | MessageType.RouteToNodeNotFound -> 
                numberOfRouteNotFound <- numberOfRouteNotFound + 1
            | _ -> ()
        
        return! loop()
    }

    loop()

let main (numberOfNodes, numberOfRequests, numberOfFailureNodes) = 
    if not (isValidInput (numberOfNodes, numberOfRequests)) then
        printfn "Error: Invalid Input"
    else
        supervisor <- spawn system "Supervisor" Supervisor
        
        let initMessage: MessageType.InitSupervisorWithFailure = {
            NumberOfNodes = numberOfNodes;
            NumberOfRequests = numberOfRequests;
            NumberOfFailureNodes = numberOfFailureNodes;
        }

        let response = Async.RunSynchronously(supervisor <? MessageType.InitSupervisorWithFailure initMessage)
        printfn "%A" response

        system.Terminate() |> ignore

// Read command line inputs and pass on to the driver function
match fsi.CommandLineArgs with
    | [|_; numberOfNodes; numberOfRequests|] -> 
        let numNodes = Utils.strToInt numberOfNodes
        main (numNodes, (Utils.strToInt numberOfRequests), numNodes / 100)
    | [|_; numberOfNodes; numberOfRequests; numberOfNodesToFail|] -> 
        main ((Utils.strToInt numberOfNodes), (Utils.strToInt numberOfRequests), (Utils.strToInt numberOfNodesToFail))
    | _ -> printfn "Error: Invalid Arguments"