import enum
#import profile
import parse_solomon_instance as psi
import math
import os
import random
import numpy as np
from multiprocessing import Pool
import time
from docplex.mp.model import Model
import traceback
import copy
import sol_checker as sc


class Route:
    def __init__(self,customers,distance_matrix,max_capacity):
        self._route = [0,0]
        self._arrival_times = [0,0]
        self.distance_matrix = distance_matrix
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
        if(cust1< cust2):
            return self.distance_matrix[cust1,cust2]
        else:
            return self.distance_matrix[cust2,cust1]
        return math.sqrt(pow(self.customers[cust1][1] - self.customers[cust2][1],2) + pow(self.customers[cust1][2] - self.customers[cust2][2],2))

    def RemoveCust(self,cust):
        new_arr_time = 0
        last_cust = -1
        index = -1
        previous_cust = 0
        for i in  range(1,len(self._route)):#enumerate(self._route,start=1):
            if(i >= len(self._route) -1):
                break
            c = self._route[i]
            if(c != cust):
                dist = self.CustomerDist(previous_cust,self._route[i])
                if new_arr_time  + dist < self.customers[c][4]:
                    new_arr_time = self.customers[c][4]
                else:
                    new_arr_time += dist
                self._arrival_times[i] = new_arr_time
                new_arr_time += self.customers[c][6]
                last_cust = c
                previous_cust = c
            else:
                index = i
                next_index = i-1
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
            # arrival_time_at_new_cust = self._arrival_times[i-1] + self.customers[self._route[i-1]][6] 
            # arrival_time_at_new_cust += self.CustomerDist(self._route[i-1],cust) 
            # if(arrival_time_at_new_cust >self.customers[cust][6]):
            #     break
            
            # if(arrival_time_at_new_cust < self.customers[cust][4]):
            #     arrival_time_at_new_cust = self.customers[cust][4]
            
            
            # #Check whether or not this position is feasible
            # possible = True
            # new_arr_time = arrival_time_at_new_cust + self.CustomerDist(self._route[i],cust)  + self.customers[cust][6]
            # for j in range(i,len(self._route)):
            #     if(new_arr_time > self.customers[self._route[j]][5]):
            #         #With the new customer inserted we dont meet the timewindow of j
            #         possible = False
            #         break
            #     if(new_arr_time < self.customers[self._route[j]][4]):
            #         new_arr_time = self.customers[self._route[j]][4]
            #     if(j != len(self._route)-1):
            #         new_arr_time += self.CustomerDist(self._route[j],self._route[j+1])  + self.customers[self._route[j]][6]
            #     #if(self._arrival_times[j] + increase > self.customers[self._route[j]][5]):
            #     #    possible = False
            #     #    break
            possible,ever_possible,dist_increase = self.CustPossibleAtPos(cust,i)
            if(not ever_possible):
                break
            if(possible):
                #dist_increase = self.CustomerDist(self._route[i-1],cust) +  self.CustomerDist(self._route[i],cust) - self.CustomerDist(self._route[i-1],self._route[i])
                #Check if this position is better than the current best position
                if(dist_increase < best_dist_increae):
                    #Update best known solution
                    best_dist_increae = dist_increase
                    best_index = i 

            
        return best_index,best_dist_increae

    def InsertCust(self,cust,pos):
        arrival_time_at_new_cust = self._arrival_times[pos-1] + self.customers[self._route[pos-1]][6] + self.CustomerDist(self._route[pos-1],cust)
        if(arrival_time_at_new_cust < self.customers[cust][4]):
            arrival_time_at_new_cust = self.customers[cust][4]
        #Update arrival times
        try:
            new_arr_time = arrival_time_at_new_cust + self.CustomerDist(self._route[pos],cust)  + self.customers[cust][6]
        except:
            print(f"oeps cust:{cust} pos:{pos} len:{len(self._route)} route:{self._route}")
            traceback.print_stack(limit=50)
        for j in range(pos,len(self._route)):
            if(new_arr_time < self.customers[self._route[j]][4]):
                new_arr_time = self.customers[self._route[j]][4]
            self._arrival_times[j] = new_arr_time
            if(j != len(self._route)-1):
                new_arr_time += self.CustomerDist(self._route[j],self._route[j+1])  + self.customers[self._route[j]][6]
        
        #Insert new values
        self._arrival_times.insert(pos,arrival_time_at_new_cust)
        self._route.insert(pos,cust)
        self.used_capacity += self.customers[cust][3]
        return
    #Returns a random customer and the reduced length of the route that would be caused by removal of said customer
    def RandomCust(self):
        if len(self._route) == 2:
            return None,None
        cust = random.randrange(1,len(self._route)-1)

        return self._route[cust],self.CustomerDist(self._route[cust-1],self._route[cust]) + self.CustomerDist(self._route[cust],self._route[cust+1]) - self.CustomerDist(self._route[cust-1],self._route[cust+1])

    def CustPossibleAtPos(self,cust,pos,skip=0):
        arrival_time_at_new_cust = self._arrival_times[pos-1] + self.customers[self._route[pos-1]][6] 
        arrival_time_at_new_cust += self.CustomerDist(self._route[pos-1],cust)
        if(arrival_time_at_new_cust >self.customers[cust][5]):
            return False,False,-math.inf
    
        if(arrival_time_at_new_cust < self.customers[cust][4]):
            arrival_time_at_new_cust = self.customers[cust][4]
        new_arr_time = arrival_time_at_new_cust + self.CustomerDist(self._route[pos+skip],cust)  + self.customers[cust][6]
        for j in range(pos+skip,len(self._route)):
            if(new_arr_time > self.customers[self._route[j]][5]):
                #With the new customer inserted we dont meet the timewindow of j
                return False,True,-math.inf
            if(new_arr_time < self.customers[self._route[j]][4]):
                new_arr_time = self.customers[self._route[j]][4]
            if(j != len(self._route)-1):
                new_arr_time += self.CustomerDist(self._route[j],self._route[j+1])  + self.customers[self._route[j]][6]
        dist_increase = self.CustomerDist(self._route[pos-1],cust) +  self.CustomerDist(self._route[pos+skip],cust) - self.CustomerDist(self._route[pos-1],self._route[pos])
        #Inlcude all skipped edges in the decrease part of the increase calculation. If edges are skipped, increase can be negative
        for i in range(skip):
            dist_increase -= self.CustomerDist(self._route[pos +i],self._route[pos +i + 1])
        return True,True,dist_increase

    def CanSwap(self,cust1,cust2):
        for i in range(1,len(self._route)-1):
            if self._route[i] == cust1:
                possible,_,dist_inrease = self.CustPossibleAtPos(cust2,i,1)
                possible = possible and (self.used_capacity - self.customers[cust1][3] + self.customers[cust2][3] <= self.max_capacity)
                return possible,dist_inrease,i
                

    def GetRouteTuple(self):
        res = [self.customers[c][0] for c in  self._route]
        return tuple(res)
    def CheckRouteValidity(self):
        arrival_time = 0
        failed = False
        used_capacity = 0
        for i in range(len(self._route)-1):
            #total_dist += dist_to_next
            used_capacity += self.customers[self._route[i+1]][3]
            if(used_capacity > self.max_capacity):
                failed = True
                print(f"FAIL exceeded vehicle capacity {self._route}")
           
            if(arrival_time  > self.customers[self._route[i]][5]):
                failed = True
                print(f"FAIL did not meet customer {self._route[i]}:{self.customers[self._route[i]]} due date. Arrived on {arrival_time + dist_to_next} on route {self._route}")
           # arrival_time += dist_to_next
            if(arrival_time <  self.customers[self._route[i]][4]):
                arrival_time = self.customers[self._route[i]][4]
            if(arrival_time < self._arrival_times[i] -pow(10,-9) or arrival_time > self._arrival_times[i]  + pow(10,-9)):
                print(f"FAIL arrival times did not match {arrival_time} and {self._arrival_times[i]} for cust {self._route[i]} on route {self._route} -> {self.GetRouteTuple()}")
                raise Exception()
            dist_to_next = math.sqrt(pow(self.customers[self._route[i]][1] -self.customers[self._route[i+1]][1] ,2) + pow(self.customers[self._route[i]][2] -self.customers[self._route[i+1]][2] ,2) )
            arrival_time += dist_to_next + self.customers[self._route[i]][6]
        return failed




