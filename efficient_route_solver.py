import numpy as np
import time
from numpy.core.numeric import Infinity
# from numpy.lib.financial import _fv_dispatcher;
from scipy.optimize import fsolve

power_settings = [50,100,150,200,250]
cyclist_power = 150

Cd = 1.18
A = 0.83
Ro = 1.18
Cr = 0.01
g= 9.81

bike_mass = 350

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



max_speed = maxSpeedPowerSetting[len(maxSpeedPowerSetting)-1]
num_customers = 25
#Dinstances in meters

customer_distances,customer_timewindows = generateCustomers(num_customers,max_speed)
# print(customer_distances)
# print(customer_timewindows)
#customer_distances = [19797, 11849, 24479, 17410, 23484, 16410 ,22480, 27691, 12591,  5551]
#customer_timewindows = [(15, 92), (9, 180), (17, 183), (77, 237), (138, 308), (192, 345), (226, 376), (278, 432), (338, 456), (368, 542)]
#customer_distances = [ 419,1356 ,1574 ,1751 , 692,  965 ,1155 , 364 ,1644 ,1975 ,2318, 1281, 1735, 2899, 2905, 2251, 1429, 2053,  896, 1959]
#customer_timewindows = [(28, 73), (11, 94), (13, 69), (29, 95), (3, 133), (0, 78), (25, 82), (26, 179), (18, 193), (10, 169), (26, 96), (29, 110), (15, 118), (14, 118), (37, 164), (35, 183), (30, 164), (40, 190), (47, 223), (64, 181)]

handling_time = 0
timesBranched = 0
timesDiscardedForWorse = 0
averageDepthDiscardedWorse = 0
timesDiscardedForNotPossible =0
averageDepthDiscardedNotPossible = 0
timesCutPreviousSubBranchBetter =0
timesEndedBranching = 0
timesEndedBranchingImprovement = 0
timesEndedExec = 0
start_time = time.time()

# #If sub solution is optimal, other branches from that node do not need to be explored
# def ShouldBranch(sol, branch):
#     for number in sol:
#         if(int(number) > 0):
#             return True
#     return False

def branchAndBound(customer_distances,customer_timewindows,speed_options,power_options,best_solution,best_solution_value,current_time,current_value,current_solution):
    max_speed = speed_options[len(speed_options)-1]
    total_dist = 0
    global timesBranched
    global timesDiscardedForNotPossible
    global timesDiscardedForWorse
    global timesEndedBranching
    global timesEndedBranchingImprovement
    global timesEndedExec
    global averageDepthDiscardedNotPossible
    global timesCutPreviousSubBranchBetter
    global averageDepthDiscardedWorse
    global handling_time
    if(len(customer_timewindows) == 0):
        
        if (current_value<best_solution_value):
            timesEndedBranchingImprovement += 1
            return current_solution,current_value, True
        else:
            timesEndedBranching += 1
            return best_solution,best_solution_value, False

    #Check if there still is a solution possible
    for i in range(len(customer_distances)):
        total_dist +=customer_distances[i]
        if(total_dist/(max_speed*60) + current_time > customer_timewindows[i][1]):
            #print("stopped because not viable on depth",len(current_solution))
            averageDepthDiscardedNotPossible = (averageDepthDiscardedNotPossible * timesDiscardedForNotPossible + len(current_solution))/(timesDiscardedForNotPossible + 1)
            timesDiscardedForNotPossible += 1
            return best_solution,best_solution_value, False

    #Check is best solution is optimal
    # if(len(best_solution) > 0 and best_solution.count('0') == len(best_solution) ):
    #     timesCutSolutionAlreadyOptimal += 1
    #     return best_solution,best_solution_value

    #Check if the solution can be better than the current best
    minimum_average_speed = 0
    # for speed in range(len(speed_options)):
    #     if(total_dist/(speed_options[speed]*60) + current_time <= customer_timewindows[len(customer_timewindows)-1][1]):
    #         minimum_average_speed=speed
    #         break
    min_remaining_cost = total_dist/(speed_options[minimum_average_speed] * 60) * power_options[minimum_average_speed]
    if(current_value + min_remaining_cost >= best_solution_value):
        #print("stopped because not better on depth",len(current_solution))
        averageDepthDiscardedWorse = (averageDepthDiscardedWorse * timesDiscardedForWorse + len(current_solution))/(timesDiscardedForWorse + 1)

        timesDiscardedForWorse += 1
        return best_solution,best_solution_value, False
    
    

    #Else branch
    # if(current_time + customer_distances[0]/speed_options[0]*60 < customer_timewindows[0][1]):
    #     best_solution, best_solution_value = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value, max( current_time + customer_distances[0]/speed_options[0]*60,customer_timewindows[0][0]),current_value + customer_distances[0]/speed_options[0]*60 * power_options[0],current_solution.append(0))
    
    # if(current_time + customer_distances[0]/speed_options[1]*60 < customer_timewindows[0][1]):
    #     best_solution, best_solution_value = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value,max( current_time + customer_distances[0]/speed_options[1]*60,customer_timewindows[0][0]),current_value + customer_distances[0]/speed_options[1]*60 * power_options[1],current_solution.append(1))

    # if(current_time + customer_distances[0]/speed_options[2]*60 < customer_timewindows[0][1]):
    #     best_solution, best_solution_value = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value,max( current_time + customer_distances[0]/speed_options[2]*60,customer_timewindows[0][0]),current_value + customer_distances[0]/speed_options[2]*60 * power_options[2],current_solution.append(2))
    best_found_in_branch = False
    for i in range(len(speed_options)):
        #Only branch if we are able to serve the next customer with that speed
        if(current_time + customer_distances[0]/(speed_options[i]*60) < customer_timewindows[0][1]):
            #Do not branch if we found the best solution in a sub branch and all speeds found are better than this potential branch
            # if not best_found_in_branch  or ShouldBranch(best_solution[len(current_solution):],i):           
            timesBranched += 1
            best_solution, best_solution_value,best_found_new = branchAndBound(customer_distances[1:],customer_timewindows[1:],speed_options,power_options,best_solution,best_solution_value, max( current_time + customer_distances[0]/(speed_options[i]*60),customer_timewindows[0][0]),current_value + customer_distances[0]/(speed_options[i]*60) * power_options[i],current_solution + str(i))
            best_found_in_branch = best_found_in_branch or best_found_new
            # else:
            #     timesCutPreviousSubBranchBetter += 1
    timesEndedExec += 1
    return best_solution,best_solution_value, best_found_in_branch

print (branchAndBound(customer_distances,customer_timewindows,maxSpeedPowerSetting,power_settings,[],Infinity,0,0,""))
print(timesBranched,"Time taken: ", time.time()-start_time)
print("\nWorse",timesDiscardedForWorse,"\nNot possible",timesDiscardedForNotPossible,"\nFinished branch without improvement",timesEndedBranching,"\nEnded with improvement",timesEndedBranchingImprovement, "\nTimes reached end of all branchings in node",timesEndedExec, "\nTotal", timesDiscardedForWorse +timesDiscardedForNotPossible+timesEndedBranching+timesEndedBranchingImprovement+timesEndedExec)
print("Times ended because Previous subrbanch better",timesCutPreviousSubBranchBetter)
print(averageDepthDiscardedWorse,averageDepthDiscardedNotPossible)
print("Total distance",sum(customer_distances))
print(customer_timewindows)
# print(customer_timewindows)
# print([customer/(max_speed * 60) for customer in customer_distances])
# print(customer_distances)


