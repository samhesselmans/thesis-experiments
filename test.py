import traceback

def Test():
    a = [1,2,3]
    traceback.print_stack(limit=50)

Test()