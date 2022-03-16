import matplotlib.pyplot as plt


scoresLabels = []
scores = []

bestScoreLabels = []
bestScores = []


with open("SA-ILP/SA-ILP/bin/Release/net6.0/SearchScores.txt") as file:
    lines = file.readlines()
    for line in lines:
        #line = line.replace("(","").replace(")","")
        lineSplit = line.split(";")
        scoresLabels.append(int(lineSplit[0]))
        scores.append(float(lineSplit[1].replace(",",".")))


with open("SA-ILP/SA-ILP/bin/Release/net6.0/BestScores.txt") as file:
    lines = file.readlines()
    for line in lines:
        #line = line.replace("(","").replace(")","")
        lineSplit = line.split(";")
        bestScoreLabels.append(int(lineSplit[0]))
        bestScores.append(float(lineSplit[1].replace(",",".")))
print("Done reading files")


plt.plot(scoresLabels,scores)
plt.show()

plt.plot(bestScoreLabels,bestScores)
plt.show()