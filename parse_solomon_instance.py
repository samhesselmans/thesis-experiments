import re

def ParseInstance(fileName):
    with open(fileName) as file:
        name = file.readline().replace('\n','')
        file.readline()
        file.readline()
        file.readline()
        line = file.readline().replace('\n','')
        line = re.sub("  +"," ",line)[1:].split(' ')
        num_vehiles = int(line[0])
        vehicle_capacity = int(line[1])

        file.readline()
        file.readline()
        file.readline()
        file.readline()
        customers = []
        line = file.readline().replace('\n','')
        while line != "":
            #do stuff
            line = re.sub("  +"," ",line)[1:].split(' ')
            customers.append((int(line[0]),int(line[1]),int(line[2]),int(line[3]),int(line[4]),int(line[5]),int(line[6])))
            line = file.readline().replace('\n','')
        return(name,num_vehiles,vehicle_capacity,customers)






