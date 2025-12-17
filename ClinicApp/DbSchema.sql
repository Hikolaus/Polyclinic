CREATE DATABASE polyclinic_db;
USE polyclinic_db;


CREATE TABLE users (
    Id INT NOT NULL AUTO_INCREMENT,
    Login VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role ENUM('Patient', 'Doctor', 'Administrator') NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100),
    Phone VARCHAR(20),
    RegistrationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsActive TINYINT(1) DEFAULT 1,
    PRIMARY KEY (Id),
    UNIQUE KEY (Login)
);

CREATE TABLE specializations (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(50) NOT NULL,
    Description TEXT,
    Requirements TEXT,
    AverageConsultationTime INT DEFAULT 30,
    PRIMARY KEY (Id)
);

CREATE TABLE medications (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    Form VARCHAR(50),
    DosageForms VARCHAR(100),
    Manufacturer VARCHAR(100),
    Contraindications TEXT,
    PrescriptionRequired TINYINT(1) DEFAULT 1,
    PRIMARY KEY (Id)
);

CREATE TABLE diagnoses (
    Id INT NOT NULL AUTO_INCREMENT,
    Code VARCHAR(10) NOT NULL,
    Name VARCHAR(255) NOT NULL,
    DefaultTreatment TEXT,
    DefaultRecommendations TEXT,
    PRIMARY KEY (Id),
    UNIQUE KEY (Code)
);

CREATE TABLE patients (
    Id INT NOT NULL,
    PolicyNumber VARCHAR(20) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Address TEXT,
    Gender ENUM('Male', 'Female') NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY (PolicyNumber),
    FOREIGN KEY (Id) REFERENCES users(Id) ON DELETE CASCADE
);

CREATE TABLE doctors (
    Id INT NOT NULL,
    SpecializationId INT NOT NULL,
    LicenseNumber VARCHAR(50) NOT NULL,
    Experience INT,
    Qualification VARCHAR(100),
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    UNIQUE KEY (LicenseNumber),
    FOREIGN KEY (Id) REFERENCES users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SpecializationId) REFERENCES specializations(Id)
);

CREATE TABLE administrators (
    Id INT NOT NULL,
    Department VARCHAR(50),
    Responsibilities TEXT,
    PRIMARY KEY (Id),
    FOREIGN KEY (Id) REFERENCES users(Id) ON DELETE CASCADE
);

CREATE TABLE schedules (
    Id INT NOT NULL AUTO_INCREMENT,
    DoctorId INT NOT NULL,
    DayOfWeek TINYINT NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsActive TINYINT(1) DEFAULT 1,
    BreakStart TIME,
    BreakEnd TIME,
    MaxPatients INT DEFAULT 10,
    SlotDurationMinutes INT NOT NULL DEFAULT 15,
    PRIMARY KEY (Id),
    FOREIGN KEY (DoctorId) REFERENCES doctors(Id) ON DELETE CASCADE
);

CREATE TABLE appointments (
    Id INT NOT NULL AUTO_INCREMENT,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    AppointmentDateTime DATETIME NOT NULL,
    Status VARCHAR(20) DEFAULT 'Scheduled',
    Reason VARCHAR(200),
    Notes TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    FOREIGN KEY (PatientId) REFERENCES patients(Id),
    FOREIGN KEY (DoctorId) REFERENCES doctors(Id)
);

CREATE TABLE notifications (
    Id INT NOT NULL AUTO_INCREMENT,
    UserId INT NOT NULL,
    Title VARCHAR(100) NOT NULL,
    Message VARCHAR(500) NOT NULL,
    Type VARCHAR(20),
    RelatedEntityId INT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsRead TINYINT(1) DEFAULT 0,
    PRIMARY KEY (Id),
    FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
);

CREATE TABLE waitlistrequests (
    Id INT NOT NULL AUTO_INCREMENT,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsNotified TINYINT(1) DEFAULT 0,
    PRIMARY KEY (Id),
    FOREIGN KEY (PatientId) REFERENCES patients(Id),
    FOREIGN KEY (DoctorId) REFERENCES doctors(Id)
);

CREATE TABLE medicalrecords (
    Id INT NOT NULL AUTO_INCREMENT,
    PatientId INT NOT NULL,
    AppointmentId INT NOT NULL,
    DiagnosisId INT,
    Complaints TEXT,
    Diagnosis VARCHAR(500),
    Treatment TEXT,
    Recommendations TEXT,
    Symptoms TEXT,
    TestResults TEXT,
    RecordDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    FOREIGN KEY (PatientId) REFERENCES patients(Id),
    FOREIGN KEY (AppointmentId) REFERENCES appointments(Id),
    FOREIGN KEY (DiagnosisId) REFERENCES diagnoses(Id)
);

CREATE TABLE prescriptions (
    Id INT NOT NULL AUTO_INCREMENT,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    MedicationId INT NOT NULL,
    AppointmentId INT,
    Dosage VARCHAR(50) NOT NULL,
    Instructions TEXT,
    IssueDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ExpiryDate DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active',
    RemainingRepeats INT DEFAULT 0,
    PRIMARY KEY (Id),
    FOREIGN KEY (PatientId) REFERENCES patients(Id),
    FOREIGN KEY (DoctorId) REFERENCES doctors(Id),
    FOREIGN KEY (MedicationId) REFERENCES medications(Id),
    FOREIGN KEY (AppointmentId) REFERENCES appointments(Id)
);