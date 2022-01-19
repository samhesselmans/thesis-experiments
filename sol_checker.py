import parse_solomon_instance as psi
import math

name,num_vehiles,vehicle_capacity,customers = psi.ParseInstance("solomon_instances/rc101.txt")
routes = [(0, 69, 98, 88, 53, 78, 60, 0)
            ,(0, 92, 31, 29, 27, 26, 32, 93, 0)
            ,(0, 72, 36, 38, 41, 40, 43, 37, 35, 0)
            ,(0, 59, 75, 87, 97, 58, 77, 0)
            ,(0, 39, 42, 44, 61, 81, 68, 55, 0)
            ,(0, 65, 52, 99, 57, 86, 74, 0)
            ,(0, 95, 62, 67, 71, 94, 96, 54, 0)
            ,(0, 14, 47, 12, 73, 79, 46, 4, 100, 0)
            ,(0, 23, 21, 18, 49, 22, 20, 25, 24, 0)
            ,(0, 82, 11, 15, 16, 9, 10, 13, 17, 0)
            ,(0, 64, 51, 85, 84, 56, 66, 0)
            ,(0, 90, 0)
            ,(0, 83, 19, 76, 89, 48, 0)
            ,(0, 5, 45, 2, 7, 6, 8, 3, 1, 70, 0)
            ,(0, 63, 33, 28, 30, 34, 50, 91, 80, 0)]

total_dist = 0
customers_visited = set()
for route in routes:
    
    arrival_time = 0
    failed = False
    used_capacity = 0
    
    for i in range(len(route)-1):
        customers_visited.add(route[i])
        used_capacity += customers[route[i]][3]
        if(used_capacity > vehicle_capacity):
            print(f"FAIL exceeded vehicle capacity {route}")
            failed = True
        if(arrival_time > customers[route[i]][5]):
            failed = True
            print(f"FAIL did not meet customer {route[i]}:{customers[route[i]]} due date. Arrived on {arrival_time + dist_to_next} on route {route}")
        # arrival_time += dist_to_next
        if(arrival_time  <  customers[route[i]][4]):
            arrival_time = customers[route[i]][4]
        
        dist_to_next = math.sqrt(pow(customers[route[i]][1] -customers[route[i+1]][1] ,2) + pow(customers[route[i]][2] -customers[route[i+1]][2] ,2) )
        total_dist += dist_to_next
        arrival_time += dist_to_next + customers[route[i]][6]
    if not failed:
        print(f"PASS {route}")
        
print(total_dist)
if(len(customers_visited) != len(customers)):
    print(f"FAIL, did not visit all customers")
    print(len(customers_visited))