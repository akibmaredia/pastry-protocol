# COP-5615 - Distributed Operating Systems

# Pastry

### Shaishav Shah 1136-3317
### Akib Maredia 3885-6489

## Instructions for running the code

* Unzip the zipped folder project3Bonus.zip 
* ```cd project3Bonus/src```
* Run the command ```dotnet fsi --langversion:preview Bonus.fsx <numberOfNodes> <numberOfRequests> <numberOfFailureNodes>```

# Implementation

* When all the nodes finish joining the network, we randomly pick `numberOfFailureNodes` (10% by default if not specified by the user) number of nodes and kill them. These nodes being killed by the Supervisor will in turn notify the nodes belonging to their table with a message that they're failing. The neighbor nodes in response to such message will update their table values. Eventually the entire network knows about the failed nodes.
* For eg: if we have 1000 nodes and by default 10% of the nodes are failing, we will only have 900 nodes actually functioning.
* Hence our network size will drop and we will be routing the messages through the reduced network size.

## Observations

* It was observed that as the number of nodes fail the average number of hops increases. 
* The number of hops increases to deliver the message to the destination node.
* Hence it is clear that the average number of hops is directly proportional to the number of failed nodes. 
* The resilience of the system is about 10% of the total number of nodes in the system. If higher number of nodes are killed chances message delivery decreases significantly and hence it might lead to the system running indefinitely.
* The first table shows the average number of hops for a network without any failed nodes. The second table shows the average number of hops in a network with failed nodes.

### Table 1

|Number of Nodes   |Number of requests   |Average number of hops   |
|:-:  |:-:  |:-:   |
|16   | 10  |1.23  |
|64   | 10  |2.11  |
|256  | 10  |2.88  |
|1024 | 10  |3.63  | 
|4096 | 10  |4.48  |
|16384| 10  |5.27  | 
|65536| 10  |6.89  |

### Table 2 - Bonus

|Number of Nodes   |Number of Failed   |Number of Requests   |Avergage Number of Hops   |
|:-:  |:-: |:-:|:-:    |
|16   | 1  |10 | 1.36  |
|64   | 6  |10 | 2.13  |
|256  |25  |10 | 3.16  |
|1024 |102 |10 | 4.30  |
|4096 |409 |10 | 5.86  |
|16384|1638|10 | 6.27  |
|65536|6553|10 | 8.65  |