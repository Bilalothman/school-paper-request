-- School Paper Request database for XAMPP MariaDB/MySQL
-- Import this file from phpMyAdmin. The backend safely seeds the two hashed
-- development users the first time it starts.

CREATE DATABASE IF NOT EXISTS `school_paper_requests`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE `school_paper_requests`;

CREATE TABLE IF NOT EXISTS `Services` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) NOT NULL,
  `Description` varchar(500) NOT NULL,
  CONSTRAINT `PK_Services` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FullName` varchar(100) NOT NULL,
  `Email` varchar(200) NOT NULL,
  `PasswordHash` longtext NOT NULL,
  `Role` varchar(20) NOT NULL,
  CONSTRAINT `PK_Users` PRIMARY KEY (`Id`),
  CONSTRAINT `IX_Users_Email` UNIQUE (`Email`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Requests` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `StudentId` int NOT NULL,
  `ServiceId` int NOT NULL,
  `Note` varchar(1000) NULL,
  `Status` varchar(20) NOT NULL DEFAULT 'Submitted',
  `AdminComment` varchar(1000) NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `CamundaProcessInstanceId` varchar(100) NULL,
  CONSTRAINT `PK_Requests` PRIMARY KEY (`Id`),
  CONSTRAINT `FK_Requests_Services_ServiceId` FOREIGN KEY (`ServiceId`) REFERENCES `Services` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Requests_Users_StudentId` FOREIGN KEY (`StudentId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
  INDEX `IX_Requests_ServiceId` (`ServiceId`),
  INDEX `IX_Requests_StudentId` (`StudentId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260809081230_InitialMySqlCreate', '9.0.10');

INSERT INTO `Services` (`Id`, `Name`, `Description`) VALUES
  (1, 'Enrollment Certificate', 'Official certificate proving student enrollment.'),
  (2, 'Grade Transcript', 'Official academic grade transcript.'),
  (3, 'Attendance Certificate', 'Official attendance certificate.')
ON DUPLICATE KEY UPDATE
  `Name` = VALUES(`Name`),
  `Description` = VALUES(`Description`);