def GetDueDate(elem):
    return elem[5]






def CalcTotalDistance(routes):
    total_dist = 0
    for route in routes:
        total_dist += route.CalcDist()
    return total_dist

def HandleMove(routes,src,dest,pos,cust):
    if(src == dest):
        print("FFF")
        raise Exception("wow exception")
    routes[src].RemoveCust(cust)
    routes[dest].InsertCust(cust,pos)
    # failed = routes[src].CheckRouteValidity()
    # if(failed):
    #     traceback.print_stack(limit=50)
    #     raise Exception()
    # failed = failed or routes[dest].CheckRouteValidity()
    # if(failed):
    #     traceback.print_stack(limit=50)
    #     raise Exception()

def HandleSwap(routes,src,dest,cust1,pos1,cust2,pos2):
    #print("Swapping!")
    routes[src].RemoveCust(cust1)
    # if(routes[src].CheckRouteValidity()):
    #     print("FAILED AFTER REMOVING")
    routes[src].InsertCust(cust2,pos1)
    # if(routes[src].CheckRouteValidity()):
    #     print("FAILED AFTER INSERTING")
    routes[dest].RemoveCust(cust2)
    routes[dest].InsertCust(cust1,pos2)
    # failed = routes[src].CheckRouteValidity()
    # if(failed):
    #     traceback.print_stack(limit=50)
    #     raise Exception(f"src:{src},dest:{dest}, cust1: {cust1}, cust2:{cust2}, pos:{pos1}, route:{routes[src]._route}, arrival times:{routes[src]._arrival_times}")
    # failed = failed or routes[dest].CheckRouteValidity()
    # if(failed):
    #     traceback.print_stack(limit=50)
    #     raise Exception()

