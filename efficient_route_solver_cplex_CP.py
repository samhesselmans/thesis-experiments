from json import load
from docplex.cp.model import CpoModel
from collections import namedtuple
import instance_generator as ig

#customer_distances = [ 419,1356 ,1574 ,1751 , 692,  965 ,1155 , 364 ,1644 ,1975 ,2318, 1281, 1735, 2899, 2905, 2251, 1429, 2053,  896, 1959]
#customer_timewindows = [(28, 73), (11, 94), (13, 69), (29, 95), (3, 133), (0, 78), (25, 82), (26, 179), (18, 193), (10, 169), (26, 96), (29, 110), (15, 118), (14, 118), (37, 164), (35, 183), (30, 164), (40, 190), (47, 223), (64, 181)]

maxSpeedPowerSetting,power_settings,customer_distances,customer_timewindows = ig.OpenInstance("12-10-2021-15-47-08.txt")#ig.GenerateInstance(25)

#Create matrix for travel time in minutes
travel_time_matrix = []
for customer in customer_distances:
    row = []
    for max_speed in maxSpeedPowerSetting:
        row.append(customer/(max_speed * 60))
    travel_time_matrix.append(row)

mdl = CpoModel()

NB_SPEED = len(power_settings)
NB_CUSTOMERS = len(customer_distances)
speed_used = mdl.integer_var_list(NB_CUSTOMERS,0,NB_SPEED-1,"Speed chosen")
#arrival_times = mdl.float_var_list(NB_CUSTOMERS)
arrival_times = [mdl.float_var() for _ in range(NB_CUSTOMERS)]
start_time = mdl.float_var()
# for sx in range(NB_SPEED):
#     mdl.add(mdl.count(speed_used,sx))

# Build an expression that computes total cost

#Travel time constraint
for cx in range(1,NB_CUSTOMERS):
    mdl.add(arrival_times[cx] - mdl.element(speed_used[cx], travel_time_matrix[cx]) ==arrival_times[cx-1])
mdl.add(arrival_times[0] == mdl.element(speed_used[0], travel_time_matrix[0]) + start_time)

for cx in range(NB_CUSTOMERS):
    mdl.add(customer_timewindows[cx][0] <= arrival_times[cx])
    # mdl.add(arrival_times[cx] <=customer_timewindows[cx][1])
total_cost = 0
for cx in range(NB_CUSTOMERS):
    total_cost = total_cost + mdl.element(speed_used[cx], travel_time_matrix[cx]) * mdl.element(speed_used[cx],power_settings)#power_settings[ speed_used[cx]]

    


mdl.add(mdl.minimize(total_cost))
print("\nSolving model....")
msol = mdl.solve(TimeLimit=10)
print("Total cost is: {}".format(msol.get_objective_values()[0][0]))
res = ""
for cx in range(NB_CUSTOMERS):
    #print(msol[speed_used[cx]])
    res += str(msol[speed_used[cx]])
print(res)

res = ""
for cx in range(NB_CUSTOMERS):
    #print(msol[speed_used[cx]])
    res += str(msol[arrival_times[cx]]) + " "
print(res)