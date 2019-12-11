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