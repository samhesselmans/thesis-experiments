from collections import namedtuple

from docplex.mp.model import Model
from docplex.util.environment import get_environment
import instance_generator as ig

maxSpeedPowerSetting,power_settings,customer_distances,customer_timewindows = ig.GenerateInstance(250,bike_mass=850,save_instance=False)#ig.OpenInstance("12-10-2021-15-20-51.txt")

travel_time_matrix = []
for customer in customer_distances:
    row = []
    for max_speed in maxSpeedPowerSetting:
        row.append(customer/(max_speed * 60))
    travel_time_matrix.append(row)

mdl = Model(name="Effiientroutes",log_output=True, float_precision=6)
cust = [i for i in range(len(customer_distances))]
speed = [i for i in range(len(maxSpeedPowerSetting))]


#Decision variables
speed_descision = mdl.var_matrix(mdl.binary_vartype,cust,speed,name=lambda f: "sp_" +str(f))#mdl.var_dict(foods, mdl.integer_vartype, lb=lambda f: f.qmin, ub=lambda f: f.qmax, name=lambda f: "q_%s" % f.name)
arrival_times = mdl.var_list(cust,mdl.continuous_vartype,lb=0,name=lambda f: "ar_" +str(f))
wait_time = mdl.var_list(cust,mdl.continuous_vartype,lb=0,name=lambda f: "wt_" +str(f))


for cx in cust:
    mdl.add_constraint(mdl.sum(speed_descision[cx,vx] for vx in speed) == 1)

#Update arrival times
for cx in range(1,len(cust)):
    mdl.add_constraint(arrival_times[cx] - mdl.sum(speed_descision[cx,cv] * travel_time_matrix[cx][cv] for cv in speed) - wait_time[cx] == arrival_times[cx-1])
mdl.add_constraint(arrival_times[0] == wait_time[0] + mdl.sum(speed_descision[0,cv] * travel_time_matrix[0][cv] for cv in speed))

#Timewindow constraints
for cx in cust:
    mdl.add_range(customer_timewindows[cx][0],arrival_times[cx],customer_timewindows[cx][1])

total_cost = mdl.sum(speed_descision[cx,cv] * travel_time_matrix[cx][cv] * power_settings[cv] for cx in cust for cv in speed)
mdl.minimize(total_cost)
sol = mdl.solve()
if sol:
    vars = mdl.find_matching_vars(pattern="sp_")
    res = ""
    for v in vars:
        if(v.solution_value == 1):
            #print(v.name.split(', ')[1][0])
            res += v.name.split(', ')[1][0]
    print(res)

    ars = mdl.find_matching_vars(pattern="ar")
    wts = mdl.find_matching_vars(pattern="wt")
    for ar in range(len(ars)):
        print(ars[ar].solution_value,customer_timewindows[ar],wts[ar].solution_value)
else:
    print("NO SOLUTION")