#Swaps two random customers
def SwapRandomCustomers(routes):
    best_dest = -1
    best_src = -1
    best_cust = -1
    best_cust2 = -1
    best_imp = -math.inf
    best_pos1 = -1
    best_pos2 = -1
    for i in range(4):
        dest = random.randrange(len(routes))
        src = random.randrange(len(routes))
        tries = 0
        while((len(routes[dest]._route) == 2 )and tries < 10):
            dest = random.randrange(len(routes))
            tries+= 1
        tries = 0
        while((dest == src or len(routes[src]._route) == 2 )and tries < 10):
            src = random.randrange(len(routes))
            tries+= 1

        if(len(routes[src]._route) == 2 or len(routes[dest]._route) == 2 or dest == src):
            continue
        cust1,_ = routes[src].RandomCust()
        cust2,_ = routes[dest].RandomCust()
        possible1,increase1,pos1 = routes[src].CanSwap(cust1,cust2)
        possible2,increase2,pos2 = routes[dest].CanSwap(cust2,cust1)
        if(possible1 and possible2):
            improvement = -(increase1+increase2)
            if(improvement > best_imp):
                best_dest=dest
                best_src = src
                best_cust = cust1
                best_cust2 = cust2
                best_pos1 = pos1
                best_pos2 = pos2
                best_imp = improvement
    if(best_dest != 1):
        return best_imp,lambda:HandleSwap(routes,best_src,best_dest,best_cust,best_pos1,best_cust2,best_pos2)
    else:
        return None,None
        #raise Exception("Not implemented")


#Swaps tails of two random routes
def SwapRandomTails(routes):
    best_dest =  -1
    best_src = -1
    best_cust = -1
    best_cust2 = -1
    best_imp = -math.inf
    best_pos = -1
    for i in range(4):
        dest = random.randrange(len(routes))
        src = random.randrange(len(routes))
        tries = 0
        while((dest == src or len(routes[src]._route) == 2 )and tries < 10):
            src = random.randrange(len(routes))
            tries+= 1


#Moves a random customer to a different route
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
        if(len(routes[src]._route) == 2 or src == dest):
            continue
        
        cust,decrease = routes[src].RandomCust()

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
    

def CalculateDistanceMatrix(customers):
    matrix = np.empty([len(customers),len(customers)])
    for i in range(len(customers)):
        for j in range(i,len(customers)):
            matrix[i,j] = math.sqrt(pow(customers[i][1] - customers[j][1],2) + pow(customers[i][2] - customers[j][2],2))
    return matrix


