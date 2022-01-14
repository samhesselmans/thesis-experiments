from sqlite3 import SQLITE_CREATE_TEMP_INDEX
from tracemalloc import start
import numpy as np
import random
import time
import math
test = [[1],[2],[3]]
test2 = np.array([[1],[2],[3]])
start_time = time.time()
for _ in range(3000000):
    a = test[0][0]
    #math.sqrt(pow(20 -40,2) + pow(30 - 60,2))
print(time.time()-start_time)
start_time = time.time()


for _ in range(3000000):
    b = test2[0,0]
    #np.linalg.norm([20-40,30-60])
    #np.random.randint(0, high=100)
print(time.time()-start_time)   