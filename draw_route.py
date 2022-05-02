import math
from socket import inet_aton
from turtle import color
from xml.etree.ElementTree import PI

from numpy import Infinity, block
import parse_vrpltt_instance as pvi
import networkx as nx
import matplotlib.pyplot as plt


instance_madrid_full = "vrpltt_instances/large/madrid_full.csv"
# customers = pvi.ParseInstance(instance)


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
    "#fcba03",
    "#32a852",
    "#4287f5",
    "#eb4034",
    "#32a852",
    "#6fab7f",
    "#8976a8",
    "#997f31",
    "#c987bc",
    "#5e0c13"

]

def ConvertToPlanarCoordinates( latitude,  longitude,  centralLatitude,  centralLongitude):
    X = longitude / 180 * math.pi* math.cos(centralLatitude / 180 * math.pi)
    Y = latitude
    return (X, Y)


def plot(solution, instance):
    customers = pvi.ParseInstance(instance)

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

    G = nx.DiGraph()

    for i,c in enumerate(customers):
        #G.add_nodes_from([(i,{"X":c[1],"Y":c[2]})])
        #G.add_node(i,pos=(float(c[1]),float(c[2])))
        G.add_node(i,pos=ConvertToPlanarCoordinates(float(c[1]),float(c[2]),centralLatitude,centralLongitude))

    index = 0
    for route in solution:
        for i in range(len(route)-1):
            G.add_edge(int(route[i]),int(route[i+1]),color=colors[index])

        index+=1 

    pos=nx.get_node_attributes(G,'pos')
    col = [G[u][v]['color'] for u,v in G.edges()]
    nx.draw(G,pos,edge_color=col)
    plt.figure()
    
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

#solution with wind power 30 with directeion (1,2) fukuoka 50 but with fixed conversion to planar coordinates
solution8 = [(0,50,8,19,14,28,12,47,38,40,0),
(0,11,23,6,5,22,41,0),
(0,33,36,27,10,3,24,31,45,15,0),
(0,39,29,7,0),
(0,46,20,42,32,0),
(0,4,43,26,48,0),
(0,37,13,44,21,17,0),
(0,25,35,0),
(0,30,34,9,16,1,0),
(0,2,0)
]

#solution with wind power 30 with directeion (1,2) madrid 100 but with fixed conversion to planar coordinates
solution9 = [(0,62,21,30,72,14,55,17,8,35,81,84,0),
(0,82,94,78,98,93,0),
(0,6,67,63,37,36,89,12,49,0),
(0,71,100,43,38,53,18,52,42,0),
(0,5,65,25,91,92,85,0),
(0,54,97,87,20,69,0),
(0,86,79,74,76,0),
(0,39,44,11,99,33,45,58,0),
(0,80,23,2,64,15,0),
(0,90,9,16,29,77,26,0),
(0,68,73,59,47,40,0),
(0,83,48,46,31,70,1,10,32,0),
(0,88,28,75,24,66,3,96,0),
(0,13,0),
(0,60,19,57,50,0),
(0,51,4,22,41,0),
(0,61,27,0),
(0,56,34,95,0),
(0,7,0)]

#solution with wind power 0 with directeion (1,2) madrid 100 but with fixed conversion to planar coordinates
solution10 = [
#(0,71,100,28,75,24,27,32,61,22,96,51,10,31,33,58,77,0),
(0,40,56,47,1,70,39,44,19,99,11,41,95,9,16,29,0),
(0,80,68,81,54,83,48,60,7,73,26,59,49,15,34,76,0),
(0,62,21,67,5,63,6,30,14,72,93,98,64,94,0),
(0,20,90,4,66,3,45,50,57,46,85,74,42,84,0),
(0,2,37,23,65,35,97,53,18,52,87,12,89,78,0),
(0,82,55,86,8,88,38,43,13,79,17,92,91,25,69,36,0)]

#solution with wind power 3 with directeion (1,2) madrid 100 but with fixed conversion to planar coordinates
solution11 = [
 #(0,71,100,28,75,24,27,32,61,22,96,51,10,31,33,58,77,0),
 (0,40,47,48,83,90,4,66,3,45,50,57,34,76,0),
(0,80,68,81,54,20,59,26,49,73,95,41,9,16,29,15,0),
(0,62,21,72,30,14,63,23,37,69,36,2,64,94,78,0),
(0,56,39,44,19,99,11,60,7,70,1,46,74,85,42,84,0),
(0,82,55,86,13,17,43,38,88,91,25,92,8,79,93,98,0),
(0,67,6,5,65,35,97,52,18,53,87,12,89,0)
]


plot(solution10,instance_madrid_full)
plot(solution11,instance_madrid_full)
plt.show()




