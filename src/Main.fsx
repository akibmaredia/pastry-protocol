#load @"./Utils.fsx"
#load @"./MessageType.fsx"

#r "nuget: Akka.FSharp"

open Utils
open MessageType

open Akka.FSharp
open Akka.Actor

open System
open System.Collections.Generic

let isValidInput (numberOfNodes, numberOfRequests) =
    (numberOfNodes > 0) && (numberOfRequests > 0)

let system = ActorSystem.Create "System"

let baseVal = 4

let PastryNode (mailbox: Actor<_>) = 
    let mutable id: int = 0
    let mutable numberOfNodes: int = 0
    let mutable numberOfRequests: int = 0
    let mutable maxRows: int = 0

    let leafSetSmaller = new List<int>()
    let leafSetLarger = new List<int>()
    
    let mutable routingTable = Array2D.zeroCreate<int> 1 1

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
        let mutable i = 0
        while i < len && s1.[i] = s2.[i] do
            i <- i + 1
        i

    let updateSamePrefixTableEntries(idInBaseVal: string, nodes: List<int>) = 
        for i in nodes do
            let iInBaseVal = Utils.numToBase(i, maxRows, baseVal)
            let index = getFirstNonMatchingIndex(idInBaseVal, iInBaseVal)
            if (routingTable.[index, (iInBaseVal.[index] |> string |> int)] = -1) then
                routingTable.[index, (iInBaseVal.[index] |> string |> int)] <- i

    let rec loop() = actor {
        let! message = mailbox.Receive()
        
        match message with
            | MessageType.InitPastryNode initMessage -> 
                id <- initMessage.Id
                numberOfNodes <- initMessage.NumberOfNodes
                numberOfRequests <- initMessage.NumberOfRequests
                maxRows <- initMessage.MaxRows

                initRoutingTable()
            | MessageType.AddFirstNode addFirstNodeMessage -> 
                addFirstNodeMessage.NodeGroup.Remove(id) |> ignore

                updateLeafSet(addFirstNodeMessage.NodeGroup)
                
                let idInBaseVal = Utils.numToBase(id, maxRows, baseVal)
                
                updateSamePrefixTableEntries(idInBaseVal, addFirstNodeMessage.NodeGroup)

                let col(row: int) = 
                    idInBaseVal.[row] |> string |> int

                [0 .. (maxRows - 1)]
                |> List.iter(fun row -> routingTable.[row, col(row)] <- id)
                |> ignore

                mailbox.Sender() <! MessageType.JoinFinish
            | MessageType.Task taskInfo -> 
                printfn "%A" taskInfo
            | _ -> ()
        
        return! loop()
    }
    loop()

let Supervisor (mailbox: Actor<_>) = 
    let mutable totalNumberOfNodes: int = 0
    let mutable numberOfRequests: int = 0
    
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

    let initPastryNodes (numberOfNodes, numberOfRequests) = 
        [0 .. (numberOfNodes - 1)]
        |> List.iter (fun i ->  let name = "PastryNode" + (i |> string)
                                let node = spawn system name PastryNode
                                let initMessage: MessageType.InitPastryNode = {
                                    Id = nodeList.[i];
                                    NumberOfNodes = numberOfNodes;
                                    NumberOfRequests = numberOfRequests;
                                    MaxRows = maxRows;
                                }
                                node <! MessageType.InitPastryNode initMessage)
        |> ignore

    let initPastryWork () =
        let firstNodeName = "/user/PastryNode" + (nodeList.[0] |> string)
        let firstNode = select firstNodeName system
        let addFirstNodeMessage: MessageType.AddFirstNode = {
            NodeGroup = new List<int>(groupOne);
        }
        firstNode <! MessageType.AddFirstNode addFirstNodeMessage

    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with 
            | MessageType.InitSupervisor initMessage -> 
                systemRef <- mailbox.Sender()

                totalNumberOfNodes <- initMessage.NumberOfNodes
                numberOfRequests <- initMessage.NumberOfRequests

                maxRows <- (ceil ((Utils.logOf initMessage.NumberOfNodes) / (Utils.logOf baseVal))) |> int
                maxNodes <- Utils.powOf (baseVal, maxRows) |> int

                initNodeList()

                groupOne.Add(nodeList.[0])

                initPastryNodes(initMessage.NumberOfNodes, initMessage.NumberOfRequests)

                initPastryWork()
            | MessageType.JoinFinish -> 
                numberOfNodesJoined <- numberOfNodesJoined + 1
                if (numberOfNodesJoined = totalNumberOfNodes) then
                    mailbox.Self <! MessageType.StartRouting
                else
                    mailbox.Self <! MessageType.JoinNodesInDT
            | MessageType.JoinNodesInDT -> 
                let nodeId = nodeList.[Random().Next(numberOfNodesJoined)]
                let nodeName = "/user/PastryNode" + (nodeId |> string)
                let node = select nodeName system
                let taskInfo: MessageType.Task = {
                    Message = "Join";
                    FromNodeId = nodeId;
                    ToNodeId = nodeList.[numberOfNodesJoined];
                    HopCount = -1;
                }
                node <! MessageType.Task taskInfo
            | MessageType.StartRouting -> 
                (select "/user/PastryNode*" system) <! MessageType.StartRouting
            | MessageType.FinishRoute finishRouteMessage -> 
                numberOfNodesRouted <- numberOfNodesRouted + 1
                numberOfHops <- numberOfHops + 1
                if (numberOfNodesRouted >= totalNumberOfNodes * numberOfRequests) then
                    let message = "Routing Finished!" + 
                                    "\nTotal Number of Routes: " + (numberOfNodesRouted |> string) + 
                                    "\nTotal Number of Hops: " + (numberOfHops |> string) + 
                                    "\nAverage Number of Hops per Route: " + 
                                    ((Utils.toDouble numberOfHops / Utils.toDouble numberOfNodesRouted) |> string)
                    systemRef <! message
            | MessageType.NodeNotFound -> 
                numberOfNodesNotInBoth <- numberOfNodesNotInBoth + 1
            | MessageType.RouteNodeNotFound -> 
                numberOfRouteNotFound <- numberOfRouteNotFound + 1
            | _ -> ()
        
        return! loop()
    }

    loop()

let main (numberOfNodes, numberOfRequests) = 
    if not (isValidInput (numberOfNodes, numberOfRequests)) then
        printfn "Error: Invalid Input"
    else
        let supervisor = spawn system "Supervisor" Supervisor

        let initMessage: MessageType.InitSupervisor = {
            NumberOfNodes = numberOfNodes;
            NumberOfRequests = numberOfRequests;
        }

        let response = Async.RunSynchronously(supervisor <? MessageType.InitSupervisor initMessage)
        printfn "%A" response

        system.Terminate() |> ignore

// Read command line inputs and pass on to the driver function
match fsi.CommandLineArgs with
    | [|_; numberOfNodes; numberOfRequests|] -> 
        main ((Utils.strToInt numberOfNodes), (Utils.strToInt numberOfRequests))
    | _ -> printfn "Error: Invalid Arguments"