--
-- Set character set the client will use to send SQL statements to the server
--
SET NAMES 'utf8';

--
-- Set default database
--
USE chatbot;

--
-- Create table `exceptionlogs`
--
CREATE TABLE exceptionlogs (
  Id int(11) NOT NULL AUTO_INCREMENT,
  Message text NOT NULL,
  StackTrace text DEFAULT NULL,
  ExceptionType varchar(255) DEFAULT NULL,
  Path varchar(255) DEFAULT NULL,
  Method varchar(10) DEFAULT NULL,
  StatusCode int(11) DEFAULT NULL,
  Timestamp datetime DEFAULT NULL,
  User varchar(255) DEFAULT NULL,
  PRIMARY KEY (Id)
)
ENGINE = INNODB,
CHARACTER SET latin1,
COLLATE latin1_swedish_ci,
ROW_FORMAT = DYNAMIC;