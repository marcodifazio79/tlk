# Telemetria Kiddie

### The code
La cartella col codice di riferimento attuale del server è net_core_v2. <br/>
Da terminale, cd nella cartella net_core_v2 e 
`dotnet build --runtime ubuntu.18.04-x64` per compilare. <br/>
L'output è l'eseguibile /net_core_v2/bin/Debug/netcoreapp3.1/ubuntu.18.04-x64/tlk_core

### The server
Se tlk_core non risulta eseguibile provare `chmod a+x path/to/tlk_core`<br/>
Il 28/01/2020 è stato creato il servizio tlk_core.service, quindi: 
`sudo systemctl stop tlk_core.service` per stoppare, <br/>
`sudo systemctl start tlk_core.service` per avviare, <br/>
`sudo systemctl enable tlk_core.service` per abilitare l'avvio al boot e <br/>
`sudo systemctl disable tlk_core.service` per disabilitare l'avvio automatico. <br/>
`sudo systemctl status tlk_core.service` per controllare lo status e <br/>
`sudo journalctl -u tlk_core.service` per leggere l'output dell'eseguibile <br/>




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


### TO DO:
Database connection -> implementere l'output sul db: WIP.
Vedi https://www.nuget.org/packages/MySql.Data.EntityFrameworkCore/ .



## For older rev: 
for reference:

https://www.geeksforgeeks.org/socket-programming-multi-threading-python/

`python -u server.py | tee -a output.txt`
per avere l'output su file e terminale

mySQL db in use for test:

CREATE DATABASE test_db;
use test_db;
CREATE TABLE packet (ID INT NOT NULL AUTO_INCREMENT PRIMARY KEY, rawPacket VARCHAR(500), ipAddress VARCHAR(16), port VARCHAR(6));
CREATE USER 'db_user'@'%' IDENTIFIED BY 'Qwerty.12';
GRANT ALL PRIVILEGES ON test_db.* to 'db_user'@'%';
