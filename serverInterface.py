from tkinter import *
import tkinter.scrolledtext
import socket 
import sys
import time
from _thread import *
import threading 
root = Tk()

#non ridimensionabile
root.resizable(0, 0)

#dimensione fissa
#root.geometry("800x600")

orologio = Label(root, text= "HH:mm:SS")
orologio.grid(row=0,column=0, columnspan=2, sticky=W+E+N+S )

lineCheckLabel = Label (root, text ="Is my line alive: ")
lineCheckLabel.grid(row=1, column = 0)

lastReceivedPacket = Label (root, text="Last packed arrived at: ")
lastReceivedPacket.grid(row=2, column =0)

txt = tkinter.scrolledtext.ScrolledText(root, undo=True, height = 10)
txt['font'] = ('consolas', '12')
txt.grid(row=3, column=0, columnspan=2, sticky=W+E+N+S , padx=5, pady=5)

#txt.insert(INSERT, "When Im with you baby I go out of my head And I just cant get enough And I just cant get enough All the things you do to me And everything you said I just cant get enough I just cant get enough")

#aggiorna l'orologio
def tick():
    orologio.config(text=time.strftime('%H:%M:%S'))
    orologio.after(250, tick)
tick()

print_lock = threading.Lock() 
# thread fuction 
def threaded(c): 
	while True: 

		# data received from client 
		data = c.recv(64) 
		if not data: 
			print('No data, bye') 
			
			# lock released on exit 
			print_lock.release() 
			break
		print ('received "%s"' % data)
        
        # reverse the given string from client 
		data = data[::-1] 

		# send back reversed string to client 
		c.send(data) 

	# connection closed 
	c.close() 


def Main(): 
	host = "192.168.17.206" 

	# reverse a port on your computer 
	# in our case it is 10000 but it 
	# can be anything 
	port = 10000
	s = socket.socket(socket.AF_INET, socket.SOCK_STREAM) 
	s.bind((host, port))	
	txt.insert(INSERT, "socket binded to port " + str( port) + "\n")
	print("socket binded to port", port) 

	# put the socket into listening mode 
	s.listen(5) 
	txt.insert(INSERT, "socket is listening")
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
    	
    start_new_thread (Main) 














root.mainloop()

