import numpy as np
from numpy.core.numeric import Infinity
from numpy.lib.financial import _fv_dispatcher;
from scipy.optimize import fsolve

power_settings = [50,100,150,200,250]
cyclist_power = 150

Cd = 1.18
A = 0.83
Ro = 1.18
Cr = 0.01
g= 9.81

bike_mass = 150

def calcFD(v):
    return Cd * A * Ro * pow(v,2) /2


power_setting = 0

func = lambda v: (calcFD(v) + Cr * bike_mass * g) * v/0.95 - (power_setting + cyclist_power)
maxSpeedPowerSetting = []

for power in power_settings:
    power_setting = power
    sol = fsolve(func,5)
    maxSpeedPowerSetting.append(min(sol[0],6.944))



#Electric power for the different bike settings

def generateCustomers(num_customers, max_speed):
    customer_distances = np.random.randint(1000,30000,num_customers)
    customer_timewindows=[]
    #Time in minutes
    prev_min = 0

    max_time_window_start_variation = 30
    min_window_size = 30
    max_window_size = 180
    for customer in customer_distances:
        travel_time = customer/(max_speed * 60)
        print(travel_time)
        timeWindowMin = np.random.randint(max(prev_min-max_window_size/3,0) ,max(prev_min-max_window_size/3,0)+ max_time_window_start_variation)
        timeWindowMax = np.random.randint(max(timeWindowMin,prev_min+ travel_time) + min_window_size,timeWindowMin + max_window_size)

        prev_min = max(timeWindowMin,prev_min+ travel_time)
        customer_timewindows.append((timeWindowMin,timeWindowMax))
    return customer_distances,customer_timewindows



max_speed = maxSpeedPowerSetting[len(maxSpeedPowerSetting)-1]
num_customers = 30
#Dinstances in meters

customer_distances,customer_timewindows = generateCustomers(num_customers,max_speed)
#customer_distances = [ 419,1356 ,1574 ,1751 , 692,  965 ,1155 , 364 ,1644 ,1975 ,2318, 1281, 1735, 2899, 2905, 2251, 1429, 2053,  896, 1959]
#customer_timewindows = [(28, 73), (11, 94), (13, 69), (29, 95), (3, 133), (0, 78), (25, 82), (26, 179), (18, 193), (10, 169), (26, 96), (29, 110), (15, 118), (14, 118), (37, 164), (35, 183), (30, 164), (40, 190), (47, 223), (64, 181)]

handling_time = 0



def branchAndBound(customer_distances,customer_timewindows,speed_options,power_options,best_solution,best_solution_value,current_time,current_value,current_solution):
    max_speed = speed_options[len(speed_options)-1]
    total_dist = 0

    if(len(customer_timewindows) == 0):
        if (current_value<best_solution_value):
            return current_solution,current_value

    #Check if there still is a solution possible
    for i in range(len(customer_distances)):
        total_dist +=customer_distances[i]
        if(total_dist/(max_speed*60) + current_time > customer_timewindows[i][1]):
            return best_solution,best_solution_value
    #Check if the solution can be better than the current best
    min_remaining_cost = total_dist/speed_options[0] * power_options[0]
    if(current_value + min_remaining_cost >= best_solution_value):
        return best_solution,best_solution_value
    
    

    #Else branch
    # if(current_time + customer_distances[0]/speed_options[0]*60 < customer_timewindows[0][1]):
    #     best_solution, best_solution_value = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value, max( current_time + customer_distances[0]/speed_options[0]*60,customer_timewindows[0][0]),current_value + customer_distances[0]/speed_options[0]*60 * power_options[0],current_solution.append(0))
    
    # if(current_time + customer_distances[0]/speed_options[1]*60 < customer_timewindows[0][1]):
    #     best_solution, best_solution_value = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value,max( current_time + customer_distances[0]/speed_options[1]*60,customer_timewindows[0][0]),current_value + customer_distances[0]/speed_options[1]*60 * power_options[1],current_solution.append(1))

    # if(current_time + customer_distances[0]/speed_options[2]*60 < customer_timewindows[0][1]):
    #     best_solution, best_solution_value = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value,max( current_time + customer_distances[0]/speed_options[2]*60,customer_timewindows[0][0]),current_value + customer_distances[0]/speed_options[2]*60 * power_options[2],current_solution.append(2))

    for i in range(len(speed_options)):
        if(current_time + customer_distances[0]/(speed_options[i]*60) < customer_timewindows[0][1]):
            best_solution, best_solution_value = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value, max( current_time + customer_distances[0]/(speed_options[i]*60),customer_timewindows[0][0]),current_value + customer_distances[0]/(speed_options[i]*60) * power_options[i],current_solution + str(i))

    return best_solution,best_solution_value

print (branchAndBound(customer_distances,customer_timewindows,maxSpeedPowerSetting,power_settings,[],Infinity,0,0,""))

# print(customer_timewindows)
# print([customer/(max_speed * 60) for customer in customer_distances])
# print(customer_distances)


