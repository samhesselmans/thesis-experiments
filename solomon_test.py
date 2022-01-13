import enum
import parse_solomon_instance as psi
import math
import os
import random
import numpy as np
from multiprocessing import Pool



class Route:
    def __init__(self,customers,max_capacity):
        self._route = [0,0]
        self._arrival_times = [0,0]
        self.time_done_with_last_cust = 0
        self.last_cust = 0
        self.customers = customers
        self.used_capacity = 0
        self.max_capacity = max_capacity

    def AddCustomerToEnd(self,cust):
        dist = self.CustomerDist(cust,self.last_cust)#math.sqrt(pow(self.customers[cust][1] - self.customers[self.last_cust][1],2) + pow(self.customers[cust][2] - self.customers[self.last_cust][2],2))
        #This if can be removed later to allow infeasible solution
        if(self.time_done_with_last_cust + dist < self.customers[cust][5] and self.used_capacity + self.customers[cust][3] <= self.max_capacity):
            self._route.insert(len(self._route)-1,cust)

            self.last_cust = cust
            self.used_capacity += self.customers[cust][3]
            if(self.time_done_with_last_cust + dist>=  self.customers[cust][4]):
                self.time_done_with_last_cust = self.time_done_with_last_cust + dist + self.customers[cust][6]
                self._arrival_times.insert(len(self._arrival_times)-1,self.time_done_with_last_cust + dist)
                
            else:
                self._arrival_times.insert(len(self._arrival_times)-1,self.customers[cust][4])
                self.time_done_with_last_cust = self.customers[cust][4] + self.customers[cust][6]
            self._arrival_times[len(self._arrival_times)-1] = self.time_done_with_last_cust + self.CustomerDist(cust,self._route[len(self._route)-1])
            return True
        else:
            return False
    
    def CalcDist(self):
        total_dist = 0
        for index,cust in enumerate(self._route):
            if(index >= len(self._route) -1):
                break
            next_cust = self._route[index + 1]
            dist = self.CustomerDist(cust,next_cust)
            total_dist += dist
        return total_dist
    
    def CustomerDist(self,cust1,cust2):
        return math.sqrt(pow(self.customers[cust1][1] - self.customers[cust2][1],2) + pow(self.customers[cust1][2] - self.customers[cust2][2],2))

    def RemoveCust(self,cust):
        new_arr_time = 0
        last_cust = -1
        index = -1
        for i in  range(1,len(self._route)):#enumerate(self._route,start=1):
            if(i >= len(self._route) -1):
                break
            c = self._route[i]
            if(c != cust):
                dist = self.CustomerDist(self._route[i-1],self._route[i])
                if new_arr_time  + dist < self.customers[c][4]:
                    new_arr_time += self.customers[c][4]
                else:
                    new_arr_time += dist
                self._arrival_times[i] = new_arr_time
                new_arr_time += self.customers[c][6]
                last_cust = c
            else:
                index = i
        self._route.pop(index)
        self._arrival_times.pop(index)
        self.time_done_with_last_cust = new_arr_time
        self.last_cust = last_cust
        self.used_capacity -= self.customers[cust][3]
    
    #Returns position and increase in length of best possible insertion location
    def BestPossibleInsert(self,cust):
        best_dist_increae = math.inf
        best_index = -1

        if(self.used_capacity + self.customers[cust][3] > self.max_capacity):
            return best_index,best_dist_increae

        for i in range(1,len(self._route)):
            #the increase in arrival_times when this customer is inserted here
            arrival_time_at_new_cust = self._arrival_times[i-1] + self.customers[self._route[i-1]][6] + self.CustomerDist(self._route[i-1],cust) 
            
            if(arrival_time_at_new_cust >self.customers[cust][6]):
                break
            
            if(arrival_time_at_new_cust < self.customers[cust][4]):
                arrival_time_at_new_cust = self.customers[cust][4]
            
            # increase = arrival_time_at_new_cust + self.CustomerDist(self._route[i],cust)  + self.customers[cust][6] - self._arrival_times[i]
            # if(increase < 0):
            #     increase = 0
            
            #Check whether or not this position is feasible
            possible = True
            new_arr_time = arrival_time_at_new_cust + self.CustomerDist(self._route[i],cust)  + self.customers[cust][6]
            for j in range(i,len(self._route)):
                if(new_arr_time > self.customers[self._route[j]][5]):
                    #With the new customer inserted we dont meet the timewindow of j
                    possible = False
                    break
                if(new_arr_time < self.customers[self._route[j]][4]):
                    new_arr_time = self.customers[self._route[j]][4]
                if(j != len(self._route)-1):
                    new_arr_time += self.CustomerDist(self._route[j],self._route[j+1])  + self.customers[self._route[j]][6]
                #if(self._arrival_times[j] + increase > self.customers[self._route[j]][5]):
                #    possible = False
                #    break
            
            if(possible):
                dist_increase = self.CustomerDist(self._route[i-1],cust) +  self.CustomerDist(self._route[i],cust) - self.CustomerDist(self._route[i-1],self._route[i])
                #Check if this position is better than the current best position
                if(dist_increase < best_dist_increae):
                    #Update best known solution
                    best_dist_increae = dist_increase
                    best_index = i 

            
        return best_index,best_dist_increae

    def InsertCust(self,cust,pos):
        arrival_time_at_new_cust = self._arrival_times[pos-1] + self.customers[self._route[pos-1]][6] + self.CustomerDist(self._route[pos-1],cust)

        #Update arrival times
        new_arr_time = arrival_time_at_new_cust + self.CustomerDist(self._route[pos],cust)  + self.customers[cust][6]
        for j in range(pos,len(self._route)):
            if(new_arr_time < self.customers[self._route[j]][4]):
                new_arr_time = self.customers[self._route[j]][4]
            self._arrival_times[j] = new_arr_time
            if(j != len(self._route)-1):
                new_arr_time += self.CustomerDist(self._route[j],self._route[j+1])  + self.customers[self._route[j]][6]
        
        #Insert new values
        self._arrival_times.insert(pos,arrival_time_at_new_cust)
        self._route.insert(pos,cust)
        self.used_capacity += self.customers[self._route[pos-1]][3]
        return
    #Returns a random customer and the reduced length of the route that would be caused by removal of said customer
    def RandomCust(self):
        if len(self._route) == 2:
            return None,None
        cust = random.randrange(1,len(self._route)-1)

        return self._route[cust],self.CustomerDist(self._route[cust-1],self._route[cust]) + self.CustomerDist(self._route[cust],self._route[cust+1]) - self.CustomerDist(self._route[cust-1],self._route[cust+1])

            
                





