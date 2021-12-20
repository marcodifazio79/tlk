 CREATE TABLE `Machines` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `ip_address` varchar(15) NOT NULL,
  `imei` BIGINT DEFAULT NULL,
  `mid` varchar(50) DEFAULT NULL,
  `version` varchar(10) DEFAULT NULL,
  `last_communication` timestamp DEFAULT CURRENT_TIMESTAMP,
  `time_creation` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IsOnline` BOOLEAN NOT NULL DEFAULT true,
  `MarkedBroken` BOOLEAN NOT NULL DEFAULT false,
  `LogEnabled` BOOLEAN NOT NULL DEFAULT false,
  PRIMARY KEY (`id`),
  KEY `index_ip_mid` (`ip_address`,`mid`),
  UNIQUE KEY `index_mid` (`mid`),
  UNIQUE KEY `index_imei` (`imei`),
  UNIQUE KEY `index_ip_address` (`ip_address`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `MachinesConnectionTrace` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `time_stamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ip_address` varchar(15) NOT NULL,
  `send_or_recv` varchar(4) NOT NULL,
  `transferred_data` varchar(10000) NOT NULL,
  `id_Macchina` int(11) DEFAULT NULL,
  `telemetria_status` int(1) DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `index_ip_address` (`ip_address`),
  KEY `index_time_stamp` (`time_stamp`),
  KEY `index_id_Macchina` (`id_Macchina`),
  KEY `index_telemetria_status` (`telemetria_status`),
  KEY `index_telemetria_status_time_stamp` (`telemetria_status`,`time_stamp`),
  CONSTRAINT `MachinesConnectionTrace_ibfk_1` FOREIGN KEY (`id_Macchina`) REFERENCES `Machines` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=536872 DEFAULT CHARSET=latin1;


 CREATE TABLE `Machines_InMemory` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `ip_address` varchar(15) NOT NULL,
  `tcp_local_port` int(11) NOT NULL,
  `mid` varchar(50) DEFAULT NULL,
  `time_creation` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `mid` (`mid`),
  KEY `index_ip_address` (`ip_address`),
  KEY `index_mid` (`mid`),
  KEY `index_local_port` (`tcp_local_port`)
) ENGINE=MEMORY DEFAULT CHARSET=latin1;

 CREATE TABLE `RemoteCommand` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `body` varchar(10000) NOT NULL,
  `ReceivedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `SendedAt` datetime DEFAULT NULL,
  `AnsweredAt` datetime DEFAULT NULL,
  `Sender` varchar(15) NOT NULL,
  `id_Macchina` int(11) DEFAULT NULL,
  `LifespanSeconds` int(11) DEFAULT 15,
  `Status` varchar(15) NOT NULL,
  KEY `index_ID_Macchina` (`id_Macchina`),
  PRIMARY KEY (`id`),
  FOREIGN KEY (`id_Macchina`)
        REFERENCES Machines(id)
        ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=22516 DEFAULT CHARSET=latin1;



 CREATE TABLE `CommandsMatch` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `ModemCommand` varchar(100) NOT NULL,
  `WebCommand` varchar(30) NOT NULL,
  `expectedAnswer` varchar(50) NOT NULL,
  `IsParameterizable` BOOLEAN NOT NULL DEFAULT false,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=22516 DEFAULT CHARSET=latin1;

 CREATE TABLE `Attr` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  `Comment` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)  
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=latin1;


 CREATE TABLE `MachinesAttributes` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `id_Macchina` int(11) NOT NULL,
  `id_Attribute` int(11) NOT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Value` varchar(50) NOT NULL,
  PRIMARY KEY (`id`),
  FOREIGN KEY (`id_Attribute`)
        REFERENCES Attr(id)
        ON DELETE CASCADE,
    FOREIGN KEY (`id_Macchina`)
        REFERENCES Machines(id)
        ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=latin1;

CREATE TABLE `DatiCassa` (
`Id` int(11) NOT NULL AUTO_INCREMENT,
`Odm` varchar(50) NOT NULL,
`Mid` varchar(50) DEFAULT NULL,
`TransferredData` varchar(10000) NOT NULL,
`JsonData` varchar(10000) NOT NULL,
`Status` varchar(50) NOT NULL,
`DataSentToTelem` datetime DEFAULT NULL,
PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `CashTransaction` (
`id` int(11) NOT NULL AUTO_INCREMENT,
`ODM` varchar(50) NOT NULL,
`ID_Machines` int(11) NOT NULL,
`ID_MachinesConnectionTrace` int(11) NOT NULL,
`Status` varchar(50) NOT NULL,
`DataCreazione` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
`DataInvioRichiesta` datetime DEFAULT NULL,
`DataPacchettoRicevuto` datetime DEFAULT NULL,
`DataSincronizzazione` datetime DEFAULT NULL,
`TentativiAutomaticiEseguiti` int(11) DEFAULT 0,
PRIMARY KEY (`id`),
FOREIGN KEY (`ID_Machines`)
        REFERENCES Machines(id)
        ON DELETE CASCADE,
FOREIGN KEY (`ID_MachinesConnectionTrace`)
        REFERENCES MachinesConnectionTrace(id)
        ON DELETE CASCADE,
KEY `index_ODM` (`ODM`),
KEY `index_DataCreazione` (`DataCreazione`),
KEY `index_DataInvioRichiesta` (`DataInvioRichiesta`),
KEY `index_DataPacchettoRicevuto` (`DataPacchettoRicevuto`),
KEY `index_DataSincronizzazione` (`DataSincronizzazione`),
KEY `index_ID_Machines` (`ID_Machines`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


CREATE TABLE `LogType` (
`id` int(11) NOT NULL AUTO_INCREMENT,
`type` varchar(50) NOT NULL,
PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

INSERT INTO `LogType`(type) VALUES ('Informational'),('Alert'),('Critical'),('Error');

CREATE TABLE `LogStatus` (
`id` int(11) NOT NULL AUTO_INCREMENT,
`status` varchar(50) NOT NULL,
PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
INSERT INTO `LogStatus`(status) VALUES ('NoActionNeeded'),('Solved'),('ActionNeeded!');


CREATE TABLE `Log` (
`Id` int(11) NOT NULL AUTO_INCREMENT,
`DataCreazione` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
`DataRisoluzione` datetime DEFAULT NULL,
`LogDescription` varchar(500) NOT NULL,
`LogSeggestedActions` varchar(500) DEFAULT NULL,
`linkToRelevantLocation` varchar(1024) DEFAULT NULL,
`ID_LogType` int(11) NOT NULL,
`ID_LogStatus` int(11) NOT NULL,
`ID_user` varchar(256) DEFAULT NULL,
`ID_machine` int(11) DEFAULT NULL,
PRIMARY KEY (`id`),
KEY `index_ID_user` (`ID_user`),
KEY `index_ID_LogType` (`ID_LogType`),
KEY `index_DataCreazione` (`DataCreazione`),
KEY `index_ID_machine` (`ID_machine`),
FOREIGN KEY (`ID_LogType`)
        REFERENCES LogType(id)
        ON DELETE NO ACTION,
FOREIGN KEY (`ID_machine`)
        REFERENCES Machines(id)
        ON DELETE NO ACTION,
FOREIGN KEY (`ID_LogStatus`)
        REFERENCES LogStatus(id)
        ON DELETE NO ACTION,
FOREIGN KEY (`ID_user`)
        REFERENCES AspNetUsers(Id)
        ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `LogTargetRole` (
`id` int(11) NOT NULL AUTO_INCREMENT,
`ID_AspNetRoles` varchar(256) NOT NULL,
`ID_Log` int(11) NOT NULL,
PRIMARY KEY (`id`),
key `index_ID_AspNetRoles` (`ID_AspNetRoles`),
key `index_ID_Log` (`ID_Log`),
FOREIGN KEY (`ID_AspNetRoles`)
        REFERENCES AspNetRoles(Id)
        ON DELETE NO ACTION,
FOREIGN KEY (`ID_Log`)
        REFERENCES Log(Id)
        ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE USER 'telemetria_ftd'@'10.10.10.%' IDENTIFIED BY 'Tghdje@#!!299_#';
GRANT select,update on listener_DB.MachinesConnectionTrace  TO 'telemetria_ftd'@'10.10.10.%';

CREATE USER 'alberto_ro'@'%' IDENTIFIED BY 'SDCko@#!12';
GRANT select on listener_DB.*  TO 'alberto_ro'@'%';