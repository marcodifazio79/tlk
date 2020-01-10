# import socket programming library 
import socket 

# import thread module 
from _thread import *
import threading 

print_lock = threading.Lock() 

# thread fuction 
def threaded(c): 
	while True: 

		# data received from client 
		#print ('test')
		data = c.recv(8) 
		if not data: 
			print('No data, bye') 
			
			# lock released on exit 
			print_lock.release() 
			break
		print ('received "%s"' % data)        
        
		data = '#PU1' 
		
		c.send(data.encode()) 

	# connection closed 
	c.close() 


def Main(): 
	host = "10.10.10.71" 
	
	# reverse a port on your computer 
	# in our case it is 10000 but it 
	# can be anything 
	port = 9000
	s = socket.socket(socket.AF_INET, socket.SOCK_STREAM) 
	s.bind((host, port)) 
	print("socket binded to port", port) 

	# put the socket into listening mode 
	s.listen(5) 
	print("socket is listening") 

	# a forever loop until client wants to exit 
	while True: 

		# establish connection with client 
		c, addr = s.accept() 

		# lock acquired by client 
		print_lock.acquire() 
		print('Connected to :', addr[0], ':', addr[1]) 

		# Start a new thread and return its identifier 
		start_new_thread(threaded, (c,)) 
	s.close() 


if __name__ == '__main__': 
	Main() 
