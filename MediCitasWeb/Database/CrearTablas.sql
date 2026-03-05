/* ===================================================== */
/* TABLA USUARIO (NO SE MODIFICA TU ESTRUCTURA BASE)    */
/* ===================================================== */

CREATE TABLE Usuario (
    id_usuario INT IDENTITY(1,1) PRIMARY KEY,
    nombres_usuario VARCHAR(50) NOT NULL,
    apellidos_usuario VARCHAR(50) NOT NULL,
    numero_documento VARCHAR(20) NULL,
    correo_usuario VARCHAR(80) NOT NULL,
    password_usuario VARCHAR(100) NOT NULL,
    rol_usuario VARCHAR(30) NOT NULL,
    fecha_registro DATETIME NOT NULL DEFAULT GETDATE()
);



/* ===================================================== */
/* TABLA PACIENTE (SOLO RELACIÓN CON USUARIO)           */
/* ===================================================== */

CREATE TABLE Paciente (
    id_paciente INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL UNIQUE,

    CONSTRAINT fk_paciente_usuario
        FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario)
);



/* ===================================================== */
/* TABLA DOCTOR (SOLO ESPECIALIDAD COMO PEDISTE)        */
/* ===================================================== */

CREATE TABLE Doctor (
    id_doctor INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL UNIQUE,
    especialidad VARCHAR(100) NOT NULL,

    CONSTRAINT fk_doctor_usuario
        FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario)
);



/* ===================================================== */
/* TABLA CITAS (AHORA APUNTA A PACIENTE Y DOCTOR)       */
/* ===================================================== */

CREATE TABLE Citas (
    id_cita INT PRIMARY KEY IDENTITY(1,1),

    id_paciente INT NOT NULL,
    id_doctor INT NOT NULL,

    fecha_cita DATE NOT NULL,
    hora_cita TIME NOT NULL,
    especialidad VARCHAR(100) NOT NULL,
    tipo_consulta VARCHAR(100) NOT NULL,
    estado VARCHAR(20) NOT NULL DEFAULT 'Activa',
    fecha_registro DATETIME DEFAULT GETDATE(),

    CONSTRAINT fk_citas_paciente
        FOREIGN KEY (id_paciente)
        REFERENCES Paciente(id_paciente),

    CONSTRAINT fk_citas_doctor
        FOREIGN KEY (id_doctor)
        REFERENCES Doctor(id_doctor)
);




/* ===================================================== */
/* INSERTS USUARIO (LOS MISMOS QUE TENÍAS)              */
/* ===================================================== */

INSERT INTO Usuario(nombres_usuario, apellidos_usuario, numero_documento, correo_usuario, password_usuario, rol_usuario)
VALUES 
('Admin', 'Principal', '1001', 'admin@gmail.com', '123456', 'Administrador'),

('Juan', 'Perez', '2001', 'doctor@gmail.com', '123456', 'Doctor'),

('Carlos', 'Ramirez', '3001', 'paciente@gmail.com', '123456', 'Paciente')

SELECT * FROM Usuario


/* ===================================================== */
/* INSERTS DOCTOR (SOLO LOS QUE TIENEN ROL DOCTOR)      */
/* ===================================================== */

-- Juan (id_usuario = 2)
INSERT INTO Doctor(id_usuario, especialidad)
VALUES (2, 'Medicina General');




/* ===================================================== */
/* INSERTS PACIENTE (LOS QUE SON PACIENTE)               */
/* ===================================================== */

-- Carlos (id_usuario = 3)
INSERT INTO Paciente(id_usuario) VALUES (3);

-- Miguel Ángel (id_usuario = 4)
INSERT INTO Paciente(id_usuario) VALUES (4);

-- Martin (id_usuario = 5)
INSERT INTO Paciente(id_usuario) VALUES (5);

-- Mari (id_usuario = 6)
INSERT INTO Paciente(id_usuario) VALUES (6);




/* ===================================================== */
/* INSERTS CITAS (AJUSTADAS A NUEVAS RELACIONES)        */
/* ===================================================== */

-- Carlos (id_paciente = 1) con Juan (id_doctor = 1)
INSERT INTO Citas (id_paciente, id_doctor, fecha_cita, hora_cita, especialidad, tipo_consulta, estado)
VALUES (1, 1, '2026-03-10', '08:00:00', 'Medicina General', 'Primera vez', 'Activa');

-- Miguel (id_paciente = 2)
INSERT INTO Citas (id_paciente, id_doctor, fecha_cita, hora_cita, especialidad, tipo_consulta, estado)
VALUES (2, 1, '2026-03-12', '09:00:00', 'Medicina General', 'Seguimiento', 'Atendida');

-- Martin (id_paciente = 3)
INSERT INTO Citas (id_paciente, id_doctor, fecha_cita, hora_cita, especialidad, tipo_consulta, estado)
VALUES (3, 1, '2026-03-15', '10:00:00', 'Medicina General', 'Primera vez', 'Cancelada');