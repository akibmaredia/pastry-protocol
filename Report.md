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
* The 
* The first table shows the average number of hops for a network without any failed nodes. The second table shows the average number of hops in a network with failed nodes.

|Number of NOdes   |Number of requests   |Average number of hops   |
|--:|--:|--:|
| 16  |   |   |
| 32  |   |   |
| 64  |   |   |

|Number of Nodes   |Number of Requests   |Number of Failed   |Avergage Number of Hops   |
|:-:|:-:|:-:|:-:|
|500   |   |   |   |
|1000   |   |   |   |
|5000   |   |   |   |
|10000  |   |   |   |
|65536