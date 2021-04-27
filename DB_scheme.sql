 CREATE TABLE `Machines` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `ip_address` varchar(15) NOT NULL,
  `imei` BIGINT DEFAULT NULL,
  `mid` varchar(50) DEFAULT NULL,
  `version` varchar(10) DEFAULT NULL,
  `last_communication` timestamp DEFAULT CURRENT_TIMESTAMP,
  `time_creation` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `IsOnline` BOOLEAN NOT NULL DEFAULT true,
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
  KEY `index_ip_address` (`ip_address`),
  KEY `index_time_stamp` (`time_stamp`),
  KEY `index_id_Macchina` (`id_Macchina`),
  
  PRIMARY KEY (`id`),
  FOREIGN KEY (`id_Macchina`)
        REFERENCES Machines(id)
        ON DELETE NO ACTION

) ENGINE=InnoDB DEFAULT CHARSET=latin1;




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
        ON DELETE NO ACTION
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
        ON DELETE NO ACTION,
    FOREIGN KEY (`id_Macchina`)
        REFERENCES Machines(id)
        ON DELETE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=latin1;



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
        ON DELETE NO ACTION,
FOREIGN KEY (`ID_MachinesConnectionTrace`)
        REFERENCES MachinesConnectionTrace(id)
        ON DELETE NO ACTION,
KEY `index_ODM` (`ODM`),
KEY `index_DataCreazione` (`DataCreazione`),
KEY `index_DataInvioRichiesta` (`DataInvioRichiesta`),
KEY `index_DataPacchettoRicevuto` (`DataPacchettoRicevuto`),
KEY `index_DataSincronizzazione` (`DataSincronizzazione`),
KEY `index_ID_Machines` (`ID_Machines`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


CREATE TABLE `AlertType` (
`id` int(11) NOT NULL AUTO_INCREMENT,
`type` varchar(50) NOT NULL,
PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `AlertStatus` (
`id` int(11) NOT NULL AUTO_INCREMENT,
`status` varchar(50) NOT NULL,
PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `Alert` (
`Id` int(11) NOT NULL AUTO_INCREMENT,
`DataCreazione` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
`DataRisoluzione` datetime DEFAULT NULL,
`AlertDescription` varchar(500) NOT NULL,
`AlertSeggestedActions` varchar(500) NOT NULL,
`ID_AlertType` int(11) NOT NULL,
`ID_AlertStatus` int(11) NOT NULL,
`ResolvedBy` varchar(256) DEFAULT NULL,
PRIMARY KEY (`id`),
FOREIGN KEY (`ID_AlertType`)
        REFERENCES AlertType(id)
        ON DELETE NO ACTION,
FOREIGN KEY (`ID_AlertStatus`)
        REFERENCES AlertStatus(id)
        ON DELETE NO ACTION,
FOREIGN KEY (`ResolvedBy`)
        REFERENCES AspNetUsers(Id)
        ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `AlertTargetRole` (
`id` int(11) NOT NULL AUTO_INCREMENT,
`ID_AspNetRoles` varchar(256) NOT NULL,
`ID_Alert` int(11) NOT NULL,
PRIMARY KEY (`id`),
key `index_ID_AspNetRoles` (`ID_AspNetRoles`),
key `index_ID_Alert` (`ID_Alert`),
FOREIGN KEY (`ID_AspNetRoles`)
        REFERENCES AspNetRoles(Id)
        ON DELETE NO ACTION,
FOREIGN KEY (`ID_Alert`)
        REFERENCES Alert(Id)
        ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


