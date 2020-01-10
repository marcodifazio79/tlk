import socket
import sys
import threading


sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Bind the socket to the port
server_address = ('10.10.10.71', 9000)
print (sys.stderr, 'starting up on %s port %s' % server_address)
sock.bind(server_address)

# Listen for incoming connections
sock.listen(1)

# Wait for a connection
print (sys.stderr, 'waiting for a connection')
connection, client_address = sock.accept()

def receive_and_print():
        for message in iter(lambda: sock.recv(1024).decode(), ''):
            print("From the modem: ", message)
            


try:
    print (sys.stderr, 'connection from', client_address)
    
    background_thread = threading.Thread(target=receive_and_print)
    background_thread.daemon = True
    background_thread.start()

    while 1:
        data = input("insert command: ").encode() 
        sock.send( data )
finally:
    x=0