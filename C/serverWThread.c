#include<stdio.h>
#include<stdlib.h>
#include<time.h>




#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(__NT__)
   //define something for Windows (32-bit and 64-bit, this part is common)
   #ifdef _WIN64
      //define something for Windows (64-bit only)
   #else
      //define something for Windows (32-bit only)
   #endif

#elif __linux__
    
    #include<sys/socket.h>
    #include<netinet/in.h>
    #include<string.h>
    #include <arpa/inet.h>
    #include <fcntl.h> 
    #include <unistd.h> 
    #include <sys/types.h>
    #include <sys/wait.h>
    #include <pthread.h>
   
#elif __unix__ // all unices not caught above
    // Unix
#elif defined(_POSIX_VERSION)
    // POSIX
#else
#   error "Unknown compiler"
#endif
int sendall(int s, char *buf, int *len)
{
    int total = 0;        // how many bytes we've sent 
    int bytesleft = *len; // how many we have left to send
    int n;

    while(total < *len) {
        n = send(s, buf+total, bytesleft, 0);
        if (n == -1) { break; }
        total += n;
        bytesleft -= n;
    }

    *len = total; // return number actually sent here

    return n==-1?-1:0; // return -1 on failure, 0 on success
}

void  socketThread(int  clientSocket)
{
    int newSocket = clientSocket;       
    int recvResult = 0, sendResult = 0;
    int commandLenght = 15;
    while(1){
        char client_message[2000];
        recvResult = recv(newSocket , client_message , 2000 , 0);
        if(recvResult == 0)
        {
            printf("Disconnected from client");
            break;
        }
        time_t mytime = time(NULL);
        char * time_str = ctime(&mytime);
        time_str[strlen(time_str)-1] = '\0';
        //printf("Current Time : %s\n", time_str);
        printf("%s: Data received: %s\n",time_str,client_message);
        // Send message to the client socket 
        //pthread_mutex_unlock(&lock);
        sleep(1);
        //send(newSocket,buffer,13,0);
        sendResult = sendall(newSocket,"#PSW123456#PU1",&commandLenght);
        //send(newSocket,"#PSW123456#PU1",14,0);
        
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