from random import randrange
from re import I
import re
from pandas import read_sql_query

from pyrsistent import b
from scipy import rand
import parse_vrpltt_instance as psi
import random
import json
import requests

res = psi.ParseInstance("vrpltt_instances/large/madrid_full.csv")

base_url = "http://samhesselmans.com:5000/table/v1/bike/"


#points generated using: http://www.geomidpoint.com/random/

customers = []
with open("utrecht.csv") as file:
    lines = file.readlines()
    for line in lines:
        split = line.split(',')
        customers.append({"breedte":split[1],"lengte":split[3]})

max_breedte = 52.091478
min_breedte = 52.086885


max_lengte = 5.108419
min_lengte = 5.088787
#with open("utrecht.csv","w") as file:
for cust in customers:
    base_url += cust["lengte"] + "," + cust["breedte"] + ";"
    #file.write(str(lengte) + "," + str(breedte)  + "\n")
base_url = base_url[:-1]
base_url += "?annotations=distance"
print(base_url )


response = requests.get(base_url)
parsed = json.loads(response.text)


base_elevation_url = "https://api.opentopodata.org/v1/eudem25m?locations="
request = base_elevation_url
for dest in parsed["destinations"]:
    request += str(dest["location"][1]) + "," + str(dest["location"][0]) + "|"
request = request[:-1]
response_elevation = requests.get(request)
parsed_elevation = json.loads(response_elevation.text)
print(parsed_elevation)


length = len(parsed["destinations"])
#GENERATE THE VRPLTT INSTANCE
with open("utrecht_full.csv","w") as outfile: 
    outfile.write(f",x,y,elevatiom,demand,tw a, tw b,s,{','.join(str(x) for x in range( length))}\n")
    for i in range(length):
        customer_id = str(i)
        latitude = str(parsed["destinations"][i]["location"][1])
        longitude = str(parsed["destinations"][i]["location"][0])
        elevation = str(parsed_elevation["results"][i]["elevation"])
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

