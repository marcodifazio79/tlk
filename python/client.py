import socket
import sys

#  for ((i=0;i<150;i++)); do  python3 client.py &   done;

# Create a TCP/IP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Connect the socket to the port where the server is listening
server_address = ('10.10.10.71', 9005)
print (sys.stderr, 'connecting to %s port %s' % server_address)
sock.connect(server_address)

try:
    while True:
        # Send data
        message = str.encode('This is the message.<-----|')
        print (sys.stderr, 'sending "%s"' % message)
        sock.sendall(message)

        # Look for the response
        amount_received = 0
        amount_expected = len(message)
        
        while amount_received < amount_expected:
            data = sock.recv(64)
            amount_received += len(data)
            print (sys.stderr, 'received "%s"' % data)
        
        input("Press any key to continue.. \n")

finally:
    print (sys.stderr, 'closing socket')
    sock.close()