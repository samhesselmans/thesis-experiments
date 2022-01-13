from hashlib import new
import networkx as nx
import matplotlib.pyplot as plt
import random
import math
import numpy as np
import instance_generator as ig
from scipy.optimize import fsolve
import time 
class Customer:
    def __init__(self,x,y,z,demand):
        self.X = x
        self.Y = y
        self.Z = z
        self.Pos = (x,y)
        self.Demand = demand
    
    def CreateDict(self):
        return {"x":self.X,"y":self.Y,"z":self.Z,"demand":self.Demand,"Pos":self.Pos}



def GenerateRandomCustomers(n=20):
    res = []
    for _ in range(n):
        res.append(Customer(random.uniform(0,100),random.uniform(0,100),random.uniform(0,100),random.uniform(0,100)))
    return res
    


def CalcEdgeWeights(G,slopes,distances,bike_mass,power_setting=250,cyclist_power=150):
    
    travel_times = dict.fromkeys(slopes.keys(),[])
    for slope in slopes:
        func = lambda v: ig.CalcTotalForce(v,bike_mass,slopes[slope]) - (power_setting + cyclist_power)
        sol = fsolve(func,5,xtol=0.01)
        speed = min(sol[0],6.944)
        travel_times[slope] = distances[slope]/speed
    nx.set_edge_attributes(G,travel_times,"traveltime")


cust = GenerateRandomCustomers(50)
G = nx.DiGraph()
#G.add_nodes_from(cust)
for i,c in enumerate(cust):
    G.add_nodes_from([(i,c.CreateDict())])

addRandomBetweenNodes = True

for i,ic in enumerate(cust):
    for j,jc in enumerate(cust):
        if(i!=j):
            dist = math.sqrt(pow(jc.X-ic.X,2) + pow(jc.Y-ic.Y,2))
            if addRandomBetweenNodes:
                num_extra = random.randrange(1,5)
                prev_node = i
                for e in range(num_extra):
                    new_dist = (dist/(num_extra+1))
                    newX = ic.X + new_dist * (jc.X-ic.X)/dist * (e+1)
                    newY = ic.Y + new_dist * (jc.Y-ic.Y)/dist * (e+1)
                    Z = random.uniform(0,100)
                    slope = np.arctan((Z - ic.Z)/new_dist) * math.pi / 180
                    color = 'r'
                    if slope >=0:
                        color = 'g'
                    new_node = f"{i},{j}_{e}"
                    G.add_node(new_node,Pos=(newX,newY),Z=Z)
                    G.add_edge(prev_node,new_node,distance=(dist/(num_extra+2)),slope=slope,color=color)
                    prev_node=new_node
                    #G.add_edge()
                slope = np.arctan((jc.Z - Z)/(dist/(num_extra+2))) * math.pi / 180
                color = 'r'
                if slope >=0:
                    color = 'g'
                G.add_edge(prev_node,j,distance=(dist/(num_extra+2)),slope=slope,color=color)
            else:
                slope = np.arctan((jc.Z - ic.Z)/dist) * math.pi / 180
                color = 'r'
                if slope >=0:
                    color = 'g'
                G.add_edge(i,j,distance=dist,slope=slope,color=color)
start_time = time.time()
travel_times = []
slopes = nx.get_edge_attributes(G,"slope")
distances = nx.get_edge_attributes(G,"distance")
for i in range(150,155):
    CalcEdgeWeights(G,slopes,distances,i)
    sol = dict(nx.johnson(G,weight='traveltime'))
    travel_times.append(sol)
    print(i)
print(f"Done calculating all shortest paths in: {time.time()-start_time} seconds")
pos = nx.get_node_attributes(G,"Pos")
colors = nx.get_edge_attributes(G,"color").values()
#nx.draw(G,pos, with_labels=True, connectionstyle='arc3, rad = 0.1',edge_color=colors)
#nx.draw_networkx_edge_labels(G,pos,edge_labels=angles)
#plt.show()  # pyplot draw()

print(G)