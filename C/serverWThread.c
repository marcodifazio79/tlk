#include<stdio.h>
#include<stdlib.h>
#include<time.h>
#include<sys/socket.h>
#include<netinet/in.h>
#include<string.h>
#include <arpa/inet.h>
#include <fcntl.h> // for open
#include <unistd.h> // for close
#include <sys/types.h>
#include <sys/wait.h>
#include <pthread.h>
void  socketThread(int  clientSocket)
{
    int newSocket = clientSocket;       
    while(1){
        char client_message[2000];
        recv(newSocket , client_message , 2000 , 0);

        time_t mytime = time(NULL);
        char * time_str = ctime(&mytime);
        time_str[strlen(time_str)-1] = '\0';
        //printf("Current Time : %s\n", time_str);

        printf("%s: Data received: %s\n",time_str,client_message);
        // Send message to the client socket 
        //pthread_mutex_unlock(&lock);
        sleep(1);
        //send(newSocket,buffer,13,0);
        send(newSocket,"#PSW123456#PU1",14,0);
        
    }
    printf("Exit socketThread \n");
    close(newSocket);
}

int main(){
  int serverSocket, newSocket;
  struct sockaddr_in serverAddr;
  struct sockaddr_storage serverStorage;
  socklen_t addr_size;
  pid_t pid[50];

  //Create the socket. 
  serverSocket = socket(PF_INET, SOCK_STREAM, 0);
  
  // Configure settings of the server address struct
  // Address family = Internet 
  serverAddr.sin_family = AF_INET;
  //Set port number, using htons function to use proper byte order 
  serverAddr.sin_port = htons(9000);

  //Set IP address to localhost 
  serverAddr.sin_addr.s_addr = inet_addr("10.10.10.71");

  //Set all bits of the padding field to 0 
  memset(serverAddr.sin_zero, '\0', sizeof serverAddr.sin_zero);
  //Bind the address struct to the socket 
  bind(serverSocket, (struct sockaddr *) &serverAddr, sizeof(serverAddr));
  //Listen on the socket, with 40 max connection requests queued 
  if(listen(serverSocket,50)==0)
        printf("Listening\n");
  else
        printf("Error\n");
    pthread_t tid[60];
    int i = 0;
    while(1)
        {
            /*---- Accept call creates a new socket for the incoming connection ----*/
            addr_size = sizeof serverStorage;
            newSocket = accept(serverSocket, (struct sockaddr *) &serverStorage, &addr_size);
            
            int pid_c = 0;
        if ((pid_c = fork())==0)
            {
                socketThread(newSocket);
            }
        else
            {
            pid[i++] = pid_c;
            if( i >= 49)
            {
                i = 0;
                while(i < 50)
                    waitpid(pid[i++], NULL, 0);
                i = 0;
            }
            }
        }
  return 0;
}