def LocalSearchInstance(id,name,num_vehiles,vehicle_capacity,customers,print_extended_info=False):
    customers[1:] = sorted(customers[1:],key=GetDueDate)
    distance_matrix = CalculateDistanceMatrix(customers)
    routes = [ Route(customers,distance_matrix,vehicle_capacity) for _ in range(num_vehiles)]
    #print(CalcTotalDistance(routes))
    to_add = [i for i in range(1,len(customers))]

    #Create an initial solution
    # for route in routes:
    #     skipped_all = False
    #     while not skipped_all:
    #         to_remove = -1
    #         res = True
    #         for cust in to_add:
    #             if(route.AddCustomerToEnd(cust)):
    #                 to_remove = cust
    #                 res = False
    #                 break
    #         skipped_all = res
    #         if(not res):
    #             to_add.remove(to_remove)
    for cust in to_add:
        best_increase = math.inf
        best_pos = -1
        best_route = None
        for route in routes:
            pos,incr = route.BestPossibleInsert(cust)
            if(incr < best_increase):
                best_increase = incr
                best_pos = pos
                best_route = route
        if(best_pos != -1):
            best_route.InsertCust(cust,best_pos)
        else:
            raise Exception("Wow cust past niet")
    for route in routes:
        if(route.CheckRouteValidity()):
            print("START gaat fout")
            return

    amt_imp = 0
    amt_worse = 0
    amt_notdone = 0
    # if(len(to_add) != 0):
    #     raise Exception("WTF!")
    iteration =0
    temp = 30
    alpha = 0.98
    totalp = 0
    countp = 0
    start_time = time.time()
    last_changed_accepted_on_it = -1
    columns = set()
    best_sol = copy.deepcopy(routes)
    best_sol_value = CalcTotalDistance(best_sol)
    current_value = best_sol_value
    while(iteration < 3000000):
        p = random.uniform(0,1)
        i = 0
        action = None
        if(p <= 0.5):
            i, action =  SwapRandomCustomers(routes)#MoveRandomCustomer(routes)
        elif (p <=1):
            i, action =  MoveRandomCustomer(routes)
        if(not (action is None)):
            if(i >0):
                action()
                # if(current_value -i != CalcTotalDistance(routes)):
                #     print ("ERROR")
                current_value -= i
                if(current_value < best_sol_value):
                    best_sol_value = current_value
                    best_sol = copy.deepcopy(routes)
                for route in routes:
                         columns.add(route.GetRouteTuple())
                amt_imp += 1
                last_changed_accepted_on_it = iteration
            else:
                 a_p = math.exp(i/temp)
                 totalp += a_p
                 countp += 1
                 if(random.uniform(0,1) <= a_p ):
                     amt_worse += 1
                     action()
                    #  if(current_value -i != CalcTotalDistance(routes)):
                    #     print ("ERROR")
                     current_value -= i
                     for route in routes:
                         columns.add(route.GetRouteTuple())
                     last_changed_accepted_on_it = iteration
        else:
            amt_notdone += 1
        if(iteration % 10000 == 0 and iteration != 0):
            # if(countp != 0):
            #     print("Average a_p after first 1000 it:",totalp/countp,countp)
            # else:
            #     print("Average a_p after first 1000 it:","No worse",amt_notdone,amt_imp)
            # return
            temp *= alpha
        if(iteration % 100000 == 0 and iteration != 0):
            used = 0
            for route in routes:
                if(len(route._route) >2):
                    used += 1
            print(f"{id}: T: {round(temp,3)}, S: {round(CalcTotalDistance(routes),3)}, TS: {round(current_value,3)}, N: {used}, IT: {iteration}, LA {iteration-last_changed_accepted_on_it}, B: {round(best_sol_value,3)}")
        iteration += 1

    print(f"DONE {id}: {name}, Score: {CalcTotalDistance(best_sol)}, in {time.time() - start_time}s")
    #print("",)
    if print_extended_info:
        # i =0
        # for route in routes:
        #     print("",f"{i}: {route.CalcDist()}")
        #     i += 1
        print("",f" {id}: Total: {amt_notdone + amt_imp + amt_worse}, improvements: {amt_imp}, worse: {amt_worse}, not done: {amt_notdone}")
    return columns

