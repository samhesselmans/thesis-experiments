with open("C:/Users/samca/Documents/Concorde/lin318.tsp") as file:
    test = ""
    with open("C:/Users/samca/Documents/GitHub/thesis-experiments/tsp_instances/lin318.tsp","w") as dest_file:
        test = ""
        name = file.readline().split(' ')[1]
        file.readline()
        file.readline()
        file.readline()
        file.readline()
        file.readline()
        points = []
        line = file.readline().replace('\n','')
        while line != "" and "EOF" not in line:
            lineSplit = line.split(' ')
            p = (int(lineSplit[0]),int(lineSplit[1]),int(lineSplit[2]))
            points.append(p)
            line = file.readline()
        print(points)

        dest_file.write(name + "\n")
        dest_file.write("VEHICLE\n")
        dest_file.write("NUMBER     CAPACITY\n")
        dest_file.write("  25         200\n")
        dest_file.write("\n")
        dest_file.write("CUSTOMER\n")
        dest_file.write("CUST NO.  XCOORD.   YCOORD.    DEMAND   READY TIME  DUE DATE   SERVICE   TIME\n")
        dest_file.write("\n")
        for point in points:
            dest_file.write(f"  {point[0]-1} {point[1]} {point[2]} 0 0 999999999 0\n")

