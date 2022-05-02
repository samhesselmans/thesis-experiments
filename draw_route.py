import math
from socket import inet_aton
from turtle import color
from xml.etree.ElementTree import PI

from numpy import Infinity
import parse_vrpltt_instance as pvi
import networkx as nx
import matplotlib.pyplot as plt


instance = "vrpltt_instances/large/fukuoka_50.csv"
customers = pvi.ParseInstance(instance)

#solution with wind power 3 with directeion (1,2) fukuoka 50
solution = [(0,20,19,28,44,13,21,17,49,31,45,15,0),
(0,50,11,23,29,39,10,4,27,7,6,22,5,0),
(0,41,18,30,34,24,3,2,32,9,16,1,42,40,0),
(0,46,8,14,37,33,36,26,35,43,48,25,38,47,12,0)
]
#solution with wind power 6 with directeion (1,2) fukuoka 50
solution2 = [(0,46,8,14,37,33,36,26,35,43,48,25,38,47,12,0),
(0,20,19,28,13,44,21,17,49,31,45,15,0),
(0,50,11,23,29,39,10,4,27,7,6,22,5,0),
(0,41,18,30,34,24,3,2,9,16,1,32,42,40,0)
]
#solution with wind power 6 with directeion (1,2) fukuoka 50

solution3 = [
    (0,8,19,20,40,42,2,32,9,16,1,3,24,31,15,38,0),
    (0,46,14,37,33,36,26,35,43,48,25,45,44,47,0),
    (0,41,18,30,34,21,49,17,13,28,12,0),
    (0,50,11,23,29,39,10,4,27,7,6,22,5,0)
]
#solution with wind power 30 with directeion (1,2) fukuoka 50
solution4 = [(0,1,11,29,48,33,26,4,27,36,10,7,38,45,0),

(0,18,50,35,39,43,37,25,15,49,17,0),
(0,2,30,34,28,12,21,9,16,3,24,32,0),
(0,20,40,42,19,0),
(0,41,8,14,46,13,6,23,22,5,44,31,47,0)
]
#solution with wind power 40 with directeion (1,2) fukuoka 50
solution5 = [
(0,50,31,49,15,45,38,48,10,36,0),
(0,11,6,22,5,44,47,24,0),
(0,33,26,27,7,0),
(0,39,0),
(0,46,14,2,30,28,13,17,1,16,0),
(0,4,0),
(0,8,18,41,40,19,42,20,0),
(0,37,23,29,25,0),
(0,35,43,12,0),
(0,32,21,34,9,3,0)]


#solution with wind power -30 with directeion (1,2) fukuoka 50
solution6 = [
    (0,50,32,2,30,34,19,40,42,20,0),
    (0,33,36,4,27,26,29,7,38,49,0),
    (0,48,43,39,10,35,15,25,45,9,3,0),
    (0,11,37,5,23,6,22,44,31,1,16,0),
    (0,41,18,8,14,46,28,12,21,17,13,47,24,0)
]
#solution with wind power -30 with directeion (1,-2) fukuoka 50
solution7 = [
(0,50,23,29,14,28,37,15,12,0),
(0,46,36,33,26,35,48,43,25,44,45,47,38,0),
(0,11,39,4,10,27,7,6,5,22,0),
(0,8,19,21,24,3,17,20,31,13,0),
(0,41,18,49,30,2,32,16,9,34,40,42,1,0)
]

minLatitude = Infinity
maxLatitude = -Infinity
minLongitude = Infinity
maxLongitude = -Infinity

for c in customers:
    if(float(c[1]) < minLatitude):
        minLatitude = float(c[1])
    if(float(c[1]) > maxLatitude):
        maxLatitude =float(c[1])
    if(float(c[2]) < minLongitude):
        minLongitude = float(c[2])
    if(float(c[2]) > maxLongitude):
        maxLongitude = float(c[2]) 

centralLatitude = (minLatitude + maxLatitude) / 2
centralLongitude = (minLongitude + maxLongitude) / 2

def ConvertToPlanarCoordinates( latitude,  longitude,  centralLatitude,  centralLongitude):
    X = longitude / 180 * math.pi* math.cos(centralLatitude / 180 * math.pi)
    Y = latitude
    return (X, Y)
        


G = nx.DiGraph()





for i,c in enumerate(customers):
    #G.add_nodes_from([(i,{"X":c[1],"Y":c[2]})])
    #G.add_node(i,pos=(float(c[1]),float(c[2])))
    G.add_node(i,pos=ConvertToPlanarCoordinates(float(c[1]),float(c[2]),centralLatitude,centralLongitude))





colors = [    "tab:red",
     "tab:orange",
     "tab:olive",
     "tab:green",
     "tab:blue",
    "tab:purple",
    "#00FF00",
    "#00FFFF",
    "#000000",
    "#FF00FF",
    "#CD853F",
]

index = 0
for route in solution4:
    for i in range(len(route)-1):
        G.add_edge(int(route[i]),int(route[i+1]),color=colors[index])

    index+=1 

pos=nx.get_node_attributes(G,'pos')
edge_colors = nx.get_edge_attributes(G,'color')
col = [G[u][v]['color'] for u,v in G.edges()]
nx.draw(G,pos,edge_color=col)
plt.show()