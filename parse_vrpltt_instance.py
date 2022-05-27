def ParseInstance(instance):
    with open(instance) as file:
        file.readline()
        f = file.readlines()
        customers = []
        for line in f:
            customers.append(line.split(',')[:8])

        return customers