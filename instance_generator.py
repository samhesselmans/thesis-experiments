import numpy as np
import time
from numpy.core.numeric import Infinity
# from numpy.lib.financial import _fv_dispatcher;
from scipy.optimize import fsolve
from datetime import date, datetime
import json




#Electric power for the different bike settings

def generateCustomers(num_customers, max_speed):
    customer_distances = np.random.randint(200,2000,num_customers)
    customer_timewindows=[]
    #Time in minutes
    prev_min = 0

    max_time_window_start_variation = 30
    min_window_size = 15
    max_window_size = 45
    for customer in customer_distances:
        travel_time = customer/(max_speed * 60)
        print(travel_time)
        timeWindowMin = np.random.randint(max(prev_min-max_window_size/3,0) ,max(prev_min-max_window_size/3,0)+ max_time_window_start_variation)
        timeWindowMax = np.random.randint(max(timeWindowMin + min_window_size,prev_min+ travel_time) ,max(timeWindowMin + max_window_size,prev_min+ travel_time) )

        prev_min = max(timeWindowMin,prev_min+ travel_time)
        customer_timewindows.append((timeWindowMin,timeWindowMax))
    return customer_distances,customer_timewindows


Cd = 1.18
A = 0.83
Ro = 1.18
Cr = 0.01
g= 9.81


def calcFD(v):
        return Cd * A * Ro * pow(v,2) /2

def CalcTotalForce(v,bike_mass,slope=0):
    return (calcFD(v) + Cr * bike_mass * g * np.cos(np.arctan(slope)) + bike_mass * g *np.sin(np.arctan(slope))) * v/0.95

def GenerateInstance(num_customers,power_settings = [50,100,150,200,250],cyclist_power = 150,bike_mass = 150,save_instance=False):
    power_setting = 0

    func = lambda v: CalcTotalForce(v,bike_mass) - (power_setting + cyclist_power)
    maxSpeedPowerSetting = []

    for power in power_settings:
        power_setting = power
        sol = fsolve(func,5)
        maxSpeedPowerSetting.append(min(sol[0],6.944))
    max_speed = maxSpeedPowerSetting[len(maxSpeedPowerSetting)-1]
    customer_distances,customer_timewindows = generateCustomers(num_customers,max_speed)

    if save_instance:
        with open('instances/' + datetime.now().strftime("%m-%d-%Y-%H-%M-%S") + ".txt",'w') as outFile:
            json.dump((maxSpeedPowerSetting,power_settings,customer_distances.tolist(),customer_timewindows),outFile)

    return maxSpeedPowerSetting,power_settings,customer_distances,customer_timewindows


def OpenInstance(instance_name):
    with open("instances/"+instance_name) as json_file:
        maxSpeedPowerSetting,power_settings,customer_distances,customer_timewindows = json.load(json_file)
        return maxSpeedPowerSetting,power_settings,customer_distances,customer_timewindows

