import enum
import parse_solomon_instance as psi
import math
import os


class Route:
    def __init__(self,customers,max_capacity):
        self._route = [0,0]
        self.time_done_with_last_cust = 0
        self.last_cust = 0
        self.customers = customers
        self.used_capacity = 0
        self.max_capacity = max_capacity

    def AddCustomerToEnd(self,cust):
        dist = math.sqrt(pow(self.customers[cust][1] - self.customers[self.last_cust][1],2) + pow(self.customers[cust][2] - self.customers[self.last_cust][2],2))
        #This if can be removed later to allow infeasible solution
        if(self.time_done_with_last_cust + dist < self.customers[cust][5] and self.used_capacity + self.customers[cust][3] <= self.max_capacity):
            self._route.insert(len(self._route)-1,cust)
            self.last_cust = cust
            self.used_capacity += self.customers[cust][3]
            self.time_done_with_last_cust = self.time_done_with_last_cust + dist + self.customers[cust][6]
            return True
        else:
            return False
    
    def CalcDist(self):
        total_dist = 0
        for index,cust in enumerate(self._route):
            if(index >= len(self._route) -1):
                break
            next_cust = self._route[index + 1]
            dist = math.sqrt(pow(self.customers[cust][1] - self.customers[next_cust][1],2) + pow(self.customers[cust][2] - self.customers[next_cust][2],2))
            total_dist += dist
        return total_dist
    
    



def GetDueDate(elem):
    return elem[5]






def CalcTotalDistance(routes):
    total_dist = 0
    for route in routes:
        total_dist += route.CalcDist()
    return total_dist


def OptimizeInstance(instance_name):
    name,num_vehiles,vehicle_capacity,customers = psi.ParseInstance(instance_name)

    customers[1:] = sorted(customers[1:],key=GetDueDate)

    routes = [ Route(customers,vehicle_capacity) for _ in range(num_vehiles)]
    print(CalcTotalDistance(routes))
    to_add = [i for i in range(1,len(customers))]

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
    print(name + ":")
    print("",to_add)
    print("",CalcTotalDistance(routes))

# dir = "solomon_instances"

# for filename in os.listdir(dir):
#     if filename.endswith(".txt"): 
#         OptimizeInstance(dir +"/" +  filename)
#         continue
#     else:
#         continue