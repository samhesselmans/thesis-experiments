from ast import Mod
import matplotlib.pyplot as plt
from numpy import double


# scoresLabels = []
# scores = []

# bestScoreLabels = []
# bestScores = []


# with open("SA-ILP/SA-ILP/bin/Release/net6.0/SearchScores.txt") as file:
#     lines = file.readlines()
#     for line in lines:
#         #line = line.replace("(","").replace(")","")
#         lineSplit = line.split(";")
#         scoresLabels.append(int(lineSplit[0]))
#         scores.append(float(lineSplit[1].replace(",",".")))


# with open("SA-ILP/SA-ILP/bin/Release/net6.0/BestScores.txt") as file:
#     lines = file.readlines()
#     for line in lines:
#         #line = line.replace("(","").replace(")","")
#         lineSplit = line.split(";")
#         bestScoreLabels.append(int(lineSplit[0]))
#         bestScores.append(float(lineSplit[1].replace(",",".")))
# print("Done reading files")


# plt.plot(scoresLabels,scores)
# plt.show()

# plt.plot(bestScoreLabels,bestScores)
# plt.show()


travelTime = []
Mean = []
Mode = []

with open("SA-ILP/SA-ILP/bin/Debug/net6.0/data.txt") as file:
    lines = file.readlines()
    for line in lines:
        linesplit = line.split(';')
        travelTime.append(double(linesplit[0].replace(",",".")))
        Mean.append(double(linesplit[1].replace(",",".")))
        Mode.append(double(linesplit[2].replace(",",".")))


plt.scatter(travelTime,Mean,s=1)
plt.show()

plt.scatter(travelTime,Mode,s=1)
plt.show()