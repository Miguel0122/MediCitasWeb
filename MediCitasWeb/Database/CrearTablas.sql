/* ===================================================== */
/* 1. TABLA USUARIO (CON RESTRICCIONES DE UNICIDAD)      */
/* ===================================================== */
CREATE TABLE Usuario (
    id_usuario INT IDENTITY(100,1) PRIMARY KEY,
    nombres_usuario VARCHAR(50) NOT NULL,
    apellidos_usuario VARCHAR(50) NOT NULL,
    numero_documento VARCHAR(20) NOT NULL UNIQUE, -- No admite documentos duplicados
    correo_usuario VARCHAR(80) NOT NULL UNIQUE,    -- No admite correos duplicados
    password_usuario VARCHAR(100) NOT NULL,
    rol_usuario VARCHAR(30) NOT NULL,
    fecha_registro DATETIME NOT NULL DEFAULT GETDATE()
);

/* ===================================================== */
/* 2. TABLA PACIENTE                                     */
/* ===================================================== */
CREATE TABLE Paciente (
    id_paciente INT IDENTITY(200,1) PRIMARY KEY,
    id_usuario INT NOT NULL UNIQUE,
    FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario)
);

/* ===================================================== */
/* 3. TABLA DOCTOR                                       */
/* ===================================================== */
CREATE TABLE Doctor (
    id_doctor INT IDENTITY(300,1) PRIMARY KEY,
    id_usuario INT NOT NULL UNIQUE,
    especialidad VARCHAR(100) NOT NULL,
    FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario)
);

/* ===================================================== */
/* 4. TABLA CITAS (EL MOTOR DEL SISTEMA)                 */
/* ===================================================== */
CREATE TABLE Citas (
    id_cita INT PRIMARY KEY IDENTITY(400,1),
    id_paciente INT NOT NULL,
    id_doctor INT NOT NULL,
    fecha_cita DATE NOT NULL,
    hora_cita TIME NOT NULL,
    especialidad VARCHAR(100) NOT NULL,
    tipo_consulta VARCHAR(100) NOT NULL,
    -- Estados dinámicos para el flujo del doctor
    estado VARCHAR(20) NOT NULL DEFAULT 'Activa', -- Activa, Atendida, Cancelada, Inasistencia
    observaciones VARCHAR(MAX) NULL,              -- Diagnóstico / anotaciones del doctor
    fecha_registro DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (id_paciente) REFERENCES Paciente(id_paciente),
    FOREIGN KEY (id_doctor) REFERENCES Doctor(id_doctor),

    -- Validación de horario EPS (6:00 AM a 6:00 PM)
    CONSTRAINT CHK_HoraCita CHECK (hora_cita >= '06:00:00' AND hora_cita <= '18:00:00')
);

/* ===================================================== */
/* 5. TABLA CHAT_SESIONES                                */
/* ===================================================== */
CREATE TABLE ChatSesiones (
    id_sesion INT IDENTITY(1,1) PRIMARY KEY,
    id_usuario INT NOT NULL,
    fecha_inicio DATETIME NOT NULL DEFAULT GETDATE(),
    fecha_fin DATETIME NULL,
    FOREIGN KEY (id_usuario) REFERENCES Usuario(id_usuario)
);

/* ===================================================== */
/* 6. TABLA CHAT_MENSAJES                                */
/* ===================================================== */
CREATE TABLE ChatMensajes (
    id_mensaje INT IDENTITY(1,1) PRIMARY KEY,
    id_sesion INT NOT NULL,
    rol VARCHAR(10) NOT NULL,          -- 'user' o 'assistant'
    contenido VARCHAR(MAX) NOT NULL,
    fecha_mensaje DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (id_sesion) REFERENCES ChatSesiones(id_sesion)
);

/* ===================================================== */
/* 7. TABLA FAQ (PREGUNTAS FRECUENTES)                   */
/* ===================================================== */
CREATE TABLE ChatFAQ (
    id_faq INT IDENTITY(1,1) PRIMARY KEY,
    pregunta VARCHAR(200) NOT NULL,
    respuesta VARCHAR(MAX) NOT NULL,
    activo BIT NOT NULL DEFAULT 1
);

/* ===================================================== */
/* 8. ÍNDICES PRO (EVITAN CRUCES DE HORARIOS)            */
/* ===================================================== */

-- Un doctor no puede tener 2 citas el mismo día a la misma hora (citas activas)
CREATE UNIQUE INDEX UQ_CitaUnica_Doctor 
ON Citas(id_doctor, fecha_cita, hora_cita) 
WHERE estado = 'Activa';

-- Un paciente no puede agendarse 2 veces el mismo día a la misma hora (citas activas)
CREATE UNIQUE INDEX UQ_CitaUnica_Paciente 
ON Citas(id_paciente, fecha_cita, hora_cita) 
WHERE estado = 'Activa';


/* FAQs iniciales para el ChatBot */
INSERT INTO ChatFAQ (pregunta, respuesta) VALUES
('¿Cómo agendo una cita?', 
 'Ve al menú principal y haz clic en ""Agendar Cita"". Selecciona la especialidad, doctor, fecha y hora disponible.'),
('¿Cómo cancelo una cita?', 
 'En ""Mis Citas"" encontrarás el botón Cancelar junto a cada cita activa.'),
('¿Cuáles son los horarios de atención?', 
 'Nuestro horario es de 6:00 AM a 6:00 PM de lunes a sábado.'),
('¿Cómo recupero mi contraseña?', 
 'En la pantalla de login haz clic en ""¿Olvidaste tu contraseña?"" e ingresa tu correo.'),
('¿Tienen atención de urgencias?', 'MediCitas es para citas programadas. Si tienes una emergencia vital, por favor acude al hospital más cercano.'),
('¿Qué documentos debo llevar?', 'Solo necesitas tu documento de identidad original y llegar 15 minutos antes de la cita.'),
('¿Puedo cambiar mi doctor?', 'Sí, siempre que haya disponibilidad, puedes cancelar tu cita actual y agendar una nueva con el doctor de tu preferencia.');