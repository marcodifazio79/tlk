from tkinter import *
import tkinter.scrolledtext
import socket 
import sys
import os
import time
from _thread import *
import threading 
import MySQLdb


#database config
config = {
    'host': '192.168.122.208',
    'port': 3306,
    'user': 'db_user',
    'password': 'Qwerty.12',
    'database': 'test_db'
}
db_user = config.get('user')
db_pwd  = config.get('password')
db_host = config.get('host')
db_port = config.get('port')
db_name = config.get('database')

#mariadb_connection = MySQLdb.connect(user=db_user, password=db_pwd, db=db_name , host=db_host)

#cursor = mariadb_connection.cursor()
#cursor.execute("SELECT ID, rawPackt FROM packet ")
#for ID in cursor:
#    print("rawPacket: {}").format(ID)


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

#controlla se in linea
def aliveCheck():
    
    response = os.system("ping -c 1 8.8.8.8")
    if response == 0:
        lineCheckLabel = Label (root, text ="") #let's reset the label, it gets strange sometimes =\
        lineCheckLabel.grid(row=1, column = 0)
        lineCheckLabel = Label (root, text ="Is my line alive: YES", fg="green")
        lineCheckLabel.grid(row=1, column = 0)
    else:
        lineCheckLabel = Label (root, text ="Is my line alive: NO, down since " + time.strftime('%H:%M:%S'), fg="red")  
        lineCheckLabel.grid(row=1, column = 0)  
    lineCheckLabel.after(30000, aliveCheck) #30 secondi, porterei anche a un minuto. Poi vediamo.
aliveCheck()


print_lock = threading.Lock() 
# thread fuction 
def threaded(c,ip,tcpPort): 
	while True: 

		# data received from client 
		data = c.recv(64) 
		if not data: 
			txt.insert(INSERT, "No data, bye\n")
			print('No data, bye') 
			
			# lock released on exit 
			print_lock.release() 
			break
		txt.insert(INSERT, "received: " + str(data)+"\n")
		insertPacketInDB(  data, tcpPort ,ip )
		print ('received "%s"' % data)
		lastReceivedPacket.config(text= "Last packed arrived at: "+ time.strftime('%H:%M:%S') )

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
	txt.insert(INSERT, "socket is listening\n")
	print("socket is listening") 

	# a forever loop until client wants to exit 
	while True: 

		# establish connection with client 
		c, addr = s.accept() 

		# lock acquired by client 
		print_lock.acquire() 
		txt.insert(INSERT, "Connected to :" + str(addr[0] ) + ':' +  str(addr[1]) + "\n")
		print('Connected to :', addr[0], ':', addr[1]) 

		# Start a new thread and return its identifier 
		start_new_thread(threaded, (c,str(addr[0]),str(addr[1]),)) 
	s.close() 

def insertPacketInDB(packetReceived, port, ip):
    mariadb_connection = MySQLdb.connect(user=db_user, password=db_pwd, db=db_name , host=db_host)
    cursor = mariadb_connection.cursor()
    cursor.execute("""INSERT IGNORE INTO packet (rawPackt , ipAddress , port) VALUES (%s,%s,%s)""", (packetReceived,ip,port,))
    mariadb_connection.commit()
    mariadb_connection.close()



if __name__ == '__main__': 
    	
    start_new_thread (Main) 














root.mainloop()

