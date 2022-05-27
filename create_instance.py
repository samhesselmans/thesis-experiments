from random import randrange
from time import sleep
import parse_vrpltt_instance as psi
import random
import json
import requests




#points generated using: http://www.geomidpoint.com/random/ with a 2km radius from Neude square
points_file = "utrecht.csv" #File with comma separated data from geomidpoint's random generator
output_file = "utrecht_full.csv"

customers = []
with open(points_file) as file:
    lines = file.readlines()
    for line in lines:
        split = line.split(',')
        customers.append({"breedte":split[1],"lengte":split[3]})


#Create distance API request. This is not permanently hosted

base_url = "http://samhesselmans.com:5000/table/v1/bike/"

request = base_url


for cust in customers:
    request += cust["lengte"] + "," + cust["breedte"] + ";"
    #file.write(str(lengte) + "," + str(breedte)  + "\n")
request = request[:-1]
request += "?annotations=distance"
print(request )


response = requests.get(request)
parsed = json.loads(response.text)

#Only supports requests of 100 points at the same time
base_elevation_url = "https://api.opentopodata.org/v1/eudem25m?locations="

#Elevation API only supports calls of 100 customers at a time
elevationAPI_results = []

#Create the elevation API request
request = base_elevation_url

count = 0

for dest in parsed["destinations"]:
    request += str(dest["location"][1]) + "," + str(dest["location"][0]) + "|"
    count += 1
    if(count == 100):
        requests.get(request)
        response_elevation = requests.get(request)
        parsed_elevation = json.loads(response_elevation.text)
        elevationAPI_results.append(parsed_elevation)
        request = base_elevation_url
        #API only allows one call every second
        sleep(1)

request = request[:-1]


response_elevation = requests.get(request)
parsed_elevation = json.loads(response_elevation.text)
elevationAPI_results.append(parsed_elevation)
print(parsed_elevation)



fixedElevations = []

index = 0
for result in elevationAPI_results:
    for res in result["results"]:
        fixedElevations.append(res)
        #index+= 1


length = len(parsed["destinations"])
#GENERATE THE VRPLTT INSTANCE
with open(output_file,"w") as outfile: 
    outfile.write(f",x,y,elevatiom,demand,tw a, tw b,s,{','.join(str(x) for x in range( length))}\n")
    for i in range(length):
        customer_id = str(i)
        latitude = str(parsed["destinations"][i]["location"][1])
        longitude = str(parsed["destinations"][i]["location"][0])
        elevation = str(fixedElevations[i]["elevation"])
        service_time = str(5)
        demand = str(random.randint(5,15))
        tw_lb = random.uniform(0,100)
        tw_up = str(tw_lb + random.uniform(30,80))
        tw_lb = str(tw_lb)
        if(i ==0):
            service_time = ""
            demand = ""
            tw_lb = ""
            tw_up = ""

        distances = parsed["distances"][i]

        true_distances = [0 for _ in range(i) ] + distances[i:]

        outfile.write(f"{customer_id},{latitude},{longitude},{elevation},{demand},{tw_lb},{tw_up},{service_time},{','.join(map(str, (x/1000 for x in true_distances)))}\n")

