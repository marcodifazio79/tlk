# Telemetria Kiddie
Requisiti hardware e software
Macchina virtualizzata in cluster con storage HA
CPU: 8 x Xeon E5640@2,37GHz
RAM: 8Gb 
Storage: 50 Gb
SO: Linux Ubuntu 18.04.5 LTS
Ambiente di sviluppo: Asp.net Core MVC, MSVS CODE in C#
DB: MySql ver. 14.14
L’ambiente di sviluppo e collaudo è disponibile su un server di staging con ip 10.10.10.72 clone del server di produzione
L’ambiente di produzione è disponibile su un server con ip 10.10.10.71 (https:\\tlk.dedemapp.com)

Per riprisinare i sofware su Ubuntu su un'altra Macchina creare la lista dei software installati seguirel aprocedura
    
    dpkg --get-selections > installed-software.log  sul vecchio server
    dpkg --set-selections < installed-software.log  sul nuovo server
    dselect
    "i" per installare i software




### The code
~~La cartella col codice di riferimento attuale del server è net_core_v2.~~ <br/>
Da terminale, `dotnet publish tlk_core.csproj --self-contained true -r linux-x64` per compilare. <br/>
L'output è l'eseguibile /bin/netcoreapp3.1/linux-x64/publish/tlk_core

Per ricostruire il db context (metti caso che aggiungiamo tabelle), da dentro la certella Functions: `dotnet ef dbcontext scaffold 'server=10.10.10.71;port=3306;user=bot_user;password=FILLMEWITHPASSWORD;database=listener_DB' MySql.Data.EntityFrameworkCore -o database -f`
NOTA BENE: controllare il risultato! Non sempre lo scaffolding funziona, in base alla versione del server e dell'ef potrebbe ignorare delle colonne nelle tabelle, non traducendole in variabili nell'ef (aggiungerle a mano). Oppure aggiungere `-t TABLEMANE` e cambiare l'output path `-o databaseTemp` per genereare nuovi file da integrare in quelli esistenti. 

### The server
~~Fondamentale è abbassare i parametri del keepalive per il tcp del server: <br/>modificare i singoli socket aperti dall'applicazione non è efficace. Quindi: <br/>
addiungere a /etc/sysctl.conf <br/>
`net.ipv4.tcp_keepalive_time = 30` <br/>
`net.ipv4.tcp_keepalive_intvl = 10` <br/>
`net.ipv4.tcp_keepalive_probes = 6` <br/>
i parametri sopra sono di riferimento, con questi una disconnessione viene riconosciuta in circa 2 minuti al massimo. Va fatto un po' di studio per tunarli bene.~~<br/>
//set the keep alive values for the socket<br/>
`state.workSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);`<br/>
`state.workSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);`<br/>
`state.workSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 6); //old value: 16`<br/>
`state.workSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 10);`<br/>

Se tlk_core non risulta eseguibile provare `chmod a+x path/to/tlk_core`<br/>
Il 28/01/2020 è stato creato il servizio tlk_core.service, quindi: 
`sudo systemctl stop tlk_core.service` per stoppare, <br/>
`sudo systemctl start tlk_core.service` per avviare, <br/>
`sudo systemctl enable tlk_core.service` per abilitare l'avvio al boot e <br/>
`sudo systemctl disable tlk_core.service` per disabilitare l'avvio automatico. <br/>
`sudo systemctl status tlk_core.service` per controllare lo status e <br/>
`sudo journalctl -u tlk_core.service` per leggere l'output dell'eseguibile <br/>



~~La ruote sul server è da rivedere, perde la configurazione dopo ogni reboot.
`sudo ip route add 172.16.0.0/16 via 10.10.10.13 dev ens160` per ricrearla.~~ fixed

Come riferimento:
https://docs.microsoft.com/en-us/dotnet/framework/network-programming/sockets
e files nella cartella tlk_docs.
Se si modifica il file tlk_core.service copiarlo nella cartella /etc/systemd/system/

