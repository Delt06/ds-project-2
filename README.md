# DS Project 2
Distributed File System implementation.

## File Server
Represents a storage (possible, remote) server. May be replicated.

## Name Server
Maintains the file/directory tree and works as a mediator between the client and storage servers.

### Setting up
Let's assume that we have 2 File Servers and they have the following IP addresses:
- FileServer1: 10.0.15.11
- FileServer2: 10.0.15.12
 
On NameServer host, run:
```
sudo docker pull deltation/name-server
sudo docker run -p 55556-55560:55556-55560 deltation/name-server 10.0.15.11:55561 10.0.15.12:55563
```

On FileServer1 host, run:
```
sudo docker pull deltation/file-server
sudo docker run -p 55561:55561 -p 55562:55562 deltation/file-server 55561 55562
```

On FileServer2 host, run:
```
sudo docker pull deltation/file-server
sudo docker run -p 55563:55563 -p 55564:55564 deltation/file-server 55563 55564
```

Then, launch the client application and enter the IP of the NameServer host:
```
sudo docker pull deltation/dfs-client
sudo docker run -i -p 55555:55555 deltation/dfs-client <NameServerIP>
``` 

After the client has connected to the NameServer, type `Initialize` to initialize the DFS.

That's it, the DFS is ready to work.

### Replication
Application supports replication of FileServer nodes. If one node is down, others will be able to server the requests.

If a node that was down is available now, it can rejoin the cluster. Let's assume that the FileServer2 is down, To make it rejoin the cluster that, run:
```
sudo docker run -p 55563:55563 -p 55564:55564 deltation/file-server 55563 55564 10.0.15.11:55562
```
This will cause the FileServer2 to load an up to date state from FileServer1.