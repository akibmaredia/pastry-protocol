# COP-5615 - Distributed Operating Systems

# Pastry

### Shaishav Shah 1136-3317
### Akib Maredia 3885-6489

## Instructions for running the code

* Unzip the zipped folder project3Bonus.zip 
* ```cd project3Bonus/src```
* Run the command ```dotnet fsi --langversion:preview Main.fsx <numberOfNodes> <numberOfRequests> <numberOfFailed>```
* <numberOfNode>, <numberOfRequests> and <numberOfFailed> should be integers else the program will exit with invalid input error.

# Implementation

* This part of the project was implemented such that when the nodes join the network, either `numberOfFailed` number of nodes will fail or by default 1% of the total nodes will fail
* These failure nodes are selected at random and when selected they send a message to all their neighbors in the table that they are failing.
* The neigboring nodes inform their neighbors that particular node has failed and so on. Eventually the entire network knows about the failed nodes
* For eg: if we have 1000 nodes and by default 1% of the nodes are failing, we will only have 990 nodes actually functioning
* Hence our network size will drop and we will be routing the messages through the reduced network size.

## Observations

* It was observed that as the number of nodes fail the average number of hops increases. 
* The number of hops increases to deliver the message to the destination node.
* Hence it is clear that the average number of hops is directly proportional to the number of failed nodes. 
* The resilience of the system is about 10% of the total number of nodes in the system. If higher number of nodes are killed chances message delivery decreases significantly and hence it might lead to the system running indefinitely.
* The first table shows the average number of hops for a network without any failed nodes. The second table shows the average number of hops in a network with failed nodes.

### Table 1

|Number of NOdes   |Number of requests   |Average number of hops   |
|:-:  |:-:  |:-:   |
|16   | 10  |1.23  |
|64   | 10  |2.11  |
|256  | 10  |2.88  |
|1024 | 10  |3.63  | 
|4096 | 10  |4.48  |
|16384| 10  |5.27  | 
|65536| 10  |6.89  |

### Table 2

|Number of Nodes   |Number of Failed   |Number of Requests   |Avergage Number of Hops   |
|:-:  |:-: |:-:|:-:    |
|16   | 1  |10 | 1.36  |
|64   | 6  |10 | 2.13  |
|256  |25  |10 | 3.16  |
|1024 |102 |10 | 4.39  |
|4096 |409 |10 | 5.86  |
|16384|1638|10 | 6.27  |
|65536|6553|10 | 8.65  |