### Modem
PuTTY strumento ❤️ per la connessione al modem <br/>
Parametri 8 bit,NO parity, 1 Stop bit,9600 baud. <br/>
Nella sezione Terminal "local echo" e "local line editing" su "force on" migliorano la qualità della vita.<br/>
Una volta collegati, CTRL+C e invio per cominciare a interagire col modem. Da questo punto digitare "menu" e invio.
Password e invio. <br/>
`#ATC+QIOPEN="TCP","10.10.10.71","9000"` per stabilire una connessione tcp col server 10.10.10.71 sulla porta 9000.<br/>
`#ATC+QISEND=20` per "inizializzare" l'invio di 20 bytes. ( n = 20, sostituire n con un numero ffs.)<br/>
`#ATC+QISEND="qualcosa che verrà spedito"` il server in riceverà AT+QISEND="qualcosa<br/>
`#atc+qird=0,1,0,256` per leggere sul modem cosa ha ricevuto<br/>
Durante il normale funzionamento il modem invia due pacchetti, "anagrafica" e "monetica", i tempi di invio son programmabili (vedi LGA e LGG).
Il modem deve registarsi sul server prima di mandare i pacchetti "anagrafica" e "monetica": in pratica il modem invia un pacchetto tipo
<MID=55555553-868324022904051><VER=110> e si aspetta di ricevere qualcosa tipo `#PSW123456#ROK,de2BUl48,20/01/14,15:21:41` (formato data yy/MM/dd,hh:mm:ss) 
la parte centrale del pacchetto (de2BUl48) è ~~stata sniffata dal vecchio portum e stiamo indagando su cosa cavolo è.~~ generata secondo un algoritmo 
importato dal vecchio portum.


### TO DO:
~~Database connection -> implementere l'output sul db: WIP.
Vedi https://www.nuget.org/packages/MySql.Data.EntityFrameworkCore/ .
Migrare su sql server per linux, 
vedi https://docs.microsoft.com/en-us/sql/linux/sql-server-linux-editions-and-components-2019?view=sql-server-ver15
Creare un nuovo db e riscrivere la funzione per caricare i pacchetti ricevuti
nel nuovo db sql server 2019 EXPRESS (user kdl_admin e password uguale all'utente di sistema kdl_admin)~~

dotnet ef dbcontext scaffold 'server=10.10.10.71;port=3306;user=bot_user;password=FILLME;database=listener_DB' MySql.Data.EntityFrameworkCore -o database -f

## For older rev: 
for reference:

https://www.geeksforgeeks.org/socket-programming-multi-threading-python/

`python -u server.py | tee -a output.txt`
per avere l'output su file e terminale

mySQL db in use for test:

CREATE DATABASE dbtest;
use dbtest;
CREATE TABLE packet (ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY, rawPacket VARCHAR(500), ipAddress VARCHAR(16), port VARCHAR(6));

Creare utente con plugin 'mysql_native_password'

CREATE USER 'testwriter'@'localhost' IDENTIFIED WITH mysql_native_password BY 'password';
CREATE USER 'testwriter'@'%' IDENTIFIED BY 'password';
GRANT ALL PRIVILEGES ON databasename.* to 'testwriter'@'%';



Le gettoniere rispondono ai comandi inviati senza inviare il mid di riferimento 
    risposta corretta <TCA=00013851-04 CAS-OK > 
    risposta gett.    <TCA=&CAS-OK >
per questo motivo TLK NON PUO' ASSOCIARE LA RISPOTA AL COMANDO INVIATO e sia nella view CommandTables lo status dei comandi sarà sempre Error
 
quando il campo [35] del pacchetto cassa "<TPK=$M1") il pacchetto è stato richiesto dal comando #CAS
quando il campo [42] del pacchetto cassa "<TPK=$M2") il pacchetto è stato richiesto dal comando #CAS
quando il campo [50] del pacchetto cassa "<TPK=$M3") il pacchetto è stato richiesto dal comando #CAS
              