def OptimizeInstance(instance_name,num_threads =1,print_extended_info=False):
    name,num_vehiles,vehicle_capacity,customers = psi.ParseInstance(instance_name)
    original_customers = customers.copy()
    found_columns = []
    print(f"Starting local search on {num_threads} Threads")
    with Pool(num_threads) as p:
        args = [(i,name,num_vehiles,vehicle_capacity,customers.copy(),print_extended_info) for i in range(num_threads)]
        found_columns = p.starmap(LocalSearchInstance,args)
    #found_collumns = LocalSearchInstance(id,name,num_vehiles,vehicle_capacity,customers,print_extended_info)
    found_columns = list(set().union(*found_columns))
    print(f"Done with local search. Starting ILP on {len(found_columns)} columns")
    sol,val = SolveILP(found_columns,original_customers,num_vehiles)
    failed = sc.CheckSolution(instance_name,sol,val)
    return failed,sol,val


def SolveILP(columns,customers,num_vehicles):
    columns = list(columns)
    costs = []
    cust_in_route = np.zeros([len(customers),len(columns)])
    for index,column in enumerate(columns):
        cost = 0
        for i in range(len(column)-1):
            cost += math.sqrt(pow(customers[column[i]][1] - customers[column[i+1]][1],2) + pow(customers[column[i]][2] - customers[column[i+1]][2],2))
            cust_in_route[column[i],index] = 1
            cust_in_route[column[i+1],index] = 1
            
        costs.append(cost)


    mdl = Model(name="Optimized",log_output=True, float_precision=6)
    column_decisions = mdl.var_list([i for i in range(len(columns))],mdl.binary_vartype,name=lambda f: "route_" +str(f))

    for cust in range(1,len(customers)):
        mdl.add_constraint(mdl.sum(cust_in_route[cust,cx] * column_decisions[cx] for cx in range(len(columns)) ) == 1)

    mdl.add_constraint(mdl.sum(column_decisions[cx] for cx in range(len(columns))) <= num_vehicles)
    total_costs = mdl.sum(column_decisions[cx] * costs[cx] for cx in range(len(columns)))
    mdl.minimize(total_costs)
    sol = mdl.solve()
    routes = []
    obj_val = math.inf
    if sol:
        obj_val = sol.objective_value
        vars = mdl.find_matching_vars(pattern="route_")
        i = 0
        for v in vars:
            if(v.solution_value == 1):
                routes.append(columns[i])
                print(columns[i])
            i+= 1
        # res = ""
        # for v in vars:
        #     if(v.solution_value == 1):
        #         #print(v.name.split(', ')[1][0])
        #         res += v.name.split(', ')[1][0]
        # print(res)

        # ars = mdl.find_matching_vars(pattern="ar")
        # wts = mdl.find_matching_vars(pattern="wt")
        # for ar in range(len(ars)):
        #     print(ars[ar].solution_value,customer_timewindows[ar],wts[ar].solution_value)
    else:
        print("NO SOLUTION")
    return routes,obj_val

def OptimizeAll():
    dir = "solomon_instances"
    with open("results.txt","w") as res_file:
        for filename in os.listdir(dir):
            if filename.endswith(".txt"):
                start_time = time.time() 
                failed,sol,val = OptimizeInstance(dir +"/" +  filename,num_threads=6)
                stop_time = time.time()
                if(failed):
                    res_file.write("FAIL ")
                res_file.write(f"{filename}: {val} Found in {round(stop_time-start_time,3)}s\n")
                with open(f"solutions/{filename}","w") as f:
                    if(failed):
                        f.write("FAILED\n")
                    f.write(f"Score: {val}\n")
                    f.write(str(sol))
                continue
            else:
                continue
if __name__ == '__main__':
    OptimizeAll()
    #with Pool(6) as p:
    #    p.starmap(OptimizeInstance,[("solomon_instances/c101.txt",0),("solomon_instances/c101.txt",1),("solomon_instances/c101.txt",2),("solomon_instances/c101.txt",3),("solomon_instances/c101.txt",4),("solomon_instances/c101.txt",5)])
    #OptimizeInstance("solomon_instances/rc105.txt",num_threads=4,print_extended_info=True)
#OptimizeAll()