def GetDueDate(elem):
    return elem[5]






def CalcTotalDistance(routes):
    total_dist = 0
    for route in routes:
        total_dist += route.CalcDist()
    return total_dist

def HandleMove(routes,src,dest,pos,cust):
    routes[src].RemoveCust(cust)
    routes[dest].InsertCust(cust,pos)

def MoveRandomCustomer(routes):
    #global amt_imp, amt_worse,amt_notdone
    best_dest = -1
    best_src = -1
    best_cust = -1
    best_imp = -math.inf
    best_pos = -1
    for i in range(4):
        dest = random.randrange(len(routes))
        src = random.randrange(len(routes))
        tries = 0
        while((dest == src or len(routes[src]._route) == 2 )and tries < 10):
            src = random.randrange(len(routes))
            tries+= 1
        cust,decrease = routes[src].RandomCust()
        if(cust is  None):
            continue
        pos,increase = routes[dest].BestPossibleInsert(cust)
        if(pos == -1):
            continue
        improvement = decrease - increase
        if(improvement > best_imp):
            best_imp = improvement
            best_dest = dest
            best_src = src
            best_cust = cust
            best_pos = pos
    if(best_dest != -1):
        return best_imp, lambda : HandleMove(routes,best_src,best_dest,best_pos,best_cust)
    else:
        return -math.inf,None
    


def OptimizeInstance(instance_name,id,print_extended_info=False):
    name,num_vehiles,vehicle_capacity,customers = psi.ParseInstance(instance_name)

    customers[1:] = sorted(customers[1:],key=GetDueDate)

    routes = [ Route(customers,vehicle_capacity) for _ in range(num_vehiles)]
    #print(CalcTotalDistance(routes))
    to_add = [i for i in range(1,len(customers))]

    #Create an initial solution
    for route in routes:
        skipped_all = False
        while not skipped_all:
            to_remove = -1
            res = True
            for cust in to_add:
                if(route.AddCustomerToEnd(cust)):
                    to_remove = cust
                    res = False
                    break
            skipped_all = res
            if(not res):
                to_add.remove(to_remove)
    amt_imp = 0
    amt_worse = 0
    amt_notdone = 0

    iteration =0
    temp = 30
    alpha = 0.98
    totalp = 0
    countp = 0
    while(iteration < 40000000):
        p = random.uniform(0,1)
        i = 0
        action = None
        if(p <= 1):
            i, action =  MoveRandomCustomer(routes)
        if(not (action is None)):
            if(i >0):
                action()
                amt_imp += 1
            else:
                 a_p = math.exp(i/temp)
                 totalp += a_p
                 countp += 1
                 if(random.uniform(0,1) <= a_p ):
                     amt_worse += 1
                     action()
        else:
            amt_notdone += 1
        if(iteration % 10000 == 0 and iteration != 0):
            # if(countp != 0):
            #     print("Average a_p after first 1000 it:",totalp/countp,countp)
            # else:
            #     print("Average a_p after first 1000 it:","No worse",amt_notdone,amt_imp)
            # return
            temp *= alpha
        if(iteration % 10000 == 0 and iteration != 0):
            print(f"{id}: Temp: {temp}, Score: {CalcTotalDistance(routes)}")
        iteration += 1

    print(f"DONE {id}: {name}, Score: {CalcTotalDistance(routes)}")
    #print("",)
    if print_extended_info:
        i =0
        for route in routes:
            print("",f"{i}: {route.CalcDist()}")
            i += 1
        print("",f"Total: {amt_notdone + amt_imp + amt_worse}, improvements: {amt_imp}, worse: {amt_worse}, not done: {amt_notdone}")



def OptimizeAll():
    dir = "solomon_instances"

    for filename in os.listdir(dir):
        if filename.endswith(".txt"): 
            OptimizeInstance(dir +"/" +  filename,0)
            continue
        else:
            continue
if __name__ == '__main__':
    with Pool(6) as p:
        p.starmap(OptimizeInstance,[("solomon_instances/c101.txt",0),("solomon_instances/c101.txt",1),("solomon_instances/c101.txt",2),("solomon_instances/c101.txt",3),("solomon_instances/c101.txt",4),("solomon_instances/c101.txt",5)])
    #OptimizeInstance("solomon_instances/c101.txt")
#OptimizeAll()