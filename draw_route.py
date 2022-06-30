import math
from socket import inet_aton
from turtle import color, distance
from xml.etree.ElementTree import PI
from matplotlib.style import available

from numpy import Infinity, append, block
import parse_vrpltt_instance as pvi
import networkx as nx
import matplotlib.pyplot as plt


instance_madrid_full = "vrpltt_instances/large/madrid_full.csv"
instance_utrecht_full = "vrpltt_instances/large/utrecht_full.csv"
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
    X = 6371 * (longitude / 180 * math.pi -centralLongitude /180 * math.pi )* math.cos(centralLatitude / 180 * math.pi)
    Y = 6371 * (latitude / 180 * math.pi - centralLatitude /180 * math.pi)
    return (X, Y)


def plot(solution, instance):
    fig = plt.figure()
    ax = fig.add_subplot(111)
    ax.set_aspect('equal', adjustable='box')

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
    #plt.set_aspect('equal', adjustable='box')
    
def levenshteinDistance(str1, str2):
    m = len(str1)
    n = len(str2)
    d = [[i] for i in range(1, m + 1)]   # d matrix rows
    d.insert(0, list(range(0, n + 1)))   # d matrix columns
    for j in range(1, n + 1):
        for i in range(1, m + 1):
            if str1[i - 1] == str2[j - 1]:   # Python (string) is 0-based
                substitutionCost = 0
            else:
                substitutionCost = 1
            d[i].insert(j, min(d[i - 1][j] + 1,
                               d[i][j - 1] + 1,
                               d[i - 1][j - 1] + substitutionCost))
    return d[-1][-1]


def CompareSolutions(sol1,sol2,instance):
    (s1,s2) = OrderSolution(sol1,sol2)

    #If routes are exactly equeal. Dont plot them
    toRemove = []
    for i in range(len(s1)):
        if(levenshteinDistance(s1[i],s2[i]) == 0):
            toRemove.append(i)
    for index in sorted(toRemove, reverse=True):
        del s1[index]
        del s2[index]

    plot(s1,instance)
    plot(s2,instance)

    plt.show()

def OrderSolution(sol1,sol2):

    alldist = []
    for r1 in sol1:
        distances = []
        for r2 in sol2:
            distances.append(levenshteinDistance(r1,r2))
        alldist.append(distances)


    #Greedily select most similar routes
    selectedIndices = []
    availableIndices = [i for i in range(len(sol2))]
    for dist in alldist:
        #dist.index(min(dist))
        #choices = [dist[i] for i ]
        minval = Infinity
        minIndex = -1
        for i in availableIndices:
            if(dist[i] < minval):
                minval = dist[i]
                minIndex = i
        availableIndices.remove(minIndex)
        selectedIndices.append(minIndex)

    newSol2 = []
    for i in selectedIndices:
        newSol2.append(sol2[i])
    for index in sorted(selectedIndices, reverse=True):
        del sol2[index]
    newSol2 += sol2
    for i in range(len(sol1)):
        print(sol1[i])
        print(newSol2[i])
        print()

    
    

    return (sol1,newSol2)

    
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
#solution with wind power 3 with directeion (1,2) madrid 100 but with fixed conversion to planar coordinates and long running code SCORE: 133.112
solution12 = [
(0,62,21,98,64,94,78,0),
(0,56,1,70,39,44,19,99,11,60,7,9,95,41,16,29,0),
(0,71,100,28,75,24,27,32,61,22,96,51,10,31,33,77,76,0),
(0,2,82,93,72,14,30,55,79,8,92,91,53,84,0),
(0,65,5,6,67,63,89,12,81,97,35,69,36,0),
(0,37,23,20,87,59,26,49,15,34,0),
(0,80,68,54,25,86,13,17,43,38,88,85,74,46,42,18,52,0),
(0,40,47,48,83,90,4,66,3,45,50,57,58,73,0)
]

#solution with wind power 0 with directeion (1,2) madrid 100 but with fixed conversion to planar coordinates and long running code SCORE: 125.438
solution13 = [
(0,78,23,37,69,36,2,64,94,0),
(0,62,21,67,63,6,5,65,35,97,81,12,89,98,0),
(0,40,47,39,70,1,19,44,60,7,41,95,9,16,29,0),
(0,71,100,28,75,24,27,32,61,22,96,51,10,31,99,11,58,0),
(0,82,93,72,14,30,55,79,8,92,91,53,84,0),
(0,56,48,83,90,4,66,3,45,57,50,33,73,77,76,0),
(0,34,15,49,26,59,87,20,0),
(0,80,68,54,25,86,13,17,43,38,88,85,74,46,42,18,52,0)
]

