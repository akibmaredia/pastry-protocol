# Pastry-Protocol

The objective of this project is to implement Pastry protocol in F# using actor model provided by Akka.NET framework. 

# Team Members

* Shaishav Shah <br>
  UFID: 1136-3317
* Akib Maredia <br>
  UFID: 3885-6489

# What is working

* Implemented the Pastry API which works for routing messages over peer to peer network according to the pastry paper.
* The program takes in number of nodes and adds them to the network. Once the program starts, the number of nodes starts routing messages.
* Every request is routed to the closest numerical value of the key that is to be requested eventually reaching the node with the desired key.
* Both the failure model and basic model are working.
* The average number of hops is printed at the end.

# Largest network

* The largest network that we were managed to run is having `4^9 = 262144` number of nodes and the average number of hops for it is `6.89` 
![Alt](./screenshots/largest_num_of_nodes.jpg "Screenshot")