#Soltion with wind power , direction 0,1 for madrid 100 but with truly fixed conversion to planar coordinates.
solution14 = [(0,80,68,81,54,90,4,66,58,95,41,9,16,29,15,78,0),
(0,82,55,86,13,28,24,75,61,32,27,22,96,3,46,74,85,0),
(0,2,23,37,65,25,91,53,52,18,87,20,69,36,0),
(0,56,39,70,1,51,10,31,45,50,57,59,26,49,76,0),
(0,40,47,83,48,19,44,60,7,11,99,33,73,77,34,0),
(0,67,5,71,100,43,38,88,97,35,12,89,98,64,94,0),
(0,62,21,63,6,30,14,72,93,79,8,17,92,42,84,0),
]

#Solution with wind power 3, direction 0,1 for utrecht 100 with tryl fixed planar projection
solution15 = [(0,9,95,31,29,93,5,81,78,56,34,33,98,4,52,0),
(0,85,77,35,43,91,57,88,92,80,65,62,73,58,50,0),
(0,53,25,8,70,99,51,3,41,82,87,64,83,67,24,84,0),
(0,66,75,14,45,42,28,10,23,20,40,12,44,94,97,0),
(0,55,7,47,1,90,22,15,11,18,59,71,0),
(0,74,39,38,63,16,26,2,69,13,37,49,79,17,21,96,0),
(0,30,19,27,86,60,32,72,89,68,36,61,46,76,6,48,54,0)]

#Same as 15, but executed using 16 starts and ILP optimization
solution16 = [(0,25,53,24,84,59,18,71,0),
(0,85,35,77,61,46,69,13,37,49,79,17,21,96,0),
(0,30,19,27,86,60,32,72,89,68,36,16,2,26,54,0),
(0,38,63,91,43,57,92,88,80,65,62,73,58,20,40,44,0),
(0,7,55,47,1,11,22,15,78,76,6,48,97,0),
(0,9,95,29,93,5,81,31,56,33,34,98,4,90,52,0),
(0,74,39,10,23,28,12,42,45,3,41,51,99,70,0),
(0,8,66,14,67,75,83,64,87,82,50,94,0)]


solution17 = [(0,56,40,47,1,70,39,99,19,11,60,7,73,77,34,0),
(0,80,68,81,54,35,97,52,18,53,87,26,59,20,0),
(0,71,100,28,75,24,27,32,61,22,96,66,3,74,85,42,84,0),
(0,72,82,55,86,8,88,38,43,13,79,17,92,91,25,69,0),
(0,67,6,65,5,63,14,30,93,12,89,98,64,94,0),
(0,62,21,2,36,37,23,49,95,41,9,16,29,15,76,78,0),
(0,90,4,51,10,31,83,48,46,45,57,50,44,33,58,0),
]

solution18 = [(0,72,82,55,86,17,43,38,88,85,74,46,52,18,42,84,0),
(0,56,71,100,28,24,75,13,93,79,8,92,91,25,53,0),
(0,40,47,39,70,1,31,83,48,50,19,99,11,44,73,77,0),
(0,29,41,95,9,16,76,34,0),
(0,62,21,23,37,36,2,64,94,0),
(0,80,68,81,54,35,97,7,60,33,58,59,26,49,15,0),
(0,67,5,6,30,14,65,63,89,12,87,20,69,98,0),
(0,90,4,27,32,61,22,96,66,3,51,10,45,57,78,0),
]
# plot(solution14,instance_madrid_full)
# plot(solution13,instance_madrid_full)
# plot(solution15,instance_utrecht_full)
# plot(solution16,instance_utrecht_full)

# plt.show()



# print(levenshteinDistance("test","stet"))

# print(levenshteinDistance([0,1,2,3,4],[0,4,1,2,3]))
# print(levenshteinDistance([0,1,2,3,4],[0,4,3,2,1]))
CompareSolutions(solution17,solution18,instance_madrid_full)

#plot(solution17,instance_madrid_full)

plt.show()